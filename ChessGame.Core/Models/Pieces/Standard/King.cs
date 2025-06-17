using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;
using System;
using System.Collections.Generic;

namespace ChessGame.Core.Models.Pieces.Standard
{
    public class King : Piece
    {
        public King(PieceColor color) : base(color)
        {
            Type = PieceType.King;
            PointValue = 0;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board)
        {
            var moves = new List<Position>();

            // 기본 이동 (모든 방향 1칸)
            int[] rowOffsets = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] colOffsets = { -1, 0, 1, -1, 1, -1, 0, 1 };

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

            // 캐슬링 체크
            if (!HasMoved)
            {
                // 킹사이드 캐슬링
                var kingSideRookPos = new Position(currentPosition.Row, 7);
                if (CanCastle(currentPosition, kingSideRookPos, board, true))
                {
                    moves.Add(new Position(currentPosition.Row, 6));
                }

                // 퀸사이드 캐슬링
                var queenSideRookPos = new Position(currentPosition.Row, 0);
                if (CanCastle(currentPosition, queenSideRookPos, board, false))
                {
                    moves.Add(new Position(currentPosition.Row, 2));
                }
            }

            return moves;
        }

        private bool CanCastle(Position kingPos, Position rookPos, ChessBoard board, bool isKingSide)
        {
            var rook = board.GetPiece(rookPos);

            // 룩이 있고 움직이지 않았는지 확인
            if (rook == null || rook.Type != PieceType.Rook ||
                rook.Color != Color || rook.HasMoved)
                return false;

            // 킹과 룩 사이의 경로가 비어있는지 확인
            int startCol = isKingSide ? 5 : 1;
            int endCol = isKingSide ? 6 : 3;

            for (int col = startCol; col <= endCol; col++)
            {
                if (!board.IsEmpty(new Position(kingPos.Row, col)))
                    return false;
            }

            // 킹이 지나가는 경로가 공격받지 않는지 확인해야 함
            // (MoveValidator에서 처리)

            return true;
        }

        public override bool CanMoveTo(Position from, Position to, ChessBoard board)
        {
            int rowDiff = Math.Abs(to.Row - from.Row);
            int colDiff = Math.Abs(to.Column - from.Column);

            // 일반 이동 (1칸)
            if (rowDiff <= 1 && colDiff <= 1)
            {
                var targetPiece = board.GetPiece(to);
                return targetPiece == null || IsOpponentPiece(targetPiece);
            }

            // 캐슬링 (같은 행에서 2칸 이동)
            if (!HasMoved && rowDiff == 0 && colDiff == 2)
            {
                bool isKingSide = to.Column > from.Column;
                var rookPos = new Position(from.Row, isKingSide ? 7 : 0);
                return CanCastle(from, rookPos, board, isKingSide);
            }

            return false;
        }

        public override string GetSymbol() => "K";

        public override string GetUnicodeSymbol()
        {
            return Color == PieceColor.White ? "♔" : "♚";
        }
    }
}