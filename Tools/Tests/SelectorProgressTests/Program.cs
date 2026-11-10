using System;
using System.Globalization;
using PvZReplica.Progression;

internal static class Program
{
    private static int checks;

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && args[0] == "--native-oracle")
            {
                RunOracleBatch();
                return 0;
            }
            if (args.Length != 0) throw new ArgumentException("Unexpected arguments.");
            NoProfile();
            AdventureBoundaries();
            IndependentUnlockFields();
            CompletionAndRestrictions();
            RuntimeGateTruthTable();
            DistinctPlatformQueries();
            OverrideAndClicks();
            Console.WriteLine("PASS: " + checks + " selector progression checks.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException(message);
    }

    private static SelectorProfileFields Profile(int level = 1, int completed = 0,
        int mini = 0, int puzzle = 0, int survival = 0, int store = 0, bool style = false)
    {
        return new SelectorProfileFields(level, completed, mini, puzzle, survival, store, style);
    }

    private static SelectorRuntimeFields Runtime(bool appFlag = false, int appValue = 0,
        bool platform = false, bool visibilityOverride = false)
    {
        return new SelectorRuntimeFields(appFlag, appValue, platform, visibilityOverride);
    }

    private static void NoProfile()
    {
        SelectorProgressState state = SelectorProgressRules.Evaluate(null, Runtime());
        Check(state.DisplayedAdventureLevel == 1 && state.UseStartAdventureArtwork, "Null profile adventure state");
        Check(state.MiniGamesLocked && state.PuzzleLocked && state.SurvivalLocked, "Null profile mode locks");
        Check(!state.AlmanacVisible && !state.StoreVisible && !state.ZenGardenVisible, "Null profile auxiliary buttons");
        Check(!state.CompletionIndicatorEligible && !state.ProfileButtonStyleFlag, "Null profile decorations");

        state = SelectorProgressRules.Evaluate(null, Runtime(appFlag: true, visibilityOverride: true));
        Check(state.AlmanacVisible && state.StoreVisible && state.ZenGardenVisible, "Override applies without profile");
        Check(state.MiniGamesLocked && state.PuzzleLocked && state.SurvivalLocked, "Override does not unlock modes");
        Check(state.UseStartAdventureArtwork && !state.CompletionIndicatorEligible, "Override does not create profile progress");

        bool threw = false;
        try { SelectorProgressRules.Evaluate(null, null); }
        catch (ArgumentNullException ex) { threw = ex.ParamName == "runtime"; }
        Check(threw, "Missing runtime inputs are rejected");
    }

    private static void AdventureBoundaries()
    {
        int[] levels = { int.MinValue, 0, 1, 2, 14, 15, 24, 25, 44, 45, int.MaxValue };
        bool[] starts = { true, true, true, false, false, false, false, false, false, false, false };
        bool[] almanacs = { false, false, false, false, false, true, true, true, true, true, true };
        bool[] stores = { false, false, false, false, false, false, false, true, true, true, true };
        bool[] gardens = { false, false, false, false, false, false, false, false, false, true, true };
        for (int i = 0; i < levels.Length; i++)
        {
            SelectorProgressState state = SelectorProgressRules.Evaluate(Profile(level: levels[i]), Runtime());
            Check(state.DisplayedAdventureLevel == levels[i], "Level is not clamped: " + levels[i]);
            Check(state.UseStartAdventureArtwork == starts[i], "Adventure artwork at " + levels[i]);
            Check(state.AlmanacVisible == almanacs[i], "Almanac boundary at " + levels[i]);
            Check(state.StoreVisible == stores[i], "Store boundary at " + levels[i]);
            Check(state.ZenGardenVisible == gardens[i], "Garden boundary at " + levels[i]);
            Check(state.MiniGamesLocked && state.PuzzleLocked && state.SurvivalLocked,
                "Level alone does not unlock the three mode signs: " + levels[i]);
        }
    }

    private static void IndependentUnlockFields()
    {
        int[] raw = { int.MinValue, -1, 0, 1, int.MaxValue };
        foreach (int value in raw)
        {
            SelectorProgressState mini = SelectorProgressRules.Evaluate(Profile(mini: value), Runtime());
            SelectorProgressState puzzle = SelectorProgressRules.Evaluate(Profile(puzzle: value), Runtime());
            SelectorProgressState survival = SelectorProgressRules.Evaluate(Profile(survival: value), Runtime());
            SelectorProgressState shop = SelectorProgressRules.Evaluate(Profile(store: value), Runtime());
            bool positive = value == 1 || value == int.MaxValue;
            Check(mini.MiniGamesLocked != positive && mini.PuzzleLocked && mini.SurvivalLocked,
                "MiniGames uses signed > 0 and its own field: " + value);
            Check(puzzle.PuzzleLocked != positive && puzzle.MiniGamesLocked && puzzle.SurvivalLocked,
                "Puzzle uses signed > 0 and its own field: " + value);
            Check(survival.SurvivalLocked != positive && survival.MiniGamesLocked && survival.PuzzleLocked,
                "Survival uses signed > 0 and its own field: " + value);
            Check(shop.StoreVisible == (value != 0) && !shop.AlmanacVisible && !shop.ZenGardenVisible,
                "Store early-access field uses != 0: " + value);
        }
        SelectorProgressState style = SelectorProgressRules.Evaluate(Profile(style: true), Runtime());
        Check(style.ProfileButtonStyleFlag && style.MiniGamesLocked && !style.StoreVisible,
            "Profile button style is not an unlock flag");
    }

    private static void CompletionAndRestrictions()
    {
        foreach (int completed in new[] { 1, int.MaxValue })
        {
            SelectorProgressState full = SelectorProgressRules.Evaluate(Profile(completed: completed), Runtime());
            Check(!full.UseStartAdventureArtwork && !full.MiniGamesLocked && !full.PuzzleLocked && !full.SurvivalLocked,
                "Completion opens all mode signs");
            Check(full.AlmanacVisible && full.StoreVisible && full.ZenGardenVisible && full.CompletionIndicatorEligible,
                "Completion opens auxiliaries in unrestricted runtime");

            SelectorProgressState restricted = SelectorProgressRules.Evaluate(Profile(completed: completed), Runtime(appFlag: true));
            Check(!restricted.MiniGamesLocked && restricted.PuzzleLocked && restricted.SurvivalLocked,
                "Restriction overrides only Puzzle and Survival mode signs");
            Check(restricted.AlmanacVisible && restricted.StoreVisible && !restricted.ZenGardenVisible &&
                !restricted.CompletionIndicatorEligible, "Restriction affects garden and completion indicator");
        }
        foreach (int completed in new[] { int.MinValue, -1, 0 })
        {
            SelectorProgressState state = SelectorProgressRules.Evaluate(Profile(completed: completed), Runtime());
            Check(state.UseStartAdventureArtwork && state.MiniGamesLocked && state.PuzzleLocked && state.SurvivalLocked,
                "Nonpositive completion is not completion: " + completed);
            Check(!state.AlmanacVisible && !state.StoreVisible && !state.ZenGardenVisible &&
                !state.CompletionIndicatorEligible, "Nonpositive completion auxiliaries: " + completed);
        }
    }

    private static void RuntimeGateTruthTable()
    {
        int[] appValues = { int.MinValue, -1, 0, 1, 2, int.MaxValue };
        foreach (int appValue in appValues)
        foreach (bool appFlag in new[] { false, true })
        foreach (bool platform in new[] { false, true })
        {
            SelectorProgressState state = SelectorProgressRules.Evaluate(
                Profile(level: 45, mini: 1, puzzle: 1, survival: 1, store: 1),
                Runtime(appFlag, appValue, platform));
            bool blocked = appFlag || (appValue == 1 && !platform);
            Check(!state.MiniGamesLocked && state.PuzzleLocked == blocked && state.SurvivalLocked == blocked,
                "Runtime mode restriction truth table");
            Check(state.AlmanacVisible && state.StoreVisible && state.ZenGardenVisible != blocked,
                "Runtime auxiliary restriction truth table");
        }
    }

    private static void OverrideAndClicks()
    {
        SelectorProgressState locked = SelectorProgressRules.Evaluate(Profile(), Runtime(visibilityOverride: true));
        Check(locked.AlmanacVisible && locked.StoreVisible && locked.ZenGardenVisible, "Visibility override");
        foreach (SelectorMode mode in new[] { SelectorMode.MiniGames, SelectorMode.Puzzle, SelectorMode.Survival })
        {
            Check(locked.DecideModeClick(mode, 0) == SelectorModeClick.ShowLockedDialog, "Locked mode click");
            Check(locked.DecideModeClick(mode, 1) == SelectorModeClick.Ignored, "Positive input blocker");
            Check(locked.DecideModeClick(mode, -1) == SelectorModeClick.Ignored, "Negative input blocker");
        }
        SelectorProgressState unlocked = SelectorProgressRules.Evaluate(Profile(completed: 1), Runtime());
        foreach (SelectorMode mode in new[] { SelectorMode.MiniGames, SelectorMode.Puzzle, SelectorMode.Survival })
            Check(unlocked.DecideModeClick(mode, 0) == SelectorModeClick.OpenModeSelection, "Unlocked mode click");

        bool threw = false;
        try { locked.DecideModeClick((SelectorMode)99, 0); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "Unknown modes are rejected");
    }

    private static void DistinctPlatformQueries()
    {
        SelectorProgressState state = SelectorProgressRules.Evaluate(Profile(completed: 1),
            new SelectorRuntimeFields(false, 1, true, false, true, false));
        Check(!state.PuzzleLocked && !state.SurvivalLocked && !state.CompletionIndicatorEligible && state.ZenGardenVisible,
            "Completion query can differ from mode and garden queries");
        state = SelectorProgressRules.Evaluate(Profile(completed: 1),
            new SelectorRuntimeFields(false, 1, false, true, false, false));
        Check(state.PuzzleLocked && state.SurvivalLocked && state.CompletionIndicatorEligible && !state.ZenGardenVisible,
            "Mode and garden queries can differ from completion query");
    }

    private static int Bit(bool value) { return value ? 1 : 0; }

    private static void RunOracleBatch()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            string[] parts = line.Split(',');
            if (parts.Length != 16) throw new ArgumentException("Expected 16 input values.");
            int[] v = new int[parts.Length];
            for (int i = 0; i < v.Length; i++) v[i] = int.Parse(parts[i], CultureInfo.InvariantCulture);
            SelectorProfileFields profile = v[0] == 0 ? null : Profile(v[1], v[2], v[3], v[4], v[5], v[6], v[7] != 0);
            SelectorRuntimeFields runtime = new SelectorRuntimeFields(v[8] != 0, v[9],
                v[10] != 0 && v[11] != 0, v[10] != 0 && v[12] != 0,
                v[10] != 0 && v[13] != 0, v[14] != 0);
            SelectorProgressState state = SelectorProgressRules.Evaluate(profile, runtime);
            Console.WriteLine(string.Join(",", new[] {
                state.DisplayedAdventureLevel, Bit(state.UseStartAdventureArtwork),
                Bit(state.MiniGamesLocked), Bit(state.PuzzleLocked), Bit(state.SurvivalLocked),
                Bit(state.AlmanacVisible), Bit(state.StoreVisible), Bit(state.ZenGardenVisible),
                Bit(state.CompletionIndicatorEligible), Bit(state.ProfileButtonStyleFlag),
                (int)state.DecideModeClick(SelectorMode.MiniGames, v[15]),
                (int)state.DecideModeClick(SelectorMode.Puzzle, v[15]),
                (int)state.DecideModeClick(SelectorMode.Survival, v[15])
            }));
        }
    }
}
