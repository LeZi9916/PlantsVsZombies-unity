using System;
using System.Collections.Generic;

namespace PvZReplica.Waves
{
    /// <summary>Raw EXE selection fields for one numeric zombie ID.</summary>
    public struct ZombieWaveDefinition
    {
        public readonly int Id;
        public readonly int BudgetCost;
        public readonly int LevelGate;
        public readonly int WaveGate;
        public readonly int SelectionWeight;

        public ZombieWaveDefinition(int id, int budgetCost, int levelGate, int waveGate, int selectionWeight)
        {
            if (id < 0 || id >= AdventureWaveRules.ZombieTypeCount) throw new ArgumentOutOfRangeException("id");
            if (budgetCost < 0) throw new ArgumentOutOfRangeException("budgetCost");
            if (levelGate < 0) throw new ArgumentOutOfRangeException("levelGate");
            if (waveGate < 0) throw new ArgumentOutOfRangeException("waveGate");
            if (selectionWeight < 0) throw new ArgumentOutOfRangeException("selectionWeight");
            Id = id;
            BudgetCost = budgetCost;
            LevelGate = levelGate;
            WaveGate = waveGate;
            SelectionWeight = selectionWeight;
        }
    }

    /// <summary>Snapshot of the profile fields consumed by the examined instructions.</summary>
    public struct AdventureProfileFields
    {
        public readonly bool HasProfile;
        public readonly int ProgressRaw;
        public readonly int LevelRaw;

        public AdventureProfileFields(bool hasProfile, int progressRaw, int levelRaw)
        {
            HasProfile = hasProfile;
            ProgressRaw = progressRaw;
            LevelRaw = levelRaw;
        }
    }

    public struct WeightedZombie
    {
        public readonly int ZombieId;
        public readonly int Weight;

        public WeightedZombie(int zombieId, int weight)
        {
            if (zombieId < 0 || zombieId >= AdventureWaveRules.ZombieTypeCount)
                throw new ArgumentOutOfRangeException("zombieId");
            if (weight < 0) throw new ArgumentOutOfRangeException("weight");
            ZombieId = zombieId;
            Weight = weight;
        }
    }

    public struct FlagWavePrefill
    {
        public readonly int NormalCount;
        public readonly int FlagCount;
        public readonly int RemainingBudget;

        internal FlagWavePrefill(int normalCount, int remainingBudget)
        {
            NormalCount = normalCount;
            FlagCount = 1;
            RemainingBudget = remainingBudget;
        }
    }

    /// <summary>
    /// Independently usable fragments of mode-0 selection. These do not generate
    /// a full wave, implement special-level setup, or reproduce the EXE RNG.
    /// </summary>
    public static class AdventureWaveRules
    {
        public const int ZombieTypeCount = 34;
        public const int AdventureLevelCount = 50;

        /// <summary>The table index used for a positive, 1-based level.</summary>
        public static int TableIndex(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException("level");
            return Math.Min(level, AdventureLevelCount) - 1;
        }

        /// <summary>
        /// The predicate at 0x4193B0. For ID 19, pass the global application's
        /// profile snapshot; the matrix flag and requested level are not consulted.
        /// Special-level setup can bypass this predicate entirely.
        /// </summary>
        public static bool IsTableEligible(ZombieWaveDefinition zombie, int level, int matrixFlag,
            AdventureProfileFields profile)
        {
            if (level < 1) throw new ArgumentOutOfRangeException("level");
            if (zombie.Id == 19)
            {
                return profile.HasProfile && profile.ProgressRaw > 0 &&
                    (profile.ProgressRaw >= 2 || profile.LevelRaw >= zombie.LevelGate);
            }
            return level >= zombie.LevelGate && zombie.SelectionWeight != 0 && matrixFlag != 0;
        }

        /// <summary>
        /// Mode-0 flag predicate at 0x414E50. A nonnegative index beyond the
        /// current list is accepted because the EXE also queries the next wave.
        /// </summary>
        public static bool IsFlagWave(int level, int waveCount, int waveIndex, AdventureProfileFields profile)
        {
            if (level < 1) throw new ArgumentOutOfRangeException("level");
            if (waveCount <= 0) throw new ArgumentOutOfRangeException("waveCount");
            if (waveIndex < 0) throw new ArgumentOutOfRangeException("waveIndex");
            bool progressed = profile.HasProfile && profile.ProgressRaw > 0;
            if (!progressed && level == 1) return false;
            int period = progressed || waveCount >= 10 ? 10 : waveCount;
            return waveIndex % period == period - 1;
        }

        /// <summary>
        /// Mode-0 budget before flag additions, special-level multipliers,
        /// forced zombies, and selection. Supports the 100 allocated wave rows.
        /// </summary>
        public static int PreAdjustmentBudget(int level, int waveIndex, AdventureProfileFields profile)
        {
            if (level < 1) throw new ArgumentOutOfRangeException("level");
            ValidateWaveIndex(waveIndex);
            return profile.HasProfile && profile.ProgressRaw > 0 && level != 5
                ? 2 * waveIndex / 5 + 1
                : waveIndex / 3 + 1;
        }

        /// <summary>
        /// Fixed mode-0 flag prefill, starting with an empty wave: append ID 0
        /// NormalCount times, then ID 1 once. Apply special-level processing and
        /// other forced insertions separately. Tested for budgets 1 through 40.
        /// </summary>
        public static FlagWavePrefill CalculateFlagPrefill(int preAdjustmentBudget,
            ZombieWaveDefinition normal, ZombieWaveDefinition flag)
        {
            if (preAdjustmentBudget < 1 || preAdjustmentBudget > 40)
                throw new ArgumentOutOfRangeException("preAdjustmentBudget");
            if (normal.Id != 0) throw new ArgumentException("Expected zombie ID 0.", "normal");
            if (flag.Id != 1) throw new ArgumentException("Expected zombie ID 1.", "flag");
            int normalCount = Math.Min(preAdjustmentBudget, 8);
            int remaining = checked(preAdjustmentBudget * 5 / 2 -
                checked(normalCount * normal.BudgetCost) - flag.BudgetCost);
            return new FlagWavePrefill(normalCount, remaining);
        }

        /// <summary>
        /// Filters an already-initialized board eligibility array for mode 0.
        /// The definition and enabled arrays must contain IDs 0..33 in order.
        /// Zero-weight candidates are retained exactly as in the native list.
        /// </summary>
        public static IReadOnlyList<WeightedZombie> BuildCandidates(
            IReadOnlyList<ZombieWaveDefinition> definitions, IReadOnlyList<bool> enabled,
            int waveIndex, int remainingBudget)
        {
            if (definitions == null) throw new ArgumentNullException("definitions");
            if (enabled == null) throw new ArgumentNullException("enabled");
            if (definitions.Count != ZombieTypeCount) throw new ArgumentException("Expected 34 definitions.", "definitions");
            if (enabled.Count != ZombieTypeCount) throw new ArgumentException("Expected 34 eligibility flags.", "enabled");
            ValidateWaveIndex(waveIndex);
            if (remainingBudget < 0) throw new ArgumentOutOfRangeException("remainingBudget");
            List<WeightedZombie> result = new List<WeightedZombie>();
            for (int id = 0; id < ZombieTypeCount; id++)
            {
                ZombieWaveDefinition zombie = definitions[id];
                if (zombie.Id != id) throw new ArgumentException("Definitions must be ordered by ID.", "definitions");
                if (enabled[id] && waveIndex + 1 >= zombie.WaveGate && remainingBudget >= zombie.BudgetCost)
                    result.Add(new WeightedZombie(id, zombie.SelectionWeight));
            }
            return result.AsReadOnly();
        }

        public static int TotalWeight(IReadOnlyList<WeightedZombie> candidates)
        {
            if (candidates == null) throw new ArgumentNullException("candidates");
            int total = 0;
            for (int i = 0; i < candidates.Count; i++) total = checked(total + candidates[i].Weight);
            return total;
        }

        /// <summary>
        /// Select using a caller-supplied ticket in [0, TotalWeight). Returns
        /// false for an empty or all-zero list. No native RNG equivalence is implied.
        /// </summary>
        public static bool TrySelectByTicket(IReadOnlyList<WeightedZombie> candidates, int ticket, out int zombieId)
        {
            int total = TotalWeight(candidates);
            zombieId = -1;
            if (total == 0) return false;
            if (ticket < 0 || ticket >= total) throw new ArgumentOutOfRangeException("ticket");
            int cumulative = 0;
            for (int i = 0; i < candidates.Count; i++)
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

        private static void ValidateWaveIndex(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= 100) throw new ArgumentOutOfRangeException("waveIndex");
        }
    }
}
