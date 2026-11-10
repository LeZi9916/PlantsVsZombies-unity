#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Waves
{
    /// <summary>
    /// Independently usable fragments of mode-0 selection. These do not generate
    /// a full wave, implement special-level setup, or reproduce the EXE RNG.
    /// </summary>
    public static class AdventureWaveRules
    {
        /// <summary>Stores zombie type count.</summary>
        public const int ZombieTypeCount = 34;
        /// <summary>Stores adventure level count.</summary>
        public const int AdventureLevelCount = 50;
        /// <summary>The table index used for a positive, 1-based level.</summary>
        public static int TableIndex(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException("level");
            }

            return Math.Min(level, AdventureLevelCount) - 1;
        }

        /// <summary>
        /// The predicate at 0x4193B0. For ID 19, pass the global application's
        /// profile snapshot; the matrix flag and requested level are not consulted.
        /// Special-level setup can bypass this predicate entirely.
        /// </summary>
        public static bool IsTableEligible(ZombieWaveDefinition zombie, int level, int matrixFlag, AdventureProfileFields profile)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException("level");
            }

            if (zombie.Id == 19)
            {
                return profile.HasProfile && profile.ProgressRaw > 0 && (profile.ProgressRaw >= 2 || profile.LevelRaw >= zombie.LevelGate);
            }

            return level >= zombie.LevelGate && zombie.SelectionWeight != 0 && matrixFlag != 0;
        }

        /// <summary>
        /// Mode-0 flag predicate at 0x414E50. A nonnegative index beyond the
        /// current list is accepted because the EXE also queries the next wave.
        /// </summary>
        public static bool IsFlagWave(int level, int waveCount, int waveIndex, AdventureProfileFields profile)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException("level");
            }

            if (waveCount <= 0)
            {
                throw new ArgumentOutOfRangeException("waveCount");
            }

            if (waveIndex < 0)
            {
                throw new ArgumentOutOfRangeException("waveIndex");
            }

            var progressed = profile.HasProfile && profile.ProgressRaw > 0;
            if (!progressed && level == 1)
            {
                return false;
            }

            var period = progressed || waveCount >= 10 ? 10 : waveCount;
            return waveIndex % period == period - 1;
        }

        /// <summary>
        /// Mode-0 budget before flag additions, special-level multipliers,
        /// forced zombies, and selection. Supports the 100 allocated wave rows.
        /// </summary>
        public static int PreAdjustmentBudget(int level, int waveIndex, AdventureProfileFields profile)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException("level");
            }

            ValidateWaveIndex(waveIndex);
            return profile.HasProfile && profile.ProgressRaw > 0 && level != 5 ? 2 * waveIndex / 5 + 1 : waveIndex / 3 + 1;
        }

        /// <summary>
        /// Fixed mode-0 flag prefill, starting with an empty wave: append ID 0
        /// NormalCount times, then ID 1 once. Apply special-level processing and
        /// other forced insertions separately. Tested for budgets 1 through 40.
        /// </summary>
        public static FlagWavePrefill CalculateFlagPrefill(int preAdjustmentBudget, ZombieWaveDefinition normal, ZombieWaveDefinition flag)
        {
            if (preAdjustmentBudget < 1 || preAdjustmentBudget > 40)
            {
                throw new ArgumentOutOfRangeException("preAdjustmentBudget");
            }

            if (normal.Id != 0)
            {
                throw new ArgumentException("Expected zombie ID 0.", "normal");
            }

            if (flag.Id != 1)
            {
                throw new ArgumentException("Expected zombie ID 1.", "flag");
            }

            var normalCount = Math.Min(preAdjustmentBudget, 8);
            var remaining = checked(preAdjustmentBudget * 5 / 2 - checked(normalCount * normal.BudgetCost) - flag.BudgetCost);
            return new FlagWavePrefill(normalCount, remaining);
        }

        /// <summary>
        /// Filters an already-initialized board eligibility array for mode 0.
        /// The definition and enabled arrays must contain IDs 0..33 in order.
        /// Zero-weight candidates are retained exactly as in the native list.
        /// </summary>
        public static IReadOnlyList<WeightedZombie> BuildCandidates(IReadOnlyList<ZombieWaveDefinition> definitions, IReadOnlyList<bool> enabled, int waveIndex, int remainingBudget)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException("definitions");
            }

            if (enabled == null)
            {
                throw new ArgumentNullException("enabled");
            }

            if (definitions.Count != ZombieTypeCount)
            {
                throw new ArgumentException("Expected 34 definitions.", "definitions");
            }

            if (enabled.Count != ZombieTypeCount)
            {
                throw new ArgumentException("Expected 34 eligibility flags.", "enabled");
            }

            ValidateWaveIndex(waveIndex);
            if (remainingBudget < 0)
            {
                throw new ArgumentOutOfRangeException("remainingBudget");
            }

            var result = new List<WeightedZombie>();
            for (var id = 0; id < ZombieTypeCount; id++)
            {
                var zombie = definitions[id];
                if (zombie.Id != id)
                {
                    throw new ArgumentException("Definitions must be ordered by ID.", "definitions");
                }

                if (enabled[id] && waveIndex + 1 >= zombie.WaveGate && remainingBudget >= zombie.BudgetCost)
                {
                    result.Add(new WeightedZombie(id, zombie.SelectionWeight));
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>Performs total weight.</summary>
        /// <param name = "candidates">The candidates.</param>
        /// <returns>The resulting total weight.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static int TotalWeight(IReadOnlyList<WeightedZombie> candidates)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException("candidates");
            }

            var total = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                total = checked(total + candidates[i].Weight);
            }

            return total;
        }

        /// <summary>
        /// Select using a caller-supplied ticket in [0, TotalWeight). Returns
        /// false for an empty or all-zero list. No native RNG equivalence is implied.
        /// </summary>
        public static bool TrySelectByTicket(IReadOnlyList<WeightedZombie> candidates, int ticket, out int zombieId)
        {
            var total = TotalWeight(candidates);
            zombieId = -1;
            if (total == 0)
            {
                return false;
            }

            if (ticket < 0 || ticket >= total)
            {
                throw new ArgumentOutOfRangeException("ticket");
            }

            var cumulative = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                cumulative += candidates[i].Weight;
                if (ticket < cumulative)
                {
                    zombieId = candidates[i].ZombieId;
                    return true;
                }
            }

            throw new InvalidOperationException("Invalid candidate weights.");
        }

        /// <summary>Performs validate wave index.</summary>
        /// <param name = "waveIndex">The wave index.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        private static void ValidateWaveIndex(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= 100)
            {
                throw new ArgumentOutOfRangeException("waveIndex");
            }
        }
    }
}
