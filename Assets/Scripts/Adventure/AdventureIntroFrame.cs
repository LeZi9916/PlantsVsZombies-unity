#nullable enable

using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro frame.</summary>
    public sealed class AdventureIntroFrame
    {
        /// <summary>Stores stage.</summary>
        public AdventureIntroStage Stage;
        /// <summary>Stores native time, board x, board y, seed bank x, seed bank y, seed bank darken, chooser y, sod position.</summary>
        public int NativeTime, BoardX, BoardY, SeedBankX, SeedBankY, SeedBankDarken, ChooserY, SodPosition;
        /// <summary>Stores world input enabled, menu visible, chooser visible, preview visible.</summary>
        public bool WorldInputEnabled, MenuVisible, ChooserVisible, PreviewVisible;
        /// <summary>Stores sod blits.</summary>
        public readonly List<AdventureIntroBlit> SodBlits = new List<AdventureIntroBlit>();
        /// <summary>Stores animations.</summary>
        public readonly List<AdventureIntroAnimation> Animations = new List<AdventureIntroAnimation>();
        /// <summary>Stores particles.</summary>
        public readonly List<AdventureIntroParticle> Particles = new List<AdventureIntroParticle>();
        /// <summary>Stores mower x.</summary>
        public readonly int[] MowerX =
        {
            -80,
            -80,
            -80,
            -80,
            -80,
            -80
        };
    }
}
