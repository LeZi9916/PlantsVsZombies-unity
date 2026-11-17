#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC 542D60's thrown imp velocity and previous native roof floor.</summary>
    public sealed class AdventureThrownImpState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the original vertical velocity, reduced by .05 each update.</summary>
        public float VelocityZ;
        /// <summary>Stores the previous unscaled native row position for roof rebasing.</summary>
        public float NativeY;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            VelocityZ = default;
            NativeY = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
