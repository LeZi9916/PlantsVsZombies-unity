#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie part drop.</summary>
    public sealed class AdventureZombiePartDrop : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores id, seed, native kind, row, particle id.</summary>
        public int Id, Seed, NativeKind, Row, ParticleId;
        /// <summary>Stores the source dolphin or pogo zombie's original phase before detachment, or minus one for other species.</summary>
        public int NativePhase = -1;
        /// <summary>Stores the source dolphin or pogo zombie's native altitude before detachment.</summary>
        public float NativeAltitude;
        /// <summary>Stores x, age, scale, animation age, rate.</summary>
        public float X, Age, Scale, AnimationAge, Rate;
        /// <summary>Stores asset, animation, anchor, image.</summary>
        public string? Asset = string.Empty, Animation = string.Empty, Anchor = string.Empty, Image = string.Empty;
        /// <summary>Stores backwards, chilled, hypnotized.</summary>
        public bool Backwards, Chilled, Hypnotized;
        /// <summary>Gets or sets duration.</summary>
        public float Duration
        {
            get
            {
                return ParticleId == 12 || ParticleId == 13 || ParticleId == 16 || ParticleId == 54 ? .4f : ParticleId == 8 || ParticleId == 9 || ParticleId == 10 || ParticleId == 11 || ParticleId == 14 || ParticleId == 74 ? .5f : 1.8f;
            }
        }
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            Seed = default;
            NativeKind = default;
            Row = default;
            ParticleId = default;
            NativePhase = -1;
            NativeAltitude = default;
            X = default;
            Age = default;
            Scale = default;
            AnimationAge = default;
            Rate = default;
            Asset = string.Empty;
            Animation = string.Empty;
            Anchor = string.Empty;
            Image = string.Empty;
            Backwards = default;
            Chilled = default;
            Hypnotized = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
