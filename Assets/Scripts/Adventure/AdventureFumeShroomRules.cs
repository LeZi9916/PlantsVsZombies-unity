#nullable enable

using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines original PC Fume-shroom targeting and row-area damage rules.</summary>
    public static class AdventureFumeShroomRules
    {
        /// <summary>Gets the original Fume-shroom horizontal attack rectangle.</summary>
        /// <param name="column">The plant's lawn column.</param>
        /// <returns>The original attack rectangle in board pixels.</returns>
        public static NativeRect AttackRectangle(int column)
        {
            // PC Plant::GetPlantAttackRect uses mX + 60 and width 340 for Fumeshroom.
            return new NativeRect(40 + column * 80 + 60, 0, 340, 80);
        }

        /// <summary>Checks whether a zombie can start a Fume-shroom attack.</summary>
        /// <param name="plant">The Fume-shroom acquiring a target.</param>
        /// <param name="zombie">The potential target.</param>
        /// <returns>Whether the zombie touches the original attack rectangle.</returns>
        public static bool CanTarget(Plant plant, Zombie zombie)
        {
            return CanTarget(plant, zombie, false);
        }

        /// <summary>Checks whether a zombie receives Fume-shroom row-area damage.</summary>
        /// <param name="plant">The Fume-shroom applying damage.</param>
        /// <param name="zombie">The potential target.</param>
        /// <returns>Whether the zombie has positive horizontal overlap with the attack rectangle.</returns>
        public static bool CanDamage(Plant plant, Zombie zombie)
        {
            return CanTarget(plant, zombie, true);
        }

        /// <summary>Checks a Fume-shroom target using either native touch or overlap semantics.</summary>
        /// <param name="plant">The Fume-shroom involved in the operation.</param>
        /// <param name="zombie">The potential target.</param>
        /// <param name="requirePositiveOverlap">Whether the operation requires positive overlap instead of edge contact.</param>
        /// <returns>Whether the zombie passes the native state and rectangle checks.</returns>
        private static bool CanTarget(Plant plant, Zombie zombie, bool requirePositiveOverlap)
        {
            if (zombie == null || zombie.Hypnotized || zombie.Row != plant.Row || zombie.Airborne || zombie.Submerged
                || zombie.DropRemaining > 0f || zombie.Kind == ZombieKind.Bungee
                || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, plant.Kind))
            {
                return false;
            }

            var attack = AttackRectangle(plant.Column);
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            var leftOverlap = body.X + body.Width - attack.X;
            var rightOverlap = attack.X + attack.Width - body.X;
            return requirePositiveOverlap ? leftOverlap > 0 && rightOverlap > 0 : leftOverlap >= 0 && rightOverlap >= 0;
        }
    }
}
