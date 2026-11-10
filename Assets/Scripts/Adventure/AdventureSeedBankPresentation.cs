using System;

namespace PvZReplica.Adventure
{
    public sealed class AdventureSeedBankContext
    {
        public int PacketKind = -1, ImitaterKind = -1;
        public bool Active = true, Refreshing, Playing = true, Paused, Picked;
        public int RefreshCounter, RefreshTime, MainCounter, NativeMode, TutorialState, TutorialTimer;
        public int CutsceneDarken = 255, SlotMachineCountdown;
        public bool CanAfford = true, PlantingRequirementsMet = true, EasyPlantingCheat, Conveyor, SlotMachine;
    }
    public readonly struct AdventureSeedPacketVisual
    {
        // Native color multiplication, NOT saturation/grayscale conversion or a black alpha overlay.
        public readonly int BaseColor, MaskColor, MaskX, MaskY, MaskWidth, MaskHeight;
        public readonly float RemainingFraction;
        public readonly bool DrawMask, DrawCost, DrawCooldownNumber;
        public AdventureSeedPacketVisual(int shade, float remaining, bool drawCost)
        {
            BaseColor = shade; MaskColor = 64; MaskX = 0; MaskY = 0; MaskWidth = 50;
            RemainingFraction = remaining; DrawMask = remaining > 0f;
            MaskHeight = DrawMask ? (int)(remaining * 68f + .5f) + 2 : 0;
            DrawCost = drawCost; DrawCooldownNumber = false;
        }
    }
    public readonly struct AdventureSeedPacketFlash
    {
        public readonly bool Spawn;
        public readonly float X, Y;
        public const int NativeEffect = 52, RenderOrder = 100002, DurationTicks = 25;
        public const string Asset = "SeedPacketFlash";
        // X/Y are packet top-left. XML supplies emitter offset25/35 and the original alpha curve.
        public AdventureSeedPacketFlash(bool spawn, float x, float y) { Spawn = spawn; X = x; Y = y; }
    }
    public struct AdventureSeedPacketCooldown
    {
        public bool Active, Refreshing;
        public int Counter, Duration;
    }
    public readonly struct AdventureSeedPacketCooldownResult
    {
        public readonly AdventureSeedPacketCooldown State;
        public readonly bool Completed;
        public AdventureSeedPacketCooldownResult(AdventureSeedPacketCooldown state, bool completed) { State = state; Completed = completed; }
    }
    public static class AdventureSeedBankPresentation
    {
        public const int SeedBankObjectRenderOrder = 400000, OrdinarySunRenderOrder = 600001, FogCollectedSunRenderOrder = 800002;
        public const string Evidence = "PC 0x49CDC0/0x49DDB0/0x49D260/0x49CC70/0x528520; 0x43B610/0x43D740; original SeedPacketFlash.xml";
        public static AdventureSeedPacketVisual Sample(AdventureSeedBankContext c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            float remaining = c.Active ? 0f : c.RefreshTime == 0 ? 1f : (c.RefreshTime - c.RefreshCounter) / (float)c.RefreshTime;
            int shade = 255;
            bool costs = !c.Conveyor && !c.SlotMachine;
            if (c.SlotMachineCountdown > 0) return new AdventureSeedPacketVisual(128, 0f, false);
            // PC 0x49E130 clears active/refreshing/counter/duration when picked;
            // 0x49DDB0 then draws its full native dark mask, with no outline image.
            if (c.Picked) return new AdventureSeedPacketVisual(128, 1f, costs);
            if ((c.NativeMode == 20 || c.NativeMode == 24) && !c.Active) shade = 64;
            else if (!c.Playing) { shade = c.CutsceneDarken; remaining = 0f; }
            else if (c.TutorialState == 1 && c.TutorialTimer == -1 && c.PacketKind == 0 ||
                (c.TutorialState == 5 || c.TutorialState == 9) && c.PacketKind == 1 || c.TutorialState == 29)
                shade = TutorialFlashColor(c.MainCounter, 75);
            else if (c.EasyPlantingCheat) remaining = 0f;
            else if (!c.CanAfford && costs || remaining > 0f || !c.PlantingRequirementsMet) shade = 128;
            return new AdventureSeedPacketVisual(shade, remaining, costs);
        }
        // PC counter advances once per 100Hz logical update and completes on strictly GREATER.
        // Host can retain its own simulation state and use this only as the native event reference.
        public static AdventureSeedPacketCooldownResult AdvanceCooldown(AdventureSeedPacketCooldown state, bool playing = true, bool seedPresent = true)
        {
            bool completed = false;
            if (playing && seedPresent && !state.Active && state.Refreshing)
            {
                state.Counter++;
                if (state.Counter > state.Duration)
                { state.Counter = 0; state.Refreshing = false; state.Active = true; completed = true; }
            }
            return new AdventureSeedPacketCooldownResult(state, completed);
        }
        public static bool CanPickUp(AdventureSeedBankContext c)
        {
            if (c.Paused || !c.Playing || c.PacketKind < 0 || c.SlotMachine) return false;
            if (c.EasyPlantingCheat) return true;
            return c.Active && (c.CanAfford || c.Conveyor) && c.PlantingRequirementsMet;
        }
        // Invoke at a REAL native-ready check, never once per draw frame.
        // Native callers: board mainCounter0; cooldown completion; sun affordability threshold;
        // and original packet activation/refresh-all actions. CanPickUp is checked at that event.
        public static AdventureSeedPacketFlash ReadyCheck(AdventureSeedBankContext c, float bankX, float bankY, float packetX, float packetY)
        { return new AdventureSeedPacketFlash(CanPickUp(c) && !c.EasyPlantingCheat && !c.Conveyor, bankX + packetX, bankY + packetY); }
        // PC 0x43D740 tests money + suns being collected - cost in [0,newSunValue).
        public static bool SunCrossesAffordability(int sunMoney, int sumAlreadyCollectingIncludingNewSun, int cost, int newSunValue)
        {
            int balance = sunMoney + sumAlreadyCollectingIncludingNewSun - cost;
            return balance >= 0 && balance < newSunValue;
        }
        public static int SunRenderOrder(bool collecting, bool fog, bool conveyor = false)
        { return collecting && fog && !conveyor ? FogCollectedSunRenderOrder : OrdinarySunRenderOrder; }
        // Only the original tutorial states use this exact integer triangle. Normal cooldown has no pulse.
        public static int TutorialFlashColor(int counter, int flashTime)
        {
            if (flashTime < 2) throw new ArgumentOutOfRangeException(nameof(flashTime));
            int half = flashTime / 2;
            return Math.Min(255, Math.Max(0, 55 + Math.Abs(half - counter % flashTime) * 200 / half));
        }
    }
}
