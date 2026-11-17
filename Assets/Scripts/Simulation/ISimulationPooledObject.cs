#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Provides explicit reset and reference traversal without reflection in the game loop.</summary>
    internal interface ISimulationPooledObject
    {
        /// <summary>Restores construction defaults only after all board roots release the object.</summary>
        void ResetForPool();

        /// <summary>Retains linked records until the next complete lifetime boundary.</summary>
        /// <param name="pool">The match-local owner marking reachable records.</param>
        void MarkPoolReferences(SimulationRuntimePool pool);
    }
}
