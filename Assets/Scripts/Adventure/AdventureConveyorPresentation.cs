using System;
using PvZReplica.Modes;

namespace PvZReplica.Adventure
{
    public sealed partial class AdventurePacket
    { internal bool ConveyorInitialized; public int ConveyorOffsetX { get; internal set; } }
    public sealed partial class AdventureBoard
    { public int ConveyorTicks { get; internal set; } }
    public static class AdventureSeedSlotLayout
    {
        // PC Board::GetSeedPacketPositionX 0x427E20 and SeedBank::RefreshAll 0x49F720.
        public static int PositionX(int index, int count)
        { return count < 8 ? 85 + 59 * index : count == 8 ? 81 + 54 * index : count == 9 ? 80 + 52 * index : 79 + 51 * index; }
        public static int BackgroundExtra(int count)
        { return count <= 6 ? 0 : count == 7 ? 60 : count == 8 ? 76 : count == 9 ? 112 : 153; }
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        public bool UsesNativeAdventureConveyor
        {
            get
            {
                if (!IsAdventure) return false;
                if (ModeSession == null) return Adventure.Level.HasConveyor;
                var definition = ModeSession.Definition;
                return definition.UsesPackets && definition.Mechanic != ModeMechanic.Vases &&
                    definition.Mechanic != ModeMechanic.RainingSeeds && definition.Mechanic != ModeMechanic.Slots;
            }
        }
        private void InitializeAdventureConveyorPacket(PvZReplica.Adventure.AdventurePacket packet, int index)
        {
            if (!UsesNativeAdventureConveyor || packet.ConveyorInitialized) return;
            // PC AddSeed 0x49F4C0 sets mOffsetX=515-51*i; retain the original 50px slot pitch.
            int offset = 515 - 51 * index;
            if (index > 0 && offset < Adventure.packets[index - 1].ConveyorOffsetX)
                offset = Adventure.packets[index - 1].ConveyorOffsetX + 40;
            packet.ConveyorOffsetX = offset; packet.ConveyorInitialized = true;
        }
        private void AdvanceAdventureConveyorPackets()
        {
            if (!UsesNativeAdventureConveyor) return;
            Adventure.ConveyorTicks++;
            for (int i = 0; i < Adventure.packets.Count; i++)
            {
                var packet = Adventure.packets[i]; InitializeAdventureConveyorPacket(packet, i);
                if (Adventure.ConveyorTicks % 4 == 0 && packet.ConveyorOffsetX > 0) packet.ConveyorOffsetX--;
            }
        }
        public float AdventureConveyorCardX(int index)
        { return 91 + 50 * index + Adventure.packets[index].ConveyorOffsetX; }
        private void RemoveAdventurePacket(PvZReplica.Adventure.AdventurePacket packet)
        {
            int index = Adventure.packets.IndexOf(packet); if (index < 0) return;
            // PC RemoveSeed 0x49F530 moves the next type forward and adds51 to its offset.
            if (UsesNativeAdventureConveyor)
                for (int i = index + 1; i < Adventure.packets.Count; i++) Adventure.packets[i].ConveyorOffsetX += 51;
            Adventure.packets.RemoveAt(index);
        }
    }
}
