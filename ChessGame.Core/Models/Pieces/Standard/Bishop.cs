using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;

namespace ChessGame.Core.Models.Pieces.Standard
{
    public class Bishop : Piece
    {
        public Bishop(PieceColor color) : base(color)
        {
            Type = PieceType.Bishop;
            PointValue = 3;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board.ChessBoard board)
        {
            var moves = new List<Position>();

            // 대각선 이동
            AddLineMoves(moves, currentPosition, board, 1, 1);   // 오른쪽 위
            AddLineMoves(moves, currentPosition, board, 1, -1);  // 왼쪽 위
            AddLineMoves(moves, currentPosition, board, -1, 1);  // 오른쪽 아래
            AddLineMoves(moves, currentPosition, board, -1, -1); // 왼쪽 아래

            return moves;
        }

        private void AddLineMoves(List<Position> moves, Position start, Board.ChessBoard board,
            int rowDir, int colDir)
        {
            int row = start.Row + rowDir;
            int col = start.Column + colDir;

            while (row >= 0 && row < 8 && col >= 0 && col < 8)
            {
                var pos = new Position(row, col);
                var piece = board.GetPiece(pos);

                if (piece == null)
                {
                    moves.Add(pos);
                }
                else
                {
                    if (IsOpponentPiece(piece))
                        moves.Add(pos);
                    break;
                }

                row += rowDir;
                col += colDir;
            }
        }

        public override bool CanMoveTo(Position from, Position to, Board.ChessBoard board)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);

            // 비숍은 대각선으로만 이동
            if (rowDiff != colDiff)
                return false;

            // 경로가 비어있는지 확인
            if (!IsPathClear(from, to, board))
                return false;

            // 목표 위치 확인
            var targetPiece = board.GetPiece(to);
            return targetPiece == null || IsOpponentPiece(targetPiece);
        }

        public override string GetSymbol() => "B";

        public override string GetUnicodeSymbol()
        {
            return Color == PieceColor.White ? "♗" : "♝";
        }
    }
}