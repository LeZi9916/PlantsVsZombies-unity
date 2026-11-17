#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Captures one original magnetic item and its source pose independently of the later disarmed zombie.</summary>
    public sealed class AdventureMagnetItem : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the PC MagnetItemType identifier, including damage variants and placed ladders.</summary>
        public int NativeKind;
        /// <summary>Counts completed attraction updates after the item was removed from its source.</summary>
        public int AgeTicks;
        /// <summary>Stores the original local destination offsets from the Magnet-shroom's native plant position.</summary>
        public float DestinationX, DestinationY;
        /// <summary>Stores the original initial world position once the retained source track is resolved.</summary>
        public float X, Y;
        /// <summary>Records whether the initial source position is fixed or has already been resolved from original artwork.</summary>
        public bool PositionResolved;
        /// <summary>Retains the original source zombie pose used by GetTrackPosition, when this item uses a reanimation track.</summary>
        public AdventureZombiePartDrop? Source;
        /// <summary>Names the original image whose integer half dimensions are subtracted at detachment.</summary>
        public string? AnchorImage;
        /// <summary>Stores the original extra unscaled source offsets, notably the Pogo stick's forty and eighty-four pixels.</summary>
        public float SourceOffsetX, SourceOffsetY;

        /// <summary>Restores construction defaults only after the plant releases this source snapshot.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            NativeKind = 0;
            AgeTicks = 0;
            DestinationX = 0f;
            DestinationY = 0f;
            X = 0f;
            Y = 0f;
            PositionResolved = false;
            Source = null;
            AnchorImage = null;
            SourceOffsetX = 0f;
            SourceOffsetY = 0f;
        }

        /// <summary>Retains the removed zombie part independently of the live zombie.</summary>
        /// <param name="pool">The owning lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {
            pool.Mark(Source);
        }
    }
}
