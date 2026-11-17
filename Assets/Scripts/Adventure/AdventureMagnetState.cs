#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC Magnet-shroom's ready, sucking and charging reanimation state.</summary>
    public sealed class AdventureMagnetState : PlantsVsZombies.Simulation.ISimulationPooledObject
    {
        /// <summary>Stores the original Ready1, MagnetshroomSucking26 or MagnetshroomCharging27 phase.</summary>
        public int NativePhase = 1;
        /// <summary>Stores the original cooldown, counted from the start of attraction for fifteen hundred native updates.</summary>
        public int CooldownTicks;
        /// <summary>Stores elapsed seconds in the current original body clip.</summary>
        public float AnimationSeconds;
        /// <summary>Stores the current original body frame rate.</summary>
        public float AnimationRate = 12f;
        /// <summary>Stores the native body-transition blend in one-hundred-hertz updates.</summary>
        public int BlendTicks = 20;
        /// <summary>Retains the one original metal item until charging finishes.</summary>
        public AdventureMagnetItem? Item;
        /// <summary>Gets the original body animation for the retained phase.</summary>
        public string Animation => NativePhase == 26 ? "anim_shooting" : NativePhase == 27 ? "anim_nonactive_idle2" : "anim_idle";
        /// <summary>Restores all mutable fields to construction defaults after live references expire.</summary>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.ResetForPool()
        {
            NativePhase = 1;
            CooldownTicks = default;
            AnimationSeconds = default;
            AnimationRate = 12f;
            BlendTicks = 20;
            Item = default;
        }

        /// <summary>Retains linked records while this object is reachable from the board.</summary>
        /// <param name="pool">The owning runtime lifetime tracker.</param>
        void PlantsVsZombies.Simulation.ISimulationPooledObject.MarkPoolReferences(PlantsVsZombies.Simulation.SimulationRuntimePool pool)
        {
            pool.Mark(Item);

        }

    }
}
