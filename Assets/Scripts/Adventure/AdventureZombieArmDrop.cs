#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie arm drop.</summary>
    public sealed class AdventureZombieArmDrop : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores id, seed, native kind.</summary>
        public int Id, Seed, NativeKind;
        /// <summary>Stores the source dolphin or pogo zombie's original phase before detachment, or minus one for other species.</summary>
        public int NativePhase = -1;
        /// <summary>Stores the source dolphin or pogo zombie's native altitude before detachment.</summary>
        public float NativeAltitude;
        /// <summary>Stores x, row, age, scale, animation age, rate.</summary>
        public float X, Row, Age, Scale, AnimationAge, Rate;
        /// <summary>Stores asset, animation, anchor, image.</summary>
        public string Asset = string.Empty, Animation = string.Empty, Anchor = string.Empty, Image = string.Empty;
        /// <summary>Stores mowed, backwards, chilled, hypnotized.</summary>
        public bool Mowed, Backwards, Chilled, Hypnotized;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            Id = default;
            Seed = default;
            NativeKind = default;
            NativePhase = -1;
            NativeAltitude = default;
            X = default;
            Row = default;
            Age = default;
            Scale = default;
            AnimationAge = default;
            Rate = default;
            Asset = string.Empty;
            Animation = string.Empty;
            Anchor = string.Empty;
            Image = string.Empty;
            Mowed = default;
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
