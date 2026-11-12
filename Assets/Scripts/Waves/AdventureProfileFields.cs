#nullable enable

namespace PlantsVsZombies.Waves
{
    /// <summary>Snapshot of the profile fields consumed by the examined instructions.</summary>
    public struct AdventureProfileFields
    {
        /// <summary>Stores has profile.</summary>
        public readonly bool HasProfile;
        /// <summary>Stores progress raw.</summary>
        public readonly int ProgressRaw;
        /// <summary>Stores level raw.</summary>
        public readonly int LevelRaw;
        /// <summary>Initializes adventure profile fields.</summary>
        /// <param name = "hasProfile">The has profile.</param>
        /// <param name = "progressRaw">The progress raw.</param>
        /// <param name = "levelRaw">The level raw.</param>
        public AdventureProfileFields(bool hasProfile, int progressRaw, int levelRaw)
        {
            HasProfile = hasProfile;
            ProgressRaw = progressRaw;
            LevelRaw = levelRaw;
        }
    }
}
