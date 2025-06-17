using ChessGame.Core.Enums;
using ChessGame.Core.Models.Pieces.Abstract;

namespace ChessGame.Core.Models.Board
{
    public class Square
    {
        public Position Position { get; set; }
        public Piece? Piece { get; set; }

        public Square(Position position)
        {
            Position = position;
        }

        public bool IsEmpty => Piece == null;

        public bool IsOccupiedBy(PieceColor color)
        {
            return Piece != null && Piece.Color == color;
        }
    }
}