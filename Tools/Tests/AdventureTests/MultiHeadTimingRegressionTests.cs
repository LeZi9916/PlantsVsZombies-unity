#nullable enable

using System;
using System.Reflection;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

/// <summary>Checks native multi-head shooter timing against the local PC instruction branches.</summary>
internal static class MultiHeadTimingRegressionTests
{
    /// <summary>Runs the independent timing regressions.</summary>
    public static void Run()
    {
        ThreepeaterAllHeadsFireFromOneTarget();
        ThreepeaterBoardEdgeOmitsMissingHead();
        SplitPeaRearStartsAtExpiration();
        SplitPeaFrontAndRearStartAtTwentyFive();
        PendingShotPrecedesReplacementAttempt();
        PendingShotIgnoresTargetLoss();
        ShootingResetCannotDuplicateFire();
        InvalidDrawIsRejected();
        SimulationThreepeaterPendingShot();
        SimulationThreepeaterEdgeRow();
        SimulationSplitPeaRearPendingShot();
        SimulationSplitPeaIndependentHeads();
        Console.WriteLine("PASS: PC multi-head shooter timing regressions.");
    }

    /// <summary>Creates a normal active tick with an injected target and native random draw.</summary>
    /// <param name="targets">The heads whose target query succeeds.</param>
    /// <param name="available">The heads whose lane is available.</param>
    /// <param name="draw">The native R(15) result when due.</param>
    /// <returns>The observations for one active update.</returns>
    private static AdventureMultiHeadTimingInput Input(AdventureShootingHeads targets, AdventureShootingHeads available, int draw = 0)
    {
        return new AdventureMultiHeadTimingInput(true, true, true, targets, available, true, draw);
    }

    /// <summary>Fails a regression when the expected PC timing branch is absent.</summary>
    /// <param name="condition">Whether the assertion holds.</param>
    /// <param name="message">The behavior under test.</param>
    /// <exception cref="InvalidOperationException">The assertion fails.</exception>
    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Checks that one target starts all valid Threepeater heads and all fire later.</summary>
    private static void ThreepeaterAllHeadsFireFromOneTarget()
    {
        var state = AdventureMultiHeadTimingRules.Initialize(PlantKind.Threepeater, 0);
        var result = AdventureMultiHeadTimingRules.Tick(state,
            Input(AdventureShootingHeads.Second, AdventureMultiHeadTimingRules.ThreepeaterHeads));
        Check(result.State.Countdown == 150 && result.State.ShootingCountdown == 35, "Threepeater starts its 35-count delay.");
        Check(result.StartedHeads == AdventureMultiHeadTimingRules.ThreepeaterHeads, "One target starts all three valid heads.");
        state = result.State;
        for (var tick = 1; tick <= 33; tick++)
        {
            result = AdventureMultiHeadTimingRules.Tick(state,
                Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.ThreepeaterHeads));
            Check(result.FiredHeads == AdventureShootingHeads.None, "Threepeater cannot shoot early at tick " + tick + ".");
            state = result.State;
        }

        result = AdventureMultiHeadTimingRules.Tick(state,
            Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.ThreepeaterHeads));
        Check(result.State.ShootingCountdown == 1 && result.FiredHeads == AdventureMultiHeadTimingRules.ThreepeaterHeads,
            "All three heads fire on the thirty-fourth later tick despite target loss.");
    }

    /// <summary>Checks that a board-edge Threepeater omits the unavailable outer head.</summary>
    private static void ThreepeaterBoardEdgeOmitsMissingHead()
    {
        var available = AdventureShootingHeads.First | AdventureShootingHeads.Second;
        var state = AdventureMultiHeadTimingRules.FromRaw(PlantKind.Threepeater, 1, 0, AdventureShootingHeads.None);
        var result = AdventureMultiHeadTimingRules.Tick(state, Input(AdventureShootingHeads.Second, available));
        Check(result.StartedHeads == available, "Only available heads begin their shooting animation.");
        state = AdventureMultiHeadTimingRules.FromRaw(PlantKind.Threepeater, result.State.Countdown, 2, result.State.ActiveHeads);
        result = AdventureMultiHeadTimingRules.Tick(state, Input(AdventureShootingHeads.None, available));
        Check(result.FiredHeads == available, "The missing edge head never fires.");
    }

    /// <summary>Checks the rear-only expiration attempt and its scheduled projectile.</summary>
    private static void SplitPeaRearStartsAtExpiration()
    {
        var state = AdventureMultiHeadTimingRules.FromRaw(PlantKind.SplitPea, 1, 0, AdventureShootingHeads.First);
        var result = AdventureMultiHeadTimingRules.Tick(state,
            Input(AdventureMultiHeadTimingRules.SplitPeaHeads, AdventureMultiHeadTimingRules.SplitPeaHeads));
        Check(result.StartedHeads == AdventureShootingHeads.Second && result.StoppedHeads == AdventureShootingHeads.First,
            "At expiration Split Pea starts the rear head and idles its front head.");
        Check(result.State.ShootingCountdown == 26 && result.State.ActiveHeads == AdventureShootingHeads.Second,
            "The rear attack owns the shared 26-count delay.");
        state = result.State;
        for (var tick = 1; tick <= 24; tick++)
        {
            result = AdventureMultiHeadTimingRules.Tick(state,
                Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.SplitPeaHeads));
            Check(result.FiredHeads == AdventureShootingHeads.None, "Rear Split Pea shot cannot fire early.");
            state = result.State;
        }

        result = AdventureMultiHeadTimingRules.Tick(state,
            Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.SplitPeaHeads));
        Check(result.FiredHeads == AdventureShootingHeads.Second && result.State.ShootingCountdown == 1,
            "The rear head fires on the twenty-fifth later tick.");
    }

    /// <summary>Checks independent front and rear acquisition at primary countdown 25.</summary>
    private static void SplitPeaFrontAndRearStartAtTwentyFive()
    {
        var state = AdventureMultiHeadTimingRules.FromRaw(PlantKind.SplitPea, 26, 0, AdventureShootingHeads.None);
        var result = AdventureMultiHeadTimingRules.Tick(state,
            Input(AdventureMultiHeadTimingRules.SplitPeaHeads, AdventureMultiHeadTimingRules.SplitPeaHeads));
        Check(result.State.Countdown == 25 && result.State.ShootingCountdown == 26,
            "The extra attempt occurs exactly when the primary countdown becomes 25.");
        Check(result.StartedHeads == AdventureMultiHeadTimingRules.SplitPeaHeads,
            "Front and rear heads acquire targets independently on the extra attempt.");
        state = AdventureMultiHeadTimingRules.FromRaw(PlantKind.SplitPea, result.State.Countdown, 2, result.State.ActiveHeads);
        result = AdventureMultiHeadTimingRules.Tick(state,
            Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.SplitPeaHeads));
        Check(result.FiredHeads == AdventureMultiHeadTimingRules.SplitPeaHeads,
            "Both active head animations create their own projectile at the shared firing tick.");
    }

    /// <summary>Checks that an old projectile resolves before a new countdown-25 attempt overwrites the delay.</summary>
    private static void PendingShotPrecedesReplacementAttempt()
    {
        var state = AdventureMultiHeadTimingRules.FromRaw(PlantKind.SplitPea, 26, 2, AdventureShootingHeads.Second);
        var result = AdventureMultiHeadTimingRules.Tick(state,
            Input(AdventureShootingHeads.First, AdventureMultiHeadTimingRules.SplitPeaHeads));
        Check(result.FiredHeads == AdventureShootingHeads.Second, "The pending rear shot resolves first.");
        Check(result.StartedHeads == AdventureShootingHeads.First && result.State.ShootingCountdown == 26,
            "The front attempt then replaces the shared delay.");
        Check(result.State.ActiveHeads == AdventureMultiHeadTimingRules.SplitPeaHeads,
            "A still-playing rear head remains active until its animation resets.");
    }

    /// <summary>Checks that target loss never cancels a shot already scheduled by an animation.</summary>
    private static void PendingShotIgnoresTargetLoss()
    {
        var state = AdventureMultiHeadTimingRules.FromRaw(PlantKind.SplitPea, 100, 2, AdventureShootingHeads.First);
        var result = AdventureMultiHeadTimingRules.Tick(state,
            Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.SplitPeaHeads));
        Check(result.FiredHeads == AdventureShootingHeads.First, "The front shot is retained after its target leaves.");
    }

    /// <summary>Checks that a held one-count animation cannot emit a projectile twice.</summary>
    private static void ShootingResetCannotDuplicateFire()
    {
        var state = AdventureMultiHeadTimingRules.FromRaw(PlantKind.Threepeater, 100, 2, AdventureShootingHeads.Second);
        var held = new AdventureMultiHeadTimingInput(true, true, true, AdventureShootingHeads.None,
            AdventureMultiHeadTimingRules.ThreepeaterHeads, false, 0);
        var result = AdventureMultiHeadTimingRules.Tick(state, held);
        Check(result.FiredHeads == AdventureShootingHeads.Second, "The first decrement to one fires.");
        result = AdventureMultiHeadTimingRules.Tick(result.State, held);
        Check(result.State.ShootingCountdown == 1 && result.FiredHeads == AdventureShootingHeads.None,
            "An unfinished animation holds at one without firing again.");
        result = AdventureMultiHeadTimingRules.Tick(result.State,
            Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.ThreepeaterHeads));
        Check(result.State.ShootingCountdown == 0 && result.State.ActiveHeads == AdventureShootingHeads.None
            && result.StoppedHeads == AdventureShootingHeads.Second && result.FiredHeads == AdventureShootingHeads.None,
            "The completed animation returns to idle without a second projectile.");
    }

    /// <summary>Checks that a required native random draw cannot exceed R(15).</summary>
    private static void InvalidDrawIsRejected()
    {
        var state = AdventureMultiHeadTimingRules.Initialize(PlantKind.SplitPea, 0);
        Check(AdventureMultiHeadTimingRules.RequiredRandomBound(state,
            Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.SplitPeaHeads)) == 15,
            "The expiring primary countdown requires R(15).");
        try
        {
            AdventureMultiHeadTimingRules.Tick(state,
                Input(AdventureShootingHeads.None, AdventureMultiHeadTimingRules.SplitPeaHeads, 15));
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        throw new InvalidOperationException("The native R(15) result must be in [0, 15).");
    }

    /// <summary>Invokes a private production simulation hook for a controlled combat fixture.</summary>
    /// <param name="game">The isolated adventure simulation.</param>
    /// <param name="method">The production method to invoke.</param>
    /// <param name="arguments">Arguments passed to the production method.</param>
    /// <returns>The production method's result.</returns>
    private static object? Invoke(GameSimulation game, string method, params object[] arguments)
    {
        return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
    }

    /// <summary>Creates an isolated lawn without an imminent automatic wave.</summary>
    /// <returns>A controlled adventure simulation.</returns>
    private static GameSimulation Fixture()
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 46012);
        Check(game.ChooseDefaultAdventureSeeds(), "The multi-head fixture selects its adventure seed bank.");
        game.ResetAdventureWaveSchedule(100000, false, false);
        return game;
    }

    /// <summary>Places one multi-head shooter with an immediately expiring primary countdown.</summary>
    /// <param name="game">The controlled adventure simulation.</param>
    /// <param name="kind">Threepeater or Split Pea.</param>
    /// <param name="row">The target lawn row.</param>
    /// <param name="column">The target lawn column.</param>
    /// <param name="countdown">The injected primary countdown.</param>
    /// <returns>The production plant used by the fixture.</returns>
    private static Plant PlaceShooter(GameSimulation game, PlantKind kind, int row, int column, int countdown)
    {
        var plant = (Plant)Invoke(game, "PlaceAdventurePlant", kind, row, column)!;
        plant.MultiHeadTiming = AdventureMultiHeadTimingRules.FromRaw(kind, countdown, 0, AdventureShootingHeads.None);
        plant.ActionTimer = countdown * .01f;
        plant.Asleep = false;
        return plant;
    }

    /// <summary>Creates a normal grounded zombie at a target row and column coordinate.</summary>
    /// <param name="row">The zombie's lawn row.</param>
    /// <param name="gridX">The zombie's logical horizontal coordinate.</param>
    /// <returns>The zombie used only by this fixture.</returns>
    private static Zombie Target(int row, float gridX)
    {
        return new Zombie
        {
            Id = row * 100 + (int)(gridX * 10f),
            Kind = ZombieKind.Normal,
            Row = row,
            X = gridX,
            Health = 270,
            StartingHealth = 270
        };
    }

    /// <summary>Advances only production plant combat by one native 100 Hz tick.</summary>
    /// <param name="game">The controlled adventure simulation.</param>
    private static void AdvancePlantTick(GameSimulation game)
    {
        Invoke(game, "AdvanceAdventurePlants", .01f);
    }

    /// <summary>Checks that one middle-lane target starts three heads and pending shots survive target loss.</summary>
    private static void SimulationThreepeaterPendingShot()
    {
        var game = Fixture();
        var plant = PlaceShooter(game, PlantKind.Threepeater, 2, 3, 0);
        var target = Target(2, 6f);
        game.Zombies.Add(target);
        AdvancePlantTick(game);
        Check(plant.MultiHeadTiming!.Value.ShootingCountdown == 35,
            "Production Threepeater starts its 35-count shooting delay on the first attempt.");
        Check(plant.PresentationHeadAge[0] == 0f && plant.PresentationHeadAge[1] == 0f && plant.PresentationHeadAge[2] == 0f,
            "One center-lane target starts all three production head animations.");
        game.Zombies.Remove(target);
        for (var tick = 1; tick <= 33; tick++)
        {
            AdvancePlantTick(game);
            Check(game.Peas.Count == 0, "Threepeater must not create a projectile before its scheduled tick.");
        }

        AdvancePlantTick(game);
        Check(game.Peas.Count == 3 && plant.MultiHeadTiming!.Value.ShootingCountdown == 1,
            "Threepeater fires three scheduled projectiles on the thirty-fourth later tick after target loss.");
        Check(game.Peas.Exists(pea => pea.Row == 1) && game.Peas.Exists(pea => pea.Row == 2)
            && game.Peas.Exists(pea => pea.Row == 3), "The three pending projectiles occupy the adjacent and center lanes.");
    }

    /// <summary>Checks that a top-row Threepeater fires only its two available heads.</summary>
    private static void SimulationThreepeaterEdgeRow()
    {
        var game = Fixture();
        var plant = PlaceShooter(game, PlantKind.Threepeater, 0, 3, 0);
        game.Zombies.Add(Target(0, 6f));
        AdvancePlantTick(game);
        Check(plant.MultiHeadTiming!.Value.ActiveHeads == (AdventureShootingHeads.First | AdventureShootingHeads.Second),
            "The off-board upper head never begins shooting on row zero.");
        for (var tick = 0; tick < 34; tick++)
        {
            AdvancePlantTick(game);
        }

        Check(game.Peas.Count == 2 && game.Peas.Exists(pea => pea.Row == 0) && game.Peas.Exists(pea => pea.Row == 1),
            "The top-row Threepeater fires in exactly its two available lanes.");
    }

    /// <summary>Checks Split Pea's rear-only first attempt and delayed shot after its target leaves.</summary>
    private static void SimulationSplitPeaRearPendingShot()
    {
        var game = Fixture();
        var plant = PlaceShooter(game, PlantKind.SplitPea, 2, 4, 0);
        var rear = Target(2, 2f);
        var front = Target(2, 7f);
        game.Zombies.Add(rear);
        game.Zombies.Add(front);
        AdvancePlantTick(game);
        Check(plant.MultiHeadTiming!.Value.ShootingCountdown == 26
            && plant.MultiHeadTiming.Value.ActiveHeads == AdventureShootingHeads.Second,
            "The first Split Pea attempt starts the rear head, independent of a front target.");
        Check(plant.PresentationHeadAge[1] == 0f && game.Peas.Count == 0,
            "The rear animation starts immediately but its projectile remains pending.");
        game.Zombies.Remove(rear);
        game.Zombies.Remove(front);
        for (var tick = 1; tick <= 24; tick++)
        {
            AdvancePlantTick(game);
            Check(game.Peas.Count == 0, "Split Pea cannot create its rear projectile early.");
        }

        AdvancePlantTick(game);
        Check(game.Peas.Count == 1 && game.Peas[0].Backward && plant.MultiHeadTiming!.Value.ShootingCountdown == 1,
            "The pending rear projectile fires on the twenty-fifth later tick after target loss.");
    }

    /// <summary>Checks independent front and rear attacks on the countdown-25 production branch.</summary>
    private static void SimulationSplitPeaIndependentHeads()
    {
        var game = Fixture();
        var plant = PlaceShooter(game, PlantKind.SplitPea, 2, 4, 26);
        game.Zombies.Add(Target(2, 2f));
        game.Zombies.Add(Target(2, 7f));
        AdvancePlantTick(game);
        Check(plant.MultiHeadTiming!.Value.Countdown == 25 && plant.MultiHeadTiming.Value.ShootingCountdown == 26,
            "The production Split Pea makes its extra attack attempt when the primary countdown becomes 25.");
        Check(plant.MultiHeadTiming.Value.ActiveHeads == AdventureMultiHeadTimingRules.SplitPeaHeads
            && plant.PresentationHeadAge[0] == 0f && plant.PresentationHeadAge[1] == 0f,
            "Front and rear targets start their respective head animations independently.");
        Check(game.Peas.Count == 0, "Both directions remain pending during the 26-count delay.");
        for (var tick = 1; tick <= 24; tick++)
        {
            AdvancePlantTick(game);
            Check(game.Peas.Count == 0, "Neither Split Pea head can fire early.");
        }

        AdvancePlantTick(game);
        Check(game.Peas.Count == 2 && game.Peas.Exists(pea => !pea.Backward) && game.Peas.Exists(pea => pea.Backward),
            "The two active Split Pea heads create one forward and one rear projectile at the scheduled tick.");
    }
}
