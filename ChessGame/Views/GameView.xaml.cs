using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ChessGame.AI.Services;
using ChessGame.Core.Enums;
using ChessGame.Core.Services;
using ChessGame.Core.Events;
using ChessGame.AI.Engines;
using System.Windows.Media;

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

        public event EventHandler? BackToMenuRequested;

        private EvaluationInfo? _lastEvaluation;
        private bool _isEvaluationEnabled = true;

        public GameView(GameMode gameMode, AiDifficulty aiDifficulty)
        {
            InitializeComponent();
            _gameMode = gameMode;
            _aiDifficulty = aiDifficulty;

            // InitializeComponent() 후에 InitializeGame 호출
            Loaded += (s, e) => InitializeGame();
        }

        private async void InitializeGame()
        {
            _gameEngine = new GameEngine();
            _gameEngine.GameStateChanged += OnGameStateChanged;
            _gameEngine.CheckDetected += OnCheckDetected;
            _gameEngine.GameEnded += OnGameEnded;
            _gameEngine.ErrorOccurred += OnErrorOccurred;

            _gameEngine.StartNewGame(_gameMode);
            ChessBoard.SetGameEngine(_gameEngine);

            try
            {
                _aiService = new AIService();
                await _aiService.InitializeAsync();
                await _aiService.SetDifficultyAsync(_aiDifficulty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"AI 초기화 실패: {ex.Message}\n\nStockfish가 Resources/Engines 폴더에 있는지 확인하세요.",
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            UpdateUI();

            // 초기 평가 실행
            if (_isEvaluationEnabled && _aiService != null)
            {
                await UpdateEvaluation();
            }
        }

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

        private async void OnGameStateChanged(object? sender, GameEventArgs e)
        {
            UpdateUI();
            UpdateMoveHistory();

            // 평가 시스템 업데이트
            if (_isEvaluationEnabled && _aiService != null)
            {
                await UpdateEvaluation();
            }

            if (_aiService != null && _gameEngine.GameState.CurrentPlayer != _playerColor &&
                _gameEngine.GameState.Result == GameResult.InProgress)
            {
                _isPlayerTurn = false;
                ChessBoard.IsEnabled = false;

                try
                {
                    await Task.Delay(500);
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

        private async Task UpdateEvaluation()
        {
            try
            {
                if (_aiService == null || _gameEngine.GameState.MoveHistory.Count == 0)
                    return;

                var lastMove = _gameEngine.GameState.MoveHistory[^1];
                var currentEval = await _aiService.EvaluatePositionAsync(_gameEngine.GameState, lastMove);

                // 평가 바 업데이트
                if (EvaluationBar != null)
                    EvaluationBar.UpdateEvaluation(currentEval);

                // 현재 평가치 텍스트 업데이트
                UpdateCurrentEvalText(currentEval);

                // 마지막 수 평가
                if (_lastEvaluation != null)
                {
                    // 방금 이동한 플레이어 확인
                    bool wasWhiteMove = _gameEngine.GameState.CurrentPlayer == PieceColor.Black;

                    var quality = currentEval.EvaluateMoveQuality(
                        _lastEvaluation.CentipawnScore,
                        currentEval.CentipawnScore,
                        lastMove,
                        _gameEngine.GameState.MoveHistory.Count,
                        wasWhiteMove,
                        _gameEngine.GameState.MoveHistory
                    );

                    // 수 번호 계산
                    int displayMoveNumber = (_gameEngine.GameState.MoveHistory.Count + 1) / 2;

                    if (MoveEvaluation != null)
                        MoveEvaluation.AddMoveEvaluation(lastMove, quality, displayMoveNumber, wasWhiteMove);
                }

                _lastEvaluation = currentEval;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"평가 오류: {ex.Message}");
            }
        }

        private void UpdateCurrentEvalText(EvaluationInfo eval)
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

        private void EvaluationToggle_Checked(object sender, RoutedEventArgs e)
        {
            _isEvaluationEnabled = true;
            if (EvaluationPanel != null)
                EvaluationPanel.Visibility = Visibility.Visible;

            // 평가 시작
            if (_aiService != null)
            {
                Task.Run(async () => await UpdateEvaluation());
            }
        }

        private void EvaluationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _isEvaluationEnabled = false;
            if (EvaluationPanel != null)
                EvaluationPanel.Visibility = Visibility.Collapsed;
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
                GameStatusText.Text = "진행 중";
            }
        }

        private void UpdateMoveHistory()
        {
            if (MoveHistoryText != null && _gameEngine.GameState.MoveHistory.Count > 0)
            {
                var lastMove = _gameEngine.GameState.MoveHistory[^1];
                string moveText;

                // 체스에서는 백의 수와 흑의 수가 하나의 번호를 공유
                // 백색 차례면 새로운 번호 시작
                if (_gameEngine.GameState.CurrentPlayer == PieceColor.Black)
                {
                    // 방금 백색이 둔 경우
                    moveText = $"{_gameEngine.GameState.FullMoveNumber}. {lastMove.ToNotation()} ";
                }
                else
                {
                    // 방금 흑색이 둔 경우 (번호 없이 수만 추가)
                    moveText = $"{lastMove.ToNotation()}\n";
                }

                MoveHistoryText.Text += moveText;
            }
        }

        private async void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            _aiService?.Dispose();
            _lastEvaluation = null;
            if (MoveEvaluation != null)
                MoveEvaluation.Clear();
            InitializeGame();
            if (MoveHistoryText != null)
                MoveHistoryText.Text = "";
        }

        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _aiService?.Dispose();
            BackToMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        private void MoveEvaluation_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}