#nullable enable

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original planting Foley at the successful player-placement boundary.</summary>
    public sealed class AdventurePlantingSoundTests
    {
        /// <summary>Checks native water-square selection for aquatic seeds, lily-supported plants, upgrades, and flying coffee.</summary>
        /// <param name="kind">The seed placed through the actual packet path.</param>
        /// <param name="row">The selected dry or water row.</param>
        /// <param name="foley">The original planting Foley identifier.</param>
        [TestCase(PlantKind.LilyPad, 2, 52)]
        [TestCase(PlantKind.SeaShroom, 2, 52)]
        [TestCase(PlantKind.TangleKelp, 2, 52)]
        [TestCase(PlantKind.Peashooter, 2, 52)]
        [TestCase(PlantKind.Cattail, 2, 52)]
        [TestCase(PlantKind.GatlingPea, 2, 52)]
        [TestCase(PlantKind.CoffeeBean, 2, 7)]
        [TestCase(PlantKind.Peashooter, 1, 7)]
        public void SuccessfulPacketPlacementRequestsOriginalSquareSound(PlantKind kind, int row, int foley)
        {
            var game = Fixture();
            if (row == 2 && kind != PlantKind.LilyPad && kind != PlantKind.SeaShroom && kind != PlantKind.TangleKelp)
            {
                Place(game, PlantKind.LilyPad, row);
            }

            if (kind == PlantKind.GatlingPea)
            {
                Place(game, PlantKind.Repeater, row);
            }
            else if (kind == PlantKind.CoffeeBean)
            {
                var mushroom = Place(game, PlantKind.PuffShroom, row);
                Assert.That(mushroom.Asleep, Is.True);
            }

            game.Presentation.ZombieSounds.Clear();
            var plantedEvents = 0;
            game.EventRaised += gameEvent =>
            {
                if (gameEvent == GameEvent.Planted)
                {
                    plantedEvents++;
                    Assert.That(game.Presentation._nativePlantingEffects, Is.True);
                }
            };
            game.Adventure._packets.Add(new AdventurePacket { Id = 1000, Kind = kind });
            Assert.That(game.TryUseAdventurePacket(1000, row, 4), Is.True, game.LastAdventurePlantingFailure.ToString());
            Assert.That(plantedEvents, Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds, Has.Count.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds[0].Foley, Is.EqualTo(foley));
            Assert.That(game.Presentation._nativePlantingEffects, Is.False);
        }

        /// <summary>Checks the sun-funded card path requests water Foley once and rejected placements remain silent.</summary>
        [Test]
        public void SeedBankPlacementRequestsWaterSoundOnlyAfterSuccess()
        {
            var game = Fixture();
            Assert.That(game.TryPlant(PlantKind.LilyPad, 2, 4), Is.True, game.LastAdventurePlantingFailure.ToString());
            Assert.That(game.Presentation.ZombieSounds, Has.Count.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds[0].Foley, Is.EqualTo(52));
            Assert.That(game.TryPlant(PlantKind.LilyPad, 2, 4), Is.False);
            game.Adventure._packets.Add(new AdventurePacket { Id = 1000, Kind = PlantKind.LilyPad });
            Assert.That(game.TryUseAdventurePacket(1000, 2, 4), Is.False);
            Assert.That(game.Presentation.ZombieSounds, Has.Count.EqualTo(1));
        }

        /// <summary>Checks scene setup does not accidentally acquire native player-placement Foley.</summary>
        [Test]
        public void InternalPlantCreationDoesNotRequestPlayerPlantingFoley()
        {
            var game = Fixture();
            Place(game, PlantKind.LilyPad, 2);
            Assert.That(game.Presentation.ZombieSounds, Is.Empty);
            Assert.That(game.Presentation._nativePlantingEffects, Is.False);
        }

        /// <summary>Creates a daytime pool with a legally selected Lily Pad card and no nearby waves.</summary>
        /// <returns>The isolated playing production board.</returns>
        private static GameSimulation Fixture()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 29 }, 7529);
            var seeds = new List<PlantKind> { PlantKind.LilyPad };
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

        /// <summary>Creates a support or prerequisite without pretending it was a player action.</summary>
        /// <param name="game">The production board receiving the fixture plant.</param>
        /// <param name="kind">The support or upgrade prerequisite seed.</param>
        /// <param name="row">The requested lawn row.</param>
        /// <returns>The created fixture plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int row)
        {
            return (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { kind, row, 4 })!;
        }
    }
}
