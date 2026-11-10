#nullable enable
using System;

namespace PlantsVsZombies.Audio
{
    /// <summary>Defines native music manifest.</summary>
    [Serializable]
    public sealed class NativeMusicManifest
    {
        /// <summary>Stores clips.</summary>
        public NativeMusicClip[] clips;
    }
}
