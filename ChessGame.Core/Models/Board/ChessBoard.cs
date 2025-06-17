using System.Net.NetworkInformation;
using ChessGame.Core.Enums;
using ChessGame.Core.Models.Pieces.Abstract;
using ChessGame.Core.Models.Pieces.Standard;

namespace ChessGame.Core.Models.Board
{
    public class ChessBoard
    {
        private Square[,] _squares;
        public const int Size = 8;

        public ChessBoard()
        {
            _squares = new Square[Size, Size];
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            // 모든 칸 초기화
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    _squares[row, col] = new Square(new Position(row, col));
                }
            }
        }

        public void SetupStandardPosition()
        {
            // 백색 기물 배치
            SetPiece(new Position(0, 0), new Rook(PieceColor.White));
            SetPiece(new Position(0, 1), new Knight(PieceColor.White));
            SetPiece(new Position(0, 2), new Bishop(PieceColor.White));
            SetPiece(new Position(0, 3), new Queen(PieceColor.White));
            SetPiece(new Position(0, 4), new King(PieceColor.White));
            SetPiece(new Position(0, 5), new Bishop(PieceColor.White));
            SetPiece(new Position(0, 6), new Knight(PieceColor.White));
            SetPiece(new Position(0, 7), new Rook(PieceColor.White));

            // 백색 폰
            for (int col = 0; col < Size; col++)
            {
                SetPiece(new Position(1, col), new Pawn(PieceColor.White));
            }

            // 흑색 기물 배치
            SetPiece(new Position(7, 0), new Rook(PieceColor.Black));
            SetPiece(new Position(7, 1), new Knight(PieceColor.Black));
            SetPiece(new Position(7, 2), new Bishop(PieceColor.Black));
            SetPiece(new Position(7, 3), new Queen(PieceColor.Black));
            SetPiece(new Position(7, 4), new King(PieceColor.Black));
            SetPiece(new Position(7, 5), new Bishop(PieceColor.Black));
            SetPiece(new Position(7, 6), new Knight(PieceColor.Black));
            SetPiece(new Position(7, 7), new Rook(PieceColor.Black));

            // 흑색 폰
            for (int col = 0; col < Size; col++)
            {
                SetPiece(new Position(6, col), new Pawn(PieceColor.Black));
            }
        }

        public Square GetSquare(Position position)
        {
            if (!position.IsValid())
                throw new ArgumentException("Invalid position");

            return _squares[position.Row, position.Column];
        }

        public Piece? GetPiece(Position position)
        {
            return GetSquare(position).Piece;
        }

        public void SetPiece(Position position, Piece? piece)
        {
            GetSquare(position).Piece = piece;
        }

        public bool IsEmpty(Position position)
        {
            return GetSquare(position).IsEmpty;
        }

        public void MovePiece(Position from, Position to)
        {
            var piece = GetPiece(from);
            if (piece == null)
                throw new InvalidOperationException("No piece at source position");

            SetPiece(to, piece);
            SetPiece(from, null);
            piece.HasMoved = true;
        }

        public ChessBoard Clone()
        {
            var clone = new ChessBoard();
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var position = new Position(row, col);
                    var piece = GetPiece(position);
                    if (piece != null)
                    {
                        Piece clonedPiece = piece.Type switch
                        {
                            PieceType.King => new King(piece.Color),
                            PieceType.Queen => new Queen(piece.Color),
                            PieceType.Rook => new Rook(piece.Color),
                            PieceType.Bishop => new Bishop(piece.Color),
                            PieceType.Knight => new Knight(piece.Color),
                            PieceType.Pawn => new Pawn(piece.Color),
                            _ => throw new InvalidOperationException($"Unknown piece type: {piece.Type}")
                        };

                        clonedPiece.HasMoved = piece.HasMoved;
                        clone.SetPiece(position, clonedPiece);
                    }
                }
            }
            return clone;
        }

        public List<Piece> GetPieces(PieceColor color)
        {
            var pieces = new List<Piece>();
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var piece = _squares[row, col].Piece;
                    if (piece != null && piece.Color == color)
                        pieces.Add(piece);
                }
            }
            return pieces;
        }

        public Position? FindKing(PieceColor color)
        {
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    var piece = _squares[row, col].Piece;
                    if (piece != null && piece.Type == PieceType.King && piece.Color == color)
                        return new Position(row, col);
                }
            }
            return null;
        }
    }
}