#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    // PC 0x53DD90 / 0x540500 / 0x54ADC0. Velocities are pixels per 100 Hz tick.
    /// <summary>Defines adventure zombie native rules.</summary>
    public static class AdventureZombieNativeRules
    {
        // Host origin = (40 + 80*grid - 57*scale). PC body origin = mPosX + 15 + 30*(1-scale).
        /// <summary>Performs native position x.</summary>
        /// <param name = "gridX">The grid x.</param>
        /// <param name = "scale">The scale.</param>
        /// <returns>The resulting native position x.</returns>
        public static float NativePositionX(float gridX, float scale = 1f)
        {
            return 80f * gridX - 5f - 27f * scale;
        }

        /// <summary>Performs grid x.</summary>
        /// <param name = "nativePositionX">The native position x.</param>
        /// <param name = "scale">The scale.</param>
        /// <returns>The resulting grid x.</returns>
        public static float GridX(float nativePositionX, float scale = 1f)
        {
            return (nativePositionX + 5f + 27f * scale) / 80f;
        }

        /// <summary>Performs reanimation source x.</summary>
        /// <param name = "gridX">The grid x.</param>
        /// <param name = "scale">The scale.</param>
        /// <returns>The resulting reanimation source x.</returns>
        public static float ReanimationSourceX(float gridX, float scale = 1f)
        {
            return 20f + 80f * gridX - 37f * scale;
        }

        /// <summary>Performs grid speed.</summary>
        /// <param name = "pixelsPerTick">The pixels per tick.</param>
        /// <returns>The resulting grid speed.</returns>
        public static float GridSpeed(float pixelsPerTick)
        {
            return pixelsPerTick * 100f / 80f;
        }

        /// <summary>Performs zamboni velocity.</summary>
        /// <param name = "nativeX">The native x.</param>
        /// <param name = "previous">The previous.</param>
        /// <returns>The resulting zamboni velocity.</returns>
        public static float ZamboniVelocity(float nativeX, float previous)
        {
            // PC 0x5464A0 truncates mPosX before the 700..300 linear calculation,
            // and retains the prior velocity once mPosX <= 400 (normal undamaged vehicle).
            if (nativeX <= 400f)
            {
                return previous;
            }

            var t = Math.Max(0f, Math.Min(1f, ((int)nativeX - 700) / -400f));
            return .25f - .2f * t;
        }

        /// <summary>Performs spawn source x.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "random40">The random40.</param>
        /// <param name = "random10">The random10.</param>
        /// <returns>The resulting spawn source x.</returns>
        public static float SpawnSourceX(int kind, int random40, int random10)
        {
            switch (kind)
            {
                case 1:
                case 33:
                    return 800f;
                case 3:
                    return 870f + random10;
                case 12:
                    return 800f + random10;
                case 13:
                    return 880f;
                case 22:
                    return 825f + random10;
                case 23:
                case 32:
                    return 845f + random10;
                default:
                    return 780f + random40;
            }
        }

        /// <summary>Performs pick speed.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "iZombie">The i zombie.</param>
        /// <param name = "randomUnit">The random unit.</param>
        /// <returns>The resulting pick speed.</returns>
        public static float PickSpeed(int kind, int phase, bool iZombie, float randomUnit)
        {
            if (phase == 59)
            {
                return .3f;
            }

            if (phase == 37)
            {
                return iZombie ? .23f : .12f;
            }

            if (kind == 24 && iZombie)
            {
                return .9f;
            }

            if (phase == 91)
            {
                return .8f;
            }

            if (kind == 19)
            {
                return .4f;
            }

            if (kind == 8 || kind == 9 || kind == 18 || kind == 1 || kind == 33)
            {
                return .45f;
            }

            if (phase == 32 || phase == 11 || kind == 7 || kind == 11 || kind == 15)
            {
                return .66f + .02f * randomUnit;
            }

            if (phase == 76 || kind == 30)
            {
                return .79f + .02f * randomUnit;
            }

            if (phase == 31 || phase == 51 || phase == 56)
            {
                return .89f + .02f * randomUnit;
            }

            return .23f + .14f * randomUnit;
        }
    }
}
