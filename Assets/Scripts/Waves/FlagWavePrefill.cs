#nullable enable

namespace PlantsVsZombies.Waves
{
    /// <summary>Defines flag wave prefill.</summary>
    public struct FlagWavePrefill
    {
        /// <summary>Stores normal count.</summary>
        public readonly int NormalCount;
        /// <summary>Stores flag count.</summary>
        public readonly int FlagCount;
        /// <summary>Stores remaining budget.</summary>
        public readonly int RemainingBudget;
        /// <summary>Initializes flag wave prefill.</summary>
        /// <param name = "normalCount">The normal count.</param>
        /// <param name = "remainingBudget">The remaining budget.</param>
        internal FlagWavePrefill(int normalCount, int remainingBudget)
        {
            NormalCount = normalCount;
            FlagCount = 1;
            RemainingBudget = remainingBudget;
        }
    }
}
