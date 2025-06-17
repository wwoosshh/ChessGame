using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;

namespace ChessGame.Core.Models.Pieces.Standard
{
    public class Knight : Piece
    {
        public Knight(PieceColor color) : base(color)
        {
            Type = PieceType.Knight;
            PointValue = 3;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board.ChessBoard board)
        {
            var moves = new List<Position>();

            // 나이트의 L자 이동 패턴
            int[] rowOffsets = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] colOffsets = { -1, 1, -2, 2, -2, 2, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                var newPos = new Position(
                    currentPosition.Row + rowOffsets[i],
                    currentPosition.Column + colOffsets[i]
                );

                if (newPos.IsValid())
                {
                    var targetPiece = board.GetPiece(newPos);
                    if (targetPiece == null || IsOpponentPiece(targetPiece))
                    {
                        moves.Add(newPos);
                    }
                }
            }

            return moves;
        }

        public override bool CanMoveTo(Position from, Position to, Board.ChessBoard board)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);

            // L자 모양 이동 확인
            bool isLShape = (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2);

            if (!isLShape)
                return false;

            // 나이트는 다른 기물을 뛰어넘을 수 있으므로 경로 확인 불필요
            var targetPiece = board.GetPiece(to);
            return targetPiece == null || IsOpponentPiece(targetPiece);
        }

        public override string GetSymbol() => "N";

        public override string GetUnicodeSymbol()
        {
            return Color == PieceColor.White ? "♘" : "♞";
        }
    }
}