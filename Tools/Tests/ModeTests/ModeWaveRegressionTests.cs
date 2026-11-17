#nullable enable

using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Waves;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlantsVsZombies.Tests.Modes
{
    /// <summary>Checks native independent-mode selection branches through production pure wave rules.</summary>
    internal static class ModeWaveRegressionTests
    {
        /// <summary>Runs machine-code-derived wave, eligibility, quota, and length regressions.</summary>
        /// <exception cref="InvalidOperationException">A native behavior regresses.</exception>
        internal static void Run()
        {
            var context = new ModeWaveContext(12345);
            var allowed = ModeWaveRules.BuildAllowed(ModeCatalog.Get(13), 5, AdventureTerrain.Pool, context);
            Check(Enumerable.Range(0, 34).Where(id => allowed[id]).SequenceEqual(new[] { 0, 1, 2, 5, 12, 17, 18, 25 }),
                "Native local MT and zero-weight survival slots");
            var waves = ModeWaveRules.Build(ModeCatalog.Get(1), 1, AdventureTerrain.Day, context, new OriginalRandomAdapter(12345));
            Check(waves[0].Zombies.Select(kind => (int)kind).SequenceEqual(new[] { 0, 2, 2 }), "Native doubled later-survival budget");
            var random = new OriginalRandomAdapter(12345);
            ModeWaveRules.Build(ModeCatalog.Get(1), 0, AdventureTerrain.Day, context, random);
            Check(random.Generator.DrawCount == 16, "Native first-wave weighted draw");
            var enabled = new bool[34];
            enabled[6] = true;
            var counts = new int[34];
            Check(ModeWaveRules.BuildCandidates(ModeCatalog.Get(13), 18, 3, 10, enabled, counts, counts).Count == 0,
                "Gate reduction remains zero at eighteen flags");
            Check(ModeWaveRules.BuildCandidates(ModeCatalog.Get(13), 20, 3, 10, enabled, counts, counts).Count == 1,
                "Gate reduction follows the eighteen-to-fifty-flag interval");
            Array.Clear(enabled, 0, enabled.Length);
            enabled[0] = true;
            enabled[2] = true;
            var weighted = ModeWaveRules.BuildCandidates(ModeCatalog.Get(13), 50, 19, 10, enabled, counts, counts);
            Check(weighted[0].Weight == 400 && weighted[1].Weight == 1000, "Native late-survival selection weights");
            Array.Clear(enabled, 0, enabled.Length);
            enabled[32] = true;
            var roundCounts = new int[34];
            roundCounts[32] = 1;
            Check(ModeWaveRules.BuildCandidates(ModeCatalog.Get(13), 10, 18, 100, enabled, counts, roundCounts).Count == 0,
                "Native ordinary-wave Redeye stage quota");
            Check(ModeWaveRules.BuildCandidates(ModeCatalog.Get(13), 10, 19, 100, enabled, counts, roundCounts).Count == 1,
                "Native flag-wave Redeye quota uses the current wave");
            var columns = ModeWaveRules.Build(ModeCatalog.Get(27), 0, AdventureTerrain.Roof, context, new OriginalRandomAdapter(12345));
            Check(columns[5].Zombies.Count(kind => (int)kind == 21) == 10, "Native forced Columns ladder wave");
            Check(columns[8].Zombies.Count(kind => (int)kind == 15) == 10, "Native forced Columns Jack-in-the-box wave");
            Check(columns[19].Zombies.Count(kind => (int)kind == 23) == 3
                && columns[29].Zombies.Count(kind => (int)kind == 23) == 5, "Native forced Columns Gargantuar waves");
            Check(ModeWaveRules.GenerationFlagsCompleted(ModeCatalog.Get(13), 0, 0) == 0,
                "Native first survival stage observes no completed flag");
            Check(ModeWaveRules.GenerationFlagsCompleted(ModeCatalog.Get(13), 6, 20) == 13,
                "Native twenty-wave re-selection counts the previous round's flag wave");
            Check(ModeWaveRules.GenerationFlagsCompleted(ModeCatalog.Get(13), 6, 0) == 12,
                "Native fresh twenty-wave board adds no extra flag");
            Check(ModeWaveRules.GenerationFlagsCompleted(ModeCatalog.Get(1), 6, 10) == 6,
                "Native ten-wave re-selection adds no extra flag");
            Check(ModeWaveRules.GenerationFlagsCompleted(ModeCatalog.Get(13), 6, 13) == 13,
                "Native mid-round wave index still floors the same flag count");
            var quota = new bool[34];
            quota[0] = true;
            quota[23] = true;
            var quotaWaves = ModeWaveRules.Build(ModeCatalog.Get(13), 6, AdventureTerrain.Pool, context,
                new OriginalRandomAdapter(12345), quota, previousWaveIndex: 20);
            Check(quotaWaves[19].Zombies.Count(kind => (int)kind == 23) == 6,
                "Native re-selection Gargantuar quota uses the previous round's completed flags");
            var freshWaves = ModeWaveRules.Build(ModeCatalog.Get(13), 6, AdventureTerrain.Pool, context,
                new OriginalRandomAdapter(12345), quota);
            Check(freshWaves[19].Zombies.Count(kind => (int)kind == 23) == 4,
                "Native fixture contract keeps the fresh-board quota");
            var wiringSession = new ModeSession(13);
            var wiringContext = new ModeWaveContext(12345);
            var differentiated = false;
            for (var candidate = 2; candidate < 30 && !differentiated; candidate++)
            {
                var wiringAllowed = ModeWaveRules.BuildAllowed(ModeCatalog.Get(13), candidate, AdventureTerrain.Pool, wiringContext);
                var freshPlan = ModeWaveRules.Build(ModeCatalog.Get(13), candidate, AdventureTerrain.Pool, wiringContext,
                    new OriginalRandomAdapter(99), wiringAllowed);
                var reSelectionPlan = ModeWaveRules.Build(ModeCatalog.Get(13), candidate, AdventureTerrain.Pool, wiringContext,
                    new OriginalRandomAdapter(99), wiringAllowed, previousWaveIndex: 20);
                if (SamePlan(freshPlan, reSelectionPlan))
                {
                    continue;
                }

                differentiated = true;
                wiringSession.Stage = candidate + 1;
                var wired = ModeWavePlan.Build(wiringSession, new OriginalRandomAdapter(99));
                Check(SamePlan(wired, reSelectionPlan), "Mode plan forwards the native re-selection wave index");
                Check(!SamePlan(wired, freshPlan), "Mode plan no longer generates the fresh-board table on a re-selection");
            }

            Check(differentiated, "A later survival stage differentiates the native wave index");
            foreach (var mode in new[] { 22, 28, 29 })
            {
                Check(ModeCatalog.Get(mode).WaveCount == 40, "Native forty-wave mode " + mode);
            }
        }

        /// <summary>Compares two generated wave tables by wave order and zombie identifiers.</summary>
        /// <param name="left">The first native plan.</param>
        /// <param name="right">The second native plan.</param>
        /// <returns>Whether both plans contain the same waves.</returns>
        private static bool SamePlan(IReadOnlyList<AdventureWave> left, IReadOnlyList<AdventureWave> right)
        {
            return left.Count == right.Count
                && left.Select((wave, index) => wave.Zombies.SequenceEqual(right[index].Zombies)).All(value => value);
        }

        /// <summary>Fails the executable regression process when an expected native result differs.</summary>
        /// <param name="condition">Whether the expected behavior is present.</param>
        /// <param name="message">The behavior being checked.</param>
        /// <exception cref="InvalidOperationException">The condition is false.</exception>
        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
