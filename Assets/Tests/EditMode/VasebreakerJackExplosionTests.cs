#nullable enable

using System;
using System.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original grid-based pot opening caused by a vase-released Jack-in-the-Box.</summary>
    public sealed class VasebreakerJackExplosionTests
    {
        /// <summary>Checks inclusive neighboring cells, clipped lawn edges, and an interrupted single hammer.</summary>
        /// <param name="independent">Whether to exercise the standalone puzzle session.</param>
        /// <param name="hypnotized">Whether the original carrier has changed factions before detonation.</param>
        /// <param name="row">The original carrier's vase row.</param>
        /// <param name="column">The original carrier's vase column.</param>
        /// <param name="opened">The number of neighboring pots remaining after the carrier is released.</param>
        [TestCase(false, false, 2, 4, 8)]
        [TestCase(true, false, 2, 4, 8)]
        [TestCase(false, true, 2, 4, 8)]
        [TestCase(true, true, 2, 4, 8)]
        [TestCase(false, false, 4, 8, 3)]
        [TestCase(true, false, 4, 8, 3)]
        public void BoxBlastOpensOriginalThreeByThreeCellsWithoutAnotherHammer(bool independent, bool hypnotized, int row, int column, int opened)
        {
            var game = CreateVaseGrid(independent, row, column);
            var carrier = ReleaseCarrier(game, row, column);
            var pendingRow = Math.Max(0, row - 1);
            var pendingColumn = Math.Max(0, column - 1);
            Assert.That(Smash(game, pendingRow, pendingColumn), Is.True);
            carrier.Hypnotized = hypnotized;
            carrier.PresentationNativePhase = 16;
            carrier.JackInTheBoxTicks = 1;
            Advance(game, .01f);
            Assert.That(game.Zombies, Has.No.Member(carrier));
            Assert.That(game.Adventure.Vases, Has.Count.EqualTo(44 - opened));
            foreach (var vase in game.Adventure.Vases)
            {
                Assert.That(Math.Abs(vase.Row - row) > 1 || Math.Abs(vase.Column - column) > 1, Is.True,
                    "PC ScaryPotterJackExplode opens the inclusive three-by-three grid, including diagonal pots.");
            }

            Assert.That(game.Adventure.Packets, Has.Count.EqualTo(opened));
            Assert.That(game.Presentation.VaseBreakSoundSerial, Is.EqualTo(1 + opened));
            Assert.That(game.Presentation.VaseSwingSoundSerial, Is.EqualTo(2), "Explosion-opened pots must not start hammer animations.");
            foreach (var packet in game.Adventure.Packets)
            {
                Assert.That(packet.Kind, Is.EqualTo(PlantKind.Peashooter));
                Assert.That(packet.Age, Is.Zero, "Explosion contents are born after this update's existing pickups.");
                Assert.That(packet.GroundTicks, Is.Zero);
            }

            Advance(game, .19f);
            Assert.That(game.Presentation.VaseMalletTicks, Is.Zero);
            Assert.That(game.Adventure.Packets, Has.Count.EqualTo(opened), "The interrupted hammer must not release the already exploded pot twice.");
            Assert.That(game.Presentation.VaseBreakSoundSerial, Is.EqualTo(1 + opened));
        }

        /// <summary>Checks an explosion releases all native content types and a released box carrier continues the chain.</summary>
        [Test]
        public void ExplosionReleasesSeedSunAndZombieContentsAndChainsAnotherBox()
        {
            var game = CreateVaseGrid(true, 2, 4);
            var session = game.RequiredModeSession;
            SetContents(game, 1, 3, null, null);
            session._vaseSun[1 * 9 + 3] = 50;
            SetContents(game, 1, 4, null, ZombieKind.Normal);
            SetContents(game, 1, 5, null, ZombieKind.JackInTheBox);
            var carrier = ReleaseCarrier(game, 2, 4);
            carrier.PresentationNativePhase = 16;
            carrier.JackInTheBoxTicks = 1;
            Advance(game, .01f);
            Assert.That(game.Adventure.Packets, Has.Count.EqualTo(5));
            Assert.That(game.Suns.Sum(sun => sun.Value), Is.EqualTo(50));
            Assert.That(game.Suns.All(sun => sun.Age == 0f), Is.True);
            Assert.That(game.Zombies.Count(zombie => zombie.Kind == ZombieKind.Normal), Is.EqualTo(1));
            var nextCarrier = game.Zombies.Single(zombie => zombie.Kind == ZombieKind.JackInTheBox);
            Assert.That(nextCarrier.Age, Is.Zero);
            Assert.That(nextCarrier.JackInTheBoxTicks, Is.EqualTo(10));
            Assert.That(game.Presentation.VaseSwingSoundSerial, Is.EqualTo(1));
            Advance(game, 1.19f);
            Assert.That(game.Zombies, Does.Contain(nextCarrier));
            Advance(game, .01f);
            Assert.That(game.Zombies, Has.No.Member(nextCarrier));
            Assert.That(game.Adventure.Packets, Has.Count.EqualTo(10), "The second box opens the five still-closed pots in its neighboring grid.");
            Assert.That(game.Adventure.Vases, Has.Count.EqualTo(31));
            Assert.That(game.Presentation.VaseBreakSoundSerial, Is.EqualTo(14));
            Assert.That(game.Presentation.VaseSwingSoundSerial, Is.EqualTo(1));
        }

        /// <summary>Creates a deterministic complete grid of real model vases with one original box carrier.</summary>
        /// <param name="independent">Whether the model uses standalone recipe content maps.</param>
        /// <param name="carrierRow">The row containing the initial Jack-in-the-Box.</param>
        /// <param name="carrierColumn">The column containing the initial Jack-in-the-Box.</param>
        /// <returns>The playing puzzle simulation with surrounding pots to test native blast boundaries.</returns>
        private static GameSimulation CreateVaseGrid(bool independent, int carrierRow, int carrierColumn)
        {
            var game = independent ? new ModeSession(51, randomSeed: 7529).Game
                : new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            game.Adventure.VaseStage = 2;
            game.Adventure._vases.Clear();
            game.ModeSession?._vaseSeeds.Clear();
            game.ModeSession?._vaseSun.Clear();
            for (var row = 0; row < 5; row++)
            {
                for (var column = 0; column < 9; column++)
                {
                    game.Adventure._vases.Add(new AdventureVase { Row = row, Column = column });
                    SetContents(game, row, column, PlantKind.Peashooter, null);
                }
            }

            SetContents(game, carrierRow, carrierColumn, null, ZombieKind.JackInTheBox);
            return game;
        }

        /// <summary>Changes only explicit hidden model contents while preserving the production open-pot implementation.</summary>
        /// <param name="game">The puzzle simulation receiving the deterministic hidden content.</param>
        /// <param name="row">The vase row.</param>
        /// <param name="column">The vase column.</param>
        /// <param name="seed">The plant card, or null for another original content type.</param>
        /// <param name="zombie">The zombie species, or null for seed or sun content.</param>
        private static void SetContents(GameSimulation game, int row, int column, PlantKind? seed, ZombieKind? zombie)
        {
            var vase = game.Adventure.Vases.Single(value => value.Row == row && value.Column == column);
            vase._seed = seed;
            vase._zombie = zombie;
            if (game.ModeSession != null)
            {
                var cell = row * 9 + column;
                game.ModeSession._vaseSeeds.Remove(cell);
                game.ModeSession._vaseSun.Remove(cell);
                if (seed.HasValue)
                {
                    game.ModeSession._vaseSeeds[cell] = (int)seed.Value;
                }
            }
        }

        /// <summary>Releases the initial box carrier through the public production mallet sequence.</summary>
        /// <param name="game">The playing puzzle simulation.</param>
        /// <param name="row">The carrier's original vase row.</param>
        /// <param name="column">The carrier's original vase column.</param>
        /// <returns>The live native box carrier produced by the opened pot.</returns>
        private static Zombie ReleaseCarrier(GameSimulation game, int row, int column)
        {
            Assert.That(Smash(game, row, column), Is.True);
            Advance(game, .2f);
            return game.Zombies.Single(zombie => zombie.Kind == ZombieKind.JackInTheBox);
        }

        /// <summary>Requests the production hammer through the appropriate public puzzle action.</summary>
        /// <param name="game">The playing puzzle simulation.</param>
        /// <param name="row">The target vase row.</param>
        /// <param name="column">The target vase column.</param>
        /// <returns>Whether the production action accepted the new hammer target.</returns>
        private static bool Smash(GameSimulation game, int row, int column)
        {
            return game.ModeSession == null ? game.SmashAdventureVase(row, column) : game.SmashModeVase(row, column);
        }

        /// <summary>Advances the production native-step loop belonging to the current puzzle.</summary>
        /// <param name="game">The playing simulation.</param>
        /// <param name="seconds">The unpaused simulation time to advance.</param>
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
