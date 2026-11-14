#nullable enable

using System;
using System.Collections.Generic;
using PlantsVsZombies.Randomness;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Reproduces PC Challenge storm timing, rain reanimations and fullscreen flash opacity.</summary>
    internal sealed class AdventureStormState
    {
        /// <summary>Retains the original local flash generator without allocating its MT array every draw.</summary>
        private readonly OriginalRandom _flashRandom = new OriginalRandom(0);
        /// <summary>Identifies the six-update group used to seed the cached flash noise.</summary>
        private int _noiseSeed = -1;
        /// <summary>Retains the original integer brightness noise from minus 32 through 31.</summary>
        private int _noise;
        /// <summary>Counts updates before the next group of three ground reanimations.</summary>
        private int _rainCountdown;
        /// <summary>Retains only live one-shot rain reanimations.</summary>
        private readonly List<AdventureRainEffect> _rain = new List<AdventureRainEffect>(32);
        /// <summary>Gets the native challenge state: Normal zero or StormFlash1 through StormFlash3, five through seven.</summary>
        internal int Phase { get; set; } = 5;
        /// <summary>Gets the native challenge-state countdown.</summary>
        internal int Countdown { get; set; } = 400;
        /// <summary>Gets the independent Board effect counter used by the rain tiles, including the introduction.</summary>
        internal int EffectTicks { get; private set; }
        /// <summary>Gets the Board main counter used by the local flash noise, excluding the introduction and pauses.</summary>
        internal int MainTicks { get; private set; }
        /// <summary>Gets whether the native loss cutscene has started.</summary>
        internal bool Lost { get; private set; }
        /// <summary>Gets the loss cutscene's elapsed centisecond updates.</summary>
        internal int LostTicks { get; private set; }
        /// <summary>Gets whether PC CutScene has opened its pausing GameOverDialog at 11000 milliseconds.</summary>
        internal bool LossDialogReady => Lost && LostTicks >= 1100;
        /// <summary>Gets the original board pan toward the house during loss.</summary>
        internal int LossBoardX => AdventureIntroPresentation.Curve(1500, 3500, LostTicks * 10, 0, 220, 4);
        /// <summary>Gets the original one-shot ZombiesWon reanimation age after the 6000-millisecond cue.</summary>
        internal float LossGraphicSeconds => Math.Max(0, LostTicks - 599) * .01f;
        /// <summary>Gets the native one-pixel horizontal banner shake.</summary>
        internal float LossShakeX { get; private set; }
        /// <summary>Gets the native one-pixel vertical banner shake.</summary>
        internal float LossShakeY { get; private set; }
        /// <summary>Retains fractional time only for updates outside the normal gameplay loop.</summary>
        internal double PresentationRemainder { get; set; }
        /// <summary>Gets the active original ground reanimations.</summary>
        internal IReadOnlyList<AdventureRainEffect> Rain => _rain;
        /// <summary>Gets whether the native state is inside a flash window rather than the opaque black interval.</summary>
        internal bool FlashVisible => (Phase == 5 || Phase == 6) && Countdown < 300
            || Phase == 7 && Countdown < 150;

        /// <summary>Applies PC Challenge::InitLevel 0x42ACA0 before the camera introduction.</summary>
        internal void BeginIntro()
        {
            Phase = 6;
            Countdown = 150;
        }

        /// <summary>Applies PC Challenge::StartLevel 0x42B000 when the introduction ends.</summary>
        internal void BeginPlaying()
        {
            Phase = 5;
            Countdown = 400;
        }

        /// <summary>Applies PC AnimateBoard 0x447B78's forced preview/return flash.</summary>
        internal void BeginIntroFlash()
        {
            Phase = 6;
            Countdown = 310;
        }

        /// <summary>Starts PC StartZombiesWon without resetting the active weather challenge.</summary>
        internal void BeginLoss()
        {
            Lost = true;
        }

        /// <summary>Advances PC 0x448040 before the board update, preserving the PC timing constants from 0x6EF150.</summary>
        /// <returns>The original chomp or scream Foley cue, or minus one when there is no cue.</returns>
        internal int AdvanceLossClock()
        {
            if (LossDialogReady)
            {
                return -1;
            }

            LostTicks++;
            return LostTicks == 510 || LostTicks == 560 ? 5 : LostTicks == 600 ? 85 : -1;
        }

        /// <summary>Advances PC UpdateStormyNight 0x42EF10 and UpdateRain 0x437750 by one centisecond.</summary>
        /// <param name="random">The existing native board random stream.</param>
        /// <param name="playing">Whether the native scene is Playing, including its award sequence.</param>
        /// <param name="paused">Whether the board objects and rain are paused.</param>
        /// <param name="lost">Whether the native scene is ZombiesWon.</param>
        /// <param name="hasYeti">Whether a Yeti remains in the original zombie iterator.</param>
        /// <returns>Whether this update emits the original Thunder Foley.</returns>
        internal bool Advance(Random random, bool playing, bool paused, bool lost, bool hasYeti)
        {
            if (!paused)
            {
                EffectTicks++;
                if (playing)
                {
                    MainTicks++;
                }

                for (var index = _rain.Count - 1; index >= 0; index--)
                {
                    var rain = _rain[index];
                    rain.Ticks++;
                    if (rain.Ticks * 12 >= (rain.Circle ? 19 : 8) * 100)
                    {
                        _rain.RemoveAt(index);
                    }
                    else
                    {
                        _rain[index] = rain;
                    }
                }
            }

            if (!paused)
            {
                var shaking = Lost && LostTicks >= 700 && LostTicks < 800;
                LossShakeX = shaking ? RainRandomFloat(random, 2f) - 1f : 0f;
                LossShakeY = shaking ? RainRandomFloat(random, 2f) - 1f : 0f;
            }

            var thunder = AdvanceFlash(random, playing, paused, lost, hasYeti);
            if (!paused && --_rainCountdown < 0)
            {
                AddRain(random, false, 90f, 150f, 100, 1.2f);
                AddRain(random, true, 290f, 120f, 50, 1.1f);
                AddRain(random, false, 450f, 130f, 100, 1.2f);
                _rainCountdown = 10 + random.Next(11);
            }

            return thunder;
        }

        /// <summary>Advances the original challenge countdown, including its special pause and Yeti branches.</summary>
        /// <param name="random">The native board random stream.</param>
        /// <param name="playing">Whether the native scene is Playing.</param>
        /// <param name="paused">Whether the native board is paused.</param>
        /// <param name="lost">Whether the native scene is ZombiesWon.</param>
        /// <param name="hasYeti">Whether the zombie iterator contains a Yeti.</param>
        /// <returns>Whether the decremented countdown requests thunder.</returns>
        private bool AdvanceFlash(Random random, bool playing, bool paused, bool lost, bool hasYeti)
        {
            if (paused && Countdown == 1)
            {
                return false;
            }

            if (paused && Phase == 5 && Countdown == 150)
            {
                Countdown = 1;
                return false;
            }

            Countdown--;
            var thunder = Phase == 5 && (Countdown == 300 || Countdown == 150)
                || Phase == 6 && Countdown == 300 || Phase == 7 && Countdown == 150;
            if (Countdown <= 0)
            {
                if (lost)
                {
                    Phase = 7;
                    Countdown = 100 + random.Next(101);
                }
                else if (!playing)
                {
                    Phase = 0;
                    Countdown = 0;
                }
                else
                {
                    Countdown = hasYeti ? 350 + random.Next(101)
                        : 450 + random.Next(random.Next(2) == 0 ? 451 : 101);
                    Phase = 5 + random.Next(3);
                }
            }

            return thunder;
        }

        /// <summary>Creates one rain reanimation in the exact native random-draw order.</summary>
        /// <param name="random">The board random stream.</param>
        /// <param name="circle">Whether to use RainCircle.</param>
        /// <param name="top">The minimum vertical origin.</param>
        /// <param name="height">The random vertical span.</param>
        /// <param name="minimumAlpha">The minimum native opacity.</param>
        /// <param name="maximumScale">The upper native scale.</param>
        private void AddRain(Random random, bool circle, float top, float height, int minimumAlpha, float maximumScale)
        {
            var x = 40f + RainRandomFloat(random, 700f);
            var y = top + RainRandomFloat(random, height);
            var alpha = minimumAlpha + random.Next(101);
            var scale = .7f + RainRandomFloat(random, maximumScale - .7f);
            _rain.Add(new AdventureRainEffect(circle, x, y, scale, alpha));
        }

        /// <summary>Preserves PC 0x5E7060's float store after scaling the full 31-bit draw.</summary>
        /// <param name="random">The native board random stream.</param>
        /// <param name="range">The original float upper bound.</param>
        /// <returns>The scaled native float, without first rounding a scale-one sample.</returns>
        private static float RainRandomFloat(Random random, float range)
        {
            return (float)(random.Next() / 2147483647.0 * range);
        }

        /// <summary>Samples PC DrawStormFlash 0x431CB0 without consuming or reseeding the board random stream.</summary>
        /// <param name="black">The black fullscreen opacity from zero through 255.</param>
        /// <param name="white">The white fullscreen opacity from zero through 255.</param>
        internal void GetFlashAlpha(out int black, out int white)
        {
            black = 255;
            white = 0;
            if (!FlashVisible)
            {
                return;
            }

            var time = Phase == 5 ? Countdown > 150 ? Countdown - 150 : Countdown
                : Phase == 6 ? Countdown / 2 : Countdown;
            var amount = Phase == 5 && Countdown <= 150 ? 92 : 255;
            var seed = MainTicks / 6;
            if (seed != _noiseSeed)
            {
                _noiseSeed = seed;
                _flashRandom.Reseed((uint)seed);
                _noise = (int)_flashRandom.NextBounded(64) - 32;
            }

            black = Math.Max(0, Math.Min(255, Linear(150, 0, time, 255 - amount, 255) + _noise));
            white = Linear(150, 75, time, amount, 0);
        }

        /// <summary>Samples native TodAnimateCurve with reversed endpoints and integer rounding away from zero.</summary>
        /// <param name="start">The first time endpoint.</param>
        /// <param name="end">The last time endpoint, which may precede the first.</param>
        /// <param name="time">The current native time.</param>
        /// <param name="from">The first integer value.</param>
        /// <param name="to">The final integer value.</param>
        /// <returns>The clamped, rounded original linear curve.</returns>
        internal static int Linear(int start, int end, int time, int from, int to)
        {
            var fraction = Math.Max(0f, Math.Min(1f, (float)(time - start) / (end - start)));
            var value = from + (to - from) * fraction;
            return (int)(value > 0f ? value + .5f : value - .5f);
        }
    }
}
