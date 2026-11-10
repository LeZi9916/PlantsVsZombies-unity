using System;
using PvZReplica.Adventure;
using PvZReplica.UI;
using UnityEngine;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private bool TryGetAdventureCursorPlant(out PlantKind kind)
        {
            kind = PlantKind.Peashooter;
            if (game == null || !game.IsAdventure || game.State != MatchState.Playing || choosingSeeds ||
                AdventureIntroActive || shovel || game.Adventure.SelectedCobCannonId.HasValue ||
                optionsView.IsOpen || confirmingQuit || unavailableFeature != null ||
                !new Rect(0, 0, 800, 600).Contains(adventurePointer)) return false;
            if (selectedAdventurePacket >= 0)
            {
                foreach (AdventurePacket packet in game.Adventure.Packets)
                    if (packet.Id == selectedAdventurePacket) { kind = packet.Kind; return true; }
                return false;
            }
            if (selected < 0 || selected >= deck.Count) return false;
            kind = deck[selected];
            return true;
        }
        private void DrawAdventurePlantPreview(int row)
        {
            PlantKind kind;
            if (!TryGetAdventureCursorPlant(out kind)) return;
            int column = Mathf.FloorToInt((adventurePointer.x - AdventureGridX) / CellWidth);
            int targetRow = Mathf.FloorToInt((adventurePointer.y - AdventureGridY - AdventureSlope(column)) / AdventurePitch);
            if (column < 0 || column > 8 || targetRow != row || targetRow < 0 || targetRow >= game.RowCount ||
                game.CanPlantAdventure(kind, row, column) != AdventurePlantingFailure.None) return;
            PlantKind resolved = kind == PlantKind.Imitater && game.Adventure.ImitaterTarget.HasValue ? game.Adventure.ImitaterTarget.Value : kind;
            var heightProbe = new Plant { Kind = resolved, Row = row, Column = column };
            Vector2 origin = new Vector2(AdventureGridX + column * CellWidth, AdventureCellY(row, column) + AdventurePlantHeight(heightProbe));
            // PC CursorPreview::Draw 0x444800 uses white with alpha100 after native CanPlantAt succeeds.
            DrawAdventureCursorPlant(kind, origin, 100f / 255f, "cursor-preview");
        }
        private void DrawAdventurePlantCursor()
        {
            PlantKind kind;
            if (!TryGetAdventureCursorPlant(out kind)) return;
            PlantKind resolved = kind == PlantKind.Imitater && game.Adventure.ImitaterTarget.HasValue ? game.Adventure.ImitaterTarget.Value : kind;
            float height = (int)resolved < 49 ? AlmanacPortraitLayout.PlantHeightOffset((int)resolved) : 0f;
            // PC CursorObject::Update 0x444100 offsets the mouse by(-25,-35).
            // Draw 0x4441A0 adds(-10,height-25), with another+30 for flowerpot/puffshroom.
            if (resolved == PlantKind.FlowerPot || resolved == PlantKind.PuffShroom) height += 30f;
            DrawAdventureCursorPlant(kind, new Vector2(adventurePointer.x - 35f, adventurePointer.y - 60f + height), 1f, "cursor-held");
        }
        private void DrawAdventureCursorPlant(PlantKind seed, Vector2 origin, float alpha, string key)
        {
            bool imitated = seed == PlantKind.Imitater && game.Adventure.ImitaterTarget.HasValue;
            PlantKind kind = imitated ? game.Adventure.ImitaterTarget.Value : seed;
            string name = AdventurePlantAnimation(kind);
            float nativeScale = 1f;
            if (kind == PlantKind.PotatoMine || kind == PlantKind.FlowerPot) { nativeScale = .8f; origin += new Vector2(12, 12); }
            string clip = kind == PlantKind.PotatoMine ? "anim_armed" : null;
            Func<string, bool> visible = kind == PlantKind.KernelPult ?
                (Func<string, bool>)(track => !string.Equals(track, "Cornpult_butter", StringComparison.OrdinalIgnoreCase)) : null;
            Color previous = GUI.color;
            try
            {
                bool explosive = kind == PlantKind.ExplodeONut;
                float greenBlue = explosive ? 64f / 255f : 1f;
                GUI.color = new Color(previous.r, previous.g * greenBlue, previous.b * greenBlue, previous.a * alpha);
                // Plant::DrawSeedType uses the cached, stationary native reanimation pose.
                if (imitated)
                    animations.DrawNativeClipped(key, new Rect(origin.x - 80, origin.y - 160, 320, 340),
                        () => animations.DrawNative(name, origin, nativeScale, 0f, clip, visible), false, true);
                else animations.DrawNative(name, origin, nativeScale, 0f, clip, visible);
            }
            finally { GUI.color = previous; }
        }
    }
}
