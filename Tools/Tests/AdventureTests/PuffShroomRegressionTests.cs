#nullable enable

using System;
using System.Reflection;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

/// <summary>Checks the original PC Puff-shroom attack interval, muzzle, collision box, and lifetime.</summary>
internal static class PuffShroomRegressionTests
{
    /// <summary>Runs the isolated short-range mushroom regressions.</summary>
    public static void Run()
    {
        TargetRectangle();
        TargetAcquisition();
        PlantedOutsideRangeRemainsIdle();
        LaunchAndLifetime();
        BodyCollisionAndTorchwood();
        Console.WriteLine("PASS: PC Puff-shroom body targeting, native muzzle, 75-tick life, and spore collision regressions.");
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
    /// <returns>The result returned by the hook.</returns>
    private static object? Invoke(GameSimulation game, string method, params object[] arguments)
    {
        return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
    }

    /// <summary>Creates an explicitly isolated night board with no imminent automatic wave.</summary>
    /// <returns>The controlled board fixture.</returns>
    private static GameSimulation Fixture()
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
        Check(game.ChooseDefaultAdventureSeeds(), "Puff-shroom fixture chooses its earned night bank.");
        game.ResetAdventureWaveSchedule(100000, false, false);
        return game;
    }

    /// <summary>Places an awake test mushroom without altering unrelated battle state.</summary>
    /// <param name="game">The isolated night board.</param>
    /// <returns>The placed mushroom in row zero, column one.</returns>
    private static Plant PlaceMushroom(GameSimulation game)
    {
        var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.PuffShroom, 0, 1)!;
        plant.ActionTimer = 0;
        plant.Asleep = false;
        return plant;
    }

    /// <summary>Creates a normal zombie at an explicit original body-left coordinate.</summary>
    /// <param name="bodyLeft">The native collision body's left edge.</param>
    /// <returns>The zombie fixture whose visual origin is intentionally separate from its hitbox.</returns>
    private static Zombie AtBodyLeft(int bodyLeft)
    {
        return new Zombie
        {
            Id = 7,
            Kind = ZombieKind.Normal,
            Row = 0,
            X = AdventureZombieNativeRules.GridX(bodyLeft - 36 + .01f),
            Health = 270,
            StartingHealth = 270
        };
    }

    /// <summary>Checks inclusive native body edges, range limits, and combat state filtering.</summary>
    private static void TargetRectangle()
    {
        var plant = new Plant { Kind = PlantKind.PuffShroom, Row = 0, Column = 1 };
        var attack = AdventurePuffShroomRules.AttackRectangle(plant.Column);
        Check(attack.X == 180 && attack.Width == 230, "PC Puff-shroom attack interval is plant+(60,0), width230.");
        Check(AdventurePuffShroomRules.CanTarget(plant, AtBodyLeft(410)), "Original far edge touching the body must be targetable.");
        Check(!AdventurePuffShroomRules.CanTarget(plant, AtBodyLeft(411)), "Body one pixel beyond the native far edge must not start a shot.");
        Check(AdventurePuffShroomRules.CanTarget(plant, AtBodyLeft(138)), "Body right edge touching the native near edge remains targetable.");
        Check(!AdventurePuffShroomRules.CanTarget(plant, AtBodyLeft(137)), "A body wholly behind the native near edge is not targetable.");
        var zombie = AtBodyLeft(300);
        zombie.Headless = true;
        Check(AdventurePuffShroomRules.CanTarget(plant, zombie), "Puff-shroom may continue shooting a live headless zombie.");
        zombie.Airborne = true;
        Check(!AdventurePuffShroomRules.CanTarget(plant, zombie), "Puff-shroom does not target airborne zombies.");
    }

    /// <summary>Checks the production shooting branch acquires the original far-edge target and rejects the next pixel.</summary>
    private static void TargetAcquisition()
    {
        foreach (var bodyLeft in new[] { 410, 411 })
        {
            var game = Fixture();
            PlaceMushroom(game);
            game.Zombies.Add(AtBodyLeft(bodyLeft));
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Check(game.Peas.Count == (bodyLeft == 410 ? 1 : 0), "Production Puff-shroom range differs at body-left " + bodyLeft + ".");
            if (game.Peas.Count != 0)
            {
                Check(game.Peas[0].Range == float.MaxValue, "Puff lifetime must not be cut short by the old three-column center-distance limit.");
            }
        }
    }

    /// <summary>Checks real planting with an existing far zombie never starts a false attack animation.</summary>
    private static void PlantedOutsideRangeRemainsIdle()
    {
        var game = Fixture();
        var zombie = AtBodyLeft(500);
        zombie.FrozenRemaining = 1000f;
        game.Zombies.Add(zombie);
        Check(game.TryPlant(PlantKind.PuffShroom, 0, 1), "Public input plants the earned Puff-shroom beside an existing far zombie.");
        var plant = game.AdventurePlantAt(0, 1, PlantLayer.Main)!;
        Check(plant.PresentationShotAge < 0f && plant.ShootAge < 0f,
            "A newly planted out-of-range Puff-shroom begins idle.");
        for (var tick = 0; tick < 100; tick++)
        {
            game.Update(.01f);
            Check(plant.PresentationShotAge < 0f && plant.ShootAge < 0f && !plant._presentationPrefiring && game.Peas.Count == 0,
                "An out-of-range zombie must not start Puff-shroom's shooting animation at native tick " + tick + ".");
        }

        zombie.X = AdventureZombieNativeRules.GridX(410 - 36 + .01f);
        game.Update(.01f);
        Check(plant.PresentationShotAge >= 0f && game.Peas.Count > 0,
            "Entering the real native attack rectangle still permits the production attack.");
    }

    /// <summary>Checks real firing coordinates, native velocity, image growth, and exact retirement update.</summary>
    private static void LaunchAndLifetime()
    {
        var game = Fixture();
        var plant = PlaceMushroom(game);
        var shot = (Pea)Invoke(game, "FireAdventurePea", plant, plant.Row, false, 20f)!;
        shot.Range = float.MaxValue;
        Check(Math.Abs(shot.X - 1.7f) < .00001f && Math.Abs(shot.PresentationLaunchY - 133.5f) < .00001f,
            "Puff image center must be original plant+(56,53.5), not the generic pea muzzle.");
        Check(Math.Abs(shot.VelocityX - 4.1625f) < .00001f, "Puff motion must retain PC 3.33 pixels per native tick.");
        Check(Math.Abs(AdventurePuffShroomRules.ImageScale(.15f) - .65f) < .00001f,
            "Puff image grows from .3 to1 over the original first thirty ticks.");
        Check(Math.Abs(AdventurePuffShroomRules.LaunchCenterY(PlantKind.PuffShroom, 100f, true) - 148.5f) < .00001f,
            "Flower-pot support subtracts the original five pixels from the spore muzzle.");
        for (var tick = 0; tick < 74; tick++)
        {
            Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
        }

        Check(game.Peas.Contains(shot), "Native short puff must survive its seventy-fourth update.");
        Check(Math.Abs(shot.X - (1.7f + 74f * 3.33f / 80f)) < .0001f, "Spore travel must use the native pixel velocity.");
        Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
        Check(!game.Peas.Contains(shot), "Native short puff must disappear before collision on update seventy-five.");
    }

    /// <summary>Checks spores use the native body hitbox and cannot turn into torchwood fire peas.</summary>
    private static void BodyCollisionAndTorchwood()
    {
        var game = Fixture();
        var plant = PlaceMushroom(game);
        var shot = (Pea)Invoke(game, "FireAdventurePea", plant, plant.Row, false, 20f)!;
        shot.Range = float.MaxValue;
        var zombie = AtBodyLeft(200);
        game.Zombies.Add(zombie);
        Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
        Check(zombie.Health == 250 && !game.Peas.Contains(shot), "Puff collides using its original forty-pixel box rather than waiting for the actor origin.");

        game = Fixture();
        plant = PlaceMushroom(game);
        shot = (Pea)Invoke(game, "FireAdventurePea", plant, plant.Row, false, 20f)!;
        shot.X = shot.StartX = 2.5f;
        shot.Range = float.MaxValue;
        game.Plants.Add(new Plant { Kind = PlantKind.Torchwood, Row = 0, Column = 2 });
        Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
        Check(!shot.Fire && shot.Damage == 20, "A mushroom spore cannot be converted by torchwood into a fire pea.");
    }
}
