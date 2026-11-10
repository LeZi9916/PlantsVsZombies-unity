#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>The production contribution to the PC plant's +0xB8 additive-flash counter.</summary>
    public struct AdventureProducerGlowState
    {
        /// <summary>Stores kind.</summary>
        public PlantKind Kind;
        /// <summary>Stores initialized.</summary>
        public bool Initialized;
        /// <summary>Stores flash ticks.</summary>
        public int FlashTicks;
        /// <summary>Stores launch counter after tick.</summary>
        public int LaunchCounterAfterTick;
        /// <summary>Stores production advanced.</summary>
        public bool ProductionAdvanced;
        /// <summary>Stores produced this tick.</summary>
        public bool ProducedThisTick;
    }
}
