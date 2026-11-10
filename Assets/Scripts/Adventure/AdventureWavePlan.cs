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
        /// <summary>Performs build.</summary>
        /// <param name = "level">The level.</param>
        /// <param name = "random">The random.</param>
        /// <param name = "progressRaw">The progress raw.</param>
        /// <param name = "originalApplicationFlag">The original application flag.</param>
        /// <returns>The resulting build.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static IReadOnlyList<AdventureWave> Build(AdventureLevelSpec level, Random random, int progressRaw = 0, bool originalApplicationFlag = true)
        {
            if (level == null || random == null)
            {
                throw new ArgumentNullException();
            }

            progressRaw = Math.Max(progressRaw, Math.Max(0, level.Cycle - 1));
            var defs = new List<ZombieWaveDefinition>();
            var enabled = new List<bool>();
            var profile = new AdventureProfileFields(true, Math.Max(0, progressRaw), level.Number);
            var raw = VerifiedDataCatalog.AdventureLevels[level.Number - 1];
            foreach (ZombieDefinition d in VerifiedDataCatalog.Zombies)
            {
                var z = new ZombieWaveDefinition(d.Id, d.WaveBudgetCostRaw, d.BaseLevelGateRaw, d.BaseWaveGateRaw, d.BaseSelectionWeightRaw);
                defs.Add(z);
                enabled.Add(AdventureWaveRules.IsTableEligible(z, level.Number, raw.ZombieFlagsRaw[d.Id], profile));
            }

            var waveCount = level.WaveCount;
            if (level.Mode != AdventureMode.WhackAZombie && progressRaw > 0 && !(originalApplicationFlag && (level.Number == 10 || level.Number == 20 || level.Number == 30)))
            {
                waveCount = waveCount < 10 ? 20 : waveCount + 10;
            }

            var result = new List<AdventureWave>();
            for (var wave = 0; wave < waveCount; wave++)
            {
                var flag = AdventureWaveRules.IsFlagWave(level.Number, waveCount, wave, profile);
                var zombies = new List<ZombieKind>();
                var budget = AdventureWaveRules.PreAdjustmentBudget(level.Number, wave, profile);
                if (level.Mode == AdventureMode.Bungee && flag)
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
                // Independent research agrees with the native forced-insertion notes in ida/waves/REPORT.md.
                for (var id = 2; id <= 24; id++)
                {
                    if (defs[id].LevelGate == level.Number && id != 10 && (id != 19 || enabled[19]) && (id == 19 ? wave == waveCount / 2 : (id == 16 || id == 17) ? wave == 6 || wave == waveCount - 1 : wave == waveCount / 2 || wave == waveCount - 1))
                    {
                        zombies.Add((ZombieKind)id);
                        budget -= defs[id].BudgetCost;
                        break;
                    }
                }

                if (wave == waveCount - 1)
                {
                    for (var id = 0; id <= 24; id++)
                    {
                        if (enabled[id] && id != 19 && !zombies.Contains((ZombieKind)id))
                        {
                            zombies.Add((ZombieKind)id);
                            budget -= defs[id].BudgetCost;
                        }
                    }

                    if (level.Number == 50 && !zombies.Contains(ZombieKind.Gargantuar))
                    {
                        zombies.Add(ZombieKind.Gargantuar);
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
    }
}
