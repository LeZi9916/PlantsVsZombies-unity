#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original dolphin jump's fixed update counter independently of its animation clock.</summary>
    public sealed class AdventureDolphinState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the original remaining jump updates, initialized to one hundred twenty on takeoff.</summary>
        public int JumpTicks;
        /// <summary>Stores the original clip-transition blend, including independent twenty-update meal transitions.</summary>
        public int AnimationBlendTicks;
        /// <summary>Identifies the latest zero-blend walking reset, including an unseen pose before immediate eating.</summary>
        public int WalkingPoseVersion;
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            JumpTicks = default;
            AnimationBlendTicks = default;
            WalkingPoseVersion = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {

        }

    }
}
