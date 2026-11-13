#nullable enable
using PlantsVsZombies.Simulation;
using PlantsVsZombies.VerifiedData;
using PlantsVsZombies.Waves;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure wave plan.</summary>
    public static class AdventureWavePlan
    {
        /// <summary>Builds the original adventure wave table, including challenge eligibility and forced entries.</summary>
        /// <param name = "level">The level.</param>
        /// <param name = "random">The random.</param>
        /// <param name = "progressRaw">The progress raw.</param>
        /// <param name = "originalApplicationFlag">The original application flag.</param>
        /// <param name="context">Explicit native state whose application lifecycle is supplied by the caller.</param>
        /// <param name="enabledOutput">An optional 34-entry array receiving the final native selection mask.</param>
        /// <returns>The precomputed waves in their original insertion order.</returns>
        /// <exception cref="ArgumentNullException">The level or random source is null.</exception>
        /// <exception cref="ArgumentException">The output eligibility array does not contain 34 entries.</exception>
        public static IReadOnlyList<AdventureWave> Build(AdventureLevelSpec level, Random random, int progressRaw = 0, bool originalApplicationFlag = true, AdventureWaveGenerationContext context = default, bool[]? enabledOutput = null)
        {
            if (level == null || random == null)
            {
                throw new ArgumentNullException();
            }

            if (enabledOutput != null && enabledOutput.Length != AdventureWaveRules.ZombieTypeCount)
            {
                throw new ArgumentException("Expected 34 eligibility entries.", nameof(enabledOutput));
            }

            progressRaw = Math.Max(progressRaw, Math.Max(0, level.Cycle - 1));
            var defs = new List<ZombieWaveDefinition>();
            var enabled = new List<bool>();
            var tableEligible = new List<bool>();
            var profile = new AdventureProfileFields(true, Math.Max(0, progressRaw), level.Number);
            var raw = VerifiedDataCatalog.AdventureLevels[level.Number - 1];
            foreach (ZombieDefinition d in VerifiedDataCatalog.Zombies)
            {
                var z = new ZombieWaveDefinition(d.Id, d.WaveBudgetCostRaw, d.BaseLevelGateRaw, d.BaseWaveGateRaw, d.BaseSelectionWeightRaw);
                defs.Add(z);
                var eligible = AdventureWaveRules.IsTableEligible(z, level.Number, raw.ZombieFlagsRaw[d.Id], profile);
                tableEligible.Add(eligible);
                enabled.Add(eligible);
            }

            // PC 0x416860 routes these boards through Challenge::InitZombieWaves (0x430B50).
            // The final-wave coverage pass still uses 0x4193B0 rather than this challenge mask.
            if (originalApplicationFlag && (level.Number == 15 || level.Number == 5 && progressRaw > 0))
            {
                for (var id = 0; id < enabled.Count; id++)
                {
                    enabled[id] = level.Number == 15
                        ? id == 0 || id == 2 || id == 4
                        : id == 0 || id >= 2 && id <= 5 || id == 19 && tableEligible[id];
                }
            }

            if (enabledOutput != null)
            {
                for (var id = 0; id < enabled.Count; id++)
                {
                    enabledOutput[id] = enabled[id];
                }
            }

            var newZombieId = FindNewZombie(defs, level.Number, tableEligible);
            var waveCount = originalApplicationFlag && level.Number == 15 ? 8 : level.BaseWaveCount;
            if (!(originalApplicationFlag && level.Number == 15) && progressRaw > 0 && !(originalApplicationFlag && (level.Number == 10 || level.Number == 20 || level.Number == 30)))
            {
                waveCount = waveCount < 10 ? 20 : waveCount + 10;
            }

            var result = new List<AdventureWave>();
            for (var wave = 0; wave < waveCount; wave++)
            {
                var flag = AdventureWaveRules.IsFlagWave(level.Number, waveCount, wave, profile);
                var zombies = new List<ZombieKind>();
                var budget = AdventureWaveRules.PreAdjustmentBudget(level.Number, wave, profile);
                if (originalApplicationFlag && level.Number == 45 && flag)
                {
                    for (var i = 0; i < 5; i++)
                    {
                        zombies.Add(ZombieKind.Bungee);
                    }

                    if (wave != waveCount - 1)
                    {
                        result.Add(new AdventureWave { Index = wave, IsFlag = true, Zombies = zombies.AsReadOnly() });
                        continue;
                    }
                }

                if (flag)
                {
                    var fill = AdventureWaveRules.CalculateFlagPrefill(budget, defs[0], defs[1]);
                    for (var i = 0; i < fill.NormalCount; i++)
                    {
                        zombies.Add(ZombieKind.Normal);
                    }

                    zombies.Add(ZombieKind.Flag);
                    budget = fill.RemainingBudget;
                }

                // Native remaining-budget overrides at 0x4155FD..0x415720 (not just raw wave counts).
                budget *= !originalApplicationFlag ? 1 : level.Number == 5 || level.Number == 25 ? 4 : level.Number == 10 || level.Number == 20 || level.Number == 30 || level.Number == 40 ? 3 : level.Number == 45 ? 2 : 1;
                if (ShouldInsertNewZombie(newZombieId, wave, waveCount, context))
                {
                    zombies.Add((ZombieKind)newZombieId);
                    budget -= defs[newZombieId].BudgetCost;
                }

                if (wave == waveCount - 1)
                {
                    // PC 0x4157D9 inserts and charges the giant before the ordinary coverage pass.
                    if (level.Number == 50)
                    {
                        zombies.Add(ZombieKind.Gargantuar);
                        budget -= defs[(int)ZombieKind.Gargantuar].BudgetCost;
                    }

                    for (var id = 0; id < defs.Count; id++)
                    {
                        if (tableEligible[id] && id != 19 && !zombies.Contains((ZombieKind)id))
                        {
                            zombies.Add((ZombieKind)id);
                            budget -= defs[id].BudgetCost;
                        }
                    }
                }

                while (budget > 0 && zombies.Count < 50)
                {
                    var choices = AdventureWaveRules.BuildCandidates(defs, enabled, wave, budget);
                    var weight = AdventureWaveRules.TotalWeight(choices);
                    int chosen;
                    if (weight <= 0 || !AdventureWaveRules.TrySelectByTicket(choices, random.Next(weight), out chosen))
                    {
                        break;
                    }

                    zombies.Add((ZombieKind)chosen);
                    budget -= Math.Max(1, defs[chosen].BudgetCost);
                }

                if (zombies.Count == 0)
                {
                    zombies.Add(ZombieKind.Normal);
                }

                result.Add(new AdventureWave { Index = wave, IsFlag = flag, Zombies = zombies.AsReadOnly() });
            }

            return result.AsReadOnly();
        }

        /// <summary>Finds the first type introduced by this board, independently of its selection mask.</summary>
        /// <param name="definitions">The original type definitions in numeric ID order.</param>
        /// <param name="level">The one-based board level.</param>
        /// <param name="tableEligible">The ordinary profile-aware eligibility snapshot.</param>
        /// <returns>The first matching type ID, or minus one when this level introduces no eligible type.</returns>
        private static int FindNewZombie(IReadOnlyList<ZombieWaveDefinition> definitions, int level, IReadOnlyList<bool> tableEligible)
        {
            if (level == 1)
            {
                return -1;
            }

            for (var id = 0; id < definitions.Count; id++)
            {
                if (definitions[id].LevelGate == level && (id != 19 || tableEligible[id]))
                {
                    return id;
                }
            }

            return -1;
        }

        /// <summary>Applies the original newcomer wave gates and explicit forced-yeti suppression.</summary>
        /// <param name="zombieId">The type returned by the original newcomer lookup, or minus one.</param>
        /// <param name="wave">The zero-based wave being constructed.</param>
        /// <param name="waveCount">The complete number of waves.</param>
        /// <param name="context">The caller-supplied native state snapshot.</param>
        /// <returns>Whether the type must be inserted before ordinary random filling.</returns>
        private static bool ShouldInsertNewZombie(int zombieId, int wave, int waveCount, AdventureWaveGenerationContext context)
        {
            if (zombieId < 0 || zombieId == 10)
            {
                return false;
            }

            if (zombieId == 19)
            {
                return wave == waveCount / 2 && !context.SuppressForcedYeti;
            }

            if (zombieId == 16 || zombieId == 17)
            {
                return wave == 6 || wave == waveCount - 1;
            }

            return wave == waveCount / 2 || wave == waveCount - 1;
        }
    }
}
