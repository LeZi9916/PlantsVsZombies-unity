#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure ice rules.</summary>
    public static class AdventureIceRules
    {
        // PC 54D720, 54D7A0; these are PC phase numbers, not mobile particle enums.
        /// <summary>Performs can chill.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "dead">The dead.</param>
        /// <param name = "hypnotized">The hypnotized.</param>
        /// <param name = "sled">The sled.</param>
        /// <returns>The resulting can chill.</returns>
        public static bool CanChill(int kind, int phase, bool dead, bool hypnotized, bool sled)
        {
            return kind != 12 && !dead && !hypnotized && !sled && phase != 1 && phase != 2 && phase != 3 && phase != 32 && phase != 33 && phase != 34 && phase != 35 && phase != 14 && phase != 50 && (kind != 25 || phase == 87 || phase == 88 || phase == 89);
        }

        /// <summary>Performs can freeze.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "canChill">The can chill.</param>
        /// <returns>The resulting can freeze.</returns>
        public static bool CanFreeze(int kind, int phase, bool canChill)
        {
            return canChill && phase != 12 && phase != 52 && phase != 54 && phase != 58 && phase != 73 && phase != 74 && phase != 71 && phase != 72 && phase != 19 && phase != 16 && phase != 93 && phase != 94 && phase != 95 && !(phase >= 20 && phase <= 28) && (kind != 20 || phase == 6);
        }

        /// <summary>Performs chill ticks.</summary>
        /// <param name = "oldTicks">The old ticks.</param>
        /// <param name = "iceTrap">The ice trap.</param>
        /// <returns>The resulting chill ticks.</returns>
        public static int ChillTicks(int oldTicks, bool iceTrap)
        {
            return Math.Max(oldTicks, iceTrap ? 2000 : 1000);
        }

        /// <summary>Performs freeze ticks.</summary>
        /// <param name = "inPool">The in pool.</param>
        /// <param name = "previouslyCold">The previously cold.</param>
        /// <param name = "randomOffset">The random offset.</param>
        /// <returns>The resulting freeze ticks.</returns>
        public static int FreezeTicks(bool inPool, bool previouslyCold, int randomOffset)
        {
            return inPool ? 300 : (previouslyCold ? 300 : 400) + randomOffset;
        }

        /// <summary>Performs projectile reaches cold state.</summary>
        /// <param name = "damage">The damage.</param>
        /// <param name = "shield">The shield.</param>
        /// <param name = "bypassShield">The bypass shield.</param>
        /// <returns>The resulting projectile reaches cold state.</returns>
        public static bool ProjectileReachesColdState(float damage, float shield, bool bypassShield)
        {
            return bypassShield || damage > shield;
        }

        // 47BB30 attaches particle 45 at top-left +(8,13). The host's 28x28 snow sprite uses a centre.
        /// <summary>Performs trail from sprite centre.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "nativeX">The native x.</param>
        /// <param name = "nativeY">The native y.</param>
        public static void TrailFromSpriteCentre(float x, float y, out float nativeX, out float nativeY)
        {
            nativeX = x - 6f;
            nativeY = y - 1f;
        }

        // 47D410: SnowPeaSplat=43; the original attached-particle coordinates are intentionally +52.
        /// <summary>Performs snow impact.</summary>
        /// <param name = "projectileX">The projectile x.</param>
        /// <param name = "projectileY">The projectile y.</param>
        /// <param name = "attached">The attached.</param>
        /// <param name = "zombieX">The zombie x.</param>
        /// <param name = "zombieY">The zombie y.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "backwards">The backwards.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        public static void SnowImpact(float projectileX, float projectileY, bool attached, int zombieX, int zombieY, int phase, bool backwards, out float x, out float y)
        {
            x = projectileX - 3f;
            y = projectileY + 12f;
            if (!attached)
            {
                return;
            }

            float dx = x + 52f - zombieX, dy = y - zombieY;
            if (phase == 59 || phase == 55)
            {
                dy += 60f;
            }

            if (backwards)
            {
                dx -= 80f;
            }
            else if (projectileX > zombieX + 40)
            {
                dx -= 60f;
            }

            x = zombieX + (int)dx;
            y = zombieY + (int)Math.Max(20f, Math.Min(100f, dy));
        }
    }
}
