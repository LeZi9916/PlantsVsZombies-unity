#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains a native foot-dust burst at its fixed board position for twenty simulation ticks.</summary>
    public struct AdventureZombieFootstepEffect
    {
        /// <summary>Identifies this short-lived particle burst within the presentation.</summary>
        public int Id;
        /// <summary>Identifies the lawn row whose zombie layer covers this burst.</summary>
        public int Row;
        /// <summary>Seeds the original particle emitter's deterministic variation.</summary>
        public int Seed;
        /// <summary>Stores the original unscaled horizontal board coordinate.</summary>
        public float X;
        /// <summary>Stores the original unscaled vertical board coordinate.</summary>
        public float Y;
        /// <summary>Stores the elapsed burst lifetime in seconds.</summary>
        public float Age;
    }
}
