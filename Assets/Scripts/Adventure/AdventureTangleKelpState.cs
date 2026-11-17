#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC kelp's hundred-update grab and its original zombie attachment.</summary>
    public sealed class AdventureTangleKelpState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the remaining original plant-state updates.</summary>
        public int Countdown = 100;
        /// <summary>Stores the stable identifier of the grabbed zombie.</summary>
        public int TargetZombieId;
        /// <summary>Stores the original unscaled attachment offsets from the zombie's native position.</summary>
        public int OffsetX, OffsetY;

        /// <summary>Samples the original once-and-hold grab without changing the native countdown.</summary>
        /// <param name="remainder">The nonnegative time since the last simulation update.</param>
        /// <returns>The attachment's elapsed animation time in seconds.</returns>
        public float SampleSeconds(float remainder)
        {
            return (100 - Countdown) * .01f + Math.Max(0f, remainder);
        }
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Countdown = 100;
            TargetZombieId = default;
            OffsetX = default;
            OffsetY = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
