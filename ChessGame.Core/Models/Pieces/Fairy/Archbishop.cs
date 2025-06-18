using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;

namespace ChessGame.Core.Models.Pieces.Fairy
{
    // Archbishop: 비숍 + 나이트 조합
    public class Archbishop : Piece
    {
        public Archbishop(PieceColor color) : base(color)
        {
            Type = PieceType.Archbishop;
            PointValue = 8; // 비숍(3) + 나이트(3) + 보너스
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board)
        {
            var moves = new List<Position>();

            // 비숍의 대각선 이동
            AddLineMoves(moves, currentPosition, board, 1, 1);
            AddLineMoves(moves, currentPosition, board, 1, -1);
            AddLineMoves(moves, currentPosition, board, -1, 1);
            AddLineMoves(moves, currentPosition, board, -1, -1);

            // 나이트의 L자 이동
            AddKnightMoves(moves, currentPosition, board);

            return moves;
        }

        private void AddLineMoves(List<Position> moves, Position start, ChessBoard board, int rowDir, int colDir)
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

        private void AddKnightMoves(List<Position> moves, Position currentPosition, ChessBoard board)
        {
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
        }

        public override bool CanMoveTo(Position from, Position to, ChessBoard board)
        {
            // 비숍 이동 체크
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);

            if (rowDiff == colDiff && rowDiff > 0) // 대각선
            {
                if (IsPathClear(from, to, board))
                {
                    var targetPiece = board.GetPiece(to);
                    return targetPiece == null || IsOpponentPiece(targetPiece);
                }
            }

            // 나이트 이동 체크
            bool isLShape = (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2);
            if (isLShape)
            {
                var targetPiece = board.GetPiece(to);
                return targetPiece == null || IsOpponentPiece(targetPiece);
            }

            return false;
        }

        public override string GetSymbol() => "A";
        public override string GetUnicodeSymbol() => Color == PieceColor.White ? "♘♗" : "♞♝";
    }

    // Chancellor: 룩 + 나이트 조합
    public class Chancellor : Piece
    {
        public Chancellor(PieceColor color) : base(color)
        {
            Type = PieceType.Chancellor;
            PointValue = 8; // 룩(5) + 나이트(3)
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board)
        {
            var moves = new List<Position>();

            // 룩의 직선 이동
            AddLineMoves(moves, currentPosition, board, 1, 0);   // 위
            AddLineMoves(moves, currentPosition, board, -1, 0);  // 아래
            AddLineMoves(moves, currentPosition, board, 0, 1);   // 오른쪽
            AddLineMoves(moves, currentPosition, board, 0, -1);  // 왼쪽

            // 나이트의 L자 이동
            AddKnightMoves(moves, currentPosition, board);

            return moves;
        }

        private void AddLineMoves(List<Position> moves, Position start, ChessBoard board, int rowDir, int colDir)
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

        private void AddKnightMoves(List<Position> moves, Position currentPosition, ChessBoard board)
        {
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
        }

        public override bool CanMoveTo(Position from, Position to, ChessBoard board)
        {
            // 룩 이동 체크
            if (from.Row == to.Row || from.Column == to.Column)
            {
                if (IsPathClear(from, to, board))
                {
                    var targetPiece = board.GetPiece(to);
                    return targetPiece == null || IsOpponentPiece(targetPiece);
                }
            }

            // 나이트 이동 체크
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);
            bool isLShape = (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2);

            if (isLShape)
            {
                var targetPiece = board.GetPiece(to);
                return targetPiece == null || IsOpponentPiece(targetPiece);
            }

            return false;
        }

        public override string GetSymbol() => "C";
        public override string GetUnicodeSymbol() => Color == PieceColor.White ? "♖♘" : "♜♞";
    }

    // Amazon: 퀸 + 나이트 조합 (가장 강력한 기물)
    public class Amazon : Piece
    {
        public Amazon(PieceColor color) : base(color)
        {
            Type = PieceType.Amazon;
            PointValue = 12; // 퀸(9) + 나이트(3)
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board)
        {
            var moves = new List<Position>();

            // 퀸의 모든 방향 이동 (직선 + 대각선)
            for (int rowDir = -1; rowDir <= 1; rowDir++)
            {
                for (int colDir = -1; colDir <= 1; colDir++)
                {
                    if (rowDir == 0 && colDir == 0) continue;
                    AddLineMoves(moves, currentPosition, board, rowDir, colDir);
                }
            }

            // 나이트의 L자 이동
            AddKnightMoves(moves, currentPosition, board);

            return moves;
        }

        private void AddLineMoves(List<Position> moves, Position start, ChessBoard board, int rowDir, int colDir)
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

        private void AddKnightMoves(List<Position> moves, Position currentPosition, ChessBoard board)
        {
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
        }

        public override bool CanMoveTo(Position from, Position to, ChessBoard board)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);

            // 퀸 이동 체크 (직선 또는 대각선)
            bool isStraight = from.Row == to.Row || from.Column == to.Column;
            bool isDiagonal = rowDiff == colDiff;

            if (isStraight || isDiagonal)
            {
                if (IsPathClear(from, to, board))
                {
                    var targetPiece = board.GetPiece(to);
                    return targetPiece == null || IsOpponentPiece(targetPiece);
                }
            }

            // 나이트 이동 체크
            bool isLShape = (rowDiff == 2 && colDiff == 1) || (rowDiff == 1 && colDiff == 2);
            if (isLShape)
            {
                var targetPiece = board.GetPiece(to);
                return targetPiece == null || IsOpponentPiece(targetPiece);
            }

            return false;
        }

        public override string GetSymbol() => "Z";
        public override string GetUnicodeSymbol() => Color == PieceColor.White ? "♕♘" : "♛♞";
    }

    // Ferz: 대각선 1칸만 이동
    public class Ferz : Piece
    {
        public Ferz(PieceColor color) : base(color)
        {
            Type = PieceType.Ferz;
            PointValue = 2;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board)
        {
            var moves = new List<Position>();

            // 대각선 1칸씩
            int[] rowOffsets = { -1, -1, 1, 1 };
            int[] colOffsets = { -1, 1, -1, 1 };

            for (int i = 0; i < 4; i++)
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

        public override bool CanMoveTo(Position from, Position to, ChessBoard board)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);

            if (rowDiff == 1 && colDiff == 1)
            {
                var targetPiece = board.GetPiece(to);
                return targetPiece == null || IsOpponentPiece(targetPiece);
            }

            return false;
        }

        public override string GetSymbol() => "F";
        public override string GetUnicodeSymbol() => Color == PieceColor.White ? "♦" : "♢";
    }

    // Wazir: 직선 1칸만 이동 (상하좌우)
    public class Wazir : Piece
    {
        public Wazir(PieceColor color) : base(color)
        {
            Type = PieceType.Wazir;
            PointValue = 2;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board)
        {
            var moves = new List<Position>();

            // 직선 1칸씩 (상하좌우)
            int[] rowOffsets = { -1, 1, 0, 0 };
            int[] colOffsets = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
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

        public override bool CanMoveTo(Position from, Position to, ChessBoard board)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);

            if ((rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1))
            {
                var targetPiece = board.GetPiece(to);
                return targetPiece == null || IsOpponentPiece(targetPiece);
            }

            return false;
        }

        public override string GetSymbol() => "W";
        public override string GetUnicodeSymbol() => Color == PieceColor.White ? "♛" : "◇";
    }

    // Camel: (3,1) 점프
    public class Camel : Piece
    {
        public Camel(PieceColor color) : base(color)
        {
            Type = PieceType.Camel;
            PointValue = 2;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board)
        {
            var moves = new List<Position>();

            // (3,1) 패턴의 8가지 이동
            int[] rowOffsets = { -3, -3, -1, -1, 1, 1, 3, 3 };
            int[] colOffsets = { -1, 1, -3, 3, -3, 3, -1, 1 };

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

        public override bool CanMoveTo(Position from, Position to, ChessBoard board)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);

            bool isCamelMove = (rowDiff == 3 && colDiff == 1) || (rowDiff == 1 && colDiff == 3);

            if (isCamelMove)
            {
                var targetPiece = board.GetPiece(to);
                return targetPiece == null || IsOpponentPiece(targetPiece);
            }

            return false;
        }

        public override string GetSymbol() => "M";
        public override string GetUnicodeSymbol() => Color == PieceColor.White ? "🐪" : "🐫";
    }
}