#nullable enable

using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using System;
using System.Linq;
using System.Reflection;

/// <summary>Verifies original PC money pickup motion, scoring, sound notifications, and save retries.</summary>
internal static class CoinFeedbackRegressionTests
{
    /// <summary>Runs the focused money pickup regressions.</summary>
    public static void Run()
    {
        foreach (var kind in new[] { AdventureCoinKind.Silver, AdventureCoinKind.Gold, AdventureCoinKind.Diamond })
        {
            Collection(kind);
        }

        SaveRetry();
        CompletedBoardCollection();
        Console.WriteLine("PASS: original money collection flight, scoring thresholds, sound notices, and save retries.");
    }

    /// <summary>Checks one of the three original currency pickup types.</summary>
    /// <param name="kind">The currency whose collection is exercised.</param>
    private static void Collection(AdventureCoinKind kind)
    {
        var game = Fixture();
        var coin = game.SpawnAdventureCoin(kind, 5f, 2f);
        var started = 0;
        var scored = 0;
        game.Adventure.NoticeRaised += notice =>
        {
            if (notice.Kind == AdventureNoticeKind.CoinCollectionStarted)
            {
                Check(notice.Value == (int)kind, "The pickup sound notification identifies the original currency type.");
                started++;
            }

            if (notice.Kind == AdventureNoticeKind.CoinCollected)
            {
                scored++;
            }
        };

        var x = coin.PresentationX;
        var y = coin.PresentationY;
        Check(game.CollectAdventureCoin(coin), "The first click starts collection.");
        Check(coin.IsBeingCollected && game.Adventure.Coins.Contains(coin) && game.Adventure.MoneyCollectedUnits == 0,
            "The pickup remains visible and unscored when collection starts.");
        Check(!game.CollectAdventureCoin(coin) && started == 1, "Repeated clicks neither collect twice nor repeat the sound.");
        game.Update(.01f);
        Check(Math.Abs(coin.PresentationX - (float)(x + (39.0 - x) / 21.0)) < .0001f
            && Math.Abs(coin.PresentationY - (float)(y + (558.0 - y) / 21.0)) < .0001f,
            "A native tick moves both coordinates one twenty-first of their remaining distance towards (39,558).");
        Check(coin.PresentationPreviousX == x && coin.PresentationPreviousY == y,
            "Rendering retains the previous position for fixed-step interpolation.");
        game.TogglePause();
        var pausedX = coin.PresentationX;
        game.Update(.1f);
        Check(coin.PresentationX == pausedX, "Pausing freezes collection flight.");
        game.TogglePause();

        var sawShrink = false;
        for (var tick = 0; tick < 150 && game.Adventure.Coins.Contains(coin); tick++)
        {
            var dx = coin.PresentationX - 39f;
            var dy = coin.PresentationY - 558f;
            var beforeDistance = (float)Math.Sqrt((float)((double)dx * dx + (double)dy * dy));
            game.Update(.01f);
            if (!game.Adventure.Coins.Contains(coin))
            {
                Check(beforeDistance < 12f, "Money is credited only inside the native pre-move twelve-pixel threshold.");
            }
            else
            {
                Check(beforeDistance >= 12f, "Money outside the native threshold continues flying.");
            }

            sawShrink |= coin.PresentationScale < 1f && coin.PresentationAlpha < 1f;
        }

        Check(sawShrink, "Collected money shrinks and fades as it approaches the wallet.");
        Check(!game.Adventure.Coins.Contains(coin) && game.Adventure.MoneyCollectedUnits == coin.ValueUnits && scored == 1,
            "The original silver/gold/diamond value is credited exactly once on arrival.");
        Check(!game.CollectAdventureCoin(coin) && started == 1, "A retired coin cannot replay its pickup sound.");
    }

    /// <summary>Checks that a failed arrival-time save preserves a retryable pickup.</summary>
    private static void SaveRetry()
    {
        var game = Fixture();
        var attempts = 0;
        game.Rules.AdventureCoinCommit = total =>
        {
            attempts++;
            return false;
        };
        var coin = game.SpawnAdventureCoin(AdventureCoinKind.Gold, 5f, 2f);
        var failures = 0;
        game.Adventure.NoticeRaised += notice =>
        {
            if (notice.Kind == AdventureNoticeKind.CoinCollectionFailed)
            {
                Check(notice.Value == coin.Id, "The host receives the identifier of the retryable pickup.");
                failures++;
            }
        };
        Check(game.CollectAdventureCoin(coin), "A manual pickup starts before its arrival-time save.");
        game.Update(2f);
        Check(attempts == 1 && failures == 1 && coin.PersistencePending && !coin.IsBeingCollected && game.Adventure.Coins.Contains(coin)
            && game.Adventure.MoneyCollectedUnits == 0, "A failed save keeps the uncredited pickup available without retrying every tick.");
        coin.Age = 35f;
        game.Update(.01f);
        Check(game.Adventure.Coins.Contains(coin), "An uncommitted pickup cannot expire.");
        game.Rules.AdventureCoinCommit = total => total == 5;
        Check(game.CollectAdventureCoin(coin), "The failed pickup can be clicked again.");
        game.Update(.01f);
        Check(!game.Adventure.Coins.Contains(coin) && game.Adventure.MoneyCollectedUnits == 5 && failures == 1,
            "A successful retry credits the pickup once without another failure notification.");
    }

    /// <summary>Checks that an already flying pickup continues while the completed lawn is still displayed.</summary>
    private static void CompletedBoardCollection()
    {
        var game = Fixture();
        var coin = game.SpawnAdventureCoin(AdventureCoinKind.Diamond, 5f, 2f);
        Check(game.CollectAdventureCoin(coin), "The completed-board fixture begins a normal pickup flight.");
        typeof(GameSimulation).GetProperty(nameof(GameSimulation.State), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(game, MatchState.Won);
        game.Adventure.Phase = AdventurePhase.Reward;
        game.Adventure.RewardPresentation = new AdventureRewardPresentation(15, 400f, 300f, 91, 78, .5f, .5f, 0);
        game.Update(1.5f);
        Check(game.Adventure.MoneyCollectedUnits == 100 && !game.Adventure.Coins.Contains(coin),
            "An existing collection flight finishes during the visible victory sequence.");
        Check(game.State == MatchState.Won && game.Adventure.Phase == AdventurePhase.Reward,
            "Finishing a pickup does not resume combat or change the committed reward state.");
    }

    /// <summary>Creates an isolated board with no injected plants, zombies, or progress mutation.</summary>
    /// <returns>A playing level eight simulation.</returns>
    private static GameSimulation Fixture()
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
        Check(game.ChooseDefaultAdventureSeeds(), "The fixture starts with an available original seed bank.");
        return game;
    }

    /// <summary>Fails the regression when expected original behavior is absent.</summary>
    /// <param name="condition">Whether the expected behavior was observed.</param>
    /// <param name="message">The behavior being checked.</param>
    /// <exception cref="InvalidOperationException">The expected behavior was not observed.</exception>
    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
