using System;
using System.Collections.Generic;
using System.Threading.Tasks;  // Task 사용을 위해 추가
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;  // Dispatcher 사용을 위해 추가
using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Game;  // Move 클래스를 위해 추가
using ChessGame.Core.Services;
using ChessGame.WPF.Helpers;

namespace ChessGame.WPF.Controls
{
    public partial class ChessBoardControl : UserControl
    {
        private Rectangle[,] _squares = new Rectangle[8, 8];
        private TextBlock[,] _pieceDisplays = new TextBlock[8, 8];
        private GameEngine _gameEngine;
        private Position? _selectedPosition;
        private List<Position> _highlightedMoves = new List<Position>();
        private Position? _hoveredPosition;
        private Rectangle? _hoverHighlight;

        private readonly SolidColorBrush _lightSquareBrush = new SolidColorBrush(Color.FromRgb(240, 217, 181));
        private readonly SolidColorBrush _darkSquareBrush = new SolidColorBrush(Color.FromRgb(181, 136, 99));
        private readonly SolidColorBrush _selectedSquareBrush = new SolidColorBrush(Color.FromRgb(255, 255, 100));
        private readonly SolidColorBrush _highlightBrush = new SolidColorBrush(Color.FromArgb(100, 124, 252, 0));
        private readonly SolidColorBrush _checkBrush = new SolidColorBrush(Color.FromArgb(150, 255, 0, 0));
        private readonly SolidColorBrush _lastMoveBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 0));

        private bool _isProcessingMove = false;
        private Move? _lastMove;

        public ChessBoardControl()
        {
            InitializeComponent();
            InitializeBoard();
            AddCoordinates();
        }

        private void InitializeBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // 체스판 칸
                    var square = new Rectangle
                    {
                        Fill = (row + col) % 2 == 0 ? _lightSquareBrush : _darkSquareBrush
                    };

                    Grid.SetRow(square, 7 - row);
                    Grid.SetColumn(square, col);
                    BoardGrid.Children.Add(square);
                    _squares[row, col] = square;

                    // 기물 표시
                    var pieceDisplay = new TextBlock
                    {
                        FontSize = 52,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false,
                        Effect = new DropShadowEffect
                        {
                            Color = Colors.Black,
                            BlurRadius = 3,
                            ShadowDepth = 2,
                            Opacity = 0.5
                        }
                    };

                    Grid.SetRow(pieceDisplay, 7 - row);
                    Grid.SetColumn(pieceDisplay, col);
                    BoardGrid.Children.Add(pieceDisplay);
                    _pieceDisplays[row, col] = pieceDisplay;
                }
            }
        }

        private void AddCoordinates()
        {
            var fontSize = 14;
            var margin = 5;

            // 파일 (a-h)
            for (int col = 0; col < 8; col++)
            {
                var text = new TextBlock
                {
                    Text = ((char)('a' + col)).ToString(),
                    FontSize = fontSize,
                    Foreground = Brushes.Gray,
                    FontWeight = FontWeights.Bold
                };

                Canvas.SetLeft(text, col * 75 + 35);
                Canvas.SetBottom(text, margin);
                CoordinatesCanvas.Children.Add(text);
            }

            // 랭크 (1-8)
            for (int row = 0; row < 8; row++)
            {
                var text = new TextBlock
                {
                    Text = (row + 1).ToString(),
                    FontSize = fontSize,
                    Foreground = Brushes.Gray,
                    FontWeight = FontWeights.Bold
                };

                Canvas.SetLeft(text, margin);
                Canvas.SetTop(text, (7 - row) * 75 + 30);
                CoordinatesCanvas.Children.Add(text);
            }
        }

        public void SetGameEngine(GameEngine gameEngine)
        {
            _gameEngine = gameEngine;
            _gameEngine.GameStateChanged += OnGameStateChanged;
            UpdateBoardDisplay();
        }

        private void OnGameStateChanged(object? sender, Core.Events.GameEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 체크 상태 표시
                if (e.GameState.IsCheck)
                {
                    ShowCheckWarning(e.GameState.CurrentPlayer);
                }
            });
        }

        private void ShowCheckWarning(PieceColor kingColor)
        {
            var kingPos = _gameEngine.GameState.Board.FindKing(kingColor);
            if (kingPos != null)
            {
                var square = _squares[kingPos.Row, kingPos.Column];

                // 체크 애니메이션
                var storyboard = (Storyboard)FindResource("CheckWarningAnimation");
                square.Fill = _checkBrush;
                storyboard.Begin(square);
            }
        }

        public void UpdateBoardDisplay()
        {
            if (_gameEngine == null) return;

            var board = _gameEngine.GameState.Board;

            // 마지막 이동 표시
            if (_lastMove != null)
            {
                HighlightLastMove(_lastMove);
            }

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    var piece = board.GetPiece(position);
                    var pieceDisplay = _pieceDisplays[row, col];

                    if (piece != null)
                    {
                        pieceDisplay.Text = piece.GetUnicodeSymbol();
                        pieceDisplay.Foreground = piece.Color == PieceColor.White
                            ? Brushes.White : Brushes.Black;

                        // 기물 등장 애니메이션
                        if (pieceDisplay.Opacity == 0)
                        {
                            AnimationHelper.FadeIn(pieceDisplay, new Duration(TimeSpan.FromMilliseconds(300)));
                        }
                    }
                    else
                    {
                        pieceDisplay.Text = "";
                        pieceDisplay.Opacity = 1;
                    }
                }
            }
        }

        private void HighlightLastMove(Move move)
        {
            // 이전 하이라이트 제거
            foreach (var square in _squares)
            {
                var row = Grid.GetRow(square);
                var col = Grid.GetColumn(square);
                var originalRow = 7 - row;
                var originalCol = col;

                if ((originalRow + originalCol) % 2 == 0)
                    square.Fill = _lightSquareBrush;
                else
                    square.Fill = _darkSquareBrush;
            }

            // 새 하이라이트 추가
            _squares[move.From.Row, move.From.Column].Fill = _lastMoveBrush;
            _squares[move.To.Row, move.To.Column].Fill = _lastMoveBrush;
        }

        private void BoardGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_gameEngine == null) return;

            var point = e.GetPosition(BoardGrid);
            int col = (int)(point.X / (BoardGrid.ActualWidth / 8));
            int row = 7 - (int)(point.Y / (BoardGrid.ActualHeight / 8));

            if (col < 0 || col >= 8 || row < 0 || row >= 8) return;

            var position = new Position(row, col);

            if (_hoveredPosition == null || !_hoveredPosition.Equals(position))
            {
                _hoveredPosition = position;
                UpdateHoverEffect(position);
            }
        }

        private void UpdateHoverEffect(Position position)
        {
            // 이전 호버 효과 제거
            if (_hoverHighlight != null)
            {
                BoardGrid.Children.Remove(_hoverHighlight);
                _hoverHighlight = null;
            }

            // 새 호버 효과 추가
            var piece = _gameEngine.GameState.Board.GetPiece(position);
            if (piece != null && piece.Color == _gameEngine.GameState.CurrentPlayer)
            {
                _hoverHighlight = new Rectangle
                {
                    Fill = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                    IsHitTestVisible = false
                };

                Grid.SetRow(_hoverHighlight, 7 - position.Row);
                Grid.SetColumn(_hoverHighlight, position.Column);
                BoardGrid.Children.Add(_hoverHighlight);

                // 커서 변경
                Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
        }

        private void BoardGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_hoverHighlight != null)
            {
                BoardGrid.Children.Remove(_hoverHighlight);
                _hoverHighlight = null;
            }
            _hoveredPosition = null;
            Cursor = Cursors.Arrow;
        }

        private void BoardGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_gameEngine == null || _isProcessingMove) return;

            var point = e.GetPosition(BoardGrid);
            int col = (int)(point.X / (BoardGrid.ActualWidth / 8));
            int row = 7 - (int)(point.Y / (BoardGrid.ActualHeight / 8));

            if (col < 0 || col >= 8 || row < 0 || row >= 8) return;

            var clickedPosition = new Position(row, col);

            if (_selectedPosition == null)
            {
                var piece = _gameEngine.GameState.Board.GetPiece(clickedPosition);
                if (piece != null && piece.Color == _gameEngine.GameState.CurrentPlayer)
                {
                    SelectPiece(clickedPosition);
                }
            }
            else
            {
                if (_highlightedMoves.Contains(clickedPosition))
                {
                    AnimateAndMove(_selectedPosition, clickedPosition);
                }
                else
                {
                    ClearSelection();
                }
            }
        }

        private void SelectPiece(Position position)
        {
            _selectedPosition = position;

            // 선택 애니메이션
            var square = _squares[position.Row, position.Column];
            AnimationHelper.PulseAnimation(square);
            square.Fill = _selectedSquareBrush;

            // 가능한 이동 표시
            _highlightedMoves = _gameEngine.GetLegalMoves(position);
            foreach (var move in _highlightedMoves)
            {
                var moveIndicator = new Ellipse
                {
                    Width = 30,
                    Height = 30,
                    Fill = _highlightBrush,
                    IsHitTestVisible = false,
                    Opacity = 0
                };

                Grid.SetRow(moveIndicator, 7 - move.Row);
                Grid.SetColumn(moveIndicator, move.Column);
                BoardGrid.Children.Add(moveIndicator);

                // 페이드인 애니메이션
                AnimationHelper.FadeIn(moveIndicator, new Duration(TimeSpan.FromMilliseconds(200)));
            }
        }

        private void AnimateAndMove(Position from, Position to)
        {
            _isProcessingMove = true;

            var fromPieceDisplay = _pieceDisplays[from.Row, from.Column];
            var pieceText = fromPieceDisplay.Text;

            // 애니메이션용 임시 TextBlock 생성
            var animatedPiece = new TextBlock
            {
                Text = pieceText,
                FontSize = 52,
                Foreground = fromPieceDisplay.Foreground,
                Effect = fromPieceDisplay.Effect
            };

            // 시작 위치 계산
            var fromPoint = new Point(from.Column * 75 + 37.5, (7 - from.Row) * 75 + 37.5);
            var toPoint = new Point(to.Column * 75 + 37.5, (7 - to.Row) * 75 + 37.5);

            Canvas.SetLeft(animatedPiece, fromPoint.X - 26);
            Canvas.SetTop(animatedPiece, fromPoint.Y - 26);
            AnimationCanvas.Children.Add(animatedPiece);

            // 원본 숨기기
            fromPieceDisplay.Opacity = 0;

            // 이동 애니메이션
            var moveOffset = new Point(toPoint.X - fromPoint.X, toPoint.Y - fromPoint.Y);
            AnimationHelper.AnimateMove(animatedPiece, new Point(0, 0), moveOffset,
                new Duration(TimeSpan.FromMilliseconds(400)), () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // 실제 이동 수행
                        if (_gameEngine.TryMakeMove(from, to))
                        {
                            _lastMove = new Move(from, to);
                            UpdateBoardDisplay();
                        }

                        // 애니메이션 정리 - UIElement로 캐스팅
                        AnimationCanvas.Children.Remove(animatedPiece as UIElement);
                        fromPieceDisplay.Opacity = 1;
                        ClearSelection();
                        _isProcessingMove = false;
                    });
                });
        }

        private void ClearSelection()
        {
            if (_selectedPosition != null)
            {
                var row = _selectedPosition.Row;
                var col = _selectedPosition.Column;
                _squares[row, col].Fill = (row + col) % 2 == 0 ? _lightSquareBrush : _darkSquareBrush;
            }

            _selectedPosition = null;

            // 이동 가능 표시 제거 - UIElement 타입으로 명시적 캐스팅
            var overlays = new List<UIElement>();
            foreach (UIElement child in BoardGrid.Children)  // var 대신 UIElement로 명시
            {
                if (child is Rectangle rect && rect.Fill == _highlightBrush)
                {
                    overlays.Add(child);
                }
                else if (child is Ellipse)  // Ellipse도 처리
                {
                    overlays.Add(child);
                }
            }

            foreach (var overlay in overlays)
            {
                BoardGrid.Children.Remove(overlay);
            }

            _highlightedMoves.Clear();
        }
    }
}