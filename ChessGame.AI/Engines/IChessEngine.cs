using ChessGame.Core.Models.Game;
using System.Threading.Tasks;

namespace ChessGame.AI.Engines
{
    public interface IChessEngine
    {
        Task InitializeAsync();
        Task<Move> GetBestMoveAsync(GameState gameState, int depth = 10);
        Task SetDifficultyAsync(int level);
        Task<EvaluationInfo> EvaluatePositionAsync(GameState gameState, Move? lastMove = null);
        void Dispose();
    }
}