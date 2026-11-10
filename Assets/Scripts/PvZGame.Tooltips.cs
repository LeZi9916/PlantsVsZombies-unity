using System;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.UI;
using PvZReplica.Reanimation;
using PvZReplica.VerifiedData;
namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private OriginalTooltip originalTooltip;
        internal OriginalTooltipRequest tooltipInspection;
        private void DrawOriginalHoverTips()
        {
            if (optionsView.IsOpen || unavailableFeature != null || confirmingQuit || profileDialog != null && profileDialog.IsOpen) return;
            if (smokeMode && tooltipInspection != null) { DrawOriginalTooltipRequest(tooltipInspection); return; }
            if (currentView != ScreenView.Board || game == null || !game.IsAdventure) return;
            if (AdventureIntroActive && !AdventureIntroAwaitingSeeds) return;
            if (AdventureIntroAwaitingSeeds)
            {
                foreach (PlantKind kind in game.Adventure.AvailableSeeds)
                {
                    Rect rect = AdventureCampaignChooserRect(kind);
                    if (rect.Contains(adventurePointer)) { DrawOriginalPlantTooltip(kind, rect, true); return; }
                }
                return;
            }
            if (IsModeBoard && modeSession.Definition.UsesPackets || game.Adventure.Level.HasConveyor) return;
            for (int i = 0; i < deck.Count; i++)
            {
                Rect rect = IsModeBoard ? ModeSeedRect(i) : AdventureSeedRect(i);
                if (rect.Contains(adventurePointer)) { DrawOriginalPlantTooltip(deck[i], rect, false); return; }
            }
        }
        private void DrawOriginalPlantTooltip(PlantKind kind, Rect card, bool chooser)
        {
            PlantKind shown = kind == PlantKind.Imitater && game.Adventure.ImitaterTarget.HasValue ? game.Adventure.ImitaterTarget.Value : kind;
            if ((int)shown < 0 || (int)shown >= VerifiedDataCatalog.Plants.Count) return;
            string key = VerifiedDataCatalog.Plants[(int)shown].InternalName;
            string warning = chooser ? "" : game.CooldownRemaining(kind) > 0 ? OriginalProgressionText("WAITING_TO_REFRESH") : game.AdventureSunAvailable < game.AdventureSeedCost(kind) ? OriginalProgressionText("NOT_ENOUGH_SUN") : "";
            DrawOriginalTooltipRequest(new OriginalTooltipRequest { Title = OriginalProgressionText(key), Body = OriginalProgressionText(key + "_TOOLTIP"), Warning = warning,
                X = (int)card.center.x, Y = (int)card.yMax + 7, Centered = true, UseSeedChooserLineWidth = chooser, WarningFlashCounter = 0 });
        }
        private void DrawOriginalTooltipRequest(OriginalTooltipRequest request)
        {
            if (originalTooltip == null) originalTooltip = new OriginalTooltip();
            originalTooltip.Draw(request);
        }
        private void DrawNativeModeZombieCard(ZombieKind kind, Rect rect)
        {
            Texture2D seeds = Texture("seeds.png");
            if (seeds != null) animations.DrawNativeImage(seeds, rect, new Rect(2f / 9f, 0, 1f / 9f, 1));
            // PC0x49D260 ->0x49CEE0 ->0x4744F0 ->0x47ECE0 caches original body at (40,40).
            float scale = kind == ZombieKind.Gargantuar || kind == ZombieKind.Zomboni ? .23f : kind == ZombieKind.Imp ? .4f : kind == ZombieKind.Bungee ? .3f : kind == ZombieKind.Football ? .33f : .35f;
            float x = kind == ZombieKind.PoleVaulter ? -8 : kind == ZombieKind.Football ? -7 : kind == ZombieKind.Zomboni ? 12 : kind == ZombieKind.Gargantuar ? 4 : kind == ZombieKind.Imp ? -12 : kind == ZombieKind.Bungee ? 1 : -3;
            float y = kind == ZombieKind.PoleVaulter ? -11 : kind == ZombieKind.Football ? -8 : kind == ZombieKind.Imp ? -16 : kind == ZombieKind.Zomboni || kind == ZombieKind.Gargantuar ? 4 : kind == ZombieKind.Bungee ? 0 : kind == ZombieKind.Balloon ? -4 : kind == ZombieKind.Ladder || kind == ZombieKind.Digger || kind == ZombieKind.Pogo || kind == ZombieKind.ScreenDoor ? -9 : -6;
            if (kind == ZombieKind.Dancing) { scale = .37f * .8f; x = -18 + 20; y = -37 + 40; }
            float bankScale = rect.width / 50f;
            Vector2 origin = new Vector2(rect.x + (x + 40 * scale) * bankScale, rect.y + (y + (kind == ZombieKind.Gargantuar ? 60 : 40) * scale) * bankScale);
            scale *= bankScale;
            ZombieVariant variant = kind == ZombieKind.Conehead ? ZombieVariant.Conehead : kind == ZombieKind.Buckethead ? ZombieVariant.Buckethead : kind == ZombieKind.ScreenDoor ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
            if (kind == ZombieKind.Normal || kind == ZombieKind.Conehead || kind == ZombieKind.Buckethead || kind == ZombieKind.ScreenDoor)
                animations.DrawZombieNative(origin, scale, 0f, variant, "anim_idle", true);
            else animations.DrawNative(AdventureZombieAnimation(kind), origin, scale, 0f, kind == ZombieKind.Pogo ? "anim_pogo" : kind == ZombieKind.PoleVaulter ? "anim_walk" : "anim_idle");
            Label(new Rect(rect.x + 2, rect.y + rect.height - 17, rect.width - 7, 15), GameSimulation.ModeZombieCost(kind).ToString(), 13, Color.black, TextAnchor.MiddleRight, true);
        }
    }
}
