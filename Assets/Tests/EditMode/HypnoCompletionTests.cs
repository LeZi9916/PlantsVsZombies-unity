#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original last-hostile award path after an awake Hypno-shroom is eaten.</summary>
    public sealed class HypnoCompletionTests
    {
        /// <summary>Checks mind control awards the level at the converted zombie without death or ordinary loot.</summary>
        [Test]
        public void LastHostileChewDropsAdventureAwardAtTheConvertedZombie()
        {
            var game = AdventureFixture();
            var zombie = AddHypnoMeal(game);
            var deathEvents = 0;
            var rewardNotices = 0;
            game.EventRaised += item =>
            {
                if (item == GameEvent.ZombieDied)
                {
                    deathEvents++;
                }
            };
            game.Adventure.NoticeRaised = notice =>
            {
                if (notice.Kind == AdventureNoticeKind.RewardDropped)
                {
                    rewardNotices++;
                }
            };
            game.Adventure._lastRewardX = 123f;
            game.Adventure._lastRewardY = 456f;

            EatHypnoShroom(game, zombie);

            Assert.That(game.State, Is.EqualTo(MatchState.Won));
            Assert.That(game.Adventure.Phase, Is.EqualTo(AdventurePhase.Reward));
            Assert.That(game.Zombies, Is.Empty, "The original end-level cleanup removes converted survivors.");
            var reward = game.Adventure.RequiredRewardPresentation;
            var expectedX = (int)(40f + zombie.X * 80f - 57f) + 63f;
            var expectedY = 80f + zombie.Row * 100f - 30f + 57f;
            Assert.That(reward.X, Is.EqualTo(expectedX).Within(.001f));
            Assert.That(reward.Y, Is.EqualTo(expectedY).Within(.001f));
            Assert.That(deathEvents, Is.Zero, "Conversion and end-level cleanup do not kill the zombie.");
            Assert.That(game.Adventure.Coins, Is.Empty, "Conversion does not roll ordinary zombie loot.");
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(rewardNotices, Is.EqualTo(1));
            game.Update(.1f);
            Assert.That(game.Adventure.RewardPresentation, Is.SameAs(reward));
            Assert.That(rewardNotices, Is.EqualTo(1), "The completed board cannot spawn a second award.");
        }

        /// <summary>Checks another hostile zombie prevents an award after the mushroom is eaten.</summary>
        [Test]
        public void AnotherHostilePreventsAdventureCompletion()
        {
            var game = AdventureFixture();
            var zombie = AddHypnoMeal(game);
            var hostile = AddZombie(game, 1, 8f);

            EatHypnoShroom(game, zombie);

            AssertStillPlaying(game);
            Assert.That(game.Zombies, Does.Contain(zombie));
            Assert.That(game.Zombies, Does.Contain(hostile));
        }

        /// <summary>Checks conversion cannot bypass unspawned waves or the pending final ambush.</summary>
        /// <param name="wavesFinished">Whether every scheduled wave has already spawned.</param>
        /// <param name="ambushFinished">Whether the final ambush has completed.</param>
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void UnfinishedWaveOrAmbushPreventsAdventureCompletion(bool wavesFinished, bool ambushFinished)
        {
            var game = AdventureFixture();
            game.Adventure.CurrentWave = game.Adventure.Waves.Count - (wavesFinished ? 0 : 1);
            game.Adventure._finalAmbush = ambushFinished;
            var zombie = AddHypnoMeal(game);

            EatHypnoShroom(game, zombie);

            AssertStillPlaying(game);
            Assert.That(game.Zombies, Does.Contain(zombie));
        }

        /// <summary>Checks the converted final zombie starts the original delayed adventure Vasebreaker interlude.</summary>
        [Test]
        public void AdventureVaseInterludeStartsAfterTheLastHostileIsConverted()
        {
            var game = AdventureFixture(35);
            game.Adventure._vases.Clear();
            game.Adventure.VaseInterludeDialogueEnabled = true;
            var zombie = AddHypnoMeal(game);

            EatHypnoShroom(game, zombie);

            Assert.That(game.Adventure.VaseInterludeActive, Is.True);
            Assert.That(game.Adventure.VaseStage, Is.Zero);
            Assert.That(game.Adventure._vaseInterludeTicks, Is.EqualTo(200));
            Assert.That(game.Zombies, Is.Empty, "Vasebreaker clears converted survivors before its interlude.");
            AssertStillPlaying(game);
        }

        /// <summary>Checks the final adventure Vasebreaker stage drops its award after the last conversion.</summary>
        [Test]
        public void FinalAdventureVaseStageCompletesAfterTheLastHostileIsConverted()
        {
            var game = AdventureFixture(35);
            game.Adventure.VaseStage = 2;
            game.Adventure._vases.Clear();
            var zombie = AddHypnoMeal(game);

            EatHypnoShroom(game, zombie);

            Assert.That(game.State, Is.EqualTo(MatchState.Won));
            Assert.That(game.Adventure.Phase, Is.EqualTo(AdventurePhase.Reward));
            Assert.That(game.Adventure.RewardPresentation, Is.Not.Null);
            Assert.That(game.Zombies, Is.Empty);
        }

        /// <summary>Checks survival stage completion preserves converted survivors only while seed reselection remains.</summary>
        /// <param name="finalStage">Whether this is the final survival stage.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void SurvivalCompletionUsesTheOriginalReselectionCleanupBoundary(bool finalStage)
        {
            var session = new ModeSession(2, randomSeed: 7529);
            var game = session.Game;
            Assert.That(session.ChooseSeeds(game.Adventure.SelectedSeeds), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Adventure.CurrentWave = game.Adventure.Waves.Count;
            game.Adventure._finalAmbush = true;
            if (finalStage)
            {
                session.Stage = session.Definition.Stages;
            }

            var zombie = AddHypnoMeal(game);
            EatHypnoShroom(game, zombie);

            Assert.That(game.State, Is.EqualTo(MatchState.Won));
            Assert.That(game.Adventure.Phase, Is.EqualTo(AdventurePhase.Reward));
            Assert.That(session.Phase, Is.EqualTo(finalStage ? ModePhase.Won : ModePhase.StageCleared));
            Assert.That(session.ClearedStages, Is.EqualTo(session.Stage));
            Assert.That(game.Adventure.RewardPresentation, Is.Null);
            if (finalStage)
            {
                Assert.That(game.Zombies, Is.Empty);
            }
            else
            {
                Assert.That(game.Zombies, Does.Contain(zombie), "Seed reselection retains the original converted survivor until transition.");
                Assert.That(session.Continue(), Is.True);
                Assert.That(session.Phase, Is.EqualTo(ModePhase.ChoosingSeeds));
                Assert.That(game.Zombies, Is.Empty);
            }
        }

        /// <summary>Creates an explicitly isolated adventure board at its final wave without changing production APIs.</summary>
        /// <param name="level">The adventure level whose rules the fixture uses.</param>
        /// <returns>The playing board with distant wave scheduling and a completed final ambush.</returns>
        private static GameSimulation AdventureFixture(int level = 19)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            if (game.Adventure.Phase == AdventurePhase.ChoosingSeeds)
            {
                Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            }

            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Adventure.CurrentWave = game.Adventure.Waves.Count;
            game.Adventure._finalAmbush = true;
            return game;
        }

        /// <summary>Places a real awake mushroom and aligns a normal zombie with its native eating rectangle.</summary>
        /// <param name="game">The isolated playing board.</param>
        /// <returns>The hostile zombie that will consume the mushroom.</returns>
        private static Zombie AddHypnoMeal(GameSimulation game)
        {
            var mushroom = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.HypnoShroom, 2, 4)!;
            mushroom.Asleep = false;
            var zombie = AddZombie(game, 2, 4.5f);
            var attack = game.AdventureZombieAttackRectangle(zombie);
            var plant = game.AdventurePlantEatingRectangle(mushroom);
            zombie.X += (plant.X + plant.Width * .5f - attack.X - attack.Width * .5f) / 80f;
            return zombie;
        }

        /// <summary>Adds an explicitly isolated normal zombie using the production native initialization.</summary>
        /// <param name="game">The board containing the fixture zombie.</param>
        /// <param name="row">The fixture row.</param>
        /// <param name="x">The grid coordinate before native initialization.</param>
        /// <returns>The initialized live hostile zombie.</returns>
        private static Zombie AddZombie(GameSimulation game, int row, float x)
        {
            var zombie = new Zombie
            {
                Id = 1000 + game.Zombies.Count,
                Kind = ZombieKind.Normal,
                Row = row,
                X = x,
                Health = 270f,
                StartingHealth = 270f,
                Wave = game.Adventure.Waves.Count - 1
            };
            game.Zombies.Add(zombie);
            Invoke(game, "InitializeAdventureZombieNative", zombie, false);
            return zombie;
        }

        /// <summary>Runs only enough native simulation ticks for the real mushroom eating event.</summary>
        /// <param name="game">The board whose public update entry point is exercised.</param>
        /// <param name="zombie">The zombie expected to eat the awake mushroom.</param>
        private static void EatHypnoShroom(GameSimulation game, Zombie zombie)
        {
            for (var tick = 0; tick < 500 && !zombie.Hypnotized && game.State == MatchState.Playing; tick++)
            {
                if (game.ModeSession == null)
                {
                    game.Update(.01f);
                }
                else
                {
                    game.UpdateMode(.01f);
                }
            }

            Assert.That(zombie.Hypnotized, Is.True, "The production awake-mushroom chew must apply mind control.");
            Assert.That(game.Plants.Exists(plant => plant.Kind == PlantKind.HypnoShroom), Is.False);
        }

        /// <summary>Checks unmet end-level conditions keep the board active without an award.</summary>
        /// <param name="game">The playing board whose completion gate is being checked.</param>
        private static void AssertStillPlaying(GameSimulation game)
        {
            Assert.That(game.State, Is.EqualTo(MatchState.Playing));
            Assert.That(game.Adventure.Phase, Is.EqualTo(AdventurePhase.Playing));
            Assert.That(game.Adventure.RewardPresentation, Is.Null);
        }

        /// <summary>Invokes production fixture setup without expanding runtime visibility.</summary>
        /// <param name="game">The isolated simulation containing the private hook.</param>
        /// <param name="name">The private hook name.</param>
        /// <param name="arguments">The original hook arguments.</param>
        /// <returns>The value returned by the production hook.</returns>
        /// <exception cref="TargetInvocationException">The production hook throws.</exception>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
