#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains one member's native sled position and boarding or crash countdown.</summary>
    public sealed class AdventureBobsledState
    {
        /// <summary>Identifies the front zombie whose helmet health belongs to the whole sled.</summary>
        public int LeaderId;
        /// <summary>Stores the original front-to-back member index, from zero through three.</summary>
        public int Position;
        /// <summary>Stores the original five-hundred-update push or one-hundred-fifty-update crash countdown.</summary>
        public int PhaseTicks = 500;
    }
}
