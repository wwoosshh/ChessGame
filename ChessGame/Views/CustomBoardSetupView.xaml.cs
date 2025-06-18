using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;
using ChessGame.Core.Models.Pieces.Standard;
using ChessGame.Core.Models.Pieces.Fairy;

namespace ChessGame.WPF.Views
{
    public partial class CustomBoardSetupView : UserControl
    {
        private Rectangle[,] _boardSquares = new Rectangle[8, 8];
        private TextBlock[,] _pieceDisplays = new TextBlock[8, 8];
        private Piece?[,] _customBoard = new Piece[8, 8];
        private bool _isEraserMode = false;
        private List<Piece?[,]> _undoStack = new List<Piece?[,]>();
        private List<Piece?[,]> _redoStack = new List<Piece?[,]>();

        private readonly SolidColorBrush _lightSquareBrush = new SolidColorBrush(Color.FromRgb(240, 217, 181));
        private readonly SolidColorBrush _darkSquareBrush = new SolidColorBrush(Color.FromRgb(181, 136, 99));
        private readonly SolidColorBrush _highlightBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));

        public event EventHandler<CustomGameStartEventArgs>? CustomGameStartRequested;
        public event EventHandler? BackToMenuRequested;

        public CustomBoardSetupView()
        {
            InitializeComponent();
            InitializeCustomBoard();
            InitializePiecePalettes();
            LoadStandardSetup();
        }

        private void InitializeCustomBoard()
        {
            // 체스판 칸들 생성
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var square = new Rectangle
                    {
                        Fill = (row + col) % 2 == 0 ? _lightSquareBrush : _darkSquareBrush,
                        AllowDrop = true
                    };

                    square.Drop += Square_Drop;
                    square.DragOver += Square_DragOver;
                    square.MouseRightButtonDown += Square_RightClick;

                    Grid.SetRow(square, 7 - row);
                    Grid.SetColumn(square, col);
                    CustomBoardGrid.Children.Add(square);
                    _boardSquares[row, col] = square;

                    // 기물 표시용 TextBlock
                    var pieceDisplay = new TextBlock
                    {
                        FontSize = 48,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false
                    };

                    Grid.SetRow(pieceDisplay, 7 - row);
                    Grid.SetColumn(pieceDisplay, col);
                    CustomBoardGrid.Children.Add(pieceDisplay);
                    _pieceDisplays[row, col] = pieceDisplay;
                }
            }

            AddCoordinates();
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

        private void InitializePiecePalettes()
        {
            // 표준 기물들
            var standardPieces = new[]
            {
                PieceType.King, PieceType.Queen, PieceType.Rook,
                PieceType.Bishop, PieceType.Knight, PieceType.Pawn
            };

            foreach (var pieceType in standardPieces)
            {
                AddPieceToPalette(WhitePiecePalette, pieceType, PieceColor.White);
                AddPieceToPalette(BlackPiecePalette, pieceType, PieceColor.Black);
            }

            // 페어리 체스 기물들
            AddPieceToPalette(ArchbishopPalette, PieceType.Archbishop, PieceColor.White);
            AddPieceToPalette(ArchbishopPalette, PieceType.Archbishop, PieceColor.Black);

            AddPieceToPalette(ChancellorPalette, PieceType.Chancellor, PieceColor.White);
            AddPieceToPalette(ChancellorPalette, PieceType.Chancellor, PieceColor.Black);

            AddPieceToPalette(AmazonPalette, PieceType.Amazon, PieceColor.White);
            AddPieceToPalette(AmazonPalette, PieceType.Amazon, PieceColor.Black);

            var otherPieces = new[] { PieceType.Ferz, PieceType.Wazir, PieceType.Camel };
            foreach (var pieceType in otherPieces)
            {
                AddPieceToPalette(OtherPiecePalette, pieceType, PieceColor.White);
                AddPieceToPalette(OtherPiecePalette, pieceType, PieceColor.Black);
            }
        }

        private void AddPieceToPalette(Panel palette, PieceType pieceType, PieceColor color)
        {
            var piece = CreatePiece(pieceType, color);
            if (piece == null) return;

            var pieceButton = new Border
            {
                Width = 50,
                Height = 50,
                Margin = new Thickness(2),
                Background = Brushes.LightGray,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Cursor = Cursors.Hand
            };

            var pieceText = new TextBlock
            {
                Text = piece.GetUnicodeSymbol(),
                FontSize = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = color == PieceColor.White ? Brushes.White : Brushes.Black
            };

            pieceButton.Child = pieceText;
            pieceButton.MouseLeftButtonDown += (s, e) => StartPieceDrag(piece, e);
            pieceButton.ToolTip = $"{color} {pieceType}";

            palette.Children.Add(pieceButton);
        }

        private Piece? CreatePiece(PieceType pieceType, PieceColor color)
        {
            return pieceType switch
            {
                PieceType.King => new King(color),
                PieceType.Queen => new Queen(color),
                PieceType.Rook => new Rook(color),
                PieceType.Bishop => new Bishop(color),
                PieceType.Knight => new Knight(color),
                PieceType.Pawn => new Pawn(color),
                PieceType.Archbishop => new Archbishop(color),
                PieceType.Chancellor => new Chancellor(color),
                PieceType.Amazon => new Amazon(color),
                PieceType.Ferz => new Ferz(color),
                PieceType.Wazir => new Wazir(color),
                PieceType.Camel => new Camel(color),
                _ => null
            };
        }

        private void StartPieceDrag(Piece piece, MouseButtonEventArgs e)
        {
            if (_isEraserMode) return;

            var data = new DataObject("Piece", piece);
            DragDrop.DoDragDrop((DependencyObject)e.Source, data, DragDropEffects.Copy);
        }

        private void CustomBoard_Drop(object sender, DragEventArgs e)
        {
            var point = e.GetPosition(CustomBoardGrid);
            var (row, col) = GetBoardPosition(point);

            if (row >= 0 && row < 8 && col >= 0 && col < 8)
            {
                if (e.Data.GetDataPresent("Piece"))
                {
                    var piece = (Piece)e.Data.GetData("Piece");
                    PlacePiece(row, col, piece);
                }
            }
        }

        private void CustomBoard_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Piece"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Square_Drop(object sender, DragEventArgs e)
        {
            var square = (Rectangle)sender;
            var row = 7 - Grid.GetRow(square);
            var col = Grid.GetColumn(square);

            if (e.Data.GetDataPresent("Piece"))
            {
                var piece = (Piece)e.Data.GetData("Piece");
                PlacePiece(row, col, piece);
            }
        }

        private void Square_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("Piece"))
            {
                e.Effects = DragDropEffects.Copy;
                var square = (Rectangle)sender;
                square.Fill = _highlightBrush;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Square_RightClick(object sender, MouseButtonEventArgs e)
        {
            var square = (Rectangle)sender;
            var row = 7 - Grid.GetRow(square);
            var col = Grid.GetColumn(square);

            // 우클릭으로 기물 제거
            PlacePiece(row, col, null);
        }

        private (int row, int col) GetBoardPosition(Point point)
        {
            int col = (int)(point.X / (CustomBoardGrid.ActualWidth / 8));
            int row = 7 - (int)(point.Y / (CustomBoardGrid.ActualHeight / 8));
            return (row, col);
        }

        private void PlacePiece(int row, int col, Piece? piece)
        {
            SaveStateForUndo();

            _customBoard[row, col] = piece;
            var pieceDisplay = _pieceDisplays[row, col];

            if (piece != null)
            {
                pieceDisplay.Text = piece.GetUnicodeSymbol();
                pieceDisplay.Foreground = piece.Color == PieceColor.White ? Brushes.White : Brushes.Black;
            }
            else
            {
                pieceDisplay.Text = "";
            }

            // 하이라이트 제거
            _boardSquares[row, col].Fill = (row + col) % 2 == 0 ? _lightSquareBrush : _darkSquareBrush;

            ValidateBoard();
        }

        private void SaveStateForUndo()
        {
            var state = new Piece[8, 8];
            Array.Copy(_customBoard, state, 64);
            _undoStack.Add(state);

            if (_undoStack.Count > 50) // 최대 50단계까지
            {
                _undoStack.RemoveAt(0);
            }

            _redoStack.Clear();
        }

        private void LoadStandardSetup_Click(object sender, RoutedEventArgs e)
        {
            LoadStandardSetup();
        }

        private void LoadStandardSetup()
        {
            SaveStateForUndo();
            ClearBoardInternal();

            // 표준 체스 배치
            var setup = new[]
            {
                // 1행 (백색)
                new[] { PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
                        PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook },
                // 2행 (백색 폰)
                new[] { PieceType.Pawn, PieceType.Pawn, PieceType.Pawn, PieceType.Pawn,
                        PieceType.Pawn, PieceType.Pawn, PieceType.Pawn, PieceType.Pawn }
            };

            for (int col = 0; col < 8; col++)
            {
                _customBoard[0, col] = CreatePiece(setup[0][col], PieceColor.White);
                _customBoard[1, col] = CreatePiece(setup[1][col], PieceColor.White);
                _customBoard[6, col] = CreatePiece(setup[1][col], PieceColor.Black);
                _customBoard[7, col] = CreatePiece(setup[0][col], PieceColor.Black);
            }

            UpdateBoardDisplay();
        }

        private void LoadAmazonChess_Click(object sender, RoutedEventArgs e)
        {
            SaveStateForUndo();
            ClearBoardInternal();

            // Amazon Chess 변형 - 퀸 대신 Amazon
            for (int col = 0; col < 8; col++)
            {
                if (col == 3) // 퀸 위치에 Amazon
                {
                    _customBoard[0, col] = CreatePiece(PieceType.Amazon, PieceColor.White);
                    _customBoard[7, col] = CreatePiece(PieceType.Amazon, PieceColor.Black);
                }
                else
                {
                    var pieceType = col switch
                    {
                        0 or 7 => PieceType.Rook,
                        1 or 6 => PieceType.Knight,
                        2 or 5 => PieceType.Bishop,
                        4 => PieceType.King,
                        _ => PieceType.Pawn
                    };

                    _customBoard[0, col] = CreatePiece(pieceType, PieceColor.White);
                    _customBoard[7, col] = CreatePiece(pieceType, PieceColor.Black);
                }

                // 폰들
                _customBoard[1, col] = CreatePiece(PieceType.Pawn, PieceColor.White);
                _customBoard[6, col] = CreatePiece(PieceType.Pawn, PieceColor.Black);
            }

            UpdateBoardDisplay();
        }

        private void ClearBoard_Click(object sender, RoutedEventArgs e)
        {
            SaveStateForUndo();
            ClearBoardInternal();
            UpdateBoardDisplay();
        }

        private void ClearBoardInternal()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    _customBoard[row, col] = null;
                }
            }
        }

        private void UpdateBoardDisplay()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = _customBoard[row, col];
                    var pieceDisplay = _pieceDisplays[row, col];

                    if (piece != null)
                    {
                        pieceDisplay.Text = piece.GetUnicodeSymbol();
                        pieceDisplay.Foreground = piece.Color == PieceColor.White ? Brushes.White : Brushes.Black;
                    }
                    else
                    {
                        pieceDisplay.Text = "";
                    }
                }
            }

            ValidateBoard();
        }

        private void ToggleEraser_Click(object sender, RoutedEventArgs e)
        {
            _isEraserMode = !_isEraserMode;
            EraserButton.Content = _isEraserMode ? "✏️ 배치 모드" : "🗑️ 지우개 모드";
            Cursor = _isEraserMode ? Cursors.No : Cursors.Arrow;
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                var currentState = new Piece[8, 8];
                Array.Copy(_customBoard, currentState, 64);
                _redoStack.Add(currentState);

                var previousState = _undoStack[_undoStack.Count - 1];
                _undoStack.RemoveAt(_undoStack.Count - 1);

                Array.Copy(previousState, _customBoard, 64);
                UpdateBoardDisplay();
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count > 0)
            {
                var currentState = new Piece[8, 8];
                Array.Copy(_customBoard, currentState, 64);
                _undoStack.Add(currentState);

                var nextState = _redoStack[_redoStack.Count - 1];
                _redoStack.RemoveAt(_redoStack.Count - 1);

                Array.Copy(nextState, _customBoard, 64);
                UpdateBoardDisplay();
            }
        }

        private void ValidateBoard_Click(object sender, RoutedEventArgs e)
        {
            ValidateBoard();
        }

        private void ValidateBoard()
        {
            var whiteKings = 0;
            var blackKings = 0;
            var whitePieces = 0;
            var blackPieces = 0;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = _customBoard[row, col];
                    if (piece != null)
                    {
                        if (piece.Color == PieceColor.White)
                        {
                            whitePieces++;
                            if (piece.Type == PieceType.King) whiteKings++;
                        }
                        else
                        {
                            blackPieces++;
                            if (piece.Type == PieceType.King) blackKings++;
                        }
                    }
                }
            }

            var messages = new List<string>();

            if (whiteKings != 1) messages.Add($"백색 킹이 {whiteKings}개 (1개 필요)");
            if (blackKings != 1) messages.Add($"흑색 킹이 {blackKings}개 (1개 필요)");
            if (whitePieces == 0) messages.Add("백색 기물이 없음");
            if (blackPieces == 0) messages.Add("흑색 기물이 없음");

            bool isValid = messages.Count == 0;
            StartGameButton.IsEnabled = isValid;

            ValidationText.Text = isValid ? "✅ 보드가 유효합니다!" : "❌ " + string.Join(", ", messages);
            ValidationText.Foreground = isValid ? Brushes.Green : Brushes.Red;
        }

        private void StartCustomGame_Click(object sender, RoutedEventArgs e)
        {
            if (!StartGameButton.IsEnabled) return;

            var gameArgs = new CustomGameStartEventArgs
            {
                CustomBoard = (Piece[,])_customBoard.Clone(),
                FirstPlayer = FirstPlayerCombo.SelectedIndex == 0 ? PieceColor.White : PieceColor.Black,
                AiDifficulty = (AiDifficulty)DifficultyCombo.SelectedIndex,
                AllowCastling = AllowCastlingCheckBox.IsChecked ?? true,
                AllowEnPassant = AllowEnPassantCheckBox.IsChecked ?? true
            };

            CustomGameStartRequested?.Invoke(this, gameArgs);
        }

        private void BackToMenu_Click(object sender, RoutedEventArgs e)
        {
            BackToMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        // 추가 프리셋들
        private void LoadKOTHSetup_Click(object sender, RoutedEventArgs e) { /* King of the Hill 구현 */ }
        private void LoadChess960Setup_Click(object sender, RoutedEventArgs e) { /* Fischer Random 구현 */ }
        private void FlipBoard_Click(object sender, RoutedEventArgs e) { /* 보드 뒤집기 구현 */ }
    }

    public class CustomGameStartEventArgs : EventArgs
    {
        public Piece[,] CustomBoard { get; set; } = new Piece[8, 8];
        public PieceColor FirstPlayer { get; set; } = PieceColor.White;
        public AiDifficulty AiDifficulty { get; set; } = AiDifficulty.Medium;
        public bool AllowCastling { get; set; } = true;
        public bool AllowEnPassant { get; set; } = true;
    }
}