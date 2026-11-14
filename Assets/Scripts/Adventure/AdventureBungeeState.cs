#nullable enable

using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC bungee descent, waiting, grabbing, cargo and departure state.</summary>
    public sealed class AdventureBungeeState
    {
        /// <summary>Gets the original BungeeDiving, Screaming, AtBottom, Grabbing, Rising or Cutscene phase.</summary>
        public int Phase { get; internal set; } = 4;
        /// <summary>Gets the native phase countdown in centisecond updates.</summary>
        public int PhaseTicks { get; internal set; }
        /// <summary>Gets whether Umbrella Leaf bounced this bungee into the native global top layer.</summary>
        public bool HitUmbrella { get; internal set; }
        /// <summary>Gets whether the descending actor carries a frozen zombie instead of stealing a plant.</summary>
        public bool CarriesZombie { get; internal set; }
        /// <summary>Gets the plant retained through the grab and lift animation, or null when the target has vanished.</summary>
        public Plant? TargetPlant { get; internal set; }
        /// <summary>Gets the current original body-animation age in seconds.</summary>
        public float AnimationSeconds { get; internal set; }
        /// <summary>Gets the original body transition blend duration in native updates.</summary>
        public int BlendTicks { get; internal set; }
        /// <summary>Gets the native reanimation layer, retaining the falling pose after an umbrella bounce.</summary>
        public string Animation => CarriesZombie || Phase == 8 && !HitUmbrella ? "anim_raise"
            : Phase == 6 ? "anim_idle" : Phase == 7 ? "anim_grab" : "anim_drop";
        /// <summary>Gets the original fixed reanimation rate, independent of walking ground tracks.</summary>
        public float AnimationRate => CarriesZombie || Phase == 8 && !HitUmbrella ? 36f : 24f;
        /// <summary>Gets whether the native animation loops instead of playing once and holding its last frame.</summary>
        public bool AnimationLoops => !CarriesZombie && Phase != 7 && (Phase != 8 || HitUmbrella);

        /// <summary>Starts an original body phase without blending through a fictitious intermediate clip.</summary>
        /// <param name="phase">The native phase identifier.</param>
        /// <param name="ticks">The native phase countdown.</param>
        /// <param name="blendTicks">The original reanimation blend duration.</param>
        internal void Enter(int phase, int ticks = 0, int blendTicks = 0)
        {
            Phase = phase;
            PhaseTicks = ticks;
            BlendTicks = blendTicks;
            AnimationSeconds = phase == 6 ? 10f / 24f : 0f;
        }
    }
}
