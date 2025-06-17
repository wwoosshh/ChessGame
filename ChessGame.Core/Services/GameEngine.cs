using System;
using System.Collections.Generic;
using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;
using ChessGame.Core.Models.Game;
using ChessGame.Core.Models.Pieces.Standard;
using ChessGame.Core.Events;

namespace ChessGame.Core.Services
{
    public class GameEngine
    {
        private GameState _gameState;
        private MoveValidator _moveValidator;
        private TurnManager _turnManager;  // 추가
        private readonly object _moveLock = new object();  // 추가

        public event EventHandler<GameEventArgs>? GameEnded;
        public event EventHandler<GameEventArgs>? CheckDetected;
        public event EventHandler<GameEventArgs>? GameStateChanged;
        public event EventHandler<string>? ErrorOccurred;  // 추가

        public GameState GameState => _gameState;

        public GameEngine()
        {
            _gameState = new GameState();
            _moveValidator = new MoveValidator();
            _turnManager = new TurnManager();
        }

        public void StartNewGame(GameMode mode = GameMode.Standard)
        {
            lock (_moveLock)
            {
                _gameState = new GameState { GameMode = mode };
                _gameState.Initialize();
                _turnManager.Reset();
            }
        }

        public bool TryMakeMove(Position from, Position to)
        {
            lock (_moveLock)
            {
                try
                {
                    var move = new Move(from, to);

                    // 1. 기본 검증
                    var piece = _gameState.Board.GetPiece(from);
                    if (piece == null)
                    {
                        ErrorOccurred?.Invoke(this, "선택한 위치에 기물이 없습니다.");
                        return false;
                    }

                    // 2. 턴 검증
                    if (!_turnManager.CanMakeMove(piece.Color))
                    {
                        ErrorOccurred?.Invoke(this, $"현재 {_gameState.CurrentPlayer} 차례입니다.");
                        return false;
                    }

                    // 3. 턴 시작
                    if (!_turnManager.StartMove(piece.Color))
                    {
                        ErrorOccurred?.Invoke(this, "이미 수를 처리 중입니다.");
                        return false;
                    }

                    try
                    {
                        // 4. 이동 검증
                        if (!_moveValidator.IsValidMove(move, _gameState))
                        {
                            _turnManager.CancelMove();
                            return false;
                        }

                        // 5. 이동 실행
                        ExecuteMove(move);

                        // 6. 게임 상태 업데이트
                        UpdateGameState();

                        // 7. 턴 완료
                        _turnManager.CompleteMove(move);

                        // 8. 턴 순서 검증
                        if (!_turnManager.ValidateTurnSequence())
                        {
                            ErrorOccurred?.Invoke(this, "턴 순서 오류가 감지되었습니다.");
                        }

                        // 9. 이벤트 발생
                        GameStateChanged?.Invoke(this, new GameEventArgs { GameState = _gameState });

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _turnManager.CancelMove();
                        ErrorOccurred?.Invoke(this, $"이동 중 오류 발생: {ex.Message}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"예기치 않은 오류: {ex.Message}");
                    return false;
                }
            }
        }

        private void ExecuteMove(Move move)
        {
            var board = _gameState.Board;
            var piece = board.GetPiece(move.From);

            if (piece == null)
                throw new InvalidOperationException("No piece at source position");

            // 캡처된 기물 저장
            move.CapturedPiece = board.GetPiece(move.To);
            move.MovedPiece = piece;

            // 특수 이동 처리
            HandleSpecialMoves(move);

            // 기물 이동
            board.MovePiece(move.From, move.To);

            // 프로모션 처리
            if (piece is Pawn pawn && pawn.IsPromotionSquare(move.To))
            {
                HandlePromotion(move);
            }

            // 이동 기록
            _gameState.MoveHistory.Add(move);

            // 턴 변경
            _gameState.CurrentPlayer = _gameState.CurrentPlayer == PieceColor.White
                ? PieceColor.Black : PieceColor.White;

            // 50수 규칙 업데이트
            if (move.IsCapture || piece is Pawn)
                _gameState.HalfMoveClock = 0;
            else
                _gameState.HalfMoveClock++;

            // 전체 수 업데이트
            if (_gameState.CurrentPlayer == PieceColor.White)
                _gameState.FullMoveNumber++;
        }

        private void HandleSpecialMoves(Move move)
        {
            var board = _gameState.Board;
            var piece = board.GetPiece(move.From);

            if (piece is King && Math.Abs(move.To.Column - move.From.Column) == 2)
            {
                HandleCastling(move);
            }
            else if (piece is Pawn)
            {
                // 앙파상 체크
                if (move.To == _gameState.EnPassantTarget)
                {
                    HandleEnPassant(move);
                }
                // 2칸 전진시 앙파상 타겟 설정
                else if (Math.Abs(move.To.Row - move.From.Row) == 2)
                {
                    int targetRow = (move.From.Row + move.To.Row) / 2;
                    _gameState.EnPassantTarget = new Position(targetRow, move.From.Column);
                }
                else
                {
                    _gameState.EnPassantTarget = null;
                }
            }
            else
            {
                _gameState.EnPassantTarget = null;
            }
        }

        private void HandleCastling(Move move)
        {
            move.IsCastling = true;
            var board = _gameState.Board;

            bool isKingSide = move.To.Column > move.From.Column;
            int rookFromCol = isKingSide ? 7 : 0;
            int rookToCol = isKingSide ? 5 : 3;

            var rookFrom = new Position(move.From.Row, rookFromCol);
            var rookTo = new Position(move.From.Row, rookToCol);

            // 룩 이동
            board.MovePiece(rookFrom, rookTo);
        }

        private void HandleEnPassant(Move move)
        {
            move.IsEnPassant = true;
            var board = _gameState.Board;

            // 캡처된 폰의 위치
            int captureRow = move.From.Row;
            var capturePos = new Position(captureRow, move.To.Column);

            // 캡처된 폰 제거
            move.CapturedPiece = board.GetPiece(capturePos);
            board.SetPiece(capturePos, null);
        }

        private void HandlePromotion(Move move)
        {
            // 기본적으로 퀸으로 승진
            move.IsPromotion = true;
            move.PromotionPiece = PieceType.Queen;

            var board = _gameState.Board;
            var pawn = board.GetPiece(move.To);
            if (pawn != null)
            {
                board.SetPiece(move.To, new Queen(pawn.Color));
            }
        }

        private void UpdateGameState()
        {
            // 체크 확인
            _gameState.IsCheck = _moveValidator.IsCheck(_gameState.CurrentPlayer, _gameState.Board);

            if (_gameState.IsCheck)
            {
                CheckDetected?.Invoke(this, new GameEventArgs { GameState = _gameState });

                // 체크메이트 확인
                _gameState.IsCheckmate = _moveValidator.IsCheckmate(_gameState);
                if (_gameState.IsCheckmate)
                {
                    _gameState.Result = _gameState.CurrentPlayer == PieceColor.White
                        ? GameResult.BlackWins : GameResult.WhiteWins;
                    GameEnded?.Invoke(this, new GameEventArgs { GameState = _gameState });
                    return;
                }
            }
            else
            {
                // 스테일메이트 확인
                _gameState.IsStalemate = _moveValidator.IsStalemate(_gameState);
                if (_gameState.IsStalemate)
                {
                    _gameState.Result = GameResult.Stalemate;
                    GameEnded?.Invoke(this, new GameEventArgs { GameState = _gameState });
                    return;
                }
            }

            // 50수 규칙 확인
            if (_gameState.HalfMoveClock >= 100) // 50수 × 2 (양쪽 플레이어)
            {
                _gameState.Result = GameResult.Draw;
                GameEnded?.Invoke(this, new GameEventArgs { GameState = _gameState });
            }
        }

        public List<Position> GetLegalMoves(Position from)
        {
            var piece = _gameState.Board.GetPiece(from);
            if (piece == null || piece.Color != _gameState.CurrentPlayer)
                return new List<Position>();

            var possibleMoves = piece.GetPossibleMoves(from, _gameState.Board);
            var legalMoves = new List<Position>();

            foreach (var to in possibleMoves)
            {
                var move = new Move(from, to);
                if (_moveValidator.IsValidMove(move, _gameState))
                    legalMoves.Add(to);
            }

            return legalMoves;
        }
    }
}