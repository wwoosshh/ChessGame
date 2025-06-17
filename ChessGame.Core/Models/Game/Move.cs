using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Pieces.Abstract;

namespace ChessGame.Core.Models.Game
{
    public class Move
    {
        public Position From { get; set; }
        public Position To { get; set; }
        public Piece? MovedPiece { get; set; }
        public Piece? CapturedPiece { get; set; }
        public bool IsCapture => CapturedPiece != null;
        public bool IsCastling { get; set; }
        public bool IsEnPassant { get; set; }
        public bool IsPromotion { get; set; }
        public PieceType? PromotionPiece { get; set; }
        public bool IsCheck { get; set; }
        public bool IsCheckmate { get; set; }

        public Move(Position from, Position to)
        {
            From = from;
            To = to;
        }

        public string ToNotation()
        {
            // 간단한 표기법에서 대수 표기법으로 개선
            if (MovedPiece == null)
            {
                // 기본 표기
                return $"{From.ToNotation()}{To.ToNotation()}";
            }

            string notation = "";

            // 캐슬링
            if (IsCastling)
            {
                return To.Column > From.Column ? "O-O" : "O-O-O";
            }

            // 기물 기호 (폰은 생략)
            if (MovedPiece.Type != PieceType.Pawn)
            {
                notation += GetPieceSymbol(MovedPiece.Type);
            }

            // 캡처
            if (IsCapture)
            {
                if (MovedPiece.Type == PieceType.Pawn)
                {
                    notation += (char)('a' + From.Column);
                }
                notation += "x";
            }

            // 도착 위치
            notation += To.ToNotation();

            // 앙파상
            if (IsEnPassant)
                notation += " e.p.";

            // 프로모션
            if (IsPromotion && PromotionPiece.HasValue)
                notation += "=" + GetPieceSymbol(PromotionPiece.Value);

            // 체크/체크메이트
            if (IsCheckmate)
                notation += "#";
            else if (IsCheck)
                notation += "+";

            return notation;
        }

        private string GetPieceSymbol(PieceType type)
        {
            return type switch
            {
                PieceType.King => "K",
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                PieceType.Pawn => "",
                _ => ""
            };
        }
    }
}