using UnityEngine;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        // Rendering.cs owner calls after DrawAdventureImpacts(row).
        private void DrawAdventureExplosionRow(int row)
        {
            if (adventureParticles == null) return;
            foreach (var explosion in game.Presentation.Explosions)
                if (!explosion.Overlay && explosion.Row == row) DrawAdventureExplosion(explosion);
            foreach (var fire in game.Presentation.JalapenoFires)
            {
                if (fire.Row != row) continue;
                foreach (var flame in fire.Flames)
                {
                    bool done = flame.DoneTick >= 0;
                    float seconds = ((done ? fire.Ticks - flame.DoneTick : fire.Ticks) * .01f) + AdventureRenderRemainder;
                    if (done && seconds >= 10f / 15f) continue;
                    Vector2 origin = new Vector2(flame.X,
                        AdventureCellY(row, (flame.X + 10f - AdventureGridX) / CellWidth) - 10f);
                    animations.DrawNativeFullLastFrame("fire", origin, flame.ScaleX, seconds,
                        done ? "anim_done" : "anim_flame", !done, done ? 15f : flame.Rate);
                }
            }
        }
        // Rendering.cs owner calls after all rows, before the Sun pass (PC layer 400000).
        private void DrawAdventureExplosionOverlay()
        {
            if (adventureParticles == null) return;
            foreach (var explosion in game.Presentation.Explosions)
                if (explosion.Overlay) DrawAdventureExplosion(explosion);
        }
        private void DrawAdventureExplosion(AdventureExplosion explosion)
        {
            Vector2 point = new Vector2(AdventureGridX + explosion.X * CellWidth,
                AdventureCellY(explosion.Row, explosion.X - .5f) + 40f);
            adventureParticles.Draw("native-explosion-" + explosion.Id, explosion.Particle,
                explosion.Ticks * .01f + AdventureRenderRemainder, point, explosion.Seed);
        }
    }
}
