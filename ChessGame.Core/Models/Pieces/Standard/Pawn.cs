using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;
using ChessGame.Core.Models.Game;
using System;
using System.Collections.Generic;

namespace ChessGame.Core.Models.Pieces.Standard
{
    public class Pawn : Piece
    {
        public Pawn(PieceColor color) : base(color)
        {
            Type = PieceType.Pawn;
            PointValue = 1;
        }

        public override List<Position> GetPossibleMoves(Position currentPosition, ChessBoard board)
        {
            var moves = new List<Position>();
            int direction = Color == PieceColor.White ? 1 : -1;

            // 전진 이동
            var oneStep = new Position(currentPosition.Row + direction, currentPosition.Column);
            if (oneStep.IsValid() && board.IsEmpty(oneStep))
            {
                moves.Add(oneStep);

                // 첫 이동시 2칸 전진
                if (!HasMoved)
                {
                    var twoStep = new Position(currentPosition.Row + (2 * direction), currentPosition.Column);
                    if (board.IsEmpty(twoStep))
                    {
                        moves.Add(twoStep);
                    }
                }
            }

            // 대각선 캡처
            for (int colOffset = -1; colOffset <= 1; colOffset += 2)
            {
                var capturePos = new Position(
                    currentPosition.Row + direction,
                    currentPosition.Column + colOffset
                );

                if (capturePos.IsValid())
                {
                    var targetPiece = board.GetPiece(capturePos);
                    if (targetPiece != null && IsOpponentPiece(targetPiece))
                    {
                        moves.Add(capturePos);
                    }
                    // 앙파상 체크는 GameState의 EnPassantTarget을 확인해야 함
                    // 여기서는 가능한 위치만 추가
                    else if (CanCaptureEnPassant(currentPosition, capturePos, board))
                    {
                        moves.Add(capturePos);
                    }
                }
            }

            return moves;
        }

        private bool CanCaptureEnPassant(Position from, Position to, ChessBoard board)
        {
            // 앙파상이 가능한 행인지 확인
            int enPassantRow = Color == PieceColor.White ? 4 : 3;
            if (from.Row != enPassantRow)
                return false;

            // 옆에 있는 폰 확인
            var adjacentPos = new Position(from.Row, to.Column);
            var adjacentPiece = board.GetPiece(adjacentPos);

            return adjacentPiece != null &&
                   adjacentPiece.Type == PieceType.Pawn &&
                   IsOpponentPiece(adjacentPiece);
        }

        public override bool CanMoveTo(Position from, Position to, ChessBoard board)
        {
            int direction = Color == PieceColor.White ? 1 : -1;
            int rowDiff = to.Row - from.Row;
            int colDiff = Math.Abs(to.Column - from.Column);

            // 전진 이동
            if (colDiff == 0)
            {
                if (rowDiff == direction && board.IsEmpty(to))
                    return true;

                // 첫 이동시 2칸
                if (!HasMoved && rowDiff == 2 * direction)
                {
                    var middlePos = new Position(from.Row + direction, from.Column);
                    return board.IsEmpty(middlePos) && board.IsEmpty(to);
                }
            }
            // 대각선 캡처
            else if (colDiff == 1 && rowDiff == direction)
            {
                var targetPiece = board.GetPiece(to);
                if (targetPiece != null && IsOpponentPiece(targetPiece))
                    return true;

                // 앙파상
                return CanCaptureEnPassant(from, to, board);
            }

            return false;
        }

        public override string GetSymbol() => "";

        public override string GetUnicodeSymbol()
        {
            return Color == PieceColor.White ? "♙" : "♟";
        }

        public bool IsPromotionSquare(Position position)
        {
            return Color == PieceColor.White ? position.Row == 7 : position.Row == 0;
        }
    }
}