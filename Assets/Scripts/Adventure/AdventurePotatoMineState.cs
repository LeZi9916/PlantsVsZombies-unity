#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure potato mine state.</summary>
    public sealed class AdventurePotatoMineState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores countdown, phase ticks, glow frames, nearest distance pixels.</summary>
        public int Countdown = 1500, PhaseTicks, GlowFrames = 10, NearestDistancePixels = 1000;
        /// <summary>Stores body rate, glow time.</summary>
        public float BodyRate = 12f, GlowTime;
        /// <summary>Stores light created.</summary>
        public bool LightCreated;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Countdown = 1500;
            PhaseTicks = default;
            GlowFrames = 10;
            NearestDistancePixels = 1000;
            BodyRate = 12f;
            GlowTime = default;
            LightCreated = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
