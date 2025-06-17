using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;

namespace ChessGame.Core.Models.Pieces.Standard
{
    public class Rook : Piece
    {
        public Rook(PieceColor color) : base(color)
        {
            Type = PieceType.Rook;
            PointValue = 5;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board.ChessBoard board)
        {
            var moves = new List<Position>();

            // 직선 이동 (수직, 수평)
            AddLineMoves(moves, currentPosition, board, 1, 0);   // 위
            AddLineMoves(moves, currentPosition, board, -1, 0);  // 아래
            AddLineMoves(moves, currentPosition, board, 0, 1);   // 오른쪽
            AddLineMoves(moves, currentPosition, board, 0, -1);  // 왼쪽

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
            // 룩은 수직 또는 수평으로만 이동
            if (from.Row != to.Row && from.Column != to.Column)
                return false;

            // 경로가 비어있는지 확인
            if (!IsPathClear(from, to, board))
                return false;

            // 목표 위치 확인
            var targetPiece = board.GetPiece(to);
            return targetPiece == null || IsOpponentPiece(targetPiece);
        }

        public override string GetSymbol() => "R";

        public override string GetUnicodeSymbol()
        {
            return Color == PieceColor.White ? "♖" : "♜";
        }
    }
}