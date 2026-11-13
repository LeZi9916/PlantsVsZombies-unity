#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the supplied original Board fog layout, roll-in, return and light footprints.</summary>
    public sealed class AdventureFogTests
    {
        /// <summary>Checks the original adventure fog progression rather than a fixed fifth-column boundary.</summary>
        /// <param name="level">The adventure stage whose fog depth is queried.</param>
        /// <param name="left">The original zero-based first fog column.</param>
        [TestCase(31, 6)]
        [TestCase(32, 5)]
        [TestCase(36, 5)]
        [TestCase(37, 4)]
        [TestCase(39, 4)]
        public void FogDepthFollowsOriginalAdventureStages(int level, int left)
        {
            Assert.That(AdventureFogState.LeftColumn(level), Is.EqualTo(left));
            Assert.That(AdventureFogState.LeftColumn(level, true), Is.EqualTo(5));
            Assert.That(AdventureFogState.LeftColumn(level, true, true), Is.EqualTo(6));
        }

        /// <summary>Checks the original two-second opening curve and the original twenty-second returning fog curve.</summary>
        [Test]
        public void FogRollsAcrossTheLawnInsteadOfAppearingAtCountdownZero()
        {
            var state = new AdventureFogState();
            var plants = new List<Plant>();
            state.BeginIntro(6);
            Assert.That(state.Offset, Is.EqualTo(585f));
            state.Advance(6, 1f, true, plants);
            Assert.That(state.Offset, Is.EqualTo(146.25f));
            state.Advance(6, 10f, false, plants);
            Assert.That(state.Offset, Is.EqualTo(146.25f));
            state.Advance(6, 0f, false, plants);
            Assert.That(state.Offset, Is.Zero);
            for (var tick = 0; tick < 100; tick++)
            {
                state.Advance(6, 0f, false, plants);
            }

            Assert.That(state.Opacity(6, 6), Is.EqualTo(200), "The seventh edge row also uses the original softer leading-column opacity.");
            Assert.That(state.Opacity(7, 6), Is.EqualTo(255));
        }

        /// <summary>Checks the original rounded Plantern area, small Torchwood cross and gradual fog restoration.</summary>
        [Test]
        public void PlanternAndTorchwoodClearTheirOriginalFogTiles()
        {
            var state = new AdventureFogState();
            var plants = new List<Plant>();
            for (var tick = 0; tick < 100; tick++)
            {
                state.Advance(4, 0f, false, plants);
            }

            plants.Add(new Plant { Kind = PlantKind.Plantern, Column = 5, Row = 3, Health = 300f });
            for (var tick = 0; tick < 100; tick++)
            {
                state.Advance(4, 0f, false, plants);
            }

            Assert.That(state.Opacity(8, 4), Is.Zero, "Plantern clears the original three-column horizontal reach.");
            Assert.That(state.Opacity(8, 5), Is.EqualTo(255), "The rounded footprint excludes the far corner.");
            plants.Clear();
            state.Advance(4, 0f, false, plants);
            Assert.That(state.Opacity(8, 4), Is.EqualTo(3), "Fog restores gradually after losing its light source.");
            plants.Add(new Plant { Kind = PlantKind.Torchwood, Column = 7, Row = 3, Health = 300f });
            for (var tick = 0; tick < 100; tick++)
            {
                state.Advance(4, 0f, false, plants);
            }

            Assert.That(state.Opacity(8, 3), Is.Zero);
            Assert.That(state.Opacity(8, 4), Is.EqualTo(255), "Torchwood only clears its original cross, excluding diagonals.");
        }

        /// <summary>Checks the held preview fog remains visible over the street before returning to the lawn.</summary>
        /// <param name="level">The original fog stage determining the grid's left column.</param>
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(37)]
        public void PreviewHoldsFogOverTheStreetUntilTheRollInEvent(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 91);
            game.BeginAdventureFogIntro();
            for (var tick = 0; tick < 600; tick++)
            {
                game.AdvanceAdventureFog(true);
            }

            var left = game.AdventureFogLeftColumn;
            var plan = AdventureIntroPresentation.CreatePlan(new AdventureIntroContext { Level = level, NativeBackground = 3 });
            var preview = AdventureIntroPresentation.Sample(plan, plan.ChooserOnEnd, true);
            Assert.That(game.Adventure.FogBlownRemaining, Is.EqualTo(2f));
            Assert.That(game.Adventure.Fog.Offset, Is.EqualTo(1065f - left * 80f));
            Assert.That(game.Adventure.Fog.Opacity(left, 3), Is.EqualTo(200), "Preview fog stays opaque rather than being disabled.");
            Assert.That(preview.BoardX, Is.EqualTo(-380));
            Assert.That(left * 80f + game.Adventure.Fog.Offset - 15f + preview.BoardX, Is.EqualTo(670f),
                "PC fog starts at board x=1050 and remains visible on the right when the camera pans to the street.");
            for (var tick = 0; tick < 201; tick++)
            {
                game.AdvanceAdventureFog(true, true);
            }

            Assert.That(game.Adventure.FogBlownRemaining, Is.Zero);
            Assert.That(game.Adventure.Fog.Offset, Is.Zero);
        }

        /// <summary>Checks Blover starts the original forty-second timer, including its twenty-second return interval.</summary>
        [Test]
        public void BloverUsesOriginalFogDurationAndStormHasNoFog()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 32 }, 91);
            game.ChooseDefaultAdventureSeeds();
            typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { PlantKind.Blover, 0, 4 });
            var advance = typeof(GameSimulation).GetMethod("AdvanceAdventurePlants", BindingFlags.Instance | BindingFlags.NonPublic)!;
            // PC PlantInit 46C23A sets fifty updates before DoSpecial, independently of the initial blow animation.
            for (var tick = 0; tick < 49; tick++)
            {
                advance.Invoke(game, new object[] { .01f });
            }

            Assert.That(game.Adventure.FogBlownRemaining, Is.Zero);
            advance.Invoke(game, new object[] { .01f });
            Assert.That(game.Adventure.FogBlownRemaining, Is.EqualTo(40f));
            Assert.That(new GameSimulation(new GameRules { AdventureLevel = 40 }, 91).AdventureHasFog, Is.False);
        }
    }
}
