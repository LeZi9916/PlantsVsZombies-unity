#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original adventure eligibility, forced insertion order, and random-consumption boundaries.</summary>
    public sealed class AdventureWaveSpawnTests
    {
        /// <summary>Checks replay bowling uses Challenge::InitZombieWaves rather than the ordinary adventure mask.</summary>
        /// <param name="cycle">The one-based adventure cycle.</param>
        /// <param name="firstWeight">The total candidate weight captured at PC 0x4195B0.</param>
        [TestCase(2, 12000)]
        [TestCase(3, 12001)]
        public void ReplayBowlingUsesNativeChallengeWeights(int cycle, int firstWeight)
        {
            var random = new ZeroTicketRandom();
            var waves = AdventureWavePlan.Build(AdventureLevels.ForRun(5, cycle), random);
            Assert.That(random.Bounds[0], Is.EqualTo(firstWeight));
            Assert.That(waves.Count, Is.EqualTo(20));
            Assert.That(waves[waves.Count - 1].Zombies, Has.No.Member(ZombieKind.Newspaper), "Final coverage uses ordinary eligibility, not the challenge selection mask.");
            Assert.That(waves[waves.Count - 1].Zombies, Has.No.Member(ZombieKind.Buckethead));
            Assert.That(waves[waves.Count - 1].Zombies, Has.No.Member(ZombieKind.PoleVaulter));
        }

        /// <summary>Checks runtime callers receive the actual special mask instead of recomputing ordinary table eligibility.</summary>
        [Test]
        public void ExportedEligibilityPreservesSpecialSelectionMasks()
        {
            var enabled = new bool[34];
            AdventureWavePlan.Build(AdventureLevels.ForRun(5, 2), new ZeroTicketRandom(), enabledOutput: enabled);
            Assert.That(enabled[3] && enabled[4] && enabled[5], Is.True);
            AdventureWavePlan.Build(AdventureLevels.ForRun(15, 3), new ZeroTicketRandom(), enabledOutput: enabled);
            Assert.That(enabled[0] && enabled[2] && enabled[4], Is.True);
            Assert.That(enabled[3] || enabled[5] || enabled[19], Is.False);
            Assert.Throws<ArgumentException>(() => AdventureWavePlan.Build(AdventureLevels.Get(5), new ZeroTicketRandom(), enabledOutput: new bool[33]));
        }

        /// <summary>Checks an original MT input stream reproduces the machine-code first wave for replay bowling.</summary>
        /// <param name="cycle">The one-based adventure cycle.</param>
        /// <param name="first">The first original type ID.</param>
        /// <param name="second">The second original type ID.</param>
        [TestCase(2, 5, 2)]
        [TestCase(3, 2, 2)]
        public void ReplayBowlingSeedOneMatchesNativeFirstWave(int cycle, int first, int second)
        {
            var waves = AdventureWavePlan.Build(AdventureLevels.ForRun(5, cycle), new OriginalRandomAdapter(1));
            Assert.That(waves[0].Zombies, Is.EqualTo(new[] { (ZombieKind)first, (ZombieKind)second }));
        }

        /// <summary>Checks Whack's unused precomputed table still consumes the original bounds during board initialization.</summary>
        [Test]
        public void WhackPlanPreservesNativeRandomBoundsAndFinalCoverage()
        {
            var random = new ZeroTicketRandom();
            var waves = AdventureWavePlan.Build(AdventureLevels.ForRun(15, 2), random);
            Assert.That(random.Bounds, Is.EqualTo(new[] { 4000, 4000, 4000, 8000, 4000, 8000, 4000, 8000, 8000, 4000, 8000, 8000, 4000 }));
            Assert.That(waves[7].Zombies, Is.EqualTo(new[] { ZombieKind.Normal, ZombieKind.Conehead, ZombieKind.PoleVaulter, ZombieKind.Buckethead, ZombieKind.Newspaper }));
        }

        /// <summary>Checks Boss table construction retains original forced entries and charges their costs.</summary>
        /// <param name="cycle">The one-based adventure cycle.</param>
        /// <param name="midpoint">The zero-based forced-Boss wave.</param>
        /// <param name="drawCount">The original number of bounded draws with zero tickets.</param>
        [TestCase(1, 15, 152)]
        [TestCase(2, 20, 324)]
        [TestCase(3, 20, 324)]
        public void BossPlanPreservesOriginalInsertionOrderAndRandomConsumption(int cycle, int midpoint, int drawCount)
        {
            var random = new ZeroTicketRandom();
            var waves = AdventureWavePlan.Build(AdventureLevels.ForRun(50, cycle), random);
            Assert.That(waves[midpoint].Zombies, Is.EqualTo(new[] { ZombieKind.Boss }));
            Assert.That(waves[waves.Count - 1].Zombies, Is.EqualTo(new[]
            {
                ZombieKind.Normal, ZombieKind.Normal, ZombieKind.Normal, ZombieKind.Normal,
                ZombieKind.Normal, ZombieKind.Normal, ZombieKind.Normal, ZombieKind.Normal,
                ZombieKind.Flag, ZombieKind.Boss, ZombieKind.Gargantuar, ZombieKind.Conehead,
                ZombieKind.Buckethead, ZombieKind.JackInTheBox, ZombieKind.Bungee, ZombieKind.Ladder,
                ZombieKind.Catapult
            }));
            Assert.That(random.Bounds.Count, Is.EqualTo(drawCount));
        }

        /// <summary>Checks explicit native state suppresses only the forced yeti and leaves ordinary profile eligibility intact.</summary>
        [Test]
        public void ExplicitContextSuppressesForcedYetiWithoutGuessingItsLifecycle()
        {
            var ordinaryRandom = new ZeroTicketRandom();
            var suppressedRandom = new ZeroTicketRandom();
            var level = AdventureLevels.ForRun(40, 2);
            var ordinary = AdventureWavePlan.Build(level, ordinaryRandom);
            var suppressed = AdventureWavePlan.Build(level, suppressedRandom, context: new AdventureWaveGenerationContext(true));
            Assert.That(ordinary[15].Zombies, Has.Member(ZombieKind.Yeti));
            Assert.That(suppressed[15].Zombies, Has.No.Member(ZombieKind.Yeti));
            Assert.That(ordinaryRandom.Bounds.Count, Is.EqualTo(583));
            Assert.That(suppressedRandom.Bounds.Count, Is.EqualTo(587));
        }

        /// <summary>Supplies deterministic tickets while recording the production candidate-weight boundaries.</summary>
        private sealed class ZeroTicketRandom : Random
        {
            /// <summary>Retains every production upper bound in call order.</summary>
            private readonly List<int> _bounds = new List<int>();

            /// <summary>Gets the captured exclusive upper bounds.</summary>
            public IReadOnlyList<int> Bounds => _bounds;

            /// <summary>Records the original candidate weight and selects its first positive interval.</summary>
            /// <param name="maxValue">The production exclusive upper bound.</param>
            /// <returns>A deterministic zero ticket.</returns>
            public override int Next(int maxValue)
            {
                _bounds.Add(maxValue);
                return 0;
            }
        }
    }
}
