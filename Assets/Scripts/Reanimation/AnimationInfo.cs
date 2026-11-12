#nullable enable

using UnityEngine;

namespace PlantsVsZombies.Reanimation
{
    /// <summary>Defines animation info.</summary>
    public struct AnimationInfo
    {
        /// <summary>Gets or sets frames per second.</summary>
        public float FramesPerSecond { get; internal set; }
        /// <summary>Gets or sets frame count.</summary>
        public int FrameCount { get; internal set; }
        /// <summary>Gets or sets source bounds.</summary>
        public Rect SourceBounds { get; internal set; }

        /// <summary>Gets or sets duration seconds.</summary>
        public float DurationSeconds
        {
            get
            {
                return FrameCount / FramesPerSecond;
            }
        }
    }
}
