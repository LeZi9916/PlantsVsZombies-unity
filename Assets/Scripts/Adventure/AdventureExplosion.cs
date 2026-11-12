#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure explosion.</summary>
    public sealed class AdventureExplosion
    {
        /// <summary>Stores id, seed, row, ticks.</summary>
        public int Id, Seed, Row, Ticks;
        /// <summary>Stores x.</summary>
        public float X;
        /// <summary>Stores the original board-pixel position of a hammer impact.</summary>
        public float NativeX, NativeY;
        /// <summary>Stores effect.</summary>
        public AdventureExplosionEffect Effect;
        /// <summary>Gets or sets overlay.</summary>
        public bool Overlay
        {
            get
            {
                return Effect != AdventureExplosionEffect.PotatoMine && Effect != AdventureExplosionEffect.PotatoMineRise;
            }
        }

        /// <summary>Gets or sets particle.</summary>
        public string Particle
        {
            get
            {
                return Effect.ToString();
            }
        }

        // Max SystemDuration in the corresponding unmodified local PC XML.
        /// <summary>Gets or sets duration ticks.</summary>
        public int DurationTicks
        {
            get
            {
                return Effect == AdventureExplosionEffect.Pow ? 50 : Effect == AdventureExplosionEffect.Doom ? 400 : Effect == AdventureExplosionEffect.PotatoMineRise ? 120 : 200;
            }
        }
    }
}
