#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains original vehicle particles and basketball deflections after their source leaves the board.</summary>
    public sealed class AdventureZomboniParticle : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the independent particle identity, random seed, and native lawn row.</summary>
        public int Id, Seed, Row;
        /// <summary>Stores the original board-pixel creation position.</summary>
        public float X, Y;
        /// <summary>Stores the elapsed original particle-system time in seconds.</summary>
        public float Age;
        /// <summary>Selects the original global top layer for a reflected basketball particle.</summary>
        public bool Foreground;
        /// <summary>Stores the unchanged original particle definition name.</summary>
        public string Particle = "ZamboniExplosion";
        /// <summary>Gets the authored maximum system duration from the original XML.</summary>
        public float Duration => Particle == "ZamboniTire" ? 5f : 2f;
        /// <summary>Restores construction defaults after all authoritative owners release this record.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            Seed = default;
            Row = default;
            X = default;
            Y = default;
            Age = default;
            Foreground = default;
            Particle = "ZamboniExplosion";
        }

        /// <summary>Retains linked model records until this effect leaves every authoritative owner.</summary>
        /// <param name="pool">The match-local lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {
        }
    }
}
