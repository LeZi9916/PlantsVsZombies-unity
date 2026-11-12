#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie death rules.</summary>
    public static class AdventureZombieDeathRules
    {
        /// <summary>Performs animation seconds.</summary>
        /// <param name = "frames">The frames.</param>
        /// <param name = "rate">The rate.</param>
        /// <returns>The resulting animation seconds.</returns>
        public static float AnimationSeconds(int frames, float rate)
        {
            return rate <= 0f ? 0f : (float)(Math.Ceiling(frames * 100d / rate) * .01d);
        }

        /// <summary>Performs fade seconds.</summary>
        /// <param name = "pool">The pool.</param>
        /// <returns>The resulting fade seconds.</returns>
        public static float FadeSeconds(bool pool)
        {
            return pool ? .1f : 1f;
        }

        /// <summary>Performs fall phase.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "clip">The clip.</param>
        /// <returns>The resulting fall phase.</returns>
        public static float FallPhase(int kind, string? clip)
        {
            if (kind == 11 || kind == 12 || kind == 14 || kind == 20 || kind == 22 || kind == 24 || kind == 25)
            {
                return -1f;
            }

            switch (kind)
            {
                case 3:
                    return .68f;
                case 7:
                    return .52f;
                case 5:
                    return .63f;
                case 8:
                case 9:
                    return .76f;
                case 13:
                    return .81f;
                case 15:
                    return .64f;
                case 16:
                    return .68f;
                case 17:
                    return .85f;
                case 18:
                    return .84f;
                case 19:
                    return .68f;
                case 21:
                    return .62f;
                case 23:
                case 32:
                    return .86f;
            }

            return clip == "anim_superlongdeath" ? .788f : clip == "anim_death2" ? .71f : .77f;
        }
    }
}
