#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original dancer's phase counters and four independently owned backup slots.</summary>
    public sealed class AdventureDancerState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Identifies the summoning leader, or zero for an independent dancer.</summary>
        public int LeaderId;

        /// <summary>Stores the original upper, lower, left, and right backup identifiers.</summary>
        public readonly int[] Followers = new int[4];

        /// <summary>Counts native updates remaining in the entrance, pointing hold, or emergence.</summary>
        public int PhaseTicks;

        /// <summary>Counts updates until a missing backup can be summoned on dance frame twelve.</summary>
        public int SummonTicks;

        /// <summary>Tracks the current phase's unpaused animation time in seconds.</summary>
        public double PhaseAge;

        /// <summary>Retains the native blend duration selected when the current body clip starts.</summary>
        public int AnimationBlendTicks;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            LeaderId = default;
            System.Array.Clear(Followers, 0, Followers.Length);
            PhaseTicks = default;
            SummonTicks = default;
            PhaseAge = default;
            AnimationBlendTicks = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
