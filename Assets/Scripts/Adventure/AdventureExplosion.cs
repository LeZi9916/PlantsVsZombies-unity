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
                return Effect != AdventureExplosionEffect.PotatoMine && Effect != AdventureExplosionEffect.PotatoMineRise
                    && Effect != AdventureExplosionEffect.GraveBuster && Effect != AdventureExplosionEffect.GraveBusterDie;
            }
        }

        /// <summary>Gets or sets particle.</summary>
        public string Particle
        {
            get
            {
                switch (Effect)
                {
                    case AdventureExplosionEffect.Powie:
                        return nameof(AdventureExplosionEffect.Powie);
                    case AdventureExplosionEffect.Doom:
                        return nameof(AdventureExplosionEffect.Doom);
                    case AdventureExplosionEffect.PotatoMine:
                        return nameof(AdventureExplosionEffect.PotatoMine);
                    case AdventureExplosionEffect.PotatoMineRise:
                        return nameof(AdventureExplosionEffect.PotatoMineRise);
                    case AdventureExplosionEffect.Pow:
                        return nameof(AdventureExplosionEffect.Pow);
                    case AdventureExplosionEffect.GraveBuster:
                        return nameof(AdventureExplosionEffect.GraveBuster);
                    case AdventureExplosionEffect.GraveBusterDie:
                        return nameof(AdventureExplosionEffect.GraveBusterDie);
                    case AdventureExplosionEffect.IceTrap:
                        return nameof(AdventureExplosionEffect.IceTrap);
                    default:
                        return Effect.ToString();
                }
            }
        }

        // Max SystemDuration in the corresponding unmodified local PC XML.
        /// <summary>Gets or sets duration ticks.</summary>
        public int DurationTicks
        {
            get
            {
                return Effect == AdventureExplosionEffect.Pow ? 50
                    : Effect == AdventureExplosionEffect.IceTrap ? 40
                    : Effect == AdventureExplosionEffect.Doom ? 400
                    : Effect == AdventureExplosionEffect.PotatoMineRise ? 120
                    : Effect == AdventureExplosionEffect.GraveBuster ? 400
                    : Effect == AdventureExplosionEffect.GraveBusterDie ? 500 : 200;
            }
        }
    }
}
