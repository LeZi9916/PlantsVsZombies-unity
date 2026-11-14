#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using System;
using System.Reflection;

/// <summary>Checks that existing projectiles finish their native motion while the completed board remains visible.</summary>
internal static class RewardProjectileRegressionTests
{
    /// <summary>Runs reward-stage motion, cleanup, timing, and combat-boundary checks.</summary>
    public static void Run()
    {
        RewardMotion();
        RewardCombatBoundary();
        RewardTimingBoundary();
        IndependentModeMotion();
        Console.WriteLine("PASS: completed-board projectile motion, retirement, and combat boundaries.");
    }

    /// <summary>Fails the focused regression when an expected behavior does not hold.</summary>
    /// <param name="condition">Whether the expected behavior was observed.</param>
    /// <param name="message">The behavior being checked.</param>
    /// <exception cref="InvalidOperationException">The assertion fails.</exception>
    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Creates an explicitly isolated completed adventure board without resuming combat.</summary>
    /// <returns>A reward-stage board with its original fixed-step presentation clock.</returns>
    private static GameSimulation Fixture()
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7529);
        Check(game.ChooseDefaultAdventureSeeds(), "Fixture chooses an earned seed bank.");
        Complete(game);
        game.Adventure.RewardPresentation = new AdventureRewardPresentation(15, 400f, 300f, 91, 78, .5f, .5f, 0);
        return game;
    }

    /// <summary>Models the state transition already committed by the production victory path.</summary>
    /// <param name="game">The isolated simulation whose combat has completed.</param>
    private static void Complete(GameSimulation game)
    {
        typeof(GameSimulation).GetProperty(nameof(GameSimulation.State))!.SetValue(game, MatchState.Won);
        game.Adventure.Phase = AdventurePhase.Reward;
    }

    /// <summary>Checks straight, diagonal, reverse, and lobbed motion along with normal range cleanup.</summary>
    private static void RewardMotion()
    {
        var game = Fixture();
        var pea = new Pea { X = 1f, StartX = 1f, Row = 2, FlightRow = 2f };
        var reverse = new Pea { X = 5f, StartX = 5f, Row = 2, FlightRow = 2f, VelocityX = -4.5f };
        var star = new Pea { X = 3f, StartX = 3f, Row = 2, FlightRow = 2f, VelocityX = 2f, VelocityY = 1f };
        var lob = new Pea
        {
            X = 3f,
            StartX = 3f,
            Row = 2,
            FlightRow = 2f,
            VelocityX = 3f,
            Lobbed = true,
            PresentationMotionInitialized = true,
            PresentationLobX = 3f,
            PresentationLobInitialVelocity = -7f,
            PresentationLobNativeX = 280f,
            PresentationLobNativeVelocityX = 2.4f
        };
        game.Peas.Add(pea);
        game.Peas.Add(reverse);
        game.Peas.Add(star);
        game.Peas.Add(lob);
        game.Update(.1f);
        Check(Math.Abs(pea.X - 1.45f) < .0001f, "Straight peas continue moving after the reward drops.");
        Check(Math.Abs(reverse.X - 4.55f) < .0001f, "Backward peas retain their velocity after completion.");
        Check(Math.Abs(star.FlightRow - 2.1f) < .0001f, "Diagonal projectiles continue vertical motion after completion.");
        Check(lob.X > 3f && lob.PresentationLobHeight < 0f && lob.PresentationNativeAge > .09f,
            "Lobbed projectiles continue their existing flight arc instead of freezing.");
        Check(pea.PresentationAge > .09f, "Projectile animation and flight clocks advance together.");
        game.Update(5f);
        Check(game.Peas.Count == 0, "Completed projectiles return to the existing pool at normal board and range bounds.");
        Check(game.State == MatchState.Won && game.Adventure.Phase == AdventurePhase.Reward,
            "Projectile retirement preserves the committed reward state.");
    }

    /// <summary>Protects the completed reward from new damage, loot, combat events, waves, and plant firing.</summary>
    private static void RewardCombatBoundary()
    {
        var game = Fixture();
        var zombie = new Zombie { Id = 91, Kind = ZombieKind.Normal, Row = 2, X = 2f, Health = 20f };
        game.Zombies.Add(zombie);
        game.Plants.Add(new Plant { Id = 1, Kind = PlantKind.Peashooter, Row = 2, Column = 0, ActionTimer = 0f });
        var pea = new Pea { X = 1.8f, StartX = 1.8f, Row = 2, FlightRow = 2f };
        game.Peas.Add(pea);
        var combatEvents = 0;
        game.EventRaised += item =>
        {
            if (item == GameEvent.ZombieHit || item == GameEvent.ZombieDied || item == GameEvent.Won)
            {
                combatEvents++;
            }
        };
        var waves = game.Adventure.TotalWavesSpawned;
        var spawned = game.SpawnedZombies;
        var coins = game.Adventure.Coins.Count;
        var sun = game.SunAmount;
        var reward = game.Adventure.RewardPresentation;
        game.Update(.1f);
        Check(pea.X > zombie.X && zombie.Health == 20f, "Post-victory flight cannot apply another combat hit.");
        Check(combatEvents == 0 && pea.HitIds.Count == 0, "Post-victory projectiles do not emit combat or repeated victory events.");
        Check(game.Peas.Count == 1 && game.SpawnedZombies == spawned && game.Adventure.TotalWavesSpawned == waves,
            "Decorative ticks do not restart shooters or spawn additional waves.");
        Check(game.Adventure.Coins.Count == coins && game.SunAmount == sun && ReferenceEquals(reward, game.Adventure.RewardPresentation),
            "Decorative projectile flight cannot grant loot or replace the committed reward.");
    }

    /// <summary>Uses native ten-millisecond ticks, rejects invalid deltas, and stops with the hidden board at introduction.</summary>
    private static void RewardTimingBoundary()
    {
        var game = Fixture();
        var pea = new Pea { X = 1f, StartX = 1f, Row = 2, FlightRow = 2f };
        game.Peas.Add(pea);
        game.Update(0f);
        game.Update(-1f);
        game.Update(float.NaN);
        game.Update(float.PositiveInfinity);
        Check(pea.X == 1f, "Invalid or empty reward deltas cannot move projectiles.");
        game.Update(.004f);
        Check(pea.X == 1f, "Reward motion waits for a complete native tick.");
        game.Update(.006f);
        Check(Math.Abs(pea.X - 1.045f) < .0001f, "Split reward deltas combine into exactly one native tick.");
        var reward = game.Adventure.RewardPresentation;
        Check(reward.BeginCollection(), "Fixture collects the dropped reward.");
        game.Update(5.1f);
        Check(reward.Stage == AdventureAwardStage.Introduction, "Reward presentation reaches the original introduction phase.");
        pea = new Pea { X = 1f, StartX = 1f, Row = 2, FlightRow = 2f };
        game.Peas.Add(pea);
        game.Update(.1f);
        Check(pea.X == 1f, "Hidden-board projectiles stop after the reward introduction replaces the lawn.");
    }

    /// <summary>Advances existing projectiles through the independent mode's production UpdateMode entry point.</summary>
    private static void IndependentModeMotion()
    {
        var session = new ModeSession(1, randomSeed: 7529);
        var game = session.Game;
        Complete(game);
        session.Phase = ModePhase.Won;
        var pea = new Pea { X = 1f, StartX = 1f, Row = 2, FlightRow = 2f };
        game.Peas.Add(pea);
        session.Update(.1f);
        Check(Math.Abs(pea.X - 1.45f) < .0001f, "Independent-mode wins preserve existing projectile movement.");
        Check(session.Phase == ModePhase.Won && game.State == MatchState.Won,
            "Independent-mode decorative ticks preserve the completed mode phase.");
    }
}
