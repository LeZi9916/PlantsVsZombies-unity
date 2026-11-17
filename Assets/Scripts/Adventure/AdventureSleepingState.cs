#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC 46CB90's independent, freely positioned original Z reanimation.</summary>
    public sealed class AdventureSleepingState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the original random six-to-eight-frame-per-second playback rate.</summary>
        public float Rate;
        /// <summary>Stores the original random initial phase followed by independent simulation playback time.</summary>
        public float AnimationSeconds;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Rate = default;
            AnimationSeconds = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
