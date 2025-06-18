using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChessGame.AI.Adapters;
using ChessGame.Core.Models.Game;
using ChessGame.Core.Enums;

namespace ChessGame.AI.Engines
{
    // 공용 클래스들을 네임스페이스 레벨로 이동
    public class CandidateMove
    {
        public string Move { get; set; } = "";
        public int Score { get; set; }
        public int Rank { get; set; }
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

    public class StockfishEngine : IChessEngine, IDisposable
    {
        private Process? _stockfishProcess;
        private StreamWriter? _inputWriter;
        private StreamReader? _outputReader;
        private readonly FENAdapter _fenAdapter;
        private readonly UCIAdapter _uciAdapter;
        private readonly SemaphoreSlim _engineSemaphore;
        private int _skillLevel = 10;
        private int _eloRating = 1000;
        private readonly Random _random = new Random();
        private bool _disposed = false;

        private int _multipv = 3;
        private double _randomnessFactor = 0.2;

        public StockfishEngine()
        {
            _fenAdapter = new FENAdapter();
            _uciAdapter = new UCIAdapter();
            _engineSemaphore = new SemaphoreSlim(1, 1);
        }

        public async Task InitializeAsync()
        {
            await _engineSemaphore.WaitAsync();
            try
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

                await SendCommandUnsafeAsync("uci");
                await WaitForResponseAsync("uciok");

                await SendCommandUnsafeAsync($"setoption name Skill Level value {_skillLevel}");
                await SendCommandUnsafeAsync($"setoption name MultiPV value {_multipv}");
                await SendCommandUnsafeAsync("setoption name Threads value 1");
                await SendCommandUnsafeAsync("setoption name Hash value 128");
                await SendCommandUnsafeAsync("isready");
                await WaitForResponseAsync("readyok");
            }
            finally
            {
                _engineSemaphore.Release();
            }
        }

        public async Task<Move> GetBestMoveAsync(GameState gameState, int depth = 10)
        {
            if (_disposed || _stockfishProcess == null || _inputWriter == null || _outputReader == null)
                throw new InvalidOperationException("Engine not initialized or disposed");

            await _engineSemaphore.WaitAsync();
            try
            {
                string fen = _fenAdapter.BoardToFEN(gameState);
                await SendCommandUnsafeAsync($"position fen {fen}");

                int adjustedDepth = AdjustDepthByDifficulty(depth);
                await SendCommandUnsafeAsync($"go depth {adjustedDepth}");

                var moveCandidates = new List<MoveCandidate>();
                string? line;

                while ((line = await _outputReader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("info depth"))
                    {
                        var candidate = ParseInfoLine(line);
                        if (candidate != null)
                        {
                            moveCandidates.RemoveAll(c => c.PvNumber == candidate.PvNumber);
                            moveCandidates.Add(candidate);
                        }
                    }
                    else if (line.StartsWith("bestmove"))
                    {
                        break;
                    }
                }

                return SelectMoveFromCandidatesImproved(moveCandidates, gameState);
            }
            finally
            {
                _engineSemaphore.Release();
            }
        }

        public async Task<EvaluationInfo> EvaluatePositionAsync(GameState gameState, Move? lastMove = null)
        {
            if (_disposed || _stockfishProcess == null || _inputWriter == null || _outputReader == null)
                throw new InvalidOperationException("Engine not initialized or disposed");

            await _engineSemaphore.WaitAsync();
            try
            {
                string fen = _fenAdapter.BoardToFEN(gameState);
                await SendCommandUnsafeAsync($"position fen {fen}");
                await SendCommandUnsafeAsync("setoption name MultiPV value 5");
                await SendCommandUnsafeAsync("go depth 15");

                var evalInfo = new EvaluationInfo();
                var candidateMoves = new List<CandidateMove>();
                string? bestMove = null;
                string? line;
                int currentDepth = 0;

                while ((line = await _outputReader.ReadLineAsync()) != null)
                {
                    if (line.Contains("info depth"))
                    {
                        var depthMatch = System.Text.RegularExpressions.Regex.Match(line, @"depth (\d+)");
                        if (depthMatch.Success)
                        {
                            currentDepth = int.Parse(depthMatch.Groups[1].Value);
                        }

                        var parts = line.Split(' ');
                        int pvIndex = Array.IndexOf(parts, "multipv");
                        int cpIndex = Array.IndexOf(parts, "cp");
                        int mateIndex = Array.IndexOf(parts, "mate");
                        int pvMoveIndex = Array.IndexOf(parts, "pv");

                        if (pvIndex >= 0 && pvMoveIndex >= 0 && pvIndex + 1 < parts.Length && pvMoveIndex + 1 < parts.Length)
                        {
                            string move = parts[pvMoveIndex + 1];
                            int pvNumber = int.Parse(parts[pvIndex + 1]);

                            var candidate = new CandidateMove
                            {
                                Move = move,
                                Rank = pvNumber
                            };

                            if (cpIndex >= 0 && cpIndex + 1 < parts.Length)
                            {
                                candidate.Score = int.Parse(parts[cpIndex + 1]);

                                if (pvNumber == 1 && currentDepth >= evalInfo.Depth)
                                {
                                    evalInfo.CentipawnScore = candidate.Score;
                                    evalInfo.Depth = currentDepth;
                                    bestMove = move;
                                }
                            }
                            else if (mateIndex >= 0 && mateIndex + 1 < parts.Length && pvNumber == 1)
                            {
                                evalInfo.MateIn = int.Parse(parts[mateIndex + 1]);
                                bestMove = move;
                            }

                            if (currentDepth >= 12)
                            {
                                candidateMoves.RemoveAll(c => c.Rank == pvNumber);
                                candidateMoves.Add(candidate);
                            }
                        }
                    }
                    else if (line.StartsWith("bestmove"))
                    {
                        break;
                    }
                }

                evalInfo.TopMoves = candidateMoves.OrderBy(c => c.Rank).Take(5).ToList();

                if (lastMove != null && bestMove != null)
                {
                    string lastMoveUCI = $"{lastMove.From.ToNotation()}{lastMove.To.ToNotation()}";

                    if (lastMove.IsPromotion && lastMove.PromotionPiece.HasValue)
                    {
                        lastMoveUCI += lastMove.PromotionPiece.Value switch
                        {
                            Core.Enums.PieceType.Queen => "q",
                            Core.Enums.PieceType.Rook => "r",
                            Core.Enums.PieceType.Bishop => "b",
                            Core.Enums.PieceType.Knight => "n",
                            _ => "q"
                        };
                    }

                    evalInfo.WasBestMove = (lastMoveUCI == bestMove);

                    var matchingCandidate = evalInfo.TopMoves.FirstOrDefault(c => c.Move == lastMoveUCI);
                    if (matchingCandidate != null)
                    {
                        evalInfo.MoveRank = matchingCandidate.Rank;
                    }
                    else
                    {
                        evalInfo.MoveRank = evalInfo.TopMoves.Count + 1;
                    }
                }

                return evalInfo;
            }
            finally
            {
                _engineSemaphore.Release();
            }
        }

        private async Task SendCommandUnsafeAsync(string command)
        {
            if (_inputWriter != null && !_disposed)
            {
                await _inputWriter.WriteLineAsync(command);
                await _inputWriter.FlushAsync();
                await Task.Delay(10);
            }
        }

        private async Task SendCommandAsync(string command)
        {
            await _engineSemaphore.WaitAsync();
            try
            {
                await SendCommandUnsafeAsync(command);
            }
            finally
            {
                _engineSemaphore.Release();
            }
        }

        private async Task WaitForResponseAsync(string expectedResponse)
        {
            while (!_disposed && _outputReader != null)
            {
                string? line = await _outputReader.ReadLineAsync();
                if (line == expectedResponse)
                    break;
                if (line == null)
                    break;
            }
        }

        public async Task SetDifficultyAsync(int level)
        {
            _eloRating = level switch
            {
                1 => 400,
                2 => 600,
                3 => 800,
                4 => 1000,
                5 => 1200,
                6 => 1400,
                7 => 1600,
                8 => 1800,
                9 => 2000,
                10 => 2200,
                _ => 1000
            };

            _skillLevel = Math.Max(0, Math.Min(20, (level - 1) * 2));
            _randomnessFactor = 0.3 - (level * 0.02);

            if (!_disposed && _inputWriter != null)
            {
                await SendCommandAsync($"setoption name Skill Level value {_skillLevel}");

                if (_eloRating < 800)
                {
                    await SendCommandAsync("setoption name Skill Level Maximum Error value 200");
                    await SendCommandAsync("setoption name Skill Level Probability value 50");
                }
            }
        }

        private Move SelectMoveFromCandidatesImproved(List<MoveCandidate> candidates, GameState gameState)
        {
            if (!candidates.Any())
                throw new Exception("No move candidates found");

            var bestCandidates = candidates
                .GroupBy(c => c.PvNumber)
                .Select(g => g.OrderByDescending(c => c.Score).First())
                .OrderByDescending(c => c.Score)
                .ToList();

            if (!bestCandidates.Any())
                throw new Exception("No valid candidates found");

            MoveCandidate selectedCandidate;

            if (_eloRating >= 1800)
            {
                selectedCandidate = bestCandidates[0];
                if (_random.NextDouble() < 0.05 && bestCandidates.Count > 1)
                {
                    var topTwo = bestCandidates.Take(2).ToList();
                    if (topTwo[0].Score - topTwo[1].Score < 30)
                    {
                        selectedCandidate = topTwo[1];
                    }
                }
            }
            else if (_eloRating >= 1200)
            {
                var topCandidates = bestCandidates.Take(3).ToList();
                double randomChance = 0.15;
                if (_random.NextDouble() < randomChance && topCandidates.Count > 1)
                {
                    var weights = CalculateCandidateWeights(topCandidates);
                    selectedCandidate = SelectWeightedCandidate(topCandidates, weights);
                }
                else
                {
                    selectedCandidate = bestCandidates[0];
                }
            }
            else
            {
                double randomChance = 0.3;
                if (_random.NextDouble() < randomChance)
                {
                    var randomCandidates = bestCandidates.Take(5).ToList();
                    selectedCandidate = randomCandidates[_random.Next(randomCandidates.Count)];
                }
                else
                {
                    selectedCandidate = bestCandidates[0];
                }

                if (_eloRating < 800 && _random.NextDouble() < 0.08)
                {
                    var blunderCandidates = bestCandidates.Where(c => c.Score < bestCandidates[0].Score - 200).ToList();
                    if (blunderCandidates.Any())
                    {
                        selectedCandidate = blunderCandidates[_random.Next(blunderCandidates.Count)];
                    }
                }
            }

            return _uciAdapter.UCIToMove(selectedCandidate.Move);
        }

        private MoveCandidate SelectWeightedCandidate(List<MoveCandidate> candidates, List<double> weights)
        {
            double totalWeight = weights.Sum();
            double random = _random.NextDouble() * totalWeight;
            double currentWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                currentWeight += weights[i];
                if (random <= currentWeight)
                {
                    return candidates[i];
                }
            }
            return candidates[0];
        }

        private List<double> CalculateCandidateWeights(List<MoveCandidate> candidates)
        {
            var weights = new List<double>();
            int bestScore = candidates[0].Score;

            foreach (var candidate in candidates)
            {
                int scoreDiff = bestScore - candidate.Score;
                double weight = Math.Exp(-scoreDiff / 100.0);
                weights.Add(weight);
            }
            return weights;
        }

        private MoveCandidate? ParseInfoLine(string line)
        {
            try
            {
                var parts = line.Split(' ');
                int pvIndex = Array.IndexOf(parts, "multipv");
                int cpIndex = Array.IndexOf(parts, "cp");
                int pvMoveIndex = Array.IndexOf(parts, "pv");

                if (pvIndex >= 0 && cpIndex >= 0 && pvMoveIndex >= 0 &&
                    pvIndex + 1 < parts.Length && cpIndex + 1 < parts.Length && pvMoveIndex + 1 < parts.Length)
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

        private int AdjustDepthByDifficulty(int baseDepth)
        {
            if (_eloRating < 500) return Math.Max(1, baseDepth - 7);
            else if (_eloRating < 800) return Math.Max(2, baseDepth - 5);
            else if (_eloRating < 1200) return Math.Max(3, baseDepth - 3);
            else if (_eloRating < 1500) return Math.Max(5, baseDepth - 1);
            else return baseDepth;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _engineSemaphore.Wait(1000);

                if (_inputWriter != null && !_stockfishProcess?.HasExited == true)
                {
                    _inputWriter.WriteLine("quit");
                    _inputWriter.Flush();
                }

                _inputWriter?.Dispose();
                _outputReader?.Dispose();

                if (_stockfishProcess != null && !_stockfishProcess.HasExited)
                {
                    _stockfishProcess.WaitForExit(2000);
                    if (!_stockfishProcess.HasExited)
                    {
                        _stockfishProcess.Kill();
                    }
                }
                _stockfishProcess?.Dispose();
            }
            catch { }
            finally
            {
                _engineSemaphore?.Release();
                _engineSemaphore?.Dispose();
            }
        }

        // StockfishEngine 내부에서만 사용하는 클래스
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
        public List<CandidateMove> TopMoves { get; set; } = new List<CandidateMove>();
        public int Depth { get; set; }

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
            // 게임 시작 시 첫 수들은 항상 Book으로 처리
            if (moveHistory.Count <= 2)
            {
                string moveNotation = $"{lastMove.From.ToNotation()}{lastMove.To.ToNotation()}";
                if (IsClassicOpeningMove(moveNotation, moveHistory.Count))
                {
                    return MoveQuality.Book;
                }
            }

            // 흑색 관점에서 점수 조정
            if (!isWhiteTurn)
            {
                scoreBefore = -scoreBefore;
                scoreAfter = -scoreAfter;
            }

            int diff = scoreAfter - scoreBefore;

            // 오프닝 단계에서는 매우 관대한 평가
            if (moveNumber <= 10)
            {
                string moveNotation = $"{lastMove.From.ToNotation()}{lastMove.To.ToNotation()}";
                if (IsBookMove(moveNotation, moveHistory))
                {
                    return MoveQuality.Book;
                }

                // 오프닝에서는 -50 이상이면 괜찮은 수로 간주
                if (diff >= -50)
                {
                    if (WasBestMove) return MoveQuality.Best;
                    if (MoveRank <= 3) return MoveQuality.Good;
                    return MoveQuality.Book;
                }
            }

            // 기물 가치 분석
            var materialAnalysis = AnalyzeMaterialChange(lastMove, scoreBefore, scoreAfter);

            // 포지션 상황별 분석
            var positionType = AnalyzePositionType(scoreBefore, moveNumber);

            // 수의 품질 결정
            return DetermineMoveQuality(diff, materialAnalysis, positionType, WasBestMove, MoveRank, moveNumber);
        }

        private bool IsClassicOpeningMove(string move, int moveCount)
        {
            if (moveCount == 1)
            {
                return new[] { "e2e4", "d2d4", "g1f3", "c2c4", "b1c3", "f2f4" }.Contains(move);
            }
            else if (moveCount == 2)
            {
                return new[] { "e7e5", "e7e6", "c7c5", "c7c6", "d7d5", "d7d6", "g8f6", "b8c6" }.Contains(move);
            }
            return false;
        }

        private MaterialAnalysis AnalyzeMaterialChange(Move move, int scoreBefore, int scoreAfter)
        {
            var analysis = new MaterialAnalysis();

            if (move.CapturedPiece != null)
            {
                analysis.MaterialGained = GetPieceValue(move.CapturedPiece.Type);
            }

            if (move.MovedPiece != null)
            {
                analysis.PieceAtRisk = GetPieceValue(move.MovedPiece.Type);
            }

            int expectedChange = analysis.MaterialGained;
            int actualChange = scoreAfter - scoreBefore;

            analysis.UnexpectedLoss = Math.Max(0, expectedChange - actualChange);
            analysis.UnexpectedGain = Math.Max(0, actualChange - expectedChange);

            return analysis;
        }

        private PositionType AnalyzePositionType(int score, int moveNumber)
        {
            int absScore = Math.Abs(score);

            if (moveNumber <= 12)
                return PositionType.Opening;
            else if (moveNumber >= 40)
                return PositionType.Endgame;
            else if (absScore > 300)
                return PositionType.Decisive;
            else if (absScore > 100)
                return PositionType.Advantage;
            else
                return PositionType.Equal;
        }

        private MoveQuality DetermineMoveQuality(int diff, MaterialAnalysis material,
            PositionType positionType, bool wasBestMove, int moveRank, int moveNumber)
        {
            if (wasBestMove)
            {
                if (material.UnexpectedLoss >= 300 && diff > 50)
                    return MoveQuality.Brilliant;
                else
                    return MoveQuality.Best;
            }

            if (moveRank > 0 && moveRank <= 3)
            {
                if (positionType == PositionType.Opening)
                {
                    if (diff >= -40) return MoveQuality.Good;
                    if (diff >= -80) return MoveQuality.Book;
                }
                else
                {
                    if (diff >= -30) return MoveQuality.Good;
                    if (diff >= -60) return MoveQuality.Book;
                }
            }

            if (material.UnexpectedLoss >= 300)
            {
                if (diff > 100)
                    return MoveQuality.Brilliant;
                else if (diff < -200)
                    return MoveQuality.Blunder;
            }

            var thresholds = GetQualityThresholds(positionType);

            if (diff >= thresholds.Brilliant)
                return MoveQuality.Brilliant;
            else if (diff >= thresholds.Good)
                return MoveQuality.Good;
            else if (diff >= thresholds.Book)
                return MoveQuality.Book;
            else if (diff >= thresholds.Dubious)
                return MoveQuality.Dubious;
            else if (diff >= thresholds.Mistake)
                return MoveQuality.Mistake;
            else
                return MoveQuality.Blunder;
        }

        private QualityThresholds GetQualityThresholds(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Opening => new QualityThresholds
                {
                    Brilliant = 100,
                    Good = 20,
                    Book = -40,
                    Dubious = -80,
                    Mistake = -150
                },
                PositionType.Endgame => new QualityThresholds
                {
                    Brilliant = 80,
                    Good = 25,
                    Book = -15,
                    Dubious = -40,
                    Mistake = -80
                },
                PositionType.Decisive => new QualityThresholds
                {
                    Brilliant = 150,
                    Good = 50,
                    Book = -20,
                    Dubious = -70,
                    Mistake = -150
                },
                _ => new QualityThresholds
                {
                    Brilliant = 90,
                    Good = 25,
                    Book = -30,
                    Dubious = -60,
                    Mistake = -120
                }
            };
        }

        private int GetPieceValue(PieceType pieceType)
        {
            return pieceType switch
            {
                PieceType.Pawn => 100,
                PieceType.Knight => 300,
                PieceType.Bishop => 300,
                PieceType.Rook => 500,
                PieceType.Queen => 900,
                PieceType.King => 0,
                _ => 0
            };
        }

        private bool IsBookMove(string move, List<Move> history)
        {
            if (history.Count == 0)
            {
                return OpeningBook["start"].Contains(move);
            }

            if (history.Count == 1)
            {
                string firstMove = $"{history[0].From.ToNotation()}{history[0].To.ToNotation()}";
                if (OpeningBook.ContainsKey(firstMove))
                {
                    return OpeningBook[firstMove].Contains(move);
                }
            }

            if (history.Count == 2)
            {
                string sequence = $"{history[0].From.ToNotation()}{history[0].To.ToNotation()}_" +
                                 $"{history[1].From.ToNotation()}{history[1].To.ToNotation()}";
                if (OpeningBook.ContainsKey(sequence))
                {
                    return OpeningBook[sequence].Contains(move);
                }
            }

            if (history.Count <= 8)
            {
                return IsCommonDevelopmentMove(move, history);
            }

            return false;
        }

        private bool IsCommonDevelopmentMove(string move, List<Move> history)
        {
            var developmentMoves = new[]
            {
                "g1f3", "b1c3", "g8f6", "b8c6",
                "f1c4", "f1e2", "f1b5", "c1f4", "c1g5",
                "f8c5", "f8e7", "c8f5", "c8g4",
                "e1g1", "e1c1", "e8g8", "e8c8",
                "d2d3", "e2e3", "d7d6", "e7e6",
                "d1e2", "d1f3", "d8e7", "d8f6"
            };

            return developmentMoves.Contains(move);
        }

        private class MaterialAnalysis
        {
            public int MaterialGained { get; set; }
            public int PieceAtRisk { get; set; }
            public int UnexpectedLoss { get; set; }
            public int UnexpectedGain { get; set; }
        }

        private enum PositionType
        {
            Opening,
            Equal,
            Advantage,
            Decisive,
            Endgame
        }

        private class QualityThresholds
        {
            public int Brilliant { get; set; }
            public int Good { get; set; }
            public int Book { get; set; }
            public int Dubious { get; set; }
            public int Mistake { get; set; }
        }

        private static readonly Dictionary<string, string[]> OpeningBook = new Dictionary<string, string[]>
        {
            ["start"] = new[] { "e2e4", "d2d4", "g1f3", "c2c4", "b1c3", "f2f4", "b2b3", "g2g3" },
            ["e2e4"] = new[] { "e7e5", "c7c5", "e7e6", "c7c6", "d7d5", "g8f6", "d7d6", "g7g6", "b8c6" },
            ["d2d4"] = new[] { "d7d5", "g8f6", "f7f5", "e7e6", "c7c6", "g7g6", "c7c5", "b8c6" },
            ["g1f3"] = new[] { "d7d5", "g8f6", "c7c5", "e7e6", "b8c6", "g7g6" },
            ["e2e4_e7e5"] = new[] { "g1f3", "f1c4", "f2f4", "b1c3", "f1b5", "d2d3", "f1e2" },
            ["e2e4_c7c5"] = new[] { "g1f3", "b1c3", "c2c3", "f2f4", "f1c4" },
            ["e2e4_e7e6"] = new[] { "d2d4", "g1f3", "b1c3", "f1c4" },
            ["d2d4_d7d5"] = new[] { "c2c4", "g1f3", "b1c3", "e2e3", "f1f4" },
            ["d2d4_g8f6"] = new[] { "c2c4", "g1f3", "b1c3", "f1g5", "e2e3" }
        };
    }
}