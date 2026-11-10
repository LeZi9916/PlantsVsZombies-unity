#nullable enable

using System;

namespace PlantsVsZombies.Timing
{
    /// <summary>Defines plant timing.</summary>
    public static class PlantTiming
    {
        /// <summary>Performs initial random bound.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting initial random bound.</returns>
        public static int InitialRandomBound(PlantKind kind)
        {
            ValidateKind(kind);
            return kind == PlantKind.Sunflower ? 951 : 151;
        }

        /// <summary>Performs initialize.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "randomDraw">The random draw.</param>
        /// <returns>The resulting initialize.</returns>
        public static PlantTimingState Initialize(PlantKind kind, int randomDraw)
        {
            var bound = InitialRandomBound(kind);
            CheckDraw(randomDraw, bound);
            return new PlantTimingState(kind, kind == PlantKind.Sunflower ? 300 + randomDraw : randomDraw, 0);
        }

        /// <summary>Performs from raw.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "countdown">The countdown.</param>
        /// <param name = "shootingCountdown">The shooting countdown.</param>
        /// <returns>The resulting from raw.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public static PlantTimingState FromRaw(PlantKind kind, int countdown, int shootingCountdown)
        {
            ValidateKind(kind);
            if (countdown < 0 || shootingCountdown < 0)
            {
                throw new ArgumentOutOfRangeException("Countdowns must be nonnegative at tick boundaries.");
            }

            if (kind == PlantKind.Sunflower && shootingCountdown != 0)
            {
                throw new ArgumentException("This model does not track non-shooting sunflower animation counters.");
            }

            return new PlantTimingState(kind, countdown, shootingCountdown);
        }

        // Returns zero when this tick does not consume a timing random draw.
        /// <summary>Performs required random bound.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "input">The input.</param>
        /// <returns>The resulting required random bound.</returns>
        public static int RequiredRandomBound(PlantTimingState state, PlantTimingInput input)
        {
            ValidateKind(state.Kind);
            if (!input.PlantUpdateEnabled || !input.MainActionEnabled || state.Countdown > 1)
            {
                return 0;
            }

            if (state.Kind == PlantKind.Sunflower)
            {
                return input.SunProductionEnabled ? 151 : 0;
            }

            return 15;
        }

        /// <summary>Performs tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "input">The input.</param>
        /// <returns>The resulting tick.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public static PlantTimingResult Tick(PlantTimingState state, PlantTimingInput input)
        {
            ValidateKind(state.Kind);
            var countdown = state.Countdown;
            var shooting = state.ShootingCountdown;
            var projectiles = 0;
            var sun = false;
            var animation = false;
            var attempt = AttackAttempt.None;
            if (!input.PlantUpdateEnabled)
            {
                return new PlantTimingResult(state, attempt, false, 0, false);
            }

            // In the EXE this runs before the plant's attack/production branch.
            if (state.Kind != PlantKind.Sunflower && input.ShootingCountdownEnabled && shooting != 0)
            {
                shooting--;
                if (shooting == 1)
                {
                    projectiles++;
                }
                else if (shooting == 0 && !input.AnimationResetReady)
                {
                    shooting = 1;
                }
            }

            if (input.MainActionEnabled)
            {
                if (state.Kind == PlantKind.Sunflower)
                {
                    if (input.SunProductionEnabled && --countdown <= 0)
                    {
                        CheckDraw(input.RandomDraw, 151);
                        countdown = 2350 + input.RandomDraw;
                        sun = true;
                    }
                }
                else
                {
                    countdown--;
                    if (countdown <= 0)
                    {
                        CheckDraw(input.RandomDraw, 15);
                        countdown = 150 - input.RandomDraw;
                        attempt = AttackAttempt.CountdownExpired;
                    }
                    else if (state.Kind == PlantKind.Repeater && countdown == 25)
                    {
                        attempt = AttackAttempt.RepeaterAt25;
                    }

                    if (attempt != AttackAttempt.None && input.HasTarget)
                    {
                        switch (input.Track)
                        {
                            case ShootingTrack.Head:
                                shooting = state.Kind == PlantKind.Repeater ? 26 : 35;
                                animation = true;
                                break;
                            case ShootingTrack.Body:
                                shooting = 29;
                                animation = true;
                                break;
                            case ShootingTrack.None:
                                projectiles++;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(input), "Unknown shooting track.");
                        }
                    }
                }
            }

            return new PlantTimingResult(new PlantTimingState(state.Kind, countdown, shooting), attempt, animation, projectiles, sun);
        }

        /// <summary>Performs validate kind.</summary>
        /// <param name = "kind">The kind.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        private static void ValidateKind(PlantKind kind)
        {
            if (kind != PlantKind.Peashooter && kind != PlantKind.Sunflower && kind != PlantKind.SnowPea && kind != PlantKind.Repeater)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        /// <summary>Performs check draw.</summary>
        /// <param name = "draw">The draw.</param>
        /// <param name = "bound">The bound.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        private static void CheckDraw(int draw, int bound)
        {
            if (draw < 0 || draw >= bound)
            {
                throw new ArgumentOutOfRangeException(nameof(draw), "Injected draw is outside [0, bound).");
            }
        }
    }
}
