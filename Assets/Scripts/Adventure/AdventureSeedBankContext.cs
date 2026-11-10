#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure seed bank context.</summary>
    public sealed class AdventureSeedBankContext
    {
        /// <summary>Stores packet kind, imitater kind.</summary>
        public int PacketKind = -1, ImitaterKind = -1;
        /// <summary>Stores active, refreshing, playing, paused, picked.</summary>
        public bool Active = true, Refreshing, Playing = true, Paused, Picked;
        /// <summary>Stores refresh counter, refresh time, main counter, native mode, tutorial state, tutorial timer.</summary>
        public int RefreshCounter, RefreshTime, MainCounter, NativeMode, TutorialState, TutorialTimer;
        /// <summary>Stores cutscene darken, slot machine countdown.</summary>
        public int CutsceneDarken = 255, SlotMachineCountdown;
        /// <summary>Stores can afford, planting requirements met, easy planting cheat, conveyor, slot machine.</summary>
        public bool CanAfford = true, PlantingRequirementsMet = true, EasyPlantingCheat, Conveyor, SlotMachine;

        /// <summary>Restores construction defaults before this context is filled for another packet.</summary>
        internal void Reset()
        {
            PacketKind = -1;
            ImitaterKind = -1;
            Active = true;
            Refreshing = false;
            Playing = true;
            Paused = false;
            Picked = false;
            RefreshCounter = 0;
            RefreshTime = 0;
            MainCounter = 0;
            NativeMode = 0;
            TutorialState = 0;
            TutorialTimer = 0;
            CutsceneDarken = 255;
            SlotMachineCountdown = 0;
            CanAfford = true;
            PlantingRequirementsMet = true;
            EasyPlantingCheat = false;
            Conveyor = false;
            SlotMachine = false;
        }
    }
}
