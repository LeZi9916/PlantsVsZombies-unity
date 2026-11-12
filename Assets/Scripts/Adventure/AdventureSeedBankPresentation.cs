#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure seed bank presentation.</summary>
    public static class AdventureSeedBankPresentation
    {
        /// <summary>Stores seed bank object render order, ordinary sun render order, fog collected sun render order.</summary>
        public const int SeedBankObjectRenderOrder = 400000, OrdinarySunRenderOrder = 600001, FogCollectedSunRenderOrder = 800002;
        /// <summary>Stores evidence.</summary>
        public const string Evidence = "PC 0x49CDC0/0x49DDB0/0x49D260/0x49CC70/0x528520; 0x43B610/0x43D740; original SeedPacketFlash.xml";
        /// <summary>Performs sample.</summary>
        /// <param name = "c">The c.</param>
        /// <returns>The resulting sample.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static AdventureSeedPacketVisual Sample(AdventureSeedBankContext c)
        {
            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }

            var remaining = c.Active ? 0f : c.RefreshTime == 0 ? 1f : (c.RefreshTime - c.RefreshCounter) / (float)c.RefreshTime;
            var shade = 255;
            var costs = !c.Conveyor && !c.SlotMachine;
            if (c.SlotMachineCountdown > 0)
            {
                return new AdventureSeedPacketVisual(128, 0f, false);
            }

            // PC 0x49E130 clears active/refreshing/counter/duration when picked;
            // 0x49DDB0 then draws its full native dark mask, with no outline image.
            if (c.Picked)
            {
                return new AdventureSeedPacketVisual(128, 1f, costs);
            }

            if ((c.NativeMode == 20 || c.NativeMode == 24) && !c.Active)
            {
                shade = 64;
            }
            else if (!c.Playing)
            {
                shade = c.CutsceneDarken;
                remaining = 0f;
            }
            else if (c.TutorialState == 1 && c.TutorialTimer == -1 && c.PacketKind == 0 || (c.TutorialState == 5 || c.TutorialState == 9) && c.PacketKind == 1 || c.TutorialState == 29)
            {
                shade = TutorialFlashColor(c.MainCounter, 75);
            }
            else if (c.EasyPlantingCheat)
            {
                remaining = 0f;
            }
            else if (!c.CanAfford && costs || remaining > 0f || !c.PlantingRequirementsMet)
            {
                shade = 128;
            }

            return new AdventureSeedPacketVisual(shade, remaining, costs);
        }

        // PC counter advances once per 100Hz logical update and completes on strictly GREATER.
        // Host can retain its own simulation state and use this only as the native event reference.
        /// <summary>Performs advance cooldown.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "playing">The playing.</param>
        /// <param name = "seedPresent">The seed present.</param>
        /// <returns>The resulting advance cooldown.</returns>
        public static AdventureSeedPacketCooldownResult AdvanceCooldown(AdventureSeedPacketCooldown state, bool playing = true, bool seedPresent = true)
        {
            var completed = false;
            if (playing && seedPresent && !state.Active && state.Refreshing)
            {
                state.Counter++;
                if (state.Counter > state.Duration)
                {
                    state.Counter = 0;
                    state.Refreshing = false;
                    state.Active = true;
                    completed = true;
                }
            }

            return new AdventureSeedPacketCooldownResult(state, completed);
        }

        /// <summary>Performs can pick up.</summary>
        /// <param name = "c">The c.</param>
        /// <returns>The resulting can pick up.</returns>
        public static bool CanPickUp(AdventureSeedBankContext c)
        {
            if (c.Paused || !c.Playing || c.PacketKind < 0 || c.SlotMachine)
            {
                return false;
            }

            if (c.EasyPlantingCheat)
            {
                return true;
            }

            return c.Active && (c.CanAfford || c.Conveyor) && c.PlantingRequirementsMet;
        }

        // Invoke at a REAL native-ready check, never once per draw frame.
        // Native callers: board mainCounter0; cooldown completion; sun affordability threshold;
        // and original packet activation/refresh-all actions. CanPickUp is checked at that event.
        /// <summary>Performs ready check.</summary>
        /// <param name = "c">The c.</param>
        /// <param name = "bankX">The bank x.</param>
        /// <param name = "bankY">The bank y.</param>
        /// <param name = "packetX">The packet x.</param>
        /// <param name = "packetY">The packet y.</param>
        /// <returns>The resulting ready check.</returns>
        public static AdventureSeedPacketFlash ReadyCheck(AdventureSeedBankContext c, float bankX, float bankY, float packetX, float packetY)
        {
            return new AdventureSeedPacketFlash(CanPickUp(c) && !c.EasyPlantingCheat && !c.Conveyor, bankX + packetX, bankY + packetY);
        }

        // PC 0x43D740 tests money + suns being collected - cost in [0,newSunValue).
        /// <summary>Performs sun crosses affordability.</summary>
        /// <param name = "sunMoney">The sun money.</param>
        /// <param name = "sumAlreadyCollectingIncludingNewSun">The sum already collecting including new sun.</param>
        /// <param name = "cost">The cost.</param>
        /// <param name = "newSunValue">The new sun value.</param>
        /// <returns>The resulting sun crosses affordability.</returns>
        public static bool SunCrossesAffordability(int sunMoney, int sumAlreadyCollectingIncludingNewSun, int cost, int newSunValue)
        {
            var balance = sunMoney + sumAlreadyCollectingIncludingNewSun - cost;
            return balance >= 0 && balance < newSunValue;
        }

        /// <summary>Performs sun render order.</summary>
        /// <param name = "collecting">The collecting.</param>
        /// <param name = "fog">The fog.</param>
        /// <param name = "conveyor">The conveyor.</param>
        /// <returns>The resulting sun render order.</returns>
        public static int SunRenderOrder(bool collecting, bool fog, bool conveyor = false)
        {
            return collecting && fog && !conveyor ? FogCollectedSunRenderOrder : OrdinarySunRenderOrder;
        }

        // Only the original tutorial states use this exact integer triangle. Normal cooldown has no pulse.
        /// <summary>Performs tutorial flash color.</summary>
        /// <param name = "counter">The counter.</param>
        /// <param name = "flashTime">The flash time.</param>
        /// <returns>The resulting tutorial flash color.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public static int TutorialFlashColor(int counter, int flashTime)
        {
            if (flashTime < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(flashTime));
            }

            var half = flashTime / 2;
            return Math.Min(255, Math.Max(0, 55 + Math.Abs(half - counter % flashTime) * 200 / half));
        }
    }
}
