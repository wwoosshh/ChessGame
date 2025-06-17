using System;
using System.Collections.Generic;
using ChessGame.Core.Enums;
using ChessGame.Core.Models.Game;

namespace ChessGame.Core.Services
{
    public class TurnManager
    {
        private readonly object _turnLock = new object();
        private PieceColor _currentTurn;
        private bool _isProcessingMove;
        private readonly List<TurnRecord> _turnHistory;

        public PieceColor CurrentTurn => _currentTurn;
        public bool IsProcessingMove => _isProcessingMove;

        public TurnManager()
        {
            _currentTurn = PieceColor.White;
            _isProcessingMove = false;
            _turnHistory = new List<TurnRecord>();
        }

        public bool CanMakeMove(PieceColor playerColor)
        {
            lock (_turnLock)
            {
                return !_isProcessingMove && _currentTurn == playerColor;
            }
        }

        public bool StartMove(PieceColor playerColor)
        {
            lock (_turnLock)
            {
                if (_isProcessingMove || _currentTurn != playerColor)
                    return false;

                _isProcessingMove = true;
                return true;
            }
        }

        public void CompleteMove(Move move)
        {
            lock (_turnLock)
            {
                if (!_isProcessingMove)
                    throw new InvalidOperationException("No move in progress");

                // 턴 기록 추가
                _turnHistory.Add(new TurnRecord
                {
                    TurnNumber = _turnHistory.Count + 1,
                    Player = _currentTurn,
                    Move = move,
                    Timestamp = DateTime.Now
                });

                // 턴 변경
                _currentTurn = _currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                _isProcessingMove = false;
            }
        }

        public void CancelMove()
        {
            lock (_turnLock)
            {
                _isProcessingMove = false;
            }
        }

        public bool ValidateTurnSequence()
        {
            if (_turnHistory.Count < 2) return true;

            for (int i = 1; i < _turnHistory.Count; i++)
            {
                var prevTurn = _turnHistory[i - 1];
                var currTurn = _turnHistory[i];

                // 같은 색이 연속으로 두었는지 확인
                if (prevTurn.Player == currTurn.Player)
                    return false;

                // 시간 순서가 올바른지 확인
                if (currTurn.Timestamp <= prevTurn.Timestamp)
                    return false;
            }

            return true;
        }

        public void Reset()
        {
            lock (_turnLock)
            {
                _currentTurn = PieceColor.White;
                _isProcessingMove = false;
                _turnHistory.Clear();
            }
        }

        private class TurnRecord
        {
            public int TurnNumber { get; set; }
            public PieceColor Player { get; set; }
            public Move Move { get; set; } = null!;
            public DateTime Timestamp { get; set; }
        }
    }
}