#nullable enable
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original PC 46F3F0/475230/472C70 cannon phases, firing and feedback.</summary>
    public sealed class AdventureCobCannonTests
    {
        /// <summary>Checks first arming, charge completion, release before the complete shooting clip, and the next reload.</summary>
        [Test]
        public void CompleteCycleUsesNativeAnimationAndReleaseMilestones()
        {
            var game = Board();
            var cannon = Cannon(game, CobCannonPhase.Arming, 5f);
            Step(game, cannon, 500);
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Loading));
            Assert.That(cannon.CannonReloadRemaining, Is.EqualTo(1.25f));
            Step(game, cannon, 124);
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Loading));
            Step(game, cannon, 1);
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Ready));
            Assert.That(game.TryAimAdventureCobCannon(cannon.Id), Is.True);
            Assert.That(game.TryFireAdventureCobCannon(7.25f, 1.8f), Is.True);
            Step(game, cannon, 204);
            Assert.That(game.Adventure.CobShells, Is.Empty);
            Step(game, cannon, 1);
            Assert.That(game.Adventure.CobShells, Has.Count.EqualTo(1));
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Firing));
            var shell = game.Adventure.CobShells[0];
            Assert.That(shell.TargetX, Is.EqualTo(7.25f - 7f / 80f));
            Assert.That(shell.TargetRow, Is.EqualTo(1f));
            Assert.That(shell.LaunchX, Is.EqualTo(3f + 37f / 80f));
            Assert.That(shell.LaunchNativeY, Is.EqualTo(135f));
            Step(game, cannon, 145);
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Reloading));
            Assert.That(cannon.CannonReloadRemaining, Is.EqualTo(30f));
            Assert.That(cannon.CannonResetVersion, Is.EqualTo(1));
            Assert.That(game.Adventure.CobShells, Has.Count.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 76).Count, Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 3).Count, Is.EqualTo(1));
            Step(game, cannon, 3125);
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Ready));
        }

        /// <summary>Checks the original thirty-tick/fifty-pixel repeat-click guard and explicit cancellation.</summary>
        [Test]
        public void AimDelayBlocksOnlyNearbyRepeatClicks()
        {
            var game = Board();
            var cannon = Cannon(game, CobCannonPhase.Ready, 0f);
            Assert.That(game.TryAimAdventureCobCannon(cannon.Id), Is.True);
            Assert.That(game.TryFireAdventureCobCannon(3.5f, 2.4f), Is.False);
            Assert.That(game.Adventure.SelectedCobCannonId, Is.EqualTo(cannon.Id));
            for (var tick = 0; tick < 30; tick++)
            {
                Invoke(game, "AdvanceAdventureUpgradeWorld", .01f);
            }
            Assert.That(game.TryFireAdventureCobCannon(3.5f, 2.4f), Is.True);
            Assert.That(game.Adventure.SelectedCobCannonId, Is.Null);
            Assert.That(game.TryAimAdventureCobCannon(cannon.Id), Is.False);
            cannon.CannonPhase = CobCannonPhase.Ready;
            Assert.That(game.TryAimAdventureCobCannon(cannon.Id), Is.True);
            Assert.That(game.CancelAdventureCobCannonAim(), Is.True);
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Ready));
        }

        /// <summary>Checks firing keeps the exact horizontal pointer coordinate and clips lower clicks onto the final row.</summary>
        [Test]
        public void TargetRetainsSubcellCoordinatesAndBottomRow()
        {
            var game = Board();
            var cannon = Cannon(game, CobCannonPhase.Ready, 0f);
            game.TryAimAdventureCobCannon(cannon.Id);
            Assert.That(game.TryFireAdventureCobCannon(7.123f, 5.1f), Is.True);
            Assert.That(cannon.CannonTargetX, Is.EqualTo(7.123f));
            Assert.That(cannon.CannonTargetRow, Is.EqualTo(4f));
        }

        /// <summary>Checks the original integer triangle-wave track color.</summary>
        /// <param name="tick">The native main counter.</param>
        /// <param name="brightness">The expected grayscale track component.</param>
        [TestCase(0, 255)]
        [TestCase(37, 55)]
        [TestCase(74, 255)]
        [TestCase(75, 255)]
        public void ReadyCobUsesOriginalTrackFlash(int tick, int brightness)
        {
            Assert.That(AdventureCobCannonRules.ReadyBrightness(tick), Is.EqualTo(brightness));
        }

        /// <summary>Creates a quiet five-row daytime board.</summary>
        /// <returns>The isolated simulation.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 19 }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Adds one cannon with an explicit original phase.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="phase">The original phase to begin.</param>
        /// <param name="remaining">The phase countdown in seconds.</param>
        /// <returns>The cannon retained by the board.</returns>
        private static Plant Cannon(GameSimulation game, CobCannonPhase phase, float remaining)
        {
            var cannon = new Plant { Id = 73, Kind = PlantKind.CobCannon, Row = 2, Column = 3,
                ColumnSpan = 2, Health = 300f, CannonPhase = phase, CannonReloadRemaining = remaining };
            SimulationFixture.Plants(game).Add(cannon);
            return cannon;
        }

        /// <summary>Advances only the cannon state machine for the required number of original updates.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="cannon">The cannon to advance.</param>
        /// <param name="ticks">The native update count.</param>
        private static void Step(GameSimulation game, Plant cannon, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                Invoke(game, "AdvanceAdventureCannon", cannon, .01f);
            }
        }

        /// <summary>Calls an existing production collaborator through the inspection boundary.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="name">The native operation.</param>
        /// <param name="arguments">The operation arguments.</param>
        /// <returns>The operation result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return SimulationInspection.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
