#nullable enable

using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Models the PC Threepeater and Split Pea countdown branches at 100 Hz.</summary>
    public static class AdventureMultiHeadTimingRules
    {
        /// <summary>Gets the mask of all Threepeater heads.</summary>
        public const AdventureShootingHeads ThreepeaterHeads = AdventureShootingHeads.First | AdventureShootingHeads.Second | AdventureShootingHeads.Third;

        /// <summary>Gets the mask of both Split Pea heads.</summary>
        public const AdventureShootingHeads SplitPeaHeads = AdventureShootingHeads.First | AdventureShootingHeads.Second;

        /// <summary>Initializes the original shooter countdown from R(151).</summary>
        /// <param name="kind">Threepeater or Split Pea.</param>
        /// <param name="randomDraw">The injected R(151) result.</param>
        /// <returns>The initialized native timing state.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The species or random draw is invalid.</exception>
        public static AdventureMultiHeadTimingState Initialize(PlantKind kind, int randomDraw)
        {
            ValidateKind(kind);
            ValidateDraw(randomDraw, 151);
            return new AdventureMultiHeadTimingState(kind, randomDraw, 0, AdventureShootingHeads.None);
        }

        /// <summary>Restores an explicitly known native tick-boundary state.</summary>
        /// <param name="kind">Threepeater or Split Pea.</param>
        /// <param name="countdown">The primary attack countdown.</param>
        /// <param name="shootingCountdown">The shared shooting countdown.</param>
        /// <param name="activeHeads">The heads currently playing shooting animations.</param>
        /// <returns>The specified native timing state.</returns>
        /// <exception cref="ArgumentOutOfRangeException">A state value is invalid.</exception>
        public static AdventureMultiHeadTimingState FromRaw(PlantKind kind, int countdown, int shootingCountdown, AdventureShootingHeads activeHeads)
        {
            ValidateKind(kind);
            if (countdown < 0 || shootingCountdown < 0 || (activeHeads & ~HeadsFor(kind)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(countdown));
            }

            return new AdventureMultiHeadTimingState(kind, countdown, shootingCountdown, activeHeads);
        }

        /// <summary>Gets the required R(15) bound, or zero when this tick consumes no draw.</summary>
        /// <param name="state">The state before this update.</param>
        /// <param name="input">The external observations for this update.</param>
        /// <returns>Fifteen when the primary countdown expires, otherwise zero.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The plant species is invalid.</exception>
        public static int RequiredRandomBound(AdventureMultiHeadTimingState state, AdventureMultiHeadTimingInput input)
        {
            ValidateKind(state.Kind);
            return input.PlantUpdateEnabled && input.MainActionEnabled && state.Countdown <= 1 ? 15 : 0;
        }

        /// <summary>Advances old pending fire before starting new attacks, as in the PC update loop.</summary>
        /// <param name="state">The state before this update.</param>
        /// <param name="input">The external observations for this update.</param>
        /// <returns>The new state and per-head firing and animation events.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The species, available-head mask, or required random draw is invalid.</exception>
        public static AdventureMultiHeadTimingResult Tick(AdventureMultiHeadTimingState state, AdventureMultiHeadTimingInput input)
        {
            ValidateKind(state.Kind);
            var heads = HeadsFor(state.Kind);
            if ((input.AvailableHeads & ~heads) != 0 || (state.Kind == PlantKind.Threepeater && (input.AvailableHeads & AdventureShootingHeads.Second) == 0))
            {
                throw new ArgumentOutOfRangeException(nameof(input));
            }

            var countdown = state.Countdown;
            var shooting = state.ShootingCountdown;
            var active = state.ActiveHeads;
            var started = AdventureShootingHeads.None;
            var stopped = AdventureShootingHeads.None;
            var fired = AdventureShootingHeads.None;
            if (!input.PlantUpdateEnabled)
            {
                return new AdventureMultiHeadTimingResult(state, started, stopped, fired);
            }

            if (input.ShootingCountdownEnabled && shooting != 0)
            {
                shooting--;
                if (shooting == 1)
                {
                    fired = active;
                }
                else if (shooting == 0)
                {
                    if (input.AnimationResetReady)
                    {
                        stopped = active;
                        active = AdventureShootingHeads.None;
                    }
                    else
                    {
                        shooting = 1;
                    }
                }
            }

            if (input.MainActionEnabled)
            {
                countdown--;
                if (countdown <= 0)
                {
                    ValidateDraw(input.RandomDraw, 15);
                    countdown = 150 - input.RandomDraw;
                    if (state.Kind == PlantKind.Threepeater)
                    {
                        if ((input.TargetHeads & input.AvailableHeads) != 0)
                        {
                            started = input.AvailableHeads;
                            active |= started;
                            shooting = 35;
                        }
                    }
                    else
                    {
                        if ((input.TargetHeads & AdventureShootingHeads.Second) != 0)
                        {
                            started = AdventureShootingHeads.Second;
                            active |= started;
                            shooting = 26;
                        }

                        stopped |= active & AdventureShootingHeads.First;
                        active &= ~AdventureShootingHeads.First;
                    }
                }

                if (state.Kind == PlantKind.SplitPea && countdown == 25)
                {
                    started |= input.TargetHeads & SplitPeaHeads;
                    active |= started;
                    if (started != AdventureShootingHeads.None)
                    {
                        shooting = 26;
                    }
                }
            }

            return new AdventureMultiHeadTimingResult(new AdventureMultiHeadTimingState(state.Kind, countdown, shooting, active),
                started, stopped, fired);
        }

        /// <summary>Gets the head mask valid for a multi-head shooter species.</summary>
        /// <param name="kind">Threepeater or Split Pea.</param>
        /// <returns>The species' valid head mask.</returns>
        private static AdventureShootingHeads HeadsFor(PlantKind kind)
        {
            return kind == PlantKind.Threepeater ? ThreepeaterHeads : SplitPeaHeads;
        }

        /// <summary>Rejects unsupported plant kinds.</summary>
        /// <param name="kind">The plant kind to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">The kind is neither Threepeater nor Split Pea.</exception>
        private static void ValidateKind(PlantKind kind)
        {
            if (kind != PlantKind.Threepeater && kind != PlantKind.SplitPea)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>Rejects a random result outside its requested bound.</summary>
        /// <param name="draw">The injected random result.</param>
        /// <param name="bound">The native random bound.</param>
        /// <exception cref="ArgumentOutOfRangeException">The draw is outside the native bound.</exception>
        private static void ValidateDraw(int draw, int bound)
        {
            if (draw < 0 || draw >= bound)
            {
                throw new ArgumentOutOfRangeException(nameof(draw));
            }
        }
    }
}
