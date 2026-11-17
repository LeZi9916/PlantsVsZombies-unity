#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Stores the original PC chomper's bite, digestion, and swallow animation state.</summary>
    public sealed class AdventureChomperState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the original plant state: idle 1, bite 10, success 11, miss 12, chew 13, swallow 14.</summary>
        public int NativePhase = 1;
        /// <summary>Stores elapsed native animation ticks and the active native countdown.</summary>
        public int PhaseTicks, Countdown;
        /// <summary>Stores the animation selected by the original state transition.</summary>
        public string Animation = "anim_idle";
        /// <summary>Stores the original animation frame rate, with zero selecting the randomized idle rate.</summary>
        public float Rate;
        /// <summary>Gets whether the current original animation repeats.</summary>
        public bool Loop => NativePhase == 1 || NativePhase == 13;
        /// <summary>Gets elapsed time within the current original animation.</summary>
        public float AnimationAge => PhaseTicks * .01f;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            NativePhase = 1;
            PhaseTicks = default;
            Countdown = default;
            Animation = "anim_idle";
            Rate = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
