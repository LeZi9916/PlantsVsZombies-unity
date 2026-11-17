#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC pogo's independent bounce counter and held body animation.</summary>
    public sealed class AdventurePogoState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the remaining original updates in the eighty-update bounce.</summary>
        public int PhaseTicks;
        /// <summary>Stores the original pogo body's forty-FPS PlayOnceAndHold clock.</summary>
        public float AnimationSeconds = 11f / 40f;
        /// <summary>Stores the current native body transition blend in one-hundred-hertz updates.</summary>
        public int AnimationBlendTicks;
        /// <summary>Retains the original raised-lawn flag while the pogo crosses a high-ground boundary.</summary>
        public bool OnHighGround;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            PhaseTicks = default;
            AnimationSeconds = 11f / 40f;
            AnimationBlendTicks = default;
            OnHighGround = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
