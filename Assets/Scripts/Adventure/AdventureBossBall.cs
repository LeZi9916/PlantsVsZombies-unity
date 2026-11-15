#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure boss ball.</summary>
    public sealed class AdventureBossBall
    {
        /// <summary>Gets or sets row.</summary>
        public int Row { get; internal set; }
        /// <summary>Gets or sets x.</summary>
        public float X { get; internal set; } = 455f / 80f;
        /// <summary>Gets the original free reanimation's top-left horizontal position.</summary>
        public float NativeX => X * 80f;
        /// <summary>Gets whether the original formation has completed and the two-FPS roll has started.</summary>
        public bool Rolling { get; internal set; }
        /// <summary>Gets the original current free-reanimation time.</summary>
        public float AnimationSeconds { get; internal set; }
        /// <summary>Gets the original formation or rolling marker.</summary>
        public string Animation => Rolling ? "anim_role" : "anim_form";
        /// <summary>Gets the original formation or rolling frame rate.</summary>
        public float AnimationRate => Rolling ? 2f : 16f;
        /// <summary>Gets the native rolling ground-track velocity in pixels per update.</summary>
        internal float Velocity => Rolling ? (IsIce ? s_iceGround : s_fireGround).Velocity(AnimationSeconds, 2f, 2f) : 0f;
        /// <summary>Stores the original fireball's fifty-seven ground-track samples.</summary>
        private static readonly AdventureZombieGroundClip s_fireGround = new AdventureZombieGroundClip(57, 2f, new[] { -156.8f, -138.3f, -119.8f, -101.3f, -82.8f, -64.2f, -45.7f, -27.2f, -8.7f, 9.7f, 28.3f, 46.8f, 65.3f, 83.8f, 102.3f, 120.8f, 139.4f, 157.9f, 176.4f, 194.9f, 213.5f, 232.0f, 250.5f, 269.0f, 287.5f, 306.0f, 324.6f, 343.1f, 361.6f, 380.1f, 398.6f, 417.2f, 435.7f, 454.2f, 472.7f, 491.2f, 509.7f, 528.3f, 546.8f, 565.3f, 583.8f, 602.3f, 620.9f, 639.4f, 657.9f, 676.4f, 694.9f, 713.5f, 732.0f, 750.5f, 769.0f, 787.5f, 806.0f, 824.5f, 843.0f, 861.6f, 880.1f });
        /// <summary>Stores the original iceball's fifty-nine ground-track samples.</summary>
        private static readonly AdventureZombieGroundClip s_iceGround = new AdventureZombieGroundClip(59, 2f, new[] { -156.8f, -138.9f, -121.0f, -103.2f, -85.3f, -67.4f, -49.5f, -31.6f, -13.8f, 4.0f, 21.9f, 39.7f, 57.6f, 75.5f, 93.3f, 111.3f, 129.2f, 147.0f, 164.9f, 182.8f, 200.7f, 218.6f, 236.4f, 254.3f, 272.2f, 290.1f, 308.0f, 325.8f, 343.7f, 361.6f, 379.5f, 397.4f, 415.2f, 433.1f, 451.0f, 468.9f, 486.8f, 504.6f, 522.5f, 540.4f, 558.3f, 576.1f, 594.0f, 611.9f, 629.8f, 647.6f, 665.5f, 683.4f, 701.3f, 719.2f, 737.0f, 755.0f, 772.8f, 790.7f, 808.6f, 826.5f, 844.4f, 862.2f, 880.1f });

        /// <summary>Gets or sets is ice.</summary>
        public bool IsIce { get; internal set; }
    }
}
