#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines the original Imitater clock and washed-out source colors for plants and packets.</summary>
    internal static class AdventureImitaterRules
    {
        /// <summary>The original idle interval in 100 Hz ticks before playing the morph clip.</summary>
        internal const int IdleTicks = 200;
        /// <summary>The authored anim_explode length in source frames.</summary>
        private const int MorphFrames = 31;
        /// <summary>The original playback rate of anim_explode in frames per second.</summary>
        internal const float MorphRate = 26f;
        /// <summary>The zero-based duration of the rounded native morph playback.</summary>
        internal const int MorphTicks = (MorphFrames * 100 + (int)MorphRate - 1) / (int)MorphRate;
        /// <summary>The first remaining tick at the original normalized 0.8 particle cue.</summary>
        internal const int ParticleRemainingTicks = MorphTicks - (MorphFrames * 80 + (int)MorphRate - 1) / (int)MorphRate;
        /// <summary>The shader selector for PC FilterEffect::WashedOut.</summary>
        private const float WashedOutFilter = 2f;
        /// <summary>The shader selector for PC FilterEffect::LessWashedOut.</summary>
        private const float LessWashedOutFilter = 3f;

        /// <summary>Returns the original washed-out effect for an imitated species in a packet or on the lawn.</summary>
        /// <param name="kind">The resolved target of the Imitater.</param>
        /// <returns>The source shader's palette selector.</returns>
        internal static float SourceFilter(PlantKind kind)
        {
            // PC Plant::DrawSeedType 4744F0 and DrawSeedPacket 49CEE0 use the same five exceptions.
            return kind is PlantKind.HypnoShroom or PlantKind.Squash or PlantKind.PotatoMine
                or PlantKind.Garlic or PlantKind.LilyPad ? LessWashedOutFilter : WashedOutFilter;
        }
    }
}
