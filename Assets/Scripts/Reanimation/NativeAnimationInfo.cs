#nullable enable

using UnityEngine;

namespace PlantsVsZombies.Reanimation
{
    /// <summary>Defines native animation info.</summary>
    public struct NativeAnimationInfo
    {
        /// <summary>Stores start, count.</summary>
        public int Start, Count;
        /// <summary>Stores source rate, ground distance.</summary>
        public float SourceRate, GroundDistance;
        /// <summary>Stores bounds.</summary>
        public Rect Bounds;
    }
}
