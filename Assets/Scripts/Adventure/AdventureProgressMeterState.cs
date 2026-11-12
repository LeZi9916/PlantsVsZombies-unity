#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Tracks the original PC board's 150-unit flag meter and native animation limits.</summary>
    public sealed class AdventureProgressMeterState
    {
        /// <summary>Stores the wave observed by the previous native update.</summary>
        private int _lastWave;
        /// <summary>Stores the remaining hundredths of a second before a newly reached flag finishes rising.</summary>
        private int _flagRaiseTicks;
        /// <summary>Gets the current original meter amount from zero to 150.</summary>
        public int Counter { get; private set; }
        /// <summary>Gets whether the native meter has begun to appear.</summary>
        public bool Visible => Counter > 0;

        /// <summary>Advances the PC 0x420360 wave, health, and countdown driven meter by one simulation tick.</summary>
        /// <param name="tick">The board's native 100 Hz main counter.</param>
        /// <param name="wave">The number of waves already spawned.</param>
        /// <param name="waveCount">The total scheduled wave count.</param>
        /// <param name="wavesPerFlag">The original interval between flag waves.</param>
        /// <param name="hasFlags">Whether this level displays flag posts.</param>
        /// <param name="countdownStart">The current wave's original starting countdown.</param>
        /// <param name="countdown">The remaining wave countdown.</param>
        /// <param name="healthStart">The original total health when this wave spawned.</param>
        /// <param name="healthThreshold">The health threshold for the next wave, or minus one when disabled.</param>
        /// <param name="healthRemaining">The current native health of the last spawned wave.</param>
        public void Advance(int tick, int wave, int waveCount, int wavesPerFlag, bool hasFlags,
            int countdownStart, int countdown, int healthStart, int healthThreshold, int healthRemaining)
        {
            if (wave <= 0 || waveCount <= 1)
            {
                return;
            }

            if (wave != _lastWave && hasFlags && wave % wavesPerFlag == 0)
            {
                _flagRaiseTicks = 100;
            }

            _lastWave = wave;
            if (_flagRaiseTicks > 0)
            {
                _flagRaiseTicks--;
            }

            var normalUnits = 150 - (hasFlags ? 12 * (waveCount / wavesPerFlag) : 0);
            var waveUnits = normalUnits / (waveCount - 1);
            var from = normalUnits * (wave - 1) / (waveCount - 1);
            var to = normalUnits * wave / (waveCount - 1);
            if (hasFlags)
            {
                var flagUnits = 12 * (wave / wavesPerFlag);
                from += flagUnits;
                to += flagUnits;
            }

            var fraction = countdownStart > 0 ? (countdownStart - countdown) / (float)countdownStart : 0f;
            if (healthThreshold != -1)
            {
                var healthRange = Math.Max(healthStart - healthThreshold, 1);
                fraction = Math.Max(fraction, (healthRange + healthThreshold - healthRemaining) / (float)healthRange);
            }

            var portion = (to - from) * fraction;
            var target = Math.Max(1, Math.Min(150, from + (int)(portion > 0f ? portion + .5f : portion - .5f)));
            var gap = target - Counter;
            if (gap > waveUnits && tick % 5 == 0 || gap > 0 && tick % 20 == 0)
            {
                Counter++;
            }
        }

        /// <summary>Updates the boss meter from original body health instead of wave scheduling.</summary>
        /// <param name="health">The boss's remaining body health.</param>
        /// <param name="maxHealth">The boss's original maximum body health.</param>
        public void SetBossHealth(float health, float maxHealth)
        {
            Counter = maxHealth > 0f ? Math.Max(0, Math.Min(150, (int)(150f * (maxHealth - health) / maxHealth))) : 150;
        }

        /// <summary>Gets the original 14-pixel lift of a flag reached by the wave scheduler.</summary>
        /// <param name="flagWave">The one-based wave represented by the flag post.</param>
        /// <returns>The current vertical lift in original pixels.</returns>
        public int FlagLift(int flagWave)
        {
            if (flagWave < _lastWave)
            {
                return 14;
            }

            return flagWave == _lastWave ? AdventureIntroPresentation.Curve(0, 100, 100 - _flagRaiseTicks, 0, 14, 1) : 0;
        }

        /// <summary>Gets the native right-to-left position of a flag post.</summary>
        /// <param name="flagWave">The one-based wave represented by the post.</param>
        /// <param name="waveCount">The board's total number of waves.</param>
        /// <returns>The post's original logical screen X coordinate.</returns>
        public static int FlagX(int flagWave, int waveCount)
        {
            return AdventureIntroPresentation.Curve(0, waveCount, flagWave, 748, 606, 1);
        }

        /// <summary>Gets the native horizontal width of the filled bar.</summary>
        public int FillWidth => AdventureIntroPresentation.Curve(0, 150, Counter, 0, 143, 1);
        /// <summary>Gets the native X coordinate of the moving zombie head.</summary>
        public int HeadX => 738 - AdventureIntroPresentation.Curve(0, 150, Counter, 0, 135, 1);

        /// <summary>Checks the PC adventure exceptions that suppress flag posts.</summary>
        /// <param name="level">The adventure level number.</param>
        /// <param name="replay">Whether the adventure is being replayed.</param>
        /// <returns>Whether the adventure progress meter displays flag posts.</returns>
        public static bool HasFlags(int level, bool replay)
        {
            return level != 15 && level != 50 && (replay || level != 1);
        }
    }
}
