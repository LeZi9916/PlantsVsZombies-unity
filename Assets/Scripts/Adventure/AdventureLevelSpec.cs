#nullable enable
using PlantsVsZombies.Simulation;
using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>First adventure only. Tables come from the local EXE; mode rules are separately documented approximations.</summary>
    public sealed class AdventureLevelSpec
    {
        /// <summary>Gets or sets number.</summary>
        public int Number { get; private set; }
        /// <summary>Gets or sets cycle.</summary>
        public int Cycle { get; private set; } = 1;

        /// <summary>Gets or sets is replay.</summary>
        public bool IsReplay
        {
            get
            {
                return Cycle > 1;
            }
        }

        /// <summary>Stores purchased slots.</summary>
        private int _purchasedSlots = 6;
        /// <summary>Gets or sets label.</summary>
        public string Label
        {
            get
            {
                return ((Number - 1) / 10 + 1) + "-" + ((Number - 1) % 10 + 1);
            }
        }

        /// <summary>Gets or sets terrain.</summary>
        public AdventureTerrain Terrain { get; private set; }
        /// <summary>Gets or sets mode.</summary>
        public AdventureMode Mode { get; private set; }
        /// <summary>Gets or sets base wave count.</summary>
        public int BaseWaveCount { get; private set; }
        /// <summary>Gets or sets wave count.</summary>
        public int WaveCount { get; private set; }

        /// <summary>Gets or sets rows.</summary>
        public int Rows
        {
            get
            {
                return HasPool ? 6 : 5;
            }
        }

        /// <summary>Gets or sets starting sun.</summary>
        public int StartingSun
        {
            get
            {
                return Mode == AdventureMode.WhackAZombie || Mode == AdventureMode.Vasebreaker || HasConveyor ? 0 : Number == 1 && !IsReplay ? 150 : 50;
            }
        }

        /// <summary>Gets or sets is night.</summary>
        public bool IsNight
        {
            get
            {
                return Terrain == AdventureTerrain.Night || Terrain == AdventureTerrain.Fog || Terrain == AdventureTerrain.BossRoof;
            }
        }

        /// <summary>Gets or sets has pool.</summary>
        public bool HasPool
        {
            get
            {
                return Terrain == AdventureTerrain.Pool || Terrain == AdventureTerrain.Fog;
            }
        }

        /// <summary>Gets or sets has roof.</summary>
        public bool HasRoof
        {
            get
            {
                return Terrain == AdventureTerrain.Roof || Terrain == AdventureTerrain.BossRoof;
            }
        }

        /// <summary>Gets or sets has conveyor.</summary>
        public bool HasConveyor
        {
            get
            {
                return Mode == AdventureMode.Conveyor || Mode == AdventureMode.Bowling || Mode == AdventureMode.LittleTrouble || Mode == AdventureMode.Storm || Mode == AdventureMode.Bungee || Mode == AdventureMode.Boss;
            }
        }

        /// <summary>Gets or sets chooses seeds.</summary>
        public bool ChoosesSeeds
        {
            get
            {
                return (IsReplay || Number >= 8) && Mode == AdventureMode.Lawn;
            }
        }

        /// <summary>Gets or sets packet slots.</summary>
        public int PacketSlots
        {
            get
            {
                return !IsReplay && Number < 5 ? Number : _purchasedSlots;
            }
        }

        /// <summary>Gets or sets reward kind.</summary>
        public AdventureRewardKind RewardKind { get; private set; }
        /// <summary>Gets or sets reward seed.</summary>
        public PlantKind? RewardSeed { get; private set; }
        /// <summary>Gets or sets unlocked seeds.</summary>
        public IReadOnlyList<PlantKind> UnlockedSeeds { get; private set; }
        /// <summary>Gets or sets default seeds.</summary>
        public IReadOnlyList<PlantKind> DefaultSeeds { get; private set; }
        /// <summary>Gets or sets conveyor seeds.</summary>
        public IReadOnlyList<SeedWeight> ConveyorSeeds { get; private set; }

        /// <summary>Initializes adventure level spec.</summary>
        /// <param name = "number">The number.</param>
        /// <param name = "terrain">The terrain.</param>
        /// <param name = "mode">The mode.</param>
        /// <param name = "rewardSeed">The reward seed.</param>
        /// <param name = "rewardKind">The reward kind.</param>
        /// <param name = "suggested">The suggested.</param>
        /// <param name = "conveyor">The conveyor.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        internal AdventureLevelSpec(int number, AdventureTerrain terrain, AdventureMode mode, int rewardSeed, AdventureRewardKind rewardKind, int[] suggested, int[] conveyor)
        {
            Number = number;
            Terrain = terrain;
            Mode = mode;
            RewardKind = rewardKind;
            RewardSeed = rewardKind == AdventureRewardKind.Seed ? (PlantKind? )rewardSeed : null;
            AdventureLevelDefinition raw;
            if (!VerifiedDataCatalog.TryGetAdventureLevel(number, out raw))
            {
                throw new ArgumentOutOfRangeException("number");
            }

            BaseWaveCount = raw.BaseWaveCount;
            WaveCount = mode == AdventureMode.WhackAZombie ? 8 : BaseWaveCount;
            // LawnApp.GetSeedsAvailable uses the award function for the CURRENT level as an exclusive count.
            // x-4 awards tools and x-9 awards notes; x-5/x-10 award the next actual seed.
            var unlocked = new List<PlantKind>();
            for (var seed = 0; seed < Math.Min(40, AwardSeedId(number)); seed++)
            {
                unlocked.Add((PlantKind)seed);
            }

            UnlockedSeeds = unlocked.AsReadOnly();
            var defaults = new List<PlantKind>();
            foreach (int seed in suggested)
            {
                if (unlocked.Contains((PlantKind)seed))
                {
                    defaults.Add((PlantKind)seed);
                }
            }

            if (ChoosesSeeds)
            {
                foreach (PlantKind seed in unlocked)
                {
                    if (defaults.Count < PacketSlots && !defaults.Contains(seed))
                    {
                        defaults.Add(seed);
                    }
                }
            }

            DefaultSeeds = defaults.AsReadOnly();
            var belt = new List<SeedWeight>();
            for (var i = 0; i < conveyor.Length; i += 2)
            {
                belt.Add(new SeedWeight((PlantKind)conveyor[i], conveyor[i + 1]));
            }

            ConveyorSeeds = belt.AsReadOnly();
        }

        /// <summary>Performs for run.</summary>
        /// <param name = "cycle">The cycle.</param>
        /// <param name = "slots">The slots.</param>
        /// <returns>The resulting for run.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        internal AdventureLevelSpec ForRun(int cycle, int slots)
        {
            if (cycle < 1 || slots < 6 || slots > 10)
            {
                throw new ArgumentOutOfRangeException();
            }

            var spec = (AdventureLevelSpec)MemberwiseClone();
            spec.Cycle = cycle;
            spec._purchasedSlots = slots;
            if (cycle > 1)
            {
                var seeds = new List<PlantKind>();
                for (var i = 0; i < 40; i++)
                {
                    seeds.Add((PlantKind)i);
                }

                spec.UnlockedSeeds = seeds.AsReadOnly();
            }

            var defaults = new List<PlantKind>(DefaultSeeds);
            if (spec.ChoosesSeeds)
            {
                foreach (var kind in spec.UnlockedSeeds)
                {
                    if (defaults.Count < spec.PacketSlots && !defaults.Contains(kind))
                    {
                        defaults.Add(kind);
                    }
                }
            }

            spec.DefaultSeeds = defaults.AsReadOnly();
            return spec;
        }

        /// <summary>Performs award seed id.</summary>
        /// <param name = "level">The level.</param>
        /// <returns>The resulting award seed id.</returns>
        internal static int AwardSeedId(int level)
        {
            var slot = (level - 1) % 10 + 1;
            return ((level - 1) / 10) * 8 + slot - (slot == 10 ? 2 : slot >= 5 ? 1 : 0);
        }

        /// <summary>Performs is lawn row.</summary>
        /// <param name = "row">The row.</param>
        /// <returns>The resulting is lawn row.</returns>
        public bool IsLawnRow(int row)
        {
            return row >= 0 && row < Rows && (IsReplay ? true : Number == 1 ? row == 2 : Number <= 3 ? row >= 1 && row <= 3 : true);
        }

        /// <summary>Performs is water row.</summary>
        /// <param name = "row">The row.</param>
        /// <returns>The resulting is water row.</returns>
        public bool IsWaterRow(int row)
        {
            return HasPool && (row == 2 || row == 3);
        }
    }
}
