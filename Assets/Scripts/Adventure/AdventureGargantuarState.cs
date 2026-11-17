#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC giant's authored weapon and once-per-clip action event.</summary>
    public sealed class AdventureGargantuarState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Identifies the original pole, crossing sign or zombie weapon variation.</summary>
        public int Weapon;
        /// <summary>Prevents repeating the current smash or throw animation's timed event.</summary>
        public bool ActionTriggered;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Weapon = default;
            ActionTriggered = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
