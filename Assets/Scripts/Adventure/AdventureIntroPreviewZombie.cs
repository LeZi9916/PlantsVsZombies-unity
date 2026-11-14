#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro preview zombie.</summary>
    public sealed class AdventureIntroPreviewZombie
    {
        /// <summary>Stores native kind, grid x, grid y, row, render order.</summary>
        public int NativeKind, GridX, GridY, Row, RenderOrder;
        /// <summary>Stores world x, world y, scale, altitude, animation rate, initial phase.</summary>
        public float WorldX, WorldY, Scale = 1f, Altitude, AnimationRate, InitialPhase;
        /// <summary>Retains the original UI Pogo bounce counter and held body clock independently of the cutscene timeline.</summary>
        public AdventurePogoState? PogoState;
        /// <summary>Stores animation.</summary>
        public string Animation = string.Empty;
        /// <summary>Stores loop, show ducky tube.</summary>
        public bool Loop = true, ShowDuckyTube;
    }
}
