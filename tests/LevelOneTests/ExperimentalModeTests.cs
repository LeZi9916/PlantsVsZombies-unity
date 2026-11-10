using System;
using System.Collections.Generic;
using PvZReplica;
using PvZReplica.Levels;

internal static class ExperimentalModeTests
{
    private static int assertions;

    private static void Check(bool condition, string description)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException("Experimental 1-1: " + description);
    }

    private static void Equal<T>(T expected, T actual, string description)
    {
        Check(EqualityComparer<T>.Default.Equals(expected, actual),
            description + " (expected " + expected + ", got " + actual + ")");
    }

    private static void Throws<T>(Action action, string description) where T : Exception
    {
        assertions++;
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Experimental 1-1: expected " + typeof(T).Name + " for " + description);
    }

    private static GameRules ExperimentalRules(ExperimentalLevelOneOptions options)
    {
        return new GameRules {
            EnableExperimentalLevelOne = true,
            LevelOneOptions = options,
            StartingSun = 999,
            ZombieCount = 1,
            FirstZombieDelay = 0.01f,
            ZombieInterval = 0.01f,
            SkySunInterval = 0f,
            ZombieSpeed = 0f,
            MowersEnabled = false
        };
    }

    private static void GiveTwoSuns(GameSimulation game)
    {
        for (int i = 0; i < 2; i++)
        {
            Sun sun = new Sun { X = 1f, Y = 1f, TargetY = 1f };
            game.Suns.Add(sun);
            Check(game.CollectSun(sun), "collect a prepared sun");
        }
    }

    private static void ReachOnslaught(GameSimulation game)
    {
        game.Update(0.01f);
        Check(game.TrySelectLevelOneSeed(PlantKind.Peashooter), "select first seed");
        Check(game.TryPlant(PlantKind.Peashooter, 2, 0), "place first plant");
        GiveTwoSuns(game);
        game.Update(7.51f);
        Equal(LevelOneTutorialStage.AwaitingSeedSelection, game.CurrentLevelOneTutorialStage.Value, "seed ready for second selection");
        Check(game.TrySelectLevelOneSeed(PlantKind.Peashooter), "select second seed");
        Check(game.TryPlant(PlantKind.Peashooter, 2, 1), "place second plant");
        Equal(LevelOneTutorialStage.ZombieOnslaughtPrompt, game.CurrentLevelOneTutorialStage.Value, "onslaught entered");
    }

    private static void DefaultModeIsUnchanged()
    {
        GameRules rules = new GameRules {
            StartingSun = 500, ZombieCount = 1, FirstZombieDelay = 0.02f,
            SkySunInterval = 0f, ZombieSpeed = 0f, MowersEnabled = false
        };
        rules.LevelOneOptions.CompletionCondition = g => { throw new Exception("experimental callback ran in demo"); };
        GameSimulation game = new GameSimulation(rules, 4);
        rules.EnableExperimentalLevelOne = true;
        Check(!game.IsExperimentalLevelOne && !game.LevelOneUsesSingleRowLawn, "default mode stays disabled");
        Equal((LevelOneTutorialStage?)null, game.CurrentLevelOneTutorialStage, "no experimental tutorial in demo");
        Equal(0, game.LevelOneTotalWaves, "no experimental waves in demo");
        Equal(0, game.LevelOneTotalZombies, "no experimental zombie count in demo");
        Check(!game.IsLevelOnePlantingCell(2, 0), "no single-row surface in demo");
        Check(!game.CanSelectLevelOneSeed(PlantKind.Peashooter) && !game.CanPlantLevelOneSeedAt(2, 0) &&
              !game.LevelOneTutorialBlocksWaveCountdown, "experimental queries stay inactive in demo");
        Check(!game.TrySelectLevelOneSeed(PlantKind.Peashooter), "experimental selection disabled in demo");
        Check(game.TryPlant(PlantKind.Sunflower, 0, 0), "demo accepts an unrestricted seed without selection");
        game.Update(0.02f);
        Equal(1, game.SpawnedZombies, "demo retains its own spawn schedule");
        Equal(ZombieKind.Flag, game.Zombies[0].Kind, "demo retains its existing final flag zombie");
    }

    private static void TeachingSelectionAndWaves()
    {
        List<int> delayCalls = new List<int>();
        bool complete = false;
        int completionCalls = 0;
        ExperimentalLevelOneOptions options = new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0005,
            InitialWaveDelaySeconds = 123.0,
            BetweenWaveDelaySeconds = 99.0,
            ZombieSpawnX = 9.0f,
            ZombieSpacingInWave = 0.5f,
            ZombieHealth = 1000f,
            BetweenWaveDelay = (g, wave) => {
                delayCalls.Add(wave);
                return wave == 0 ? 0.03 : wave == 1 ? 0.02 : 0.04;
            },
            CompletionCondition = g => { completionCalls++; return complete && g.Zombies.Count == 0; }
        };
        GameRules rules = ExperimentalRules(options);
        GameSimulation game = new GameSimulation(rules, 9);
        List<GameEvent> events = new List<GameEvent>();
        bool firstPlantEventSawTutorial = false;
        game.EventRaised = e => {
            events.Add(e);
            if (e == GameEvent.Planted && game.Plants.Count == 1)
                firstPlantEventSawTutorial = game.CurrentLevelOneTutorialStage == LevelOneTutorialStage.AwaitingUsableSeed &&
                    game.LevelOneSelectedSeed == null;
        };
        Equal(150, game.SunAmount, "experimental starting sun overrides demo rules");
        Check(game.IsExperimentalLevelOne && game.LevelOneUsesSingleRowLawn, "experimental lawn detection");
        Equal(4, game.LevelOneTotalWaves, "total batches");
        Equal(5, game.LevelOneTotalZombies, "total scheduled zombies");
        Equal(0f, game.LevelOneWaveProgress, "initial progress");
        Equal(LevelOneTutorialStage.NotStarted, game.CurrentLevelOneTutorialStage.Value, "not started before first tick");
        Check(!game.CanSelectLevelOneSeed(PlantKind.Peashooter) && !game.CanPlantLevelOneSeedAt(2, 0),
            "queries reject interaction before first tick");
        Check(!game.TrySelectLevelOneSeed(PlantKind.Peashooter), "selection before gameplay tick rejected");
        Check(!game.TryPlant(PlantKind.Peashooter, 2, 0), "unselected placement rejected");
        for (int row = 0; row < GameSimulation.Rows; row++)
            for (int column = 0; column < GameSimulation.Columns; column++)
                Equal(row == 2, game.IsLevelOnePlantingCell(row, column), "single-row cell");
        Check(!game.IsLevelOnePlantingCell(-1, 0) && !game.IsLevelOnePlantingCell(5, 0) &&
              !game.IsLevelOnePlantingCell(2, 9), "invalid cells return false");

        game.Update(0.01f);
        Equal(LevelOneTutorialStage.AwaitingSeedSelection, game.CurrentLevelOneTutorialStage.Value, "first active update starts teaching");
        Check(game.LevelOneTutorialBlocksWaveCountdown && game.CanSelectLevelOneSeed(PlantKind.Peashooter),
            "tutorial and usable card can be queried without selecting");
        game.Update(20f);
        Equal(0, game.LevelOneWavesDispatched, "tutorial selection blocks wave countdown");
        Equal(0, game.Suns.Count, "sky sun can be disabled");
        Equal(123.0, game.LevelOneWaveCountdownSeconds.Value, "blocked countdown does not advance");
        Check(!game.TrySelectLevelOneSeed(PlantKind.Sunflower), "other seed rejected");
        Check(game.TrySelectLevelOneSeed(PlantKind.Peashooter), "seed selected");
        Check(!game.CanSelectLevelOneSeed(PlantKind.Peashooter) && game.CanPlantLevelOneSeedAt(2, 0) &&
              !game.CanPlantLevelOneSeedAt(1, 0), "selection and planting condition queries");
        Equal((PlantKind?)PlantKind.Peashooter, game.LevelOneSelectedSeed, "selection exposed to host");
        Equal(LevelOneTutorialStage.AwaitingPlantPlacement, game.CurrentLevelOneTutorialStage.Value, "selection advances teaching");
        Check(!game.TrySelectLevelOneSeed(PlantKind.Peashooter), "duplicate selection rejected");
        Check(game.CancelLevelOneSeedSelection(), "selected seed can be cancelled");
        Equal(LevelOneTutorialStage.AwaitingSeedSelection, game.CurrentLevelOneTutorialStage.Value, "cancel restores teaching state");
        Check(!game.CancelLevelOneSeedSelection(), "cannot cancel twice");
        Check(game.TrySelectLevelOneSeed(PlantKind.Peashooter), "select again");
        Check(!game.TryPlant(PlantKind.Sunflower, 2, 0) && !game.TryPlant(PlantKind.Peashooter, 1, 0) &&
              !game.TryPlant(PlantKind.Peashooter, 2, 9), "wrong type and cells rejected");
        Equal((PlantKind?)PlantKind.Peashooter, game.LevelOneSelectedSeed, "invalid placement retains seed");
        Equal(150, game.SunAmount, "rejected input does not spend sun");
        game.TogglePause();
        Check(!game.CanPlantLevelOneSeedAt(2, 0), "placement query observes pause");
        Check(!game.TryPlant(PlantKind.Peashooter, 2, 0) && !game.CancelLevelOneSeedSelection(), "pause rejects experimental input");
        game.Update(100f);
        Equal(0, game.LevelOneWavesDispatched, "pause freezes waves");
        game.TogglePause();
        Check(game.TryPlant(PlantKind.Peashooter, 2, 0), "valid first plant");
        Check(firstPlantEventSawTutorial, "tutorial and selection updated before planted event");
        Check(game.Plants[0].Timing.HasValue, "existing PlantTiming integration remains active");
        Equal(50, game.SunAmount, "first pea spends 100 sun");
        Equal(LevelOneTutorialStage.AwaitingUsableSeed, game.CurrentLevelOneTutorialStage.Value, "waiting for seed");
        Check(game.LevelOneTutorialBlocksWaveCountdown && !game.CanSelectLevelOneSeed(PlantKind.Peashooter),
            "seed recharge and tutorial gate visible to host");
        Check(!game.RemovePlant(2, 0), "unverified shovel disabled by default");
        GiveTwoSuns(game);
        Equal(100, game.SunAmount, "sun collection reaches the next seed cost");
        Check(!game.TrySelectLevelOneSeed(PlantKind.Peashooter), "cooldown still blocks seed");
        game.Update(7.50f);
        Equal(LevelOneTutorialStage.AwaitingUsableSeed, game.CurrentLevelOneTutorialStage.Value, "strict 750-tick cooldown remains gated");
        Equal(0, game.LevelOneWavesDispatched, "cooldown tutorial blocks waves");
        game.Update(0.01f);
        Equal(LevelOneTutorialStage.AwaitingSeedSelection, game.CurrentLevelOneTutorialStage.Value, "recharged and affordable seed advances teaching");
        Check(game.TrySelectLevelOneSeed(PlantKind.Peashooter), "second seed selected");
        Check(!game.TryPlant(PlantKind.Peashooter, 2, 0), "occupied cell rejected");
        Check(game.TryPlant(PlantKind.Peashooter, 2, 1), "second plant accepted");
        Equal(LevelOneTutorialStage.ZombieOnslaughtPrompt, game.CurrentLevelOneTutorialStage.Value, "second plant starts onslaught stage");
        Check(!game.LevelOneTutorialBlocksWaveCountdown && !game.CanPlantLevelOneSeedAt(2, 0),
            "onslaught releases tutorial gate without selecting a seed");
        Check(Math.Abs(game.LevelOneWaveCountdownSeconds.Value - 0.05) < 0.0000001, "100 native ticks use configurable conversion");
        Equal(0, completionCalls, "completion policy not evaluated early");

        game.Update(0.04f);
        Equal(0, game.LevelOneWavesDispatched, "first batch waits for reset countdown");
        game.Update(0.01f);
        Equal(1, game.LevelOneWavesDispatched, "first batch dispatched");
        Equal(1, game.SpawnedZombies, "first batch size");
        Equal(0.25f, game.LevelOneWaveProgress, "batch progress after first wave");
        game.Update(0.02f);
        Equal(1, game.LevelOneWavesDispatched, "custom inter-wave delay respected");
        game.Update(0.01f);
        Equal(2, game.LevelOneWavesDispatched, "second batch dispatched");
        Equal(2, game.SpawnedZombies, "second batch size");
        game.Update(0.02f);
        Equal(3, game.LevelOneWavesDispatched, "third batch dispatched");
        Equal(3, game.SpawnedZombies, "third batch size");
        game.Update(0.04f);
        Equal(4, game.LevelOneWavesDispatched, "fourth batch dispatched");
        Equal(5, game.SpawnedZombies, "fourth batch contains two zombies");
        Equal(1f, game.LevelOneWaveProgress, "coarse progress complete");
        Equal((double?)null, game.LevelOneWaveCountdownSeconds, "no countdown after last batch");
        Equal(3, delayCalls.Count, "delay callback called only between batches");
        Check(delayCalls[0] == 0 && delayCalls[1] == 1 && delayCalls[2] == 2, "delay callback indices");
        Equal(5, game.Zombies.Count, "all spawned zombies still present");
        foreach (Zombie zombie in game.Zombies)
            Check(zombie.Kind == ZombieKind.Normal && zombie.Row == 2 && zombie.Health == 1000f &&
                  zombie.ArmorHealth == 0f && zombie.ShieldHealth == 0f, "normal zombie in center lane");
        Check(Math.Abs(game.Zombies[3].X - 9f) < 0.00001f && Math.Abs(game.Zombies[4].X - 9.5f) < 0.00001f,
            "replaceable in-wave spawn spacing");
        Check(completionCalls > 0 && game.State == MatchState.Playing, "custom completion can wait for reward handling");
        game.Zombies.Clear();
        game.Update(0.01f);
        Equal(MatchState.Playing, game.State, "clearing zombies alone does not override custom completion");
        complete = true;
        game.Update(0.01f);
        Equal(MatchState.Won, game.State, "custom completion ends experiment");
        Equal(1, events.FindAll(e => e == GameEvent.Won).Count, "won event raised once");
        game.Update(1f);
        Equal(1, events.FindAll(e => e == GameEvent.Won).Count, "completed experiment stops updating");
    }

    private static void SkySunAndRemovalOptions()
    {
        GameRules mowerRules = ExperimentalRules(new ExperimentalLevelOneOptions());
        mowerRules.MowersEnabled = true;
        GameSimulation mowerGame = new GameSimulation(mowerRules);
        for (int row = 0; row < GameSimulation.Rows; row++)
            Equal(row == 2, mowerGame.Mowers[row].Available, "provisional mower only on open lane");

        GameRules defaultSunRules = ExperimentalRules(new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0001, SkySunTargetY = 2.25f
        });
        defaultSunRules.SkySunInterval = 1f;
        GameSimulation defaultSun = new GameSimulation(defaultSunRules, 8);
        defaultSun.Update(0.01f);
        Check(defaultSun.TrySelectLevelOneSeed(PlantKind.Peashooter) && defaultSun.TryPlant(PlantKind.Peashooter, 2, 0),
            "default single-row sun setup");
        defaultSun.Update(0.03f);
        Check(defaultSun.Suns.Count == 1 && defaultSun.Suns[0].TargetY == 2.25f,
            "default sky-sun target is configurable");

        int made = 0;
        ExperimentalLevelOneOptions options = new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0005,
            AllowPlantRemoval = true,
            SkySunFactory = (g, rng) => { made++; return new Sun { X = 3f, Y = -1f, TargetY = 2.7f }; }
        };
        GameRules rules = ExperimentalRules(options);
        rules.SkySunInterval = 0.05f;
        GameSimulation game = new GameSimulation(rules, 7);
        game.Update(5f);
        Equal(0, made, "tutorial blocks sky sun before first plant");
        Check(game.TrySelectLevelOneSeed(PlantKind.Peashooter), "select for custom sun");
        game.Update(1f);
        Equal(0, made, "held seed with no plant also blocks sun");
        Check(game.TryPlant(PlantKind.Peashooter, 2, 0), "place for custom sun");
        game.Update(0.12f);
        Check(made > 0 && game.Suns.Count > 0 && game.Suns[0].X == 3f, "replaceable sky-sun factory runs after first planting");
        Check(game.RemovePlant(2, 0), "plant removal may be enabled explicitly");
        rules.SkySunInterval = 0f;
        GiveTwoSuns(game);
        game.Update(7.51f);
        Check(game.TrySelectLevelOneSeed(PlantKind.Peashooter) && game.TryPlant(PlantKind.Peashooter, 2, 8),
            "replacement plant after removal");
        Equal(LevelOneTutorialStage.AwaitingUsableSeed, game.CurrentLevelOneTutorialStage.Value,
            "tutorial uses current simulation count, not lifetime placements");
    }

    private static void EndToEndAndOptions()
    {
        ExperimentalLevelOneOptions options = new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0001, BetweenWaveDelaySeconds = 0.01,
            ZombieSpawnX = 3f, ZombieSpacingInWave = 0.2f, ZombieHealth = 20f
        };
        GameSimulation game = new GameSimulation(ExperimentalRules(options), 2);
        int shots = 0;
        game.EventRaised = e => { if (e == GameEvent.Shoot) shots++; };
        ReachOnslaught(game);
        game.Update(30f);
        Equal(4, game.LevelOneWavesDispatched, "all waves in live combat");
        Equal(5, game.SpawnedZombies, "all five zombies in live combat");
        Check(shots > 0, "PlantTiming drives actual projectiles in experiment");
        Equal(MatchState.Won, game.State, "provisional default completion can be reached through combat");

        Throws<ArgumentNullException>(() => new GameSimulation(ExperimentalRules(null)), "missing options");
        Throws<ArgumentOutOfRangeException>(() => new GameSimulation(ExperimentalRules(new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0 })), "invalid native tick conversion");
        Throws<ArgumentOutOfRangeException>(() => new GameSimulation(ExperimentalRules(new ExperimentalLevelOneOptions {
            BetweenWaveDelaySeconds = double.NaN })), "invalid wave delay");
        Throws<ArgumentOutOfRangeException>(() => new GameSimulation(ExperimentalRules(new ExperimentalLevelOneOptions {
            ZombieHealth = 0f })), "invalid zombie health");
        Throws<ArgumentOutOfRangeException>(() => new GameSimulation(ExperimentalRules(new ExperimentalLevelOneOptions {
            SkySunTargetY = float.NaN })), "invalid sky-sun target");

        ExperimentalLevelOneOptions snapshot = new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0001, BetweenWaveDelaySeconds = 0.02,
            CompletionCondition = g => false
        };
        game = new GameSimulation(ExperimentalRules(snapshot));
        snapshot.BetweenWaveDelaySeconds = 1000;
        snapshot.CompletionCondition = g => true;
        ReachOnslaught(game);
        game.Update(0.1f);
        Equal(4, game.LevelOneWavesDispatched, "options are captured at construction");
        game.Zombies.Clear();
        game.Update(0.01f);
        Equal(MatchState.Playing, game.State, "captured completion policy remains in force");
    }

    private static void FixedStepsAndCallbackBoundaries()
    {
        Func<ExperimentalLevelOneOptions> options = () => new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0001,
            BetweenWaveDelaySeconds = 0.05,
            ZombieHealth = 10000f,
            CompletionCondition = g => false
        };
        GameSimulation largeFrames = new GameSimulation(ExperimentalRules(options()), 123);
        GameSimulation smallFrames = new GameSimulation(ExperimentalRules(options()), 123);
        ReachOnslaught(largeFrames);
        ReachOnslaught(smallFrames);
        largeFrames.Update(1f);
        for (int i = 0; i < 100; i++) smallFrames.Update(0.01f);
        Equal(largeFrames.LevelOneWavesDispatched, smallFrames.LevelOneWavesDispatched, "batch count is frame-independent");
        Equal(largeFrames.SpawnedZombies, smallFrames.SpawnedZombies, "spawn count is frame-independent");
        Equal(largeFrames.Peas.Count, smallFrames.Peas.Count, "PlantTiming shots are frame-independent");
        Equal(largeFrames.State, smallFrames.State, "completion state is frame-independent");
        for (int i = 0; i < largeFrames.Zombies.Count; i++)
        {
            Check(Math.Abs(largeFrames.Zombies[i].X - smallFrames.Zombies[i].X) < 0.00001f &&
                  largeFrames.Zombies[i].Kind == smallFrames.Zombies[i].Kind,
                "zombie state is frame-independent");
        }
        for (int i = 0; i < largeFrames.Plants.Count; i++)
        {
            Equal(largeFrames.Plants[i].Timing.Value.Countdown, smallFrames.Plants[i].Timing.Value.Countdown,
                "PlantTiming countdown is frame-independent");
            Equal(largeFrames.Plants[i].Timing.Value.ShootingCountdown, smallFrames.Plants[i].Timing.Value.ShootingCountdown,
                "PlantTiming shooting countdown is frame-independent");
        }

        GameSimulation zeroDelay = new GameSimulation(ExperimentalRules(new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0001, BetweenWaveDelaySeconds = 0.0,
            CompletionCondition = g => false
        }));
        ReachOnslaught(zeroDelay);
        for (int wave = 1; wave <= 4; wave++)
        {
            zeroDelay.Update(0.01f);
            Equal(wave, zeroDelay.LevelOneWavesDispatched, "zero delay dispatches at most one batch per tick");
        }

        GameSimulation badDelay = new GameSimulation(ExperimentalRules(new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0001, BetweenWaveDelay = (g, wave) => double.PositiveInfinity
        }));
        ReachOnslaught(badDelay);
        Throws<InvalidOperationException>(() => badDelay.Update(0.01f), "invalid dynamic delay");

        int factoryCalls = 0;
        GameRules badSunRules = ExperimentalRules(new ExperimentalLevelOneOptions {
            SecondsPerNativeTick = 0.0001,
            SkySunFactory = (g, rng) => { factoryCalls++; return null; }
        });
        badSunRules.SkySunInterval = 1f;
        GameSimulation badSun = new GameSimulation(badSunRules);
        badSun.Update(0.01f);
        Check(badSun.TrySelectLevelOneSeed(PlantKind.Peashooter) && badSun.TryPlant(PlantKind.Peashooter, 2, 0),
            "invalid sun factory setup");
        Throws<InvalidOperationException>(() => badSun.Update(0.02f), "invalid sky-sun factory result");
        Equal(1, factoryCalls, "invalid sun factory invoked once");
    }

    public static int Run()
    {
        DefaultModeIsUnchanged();
        TeachingSelectionAndWaves();
        SkySunAndRemovalOptions();
        EndToEndAndOptions();
        FixedStepsAndCallbackBoundaries();
        return assertions;
    }
}
