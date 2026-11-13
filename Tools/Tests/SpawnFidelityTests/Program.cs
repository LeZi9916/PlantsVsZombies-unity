#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Waves;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PlantsVsZombies.Tests.SpawnFidelity
{
    /// <summary>Checks production generators against committed outputs captured from the supplied PC executable.</summary>
    internal static class Program
    {
        /// <summary>Counts successful independent equality assertions.</summary>
        private static int s_checks;

        /// <summary>Runs complete local wave plans and controlled candidate-input comparisons.</summary>
        /// <returns>Zero when all original-machine-code fixtures match.</returns>
        private static int Main()
        {
            var directory = Path.Combine(OriginalReferencePaths.RepositoryRoot(), "Tools", "Tests", "Modules", "Spawn");
            CheckAdventure(Path.Combine(directory, "adventure-wave-native.json"));
            CheckModes(Path.Combine(directory, "mode-wave-native.json"));
            Console.WriteLine("PASS: " + s_checks + " original-PC spawn assertions; local generator fixtures, not full game replay.");
            return 0;
        }

        /// <summary>Checks every adventure board's full zero-ticket plan, final eligibility, and bounded draw sequence.</summary>
        /// <param name="path">The committed native fixture path.</param>
        private static void CheckAdventure(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var entry in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var level = entry.GetProperty("level").GetInt32();
                var progress = entry.GetProperty("progress").GetInt32();
                var enabled = new bool[34];
                var random = new ZeroTicketRandom();
                var waves = AdventureWavePlan.Build(AdventureLevels.ForRun(level, progress + 1), random, enabledOutput: enabled);
                var prefix = "adventure " + level + ", progress " + progress;
                Equal(WaveJson(waves), entry.GetProperty("waves").GetRawText(), prefix + " waves");
                Equal(JsonSerializer.Serialize(random.Bounds), entry.GetProperty("bounds").GetRawText(), prefix + " draw bounds");
                Equal(JsonSerializer.Serialize(EnabledIds(enabled)), entry.GetProperty("enabled").GetRawText(), prefix + " eligibility");
            }
        }

        /// <summary>Checks original local survival/minigame plans, MT draw counts, and candidate branch boundaries.</summary>
        /// <param name="path">The committed native fixture path.</param>
        private static void CheckModes(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var entry in document.RootElement.GetProperty("plans").EnumerateArray())
            {
                var modeId = entry.GetProperty("mode").GetInt32();
                var stage = entry.GetProperty("stage").GetInt32();
                var seed = entry.GetProperty("seed").GetUInt32();
                var mode = ModeCatalog.Get(modeId);
                var terrain = (AdventureTerrain)entry.GetProperty("terrain").GetInt32();
                var context = new ModeWaveContext(seed);
                var enabled = ModeWaveRules.BuildAllowed(mode, stage, terrain, context);
                var random = new OriginalRandomAdapter(seed);
                var waves = ModeWaveRules.Build(mode, stage, terrain, context, random, enabled);
                var prefix = "mode " + modeId + ", stage " + stage + ", seed " + seed;
                Equal(JsonSerializer.Serialize(EnabledIds(enabled)), entry.GetProperty("allowed").GetRawText(), prefix + " eligibility");
                Equal(waves.Count, entry.GetProperty("wave_count").GetInt32(), prefix + " wave count");
                using var sha = SHA256.Create();
                var digest = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(WaveJson(waves)))).Replace("-", "").ToLowerInvariant();
                Equal(digest, entry.GetProperty("sha256").GetString() ?? string.Empty, prefix + " complete plan digest");
                Equal(random.Generator.DrawCount, entry.GetProperty("draws").GetUInt64(), prefix + " global MT draws");
            }

            foreach (var entry in document.RootElement.GetProperty("eligibility").EnumerateArray())
            {
                var modeId = entry.GetProperty("mode").GetInt32();
                var stage = entry.GetProperty("stage").GetInt32();
                var context = new ModeWaveContext(entry.GetProperty("seed").GetUInt32(),
                    entry.GetProperty("board_seed_offset").GetUInt32(), entry.GetProperty("profile_progress").GetInt32(),
                    entry.GetProperty("profile_level").GetInt32(), specialRulesEnabled: entry.GetProperty("special_rules").GetBoolean());
                var actual = ModeWaveRules.BuildAllowed(ModeCatalog.Get(modeId), stage,
                    (AdventureTerrain)entry.GetProperty("terrain").GetInt32(), context);
                Equal(JsonSerializer.Serialize(EnabledIds(actual)), entry.GetProperty("allowed").GetRawText(),
                    "mode " + modeId + " profile eligibility");
            }

            foreach (var entry in document.RootElement.GetProperty("candidates").EnumerateArray())
            {
                var modeId = entry.GetProperty("mode").GetInt32();
                var enabled = new bool[34];
                foreach (var id in entry.GetProperty("enabled").EnumerateArray())
                {
                    enabled[id.GetInt32()] = true;
                }

                var waveCounts = ReadCounts(entry.GetProperty("wave_counts"));
                var roundCounts = ReadCounts(entry.GetProperty("round_counts"));
                var actual = ModeWaveRules.BuildCandidates(ModeCatalog.Get(modeId), entry.GetProperty("flags").GetInt32(),
                    entry.GetProperty("wave").GetInt32(), entry.GetProperty("budget").GetInt32(), enabled, waveCounts, roundCounts);
                var pairs = new int[actual.Count][];
                for (var i = 0; i < actual.Count; i++)
                {
                    pairs[i] = new[] { actual[i].ZombieId, actual[i].Weight };
                }

                Equal(JsonSerializer.Serialize(pairs), entry.GetProperty("pairs").GetRawText(), "mode " + modeId + " candidate boundary");
            }
        }

        /// <summary>Decodes sparse original per-type insertion counters.</summary>
        /// <param name="source">Pairs containing the original numeric type and count.</param>
        /// <returns>The native 34-element counter array.</returns>
        private static int[] ReadCounts(JsonElement source)
        {
            var counts = new int[34];
            foreach (var pair in source.EnumerateArray())
            {
                counts[pair[0].GetInt32()] = pair[1].GetInt32();
            }

            return counts;
        }

        /// <summary>Extracts enabled numeric identifiers in native table order.</summary>
        /// <param name="enabled">The original board's eligibility flags.</param>
        /// <returns>Enabled numeric identifiers.</returns>
        private static List<int> EnabledIds(IReadOnlyList<bool> enabled)
        {
            var result = new List<int>();
            for (var i = 0; i < enabled.Count; i++)
            {
                if (enabled[i])
                {
                    result.Add(i);
                }
            }

            return result;
        }

        /// <summary>Serializes ordered wave identifiers using the native fixture's compact UTF-8 JSON convention.</summary>
        /// <param name="waves">The complete ordered production wave table.</param>
        /// <returns>A compact JSON array of per-wave integer arrays.</returns>
        private static string WaveJson(IReadOnlyList<AdventureWave> waves)
        {
            var result = new int[waves.Count][];
            for (var wave = 0; wave < waves.Count; wave++)
            {
                var source = waves[wave].Zombies;
                result[wave] = new int[source.Count];
                for (var slot = 0; slot < source.Count; slot++)
                {
                    result[wave][slot] = (int)source[slot];
                }
            }

            return JsonSerializer.Serialize(result);
        }

        /// <summary>Checks an exact expected value and records a successful assertion.</summary>
        /// <typeparam name="T">The compared value type.</typeparam>
        /// <param name="actual">The production result.</param>
        /// <param name="expected">The independently captured native result.</param>
        /// <param name="context">The scenario to report on failure.</param>
        /// <exception cref="InvalidOperationException">Production and native values differ.</exception>
        private static void Equal<T>(T actual, T expected, string context)
        {
            if (!EqualityComparer<T>.Default.Equals(actual, expected))
            {
                throw new InvalidOperationException(context + ": expected " + expected + ", got " + actual);
            }

            s_checks++;
        }

        /// <summary>Supplies zero tickets while preserving each original bounded-random call.</summary>
        private sealed class ZeroTicketRandom : Random
        {
            /// <summary>Gets exclusive upper bounds in production call order.</summary>
            internal List<int> Bounds { get; } = new List<int>();

            /// <summary>Records a valid native selection bound and returns its zero ticket.</summary>
            /// <param name="maxValue">The exclusive positive selection bound.</param>
            /// <returns>The zero ticket.</returns>
            public override int Next(int maxValue)
            {
                Bounds.Add(maxValue);
                return 0;
            }
        }
    }
}
