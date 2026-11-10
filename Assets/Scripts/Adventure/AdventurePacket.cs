#nullable enable

using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure packet.</summary>
    public sealed class AdventurePacket
    {
        /// <summary>Stores conveyor initialized.</summary>
        internal bool _conveyorInitialized;
        /// <summary>Gets or sets conveyor offset x.</summary>
        public int ConveyorOffsetX { get; internal set; }
        /// <summary>Gets or sets id.</summary>
        public int Id { get; internal set; }
        /// <summary>Gets or sets kind.</summary>
        public PlantKind Kind { get; internal set; }
        /// <summary>Gets or sets age.</summary>
        public float Age { get; internal set; }
        /// <summary>Gets or sets from vase column.</summary>
        public int? FromVaseColumn { get; internal set; }
        /// <summary>Gets or sets from vase row.</summary>
        public int? FromVaseRow { get; internal set; }
    }
}
