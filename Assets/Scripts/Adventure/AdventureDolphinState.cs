#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original dolphin jump's fixed update counter independently of its animation clock.</summary>
    public sealed class AdventureDolphinState
    {
        /// <summary>Stores the original remaining jump updates, initialized to one hundred twenty on takeoff.</summary>
        public int JumpTicks;
        /// <summary>Stores the original clip-transition blend, including independent twenty-update meal transitions.</summary>
        public int AnimationBlendTicks;
    }
}
