#nullable enable

namespace PlantsVsZombies.Reanimation
{
    /// <summary>Defines head playback.</summary>
    public struct HeadPlayback
    {
        /// <summary>Stores animation, anchor.</summary>
        public string Animation, Anchor;
        /// <summary>Stores seconds, rate.</summary>
        public float Seconds, Rate;
        /// <summary>Stores loop.</summary>
        public bool Loop;
    }
}
