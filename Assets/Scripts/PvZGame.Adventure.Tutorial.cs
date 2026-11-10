using UnityEngine;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private string AdventureFirstWaveTutorialAdvice()
        {
            var board = game.Adventure;
            if (board.Level.IsReplay || board.CurrentWave != 0 || board.Phase != AdventurePhase.Playing) return null;
            int peas = 0, flowers = 0;
            foreach (Plant plant in game.Plants)
            { if (plant.Kind == PlantKind.Peashooter) peas++; if (plant.Kind == PlantKind.Sunflower) flowers++; }
            // These original tutorial messages explain the same accepted-plant thresholds as the PC scheduler.
            if (board.Level.Number == 1 && peas == 1)
            {
                if (selected >= 0) return OriginalProgressionText("ADVICE_CLICK_ON_GRASS");
                if (game.AdventureSunAvailable >= 100 && game.CooldownRemaining(PlantKind.Peashooter) <= 0) return OriginalProgressionText("ADVICE_CLICK_PEASHOOTER");
                return OriginalProgressionText(board.CollectedSun ? "ADVICE_CLICKED_ON_SUN" : "ADVICE_CLICK_ON_SUN");
            }
            if (board.Level.Number == 2 && flowers < 3) return OriginalProgressionText("ADVICE_PLANT_SUNFLOWER2");
            return null;
        }
    }
}
