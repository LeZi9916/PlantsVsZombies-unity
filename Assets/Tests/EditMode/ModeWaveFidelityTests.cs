#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Waves;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks independent-mode branches against the supplied PC executable's machine-code oracle.</summary>
    public sealed class ModeWaveFidelityTests
    {
        /// <summary>Checks PC 0x4309A0's local MT, rejection draws, zero-weight slots, and random Redeye eligibility.</summary>
        [Test]
        public void SurvivalEligibilityUsesTheOriginalLocalSeedAndSlotDomain()
        {
            var enabled = ModeWaveRules.BuildAllowed(ModeCatalog.Get(13), 5, AdventureTerrain.Pool, new ModeWaveContext(12345));
            Assert.That(EnabledIds(enabled), Is.EqualTo(new[] { 0, 1, 2, 5, 12, 17, 18, 25 }));
            Assert.That(enabled[32], Is.False);
            var hard = ModeWaveRules.BuildAllowed(ModeCatalog.Get(6), 1, AdventureTerrain.Day, new ModeWaveContext(12345));
            Assert.That(EnabledIds(hard), Is.EqualTo(new[] { 0, 2, 22, 33 }));
        }

        /// <summary>Checks PC 0x415E40's raw unsigned seed contributions and wraparound.</summary>
        [Test]
        public void LocalSeedPreservesRawUnsignedWraparound()
        {
            var context = new ModeWaveContext(uint.MaxValue, uint.MaxValue);
            Assert.That(context.DeriveSeed(13, 500), Is.EqualTo(50511u));
        }

        /// <summary>Checks the late-survival budget includes the native factor of two.</summary>
        [Test]
        public void LaterSurvivalBudgetMatchesTheNativeFirstWave()
        {
            var random = new OriginalRandomAdapter(12345);
            var waves = ModeWaveRules.Build(ModeCatalog.Get(1), 1, AdventureTerrain.Day, new ModeWaveContext(12345), random);
            Assert.That(waves[0].Zombies.Select(kind => (int)kind), Is.EqualTo(new[] { 0, 2, 2 }));
        }

        /// <summary>Checks the first wave performs its real weighted draw and preserves the complete native draw count.</summary>
        [Test]
        public void FirstWaveConsumesTheNativeWeightedDraw()
        {
            var random = new OriginalRandomAdapter(12345);
            var waves = ModeWaveRules.Build(ModeCatalog.Get(1), 0, AdventureTerrain.Day, new ModeWaveContext(12345), random);
            Assert.That(waves[0].Zombies, Is.EqualTo(new[] { ZombieKind.Normal }));
            Assert.That(random.Generator.DrawCount, Is.EqualTo(16ul));
        }

        /// <summary>Checks the native fixed lists for Beghouled and Little Trouble.</summary>
        /// <param name="mode">The original independent-mode identifier.</param>
        /// <param name="expected">The exact eligibility identifier list.</param>
        [TestCase(20, new[] { 0, 2, 4, 5, 6, 7 })]
        [TestCase(24, new[] { 0, 2, 4, 5, 6, 7 })]
        [TestCase(25, new[] { 0, 2, 7, 11 })]
        public void FixedLineupsMatchTheNativeModeLists(int mode, int[] expected)
        {
            var enabled = ModeWaveRules.BuildAllowed(ModeCatalog.Get(mode), 0, AdventureTerrain.Pool, new ModeWaveContext(12345));
            Assert.That(EnabledIds(enabled), Is.EqualTo(expected));
        }

        /// <summary>Checks the explicit disabled-special-rules input follows the original Little Trouble fallback branch.</summary>
        [Test]
        public void DisabledSpecialRulesKeepTheirOriginalModeFallback()
        {
            var context = new ModeWaveContext(12345, specialRulesEnabled: false);
            var mode = ModeCatalog.Get(25);
            var enabled = ModeWaveRules.BuildAllowed(mode, 0, AdventureTerrain.Pool, context);
            Assert.That(EnabledIds(enabled), Is.EqualTo(new[] { 0, 2, 4 }));
            var waves = ModeWaveRules.Build(mode, 0, AdventureTerrain.Pool, context, new OriginalRandomAdapter(12345), enabled);
            Assert.That(waves.Count, Is.EqualTo(40));
        }

        /// <summary>Checks the mode-17 ticket intervals follow native ID order rather than handwritten-list order.</summary>
        [Test]
        public void CandidatesUseOriginalIdentifierOrder()
        {
            var candidates = Candidates(17, 0, 5, 8, new[] { 0, 2, 4, 3, 5 });
            Assert.That(candidates.Select(candidate => candidate.ZombieId), Is.EqualTo(new[] { 0, 2, 3, 4, 5 }));
        }

        /// <summary>Checks the native gate-reduction interval starts at 18 completed flags.</summary>
        /// <param name="flags">The native completed-flag count.</param>
        /// <param name="expected">Whether screen-door zombies pass the fourth-wave gate.</param>
        [TestCase(16, false)]
        [TestCase(18, false)]
        [TestCase(20, true)]
        [TestCase(50, true)]
        public void EndlessGateReductionStartsAtEighteenFlags(int flags, bool expected)
        {
            Assert.That(Candidates(13, flags, 3, 10, new[] { 6 }).Count > 0, Is.EqualTo(expected));
        }

        /// <summary>Checks the original 10-to-50-flag Normal and Conehead weight curves.</summary>
        /// <param name="flags">The completed-flag count.</param>
        /// <param name="normalWeight">The native Normal weight.</param>
        /// <param name="coneWeight">The native Conehead weight.</param>
        [TestCase(10, 4000, 4000)]
        [TestCase(30, 2200, 2500)]
        [TestCase(50, 400, 1000)]
        public void SurvivalWeightsFollowTheNativeCurves(int flags, int normalWeight, int coneWeight)
        {
            var candidates = Candidates(13, flags, 19, 10, new[] { 0, 2 });
            Assert.That(candidates[0].Weight, Is.EqualTo(normalWeight));
            Assert.That(candidates[1].Weight, Is.EqualTo(coneWeight));
        }

        /// <summary>Checks flag waves use the per-wave Redeye quota while ordinary waves use stage totals and weight 1000.</summary>
        /// <param name="wave">The zero-based native wave index.</param>
        /// <param name="waveCount">The Redeye count already in this wave.</param>
        /// <param name="roundCount">The Redeye count already in this stage.</param>
        /// <param name="expectedWeight">The expected Redeye weight, or zero for exclusion.</param>
        [TestCase(18, 0, 0, 1000)]
        [TestCase(18, 0, 1, 0)]
        [TestCase(19, 0, 50, 6000)]
        [TestCase(19, 1, 0, 0)]
        public void RedeyeLimitsUseTheCorrectCounter(int wave, int waveCount, int roundCount, int expectedWeight)
        {
            var candidates = Candidates(13, 10, wave, 100, new[] { 32 }, 32, waveCount, roundCount);
            Assert.That(candidates.Count == 0 ? 0 : candidates[0].Weight, Is.EqualTo(expectedWeight));
        }

        /// <summary>Checks Gargantuar and Zamboni cannot exceed the native early per-wave quota.</summary>
        /// <param name="id">The native zombie identifier.</param>
        [TestCase(12)]
        [TestCase(23)]
        public void HeavyZombieQuotaChecksAlreadyInsertedCounts(int id)
        {
            Assert.That(Candidates(13, 8, 19, 100, new[] { id }, id, 1).Count, Is.EqualTo(1));
            Assert.That(Candidates(13, 8, 19, 100, new[] { id }, id, 2).Count, Is.Zero);
        }

        /// <summary>Checks endless Bungee selection is flag-only and bypasses the remaining budget.</summary>
        [Test]
        public void EndlessBungeeUsesItsNativeFlagException()
        {
            Assert.That(Candidates(13, 50, 18, 1, new[] { 20 }).Count, Is.Zero);
            Assert.That(Candidates(13, 50, 19, 1, new[] { 20 }).Count, Is.EqualTo(1));
        }

        /// <summary>Checks Pogo Party and Bobsled Bonanza bypass the standard budget and wave gates.</summary>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="id">The original zombie identifier.</param>
        [TestCase(28, 12)]
        [TestCase(34, 18)]
        public void SpecialModesBypassBudgetAndWaveGates(int mode, int id)
        {
            Assert.That(Candidates(mode, 0, 0, 1, new[] { id }).Count, Is.EqualTo(1));
        }

        /// <summary>Checks Columns Like You See 'Em inserts the original fixed Ladder, Jack-in-the-box, and Gargantuar waves.</summary>
        [Test]
        public void ColumnsKeepsNativeForcedInsertions()
        {
            var waves = ModeWaveRules.Build(ModeCatalog.Get(27), 0, AdventureTerrain.Roof,
                new ModeWaveContext(12345), new OriginalRandomAdapter(12345));
            Assert.That(waves[5].Zombies.Count(kind => kind == ZombieKind.Ladder), Is.EqualTo(10));
            Assert.That(waves[8].Zombies.Count(kind => kind == ZombieKind.JackInTheBox), Is.EqualTo(10));
            Assert.That(waves[19].Zombies.Count(kind => kind == ZombieKind.Gargantuar), Is.EqualTo(3));
            Assert.That(waves[29].Zombies.Count(kind => kind == ZombieKind.Gargantuar), Is.EqualTo(5));
        }

        /// <summary>Checks the three formerly truncated modes retain all 40 native waves.</summary>
        /// <param name="mode">The original mode identifier.</param>
        [TestCase(22)]
        [TestCase(28)]
        [TestCase(29)]
        public void FortyWaveModesKeepTheOriginalLength(int mode)
        {
            Assert.That(ModeCatalog.Get(mode).WaveCount, Is.EqualTo(40));
        }

        /// <summary>Checks the original raw profile gate and special-mode Yeti exclusions.</summary>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="progress">The raw profile +0x54 value.</param>
        /// <param name="level">The raw profile +0x4C value.</param>
        /// <param name="expected">Whether the Yeti eligibility flag is enabled.</param>
        [TestCase(13, 1, 39, false)]
        [TestCase(13, 1, 40, true)]
        [TestCase(13, 2, 1, true)]
        [TestCase(25, 2, 1, false)]
        [TestCase(30, 2, 1, false)]
        public void YetiEligibilityUsesTheNativeProfileGate(int mode, int progress, int level, bool expected)
        {
            var context = new ModeWaveContext(12345, profileProgress: progress, profileLevel: level);
            var enabled = ModeWaveRules.BuildAllowed(ModeCatalog.Get(mode), 0, AdventureTerrain.Pool, context);
            Assert.That(enabled[19], Is.EqualTo(expected));
        }

        /// <summary>Checks impossible synthetic survival stages cannot enter an unbounded rejection loop.</summary>
        [Test]
        public void ImpossibleEligibilityRequestsFailExplicitly()
        {
            Assert.Throws<InvalidOperationException>(() => ModeWaveRules.BuildAllowed(ModeCatalog.Get(1), 7,
                AdventureTerrain.Day, new ModeWaveContext(12345)));
        }

        /// <summary>Checks invalid all-zero-weight input fails explicitly instead of inventing a Normal zombie or looping.</summary>
        [Test]
        public void EmptyPositiveWeightSelectionFailsWithoutDrawing()
        {
            var enabled = new bool[34];
            enabled[1] = true;
            var random = new OriginalRandomAdapter(12345);
            Assert.Throws<InvalidOperationException>(() => ModeWaveRules.Build(ModeCatalog.Get(1), 0,
                AdventureTerrain.Day, new ModeWaveContext(12345), random, enabled));
            Assert.That(random.Generator.DrawCount, Is.Zero);
        }

        /// <summary>Enumerates an eligibility array in native identifier order.</summary>
        /// <param name="enabled">The complete native eligibility flags.</param>
        /// <returns>The enabled original identifiers.</returns>
        private static int[] EnabledIds(IReadOnlyList<bool> enabled)
        {
            return Enumerable.Range(0, enabled.Count).Where(id => enabled[id]).ToArray();
        }

        /// <summary>Builds explicit native candidate inputs for focused branch tests.</summary>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="flags">The completed-flag count.</param>
        /// <param name="wave">The zero-based wave index.</param>
        /// <param name="budget">The remaining budget.</param>
        /// <param name="ids">The eligible original identifiers.</param>
        /// <param name="countedId">The identifier whose counters should be populated, or -1.</param>
        /// <param name="waveCount">The populated per-wave count.</param>
        /// <param name="roundCount">The populated per-stage count.</param>
        /// <returns>The native-ID-ordered candidates.</returns>
        private static IReadOnlyList<WeightedZombie> Candidates(int mode, int flags, int wave, int budget,
            int[] ids, int countedId = -1, int waveCount = 0, int roundCount = 0)
        {
            var enabled = new bool[34];
            foreach (var id in ids)
            {
                enabled[id] = true;
            }

            var waveCounts = new int[34];
            var roundCounts = new int[34];
            if (countedId >= 0)
            {
                waveCounts[countedId] = waveCount;
                roundCounts[countedId] = roundCount;
            }

            return ModeWaveRules.BuildCandidates(ModeCatalog.Get(mode), flags, wave, budget, enabled, waveCounts, roundCounts);
        }
    }
}
