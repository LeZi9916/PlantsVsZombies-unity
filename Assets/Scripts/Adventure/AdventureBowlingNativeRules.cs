#nullable enable
using PlantsVsZombies.BoardUI;
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure bowling native rules.</summary>
    public static class AdventureBowlingNativeRules
    {
        // Original Wallnut.reanim _ground frame43..55; negative ground movement rolls right.
        /// <summary>Stores ground.</summary>
        public static readonly float[] Ground =
        {
            44.8f,
            27.6f,
            8.9f,
            -11.3f,
            -31.5f,
            -50.2f,
            -67.4f,
            -84.6f,
            -103.4f,
            -123.6f,
            -143.8f,
            -162.5f,
            -179.8f
        };
        /// <summary>Performs horizontal step.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "age">The age.</param>
        /// <param name = "rate">The rate.</param>
        /// <param name = "giant">The giant.</param>
        /// <returns>The resulting horizontal step.</returns>
        public static int HorizontalStep(int x, float age, float rate, bool giant)
        {
            var phase = age * rate / Ground.Length;
            phase -= (float)Math.Floor(phase);
            var before = Math.Min(Ground.Length - 1, (int)((Ground.Length - 1) * phase));
            var after = Math.Min(Ground.Length - 1, before + 1);
            var velocity = (Ground[after] - Ground[before]) * .01f * rate * (giant ? 2f : 1f);
            // PC casts the resulting absolute position, not just the velocity (important for positive X).
            return (int)(x - velocity);
        }

        /// <summary>Performs vertical step.</summary>
        /// <param name = "y">The y.</param>
        /// <param name = "direction">The direction.</param>
        /// <returns>The resulting vertical step.</returns>
        public static int VerticalStep(int y, int direction)
        {
            return y + (direction == 28 ? -2 : direction == 29 ? 2 : 0);
        }

        /// <summary>Performs at row.</summary>
        /// <param name = "y">The y.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "pitch">The pitch.</param>
        /// <returns>The resulting at row.</returns>
        public static bool AtRow(int y, int row, int pitch = 100)
        {
            return Math.Abs(80 + row * pitch - y) <= 2;
        }

        /// <summary>Performs next direction.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "direction">The direction.</param>
        /// <param name = "hit">The hit.</param>
        /// <param name = "giant">The giant.</param>
        /// <param name = "randomBit">The random bit.</param>
        /// <returns>The resulting next direction.</returns>
        public static int NextDirection(int row, int direction, bool hit, bool giant, int randomBit)
        {
            var next = direction;
            if (direction == 28 && row == 0)
            {
                next = 29;
            }
            else if (direction == 29 && row == 4)
            {
                next = 28;
            }

            if (hit && !giant)
            {
                if (row == 4 || direction == 29)
                {
                    return 28;
                }

                if (row == 0 || direction == 28)
                {
                    return 29;
                }

                return randomBit == 0 ? 29 : 28;
            }

            return next;
        }

        /// <summary>Performs zombie bounds.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "backwards">The backwards.</param>
        /// <param name = "bodyY">The body y.</param>
        /// <param name = "clip">The clip.</param>
        /// <returns>The resulting zombie bounds.</returns>
        public static NativeRect ZombieBounds(int kind, int phase, int x, int y, bool backwards, int bodyY = 0, int clip = -10000)
        {
            int dx = 36, dy = 0, w = 42, h = 115, actor = 120;
            switch (kind)
            {
                case 7:
                    dx = 50;
                    w = 57;
                    break;
                case 17:
                    dx = 50;
                    w = 28;
                    break;
                case 11:
                    dx = 12;
                    w = 62;
                    break;
                case 12:
                case 22:
                    dx = 0;
                    dy = -13;
                    w = 153;
                    h = 140;
                    break;
                case 23:
                case 32:
                    dx = -17;
                    dy = -38;
                    w = 125;
                    h = 154;
                    actor = 180;
                    break;
                case 16:
                    dx = 36;
                    dy = 30;
                    w = 42;
                    break;
                case 20:
                    dx = -20;
                    dy = 22;
                    w = 110;
                    h = 94;
                    break;
                case 18:
                    if (phase < 20 || phase > 28)
                    {
                        dy = 17;
                    }

                    break;
            }

            if (backwards)
            {
                dx = actor - w - dx;
            }

            return new NativeRect(x + dx, y + dy + bodyY, w, Math.Max(0, h - (clip > -100 ? clip : 0)));
        }
    }
}
