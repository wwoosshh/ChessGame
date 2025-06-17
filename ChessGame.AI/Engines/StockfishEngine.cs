using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ChessGame.AI.Adapters;
using ChessGame.Core.Models.Game;

namespace ChessGame.AI.Engines
{
    public class StockfishEngine : IChessEngine, IDisposable
    {
        private Process? _stockfishProcess;
        private StreamWriter? _inputWriter;
        private StreamReader? _outputReader;
        private readonly FENAdapter _fenAdapter;
        private readonly UCIAdapter _uciAdapter;
        private readonly object _lock = new object();
        private int _skillLevel = 10;
        private int _eloRating = 1000;
        private readonly Random _random = new Random();

        // 오프닝 다양성을 위한 설정
        private int _multipv = 3; // 여러 후보수를 고려
        private double _randomnessFactor = 0.2; // 20% 확률로 차선책 선택

        public StockfishEngine()
        {
            _fenAdapter = new FENAdapter();
            _uciAdapter = new UCIAdapter();
        }

        public async Task InitializeAsync()
        {
            var enginePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "Engines", "stockfish.exe");

            if (!File.Exists(enginePath))
            {
                throw new FileNotFoundException($"Stockfish engine not found at: {enginePath}");
            }

            _stockfishProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = enginePath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _stockfishProcess.Start();
            _inputWriter = _stockfishProcess.StandardInput;
            _outputReader = _stockfishProcess.StandardOutput;

            // UCI 프로토콜 초기화
            await SendCommandAsync("uci");
            await WaitForResponseAsync("uciok");

            // 옵션 설정
            await SendCommandAsync($"setoption name Skill Level value {_skillLevel}");
            await SendCommandAsync($"setoption name MultiPV value {_multipv}");
            await SendCommandAsync("setoption name Threads value 1");
            await SendCommandAsync("setoption name Hash value 128");
            await SendCommandAsync("isready");
            await WaitForResponseAsync("readyok");
        }

        public async Task<Move> GetBestMoveAsync(GameState gameState, int depth = 10)
        {
            if (_stockfishProcess == null || _inputWriter == null || _outputReader == null)
                throw new InvalidOperationException("Engine not initialized");

            // FEN 포지션 설정
            string fen = _fenAdapter.BoardToFEN(gameState);
            await SendCommandAsync($"position fen {fen}");

            // 난이도에 따른 깊이 조정
            int adjustedDepth = AdjustDepthByDifficulty(depth);

            // MultiPV로 여러 후보수 계산
            await SendCommandAsync($"go depth {adjustedDepth}");

            // 모든 후보수 수집
            var candidates = new List<MoveCandidate>();
            string? line;

            while ((line = await _outputReader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("info depth"))
                {
                    var candidate = ParseInfoLine(line);
                    if (candidate != null)
                        candidates.Add(candidate);
                }
                else if (line.StartsWith("bestmove"))
                {
                    break;
                }
            }

            // 후보수 중에서 선택
            return SelectMoveFromCandidates(candidates, gameState);
        }

        private MoveCandidate? ParseInfoLine(string line)
        {
            try
            {
                var parts = line.Split(' ');
                int pvIndex = Array.IndexOf(parts, "multipv");
                int cpIndex = Array.IndexOf(parts, "cp");
                int pvMoveIndex = Array.IndexOf(parts, "pv");

                if (pvIndex >= 0 && cpIndex >= 0 && pvMoveIndex >= 0)
                {
                    return new MoveCandidate
                    {
                        PvNumber = int.Parse(parts[pvIndex + 1]),
                        Score = int.Parse(parts[cpIndex + 1]),
                        Move = parts[pvMoveIndex + 1]
                    };
                }
            }
            catch { }

            return null;
        }

        private Move SelectMoveFromCandidates(List<MoveCandidate> candidates, GameState gameState)
        {
            if (!candidates.Any())
                throw new Exception("No move candidates found");

            // PV별로 최고 점수 후보만 유지
            var bestCandidates = candidates
                .GroupBy(c => c.PvNumber)
                .Select(g => g.OrderByDescending(c => c.Score).First())
                .OrderByDescending(c => c.Score)
                .ToList();

            // 게임 초반(오프닝)에는 더 많은 변수 추가
            bool isOpening = gameState.MoveHistory.Count < 10;
            double randomChance = isOpening ? _randomnessFactor * 1.5 : _randomnessFactor;

            // 난이도에 따른 실수 확률
            if (_eloRating < 800)
                randomChance *= 2; // 낮은 레이팅에서는 더 많은 실수

            MoveCandidate selectedCandidate;

            // 랜덤하게 차선책 선택
            if (_random.NextDouble() < randomChance && bestCandidates.Count > 1)
            {
                // 점수 차이가 너무 크지 않은 후보들 중에서 선택
                var viableCandidates = bestCandidates
                    .Where(c => bestCandidates[0].Score - c.Score < 100)
                    .ToList();

                int index = _random.Next(Math.Min(viableCandidates.Count, 3));
                selectedCandidate = viableCandidates[index];
            }
            else
            {
                selectedCandidate = bestCandidates[0];
            }

            // 가끔 블런더 추가 (낮은 난이도에서만)
            if (_eloRating < 600 && _random.NextDouble() < 0.1)
            {
                // 최악의 수 중 하나 선택
                var blunders = bestCandidates.OrderBy(c => c.Score).Take(2).ToList();
                selectedCandidate = blunders[_random.Next(blunders.Count)];
            }

            return _uciAdapter.UCIToMove(selectedCandidate.Move);
        }

        private int AdjustDepthByDifficulty(int baseDepth)
        {
            // 레이팅에 따른 깊이 조정
            if (_eloRating < 500)
                return Math.Max(1, baseDepth - 7);
            else if (_eloRating < 800)
                return Math.Max(2, baseDepth - 5);
            else if (_eloRating < 1200)
                return Math.Max(3, baseDepth - 3);
            else if (_eloRating < 1500)
                return Math.Max(5, baseDepth - 1);
            else
                return baseDepth;
        }

        public async Task SetDifficultyAsync(int level)
        {
            // ELO 레이팅 설정
            _eloRating = level switch
            {
                1 => 400,   // 쉬움 (300-500)
                2 => 600,
                3 => 800,
                4 => 1000,  // 보통 (500-1000) 
                5 => 1200,
                6 => 1400,
                7 => 1600,  // 어려움 (1500+)
                8 => 1800,
                9 => 2000,
                10 => 2200,
                _ => 1000
            };

            // Stockfish Skill Level (0-20)
            _skillLevel = Math.Max(0, Math.Min(20, (level - 1) * 2));

            // 변수성 조정
            _randomnessFactor = 0.3 - (level * 0.02); // 높은 레벨일수록 변수 감소

            if (_inputWriter != null)
            {
                await SendCommandAsync($"setoption name Skill Level value {_skillLevel}");

                // 낮은 난이도에서는 실수 확률 증가
                if (_eloRating < 800)
                {
                    await SendCommandAsync("setoption name Skill Level Maximum Error value 200");
                    await SendCommandAsync("setoption name Skill Level Probability value 50");
                }
            }
        }

        // 평가 기능 추가
        public async Task<EvaluationInfo> EvaluatePositionAsync(GameState gameState, Move? lastMove = null)
        {
            if (_stockfishProcess == null || _inputWriter == null || _outputReader == null)
                throw new InvalidOperationException("Engine not initialized");

            string fen = _fenAdapter.BoardToFEN(gameState);
            await SendCommandAsync($"position fen {fen}");

            // MultiPV를 사용하여 여러 후보수 평가
            await SendCommandAsync("setoption name MultiPV value 5");
            await SendCommandAsync("go depth 20"); // 더 깊은 분석

            var evalInfo = new EvaluationInfo();
            var candidateMoves = new Dictionary<string, int>();
            string? bestMove = null;
            string? line;

            while ((line = await _outputReader.ReadLineAsync()) != null)
            {
                if (line.Contains(" pv "))
                {
                    var parts = line.Split(' ');
                    int pvIndex = Array.IndexOf(parts, "pv");
                    int cpIndex = Array.IndexOf(parts, "cp");
                    int mateIndex = Array.IndexOf(parts, "mate");

                    if (pvIndex >= 0 && pvIndex + 1 < parts.Length)
                    {
                        string move = parts[pvIndex + 1];

                        if (cpIndex >= 0 && cpIndex + 1 < parts.Length)
                        {
                            int score = int.Parse(parts[cpIndex + 1]);
                            candidateMoves[move] = score;

                            if (bestMove == null)
                            {
                                bestMove = move;
                                evalInfo.CentipawnScore = score;
                            }
                        }
                        else if (mateIndex >= 0 && mateIndex + 1 < parts.Length)
                        {
                            evalInfo.MateIn = int.Parse(parts[mateIndex + 1]);
                        }
                    }
                }
                else if (line.StartsWith("bestmove"))
                {
                    break;
                }
            }

            // 마지막 수가 최선의 수였는지 확인
            if (lastMove != null && bestMove != null)
            {
                string lastMoveUCI = $"{lastMove.From.ToNotation()}{lastMove.To.ToNotation()}";
                evalInfo.WasBestMove = (lastMoveUCI == bestMove);

                // 후보수 중 순위 확인
                var sortedMoves = candidateMoves.OrderByDescending(kvp => kvp.Value).ToList();
                for (int i = 0; i < sortedMoves.Count; i++)
                {
                    if (sortedMoves[i].Key == lastMoveUCI)
                    {
                        evalInfo.MoveRank = i + 1;
                        break;
                    }
                }
            }

            return evalInfo;
        }

        private async Task SendCommandAsync(string command)
        {
            lock (_lock)
            {
                _inputWriter?.WriteLine(command);
                _inputWriter?.Flush();
            }
            await Task.Delay(10);
        }

        private async Task WaitForResponseAsync(string expectedResponse)
        {
            while (true)
            {
                string? line = await _outputReader!.ReadLineAsync();
                if (line == expectedResponse)
                    break;
            }
        }

        public void Dispose()
        {
            try
            {
                _inputWriter?.WriteLine("quit");
                _inputWriter?.Flush();
                _inputWriter?.Dispose();
                _outputReader?.Dispose();

                if (_stockfishProcess != null && !_stockfishProcess.HasExited)
                {
                    _stockfishProcess.WaitForExit(1000);
                    if (!_stockfishProcess.HasExited)
                    {
                        _stockfishProcess.Kill();
                    }
                }
                _stockfishProcess?.Dispose();
            }
            catch { }
        }

        private class MoveCandidate
        {
            public int PvNumber { get; set; }
            public int Score { get; set; }
            public string Move { get; set; } = "";
        }
    }

    public class EvaluationInfo
    {
        public int CentipawnScore { get; set; }
        public int? MateIn { get; set; }
        public bool IsWhiteAdvantage => CentipawnScore > 0;
        public bool WasBestMove { get; set; }
        public int MoveRank { get; set; } = 0;

        // 오프닝 북 (더 많은 수 추가)
        private static readonly Dictionary<string, string[]> OpeningBook = new Dictionary<string, string[]>
        {
            // 첫 수
            ["start"] = new[] { "e2e4", "d2d4", "g1f3", "c2c4", "b1c3", "f2f4", "b2b3", "g2g3" },

            // e4에 대한 응수
            ["e2e4"] = new[] { "e7e5", "c7c5", "e7e6", "c7c6", "d7d5", "g8f6", "d7d6", "g7g6" },

            // d4에 대한 응수
            ["d2d4"] = new[] { "d7d5", "g8f6", "f7f5", "e7e6", "c7c6", "g7g6", "c7c5" },

            // 1.e4 e5에 대한 계속
            ["e2e4_e7e5"] = new[] { "g1f3", "f1c4", "f2f4", "b1c3", "f1b5" },

            // 1.e4 c5에 대한 계속 (시실리안)
            ["e2e4_c7c5"] = new[] { "g1f3", "b1c3", "c2c3", "f2f4" }
        };

        public string GetAdvantageText()
        {
            if (MateIn.HasValue)
                return $"M{Math.Abs(MateIn.Value)}";

            double pawns = Math.Abs(CentipawnScore) / 100.0;
            return $"{pawns:F1}";
        }

        public MoveQuality EvaluateMoveQuality(int scoreBefore, int scoreAfter,
            Move lastMove, int moveNumber, bool isWhiteTurn, List<Move> moveHistory)
        {
            string moveNotation = $"{lastMove.From.ToNotation()}{lastMove.To.ToNotation()}";

            // 오프닝 북 체크
            if (moveNumber <= 15)
            {
                if (IsBookMove(moveNotation, moveHistory))
                {
                    return MoveQuality.Book;
                }
            }

            // 최선의 수였는지 확인
            if (WasBestMove)
            {
                return MoveQuality.Best;
            }

            // 흑색 관점에서 점수 조정
            if (!isWhiteTurn)
            {
                scoreBefore = -scoreBefore;
                scoreAfter = -scoreAfter;
            }

            // 평가치 차이 계산
            int diff = scoreAfter - scoreBefore;

            // 수의 순위도 고려
            if (MoveRank > 0 && MoveRank <= 3 && diff > -30)
            {
                // 상위 3개 후보 중 하나면 괜찮은 수
                return MoveQuality.Book;
            }

            // 평가 기준
            if (diff > 150) return MoveQuality.Brilliant;
            if (diff > 30) return MoveQuality.Good;
            if (diff >= -30) return MoveQuality.Book;
            if (diff >= -100) return MoveQuality.Dubious;
            if (diff >= -200) return MoveQuality.Mistake;
            return MoveQuality.Blunder;
        }

        private bool IsBookMove(string move, List<Move> history)
        {
            // 첫 수
            if (history.Count == 0)
            {
                return OpeningBook["start"].Contains(move);
            }

            // 이전 수들을 기반으로 체크
            if (history.Count == 1)
            {
                string firstMove = $"{history[0].From.ToNotation()}{history[0].To.ToNotation()}";
                if (OpeningBook.ContainsKey(firstMove))
                {
                    return OpeningBook[firstMove].Contains(move);
                }
            }

            // 두 수의 조합으로 체크
            if (history.Count == 2)
            {
                string sequence = $"{history[0].From.ToNotation()}{history[0].To.ToNotation()}_" +
                                 $"{history[1].From.ToNotation()}{history[1].To.ToNotation()}";
                if (OpeningBook.ContainsKey(sequence))
                {
                    return OpeningBook[sequence].Contains(move);
                }
            }

            return false;
        }
    }

    public enum MoveQuality
    {
        Brilliant,  // !! - 탁월한 수
        Good,       // ! - 좋은 수
        Best,       // * - 최고의 수
        Book,       // (이론) - 이론적인 수/표준 수
        Dubious,    // ?! - 의문스러운 수
        Mistake,    // ? - 실수
        Blunder     // ?? - 블런더
    }
}