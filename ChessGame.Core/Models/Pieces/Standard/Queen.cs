using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;

namespace ChessGame.Core.Models.Pieces.Standard
{
    public class Queen : Piece
    {
        public Queen(PieceColor color) : base(color)
        {
            Type = PieceType.Queen;
            PointValue = 9;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, Board.ChessBoard board)
        {
            var moves = new List<Position>();

            // 퀸은 룩과 비숍의 움직임을 합친 것
            // 직선 이동 (룩처럼)
            AddLineMoves(moves, currentPosition, board, 1, 0);   // 위
            AddLineMoves(moves, currentPosition, board, -1, 0);  // 아래
            AddLineMoves(moves, currentPosition, board, 0, 1);   // 오른쪽
            AddLineMoves(moves, currentPosition, board, 0, -1);  // 왼쪽

            // 대각선 이동 (비숍처럼)
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

            // 직선 또는 대각선 이동만 가능
            bool isStraight = from.Row == to.Row || from.Column == to.Column;
            bool isDiagonal = rowDiff == colDiff;

            if (!isStraight && !isDiagonal)
                return false;

            // 경로가 비어있는지 확인
            if (!IsPathClear(from, to, board))
                return false;

            // 목표 위치에 같은 색 기물이 있으면 안됨
            var targetPiece = board.GetPiece(to);
            return targetPiece == null || IsOpponentPiece(targetPiece);
        }

        public override string GetSymbol() => "Q";

        public override string GetUnicodeSymbol()
        {
            return Color == PieceColor.White ? "♕" : "♛";
        }
    }
}