#nullable enable

using System;
using System.Reflection;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

/// <summary>Checks original PC Starfruit targeting and scheduled five-star firing rules.</summary>
internal static class StarfruitRegressionTests
{
    /// <summary>Runs the focused original PC Starfruit checks.</summary>
    public static void Run()
    {
        TargetPaths();
        ScheduledShot();
        StarVectors();
        ProductionIntegration();
        Console.WriteLine("PASS: PC Starfruit target paths, 40-count pending attack, 39th-tick burst, and five native vectors.");
    }

    /// <summary>Fails when the checked original PC behavior is absent.</summary>
    /// <param name="condition">Whether the expected original behavior holds.</param>
    /// <param name="message">The behavior under test.</param>
    /// <exception cref="InvalidOperationException">The behavior does not hold.</exception>
    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Checks the original same-row reverse path, vertical crossing, diagonal windows, and target gates.</summary>
    private static void TargetPaths()
    {
        var plant = new Plant { Kind = PlantKind.Starfruit, Row = 2, Column = 4 };
        var zombie = new Zombie { Kind = ZombieKind.Normal, Row = 2, Health = 270 };
        Check(AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(350, 280, 42, 115), 280, 0f, true),
            "A same-row zombie wholly left of Starfruit enters its reverse star path.");
        Check(!AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(358, 280, 42, 115), 280, 0f, true),
            "A same-row body touching the firing center is not wholly behind Starfruit.");
        Check(!AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(500, 280, 42, 115), 280, 0f, true),
            "A same-row zombie ahead of Starfruit cannot trigger its five-star burst.");
        zombie.Row = 1;
        Check(AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(390, 180, 42, 115), 280, 0f, true),
            "An adjacent-row body crossing Starfruit's vertical line starts an attack.");
        Check(AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(500, 180, 42, 115), 280, 0f, true),
            "An adjacent-row body inside the strict diagonal angle window starts an attack.");
        Check(!AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(500, 180, 42, 115), 280, 2f, true),
            "The native target lead can move a walking body outside the diagonal angle window.");
        Check(!AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(700, 180, 42, 115), 280, 0f, true),
            "An adjacent-row body outside both diagonal and vertical paths is ignored.");
        Check(!AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(500, 180, 42, 115), 280, 0f, false),
            "The native EffectedByDamage gate excludes ineligible zombies.");
        zombie.Health = 0;
        Check(!AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(500, 180, 42, 115), 280, 0f, true),
            "Dead zombies cannot start a Starfruit attack.");
        zombie.Kind = ZombieKind.Boss;
        zombie.Row = 5;
        zombie.Health = 270;
        plant.Column = 5;
        Check(AdventureStarfruitRules.CanTarget(plant, zombie, new NativeRect(800, 480, 42, 115), 280, 0f, true),
            "A boss can trigger a Starfruit in original plant column five or later.");
    }

    /// <summary>Checks native body animation scheduling and target-independent firing on the thirty-ninth later tick.</summary>
    private static void ScheduledShot()
    {
        Check(AdventureStarfruitRules.StartAttack(false) == 0, "No target leaves Starfruit idle.");
        Check(AdventureStarfruitRules.StartAttack(false, true) == 40,
            "The original recently eaten counter can start an attack without another target.");
        var countdown = AdventureStarfruitRules.StartAttack(true);
        Check(countdown == 40, "A target starts the original 40-count body shooting animation.");
        for (var tick = 0; tick < 38; tick++)
        {
            Check(!AdventureStarfruitRules.AdvanceAttack(ref countdown, true),
                "The pending attack cannot launch before its thirty-ninth later tick.");
        }

        Check(countdown == 2, "The shooting counter is two immediately before launch.");
        Check(AdventureStarfruitRules.AdvanceAttack(ref countdown, true) && countdown == 1,
            "The already scheduled burst fires when the counter reaches one, without reacquiring its target.");
        Check(!AdventureStarfruitRules.AdvanceAttack(ref countdown, true) && countdown == 0,
            "The completed burst cannot fire again on the next tick.");
    }

    /// <summary>Checks original star spawn and all five velocity directions from native 0x46DA80.</summary>
    private static void StarVectors()
    {
        Check(AdventureStarfruitRules.LaunchX(4) == 385 && AdventureStarfruitRules.LaunchY(280) == 305,
            "All five stars originate at plant plus 25 pixels in each axis.");
        var vx = new[] { -3.33f, 0f, 0f, 2.883864f, 2.883864f };
        var vy = new[] { 0f, 3.33f, -3.33f, 1.665f, -1.665f };
        for (var index = 0; index < 5; index++)
        {
            Check(Math.Abs(AdventureStarfruitRules.VelocityX(index) - vx[index]) < .00001f
                && Math.Abs(AdventureStarfruitRules.VelocityY(index) - vy[index]) < .00001f,
                "Native Starfruit vector differs for projectile " + index + ".");
        }
    }

    /// <summary>Exercises target gates and the simultaneous native attack counters in the simulation.</summary>
    private static void ProductionIntegration()
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 46012);
        var plant = new Plant { Kind = PlantKind.Starfruit, Row = 2, Column = 4, StarfruitLaunchCountdown = 0 };
        game.Plants.Add(plant);
        var target = new Zombie { Kind = ZombieKind.Normal, Row = 2, X = 3f, Health = 270, StartingHealth = 270 };
        game.Zombies.Add(target);
        var tick = typeof(GameSimulation).GetMethod("AdvanceAdventurePlants", BindingFlags.Instance | BindingFlags.NonPublic)!;
        target.Hypnotized = true;
        tick.Invoke(game, new object[] { .01f });
        Check(plant.StarfruitShootingCountdown == 0 && plant.PresentationShotAge < 0f, "Mind-controlled bodies do not start a Starfruit attack.");
        target.Hypnotized = false;
        target.Submerged = true;
        plant.StarfruitLaunchCountdown = 0;
        tick.Invoke(game, new object[] { .01f });
        Check(plant.StarfruitShootingCountdown == 0, "Submerged bodies do not start a Starfruit attack.");
        target.Submerged = false;
        target.Airborne = true;
        plant.StarfruitLaunchCountdown = 0;
        tick.Invoke(game, new object[] { .01f });
        Check(plant.StarfruitShootingCountdown == 0, "Airborne bodies do not start a Starfruit attack.");
        target.Airborne = false;
        plant.StarfruitLaunchCountdown = 0;
        tick.Invoke(game, new object[] { .01f });
        Check(plant.StarfruitShootingCountdown == 40 && plant.PresentationShotAge == 0f && game.Peas.Count == 0,
            "A valid reverse target starts the native body animation without an immediate burst.");
        var launchCounter = plant.StarfruitLaunchCountdown;
        game.Zombies.Clear();
        for (var i = 0; i < 38; i++)
        {
            tick.Invoke(game, new object[] { .01f });
            Check(game.Peas.Count == 0, "Starfruit must not fire before the thirty-ninth later tick.");
        }

        Check(plant.StarfruitLaunchCountdown == launchCounter - 38, "The launch counter runs while the shooting animation is pending.");
        tick.Invoke(game, new object[] { .01f });
        Check(game.Peas.Count == 5 && plant.StarfruitShootingCountdown == 1,
            "A scheduled burst fires after target loss when the shooting counter reaches one.");
        Check(Math.Abs(game.Peas[1].VelocityY - 3.33f) < .0001f && Math.Abs(game.Peas[0].X - 4.3125f) < .0001f,
            "The production burst uses the native launch x and vertical pixel velocity on a 100-pixel row.");

        var bitten = new Plant { Kind = PlantKind.Starfruit, Row = 3, Column = 4, StarfruitLaunchCountdown = 0, RecentlyEatenTicks = 50 };
        game.Plants.Clear();
        game.Plants.Add(bitten);
        tick.Invoke(game, new object[] { .01f });
        Check(bitten.StarfruitShootingCountdown == 40, "A recently chewed Starfruit may begin an attack without a target.");

        var pool = new GameSimulation(new GameRules { AdventureLevel = 39 }, 46012);
        var poolPlant = new Plant { Kind = PlantKind.Starfruit, Row = 2, Column = 4 };
        var fire = typeof(GameSimulation).GetMethod("FireAdventureStarfruit", BindingFlags.Instance | BindingFlags.NonPublic)!;
        fire.Invoke(pool, new object[] { poolPlant });
        Check(Math.Abs(pool.Peas[1].VelocityY * 85f / 100f - 3.33f) < .0001f,
            "Pool rows use their 85-pixel pitch while retaining the original vertical pixel velocity.");
    }
}
