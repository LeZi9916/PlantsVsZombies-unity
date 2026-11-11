#nullable enable

using System;
using System.Reflection;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Timing;

/// <summary>Checks original PC Fume-shroom targeting, timing, and instant row damage.</summary>
internal static class FumeShroomRegressionTests
{
    /// <summary>Runs the focused Fume-shroom regressions.</summary>
    public static void Run()
    {
        TargetRectangle();
        TargetAcquisition();
        IdleWithoutTarget();
        PublicPlantingWaitsForRange();
        ScheduledRowAttack();
        PendingAttackUsesCurrentTargets();
        Console.WriteLine("PASS: PC Fume-shroom 340-pixel targeting, 49-tick instant row damage, shield piercing, and target-change regressions.");
    }

    /// <summary>Fails the focused regression when original behavior is not preserved.</summary>
    /// <param name="condition">Whether the expected original behavior holds.</param>
    /// <param name="message">The behavior under test.</param>
    /// <exception cref="InvalidOperationException">The checked behavior does not hold.</exception>
    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Invokes a private simulation hook without widening production visibility.</summary>
    /// <param name="game">The isolated simulation.</param>
    /// <param name="method">The private hook to invoke.</param>
    /// <param name="arguments">The hook's arguments.</param>
    /// <returns>The hook's result.</returns>
    private static object? Invoke(GameSimulation game, string method, params object[] arguments)
    {
        return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
    }

    /// <summary>Creates an isolated night board with no imminent automatic wave.</summary>
    /// <returns>The controlled board fixture.</returns>
    private static GameSimulation Fixture()
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
        Check(game.ChooseDefaultAdventureSeeds(), "Fume-shroom fixture chooses its earned night bank.");
        game.ResetAdventureWaveSchedule(100000, false, false);
        return game;
    }

    /// <summary>Places an awake mushroom with an immediate first attack attempt.</summary>
    /// <param name="game">The controlled night board.</param>
    /// <returns>The mushroom in row zero, column one.</returns>
    private static Plant PlaceMushroom(GameSimulation game)
    {
        var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantsVsZombies.Simulation.PlantKind.FumeShroom, 0, 1)!;
        plant.Timing = PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.FumeShroom, 0, 0);
        plant.ActionTimer = 0;
        plant.Asleep = false;
        return plant;
    }

    /// <summary>Creates a zombie at an explicit original body-left coordinate.</summary>
    /// <param name="bodyLeft">The native collision body's left edge.</param>
    /// <param name="kind">The zombie species.</param>
    /// <returns>The zombie fixture.</returns>
    private static Zombie AtBodyLeft(int bodyLeft, ZombieKind kind = ZombieKind.Normal)
    {
        return new Zombie
        {
            Id = bodyLeft,
            Kind = kind,
            Row = 0,
            X = AdventureZombieNativeRules.GridX(bodyLeft - 36 + .01f),
            Health = 270,
            StartingHealth = 270,
            ShieldHealth = kind == ZombieKind.ScreenDoor ? 1100 : 0
        };
    }

    /// <summary>Checks inclusive aiming edges and strictly positive damage overlap.</summary>
    private static void TargetRectangle()
    {
        var plant = new Plant { Kind = PlantsVsZombies.Simulation.PlantKind.FumeShroom, Row = 0, Column = 1 };
        var attack = AdventureFumeShroomRules.AttackRectangle(plant.Column);
        Check(attack.X == 180 && attack.Width == 340, "PC Fume-shroom attack rectangle starts at plant+60 and spans 340 pixels.");
        var nearTouch = AtBodyLeft(138);
        var farTouch = AtBodyLeft(520);
        Check(AdventureFumeShroomRules.CanTarget(plant, nearTouch) && AdventureFumeShroomRules.CanTarget(plant, farTouch),
            "A zombie body touching either original rectangle edge can start the attack.");
        Check(!AdventureFumeShroomRules.CanDamage(plant, nearTouch) && !AdventureFumeShroomRules.CanDamage(plant, farTouch),
            "A body touching only the rectangle edge receives no area damage.");
        Check(!AdventureFumeShroomRules.CanTarget(plant, AtBodyLeft(137)) && !AdventureFumeShroomRules.CanTarget(plant, AtBodyLeft(521)),
            "A body one pixel beyond either rectangle edge cannot start the attack.");
        Check(AdventureFumeShroomRules.CanDamage(plant, AtBodyLeft(139)) && AdventureFumeShroomRules.CanDamage(plant, AtBodyLeft(519)),
            "A one-pixel overlap on either edge receives area damage.");
        var otherRow = AtBodyLeft(300);
        otherRow.Row = 1;
        Check(!AdventureFumeShroomRules.CanTarget(plant, otherRow) && !AdventureFumeShroomRules.CanDamage(plant, otherRow),
            "Fume-shroom cannot aim at or damage another row.");
    }

    /// <summary>Checks the production attack branch acquires a far-edge touch but rejects the next pixel.</summary>
    private static void TargetAcquisition()
    {
        // A zombie occupying the planted column can still be completely behind
        // the original mX + 60 attack rectangle; that placement must not start
        // the body shooting animation once.
        foreach (var bodyLeft in new[] { 120, 520, 521 })
        {
            var game = Fixture();
            var plant = PlaceMushroom(game);
            game.Zombies.Add(AtBodyLeft(bodyLeft));
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Check((plant.PresentationShotAge == 0f) == (bodyLeft == 520),
                "Production Fume-shroom aiming differs at body-left " + bodyLeft + ".");
            Check(game.Peas.Count == 0, "A far-edge aim cannot create a projectile.");
        }
    }

    /// <summary>Checks that an empty attack rectangle never starts the body animation.</summary>
    private static void IdleWithoutTarget()
    {
        var game = Fixture();
        var plant = PlaceMushroom(game);
        for (var tick = 0; tick < 160; tick++)
        {
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Check(plant.PresentationShotAge < 0f && !plant._presentationPrefiring && game.Peas.Count == 0,
                "Fume-shroom must remain idle with no target at native tick " + tick + ".");
        }
    }

    /// <summary>Checks public planting stays idle across repeated attempts until a frozen zombie enters range.</summary>
    private static void PublicPlantingWaitsForRange()
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 7529);
        Check(game.ChooseAdventureSeeds(new[]
        {
            PlantsVsZombies.Simulation.PlantKind.FumeShroom,
            PlantsVsZombies.Simulation.PlantKind.GraveBuster,
            PlantsVsZombies.Simulation.PlantKind.PuffShroom,
            PlantsVsZombies.Simulation.PlantKind.IceShroom,
            PlantsVsZombies.Simulation.PlantKind.HypnoShroom,
            PlantsVsZombies.Simulation.PlantKind.SunShroom
        }), "The public Fume-shroom fixture chooses a legal earned deck.");
        game.ResetAdventureWaveSchedule(100000, false, false);
        typeof(GameSimulation).GetProperty(nameof(GameSimulation.SunAmount))!.SetValue(game, 1000);
        var zombie = AtBodyLeft(600);
        zombie.FrozenRemaining = 1000f;
        game.Zombies.Add(zombie);
        Check(game.TryPlant(PlantsVsZombies.Simulation.PlantKind.FumeShroom, 0, 1),
            "Public input plants the earned Fume-shroom while a distant zombie is present.");
        var plant = game.AdventurePlantAt(0, 1, PlantLayer.Main)!;
        for (var tick = 0; tick < 400; tick++)
        {
            game.Update(.01f);
            Check(plant.PresentationShotAge < 0f && !plant._presentationPrefiring && zombie.Health == 270 && game.Peas.Count == 0,
                "A far same-row zombie cannot start Fume-shroom's attack across repeated native attempts at tick " + tick + ".");
        }

        zombie.X = AdventureZombieNativeRules.GridX(400 - 36 + .01f);
        var started = false;
        for (var tick = 0; tick < 160; tick++)
        {
            game.Update(.01f);
            if (plant.PresentationShotAge >= 0f)
            {
                started = true;
                break;
            }
        }

        Check(started && plant.Timing!.Value.ShootingCountdown == 50 && zombie.Health == 270 && game.Peas.Count == 0,
            "The same publicly planted Fume-shroom begins its complete body windup after the zombie enters range.");
    }

    /// <summary>Checks one scheduled strike hits every overlapping body through shields on the forty-ninth later tick.</summary>
    private static void ScheduledRowAttack()
    {
        var game = Fixture();
        var plant = PlaceMushroom(game);
        var front = AtBodyLeft(300, ZombieKind.ScreenDoor);
        var rear = AtBodyLeft(400);
        var edge = AtBodyLeft(520);
        game.Zombies.Add(front);
        game.Zombies.Add(rear);
        game.Zombies.Add(edge);
        Invoke(game, "AdvanceAdventurePlants", .01f);
        Check(plant.PresentationShotAge == 0f && plant.Timing!.Value.ShootingCountdown == 50,
            "A target starts the body animation and original 50-count shooting counter.");
        for (var tick = 0; tick < 48; tick++)
        {
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Check(front.Health == 270 && rear.Health == 270 && edge.Health == 270 && game.Peas.Count == 0,
                "The scheduled Fume-shroom attack cannot damage or create a projectile early.");
        }

        Invoke(game, "AdvanceAdventurePlants", .01f);
        Check(plant.Timing!.Value.ShootingCountdown == 1, "The damage tick leaves the original shooting counter at one.");
        Check(front.Health == 250 && front.ShieldHealth == 1080 && rear.Health == 250,
            "The instant row attack hits multiple bodies for 20 damage and also damages the pierced screen door.");
        Check(edge.Health == 270, "The far edge touch can aim but cannot receive zero-overlap area damage.");
        Check(game.Peas.Count == 0, "Fume-shroom area damage does not create a Pea projectile.");
    }

    /// <summary>Checks firing uses the zombies present when an already scheduled attack resolves.</summary>
    private static void PendingAttackUsesCurrentTargets()
    {
        var game = Fixture();
        var plant = PlaceMushroom(game);
        var original = AtBodyLeft(300);
        game.Zombies.Add(original);
        Invoke(game, "AdvanceAdventurePlants", .01f);
        Check(plant.Timing!.Value.ShootingCountdown == 50, "An original target schedules the first attack.");
        game.Zombies.Remove(original);
        for (var tick = 0; tick < 48; tick++)
        {
            Invoke(game, "AdvanceAdventurePlants", .01f);
        }

        var replacement = AtBodyLeft(400);
        game.Zombies.Add(replacement);
        Invoke(game, "AdvanceAdventurePlants", .01f);
        Check(original.Health == 270 && replacement.Health == 250 && game.Peas.Count == 0,
            "A pending attack completes against the current overlapping target without reacquiring its original target.");
    }
}
