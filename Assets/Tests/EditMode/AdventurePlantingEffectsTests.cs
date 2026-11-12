#nullable enable

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the native PC planting burst selection, placement and player-action boundary.</summary>
    public sealed class AdventurePlantingEffectsTests
    {
        /// <summary>Checks the original soil and pool particles, support offsets, roof slope and authored lifetime.</summary>
        /// <param name="level">The original terrain fixture.</param>
        /// <param name="kind">The seed planted through the public placement operation.</param>
        /// <param name="row">The planted lawn row.</param>
        /// <param name="column">The planted lawn column.</param>
        /// <param name="x">The original horizontal particle origin.</param>
        /// <param name="y">The original vertical particle origin.</param>
        /// <param name="pool">Whether the original pool emitter is required.</param>
        [TestCase(9, PlantKind.Sunflower, 2, 4, 401f, 354f, false)]
        [TestCase(29, PlantKind.LilyPad, 2, 4, 401f, 339f, true)]
        [TestCase(49, PlantKind.FlowerPot, 1, 4, 401f, 279f, false)]
        public void PublicPlantingUsesOriginalEmitterAndCoordinates(int level, PlantKind kind, int row, int column, float x, float y, bool pool)
        {
            // PC Board::DoPlantingEffects 0x418AB0, particle IDs 21/22 and the original XML durations.
            var game = Fixture(level, kind);
            Assert.That(game.Presentation.PlantingEffects, Is.Empty, "Preplanted roof pots do not generate player planting bursts.");
            Assert.That(game.TryPlant(kind, row, column), Is.True);
            Assert.That(game.Presentation.PlantingEffects, Has.Count.EqualTo(1));
            var effect = game.Presentation.PlantingEffects[0];
            Assert.That(effect.X, Is.EqualTo(x));
            Assert.That(effect.Y, Is.EqualTo(y));
            Assert.That(effect.Row, Is.EqualTo(row));
            Assert.That(effect.Pool, Is.EqualTo(pool));
            Assert.That(effect.Age, Is.Zero);
            game.TogglePause();
            game.Update(.1f);
            Assert.That(game.Presentation.PlantingEffects[0].Age, Is.Zero);
            game.TogglePause();
            game.Update(pool ? .39f : .29f);
            Assert.That(game.Presentation.PlantingEffects, Has.Count.EqualTo(1));
            game.Update(.01f);
            Assert.That(game.Presentation.PlantingEffects, Is.Empty);
        }

        /// <summary>Checks fixture initialization and rejected player placement never create planting particles.</summary>
        [Test]
        public void InternalCreationAndRejectedPlacementDoNotCreateBursts()
        {
            var game = Fixture(9, PlantKind.Sunflower);
            Invoke(game, "PlaceAdventurePlant", PlantKind.Sunflower, 2, 4);
            Assert.That(game.Presentation.PlantingEffects, Is.Empty);
            Assert.That(game.TryPlant(PlantKind.Sunflower, 2, 4), Is.False);
            Assert.That(game.Presentation.PlantingEffects, Is.Empty);
        }

        /// <summary>Checks a successfully consumed Vasebreaker packet uses the same original planting path.</summary>
        [Test]
        public void PublicPacketPlantingCreatesOneBurst()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            game.Adventure._packets.Add(new AdventurePacket { Id = 1000, Kind = PlantKind.PuffShroom });
            Assert.That(game.TryUseAdventurePacket(1000, 0, 0), Is.True);
            Assert.That(game.Presentation.PlantingEffects, Has.Count.EqualTo(1));
            Assert.That(game.Presentation.PlantingEffects[0].X, Is.EqualTo(81f));
            Assert.That(game.Presentation.PlantingEffects[0].Y, Is.EqualTo(154f));
            Assert.That(game.TryUseAdventurePacket(1000, 0, 1), Is.False);
            Assert.That(game.Presentation.PlantingEffects, Has.Count.EqualTo(1));
        }

        /// <summary>Checks the independent Vasebreaker cherry button records its successful ground planting.</summary>
        [Test]
        public void VaseCherryButtonCreatesOneBurstAfterSpendingSun()
        {
            var game = new ModeSession(51, randomSeed: 7529).Game;
            typeof(GameSimulation).GetProperty(nameof(GameSimulation.SunAmount))!.SetValue(game, GameSimulation.Cost(PlantKind.CherryBomb));
            Assert.That(game.PlantModeVaseCherry(0, 0), Is.True);
            Assert.That(game.SunAmount, Is.Zero);
            Assert.That(game.Presentation.PlantingEffects, Has.Count.EqualTo(1));
            Assert.That(game.Presentation.PlantingEffects[0].X, Is.EqualTo(81f));
            Assert.That(game.Presentation.PlantingEffects[0].Y, Is.EqualTo(154f));
        }

        /// <summary>Checks flying coffee retains its successful wake operation without a ground burst.</summary>
        [Test]
        public void CoffeeBeanDoesNotCreateSoilOrWaterParticles()
        {
            var game = Fixture(49, PlantKind.CoffeeBean);
            // Fund only this isolated coffee action; ordinary first-play sunlight starts below its price.
            typeof(GameSimulation).GetProperty(nameof(GameSimulation.SunAmount))!.SetValue(game, GameSimulation.Cost(PlantKind.CoffeeBean));
            Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 0, 4);
            var mushroom = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.PuffShroom, 0, 4)!;
            Assert.That(mushroom.Asleep, Is.True);
            Assert.That(game.TryPlant(PlantKind.CoffeeBean, 0, 4), Is.True);
            Assert.That(mushroom.WakeTimer, Is.EqualTo(2f));
            Assert.That(game.Presentation.PlantingEffects, Is.Empty);
        }

        /// <summary>Creates a legal seed bank containing one earned fixture seed without changing player progress.</summary>
        /// <param name="level">The adventure level whose terrain and earned cards are used.</param>
        /// <param name="kind">The earned seed required by the test.</param>
        /// <returns>A playing board with distant wave scheduling.</returns>
        private static GameSimulation Fixture(int level, PlantKind kind)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            var seeds = new List<PlantKind> { kind };
            foreach (var candidate in game.Adventure.AvailableSeeds)
            {
                if (seeds.Count == game.Adventure.Level.PacketSlots)
                {
                    break;
                }

                if (!seeds.Contains(candidate))
                {
                    seeds.Add(candidate);
                }
            }

            Assert.That(game.ChooseAdventureSeeds(seeds), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Invokes existing private fixture setup without adding runtime APIs.</summary>
        /// <param name="game">The isolated board.</param>
        /// <param name="name">The production setup method.</param>
        /// <param name="arguments">The production method arguments.</param>
        /// <returns>The production method result.</returns>
        /// <exception cref="TargetInvocationException">The invoked production method throws.</exception>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
