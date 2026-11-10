using System;
using UnityEngine;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private static readonly Rect NativeConveyorClip = new Rect(90, 0, 501, 87);
        private void DrawAdventureSeedBankBackground(Vector2 origin, int count)
        {
            Texture2D image = Texture("SeedBank.png"); if (image == null) return;
            GUI.DrawTexture(new Rect(origin.x, origin.y, image.width, image.height), image);
            int extra = AdventureSeedSlotLayout.BackgroundExtra(count);
            if (extra <= 0) return;
            // PC 0x49F100 copies the original right-hand strip; it does not stretch the panel.
            GUI.DrawTextureWithTexCoords(new Rect(origin.x + image.width - 12, origin.y, extra + 12, image.height), image,
                new Rect((image.width - extra - 12f) / image.width, 0, (extra + 12f) / image.width, 1));
        }
        private void DrawAdventureConveyorBackground()
        {
            Texture2D backdrop = Texture("ConveyorBelt_backdrop.png");
            if (backdrop != null) GUI.DrawTexture(new Rect(83, 0, backdrop.width, backdrop.height), backdrop);
            Texture2D belt = Texture("ConveyorBelt.png");
            if (belt == null) return;
            int row = game.Adventure.ConveyorTicks / 4 % 6;
            GUI.DrawTextureWithTexCoords(new Rect(90, 63, belt.width, belt.height / 6f), belt, new Rect(0, 1f - (row + 1) / 6f, 1, 1f / 6f));
        }
        private bool AdventurePacketContains(int index, Vector2 point)
        { return (!game.UsesNativeAdventureConveyor || NativeConveyorClip.Contains(point)) && AdventureSeedRect(index, true).Contains(point); }
        private void DrawAdventureConveyorPackets(bool showBackwards = false)
        {
            Func<bool> draw = () =>
            {
                for (int i = 0; i < game.Adventure.Packets.Count && i < 10; i++)
                {
                    var packet = game.Adventure.Packets[i]; Rect card = AdventureSeedRect(i, true);
                    DrawAdventureConveyorPacket(packet, card);
                    if (showBackwards && modeSession != null && modeSession.BackwardPackets.Contains(packet.Id))
                        Label(new Rect(card.x, card.y + 45, card.width, 20), "←", 18, Color.yellow, TextAnchor.MiddleCenter, true);
                }
                return true;
            };
            if (game.UsesNativeAdventureConveyor) animations.DrawNativeClipped("native-conveyor-packets", NativeConveyorClip, draw);
            else draw();
        }
    }
}
