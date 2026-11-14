#nullable enable
using System;
using System.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks native plant muzzles and actual height-based roof contacts rather than tile-distance expiry.</summary>
    public sealed class AdventureRoofShooterTests
    {
        /// <summary>Checks pea muzzles use the source cell, original animated anchor and five-pixel flowerpot height.</summary>
        /// <param name="column">The original sloping or flat roof column.</param>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(8)]
        public void PeaMuzzlesUseNativeBodyAnchorOnEveryRoofSection(int column)
        {
            foreach (var kind in new[] { PlantKind.Peashooter, PlantKind.SnowPea, PlantKind.Repeater, PlantKind.GatlingPea, PlantKind.Threepeater })
            {
                var game = AdventureGargantuarTests.Board(49);
                AdventureGargantuarTests.Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 1, column);
                var plant = (Plant)AdventureGargantuarTests.Invoke(game, "PlaceAdventurePlant", kind, 1, column)!;
                plant.Age = 0f;
                var pea = (Pea)AdventureGargantuarTests.Invoke(game, "FireAdventurePea", plant, 1, false, 20f)!;
                var plantY = 70f + 85f + Math.Max(0, 5 - column) * 20f;
                var expectedX = 40f + column * 80f + (kind == PlantKind.Threepeater ? 59f : kind == PlantKind.GatlingPea ? 85f : 75f);
                Assert.That(40f + pea.X * 80f, Is.EqualTo(expectedX).Within(.0001f), kind + " source X");
                Assert.That(pea.PresentationLaunchY, Is.EqualTo(plantY + (kind == PlantKind.Threepeater ? 19f : 24f)), kind + " source Y");
                Assert.That(pea.Range, Is.EqualTo(20f));
            }
        }

        /// <summary>Checks every real straight shooter can actually launch on the sloping roof before height collision.</summary>
        /// <param name="kind">The original straight-shooting plant.</param>
        [TestCase(PlantKind.Peashooter)]
        [TestCase(PlantKind.SnowPea)]
        [TestCase(PlantKind.Repeater)]
        [TestCase(PlantKind.GatlingPea)]
        [TestCase(PlantKind.Threepeater)]
        [TestCase(PlantKind.SplitPea)]
        [TestCase(PlantKind.Cactus)]
        [TestCase(PlantKind.ScaredyShroom)]
        public void ProductionShootersActuallyLaunchBeforeStrikingTheRoof(PlantKind kind)
        {
            var game = AdventureGargantuarTests.Board(49);
            AdventureGargantuarTests.Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 1, 1);
            var plant = (Plant)AdventureGargantuarTests.Invoke(game, "PlaceAdventurePlant", kind, 1, 1)!;
            plant.Asleep = false;
            plant.AwakeByCoffee = true;
            var zombie = (Zombie)AdventureGargantuarTests.Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Normal,
                1, AdventureZombieNativeRules.GridX(650f), 0)!;
            zombie.FrozenRemaining = 10f;
            for (var tick = 0; tick < 400 && game.Peas.Count == 0; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Peas.Any(shot => shot.SourceKind == kind), Is.True);
        }

        /// <summary>Checks the other straight shooters retain native spike/spore dimensions and pot-adjusted plant-cell muzzles.</summary>
        /// <param name="column">The sloping or flat roof launch column.</param>
        [TestCase(1)]
        [TestCase(5)]
        public void SpikeAndSporeMuzzlesUseOriginalRoofPlantCell(int column)
        {
            foreach (var kind in new[] { PlantKind.Cactus, PlantKind.ScaredyShroom })
            {
                var game = AdventureGargantuarTests.Board(49);
                AdventureGargantuarTests.Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 1, column);
                var plant = (Plant)AdventureGargantuarTests.Invoke(game, "PlaceAdventurePlant", kind, 1, column)!;
                AdventureGargantuarTests.Invoke(game, "FireAdventureNativeTimedProjectile", plant);
                var shot = game.Peas.Single();
                Assert.That(40f + shot.X * 80f, Is.EqualTo(40f + column * 80f + (kind == PlantKind.Cactus ? 85f : 45f)).Within(.0001f));
                Assert.That(shot.PresentationLaunchY, Is.EqualTo(155f + Math.Max(0, 5 - column) * 20f
                    + (kind == PlantKind.Cactus ? 26f : 29.5f)));
            }
        }

        /// <summary>Checks a native independent shadow/floor oracle and the real roof-impact Foley and particle point.</summary>
        /// <param name="column">The sloping or flat launch column.</param>
        /// <param name="kind">The original pea weapon.</param>
        [TestCase(0, PlantKind.Peashooter)]
        [TestCase(1, PlantKind.SnowPea)]
        [TestCase(4, PlantKind.Repeater)]
        [TestCase(5, PlantKind.GatlingPea)]
        public void RoofContactsFollowOriginalShadowClearanceAndCreateAnImpact(int column, PlantKind kind)
        {
            var game = AdventureGargantuarTests.Board(49);
            AdventureGargantuarTests.Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 1, column);
            var plant = (Plant)AdventureGargantuarTests.Invoke(game, "PlaceAdventurePlant", kind, 1, column)!;
            var pea = (Pea)AdventureGargantuarTests.Invoke(game, "FireAdventurePea", plant, 1, false, 20f)!;
            game.Plants.Remove(plant);
            var launchX = 40f + pea.X * 80f - 14f;
            var topY = pea.PresentationLaunchY - 14f;
            var cell = Math.Max(0, Math.Min(8, ((int)launchX - 40) / 80));
            var shadow = 155f + Math.Max(0, 5 - cell) * 20f + 67f - (launchX < 480f ? 12f : 0f);
            var expectedTick = 0;
            var x = launchX;
            for (var tick = 1; tick < 250; tick++)
            {
                var previous = x;
                x += 3.33f;
                if (shadow - topY < 28f)
                {
                    expectedTick = tick;
                    break;
                }
                shadow += (Math.Max(0f, 440f - x) - Math.Max(0f, 440f - previous)) * .25f;
            }

            for (var tick = 1; tick <= (expectedTick > 0 ? expectedTick : 120); tick++)
            {
                AdventureGargantuarTests.Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
                Assert.That(game.Peas.Contains(pea), Is.EqualTo(tick != expectedTick), "Native roof contact update " + tick);
            }

            if (expectedTick > 0)
            {
                var impact = game.Presentation.Impacts.Single();
                Assert.That(impact.NativeIcePoint, Is.True);
                Assert.That(impact.NativeParticleX, Is.EqualTo(x - 3f).Within(.002f));
                Assert.That(impact.NativeParticleY, Is.EqualTo(topY + 12f).Within(.0001f));
                Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 1), Is.EqualTo(1));
            }
            else
            {
                Assert.That(game.Presentation.Impacts, Is.Empty, "A flat-roof shot stays above the original contact plane.");
            }
        }
    }
}
