using System;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Reanimation;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private OriginalParticleRenderer seedBankParticles;
        private GameSimulation seedBankPresentedGame;
        private void DrawAdventurePacket(PlantKind kind, Rect card)
        {
            var context = game.AdventurePacketContext(kind);
            context.Picked = selected >= 0 && selected < deck.Count && deck[selected] == kind;
            var visual = AdventureSeedBankPresentation.Sample(context);
            Color previous = GUI.color;
            try
            {
                float shade = visual.BaseColor / 255f; GUI.color = new Color(previous.r * shade, previous.g * shade, previous.b * shade, previous.a);
                DrawAdventureCard(kind, card, false);
                if (visual.DrawMask)
                {
                    GUI.color = new Color(previous.r * visual.MaskColor / 255f, previous.g * visual.MaskColor / 255f, previous.b * visual.MaskColor / 255f, previous.a);
                    animations.DrawNativeClipped("seed-cooldown-" + (int)kind, card, () => { DrawAdventureCard(kind, card, false); return true; }, topCrop:Math.Min(card.height, visual.MaskHeight));
                }
            }
            finally { GUI.color = previous; }
            if (visual.DrawCost) DrawAdventurePacketCost(kind, card);
        }
        private void DrawAdventureConveyorPacket(AdventurePacket packet, Rect card)
        {
            Color previous = GUI.color;
            try
            {
                if (selectedAdventurePacket == packet.Id) GUI.color = new Color(previous.r * 64f / 255f, previous.g * 64f / 255f, previous.b * 64f / 255f, previous.a);
                DrawAdventureCard(packet.Kind, card, false);
            }
            finally { GUI.color = previous; }
        }
        private void DrawAdventurePacketCost(PlantKind kind, Rect card)
        {
            Label(new Rect(card.x + 2, card.y + 53, 43, 15), game.AdventureSeedCost(kind).ToString(), 13, Color.black, TextAnchor.MiddleRight, true);
        }
        private void DrawAdventurePacketReadyFlashes()
        {
            if (!ReferenceEquals(seedBankPresentedGame, game)) { seedBankPresentedGame = game; seedBankParticles = new OriginalParticleRenderer(root, animations); }
            seedBankParticles.BeginFrame();
            foreach (var flash in game.Adventure.SeedFlashes)
                for (int i = 0; i < deck.Count; i++) if (deck[i] == flash.Kind)
                {
                    Rect card = IsModeBoard ? ModeSeedRect(i) : AdventureSeedRect(i);
                    seedBankParticles.Draw("packet-ready-" + flash.Id, AdventureSeedPacketFlash.Asset, flash.Age + AdventureRenderRemainder, new Vector2(card.x, card.y), flash.Id);
                    break;
                }
            seedBankParticles.EndFrame();
        }
    }
}
