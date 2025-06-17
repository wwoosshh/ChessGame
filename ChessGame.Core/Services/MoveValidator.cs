using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Game;
using ChessGame.Core.Models.Pieces.Abstract;
using ChessGame.Core.Models.Pieces.Standard;

namespace ChessGame.Core.Services
{
    public class MoveValidator
    {
        public bool IsValidMove(Move move, GameState gameState)
        {
            var board = gameState.Board;
            var piece = board.GetPiece(move.From);

            if (piece == null)
                return false;

            if (piece.Color != gameState.CurrentPlayer)
                return false;

            // 캐슬링 특별 처리
            if (piece.Type == PieceType.King && Math.Abs(move.To.Column - move.From.Column) == 2)
            {
                return IsValidCastling(move, gameState);
            }

            // 앙파상 특별 처리
            if (piece.Type == PieceType.Pawn &&
                move.To == gameState.EnPassantTarget &&
                Math.Abs(move.To.Column - move.From.Column) == 1)
            {
                return IsValidEnPassant(move, gameState);
            }

            if (!piece.CanMoveTo(move.From, move.To, board))
                return false;

            if (WouldResultInCheck(move, gameState))
                return false;

            return true;
        }

        private bool WouldResultInCheck(Move move, GameState gameState)
        {
            // 임시로 이동을 수행
            var tempState = gameState.Clone();
            var tempBoard = tempState.Board;

            var piece = tempBoard.GetPiece(move.From);
            tempBoard.MovePiece(move.From, move.To);

            // 현재 플레이어의 킹이 체크 상태인지 확인
            var kingPosition = tempBoard.FindKing(gameState.CurrentPlayer);
            if (kingPosition == null)
                return true; // 킹이 없으면 문제

            return IsSquareUnderAttack(kingPosition, gameState.CurrentPlayer, tempBoard);
        }

        public bool IsSquareUnderAttack(Position square, PieceColor defendingColor, ChessBoard board)
        {
            var attackingColor = defendingColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var pos = new Position(row, col);
                    var piece = board.GetPiece(pos);

                    if (piece != null && piece.Color == attackingColor)
                    {
                        if (piece.CanMoveTo(pos, square, board))
                            return true;
                    }
                }
            }

            return false;
        }
        public bool IsValidCastling(Move move, GameState gameState)
        {
            var board = gameState.Board;
            var king = board.GetPiece(move.From);

            if (king == null || king.Type != PieceType.King || king.HasMoved)
                return false;

            int colDiff = move.To.Column - move.From.Column;
            if (Math.Abs(colDiff) != 2)
                return false;

            // 킹이 체크 상태면 캐슬링 불가
            if (gameState.IsCheck)
                return false;

            bool isKingSide = colDiff > 0;
            var rookCol = isKingSide ? 7 : 0;
            var rookPos = new Position(move.From.Row, rookCol);
            var rook = board.GetPiece(rookPos);

            if (rook == null || rook.Type != PieceType.Rook ||
                rook.Color != king.Color || rook.HasMoved)
                return false;

            // 킹이 지나가는 모든 칸이 공격받지 않아야 함
            int startCol = move.From.Column;
            int endCol = move.To.Column;
            int step = isKingSide ? 1 : -1;

            for (int col = startCol; col != endCol + step; col += step)
            {
                var pos = new Position(move.From.Row, col);
                if (IsSquareUnderAttack(pos, king.Color, board))
                    return false;
            }

            return true;
        }

        public bool IsValidEnPassant(Move move, GameState gameState)
        {
            var board = gameState.Board;
            var pawn = board.GetPiece(move.From);

            if (pawn == null || pawn.Type != PieceType.Pawn)
                return false;

            // 앙파상 타겟이 설정되어 있고 목표 위치와 일치하는지 확인
            if (gameState.EnPassantTarget == null || move.To != gameState.EnPassantTarget)
                return false;

            // 캡처할 폰의 위치
            int captureRow = pawn.Color == PieceColor.White ? 4 : 3;
            var capturePos = new Position(captureRow, move.To.Column);
            var targetPawn = board.GetPiece(capturePos);

            return targetPawn != null &&
                   targetPawn.Type == PieceType.Pawn &&
                   targetPawn.Color != pawn.Color;
        }

        public bool IsCheck(PieceColor color, ChessBoard board)
        {
            var kingPosition = board.FindKing(color);
            if (kingPosition == null)
                return false;

            return IsSquareUnderAttack(kingPosition, color, board);
        }

        public bool IsCheckmate(GameState gameState)
        {
            if (!gameState.IsCheck)
                return false;

            // 현재 플레이어가 할 수 있는 모든 합법적인 수를 확인
            return !HasAnyLegalMoves(gameState);
        }

        public bool IsStalemate(GameState gameState)
        {
            if (gameState.IsCheck)
                return false;

            // 현재 플레이어가 할 수 있는 합법적인 수가 없으면 스테일메이트
            return !HasAnyLegalMoves(gameState);
        }

        private bool HasAnyLegalMoves(GameState gameState)
        {
            var board = gameState.Board;
            var currentPlayer = gameState.CurrentPlayer;

            for (int fromRow = 0; fromRow < 8; fromRow++)
            {
                for (int fromCol = 0; fromCol < 8; fromCol++)
                {
                    var from = new Position(fromRow, fromCol);
                    var piece = board.GetPiece(from);

                    if (piece != null && piece.Color == currentPlayer)
                    {
                        var possibleMoves = piece.GetPossibleMoves(from, board);

                        foreach (var to in possibleMoves)
                        {
                            var move = new Move(from, to);
                            if (IsValidMove(move, gameState))
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}