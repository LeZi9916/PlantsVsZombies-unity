#nullable enable

using PlantsVsZombies.Simulation;
using System;

namespace PlantsVsZombies.Progression
{
    /// <summary>Defines garden plant.</summary>
    public sealed class GardenPlant
    {
        /// <summary>Gets or sets id.</summary>
        public Guid Id { get; internal set; }
        /// <summary>Gets or sets original index.</summary>
        public int OriginalIndex { get; internal set; } = -1;
        /// <summary>Gets or sets kind.</summary>
        public PlantKind Kind { get; internal set; }
        /// <summary>Gets or sets garden.</summary>
        public GardenKind Garden { get; internal set; }
        /// <summary>Gets or sets slot.</summary>
        public int Slot { get; internal set; }
        /// <summary>Gets or sets growth.</summary>
        public int Growth { get; internal set; }
        /// <summary>Gets or sets waterings.</summary>
        public int Waterings { get; internal set; }
        /// <summary>Gets or sets feedings per grow.</summary>
        public int FeedingsPerGrow { get; internal set; } = 3;
        /// <summary>Gets or sets mature need.</summary>
        public GardenNeed MatureNeed { get; internal set; } = GardenNeed.BugSpray;
        /// <summary>Gets or sets next care utc.</summary>
        public long NextCareUtc { get; internal set; }
        /// <summary>Gets or sets happy until utc.</summary>
        public long HappyUntilUtc { get; internal set; }
        /// <summary>Gets or sets last water utc.</summary>
        public long LastWaterUtc { get; internal set; }
        /// <summary>Gets or sets last need fulfilled utc.</summary>
        public long LastNeedFulfilledUtc { get; internal set; }
        /// <summary>Gets or sets last fertilized utc.</summary>
        public long LastFertilizedUtc { get; internal set; }
        /// <summary>Gets or sets chocolate until utc.</summary>
        public long ChocolateUntilUtc { get; internal set; }
        /// <summary>Gets or sets last coin utc.</summary>
        public long LastCoinUtc { get; internal set; }
        /// <summary>Gets or sets pending coins.</summary>
        public int PendingCoins { get; internal set; }
        /// <summary>Gets or sets care cycle.</summary>
        public int CareCycle { get; internal set; }

        /// <summary>Performs copy.</summary>
        /// <returns>The resulting copy.</returns>
        internal GardenPlant Copy()
        {
            return (GardenPlant)MemberwiseClone();
        }

        /// <summary>Gets or sets in preferred garden.</summary>
        public bool InPreferredGarden
        {
            get
            {
                return IsAquatic(Kind) ? Garden == GardenKind.Aquarium : IsMushroom(Kind) ? Garden == GardenKind.Mushroom : Garden == GardenKind.Greenhouse;
            }
        }

        /// <summary>Performs need.</summary>
        /// <param name = "utc">The utc.</param>
        /// <returns>The resulting need.</returns>
        public GardenNeed Need(long utc)
        {
            if (LastNeedFulfilledUtc > 0 && (utc - LastNeedFulfilledUtc < 3600 || DateTimeOffset.FromUnixTimeSeconds(utc).LocalDateTime.Date == DateTimeOffset.FromUnixTimeSeconds(LastNeedFulfilledUtc).LocalDateTime.Date))
            {
                return GardenNeed.Happy;
            }

            if (utc < NextCareUtc)
            {
                return GardenNeed.Resting;
            }

            if (Growth > 0 && !InPreferredGarden)
            {
                return GardenNeed.Resting;
            }

            if (!IsAquatic(Kind) || Growth == 0)
            {
                if (Waterings < FeedingsPerGrow)
                {
                    return GardenNeed.Water;
                }
            }

            if (Growth < 3)
            {
                return GardenNeed.Fertilizer;
            }

            return MatureNeed;
        }

        /// <summary>Performs is aquatic.</summary>
        /// <param name = "k">The k.</param>
        /// <returns>The resulting is aquatic.</returns>
        public static bool IsAquatic(PlantKind k)
        {
            return k == PlantKind.LilyPad || k == PlantKind.TangleKelp || k == PlantKind.SeaShroom || k == PlantKind.Cattail;
        }

        /// <summary>Performs is mushroom.</summary>
        /// <param name = "k">The k.</param>
        /// <returns>The resulting is mushroom.</returns>
        public static bool IsMushroom(PlantKind k)
        {
            return (int)k >= 8 && (int)k <= 15 || k == PlantKind.SeaShroom || k == PlantKind.MagnetShroom || k == PlantKind.GloomShroom;
        }
    }
}
