using System;

namespace PvZUnity.Sun
{
    // Numeric values are the pickup type identifiers in the examined EXE.
    public enum SunKind { Normal = 4, Small = 5, Large = 6 }
    public enum SunPlantKind { Sunflower = 1, SunShroom = 9, TwinSunflower = 41 }

    public readonly struct SkySunState
    {
        public int Countdown { get; }
        public int Drops { get; }

        public SkySunState(int countdown, int drops)
        {
            if (drops < 0 || drops == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(drops));
            Countdown = countdown;
            Drops = drops;
        }
    }

    // The four opaque predicates in the EXE and the outer scene gate are supplied by the caller.
    public readonly struct SkySunContext
    {
        public int Background { get; }
        public int Mode { get; }
        public bool UpdateEnabled { get; }
        public bool BoardFlag5624 { get; }
        public int BoardCountdown561C { get; }
        public int BoardCountdown5618 { get; }
        public int TutorialState { get; }
        public int PlantCount { get; }
        public bool ExternalPredicateBlocked { get; }

        public SkySunContext(int background, int mode, bool updateEnabled, bool boardFlag5624,
            int boardCountdown561C, int boardCountdown5618, int tutorialState, int plantCount,
            bool externalPredicateBlocked)
        {
            Background = background;
            Mode = mode;
            UpdateEnabled = updateEnabled;
            BoardFlag5624 = boardFlag5624;
            BoardCountdown561C = boardCountdown561C;
            BoardCountdown5618 = boardCountdown5618;
            TutorialState = tutorialState;
            PlantCount = plantCount;
            ExternalPredicateBlocked = externalPredicateBlocked;
        }
    }

    public readonly struct SkySunTick
    {
        public SkySunState State { get; }
        public bool Spawned { get; }
        public int X { get; }
        public int Y { get; }
        public SunKind Kind { get; }
        public int MotionType { get; }

        internal SkySunTick(SkySunState state, bool spawned, int x, SunKind kind)
        {
            State = state;
            Spawned = spawned;
            X = x;
            Y = spawned ? 60 : 0;
            Kind = kind;
            MotionType = 0;
        }
    }

    public static class SkySunRules
    {
        public const int InitialDrawBound = 276;
        public const int XDrawBound = 550;
        public const int IntervalDrawBound = 275;

        public static bool SupportsBackground(int background)
        {
            return background != 1 && background != 3 && background != 5 && background != 6 && background != 8;
        }

        // Only call for a background on which the initializer writes the counter.
        public static SkySunState Initialize(int background, int draw)
        {
            if (!SupportsBackground(background)) throw new ArgumentException("This background does not initialize the sky counter.", nameof(background));
            SunValidation.Draw(draw, InitialDrawBound);
            return new SkySunState(425 + draw, 0);
        }

        public static bool CanAdvance(SkySunContext context)
        {
            int mode = context.Mode;
            return context.UpdateEnabled && SupportsBackground(context.Background) && !context.BoardFlag5624
                && context.BoardCountdown561C <= 0 && context.BoardCountdown5618 < 0
                && mode != 19 && mode != 42 && mode != 71 && mode != 72 && mode != 23
                && mode != 43 && mode != 50 && mode != 31 && !context.ExternalPredicateBlocked
                && context.TutorialState != 13
                && ((context.TutorialState != 1 && context.TutorialState != 2) || context.PlantCount != 0);
        }

        public static bool NeedsDraws(SkySunState state, SkySunContext context)
        {
            return CanAdvance(context) && state.Countdown == 1;
        }

        // Draw order at the scheduler is X then interval. Pickup construction can consume more draws.
        public static SkySunTick Tick(SkySunState state, SkySunContext context, int xDraw, int intervalDraw)
        {
            if (!CanAdvance(context)) return new SkySunTick(state, false, 0, SunKind.Normal);
            int countdown = unchecked(state.Countdown - 1);
            if (countdown != 0)
                return new SkySunTick(new SkySunState(countdown, state.Drops), false, 0, SunKind.Normal);
            SunValidation.Draw(xDraw, XDrawBound);
            SunValidation.Draw(intervalDraw, IntervalDrawBound);
            int drops = checked(state.Drops + 1);
            int baseInterval = drops >= 53 ? 950 : 425 + 10 * drops;
            var next = new SkySunState(baseInterval + intervalDraw, drops);
            return new SkySunTick(next, true, 100 + xDraw, context.Mode == 37 ? SunKind.Large : SunKind.Normal);
        }
    }

    public readonly struct SunPlantState
    {
        public SunPlantKind Kind { get; }
        public int Countdown { get; }

        public SunPlantState(SunPlantKind kind, int countdown)
        {
            SunPlantRules.ValidateKind(kind);
            Countdown = countdown;
            Kind = kind;
        }
    }

    public readonly struct SunPlantTick
    {
        public SunPlantState State { get; }
        public int SpawnCount { get; }
        public SunKind SpawnKind { get; }
        public int MotionType { get; }

        internal SunPlantTick(SunPlantState state, int spawnCount, SunKind spawnKind)
        {
            State = state;
            SpawnCount = spawnCount;
            SpawnKind = spawnKind;
            MotionType = 2;
        }
    }

    public static class SunPlantRules
    {
        public const int InitialDrawBound = 951;
        public const int PeriodDrawBound = 151;

        public static SunPlantState Initialize(SunPlantKind kind, int draw)
        {
            ValidateKind(kind);
            SunValidation.Draw(draw, InitialDrawBound);
            return new SunPlantState(kind, 300 + draw);
        }

        // enabled aggregates the plant-update, production and special-mode predicates.
        public static bool NeedsDraw(SunPlantState state, bool enabled)
        {
            ValidateKind(state.Kind);
            return enabled && unchecked(state.Countdown - 1) <= 0;
        }

        public static SunPlantTick Tick(SunPlantState state, bool enabled, int mode, int plantPhase, int draw)
        {
            ValidateKind(state.Kind);
            if (!enabled) return new SunPlantTick(state, 0, SunKind.Normal);
            int countdown = unchecked(state.Countdown - 1);
            if (countdown > 0) return new SunPlantTick(new SunPlantState(state.Kind, countdown), 0, SunKind.Normal);
            SunValidation.Draw(draw, PeriodDrawBound);
            var next = new SunPlantState(state.Kind, 2350 + draw);
            SunKind kind = state.Kind == SunPlantKind.SunShroom && plantPhase == 23 ? SunKind.Small : SunKind.Normal;
            int count = state.Kind == SunPlantKind.TwinSunflower || (state.Kind == SunPlantKind.Sunflower && mode == 39) ? 2 : 1;
            return new SunPlantTick(next, count, kind);
        }

        internal static void ValidateKind(SunPlantKind kind)
        {
            if (kind != SunPlantKind.Sunflower && kind != SunPlantKind.SunShroom && kind != SunPlantKind.TwinSunflower)
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    internal static class SunValidation
    {
        internal static void Draw(int draw, int bound)
        {
            if (draw < 0 || draw >= bound) throw new ArgumentOutOfRangeException(nameof(draw));
        }

        internal static void Kind(SunKind kind)
        {
            if (kind != SunKind.Normal && kind != SunKind.Small && kind != SunKind.Large)
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}
