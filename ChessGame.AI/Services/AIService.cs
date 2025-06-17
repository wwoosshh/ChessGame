using System;
using System.Threading.Tasks;
using ChessGame.AI.Engines;
using ChessGame.Core.Models.Game;
using ChessGame.Core.Enums;

namespace ChessGame.AI.Services
{
    public class AIService : IDisposable
    {
        private StockfishEngine? _engine;

        public async Task InitializeAsync()
        {
            _engine = new StockfishEngine();
            await _engine.InitializeAsync();
        }

        public async Task SetDifficultyAsync(AiDifficulty difficulty)
        {
            if (_engine == null)
                throw new InvalidOperationException("AI not initialized");

            int level = difficulty switch
            {
                AiDifficulty.Easy => 2,    // ~400 ELO
                AiDifficulty.Medium => 5,   // ~1000 ELO
                AiDifficulty.Hard => 8,     // ~1800 ELO
                _ => 5
            };

            await _engine.SetDifficultyAsync(level);
        }

        public async Task<Move> GetBestMoveAsync(GameState gameState)
        {
            if (_engine == null)
                throw new InvalidOperationException("AI not initialized");

            return await _engine.GetBestMoveAsync(gameState);
        }

        public async Task<EvaluationInfo> EvaluatePositionAsync(GameState gameState, Move? lastMove = null)
        {
            if (_engine == null)
                throw new InvalidOperationException("AI not initialized");

            return await _engine.EvaluatePositionAsync(gameState, lastMove);
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}