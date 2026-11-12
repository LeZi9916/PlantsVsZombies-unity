#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original atlas selection, native emergence, and preview gating.</summary>
    public sealed class AdventureGravePresentationTests
    {
        /// <summary>Checks top-row, special, and ordinary graves select the original atlas cells.</summary>
        [Test]
        public void AtlasUsesFiveColumnsAndOriginalRowPrecedence()
        {
            var grave = new AdventureGravePresentation(19, -5, 4, 0);
            Assert.That(grave.AtlasRow(0), Is.EqualTo(1));
            Assert.That(grave.AtlasRow(2), Is.EqualTo(3));
            grave.Special = true;
            Assert.That(grave.AtlasRow(2), Is.Zero);
            Assert.That(grave.AtlasRow(0), Is.EqualTo(1));
            for (var tick = 0; tick < 100; tick++)
            {
                grave.Tick();
            }

            var stone = grave.Stone(2, 86, 91);
            var mound = grave.Mound(2, 86, 91);
            Assert.That(stone.Source.X, Is.EqualTo(4 * 86));
            Assert.That(stone.Source.Y, Is.Zero);
            Assert.That(stone.Destination.Width, Is.EqualTo(86));
            Assert.That(stone.Destination.Height, Is.EqualTo(91));
            Assert.That(stone.Destination.X, Is.EqualTo(-9));
            Assert.That(stone.Destination.Y, Is.EqualTo(-5));
            Assert.That(mound.Source.Height, Is.EqualTo(91));
            Assert.That(mound.Destination.Y, Is.EqualTo(-5));
            var eaten = grave.Stone(2, 86, 91, 40);
            Assert.That(eaten.Source.Y, Is.EqualTo(40));
            Assert.That(eaten.Source.Height, Is.EqualTo(51));
            Assert.That(eaten.Destination.Y, Is.EqualTo(35));
        }

        /// <summary>Checks the original random delay and one-hundred-tick emergence counter.</summary>
        [Test]
        public void EmergenceWaitsForItsDelayAndStopsAtOneHundred()
        {
            var grave = new AdventureGravePresentation(2, 0, 0, 49);
            for (var tick = 0; tick < 49; tick++)
            {
                Assert.That(grave.Stone(2, 86, 91).Source.Height, Is.Zero);
                grave.Tick();
            }

            Assert.That(grave.Counter, Is.Zero);
            for (var tick = 0; tick < 50; tick++)
            {
                grave.Tick();
            }

            var halfway = grave.Stone(2, 86, 91);
            Assert.That(halfway.Source.Height, Is.EqualTo(32));
            Assert.That(halfway.Destination.Y, Is.EqualTo(36));
            Assert.That(grave.Mound(2, 86, 91).Source.Height, Is.EqualTo(91));
            for (var tick = 0; tick < 100; tick++)
            {
                grave.Tick();
            }

            Assert.That(grave.Counter, Is.EqualTo(100));
            Assert.That(grave.Stone(2, 86, 91).Source.Height, Is.EqualTo(91));
        }

        /// <summary>Checks introduction preview ticks cannot reveal or advance hidden graves.</summary>
        [Test]
        public void DisabledPreviewDoesNotAdvanceGravestones()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            game.Adventure.GravesEnabled = false;
            var grave = game.Adventure.Graves[0].GravePresentation!;
            var initial = grave.Counter;
            for (var tick = 0; tick < 200; tick++)
            {
                game.AdvanceAdventureGraves();
            }

            Assert.That(grave.Counter, Is.EqualTo(initial));
            Assert.That(grave.Age, Is.Zero);
            game.Adventure.GravesEnabled = true;
            for (var tick = 0; tick < 150; tick++)
            {
                game.AdvanceAdventureGraves();
            }

            Assert.That(grave.Counter, Is.EqualTo(100));
            Assert.That(grave.Age, Is.EqualTo(1.5f).Within(.00001f));
            var special = 0;
            foreach (var cell in game.Adventure.Graves)
            {
                if (cell.GravePresentation!.Special)
                {
                    special++;
                }
            }

            Assert.That(special, Is.EqualTo(1));
        }
    }
}
