#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie sound event.</summary>
    public sealed class AdventureZombieSoundEvent : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores id, foley, tick.</summary>
        public int Id, Foley, Tick;
        /// <summary>Stores whether simulation has resolved this request's original playback choices.</summary>
        public bool Resolved;
        /// <summary>Stores the selected original variation index, or minus one when native admission rejects playback.</summary>
        public int SampleIndex = -1;
        /// <summary>Stores the original explicit and randomized pitch after the native float store.</summary>
        public float ResolvedPitch;
        /// <summary>Stores the original explicit Foley pitch adjustment in semitones.</summary>
        public float Pitch;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            Foley = default;
            Tick = default;
            Resolved = default;
            SampleIndex = -1;
            ResolvedPitch = default;
            Pitch = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
