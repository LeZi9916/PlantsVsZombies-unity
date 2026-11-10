#nullable enable

namespace PlantsVsZombies.Modes
{
    /// <summary>Defines mode rain packet.</summary>
    public sealed class ModeRainPacket
    {
        /// <summary>Stores packet id.</summary>
        public int PacketId;
        /// <summary>Stores x, y, target y, age.</summary>
        public float X, Y, TargetY, Age;
    }
}
