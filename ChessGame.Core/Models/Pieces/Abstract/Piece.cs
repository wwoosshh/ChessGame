using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Game;
using System;
using System.Collections.Generic;

namespace ChessGame.Core.Models.Pieces.Abstract
{
    public abstract class Piece
    {
        public PieceType Type { get; protected set; }
        public PieceColor Color { get; set; }
        public int PointValue { get; protected set; }
        public bool HasMoved { get; set; }

        protected Piece(PieceColor color)
        {
            Color = color;
            HasMoved = false;
        }

        public abstract List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board);
        public abstract bool CanMoveTo(Position from, Position to, ChessBoard board);
        public abstract string GetSymbol();
        public abstract string GetUnicodeSymbol();

        protected bool IsSameColor(Piece? other)
        {
            return other != null && other.Color == Color;
        }

        protected bool IsOpponentPiece(Piece? other)
        {
            return other != null && other.Color != Color;
        }

        protected bool IsPathClear(Position from, Position to, ChessBoard board)
        {
            int rowStep = Math.Sign(to.Row - from.Row);
            int colStep = Math.Sign(to.Column - from.Column);

            int currentRow = from.Row + rowStep;
            int currentCol = from.Column + colStep;

            while (currentRow != to.Row || currentCol != to.Column)
            {
                var pos = new Position(currentRow, currentCol);
                if (!board.IsEmpty(pos))
                    return false;

                currentRow += rowStep;
                currentCol += colStep;
            }

            return true;
        }
    }
}