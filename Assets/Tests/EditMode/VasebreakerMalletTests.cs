#nullable enable
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original single hammer, deferred contents, and puzzle-stage screen flash.</summary>
    public sealed class VasebreakerMalletTests
    {
        /// <summary>Checks each adventure stage holds its contents for the eight-frame forty-FPS swing.</summary>
        /// <param name="stage">The zero-based original adventure vase stage.</param>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AdventureReleasesContentsOnTwentiethTick(int stage)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            game.Adventure.VaseStage = stage;
            typeof(GameSimulation).GetMethod("InitializeVaseStage", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, null);
            AssertMalletTiming(game);
        }

        /// <summary>Checks all ten independent recipes use the same original single-hammer sequence.</summary>
        /// <param name="mode">The original PC Vasebreaker mode identifier.</param>
        [TestCase(51)]
        [TestCase(52)]
        [TestCase(53)]
        [TestCase(54)]
        [TestCase(55)]
        [TestCase(56)]
        [TestCase(57)]
        [TestCase(58)]
        [TestCase(59)]
        [TestCase(60)]
        public void IndependentPuzzlesReleaseContentsOnTwentiethTick(int mode)
        {
            AssertMalletTiming(new ModeSession(mode, randomSeed: 7529).Game);
        }

        /// <summary>Checks a released zombie begins updating on the following tick rather than during its creation tick.</summary>
        /// <param name="independent">Whether to exercise the standalone puzzle's public action path.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void ZombieContentsReleaseAfterExistingBoardObjects(bool independent)
        {
            var game = independent ? new ModeSession(51, randomSeed: 7529).Game
                : new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            var vase = game.Adventure.Vases.First(value => value._zombie == ZombieKind.Normal);
            Assert.That(Smash(game, vase.Row, vase.Column), Is.True);
            Advance(game, .19f);
            Assert.That(game.Zombies, Is.Empty);
            Advance(game, .01f);
            Assert.That(game.Zombies, Has.Count.EqualTo(1));
            var zombie = game.Zombies[0];
            Assert.That(zombie.Kind, Is.EqualTo(ZombieKind.Normal));
            Assert.That(zombie.Row, Is.EqualTo(vase.Row));
            Assert.That(zombie.Age, Is.Zero);
            Advance(game, .01f);
            Assert.That(zombie.Age, Is.GreaterThan(0f));
        }

        /// <summary>Checks sun pots retain their complete original value until the hammer impact.</summary>
        [Test]
        public void SunContentsReleaseOnlyAtImpact()
        {
            var session = new ModeSession(60, randomSeed: 7529);
            var contents = session._vaseSun.First();
            var row = contents.Key / 9;
            var column = contents.Key % 9;
            Assert.That(session.SmashVase(row, column), Is.True);
            session.Update(.19f);
            Assert.That(session.Game.Suns, Is.Empty);
            session.Update(.01f);
            Assert.That(session.Game.Suns, Has.Count.EqualTo(contents.Value / 25));
            Assert.That(session.Game.Suns.Sum(value => value.Value), Is.EqualTo(contents.Value));
            foreach (var sun in session.Game.Suns)
            {
                Assert.That(sun.Age, Is.Zero);
            }

            Assert.That(session.Game.Presentation.VaseBreakSoundSerial, Is.EqualTo(1));
        }

        /// <summary>Checks pause freezes a pending swing and an absent target produces no false impact.</summary>
        [Test]
        public void PendingSwingStopsWithTheBoardAndDoesNotOpenAnotherVase()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            var vase = game.Adventure.Vases[0];
            Assert.That(game.SmashAdventureVase(vase.Row, vase.Column), Is.True);
            game.TogglePause();
            game.Update(1f);
            Assert.That(game.Presentation.VaseMalletTicks, Is.EqualTo(20));
            Assert.That(game.SmashAdventureVase(vase.Row, vase.Column), Is.False);
            game.TogglePause();
            game.Adventure._vases.Remove(vase);
            game.Update(.2f);
            Assert.That(game.Presentation.VaseMalletTicks, Is.Zero);
            Assert.That(game.Presentation.VaseBreakSoundSerial, Is.Zero);
            Assert.That(game.Adventure.Packets, Is.Empty);
        }

        /// <summary>Checks the flash starts with native stage clearing before the later dialogue population entry.</summary>
        [Test]
        public void AdventureStageFlashOccursOnceAtDaveEntry()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            game.Adventure.VaseInterludeDialogueEnabled = true;
            Assert.That(game.Presentation.VaseScreenFlashAge, Is.EqualTo(-1f));
            for (var stage = 0; stage < 2; stage++)
            {
                game.Adventure._vases.Clear();
                game.Update(.01f);
                game.Update(1.99f);
                Assert.That(game.Presentation.VaseScreenFlashSerial, Is.EqualTo(stage));
                game.Update(.01f);
                Assert.That(game.Presentation.VaseScreenFlashSerial, Is.EqualTo(stage + 1));
                Assert.That(game.Presentation.VaseScreenFlashAge, Is.Zero);
                Assert.That(game.Adventure.Vases, Is.Empty);
                Assert.That(game.PopulateAdventureVaseInterlude(stage == 0 ? 2702 : 2801), Is.True);
                Assert.That(game.Presentation.VaseScreenFlashSerial, Is.EqualTo(stage + 1));
                Assert.That(game.FinishAdventureVaseInterlude(), Is.True);
                game.Update(.5f);
                Assert.That(game.Presentation.VaseScreenFlashTicks, Is.EqualTo(50));
                Assert.That(game.Presentation.VaseScreenFlashAge, Is.EqualTo(.5f));
            }
        }

        /// <summary>Checks an endless puzzle emits the original flash when its next stage clears and populates.</summary>
        [Test]
        public void EndlessStageFlashOccursOnceOnContinuation()
        {
            var session = new ModeSession(60, randomSeed: 7529);
            session.Game.Adventure._vases.Clear();
            session.Update(.01f);
            Assert.That(session.Phase, Is.EqualTo(ModePhase.StageCleared));
            Assert.That(session.Game.Presentation.VaseScreenFlashSerial, Is.Zero);
            Assert.That(session.Continue(), Is.True);
            Assert.That(session.Game.Presentation.VaseScreenFlashSerial, Is.EqualTo(1));
            Assert.That(session.Game.Presentation.VaseScreenFlashAge, Is.Zero);
            Assert.That(session.Continue(), Is.False);
            Assert.That(session.Game.Presentation.VaseScreenFlashSerial, Is.EqualTo(1));
        }

        /// <summary>Checks acceptance, mutual exclusion, exact impact timing, and single content release.</summary>
        /// <param name="game">The isolated adventure or independent puzzle simulation.</param>
        private static void AssertMalletTiming(GameSimulation game)
        {
            var vase = game.Adventure.Vases.First(value => value._seed.HasValue);
            var other = game.Adventure.Vases.First(value => !ReferenceEquals(value, vase));
            var count = game.Adventure.Vases.Count;
            Assert.That(Smash(game, -1, -1), Is.False);
            Assert.That(Smash(game, vase.Row, vase.Column), Is.True);
            Assert.That(game.Presentation.VaseMalletRow, Is.EqualTo(vase.Row));
            Assert.That(game.Presentation.VaseMalletColumn, Is.EqualTo(vase.Column));
            Assert.That(game.Presentation.VaseMalletAge, Is.Zero);
            Assert.That(game.Presentation.VaseSwingSoundSerial, Is.EqualTo(1));
            Assert.That(Smash(game, vase.Row, vase.Column), Is.False);
            Assert.That(Smash(game, other.Row, other.Column), Is.False);
            Assert.That(game.Presentation.VaseSwingSoundSerial, Is.EqualTo(1));
            Advance(game, .19f);
            Assert.That(game.Presentation.VaseMalletTicks, Is.EqualTo(1));
            Assert.That(game.Presentation.VaseMalletAge, Is.EqualTo(.19f).Within(.00001f));
            Assert.That(game.Adventure.Vases, Has.Count.EqualTo(count));
            Assert.That(game.Adventure.Packets, Is.Empty);
            Assert.That(game.Presentation.VaseBreakSoundSerial, Is.Zero);
            Advance(game, .01f);
            Assert.That(game.Presentation.VaseMalletTicks, Is.Zero);
            Assert.That(game.Adventure.Vases, Has.Count.EqualTo(count - 1));
            Assert.That(game.Adventure.Packets, Has.Count.EqualTo(1));
            Assert.That(game.Adventure.Packets[0].Kind, Is.EqualTo(vase._seed!.Value));
            Assert.That(game.Presentation.VaseBreakSoundSerial, Is.EqualTo(1));
            Assert.That(Smash(game, vase.Row, vase.Column), Is.False);
            Advance(game, .2f);
            Assert.That(game.Adventure.Packets, Has.Count.EqualTo(1));
            Assert.That(game.Presentation.VaseBreakSoundSerial, Is.EqualTo(1));
        }

        /// <summary>Requests a smash through the public API belonging to the current board.</summary>
        /// <param name="game">The isolated simulation.</param>
        /// <param name="row">The target lawn row.</param>
        /// <param name="column">The target lawn column.</param>
        /// <returns>Whether the board accepted the new hammer target.</returns>
        private static bool Smash(GameSimulation game, int row, int column)
        {
            return game.ModeSession == null ? game.SmashAdventureVase(row, column) : game.SmashModeVase(row, column);
        }

        /// <summary>Advances the appropriate public native-step update loop.</summary>
        /// <param name="game">The isolated simulation.</param>
        /// <param name="seconds">The elapsed seconds to advance.</param>
        private static void Advance(GameSimulation game, float seconds)
        {
            if (game.ModeSession == null)
            {
                game.Update(seconds);
            }
            else
            {
                game.UpdateMode(seconds);
            }
        }
    }
}
