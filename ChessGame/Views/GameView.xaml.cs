using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ChessGame.AI.Services;
using ChessGame.Core.Enums;
using ChessGame.Core.Services;
using ChessGame.Core.Events;
using ChessGame.AI.Engines;
using ChessGame.Core.Models.Pieces.Abstract;
using System.Windows.Media;
using ChessGame.Core.Models.Game;

namespace ChessGame.WPF.Views
{
    public partial class GameView : UserControl
    {
        private GameEngine _gameEngine = null!;
        private GameMode _gameMode;
        private AiDifficulty _aiDifficulty;
        private AIService? _aiService;
        private bool _isPlayerTurn = true;
        private PieceColor _playerColor = PieceColor.White;

        // 커스텀 모드 관련 필드들
        private Piece?[,]? _customBoard;
        private bool _allowCastling = true;
        private bool _allowEnPassant = true;

        public event EventHandler? BackToMenuRequested;

        private EvaluationInfo? _lastEvaluation;
        private EvaluationInfo? _previousEvaluation;
        private bool _isEvaluationEnabled = true;
        private bool _isEvaluating = false;

        // 기존 생성자 (표준 모드용)
        public GameView(GameMode gameMode, AiDifficulty aiDifficulty)
        {
            InitializeComponent();
            _gameMode = gameMode;
            _aiDifficulty = aiDifficulty;
            _customBoard = null;

            Loaded += (s, e) => InitializeGame();
        }

        // 새로 추가: 커스텀 모드용 생성자
        public GameView(GameMode gameMode, AiDifficulty aiDifficulty, Piece?[,] customBoard,
                       PieceColor firstPlayer, bool allowCastling = true, bool allowEnPassant = true)
        {
            InitializeComponent();
            _gameMode = gameMode;
            _aiDifficulty = aiDifficulty;
            _customBoard = customBoard;
            _playerColor = firstPlayer;
            _allowCastling = allowCastling;
            _allowEnPassant = allowEnPassant;

            // 커스텀 모드 UI 업데이트
            UpdateCustomModeUI();

            Loaded += (s, e) => InitializeGame();
        }

        private void UpdateCustomModeUI()
        {
            if (_gameMode == GameMode.Custom)
            {
                // 커스텀 모드 정보 표시
                if (GameStatusText != null)
                {
                    GameStatusText.Text = $"커스텀 모드 - {(_playerColor == PieceColor.White ? "백색" : "흑색")} 선공";
                }

                // 특수 규칙 정보 표시
                var ruleInfo = "특수 규칙: ";
                if (!_allowCastling) ruleInfo += "캐슬링 금지 ";
                if (!_allowEnPassant) ruleInfo += "앙파상 금지 ";
                if (_allowCastling && _allowEnPassant) ruleInfo += "모든 규칙 허용";

                // 추가 정보를 표시할 UI 요소가 있다면 업데이트
            }
        }

        private async void InitializeGame()
        {
            try
            {
                _gameEngine = new GameEngine();
                _gameEngine.GameStateChanged += OnGameStateChanged;
                _gameEngine.CheckDetected += OnCheckDetected;
                _gameEngine.GameEnded += OnGameEnded;
                _gameEngine.ErrorOccurred += OnErrorOccurred;

                // 게임 시작 방식 분기
                if (_gameMode == GameMode.Custom && _customBoard != null)
                {
                    // 커스텀 모드로 시작
                    _gameEngine.StartCustomGame(_customBoard, _playerColor, _allowCastling, _allowEnPassant);
                }
                else
                {
                    // 표준 모드로 시작
                    _gameEngine.StartNewGame(_gameMode);
                }

                ChessBoard.SetGameEngine(_gameEngine);

                // 평가 시스템 초기화
                _lastEvaluation = null;
                _previousEvaluation = null;
                _isEvaluating = false;

                // 기존 AI 서비스 정리
                if (_aiService != null)
                {
                    _aiService.Dispose();
                    _aiService = null;
                }

                // AI 서비스 초기화
                _aiService = new AIService();
                await _aiService.InitializeAsync();
                await _aiService.SetDifficultyAsync(_aiDifficulty);

                UpdateUI();

                // 초기 평가 실행
                if (_isEvaluationEnabled)
                {
                    await Task.Delay(500);
                    _ = Task.Run(async () => await UpdateEvaluation());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"게임 초기화 실패: {ex.Message}\n\n" +
                              (_gameMode == GameMode.Custom ?
                               "커스텀 보드 설정을 확인하거나 " : "") +
                              "Stockfish가 Resources/Engines 폴더에 있는지 확인하세요.",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 기존 메서드들은 그대로 유지하되, 커스텀 기물 지원을 위한 약간의 수정

        private async Task UpdateEvaluation(Move? justPlayedMove = null)
        {
            if (_isEvaluating || _aiService == null)
                return;

            _isEvaluating = true;
            try
            {
                EvaluationInfo currentEval;

                if (_gameEngine.GameState.MoveHistory.Count == 0)
                {
                    currentEval = new EvaluationInfo
                    {
                        CentipawnScore = 0,
                        Depth = 1,
                        WasBestMove = false,
                        MoveRank = 0
                    };
                }
                else
                {
                    currentEval = await _aiService.EvaluatePositionAsync(_gameEngine.GameState, justPlayedMove);
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    if (EvaluationBar != null)
                        EvaluationBar.UpdateEvaluation(currentEval);

                    UpdateCurrentEvalText(currentEval);
                });

                if (justPlayedMove != null && _lastEvaluation != null && _gameEngine.GameState.MoveHistory.Count > 0)
                {
                    await EvaluateMoveQuality(justPlayedMove, _lastEvaluation, currentEval);
                }

                _previousEvaluation = _lastEvaluation;
                _lastEvaluation = currentEval;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"평가 오류: {ex.Message}");
                await Dispatcher.InvokeAsync(() =>
                {
                    if (CurrentEvalText != null)
                        CurrentEvalText.Text = "0.0";
                });
            }
            finally
            {
                _isEvaluating = false;
            }
        }

        private async Task EvaluateMoveQuality(Move move, EvaluationInfo beforeEval, EvaluationInfo afterEval)
        {
            try
            {
                bool wasWhiteMove = _gameEngine.GameState.CurrentPlayer == PieceColor.Black;
                int displayMoveNumber = (_gameEngine.GameState.MoveHistory.Count + 1) / 2;

                int scoreBefore = wasWhiteMove ? beforeEval.CentipawnScore : -beforeEval.CentipawnScore;
                int scoreAfter = wasWhiteMove ? afterEval.CentipawnScore : -afterEval.CentipawnScore;

                if (beforeEval.MateIn.HasValue)
                {
                    scoreBefore = wasWhiteMove
                        ? (beforeEval.MateIn.Value > 0 ? 10000 : -10000)
                        : (beforeEval.MateIn.Value > 0 ? -10000 : 10000);
                }
                if (afterEval.MateIn.HasValue)
                {
                    scoreAfter = wasWhiteMove
                        ? (afterEval.MateIn.Value > 0 ? 10000 : -10000)
                        : (afterEval.MateIn.Value > 0 ? -10000 : 10000);
                }

                var quality = afterEval.EvaluateMoveQuality(
                    scoreBefore, scoreAfter, move,
                    _gameEngine.GameState.MoveHistory.Count,
                    wasWhiteMove, _gameEngine.GameState.MoveHistory
                );

                await Dispatcher.InvokeAsync(() =>
                {
                    if (MoveEvaluation != null)
                    {
                        MoveEvaluation.AddMoveEvaluation(move, quality, displayMoveNumber, wasWhiteMove);
                    }
                });

                Console.WriteLine($"수 평가: {move.ToNotation()}, " +
                                 $"이전: {scoreBefore}, 현재: {scoreAfter}, " +
                                 $"차이: {scoreAfter - scoreBefore}, 품질: {quality}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"수 품질 평가 오류: {ex.Message}");
            }
        }

        private void UpdateCurrentEvalText(EvaluationInfo eval)
        {
            try
            {
                if (CurrentEvalText == null) return;

                string evalText;

                if (eval.MateIn.HasValue)
                {
                    evalText = $"#{eval.MateIn}";
                    CurrentEvalText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                }
                else
                {
                    evalText = eval.GetAdvantageText();
                    if (eval.IsWhiteAdvantage)
                    {
                        evalText = "+" + evalText;
                        CurrentEvalText.Foreground = Brushes.Black;
                    }
                    else
                    {
                        evalText = "-" + evalText;
                        CurrentEvalText.Foreground = Brushes.DarkGray;
                    }
                }

                CurrentEvalText.Text = evalText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"평가 텍스트 업데이트 오류: {ex.Message}");
            }
        }

        private async void OnGameStateChanged(object? sender, GameEventArgs e)
        {
            try
            {
                Move? lastMove = null;
                if (_gameEngine.GameState.MoveHistory.Count > 0)
                {
                    lastMove = _gameEngine.GameState.MoveHistory[^1];
                }

                UpdateUI();
                UpdateMoveHistory();

                if (_isEvaluationEnabled && _aiService != null)
                {
                    _ = Task.Run(async () => await UpdateEvaluation(lastMove));
                }

                if (_aiService != null && _gameEngine.GameState.CurrentPlayer != _playerColor &&
                    _gameEngine.GameState.Result == GameResult.InProgress)
                {
                    _isPlayerTurn = false;
                    ChessBoard.IsEnabled = false;

                    try
                    {
                        await Task.Delay(800);
                        var aiMove = await _aiService.GetBestMoveAsync(_gameEngine.GameState);

                        if (_gameEngine.TryMakeMove(aiMove.From, aiMove.To))
                        {
                            ChessBoard.UpdateBoardDisplay();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"AI 오류: {ex.Message}", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        _isPlayerTurn = true;
                        ChessBoard.IsEnabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"게임 상태 변경 처리 오류: {ex.Message}");
            }
        }

        // 나머지 기존 메서드들은 그대로 유지...
        private void OnErrorOccurred(object? sender, string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                if (ErrorMessageText != null && ErrorMessageBorder != null)
                {
                    ErrorMessageText.Text = errorMessage;
                    ErrorMessageBorder.Visibility = Visibility.Visible;

                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, e) =>
                    {
                        if (ErrorMessageBorder != null)
                            ErrorMessageBorder.Visibility = Visibility.Collapsed;
                        timer.Stop();
                    };
                    timer.Start();
                }
            });
        }

        private void OnCheckDetected(object? sender, GameEventArgs e)
        {
            if (GameStatusText != null)
                GameStatusText.Text = "체크!";
        }

        private void OnGameEnded(object? sender, GameEventArgs e)
        {
            string message = e.GameState.Result switch
            {
                GameResult.WhiteWins => "백색 승리!",
                GameResult.BlackWins => "흑색 승리!",
                GameResult.Draw => "무승부!",
                GameResult.Stalemate => "스테일메이트!",
                _ => "게임 종료"
            };

            if (GameStatusText != null)
                GameStatusText.Text = message;

            MessageBox.Show(message, "게임 종료", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateUI()
        {
            if (CurrentPlayerText != null)
                CurrentPlayerText.Text = _gameEngine.GameState.CurrentPlayer == PieceColor.White
                    ? "백색" : "흑색";

            if (GameStatusText != null && !_gameEngine.GameState.IsCheck &&
                _gameEngine.GameState.Result == GameResult.InProgress)
            {
                GameStatusText.Text = _gameMode == GameMode.Custom ? "커스텀 게임 진행 중" : "진행 중";
            }
        }

        private void UpdateMoveHistory()
        {
            if (MoveHistoryText != null && _gameEngine.GameState.MoveHistory.Count > 0)
            {
                var lastMove = _gameEngine.GameState.MoveHistory[^1];
                string moveText;

                if (_gameEngine.GameState.CurrentPlayer == PieceColor.Black)
                {
                    moveText = $"{_gameEngine.GameState.FullMoveNumber}. {lastMove.ToNotation()} ";
                }
                else
                {
                    moveText = $"{lastMove.ToNotation()}\n";
                }

                MoveHistoryText.Text += moveText;
            }
        }

        private async void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isEvaluating = false;

                if (_aiService != null)
                {
                    _aiService.Dispose();
                    _aiService = null;
                }

                _lastEvaluation = null;
                _previousEvaluation = null;

                if (MoveEvaluation != null)
                    MoveEvaluation.Clear();
                if (MoveHistoryText != null)
                    MoveHistoryText.Text = "";

                InitializeGame();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"새 게임 시작 오류: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _aiService?.Dispose();
            BackToMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        private void EvaluationToggle_Checked(object sender, RoutedEventArgs e)
        {
            _isEvaluationEnabled = true;
            if (EvaluationPanel != null)
                EvaluationPanel.Visibility = Visibility.Visible;

            if (_aiService != null)
            {
                _ = Task.Run(async () => await UpdateEvaluation());
            }
        }

        private void EvaluationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _isEvaluationEnabled = false;
            _isEvaluating = false;
            if (EvaluationPanel != null)
                EvaluationPanel.Visibility = Visibility.Collapsed;
        }
    }
}