#nullable enable

using System;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Checks the original Scaredy-shroom fear circle against native zombie body rectangles.</summary>
    public static class AdventureScaredyShroomRules
    {
        /// <summary>Checks Plant::UpdateScaredyShroom's original hostile, lane, and circular overlap gates.</summary>
        /// <param name="plant">The awake Scaredy-shroom performing the fear check.</param>
        /// <param name="zombie">The potential hostile zombie, including airborne and emerging actors.</param>
        /// <param name="body">The actual native zombie rectangle including body offsets and clipping.</param>
        /// <param name="plantY">The plant cell's original board y coordinate including the roof slope.</param>
        /// <returns>Whether this living hostile actor makes the mushroom hide.</returns>
        public static bool CanScare(Plant plant, Zombie zombie, NativeRect body, int plantY)
        {
            return CanScare(plant, zombie, body, body.Y, body.Height, plantY);
        }

        /// <summary>Checks the original circle against a body whose emergence clipping may leave a signed height.</summary>
        /// <param name="plant">The awake Scaredy-shroom performing the fear check.</param>
        /// <param name="zombie">The potential hostile zombie, including underground and emerging actors.</param>
        /// <param name="body">The nonnegative base rectangle supplying the native horizontal interval.</param>
        /// <param name="bodyY">The native top edge after the body's altitude and draw offset are applied.</param>
        /// <param name="bodyHeight">The signed native height remaining after emergence clipping.</param>
        /// <param name="plantY">The plant cell's original board y coordinate including the roof slope.</param>
        /// <returns>Whether PC GetCircleRectOverlap admits the native clipped rectangle.</returns>
        public static bool CanScare(Plant plant, Zombie zombie, NativeRect body, int bodyY, int bodyHeight, int plantY)
        {
            if (zombie.Hypnotized || zombie.Health <= 0f
                || zombie.PresentationNativePhase >= 1 && zombie.PresentationNativePhase <= 3
                || zombie.Kind != ZombieKind.Boss && Math.Abs(zombie.Row - plant.Row) > 1)
            {
                return false;
            }

            var x = 40 + plant.Column * 80;
            var y = plantY + 20;
            var dx = Math.Max(0, Math.Max(body.X - x, x - body.X - body.Width));
            // PC 0x428460 selects the top-edge branch first, even when emergence makes height negative.
            var dy = y < bodyY ? bodyY - y : Math.Max(0, y - bodyY - bodyHeight);
            return dx * dx + dy * dy <= 120 * 120;
        }
    }
}
