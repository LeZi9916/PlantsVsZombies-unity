#nullable enable
using System;

namespace PlantsVsZombies.Audio
{
    /// <summary>Defines native music row.</summary>
    [Serializable]
    public struct NativeMusicRow
    {
        /// <summary>Stores sample.</summary>
        public int sample;
        /// <summary>Stores order.</summary>
        public int order;
        /// <summary>Stores row.</summary>
        public int row;
    }
}
