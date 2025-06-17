using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Game;

namespace ChessGame.AI.Adapters
{
    public class UCIAdapter
    {
        public string MoveToUCI(Move move)
        {
            string uci = $"{move.From.ToNotation()}{move.To.ToNotation()}";

            // 프로모션 처리
            if (move.IsPromotion && move.PromotionPiece.HasValue)
            {
                uci += move.PromotionPiece.Value switch
                {
                    Core.Enums.PieceType.Queen => "q",
                    Core.Enums.PieceType.Rook => "r",
                    Core.Enums.PieceType.Bishop => "b",
                    Core.Enums.PieceType.Knight => "n",
                    _ => ""
                };
            }

            return uci;
        }

        public Move UCIToMove(string uci)
        {
            if (uci.Length < 4)
                throw new ArgumentException("Invalid UCI move");

            var from = new Position(uci.Substring(0, 2));
            var to = new Position(uci.Substring(2, 2));

            var move = new Move(from, to);

            // 프로모션 확인
            if (uci.Length > 4)
            {
                move.IsPromotion = true;
                move.PromotionPiece = uci[4] switch
                {
                    'q' => Core.Enums.PieceType.Queen,
                    'r' => Core.Enums.PieceType.Rook,
                    'b' => Core.Enums.PieceType.Bishop,
                    'n' => Core.Enums.PieceType.Knight,
                    _ => Core.Enums.PieceType.Queen
                };
            }

            return move;
        }
    }
}