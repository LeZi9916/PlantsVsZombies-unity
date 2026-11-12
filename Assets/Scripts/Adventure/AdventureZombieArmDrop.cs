#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie arm drop.</summary>
    public sealed class AdventureZombieArmDrop
    {
        /// <summary>Stores id, seed, native kind.</summary>
        public int Id, Seed, NativeKind;
        /// <summary>Stores x, row, age, scale, animation age, rate.</summary>
        public float X, Row, Age, Scale, AnimationAge, Rate;
        /// <summary>Stores asset, animation, anchor, image.</summary>
        public string Asset = string.Empty, Animation = string.Empty, Anchor = string.Empty, Image = string.Empty;
        /// <summary>Stores mowed, backwards, chilled, hypnotized.</summary>
        public bool Mowed, Backwards, Chilled, Hypnotized;
    }
}
