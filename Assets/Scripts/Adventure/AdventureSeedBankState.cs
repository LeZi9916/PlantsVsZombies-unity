using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica.Adventure
{
    public sealed class AdventureSeedFlashState { public int Id; public PlantKind Kind; public float Age; }
    public sealed partial class AdventureBoard
    { public readonly List<AdventureSeedFlashState> SeedFlashes = new List<AdventureSeedFlashState>(); internal int SeedFlashId; }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        public AdventureSeedBankContext AdventurePacketContext(PlantKind kind, bool playing = true, int darken = 255, int anticipatedSun = -1)
        {
            int left = seedCooldownTicks[(int)kind], duration = DefinitionFor(ResolveAdventureSeed(kind)).RechargeThresholdRaw;
            return new AdventureSeedBankContext { PacketKind = (int)kind, ImitaterKind = kind == PlantKind.Imitater && Adventure.ImitaterTarget.HasValue ? (int)Adventure.ImitaterTarget.Value : -1,
                Active = left <= 0, Refreshing = left > 0, RefreshTime = duration, RefreshCounter = left > 0 ? Math.Max(0, duration + 1 - left) : 0,
                Playing = playing && State == MatchState.Playing && Adventure.Phase == AdventurePhase.Playing, Paused = State == MatchState.Paused,
                TutorialState = Adventure.Tutorial == AdventureTutorial.ChooseSeed ? 1 : Adventure.Tutorial == AdventureTutorial.PlantSunflower ? 5 : Adventure.Tutorial == AdventureTutorial.RechargeSunflower ? 9 : 0, TutorialTimer = -1,
                MainCounter = Adventure.Ticks, CutsceneDarken = darken, NativeMode = ModeSession == null ? 0 : ModeSession.Definition.Id,
                CanAfford = (anticipatedSun >= 0 ? anticipatedSun : AdventureSunAvailable) >= AdventureSeedCost(kind), Conveyor = UsesNativeAdventureConveyor,
                PlantingRequirementsMet = AdventurePacketRequirements(kind) };
        }
        private bool AdventurePacketRequirements(PlantKind kind)
        {
            PlantKind resolved = ResolveAdventureSeed(kind);
            if (kind == PlantKind.Imitater && !Adventure.ImitaterTarget.HasValue) return false;
            if (resolved == PlantKind.CoffeeBean) return Plants.Exists(p => p.Asleep && p.WakeTimer <= 0);
            if (resolved == PlantKind.GraveBuster) return Adventure.Graves.Count > 0;
            PlantKind? prerequisite = UpgradePrerequisite(resolved);
            if (!prerequisite.HasValue) return true;
            foreach (Plant p in Plants)
                if (p.Kind == prerequisite.Value && (resolved != PlantKind.CobCannon || Plants.Exists(other => other.Row == p.Row && other.Column == p.Column + 1 && other.Kind == PlantKind.KernelPult))) return true;
            return false;
        }
        private void AdvanceAdventurePacketFeedback(float dt)
        {
            AdvanceAdventureConveyorPackets();
            for (int i = Adventure.SeedFlashes.Count - 1; i >= 0; i--)
            { Adventure.SeedFlashes[i].Age += dt; if (Adventure.SeedFlashes[i].Age >= .25f) Adventure.SeedFlashes.RemoveAt(i); }
            if (Adventure.Ticks == 1) foreach (PlantKind kind in Adventure.SelectedSeeds) AdventurePacketReady(kind);
        }
        private void AdventurePacketReady(PlantKind kind, int anticipatedSun = -1)
        {
            if (UsesNativeAdventureConveyor || !ContainsSeed(Adventure.SelectedSeeds, kind)) return;
            var flash = AdventureSeedBankPresentation.ReadyCheck(AdventurePacketContext(kind, anticipatedSun:anticipatedSun), 0, 0, 0, 0);
            if (flash.Spawn) Adventure.SeedFlashes.Add(new AdventureSeedFlashState { Id = ++Adventure.SeedFlashId, Kind = kind });
        }
        private void AdventureSunSelectedForPacketFeedback(Sun added)
        {
            int collecting = 0; foreach (Sun sun in Suns) if (sun.PresentationNative && sun.PresentationPickup.Collecting) collecting += sun.Value;
            foreach (PlantKind kind in Adventure.SelectedSeeds)
                if (AdventureSeedBankPresentation.SunCrossesAffordability(SunAmount - adventureSunReservation, collecting, AdventureSeedCost(kind), added.Value)) AdventurePacketReady(kind);
        }
    }
}
