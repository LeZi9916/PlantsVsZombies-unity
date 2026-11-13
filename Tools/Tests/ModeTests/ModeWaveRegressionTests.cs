#nullable enable

using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Waves;
using System;
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
            foreach (var mode in new[] { 22, 28, 29 })
            {
                Check(ModeCatalog.Get(mode).WaveCount == 40, "Native forty-wave mode " + mode);
            }
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
