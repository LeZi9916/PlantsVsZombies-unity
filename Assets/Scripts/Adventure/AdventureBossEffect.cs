#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains a PC boss particle or free fire reanimation at its simulation-owned creation contact.</summary>
    public sealed class AdventureBossEffect
    {
        /// <summary>Stores the unique native effect identity and elapsed updates.</summary>
        public int Id, Ticks;
        /// <summary>Stores the original board-pixel effect origin.</summary>
        public float X, Y;
        /// <summary>Stores the unchanged original particle name, or null for the full fire reanimation.</summary>
        public string? Particle;
        /// <summary>Stores the original ice-particle image override.</summary>
        public string? Image;
        /// <summary>Stores the original free fire reanimation rate.</summary>
        public float Rate;
        /// <summary>Stores the body marker sampled when a jaw spit effect was created.</summary>
        public string? JawAnimation;
        /// <summary>Stores the body time and rate sampled at the original spit effect contact.</summary>
        public float JawSeconds, JawRate;
        /// <summary>Stores an optional original particle attachment track.</summary>
        public string? Track;
        /// <summary>Stores whether the particle still follows its source track instead of a retained detach snapshot.</summary>
        public bool LiveAttachment;
        /// <summary>Stores the original particle cross-fade start, or minus one while it is active.</summary>
        public int StopTick = -1;
        /// <summary>Gets the original maximum lifetime in native updates.</summary>
        public int Lifetime => Particle == "ZamboniSmoke" ? StopTick < 0 ? int.MaxValue : StopTick + 50 : Particle == null ? (int)System.Math.Ceiling(13f * .8f / Rate * 100f)
            : Particle == "BossExplosion" ? 80 : Particle == "IceballDeath" ? 40 : 280;
    }
}
