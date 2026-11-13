#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC spawn scheduling and lane histories against the examined original executable.</summary>
    public sealed class AdventureSpawnFidelityTests
    {
        /// <summary>Checks complete six-lane pick histories from PC 0x5269A0, including changing eligibility and mower weights.</summary>
        /// <param name="seed">The seed passed to the original thirty-one-bit generator.</param>
        /// <param name="nativeRows">Sixty-four row results executed directly from the original PE.</param>
        [TestCase(1u, "5,5,1,4,0,1,5,5,0,4,1,5,0,4,5,1,4,5,0,4,5,1,3,2,4,5,3,2,1,3,4,5,3,4,2,5,2,4,3,5,0,1,4,2,3,0,2,1,5,4,3,2,0,5,1,4,2,0,4,3,1,2,5,0")]
        [TestCase(12345u, "5,5,1,0,4,0,4,5,0,1,4,5,4,1,0,5,4,1,0,4,5,5,4,1,2,3,4,5,2,3,4,2,5,4,3,1,5,3,2,4,0,2,1,5,4,3,1,0,5,2,3,4,5,1,0,2,4,1,3,5,0,2,4,1")]
        [TestCase(4357u, "5,1,4,5,0,5,4,1,0,4,5,0,1,4,0,5,4,1,0,5,4,4,3,5,1,4,2,5,3,2,4,3,1,4,5,2,3,4,2,3,0,0,1,5,2,3,0,1,4,5,2,4,0,3,1,2,5,0,4,1,3,0,4,5")]
        public void SmoothPickerMatchesOriginalContinuousSequences(uint seed, string nativeRows)
        {
            var expected = nativeRows.Split(',');
            var random = new OriginalRandomAdapter(seed);
            var picker = new AdventureRowPicker();
            for (var index = 0; index < expected.Length; index++)
            {
                for (var row = 0; row < AdventureRowPicker.RowCount; row++)
                {
                    picker.Weights[row] = index < 20 && (row == 2 || row == 3) ? 0f
                        : index >= 20 && index < 40 && row == 0 ? 0.01f
                        : index >= 20 && index < 40 && row == 1 ? 0.5f : 1f;
                }

                Assert.That(picker.Pick(random), Is.EqualTo(int.Parse(expected[index])), "Native selection " + index);
            }

            Assert.That(random.Generator.DrawCount, Is.EqualTo(64UL));
        }

        /// <summary>Checks the float-store boundaries and clamp from PC 0x5268B0.</summary>
        /// <param name="weight">The normalized lane weight.</param>
        /// <param name="last">The eligible age of its most recent selection.</param>
        /// <param name="second">The eligible age of its preceding selection.</param>
        /// <param name="expected">The actual original single-precision return value.</param>
        [TestCase(0f, 0f, 0f, 0f)]
        [TestCase(0.000001f, 0f, 0f, 0.00000001f)]
        [TestCase(0.25f, 0f, 0f, 0.0025f)]
        [TestCase(0.25f, 10f, 20f, 1.109375f)]
        [TestCase(0.01f, 1f, 2f, 0.0001f)]
        [TestCase(0.5f, 3f, 10f, 1.6875f)]
        [TestCase(0.16666667f, 5f, 12f, 0.1736111044883728f)]
        [TestCase(1f, 1000f, 2000f, 100f)]
        public void SmoothWeightsMatchOriginalFloatStores(float weight, float last, float second, float expected)
        {
            Assert.That(AdventureRowPicker.CalculateSmoothWeight(weight, last, second), Is.EqualTo(expected));
        }

        /// <summary>Checks PC 0x4197D0's first-row exception applies to all endless survival boards.</summary>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="expected">Whether its first row may receive a Gargantuar.</param>
        [TestCase(1, false)]
        [TestCase(6, false)]
        [TestCase(10, false)]
        [TestCase(11, true)]
        [TestCase(12, true)]
        [TestCase(13, true)]
        [TestCase(14, true)]
        [TestCase(15, true)]
        public void EndlessGargantuarsMayUseTheFirstRow(int mode, bool expected)
        {
            var game = new ModeSession(mode).Game;
            Assert.That(Invoke(game, "CanAdventureZombieSpawnInRow", ZombieKind.Gargantuar, 0), Is.EqualTo(expected));
        }

        /// <summary>Checks the actual row picker applies recent-mower suppression and the invisible mode's early sixth-row exclusion.</summary>
        [Test]
        public void RowPickerPreservesMowerWeightsAndInvisibleEarlyGate()
        {
            var game = Fixture(8);
            game.Adventure.CurrentWave = 10;
            game.Adventure.RowPicker.LastMowedWave[0] = 9;
            game.Adventure.RowPicker.LastMowedWave[1] = 8;
            Invoke(game, "ChooseAdventureZombieRow", ZombieKind.Normal);
            Assert.That(game.Adventure.RowPicker.Weights[0], Is.EqualTo(0.01f));
            Assert.That(game.Adventure.RowPicker.Weights[1], Is.EqualTo(0.5f));
            Assert.That(game.Adventure.RowPicker.Weights[2], Is.EqualTo(1f));

            var invisible = new ModeSession(21).Game;
            for (var wave = 0; wave < 5; wave++)
            {
                invisible.Adventure.CurrentWave = wave;
                Invoke(invisible, "ChooseAdventureZombieRow", ZombieKind.Normal);
                Assert.That(invisible.Adventure.RowPicker.Weights[5], Is.EqualTo(wave < 4 ? 0f : 1f));
            }
        }

        /// <summary>Checks PC 0x432A40's near-mower, portal-lane, and ordinary-lane weights.</summary>
        /// <param name="row">The lane in the original initial portal layout.</param>
        /// <param name="expected">Its original spawn weight.</param>
        [TestCase(0, 1f)]
        [TestCase(1, 0.01f)]
        [TestCase(2, 0.2f)]
        [TestCase(3, 0.01f)]
        [TestCase(4, 1f)]
        public void PortalRowsUsePathDistanceWeights(int row, float expected)
        {
            var game = new ModeSession(26).Game;
            Assert.That(Invoke(game, "AdventurePortalRowSpawnWeight", row), Is.EqualTo(expected));
        }

        /// <summary>Checks a forced rake pick updates the existing history without drawing a random value.</summary>
        [Test]
        public void RakeUpdatesSmoothHistoryWithoutConsumingRandom()
        {
            var game = Fixture(8);
            game.Adventure.RowPicker.Weights[1] = 1f;
            game.Adventure.RowPicker.LastPicked[1] = 3f;
            game.Adventure.RakeRow = 1;
            game.Adventure.RakeAttracting = true;
            var random = AttachRandom(game);
            Assert.That(Invoke(game, "ChooseAdventureZombieRow", ZombieKind.Normal), Is.EqualTo(1));
            Assert.That(random.RawCalls, Is.Zero);
            Assert.That(random.Bounds, Is.Empty);
            Assert.That(game.Adventure.RowPicker.LastPicked[1], Is.Zero);
            Assert.That(game.Adventure.RowPicker.SecondLastPicked[1], Is.EqualTo(4f));
            Assert.That(game.Adventure.RakeAttracting, Is.False);
        }

        /// <summary>Checks PC InitZombieWaves gives every survival reselection six hundred update counts.</summary>
        /// <param name="mode">A normal, hard, or endless survival mode.</param>
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(11)]
        [TestCase(13)]
        [TestCase(15)]
        public void SurvivalReselectionPreservesSixHundredCountOpening(int mode)
        {
            var session = new ModeSession(mode);
            session.Game.Adventure.RowPicker.LastPicked[0] = 3f;
            session.Game.Adventure.RowPicker.LastMowedWave[0] = 9;
            session.Phase = ModePhase.StageCleared;
            Assert.That(session.Continue(), Is.True);
            Assert.That(session.Game.RequiredWaveSchedule.CountdownTicks, Is.EqualTo(600));
            var seeds = session.Game.Adventure.AvailableSeeds.Take(Math.Min(session.PacketSlots, session.Game.Adventure.AvailableSeeds.Count));
            Assert.That(session.Game.ChooseModeSeeds(seeds), Is.True);
            Assert.That(session.Game.RequiredWaveSchedule.CountdownTicks, Is.EqualTo(600));
            Assert.That(session.Game.Adventure.RowPicker.LastPicked[0], Is.EqualTo(3f));
            Assert.That(session.Game.Adventure.RowPicker.LastMowedWave[0], Is.EqualTo(-100));
        }

        /// <summary>Checks the native fixed interval avoids consuming the ordinary bounded timer draw.</summary>
        /// <param name="mode">Plants in Columns or Last Stand.</param>
        [TestCase(27)]
        [TestCase(31)]
        public void SpecialModesUseSevenHundredFiftyCountRefresh(int mode)
        {
            var game = new ModeSession(mode).Game;
            game.Adventure.Waves = new[] { new AdventureWave { Index = 0, Zombies = new[] { ZombieKind.Normal } } };
            game.Adventure.CurrentWave = 0;
            game.ResetAdventureWaveSchedule(1, false);
            var random = AttachRandom(game);
            Invoke(game, "AdvanceScheduledAdventureWaveTick");
            Assert.That(game.RequiredWaveSchedule.CountdownTicks, Is.EqualTo(750));
            Assert.That(random.Bounds.Contains(600), Is.False);
        }

        /// <summary>Checks the actual native threshold vector that changes integer rounding at a body health of two hundred seventy.</summary>
        [Test]
        public void ThresholdUsesNativeFloatRangeBeforeHealthMultiplication()
        {
            var random = new OriginalRandomAdapter(1);
            for (var index = 0; index < 1180; index++)
            {
                random.Next();
            }

            var threshold = AdventureSpawnRandom.NextRange(random, 0.5f, 0.65f);
            Assert.That((int)((double)threshold * 270), Is.EqualTo(160));
            Assert.That(random.Generator.DrawCount, Is.EqualTo(1181UL));
        }

        /// <summary>Checks final ambushes use the current difficulty mask rather than the borrowed adventure-level matrix.</summary>
        [Test]
        public void HardPoolAmbushRetainsTheEnabledBucketheadWeight()
        {
            var game = new ModeSession(8).Game;
            Assert.That(game.Adventure.EnabledZombieTypes[4], Is.True);
            var random = AttachRandom(game);
            random.LastTicket = true;
            Assert.That(Invoke(game, "PickScheduledAmbushZombie", false), Is.EqualTo(ZombieKind.Buckethead));
            Assert.That(random.Bounds, Is.EqualTo(new[] { 11000 }));
        }

        /// <summary>Checks airborne attachment health contributes until damage pops it, without taking the same damage from the body.</summary>
        [Test]
        public void BalloonHealthIsCountedUntilItsAttachmentBreaks()
        {
            var game = Fixture(38);
            var balloon = (Zombie)Invoke(game, "SpawnAdventureZombie", ZombieKind.Balloon, 0, 9.5f, 0)!;
            Assert.That(balloon.FlyingHealth, Is.EqualTo(20));
            Assert.That(Invoke(game, "ScheduledWaveHealth", 0), Is.EqualTo(290));
            Invoke(game, "ApplyAdventureZombieDamage", balloon, 10f, false, 0u);
            Assert.That(balloon.FlyingHealth, Is.EqualTo(10));
            Assert.That(balloon.Health, Is.EqualTo(270f));
            Assert.That(balloon.Airborne, Is.True);
            Invoke(game, "ApplyAdventureZombieDamage", balloon, 10f, false, 0u);
            Assert.That(balloon.FlyingHealth, Is.Zero);
            Assert.That(balloon.Health, Is.EqualTo(270f));
            Assert.That(balloon.Airborne, Is.False);
            Assert.That(Invoke(game, "ScheduledWaveHealth", 0), Is.EqualTo(270));
        }

        /// <summary>Checks only PC AddZombieInRow-based creations consume the extra one-in-five variant draw.</summary>
        [Test]
        public void IndependentCreationAddsVariantDrawWithoutChangingRawSummonInitialization()
        {
            var game = Fixture(8);
            var random = AttachRandom(game);
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Normal, 2, 9.5f, 0)!;
            Assert.That(random.Bounds[0], Is.EqualTo(5));
            Assert.That(random.Bounds[1], Is.EqualTo(40));
            Assert.That(zombie.NativeVariant, Is.True);
            random.Bounds.Clear();
            Invoke(game, "SpawnAdventureZombie", ZombieKind.Normal, 2, 9.5f, 0);
            Assert.That(random.Bounds.Contains(5), Is.False);
        }

        /// <summary>Checks four fallback bodies each choose a lane and a variant, while real sled followers do neither.</summary>
        [Test]
        public void MissingIceCreatesFourIndependentPlainZombies()
        {
            var game = Fixture(28);
            var random = AttachRandom(game);
            Invoke(game, "SpawnScheduledAdventureWave", new AdventureWave { Index = 0, Zombies = new[] { ZombieKind.Bobsled } });
            Assert.That(game.Zombies, Has.Count.EqualTo(4));
            Assert.That(game.Zombies.All(zombie => zombie.Kind == ZombieKind.Normal), Is.True);
            Assert.That(random.RawCalls, Is.EqualTo(4));
            Assert.That(random.Bounds.Count(bound => bound == 5), Is.EqualTo(4));
        }

        /// <summary>Checks PC 0x41EC80 creates both actors and consumes both native variant draws before Umbrella Leaf intercepts them.</summary>
        [Test]
        public void UmbrellaInterceptionFollowsCarrierAndPassengerCreation()
        {
            var game = Fixture(48);
            game.Plants.Add(new Plant { Kind = PlantKind.UmbrellaLeaf, Row = 2, Column = 5, Layer = PlantLayer.Main, Health = 300f });
            var random = AttachRandom(game);
            Invoke(game, "DropAdventureZombie", ZombieKind.Normal, 2, 5, 0);
            Assert.That(game.Zombies, Has.Count.EqualTo(2));
            Assert.That(game.Zombies[0].Kind, Is.EqualTo(ZombieKind.Bungee));
            Assert.That(game.Zombies[0].LinkedZombieId, Is.EqualTo(game.Zombies[1].Id));
            Assert.That(random.Bounds.Count(bound => bound == 5), Is.EqualTo(2));
            Invoke(game, "AdvanceAdventureZombies", 0.01f);
            Assert.That(game.Zombies, Is.Empty);
        }

        /// <summary>Checks the deferred independent-mode board consumes no discarded adventure plan, grave, pot, or sky-sun draws.</summary>
        [Test]
        public void DeferredBoardLeavesTheBattleStreamUntouched()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 48, DeferAdventureWaveInitialization = true }, 123);
            Assert.That(game.Adventure.Waves, Is.Empty);
            Assert.That(game.Adventure.Graves, Is.Empty);
            Assert.That(game.Plants, Is.Empty);
            Assert.That(game.CaptureBattleRandomState().DrawCount, Is.Zero);
        }

        /// <summary>Creates an actual adventure board with no synthetic entities.</summary>
        /// <param name="level">The borrowed adventure board number.</param>
        /// <returns>The production simulation used by a controlled regression.</returns>
        private static GameSimulation Fixture(int level)
        {
            return new GameSimulation(new GameRules { AdventureLevel = level }, 123);
        }

        /// <summary>Calls a private production boundary without duplicating the behavior under test.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="method">The private boundary's method name.</param>
        /// <param name="arguments">Its original parameter values.</param>
        /// <returns>The boundary's result, or null for a void method.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }

        /// <summary>Installs a recording stream so draw order can be checked separately from unrelated constructor draws.</summary>
        /// <param name="game">The board whose subsequent draws are observed.</param>
        /// <returns>The installed deterministic stream.</returns>
        private static RecordingRandom AttachRandom(GameSimulation game)
        {
            var random = new RecordingRandom();
            typeof(GameSimulation).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, random);
            return random;
        }

        /// <summary>Records bounded and raw calls while providing valid nonzero floating tickets.</summary>
        private sealed class RecordingRandom : Random
        {
            /// <summary>The bounded draws consumed after installation.</summary>
            internal List<int> Bounds { get; } = new List<int>();
            /// <summary>The number of native raw float draws consumed by the tested path.</summary>
            internal int RawCalls { get; private set; }
            /// <summary>Whether bounded draws select their last ticket instead of zero.</summary>
            internal bool LastTicket { get; set; }

            /// <summary>Returns the first positive native raw value and records the raw draw.</summary>
            /// <returns>One, which cannot select an excluded zero-weight prefix through an exact zero ticket.</returns>
            public override int Next()
            {
                RawCalls++;
                return 1;
            }

            /// <summary>Records a bounded native draw and returns a controlled ticket.</summary>
            /// <param name="maxValue">The exclusive original bound.</param>
            /// <returns>The first or last valid ticket.</returns>
            public override int Next(int maxValue)
            {
                Bounds.Add(maxValue);
                return LastTicket ? maxValue - 1 : 0;
            }

            /// <summary>Returns the midpoint used by unrelated existing animation and speed consumers.</summary>
            /// <returns>One half.</returns>
            public override double NextDouble()
            {
                return 0.5;
            }
        }
    }
}
