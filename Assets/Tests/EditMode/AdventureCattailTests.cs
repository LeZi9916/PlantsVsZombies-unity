#nullable enable
using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC 46DC00/472C70/4752E0/47CCB0 Cattail attack and homing behavior.</summary>
    public sealed class AdventureCattailTests
    {
        /// <summary>Checks each body animation emits one spike at countdown nineteen and the second starts at launch countdown fifty.</summary>
        [Test]
        public void EachAttackAnimationFiresExactlyOneOriginalSpike()
        {
            var game = Board();
            var plant = new Plant { Id = 80, Kind = PlantKind.Cattail, Row = 2, Column = 3, Health = 300f, ActionTimer = .01f };
            SimulationFixture.Plants(game).Add(plant);
            Spawn(game, ZombieKind.Normal, 2, 7f);
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(plant.UpgradeBurstTicks, Is.EqualTo(50));
            for (var tick = 0; tick < 30; tick++)
            {
                Invoke(game, "AdvanceAdventurePlants", .01f);
            }
            Assert.That(game.Peas, Is.Empty);
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(game.Peas, Has.Count.EqualTo(1));
            var shot = game.Peas[0];
            Assert.That(shot.X, Is.EqualTo(3.25f));
            Assert.That(shot.PresentationHomingY, Is.EqualTo(247f));
            Assert.That(shot.VelocityX, Is.EqualTo(2.5f));
            for (var tick = 0; tick < 19; tick++)
            {
                Invoke(game, "AdvanceAdventurePlants", .01f);
            }
            Assert.That(game.Peas, Has.Count.EqualTo(1), "Countdown one ends this attack without a second spike.");
            plant.ActionTimer = .51f;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(plant.UpgradeBurstTicks, Is.EqualTo(50));
            Assert.That(plant.PresentationShotAge, Is.Zero);
            for (var tick = 0; tick < 31; tick++)
            {
                Invoke(game, "AdvanceAdventurePlants", .01f);
            }
            Assert.That(game.Peas, Has.Count.EqualTo(2));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 3).Count, Is.EqualTo(2));
        }

        /// <summary>Checks original two-pixel speed, gradual turning, and no reacquisition after the chosen target dies.</summary>
        [Test]
        public void HomingTurnsGraduallyAndRetainsDirectionAfterTargetDies()
        {
            var game = Board();
            var target = Spawn(game, ZombieKind.Normal, 4, 7f);
            var shot = new Pea { Homing = true, TargetZombieId = target.Id, X = 3.25f, FlightRow = 2f,
                PresentationHomingY = 247f, VelocityX = 2.5f, SourceKind = PlantKind.Cattail };
            Invoke(game, "UpdateAdventureHomingVelocity", shot, .01f);
            var vx = shot.VelocityX * .8f;
            var vy = shot.VelocityY * .85f;
            Assert.That(Math.Sqrt(vx * vx + vy * vy), Is.EqualTo(2f).Within(.00001f));
            Assert.That(vy, Is.GreaterThan(0f).And.LessThan(.002f), "The first native age adds only .001 toward the target direction.");
            Assert.That(shot.PresentationHomingY - 247f, Is.LessThan(.002f));
            target.Health = 0f;
            Spawn(game, ZombieKind.Balloon, 0, 6f).Airborne = true;
            Invoke(game, "UpdateAdventureHomingVelocity", shot, .01f);
            Assert.That(shot.TargetZombieId, Is.EqualTo(target.Id));
            Assert.That(shot.VelocityX * .8f, Is.EqualTo(vx));
            Assert.That(shot.VelocityY * .85f, Is.EqualTo(vy));
        }

        /// <summary>Checks a nearer body in pixels wins, while an original airborne target outranks ordinary bodies.</summary>
        [Test]
        public void TargetSelectionUsesBodyPixelsAndBalloonPriority()
        {
            var game = Board();
            var near = Spawn(game, ZombieKind.Normal, 2, 4.5f);
            Spawn(game, ZombieKind.Normal, 0, 3.5f);
            Assert.That(Invoke(game, "FindAdventureCattailTarget", 3.5f, 2f), Is.SameAs(near));
            var balloon = Spawn(game, ZombieKind.Balloon, 5, 8f);
            balloon.Airborne = true;
            Assert.That(Invoke(game, "FindAdventureCattailTarget", 3.5f, 2f), Is.SameAs(balloon));
            balloon.Health = 0f;
            Assert.That(Invoke(game, "FindAdventureCattailTarget", 3.5f, 2f), Is.SameAs(near));
        }

        /// <summary>Creates a quiet pool-terrain simulation.</summary>
        /// <returns>The isolated board.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 21 }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates an initialized native target.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="kind">The target species.</param>
        /// <param name="row">The native row.</param>
        /// <param name="x">The board x coordinate.</param>
        /// <returns>The initialized target.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, int row, float x)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", kind, row, x, 0)!;
        }

        /// <summary>Invokes the existing production inspection boundary.</summary>
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
