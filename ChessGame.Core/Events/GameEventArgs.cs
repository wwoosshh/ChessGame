using ChessGame.Core.Models.Game;

namespace ChessGame.Core.Events
{
    public class GameEventArgs : EventArgs
    {
        public GameState GameState { get; set; } = null!;
    }

    public class MoveEventArgs : EventArgs
    {
        public Move Move { get; set; } = null!;
        public GameState GameState { get; set; } = null!;
    }
}