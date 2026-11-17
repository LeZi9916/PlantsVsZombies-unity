#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original pool body's altitude and independently running splash and seaweed effects.</summary>
    public sealed class AdventureZombiePoolState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores whether the original plain body was initialized with its duck tube and water tracks.</summary>
        public bool HasDuckyTube;
        /// <summary>Stores the original body altitude, with negative values placing the body below the water surface.</summary>
        public float Altitude;
        /// <summary>Stores the last native splash animation age, or minus one when there is no splash.</summary>
        public float SplashAge = -1f;
        /// <summary>Stores the fixed native board origin where the last splash reanimation was created.</summary>
        public float SplashNativeX, SplashNativeY;
        /// <summary>Stores the original attached seaweed age, or minus one when this zombie did not emerge from the pool.</summary>
        public float SeaweedAge = -1f;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            HasDuckyTube = default;
            Altitude = default;
            SplashAge = -1f;
            SplashNativeX = default;
            SplashNativeY = default;
            SeaweedAge = -1f;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
