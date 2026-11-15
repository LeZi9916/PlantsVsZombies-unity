#nullable enable

using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Exposes read-only match observations to experimental rule callbacks.</summary>
    public sealed class SimulationView
    {
        /// <summary>Stores the match state.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Creates a view of the supplied match.</summary>
        /// <param name="world">The observed match.</param>
        internal SimulationView(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Gets the current plants in update order.</summary>
        public IReadOnlyList<Plant> Plants => _world.Plants;
        /// <summary>Gets the current zombies in update order.</summary>
        public IReadOnlyList<Zombie> Zombies => _world.Zombies;
        /// <summary>Gets elapsed simulation seconds.</summary>
        public float Elapsed => _world.Elapsed;
        /// <summary>Gets the match state.</summary>
        public MatchState State => _world.State;
        /// <summary>Gets the spendable sun total.</summary>
        public int SunAmount => _world.SunAmount;
    }
}
