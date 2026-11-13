#nullable enable
using PlantsVsZombies.BoardUI;
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie bite rules.</summary>
    public static class AdventureZombieBiteRules
    {
        /// <summary>Performs local attack rect.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "bowling">The bowling.</param>
        /// <returns>The resulting local attack rect.</returns>
        public static NativeRect LocalAttackRect(int kind, int phase, bool bowling)
        {
            // PC 0x53DD90 then LoadPlainZombieReanim 0x53FE40. Plain load overrides the constructor's 50/20.
            if (phase == 12 || phase == 54)
            {
                return new NativeRect(-40, 0, 100, 115);
            }

            if (kind == 3 && phase == 11)
            {
                return new NativeRect(bowling ? -229 : -29, 0, bowling ? 270 : 70, 115);
            }

            if (kind == 14 && phase == 53)
            {
                return new NativeRect(-29, 0, 70, 115);
            }

            if (kind == 14 && (phase == 55 || phase == 56))
            {
                return new NativeRect(30, 0, 30, 115);
            }

            if (kind == 18)
            {
                return phase >= 20 && phase <= 28 ? new NativeRect(10, 0, 30, 115) : new NativeRect(20, 17, 50, 115);
            }

            if (kind == 16)
            {
                return new NativeRect(20, 30, 50, 115);
            }

            if (kind == 23 || kind == 32)
            {
                return new NativeRect(-30, -38, 89, 154);
            }

            if (kind == 12 || kind == 22)
            {
                return new NativeRect(10, -13, 133, 140);
            }

            if (kind == 11)
            {
                return new NativeRect(-5, 0, 55, 115);
            }

            if (kind == 21)
            {
                return new NativeRect(10, 0, 50, 115);
            }

            if (kind == 20 || kind == 25)
            {
                return new NativeRect(0, 0, 0, 0);
            }

            if (kind == 0 || kind == 1 || kind == 2 || kind == 4 || kind == 6 || kind == 10 || kind >= 26 && kind <= 31 || kind == 33 || kind == 5)
            {
                return new NativeRect(20, 0, 50, 115);
            }

            return new NativeRect(50, 0, 20, 115);
        }

        /// <summary>Performs attack rect.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "bowling">The bowling.</param>
        /// <param name = "backwards">The backwards.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "bodyY">The body y.</param>
        /// <param name = "clipHeight">The clip height.</param>
        /// <param name = "width">The width.</param>
        /// <returns>The resulting attack rect.</returns>
        public static NativeRect AttackRect(int kind, int phase, bool bowling, bool backwards, int x, int y, int bodyY = 0, int clipHeight = -10000, int width = 120)
        {
            var b = LocalAttackRect(kind, phase, bowling);
            var left = backwards ? width - b.Width - b.X : b.X;
            // PC 0x54DED0 does not shrink the attack rectangle when the reanim scale is .5.
            return new NativeRect(x + left, y + bodyY + b.Y, b.Width, Math.Max(0, b.Height - (clipHeight > -100 ? clipHeight : 0)));
        }

        /// <summary>Performs plant rect.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <returns>The resulting plant rect.</returns>
        public static NativeRect PlantRect(int kind, int x, int y, int width = 80, int height = 80)
        {
            return kind == 23 ? new NativeRect(x + 10, y, width, height) : kind == 30 ? new NativeRect(x, y, width - 20, height) : kind == 47 ? new NativeRect(x, y, 140, 80) : new NativeRect(x + 10, y, width - 20, height);
        }

        /// <summary>Performs horizontal overlap.</summary>
        /// <param name = "a">The a.</param>
        /// <param name = "b">The b.</param>
        /// <returns>The resulting horizontal overlap.</returns>
        public static int HorizontalOverlap(NativeRect a, NativeRect b)
        {
            return Math.Min(a.X + a.Width, b.X + b.Width) - Math.Max(a.X, b.X);
        }

        /// <summary>Performs reaches.</summary>
        /// <param name = "attack">The attack.</param>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting reaches.</returns>
        public static bool Reaches(NativeRect attack, NativeRect plant)
        {
            return HorizontalOverlap(attack, plant) >= 20;
        }

        /// <summary>Performs can chew.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "flying">The flying.</param>
        /// <param name = "sled">The sled.</param>
        /// <returns>The resulting can chew.</returns>
        public static bool CanChew(int kind, int phase, int height, bool flying, bool sled)
        {
            if (kind == 20 || kind == 23 || kind == 32 || kind == 12 || kind == 22 || kind == 25 || flying || sled)
            {
                return false;
            }

            if (height == 9 || height == 6 || height == 1 || height == 2 || height == 7)
            {
                return false;
            }

            switch (phase)
            {
                case 12:
                case 11:
                case 30:
                case 33:
                case 34:
                case 35:
                case 36:
                case 14:
                case 71:
                case 72:
                case 50:
                case 41:
                case 42:
                case 43:
                case 51:
                case 56:
                case 52:
                case 53:
                case 54:
                case 58:
                case 57:
                case 77:
                    return false;
            }

            return !(phase >= 20 && phase <= 28);
        }

        /// <summary>Performs chew phases.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "first">The first.</param>
        /// <param name = "second">The second.</param>
        public static void ChewPhases(int kind, out float first, out float second)
        {
            first = .14f;
            second = .68f;
            switch (kind)
            {
                case 3:
                    first = .38f;
                    second = .8f;
                    break;
                case 5:
                case 21:
                    first = second = .42f;
                    break;
                case 15:
                    first = second = .53f;
                    break;
                case 13:
                    first = .33f;
                    second = .83f;
                    break;
                case 24:
                    first = .33f;
                    second = .79f;
                    break;
            }
        }

        /// <summary>Performs crossed.</summary>
        /// <param name = "before">The before.</param>
        /// <param name = "after">The after.</param>
        /// <param name = "phase">The phase.</param>
        /// <returns>The resulting crossed.</returns>
        public static bool Crossed(double before, double after, float phase)
        {
            return after >= before && Math.Floor(after - phase) > Math.Floor(before - phase);
        }

        /// <summary>Performs chew foley.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "hypnotized">The hypnotized.</param>
        /// <returns>The resulting chew foley.</returns>
        public static int ChewFoley(int plant, bool hypnotized)
        {
            return plant == 3 || plant == 23 || plant == 30 || plant < 0 && hypnotized ? 6 : 5;
        }

        /// <summary>Performs eating checks.</summary>
        /// <param name = "age">The age.</param>
        /// <param name = "dt">The dt.</param>
        /// <param name = "chilled">The chilled.</param>
        /// <returns>The resulting eating checks.</returns>
        public static int EatingChecks(float age, float dt, bool chilled)
        {
            var period = chilled ? 8 : 4;
            return Math.Max(0, (int)Math.Round(age * 100) / period - (int)Math.Round((age - dt) * 100) / period);
        }
    }
}
