#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Progression
{
    /// <summary>Defines player account.</summary>
    public sealed class PlayerAccount
    {
        /// <summary>Gets or sets profile id.</summary>
        public Guid ProfileId { get; internal set; }
        /// <summary>Gets or sets coins.</summary>
        public int Coins { get; internal set; }

        /// <summary>Gets or sets display coins.</summary>
        public long DisplayCoins
        {
            get
            {
                return (long)Coins * 10;
            }
        }

        /// <summary>Gets whether the magic taco diamond is waiting to be collected.</summary>
        public bool HasPendingTacoReward
        {
            get
            {
                return _receipts.ContainsKey("taco:pending");
            }
        }

        /// <summary>Gets whether Dave has presented the pending taco diamond.</summary>
        public bool TacoDiamondPresented
        {
            get
            {
                return _receipts.TryGetValue("taco:pending", out var state) && state == 2;
            }
        }

        /// <summary>Gets the number of adventure packet upgrade offers declined.</summary>
        public int PacketUpgradeDeclines { get; internal set; }

        /// <summary>Gets or sets tree height.</summary>
        public int TreeHeight { get; internal set; }
        /// <summary>Gets whether Dave has provided the first five bags of tree food.</summary>
        public bool TreeFoodGiftClaimed { get; internal set; } = true;
        /// <summary>Gets or sets snail awake until utc.</summary>
        public long SnailAwakeUntilUtc { get; internal set; }
        /// <summary>Gets or sets garden gift claimed.</summary>
        public bool GardenGiftClaimed { get; internal set; }
        /// <summary>Gets or sets minigames unlocked.</summary>
        public bool MinigamesUnlocked { get; internal set; }
        /// <summary>Gets or sets puzzle unlocked.</summary>
        public bool PuzzleUnlocked { get; internal set; }
        /// <summary>Gets or sets survival unlocked.</summary>
        public bool SurvivalUnlocked { get; internal set; }

        /// <summary>Stores reward cycle.</summary>
        internal int _rewardCycle;
        /// <summary>Stores reward mask.</summary>
        internal ulong _rewardMask;
        /// <summary>Stores purchases.</summary>
        internal int[] _purchases = new int[80];
        /// <summary>Stores mode records.</summary>
        internal int[] _modeRecords = new int[100];
        /// <summary>Stores garden.</summary>
        internal List<GardenPlant> _garden = new List<GardenPlant>();
        /// <summary>Stores receipts.</summary>
        internal Dictionary<string, int> _receipts = new Dictionary<string, int>();
        /// <summary>Gets the persisted Zen tutorial stage, or zero when no tutorial is pending.</summary>
        public int ZenGardenTutorialStage
        {
            get
            {
                return _receipts.TryGetValue("zen:tutorial", out var stage) ? stage : 0;
            }
        }

        /// <summary>Gets whether the original garden fertilizer counter has been initialized.</summary>
        public bool GardenFertilizerInitialized
        {
            get
            {
                if (PurchaseCount(14) > 0)
                {
                    return true;
                }

                if (_receipts.TryGetValue("zen:fertilizer", out var initialized))
                {
                    return initialized != 0;
                }

                // Older accounts predate this marker. A completed tutorial has already granted fertilizer.
                return GardenGiftClaimed && (ZenGardenTutorialStage == 0 || ZenGardenTutorialStage >= 6);
            }
        }

        /// <summary>Gets or sets plants.</summary>
        public IReadOnlyList<GardenPlant> Plants
        {
            get
            {
                return _garden.AsReadOnly();
            }
        }

        /// <summary>Performs purchase count.</summary>
        /// <param name = "originalStoreId">The original store id.</param>
        /// <returns>The resulting purchase count.</returns>
        public int PurchaseCount(int originalStoreId)
        {
            return originalStoreId >= 0 && originalStoreId < _purchases.Length ? _purchases[originalStoreId] : 0;
        }

        /// <summary>Performs mode record.</summary>
        /// <param name = "originalModeId">The original mode id.</param>
        /// <returns>The resulting mode record.</returns>
        public int ModeRecord(int originalModeId)
        {
            return originalModeId >= 0 && originalModeId < _modeRecords.Length ? _modeRecords[originalModeId] : 0;
        }

        /// <summary>Gets or sets packet slots.</summary>
        public int PacketSlots
        {
            get
            {
                return 6 + Math.Min(4, PurchaseCount(21));
            }
        }

        /// <summary>Performs owns.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting owns.</returns>
        public bool Owns(PlantKind kind)
        {
            return (int)kind >= 40 && (int)kind <= 48 && _purchases[(int)kind - 40] > 0;
        }

        /// <summary>Performs seed access.</summary>
        /// <param name = "imitater">The imitater.</param>
        /// <returns>The resulting seed access.</returns>
        public AdventureSeedAccess SeedAccess(PlantKind? imitater = null)
        {
            var seeds = new List<PlantKind>();
            for (var i = 0; i < 9; i++)
            {
                if (_purchases[i] > 0)
                {
                    seeds.Add((PlantKind)(40 + i));
                }
            }

            return new AdventureSeedAccess(seeds, Owns(PlantKind.Imitater) ? imitater : null);
        }

        /// <summary>Performs copy.</summary>
        /// <returns>The resulting copy.</returns>
        internal PlayerAccount Copy()
        {
            var p = (PlayerAccount)MemberwiseClone();
            p._purchases = (int[])_purchases.Clone();
            p._modeRecords = (int[])_modeRecords.Clone();
            p._garden = new List<GardenPlant>();
            foreach (var plant in _garden)
            {
                p._garden.Add(plant.Copy());
            }

            p._receipts = new Dictionary<string, int>(_receipts);
            return p;
        }
    }
}
