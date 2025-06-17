using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Game;
using ChessGame.Core.Models.Pieces.Abstract;
using System.Text;

namespace ChessGame.AI.Adapters
{
    public class FENAdapter
    {
        public string BoardToFEN(GameState gameState)
        {
            var sb = new StringBuilder();

            // 1. 보드 상태
            for (int row = 7; row >= 0; row--)
            {
                int emptyCount = 0;

                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    var piece = gameState.Board.GetPiece(position);

                    if (piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            sb.Append(emptyCount);
                            emptyCount = 0;
                        }
                        sb.Append(GetPieceFENChar(piece));
                    }
                }

                if (emptyCount > 0)
                    sb.Append(emptyCount);

                if (row > 0)
                    sb.Append('/');
            }

            // 2. 현재 차례
            sb.Append(' ');
            sb.Append(gameState.CurrentPlayer == PieceColor.White ? 'w' : 'b');

            // 3. 캐슬링 권한
            sb.Append(' ');
            string castling = GetCastlingRights(gameState);
            sb.Append(string.IsNullOrEmpty(castling) ? "-" : castling);

            // 4. 앙파상 타겟
            sb.Append(' ');
            sb.Append(gameState.EnPassantTarget?.ToNotation() ?? "-");

            // 5. 50수 규칙 카운터
            sb.Append(' ');
            sb.Append(gameState.HalfMoveClock);

            // 6. 전체 수
            sb.Append(' ');
            sb.Append(gameState.FullMoveNumber);

            return sb.ToString();
        }

        private char GetPieceFENChar(Piece piece)
        {
            char pieceChar = piece.Type switch
            {
                PieceType.King => 'K',
                PieceType.Queen => 'Q',
                PieceType.Rook => 'R',
                PieceType.Bishop => 'B',
                PieceType.Knight => 'N',
                PieceType.Pawn => 'P',
                _ => '?'
            };

            return piece.Color == PieceColor.White ? pieceChar : char.ToLower(pieceChar);
        }

        private string GetCastlingRights(GameState gameState)
        {
            var sb = new StringBuilder();
            var board = gameState.Board;

            // 백색 킹사이드 캐슬링
            var whiteKing = board.GetPiece(new Position(0, 4));
            var whiteKingsideRook = board.GetPiece(new Position(0, 7));
            if (whiteKing != null && !whiteKing.HasMoved &&
                whiteKingsideRook != null && !whiteKingsideRook.HasMoved)
            {
                sb.Append('K');
            }

            // 백색 퀸사이드 캐슬링
            var whiteQueensideRook = board.GetPiece(new Position(0, 0));
            if (whiteKing != null && !whiteKing.HasMoved &&
                whiteQueensideRook != null && !whiteQueensideRook.HasMoved)
            {
                sb.Append('Q');
            }

            // 흑색 킹사이드 캐슬링
            var blackKing = board.GetPiece(new Position(7, 4));
            var blackKingsideRook = board.GetPiece(new Position(7, 7));
            if (blackKing != null && !blackKing.HasMoved &&
                blackKingsideRook != null && !blackKingsideRook.HasMoved)
            {
                sb.Append('k');
            }

            // 흑색 퀸사이드 캐슬링
            var blackQueensideRook = board.GetPiece(new Position(7, 0));
            if (blackKing != null && !blackKing.HasMoved &&
                blackQueensideRook != null && !blackQueensideRook.HasMoved)
            {
                sb.Append('q');
            }

            return sb.ToString();
        }
    }
}