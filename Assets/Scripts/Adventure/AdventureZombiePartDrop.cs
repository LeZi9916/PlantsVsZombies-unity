#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie part drop.</summary>
    public sealed class AdventureZombiePartDrop
    {
        /// <summary>Stores id, seed, native kind, row, particle id.</summary>
        public int Id, Seed, NativeKind, Row, ParticleId;
        /// <summary>Stores x, age, scale, animation age, rate.</summary>
        public float X, Age, Scale, AnimationAge, Rate;
        /// <summary>Stores asset, animation, anchor, image.</summary>
        public string? Asset = string.Empty, Animation = string.Empty, Anchor = string.Empty, Image = string.Empty;
        /// <summary>Stores backwards, chilled, hypnotized.</summary>
        public bool Backwards, Chilled, Hypnotized;
        /// <summary>Gets or sets duration.</summary>
        public float Duration
        {
            get
            {
                return ParticleId == 12 || ParticleId == 13 || ParticleId == 54 ? .4f : ParticleId == 8 || ParticleId == 9 || ParticleId == 10 || ParticleId == 11 || ParticleId == 14 || ParticleId == 74 ? .5f : 1.8f;
            }
        }
    }
}
