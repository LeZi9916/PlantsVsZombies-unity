#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC ice vehicle's damage smoke and original punctured-tire death.</summary>
    public sealed class AdventureZomboniState
    {
        /// <summary>Stores the zombie age at which the original damaged-engine smoke was attached.</summary>
        public float SmokeStartAge = -1f;
        /// <summary>Stores whether the original spike damage has punctured the tires.</summary>
        public bool FlatTires;
        /// <summary>Stores the original one-in-four alternate wheelie choice.</summary>
        public bool Wheelie2;
        /// <summary>Stores the remaining original death updates before the vehicle explodes.</summary>
        public int DeathTicks;
        /// <summary>Prevents duplicate explosion effects when a retained dying vehicle is finally removed.</summary>
        public bool Exploded;
        /// <summary>Gets the original once-and-hold puncture animation.</summary>
        public string Animation => Wheelie2 ? "anim_wheelie2" : "anim_wheelie1";
        /// <summary>Gets the original puncture animation's frame rate.</summary>
        public float Rate => Wheelie2 ? 10f : 12f;
    }
}
