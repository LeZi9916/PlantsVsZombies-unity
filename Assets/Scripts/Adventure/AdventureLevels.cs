using System;
using System.Collections.Generic;
using PvZReplica.VerifiedData;

namespace PvZReplica.Adventure
{
    public enum AdventureTerrain { Day, Night, Pool, Fog, Roof, BossRoof }
    public enum AdventureMode { Lawn, Bowling, WhackAZombie, LittleTrouble, Vasebreaker, Conveyor, Storm, Bungee, Boss }
    public enum AdventureRewardKind { Seed, Shovel, Almanac, CarKeys, Taco, WateringCan, SilverSunflower, Note }
    public enum AdventureTutorial { ChooseSeed, PlantPeashooter, CollectSun, PlantSunflower, RechargeSunflower,
        Shovel, NightMushrooms, PoolSupport, FogVisibility, RoofPots, Conveyor, Bowling, Hammer, Vases, Boss, Complete }
    public enum AdventurePhase { ChoosingSeeds, Tutorial, Playing, Reward, Finished, Lost }

    /// <summary>First adventure only. Tables come from the local EXE; mode rules are separately documented approximations.</summary>
    public sealed class AdventureLevelSpec
    {
        public int Number { get; private set; }
        public int Cycle { get; private set; } = 1;
        public bool IsReplay { get { return Cycle>1; } }
        private int purchasedSlots = 6;
        public string Label { get { return ((Number - 1) / 10 + 1) + "-" + ((Number - 1) % 10 + 1); } }
        public AdventureTerrain Terrain { get; private set; }
        public AdventureMode Mode { get; private set; }
        public int BaseWaveCount { get; private set; }
        public int WaveCount { get; private set; }
        public int Rows { get { return HasPool ? 6 : 5; } }
        public int StartingSun { get { return Mode == AdventureMode.WhackAZombie || Mode == AdventureMode.Vasebreaker || HasConveyor ? 0 : Number == 1 && !IsReplay ? 150 : 50; } }
        public bool IsNight { get { return Terrain == AdventureTerrain.Night || Terrain == AdventureTerrain.Fog || Terrain == AdventureTerrain.BossRoof; } }
        public bool HasPool { get { return Terrain == AdventureTerrain.Pool || Terrain == AdventureTerrain.Fog; } }
        public bool HasRoof { get { return Terrain == AdventureTerrain.Roof || Terrain == AdventureTerrain.BossRoof; } }
        public bool HasConveyor { get { return Mode == AdventureMode.Conveyor || Mode == AdventureMode.Bowling || Mode == AdventureMode.LittleTrouble || Mode == AdventureMode.Storm || Mode == AdventureMode.Bungee || Mode == AdventureMode.Boss; } }
        public bool ChoosesSeeds { get { return (IsReplay || Number >= 8) && Mode == AdventureMode.Lawn; } }
        public int PacketSlots { get { return !IsReplay && Number < 5 ? Number : purchasedSlots; } }
        public AdventureRewardKind RewardKind { get; private set; }
        public PlantKind? RewardSeed { get; private set; }
        public IReadOnlyList<PlantKind> UnlockedSeeds { get; private set; }
        public IReadOnlyList<PlantKind> DefaultSeeds { get; private set; }
        public IReadOnlyList<SeedWeight> ConveyorSeeds { get; private set; }

        internal AdventureLevelSpec(int number, AdventureTerrain terrain, AdventureMode mode, int rewardSeed,
            AdventureRewardKind rewardKind, int[] suggested, int[] conveyor)
        {
            Number = number; Terrain = terrain; Mode = mode; RewardKind = rewardKind;
            RewardSeed = rewardKind == AdventureRewardKind.Seed ? (PlantKind?)rewardSeed : null;
            AdventureLevelDefinition raw;
            if (!VerifiedDataCatalog.TryGetAdventureLevel(number, out raw)) throw new ArgumentOutOfRangeException("number");
            BaseWaveCount = raw.BaseWaveCount;
            WaveCount = mode == AdventureMode.WhackAZombie ? 8 : BaseWaveCount;
            // LawnApp.GetSeedsAvailable uses the award function for the CURRENT level as an exclusive count.
            // x-4 awards tools and x-9 awards notes; x-5/x-10 award the next actual seed.
            var unlocked = new List<PlantKind>();
            for (int seed = 0; seed < Math.Min(40, AwardSeedId(number)); seed++) unlocked.Add((PlantKind)seed);
            UnlockedSeeds = unlocked.AsReadOnly();
            var defaults = new List<PlantKind>();
            foreach (int seed in suggested)
                if (unlocked.Contains((PlantKind)seed)) defaults.Add((PlantKind)seed);
            if (ChoosesSeeds) foreach (PlantKind seed in unlocked)
                if (defaults.Count < PacketSlots && !defaults.Contains(seed)) defaults.Add(seed);
            DefaultSeeds = defaults.AsReadOnly();
            var belt = new List<SeedWeight>();
            for (int i = 0; i < conveyor.Length; i += 2) belt.Add(new SeedWeight((PlantKind)conveyor[i], conveyor[i + 1]));
            ConveyorSeeds = belt.AsReadOnly();
        }
        internal AdventureLevelSpec ForRun(int cycle,int slots)
        {
            if(cycle<1||slots<6||slots>10)throw new ArgumentOutOfRangeException();
            var spec=(AdventureLevelSpec)MemberwiseClone();spec.Cycle=cycle;spec.purchasedSlots=slots;
            if(cycle>1){var seeds=new List<PlantKind>();for(int i=0;i<40;i++)seeds.Add((PlantKind)i);spec.UnlockedSeeds=seeds.AsReadOnly();}
            var defaults=new List<PlantKind>(DefaultSeeds);
            if(spec.ChoosesSeeds)foreach(var kind in spec.UnlockedSeeds)if(defaults.Count<spec.PacketSlots&&!defaults.Contains(kind))defaults.Add(kind);
            spec.DefaultSeeds=defaults.AsReadOnly();return spec;
        }
        internal static int AwardSeedId(int level)
        {
            int slot = (level - 1) % 10 + 1;
            return ((level - 1) / 10) * 8 + slot - (slot == 10 ? 2 : slot >= 5 ? 1 : 0);
        }
        public bool IsLawnRow(int row) { return row >= 0 && row < Rows && (IsReplay ? true : Number == 1 ? row == 2 : Number <= 3 ? row >= 1 && row <= 3 : true); }
        public bool IsWaterRow(int row) { return HasPool && (row == 2 || row == 3); }
    }
    public struct SeedWeight
    {
        public readonly PlantKind Kind;
        public readonly int Weight;
        public SeedWeight(PlantKind kind, int weight) { Kind = kind; Weight = weight; }
    }

    public static class AdventureLevels
    {
        private static readonly int[] NoBelt = new int[0];
        private static readonly AdventureLevelSpec[] levels = Create();
        public static IReadOnlyList<AdventureLevelSpec> All { get { return Array.AsReadOnly(levels); } }
        public static AdventureLevelSpec Get(int level) { if (level < 1 || level > 50) throw new ArgumentOutOfRangeException("level"); return levels[level - 1]; }
        public static AdventureLevelSpec ForRun(int level,int cycle,int slots=6) { return Get(level).ForRun(cycle,slots); }
        private static AdventureLevelSpec L(int n, AdventureTerrain t, AdventureMode m, int[] seeds, int[] belt = null)
        {
            AdventureRewardKind reward = n == 4 ? AdventureRewardKind.Shovel : n == 14 ? AdventureRewardKind.Almanac :
                n == 24 ? AdventureRewardKind.CarKeys : n == 34 ? AdventureRewardKind.Taco :
                n == 44 ? AdventureRewardKind.WateringCan : n == 50 ? AdventureRewardKind.SilverSunflower : n % 10 == 9 ? AdventureRewardKind.Note : AdventureRewardKind.Seed;
            return new AdventureLevelSpec(n, t, m, AdventureLevelSpec.AwardSeedId(n), reward, seeds, belt ?? NoBelt);
        }
        private static AdventureLevelSpec[] Create()
        {
            // Each explicit row chooses its actual ruleset and useful first-play loadout; wave counts are read from verified EXE data.
            const AdventureTerrain D = AdventureTerrain.Day, N = AdventureTerrain.Night, P = AdventureTerrain.Pool,
                F = AdventureTerrain.Fog, R = AdventureTerrain.Roof, B = AdventureTerrain.BossRoof;
            const AdventureMode Lw = AdventureMode.Lawn, C = AdventureMode.Conveyor;
            return new[] {
                L(1,D,Lw,new[]{0}), L(2,D,Lw,new[]{0,1}), L(3,D,Lw,new[]{0,1,2}), L(4,D,Lw,new[]{0,1,2,3}),
                L(5,D,AdventureMode.Bowling,new int[0],new[]{3,85,49,15}),
                L(6,D,Lw,new[]{0,1,2,3,4}), L(7,D,Lw,new[]{0,1,2,3,4,5}), L(8,D,Lw,new[]{1,0,5,6,3,4}),
                L(9,D,Lw,new[]{1,7,5,2,3,4}),
                L(10,D,C,new int[0],new[]{0,20,2,20,3,15,7,20,5,10,6,5,4,10}),
                L(11,N,Lw,new[]{8,1,0,7,3,2}), L(12,N,Lw,new[]{8,9,7,3,4,2}),
                L(13,N,Lw,new[]{8,9,10,7,3,2}), L(14,N,Lw,new[]{8,9,10,11,3,2}),
                L(15,N,AdventureMode.WhackAZombie,new int[0]),
                L(16,N,Lw,new[]{8,9,10,12,11,3}), L(17,N,Lw,new[]{8,9,10,13,11,3}),
                L(18,N,Lw,new[]{8,9,10,14,11,3}), L(19,N,Lw,new[]{8,9,10,15,11,3}),
                L(20,N,C,new int[0],new[]{11,20,14,15,15,15,12,10,13,15,10,15,8,10}),
                L(21,P,Lw,new[]{1,16,7,5,3,2}), L(22,P,Lw,new[]{1,16,7,17,3,2}),
                L(23,P,Lw,new[]{1,16,18,5,3,2}), L(24,P,Lw,new[]{1,16,18,19,3,2}),
                L(25,P,AdventureMode.LittleTrouble,new int[0],new[]{16,25,3,15,0,25,2,35}),
                L(26,P,Lw,new[]{1,16,18,20,3,19}), L(27,P,Lw,new[]{1,16,18,21,20,19}),
                L(28,P,Lw,new[]{1,16,7,22,20,19}), L(29,P,Lw,new[]{1,16,18,23,20,19}),
                L(30,P,C,new int[0],new[]{16,25,17,5,18,25,19,5,20,10,21,10,22,10,23,10}),
                L(31,F,Lw,new[]{9,16,24,10,18,19}), L(32,F,Lw,new[]{9,16,24,25,18,19}),
                L(33,F,Lw,new[]{9,16,26,25,10,19}), L(34,F,Lw,new[]{9,16,26,27,10,19}),
                L(35,N,AdventureMode.Vasebreaker,new int[0]),
                L(36,F,Lw,new[]{9,16,26,28,10,19}), L(37,F,Lw,new[]{9,16,26,29,10,19}),
                L(38,F,Lw,new[]{9,16,26,29,30,19}), L(39,F,Lw,new[]{9,16,26,31,30,19}),
                L(40,F,AdventureMode.Storm,new int[0],new[]{16,25,24,10,31,5,27,5,26,15,29,25,28,5,30,10}),
                L(41,R,Lw,new[]{1,32,2,3,20,4}), L(42,R,Lw,new[]{1,33,32,2,3,20}),
                L(43,R,Lw,new[]{1,33,34,32,2,3}), L(44,R,Lw,new[]{1,33,34,35,31,2}),
                L(45,R,AdventureMode.Bungee,new int[0],new[]{33,50,6,25,30,15,2,10}),
                L(46,R,Lw,new[]{1,33,32,36,2,20}), L(47,R,Lw,new[]{1,33,34,37,2,20}),
                L(48,R,Lw,new[]{1,33,32,37,38,20}), L(49,R,Lw,new[]{1,33,39,37,2,20}),
                L(50,B,AdventureMode.Boss,new int[0],new[]{33,55,39,10,20,12,32,10,34,5,14,8})
            };
        }
    }
}
