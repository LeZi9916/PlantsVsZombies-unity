#nullable enable

using PlantsVsZombies.Adventure;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.VerifiedData;
using PlantsVsZombies.Waves;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Modes
{
    /// <summary>Implements the supplied PC executable's independent-mode eligibility and wave generator.</summary>
    public static class ModeWaveRules
    {
        /// <summary>Stores PC 0x430B50's fixed eligibility lists for mode identifiers 16 through 35.</summary>
        private static readonly int[][] s_fixedLineups =
        {
            new[] { 26, 27 },
            new[] { 0, 2, 3, 4, 5 },
            new[] { 0, 2, 4 },
            new[] { 0, 2, 4, 5, 6, 7, 15, 20 },
            new[] { 0, 2, 4, 5, 6, 7 },
            new[] { 0, 2, 4, 12, 14, 15 },
            new[] { 0, 2, 4 },
            new[] { 0, 2, 4 },
            new[] { 0, 2, 4, 5, 6, 7 },
            new[] { 0, 2, 7, 11 },
            new[] { 0, 4, 7, 16 },
            new[] { 0, 2, 4, 7 },
            new[] { 12, 13 },
            new[] { 0, 2, 3, 14 },
            new[] { 0, 2, 4 },
            new[] { 0, 2, 3, 4, 5, 6, 7, 14, 15, 21 },
            new[] { 26, 27, 28, 29, 30, 31 },
            new[] { 0, 2, 3, 4, 5, 6, 8 },
            new[] { 18 },
            new[] { 0, 2, 4 }
        };

        /// <summary>Generates the original eligibility flags without consuming the shared combat random stream.</summary>
        /// <param name="mode">The independent-mode definition.</param>
        /// <param name="stageIndex">The zero-based survival stage.</param>
        /// <param name="terrain">The board terrain used by native pool, roof, and grave restrictions.</param>
        /// <param name="context">The explicit native profile and seed inputs.</param>
        /// <returns>All 34 eligibility flags, including zero-weight original identifiers.</returns>
        /// <exception cref="ArgumentNullException">A required definition or context is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The stage index is negative.</exception>
        /// <exception cref="InvalidOperationException">A later survival stage has no profile or requests an impossible number of types.</exception>
        public static bool[] BuildAllowed(ModeDefinition mode, int stageIndex, AdventureTerrain terrain, ModeWaveContext context)
        {
            ValidateInputs(mode, stageIndex, context);
            var enabled = new bool[AdventureWaveRules.ZombieTypeCount];
            if (mode.Survival)
            {
                enabled[0] = true;
                if (stageIndex == 0)
                {
                    enabled[2] = true;
                    enabled[4] = mode.Hard;
                }
                else
                {
                    if (!context.HasPlayer)
                    {
                        throw new InvalidOperationException("The original survival eligibility seed requires a player profile.");
                    }

                    var localRandom = new OriginalRandom(context.DeriveSeed(mode.Id, stageIndex));
                    enabled[localRandom.NextBounded(5) == 0 ? 5 : 2] = true;
                    var remaining = Math.Min(stageIndex + 1, 9);
                    var flagsCompleted = stageIndex * mode.FlagsPerStage;
                    var available = 0;
                    for (var id = 0; id < enabled.Length; id++)
                    {
                        if (!enabled[id] && IsSurvivalTypeAllowed(mode, terrain, flagsCompleted, id))
                        {
                            available++;
                        }
                    }

                    if (remaining > available)
                    {
                        throw new InvalidOperationException("The survival stage requests more eligible zombie types than the board permits.");
                    }

                    while (remaining > 0)
                    {
                        var id = (int)localRandom.NextBounded(AdventureWaveRules.ZombieTypeCount);
                        if (enabled[id] || !IsSurvivalTypeAllowed(mode, terrain, flagsCompleted, id))
                        {
                            continue;
                        }

                        enabled[id] = true;
                        remaining--;
                    }
                }
            }
            else
            {
                var ids = mode.Id >= 16 && mode.Id <= 35 ? s_fixedLineups[mode.Id - 16] : s_fixedLineups[2];
                if (mode.Id == 25 && !context.SpecialRulesEnabled)
                {
                    ids = s_fixedLineups[2];
                }

                for (var i = 0; i < ids.Length; i++)
                {
                    enabled[ids[i]] = true;
                }
            }

            var yeti = VerifiedDataCatalog.Zombies[19];
            if (context.HasPlayer && context.ProfileProgress > 0
                && (context.ProfileProgress >= 2 || context.ProfileLevel >= yeti.BaseLevelGateRaw)
                && (!context.SpecialRulesEnabled || (mode.Id != 25 && mode.Id != 30)))
            {
                enabled[19] = true;
            }

            return enabled;
        }

        /// <summary>Builds the finite wave table, preserving the native weighted-draw order and forced insertions.</summary>
        /// <param name="mode">The independent-mode definition.</param>
        /// <param name="stageIndex">The zero-based survival or Last Stand stage.</param>
        /// <param name="terrain">The board terrain used when eligibility has not already been supplied.</param>
        /// <param name="context">The explicit native profile and seed inputs.</param>
        /// <param name="random">The shared random stream used by PC 0x5267C0.</param>
        /// <param name="enabled">Optional eligibility flags already generated for the board.</param>
        /// <returns>The original wave table; special controllers retain their catalog's empty table.</returns>
        /// <exception cref="ArgumentNullException">A required definition, context, or random source is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The stage index is negative.</exception>
        /// <exception cref="ArgumentException">An eligibility array does not contain 34 entries.</exception>
        /// <exception cref="InvalidOperationException">No positive-weight candidate exists for a positive budget.</exception>
        public static IReadOnlyList<AdventureWave> Build(ModeDefinition mode, int stageIndex, AdventureTerrain terrain,
            ModeWaveContext context, Random random, IReadOnlyList<bool>? enabled = null)
        {
            ValidateInputs(mode, stageIndex, context);
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            enabled ??= BuildAllowed(mode, stageIndex, terrain, context);
            ValidateCounts(enabled.Count, nameof(enabled));
            var waveCount = mode.Id == 25 && !context.SpecialRulesEnabled ? 40 : mode.WaveCount;
            var waves = new List<AdventureWave>(waveCount);
            var waveCounts = new int[AdventureWaveRules.ZombieTypeCount];
            var roundCounts = new int[AdventureWaveRules.ZombieTypeCount];
            var candidates = new List<WeightedZombie>(AdventureWaveRules.ZombieTypeCount);
            var flagsCompleted = stageIndex * mode.FlagsPerStage;
            for (var waveIndex = 0; waveIndex < waveCount; waveIndex++)
            {
                Array.Clear(waveCounts, 0, waveCounts.Length);
                var flag = waveIndex % 10 == 9;
                var budget = PreAdjustmentBudget(mode, stageIndex, waveIndex);
                var zombies = new List<ZombieKind>(50);
                if (flag)
                {
                    var normalCount = Math.Min(budget, 8);
                    budget = (int)(budget * 2.5);
                    if (mode.Id != 16 && mode.Id != 32)
                    {
                        AddRepeated(zombies, 0, normalCount, waveCounts, roundCounts, ref budget);
                        AddRepeated(zombies, 1, 1, waveCounts, roundCounts, ref budget);
                    }
                }

                budget *= BudgetMultiplier(mode.Id, context.SpecialRulesEnabled);
                if (mode.Id == 27)
                {
                    if (waveIndex % 10 == 5)
                    {
                        AddRepeated(zombies, 21, 10, waveCounts, roundCounts, ref budget);
                    }

                    if (waveIndex % 10 == 8)
                    {
                        AddRepeated(zombies, 15, 10, waveCounts, roundCounts, ref budget);
                    }

                    if (waveIndex == 19 || waveIndex == 29)
                    {
                        AddRepeated(zombies, 23, waveIndex == 19 ? 3 : 5, waveCounts, roundCounts, ref budget);
                    }
                }

                while (budget > 0 && zombies.Count < 50)
                {
                    FillCandidates(mode, flagsCompleted, waveIndex, budget, enabled, waveCounts, roundCounts, candidates);
                    var total = AdventureWaveRules.TotalWeight(candidates);
                    if (total <= 0)
                    {
                        throw new InvalidOperationException("No positive-weight zombie candidate exists for mode " + mode.Id
                            + ", stage " + stageIndex + ", wave " + waveIndex + ", budget " + budget + ".");
                    }

                    AdventureWaveRules.TrySelectByTicket(candidates, random.Next(total), out var chosen);
                    AddRepeated(zombies, chosen, 1, waveCounts, roundCounts, ref budget);
                }

                waves.Add(new AdventureWave { Index = waveIndex, IsFlag = flag, Zombies = zombies.AsReadOnly() });
            }

            return waves.AsReadOnly();
        }

        /// <summary>Enumerates PC 0x4195B0 candidates with native mode, flag-count, and quota exceptions.</summary>
        /// <param name="mode">The independent-mode definition.</param>
        /// <param name="flagsCompleted">The native completed-flag count at generation time.</param>
        /// <param name="waveIndex">The zero-based wave being generated.</param>
        /// <param name="remainingBudget">The remaining native budget, which special modes may bypass.</param>
        /// <param name="enabled">The 34 board eligibility flags.</param>
        /// <param name="waveCounts">The 34 counts already inserted into this wave.</param>
        /// <param name="roundCounts">The 34 counts already inserted into the current stage's table.</param>
        /// <returns>Candidates in original ID order, retaining zero-weight entries.</returns>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        /// <exception cref="ArgumentException">A count or eligibility list does not contain 34 entries.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The flag count or wave index is negative.</exception>
        public static IReadOnlyList<WeightedZombie> BuildCandidates(ModeDefinition mode, int flagsCompleted, int waveIndex,
            int remainingBudget, IReadOnlyList<bool> enabled, IReadOnlyList<int> waveCounts, IReadOnlyList<int> roundCounts)
        {
            if (mode == null || enabled == null || waveCounts == null || roundCounts == null)
            {
                throw new ArgumentNullException(mode == null ? nameof(mode) : enabled == null ? nameof(enabled)
                    : waveCounts == null ? nameof(waveCounts) : nameof(roundCounts));
            }

            if (flagsCompleted < 0 || waveIndex < 0)
            {
                throw new ArgumentOutOfRangeException(flagsCompleted < 0 ? nameof(flagsCompleted) : nameof(waveIndex));
            }

            ValidateCounts(enabled.Count, nameof(enabled));
            ValidateCounts(waveCounts.Count, nameof(waveCounts));
            ValidateCounts(roundCounts.Count, nameof(roundCounts));
            var result = new List<WeightedZombie>(AdventureWaveRules.ZombieTypeCount);
            FillCandidates(mode, flagsCompleted, waveIndex, remainingBudget, enabled, waveCounts, roundCounts, result);
            return result.AsReadOnly();
        }

        /// <summary>Checks PC 0x4309A0's restrictions without removing eligible zero-weight identifiers.</summary>
        /// <param name="mode">The survival-mode definition.</param>
        /// <param name="terrain">The active board terrain.</param>
        /// <param name="flagsCompleted">The completed survival flags.</param>
        /// <param name="id">The original zombie identifier.</param>
        /// <returns>Whether the identifier may occupy a survival eligibility slot.</returns>
        private static bool IsSurvivalTypeAllowed(ModeDefinition mode, AdventureTerrain terrain, int flagsCompleted, int id)
        {
            var pool = terrain == AdventureTerrain.Pool || terrain == AdventureTerrain.Fog;
            var roof = terrain == AdventureTerrain.Roof || terrain == AdventureTerrain.BossRoof;
            if ((id == 11 || id == 14) && !pool || roof && (id == 17 || id == 8)
                || terrain == AdventureTerrain.Night && id == 12 || !roof && !mode.Endless && id == 20)
            {
                return false;
            }

            if (flagsCompleted < 4 && (id == 23 || id == 12) || flagsCompleted < 10 && id == 32
                || mode.Id <= 3 && id > 11)
            {
                return false;
            }

            return id != 9 && id != 10 && id != 13 && id != 19 && id != 24
                && id != 26 && id != 27 && id != 28 && id != 29 && id != 30 && id != 31;
        }

        /// <summary>Fills a reusable native-ID-ordered candidate list for one weighted draw.</summary>
        /// <param name="mode">The independent-mode definition.</param>
        /// <param name="flagsCompleted">The native completed-flag count.</param>
        /// <param name="waveIndex">The zero-based wave being generated.</param>
        /// <param name="remainingBudget">The remaining budget.</param>
        /// <param name="enabled">The eligibility array.</param>
        /// <param name="waveCounts">Counts already inserted into this wave.</param>
        /// <param name="roundCounts">Counts already inserted into this stage.</param>
        /// <param name="result">The candidate buffer to clear and fill.</param>
        private static void FillCandidates(ModeDefinition mode, int flagsCompleted, int waveIndex, int remainingBudget,
            IReadOnlyList<bool> enabled, IReadOnlyList<int> waveCounts, IReadOnlyList<int> roundCounts, List<WeightedZombie> result)
        {
            result.Clear();
            var flag = waveIndex % 10 == 9;
            for (var id = 0; id < AdventureWaveRules.ZombieTypeCount; id++)
            {
                if (!enabled[id])
                {
                    continue;
                }

                var definition = VerifiedDataCatalog.Zombies[id];
                var endlessBungee = mode.Survival && mode.Endless && id == 20;
                if (endlessBungee && !flag)
                {
                    continue;
                }

                if (!endlessBungee && mode.Id != 28 && mode.Id != 34 && mode.Id != 41)
                {
                    var gate = definition.BaseWaveGateRaw;
                    if (mode.Survival && mode.Endless)
                    {
                        gate = Math.Max(1, gate - LinearCurve(flagsCompleted, 18, 50, 0, 15));
                    }

                    if (waveIndex + 1 < gate || remainingBudget < definition.WaveBudgetCostRaw)
                    {
                        continue;
                    }
                }

                var weight = definition.BaseSelectionWeightRaw;
                if (mode.Survival)
                {
                    if ((id == 12 || id == 23) && waveCounts[id] >= LinearCurve(flagsCompleted, 10, 50, 2, 50))
                    {
                        continue;
                    }

                    if (id == 32)
                    {
                        if (flag && waveCounts[id] >= LinearCurve(flagsCompleted, 14, 100, 1, 50)
                            || !flag && roundCounts[id] >= LinearCurve(flagsCompleted, 10, 110, 1, 50))
                        {
                            continue;
                        }

                        if (!flag)
                        {
                            weight = 1000;
                        }
                    }
                    else if (id == 0 || id == 2)
                    {
                        weight = LinearCurve(flagsCompleted, 10, 50, weight, weight / (id == 0 ? 10 : 4));
                    }
                }

                result.Add(new WeightedZombie(id, weight));
            }
        }

        /// <summary>Samples PC 0x526F20's clamped linear float interpolation and integer rounding.</summary>
        /// <param name="value">The current flag count.</param>
        /// <param name="start">The interpolation's starting flag count.</param>
        /// <param name="end">The interpolation's ending flag count.</param>
        /// <param name="startValue">The value before the interval.</param>
        /// <param name="endValue">The value after the interval.</param>
        /// <returns>The native rounded interpolation result.</returns>
        internal static int LinearCurve(int value, int start, int end, int startValue, int endValue)
        {
            if (value <= start)
            {
                return startValue;
            }

            if (value >= end)
            {
                return endValue;
            }

            var fraction = (float)((double)(value - start) / (end - start));
            var result = (float)(startValue + (((double)endValue - startValue) * fraction));
            return (int)(result + (result > 0f ? 0.5 : -0.5));
        }

        /// <summary>Computes PC 0x4150C0's budget before flag and special-mode processing.</summary>
        /// <param name="mode">The independent-mode definition.</param>
        /// <param name="stageIndex">The zero-based stage.</param>
        /// <param name="waveIndex">The zero-based wave.</param>
        /// <returns>The unadjusted native wave budget.</returns>
        private static int PreAdjustmentBudget(ModeDefinition mode, int stageIndex, int waveIndex)
        {
            if (mode.Id == 31)
            {
                return ((2 * (waveIndex + (stageIndex * 10)) + 20) / 5) + 1;
            }

            return mode.Survival && stageIndex > 0 ? ((2 * (waveIndex + (stageIndex * mode.WaveCount))) / 5) + 1 : (waveIndex / 3) + 1;
        }

        /// <summary>Gets PC 0x4155E6's multiplier applied after flag prefill costs.</summary>
        /// <param name="modeId">The original mode identifier.</param>
        /// <param name="specialRulesEnabled">Whether application +0x868 branches are enabled.</param>
        /// <returns>The multiplier for the remaining budget.</returns>
        private static int BudgetMultiplier(int modeId, bool specialRulesEnabled)
        {
            if (modeId == 27)
            {
                return 6;
            }

            if (specialRulesEnabled && (modeId == 17 || modeId == 25 || modeId == 33))
            {
                return 4;
            }

            return modeId == 21 || modeId == 26 ? 2 : 1;
        }

        /// <summary>Inserts forced or selected identifiers, debits their original costs, and updates both native counters.</summary>
        /// <param name="zombies">The current wave's identifier list.</param>
        /// <param name="id">The identifier to insert.</param>
        /// <param name="count">The number of insertions.</param>
        /// <param name="waveCounts">The per-wave counts.</param>
        /// <param name="roundCounts">The stage's cumulative counts.</param>
        /// <param name="budget">The remaining budget to debit.</param>
        private static void AddRepeated(List<ZombieKind> zombies, int id, int count, int[] waveCounts, int[] roundCounts, ref int budget)
        {
            for (var i = 0; i < count; i++)
            {
                zombies.Add((ZombieKind)id);
                budget -= VerifiedDataCatalog.Zombies[id].WaveBudgetCostRaw;
                waveCounts[id]++;
                roundCounts[id]++;
            }
        }

        /// <summary>Validates the shared independent-mode generator inputs.</summary>
        /// <param name="mode">The independent-mode definition.</param>
        /// <param name="stageIndex">The zero-based stage.</param>
        /// <param name="context">The explicit native profile context.</param>
        /// <exception cref="ArgumentNullException">A required definition or context is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The stage index is negative.</exception>
        private static void ValidateInputs(ModeDefinition mode, int stageIndex, ModeWaveContext context)
        {
            if (mode == null || context == null)
            {
                throw new ArgumentNullException(mode == null ? nameof(mode) : nameof(context));
            }

            if (stageIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stageIndex));
            }
        }

        /// <summary>Checks the native table's fixed identifier count.</summary>
        /// <param name="count">The supplied list length.</param>
        /// <param name="parameterName">The invalid argument's name.</param>
        /// <exception cref="ArgumentException">The list does not have 34 entries.</exception>
        private static void ValidateCounts(int count, string parameterName)
        {
            if (count != AdventureWaveRules.ZombieTypeCount)
            {
                throw new ArgumentException("Expected 34 original zombie identifiers.", parameterName);
            }
        }
    }
}
