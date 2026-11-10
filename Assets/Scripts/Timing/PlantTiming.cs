using System;

namespace PvZUnity.Timing
{
    public enum PlantKind
    {
        Peashooter = 0,
        Sunflower = 1,
        SnowPea = 5,
        Repeater = 7
    }

    public enum ShootingTrack
    {
        None,
        Head,
        Body
    }

    public enum AttackAttempt
    {
        None,
        CountdownExpired,
        RepeaterAt25
    }

    public readonly struct PlantTimingState
    {
        public PlantKind Kind { get; }
        public int Countdown { get; }
        public int ShootingCountdown { get; }

        internal PlantTimingState(PlantKind kind, int countdown, int shootingCountdown)
        {
            Kind = kind;
            Countdown = countdown;
            ShootingCountdown = shootingCountdown;
        }
    }

    // These are snapshots of external game conditions, not modeled game systems.
    public readonly struct PlantTimingInput
    {
        public bool PlantUpdateEnabled { get; }
        public bool ShootingCountdownEnabled { get; }
        public bool MainActionEnabled { get; }
        public bool SunProductionEnabled { get; }
        public bool HasTarget { get; }
        public ShootingTrack Track { get; }
        public bool AnimationResetReady { get; }
        public int RandomDraw { get; }

        public PlantTimingInput(
            bool plantUpdateEnabled,
            bool shootingCountdownEnabled,
            bool mainActionEnabled,
            bool sunProductionEnabled,
            bool hasTarget,
            ShootingTrack track,
            bool animationResetReady,
            int randomDraw)
        {
            PlantUpdateEnabled = plantUpdateEnabled;
            ShootingCountdownEnabled = shootingCountdownEnabled;
            MainActionEnabled = mainActionEnabled;
            SunProductionEnabled = sunProductionEnabled;
            HasTarget = hasTarget;
            Track = track;
            AnimationResetReady = animationResetReady;
            RandomDraw = randomDraw;
        }
    }

    public readonly struct PlantTimingResult
    {
        public PlantTimingState State { get; }
        public AttackAttempt Attempt { get; }
        public bool AnimationStarted { get; }
        public int ProjectilesCreated { get; }
        public bool SunCreated { get; }

        internal PlantTimingResult(
            PlantTimingState state, AttackAttempt attempt, bool animationStarted,
            int projectilesCreated, bool sunCreated)
        {
            State = state;
            Attempt = attempt;
            AnimationStarted = animationStarted;
            ProjectilesCreated = projectilesCreated;
            SunCreated = sunCreated;
        }
    }

    public static class PlantTiming
    {
        public static int InitialRandomBound(PlantKind kind)
        {
            ValidateKind(kind);
            return kind == PlantKind.Sunflower ? 951 : 151;
        }

        public static PlantTimingState Initialize(PlantKind kind, int randomDraw)
        {
            int bound = InitialRandomBound(kind);
            CheckDraw(randomDraw, bound);
            return new PlantTimingState(kind,
                kind == PlantKind.Sunflower ? 300 + randomDraw : randomDraw, 0);
        }

        public static PlantTimingState FromRaw(PlantKind kind, int countdown, int shootingCountdown)
        {
            ValidateKind(kind);
            if (countdown < 0 || shootingCountdown < 0)
                throw new ArgumentOutOfRangeException("Countdowns must be nonnegative at tick boundaries.");
            if (kind == PlantKind.Sunflower && shootingCountdown != 0)
                throw new ArgumentException("This model does not track non-shooting sunflower animation counters.");
            return new PlantTimingState(kind, countdown, shootingCountdown);
        }

        // Returns zero when this tick does not consume a timing random draw.
        public static int RequiredRandomBound(PlantTimingState state, PlantTimingInput input)
        {
            ValidateKind(state.Kind);
            if (!input.PlantUpdateEnabled || !input.MainActionEnabled || state.Countdown > 1)
                return 0;
            if (state.Kind == PlantKind.Sunflower)
                return input.SunProductionEnabled ? 151 : 0;
            return 15;
        }

        public static PlantTimingResult Tick(PlantTimingState state, PlantTimingInput input)
        {
            ValidateKind(state.Kind);
            int countdown = state.Countdown;
            int shooting = state.ShootingCountdown;
            int projectiles = 0;
            bool sun = false;
            bool animation = false;
            AttackAttempt attempt = AttackAttempt.None;

            if (!input.PlantUpdateEnabled)
                return new PlantTimingResult(state, attempt, false, 0, false);

            // In the EXE this runs before the plant's attack/production branch.
            if (state.Kind != PlantKind.Sunflower && input.ShootingCountdownEnabled && shooting != 0)
            {
                shooting--;
                if (shooting == 1)
                    projectiles++;
                else if (shooting == 0 && !input.AnimationResetReady)
                    shooting = 1;
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

            return new PlantTimingResult(new PlantTimingState(state.Kind, countdown, shooting),
                attempt, animation, projectiles, sun);
        }

        private static void ValidateKind(PlantKind kind)
        {
            if (kind != PlantKind.Peashooter && kind != PlantKind.Sunflower &&
                kind != PlantKind.SnowPea && kind != PlantKind.Repeater)
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        private static void CheckDraw(int draw, int bound)
        {
            if (draw < 0 || draw >= bound)
                throw new ArgumentOutOfRangeException(nameof(draw), "Injected draw is outside [0, bound).");
        }
    }
}
