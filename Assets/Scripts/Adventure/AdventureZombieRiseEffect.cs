#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original independent ZombieRise particles after their zombie leaves the grave.</summary>
    public sealed class AdventureZombieRiseEffect : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Identifies this particle instance and its lawn row.</summary>
        public int Id, Row;

        /// <summary>Stores the original world-space particle origin and its elapsed lifetime.</summary>
        public float X, Y, Age;

        /// <summary>Gets whether this is the Whack-a-Zombie rise particle.</summary>
        public bool Whack;

        /// <summary>Gets whether this effect uses the original backup dancer emergence particles.</summary>
        public bool Dancer;

        /// <summary>Gets whether this is the original independent DiggerRise particle and rising dirt reanimation.</summary>
        public bool Digger;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            Row = default;
            X = default;
            Y = default;
            Age = default;
            Whack = default;
            Dancer = default;
            Digger = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
