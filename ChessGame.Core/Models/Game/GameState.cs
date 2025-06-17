using ChessGame.Core.Enums;
using ChessGame.Core.Models.Board;

namespace ChessGame.Core.Models.Game
{
    public class GameState
    {
        public Board.ChessBoard Board { get; set; }
        public PieceColor CurrentPlayer { get; set; }
        public List<Move> MoveHistory { get; set; }
        public GameMode GameMode { get; set; }
        public GameResult Result { get; set; }
        public bool IsCheck { get; set; }
        public bool IsCheckmate { get; set; }
        public bool IsStalemate { get; set; }
        public int HalfMoveClock { get; set; } // 50수 규칙용
        public int FullMoveNumber { get; set; }
        public Position? EnPassantTarget { get; set; }

        public GameState()
        {
            Board = new Board.ChessBoard();
            CurrentPlayer = PieceColor.White;
            MoveHistory = new List<Move>();
            GameMode = GameMode.Standard;
            Result = GameResult.InProgress;
            HalfMoveClock = 0;
            FullMoveNumber = 1;
        }

        public void Initialize()
        {
            Board.SetupStandardPosition();
        }

        public GameState Clone()
        {
            return new GameState
            {
                Board = Board.Clone(),
                CurrentPlayer = CurrentPlayer,
                MoveHistory = new List<Move>(MoveHistory),
                GameMode = GameMode,
                Result = Result,
                IsCheck = IsCheck,
                IsCheckmate = IsCheckmate,
                IsStalemate = IsStalemate,
                HalfMoveClock = HalfMoveClock,
                FullMoveNumber = FullMoveNumber,
                EnPassantTarget = EnPassantTarget
            };
        }
    }
}