#nullable enable

namespace PlantsVsZombies.Modes
{
    /// <summary>Defines mode phase.</summary>
    public enum ModePhase
    {
        /// <summary>Identifies choosing seeds.</summary>
        ChoosingSeeds,
        /// <summary>Identifies preparing.</summary>
        Preparing,
        /// <summary>Identifies playing.</summary>
        Playing,
        /// <summary>Identifies stage cleared.</summary>
        StageCleared,
        /// <summary>Identifies won.</summary>
        Won,
        /// <summary>Identifies lost.</summary>
        Lost
    }
}
