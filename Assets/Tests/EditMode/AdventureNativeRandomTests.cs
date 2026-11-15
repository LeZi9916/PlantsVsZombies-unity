#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original x86 boss decision vectors and native blink random-consumption boundaries.</summary>
    public sealed class AdventureNativeRandomTests
    {
        /// <summary>Checks blink attempts reset their original countdown before rejecting a firing or airborne plant.</summary>
        /// <param name="shooting">Whether the original shooter counter blocks attachment.</param>
        /// <param name="airborne">Whether the original NotOnGround gate blocks attachment.</param>
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void RejectedBlinkStillConsumesItsOriginalReset(bool shooting, bool airborne)
        {
            var plant = new Plant { Kind = PlantKind.Peashooter, BlinkCountdown = 1, PresentationBiteNotOnGround = airborne };
            if (shooting)
            {
                plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.Peashooter, 50, 12);
            }

            var random = new RecordingRandom(1);
            AdventurePlantBlinkRules.Tick(plant, random);
            Assert.That(random.Bounds, Is.EqualTo(new[] { 400 }));
            Assert.That(plant.BlinkCountdown, Is.InRange(400, 799));
            Assert.That(plant.BlinkAnimation, Is.Null);
        }

        /// <summary>Checks the original nut and multi-head selection bounds and independent blink lifetime.</summary>
        /// <param name="kind">The original plant identifier.</param>
        /// <param name="expectedBounds">The original reset and attachment draw bounds.</param>
        [TestCase(PlantKind.Wallnut, "400,1000,10")]
        [TestCase(PlantKind.TallNut, "400,1000,10")]
        [TestCase(PlantKind.Threepeater, "400,3")]
        [TestCase(PlantKind.SplitPea, "400,2")]
        [TestCase(PlantKind.TwinSunflower, "400,2")]
        [TestCase(PlantKind.PuffShroom, "400")]
        public void BlinkSelectionUsesTheOriginalSpeciesBranches(PlantKind kind, string expectedBounds)
        {
            var plant = new Plant { Kind = kind, Health = 8000f, BlinkCountdown = 1 };
            var random = new RecordingRandom(4357);
            AdventurePlantBlinkRules.Tick(plant, random);
            Assert.That(random.Bounds, Is.EqualTo(expectedBounds.Split(',').Select(int.Parse)));
            if (plant.BlinkAnimation == null)
            {
                Assert.That(kind, Is.EqualTo(PlantKind.TwinSunflower));
                Assert.That(plant.BlinkHead, Is.EqualTo(1));
                return;
            }

            var frames = AdventurePlantBlinkRules.FrameCount(kind, plant.BlinkAnimation);
            for (var tick = 1; tick * 15 < frames * 100; tick++)
            {
                AdventurePlantBlinkRules.Tick(plant, random);
                Assert.That(plant.BlinkAnimation, Is.Not.Null);
            }

            AdventurePlantBlinkRules.Tick(plant, random);
            Assert.That(plant.BlinkAnimation, Is.Null);
        }

        /// <summary>Checks sleep pauses blink attempts while an existing independent attachment completes.</summary>
        [Test]
        public void SleepingPlantFinishesAnExistingBlinkWithoutDrawingAgain()
        {
            var plant = new Plant { Kind = PlantKind.PuffShroom, Asleep = true, BlinkCountdown = 1,
                BlinkAnimation = "anim_blink", BlinkAnimationTicks = 19 };
            var random = new RecordingRandom(1);
            AdventurePlantBlinkRules.Tick(plant, random);
            Assert.That(plant.BlinkAnimation, Is.Null);
            Assert.That(plant.BlinkCountdown, Is.EqualTo(1));
            Assert.That(random.Bounds, Is.Empty);
        }

        /// <summary>Checks original initialization counters are sampled even for plants whose specialized state may later ignore them.</summary>
        /// <param name="kind">The original plant species.</param>
        /// <param name="bound">The original counter's exclusive bound.</param>
        [TestCase(PlantKind.Plantern, 2501)]
        [TestCase(PlantKind.GatlingPea, 151)]
        [TestCase(PlantKind.TwinSunflower, 951)]
        [TestCase(PlantKind.GloomShroom, 201)]
        [TestCase(PlantKind.Cattail, 151)]
        [TestCase(PlantKind.CobCannon, 601)]
        public void EveryNativeLaunchCounterRetainsItsInitializationDraw(PlantKind kind, int bound)
        {
            var game = Board(19);
            var random = Attach(game, 1);
            Invoke(game, "PlaceAdventurePlant", kind, 1, 2);
            Assert.That(random.Bounds.Take(3), Is.EqualTo(new[] { 7, 400, bound }));
        }

        /// <summary>Checks the armed mine stores its original new blink countdown rather than discarding the ticket.</summary>
        [Test]
        public void PotatoMineArmingRestartsItsOriginalBlinkCountdown()
        {
            var game = Board(19);
            var random = Attach(game, 1);
            var plant = new Plant { Kind = PlantKind.PotatoMine, MinePhase = MinePhase.Rising,
                PotatoMinePresentation = new AdventurePotatoMineState { PhaseTicks = 105 } };
            Invoke(game, "AdvanceAdventurePotatoMine", plant);
            Assert.That(plant.MinePhase, Is.EqualTo(MinePhase.Armed));
            Assert.That(plant.BlinkCountdown, Is.InRange(400, 799));
            Assert.That(random.Bounds, Is.EqualTo(new[] { 400 }));
        }

        /// <summary>Creates a playing board without asynchronous game boot or persistent data.</summary>
        /// <param name="level">The original adventure level.</param>
        /// <returns>The controlled production board.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            SimulationFixture.Plants(game).Clear();
            return game;
        }

        /// <summary>Calls the existing production inspection boundary.</summary>
        /// <param name="game">The controlled board.</param>
        /// <param name="name">The production operation.</param>
        /// <param name="arguments">The original operation arguments.</param>
        /// <returns>The production result, or null.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return SimulationInspection.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }

        /// <summary>Installs a recording wrapper around the real original generator.</summary>
        /// <param name="game">The board receiving the controlled stream.</param>
        /// <param name="seed">The seed used by the original x86 oracle.</param>
        /// <returns>The recording original stream.</returns>
        private static RecordingRandom Attach(GameSimulation game, uint seed)
        {
            var random = new RecordingRandom(seed);
            SimulationInspection.GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, random);
            return random;
        }

        /// <summary>Records integer bounds without changing the original thirty-one-bit sequence.</summary>
        private sealed class RecordingRandom : Random
        {
            /// <summary>Owns the unchanged original generator adapter.</summary>
            private readonly OriginalRandomAdapter _random;
            /// <summary>Gets the number of native raw float calls.</summary>
            internal int RawCalls { get; private set; }
            /// <summary>Gets the ordered exclusive integer bounds.</summary>
            internal List<int> Bounds { get; } = new List<int>();

            /// <summary>Creates the native recording wrapper.</summary>
            /// <param name="seed">The original generator seed.</param>
            internal RecordingRandom(uint seed)
            {
                _random = new OriginalRandomAdapter(seed);
            }

            /// <inheritdoc/>
            public override int Next(int maxValue)
            {
                Bounds.Add(maxValue);
                return _random.Next(maxValue);
            }

            /// <inheritdoc/>
            public override int Next()
            {
                RawCalls++;
                return _random.Next();
            }

            /// <inheritdoc/>
            public override double NextDouble()
            {
                return _random.NextDouble();
            }
        }
    }
}
