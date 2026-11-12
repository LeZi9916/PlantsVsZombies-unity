#nullable enable

namespace PlantsVsZombies.Profiles
{
    /// <summary>Identifies observable stages of an atomic profile-catalog commit.</summary>
    internal enum ProfileSaveStage
    {
        /// <summary>The replacement file is ready, but the destination has not been committed.</summary>
        BeforeCommit,
        /// <summary>The destination has been committed, before the in-memory snapshot is published.</summary>
        AfterCommit
    }
}
