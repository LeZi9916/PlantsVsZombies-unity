#nullable enable

using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure coin.</summary>
    public sealed class AdventureCoin
    {
        /// <summary>Gets or sets id.</summary>
        public int Id { get; internal set; }
        /// <summary>Gets or sets kind.</summary>
        public AdventureCoinKind Kind { get; internal set; }
        /// <summary>Gets or sets gift plant.</summary>
        public PlantKind? GiftPlant { get; internal set; }

        /// <summary>Gets or sets value units.</summary>
        public int ValueUnits
        {
            get
            {
                return Kind == AdventureCoinKind.Silver ? 1 : Kind == AdventureCoinKind.Gold ? 5 : Kind == AdventureCoinKind.Diamond ? 100 : 0;
            }
        }

        /// <summary>Gets or sets x.</summary>
        public float X { get; internal set; }
        /// <summary>Gets or sets row.</summary>
        public float Row { get; internal set; }
        /// <summary>Gets or sets age.</summary>
        public float Age { get; internal set; }
        /// <summary>Gets or sets is being collected.</summary>
        public bool IsBeingCollected { get; internal set; }
        /// <summary>Gets or sets persistence pending.</summary>
        public bool PersistencePending { get; internal set; }
        /// <summary>Gets or sets collector plant id.</summary>
        public int CollectorPlantId { get; internal set; }

        /// <summary>Stores collection remaining.</summary>
        internal float _collectionRemaining;
    }
}
