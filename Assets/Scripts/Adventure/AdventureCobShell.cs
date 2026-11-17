#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure cob shell.</summary>
    public sealed class AdventureCobShell : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Gets or sets source plant id.</summary>
        public int SourcePlantId { get; internal set; }
        /// <summary>Gets or sets target x.</summary>
        public float TargetX { get; internal set; }
        /// <summary>Gets or sets target row.</summary>
        public float TargetRow { get; internal set; }
        /// <summary>Gets or sets flight remaining.</summary>
        public float FlightRemaining { get; internal set; }
        /// <summary>Retains the original descending projectile's top-left floor y, including its roof column.</summary>
        public float TargetNativeY { get; internal set; }
        /// <summary>Retains the original rising image center y before the projectile's altitude is applied.</summary>
        public float LaunchNativeY { get; internal set; }
        /// <summary>Gets or sets launch x.</summary>
        public float LaunchX { get; internal set; }
        /// <summary>Gets or sets launch row.</summary>
        public float LaunchRow { get; internal set; }
        /// <summary>Restores construction defaults after all authoritative owners release this record.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            SourcePlantId = default;
            TargetX = default;
            TargetRow = default;
            FlightRemaining = default;
            TargetNativeY = default;
            LaunchNativeY = default;
            LaunchX = default;
            LaunchRow = default;
        }

        /// <summary>Retains linked model records until this effect leaves every authoritative owner.</summary>
        /// <param name="pool">The match-local lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {
        }
    }
}
