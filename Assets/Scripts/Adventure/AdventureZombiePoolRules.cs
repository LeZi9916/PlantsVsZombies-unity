#nullable enable

using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines the local PC water-lane eligibility, plain-body decoration, and pool emergence curve.</summary>
    public static class AdventureZombiePoolRules
    {
        /// <summary>Checks the local PC ZombieTypeCanGoInPool list at 0x54DDF0.</summary>
        /// <param name="kind">The original zombie identifier.</param>
        /// <returns>Whether this kind may occupy an ordinary water lane.</returns>
        public static bool CanGoInPool(ZombieKind kind)
        {
            var id = (int)kind;
            return id == 0 || id == 1 || id == 2 || id == 4 || id == 11 || id == 14 || id == 16
                || id == 26 || id == 27 || id == 28 || id == 29 || id == 31 || id == 33;
        }

        /// <summary>Checks the PC pool-only spawn classification used by Board::RowCanHaveZombieType.</summary>
        /// <param name="kind">The original zombie identifier.</param>
        /// <returns>Whether the zombie must spawn in water.</returns>
        public static bool IsPoolOnly(ZombieKind kind)
        {
            return kind == ZombieKind.Snorkel || kind == ZombieKind.DolphinRider;
        }

        /// <summary>Checks the original plain-body initialization branch that enables tube and water artwork.</summary>
        /// <param name="kind">The original identifier, retained so armor and head variants remain intact.</param>
        /// <returns>Whether this body uses the original Zombie reanimation's tube and water tracks.</returns>
        public static bool UsesPlainBody(ZombieKind kind)
        {
            return kind == ZombieKind.Normal || kind == ZombieKind.Flag || kind == ZombieKind.Conehead
                || kind == ZombieKind.Buckethead || kind == ZombieKind.DuckyTube;
        }

        /// <summary>Checks the original pool-row wave gate without reclassifying legal airborne zombies as swimmers.</summary>
        /// <param name="kind">The original zombie kind.</param>
        /// <param name="waterRow">Whether the candidate lawn row is water.</param>
        /// <param name="wave">The zero-based wave currently being spawned.</param>
        /// <returns>Whether the water-specific gates permit this row.</returns>
        public static bool CanSpawnInRow(ZombieKind kind, bool waterRow, int wave)
        {
            return waterRow ? CanGoInPool(kind) && (wave >= 5 || IsPoolOnly(kind)) : !IsPoolOnly(kind);
        }

        /// <summary>Samples the original fifty-update pool rise at 0x544970, including native integer curve rounding.</summary>
        /// <param name="remainingTicks">The original phase counter from fifty down to zero.</param>
        /// <param name="scale">The native zombie body scale.</param>
        /// <returns>The altitude from minus one hundred fifty to minus forty, multiplied by body scale.</returns>
        public static float RiseAltitude(int remainingTicks, float scale)
        {
            var remaining = Math.Max(0, Math.Min(50, remainingTicks));
            return (float)Math.Round(-40f - remaining * 110f / 50f, MidpointRounding.AwayFromZero) * scale;
        }

        /// <summary>Gets the body altitude shared by original pool rendering and collision coordinates.</summary>
        /// <param name="zombie">The pool actor being sampled.</param>
        /// <returns>The active pool altitude, or zero when the actor has no pool-body state.</returns>
        public static float BodyAltitude(Zombie zombie)
        {
            return zombie.PoolState?.Altitude ?? 0f;
        }
    }
}
