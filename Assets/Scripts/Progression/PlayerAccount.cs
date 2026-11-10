using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica.Progression
{
    public enum GardenKind { Greenhouse, Mushroom, Aquarium }
    public enum GardenNeed { Water, Fertilizer, BugSpray, Music, Happy, Resting }
    public sealed class GardenPlant
    {
        public Guid Id { get; internal set; }
        public int OriginalIndex { get; internal set; } = -1;
        public PlantKind Kind { get; internal set; }
        public GardenKind Garden { get; internal set; }
        public int Slot { get; internal set; }
        public int Growth { get; internal set; }
        public int Waterings { get; internal set; }
        public int FeedingsPerGrow { get; internal set; } = 3;
        public GardenNeed MatureNeed { get; internal set; } = GardenNeed.BugSpray;
        public long NextCareUtc { get; internal set; }
        public long HappyUntilUtc { get; internal set; }
        public long LastWaterUtc { get; internal set; }
        public long LastNeedFulfilledUtc { get; internal set; }
        public long LastFertilizedUtc { get; internal set; }
        public long ChocolateUntilUtc { get; internal set; }
        public long LastCoinUtc { get; internal set; }
        public int PendingCoins { get; internal set; }
        public int CareCycle { get; internal set; }
        internal GardenPlant Copy() { return (GardenPlant)MemberwiseClone(); }
        public bool InPreferredGarden { get { return IsAquatic(Kind) ? Garden==GardenKind.Aquarium : IsMushroom(Kind) ? Garden==GardenKind.Mushroom : Garden==GardenKind.Greenhouse; } }
        public GardenNeed Need(long utc)
        {
            if(LastNeedFulfilledUtc>0 && (utc-LastNeedFulfilledUtc<3600 || DateTimeOffset.FromUnixTimeSeconds(utc).LocalDateTime.Date==DateTimeOffset.FromUnixTimeSeconds(LastNeedFulfilledUtc).LocalDateTime.Date))return GardenNeed.Happy;
            if(utc<NextCareUtc)return GardenNeed.Resting;
            if(Growth>0&&!InPreferredGarden)return GardenNeed.Resting;
            if(!IsAquatic(Kind)||Growth==0)if(Waterings<FeedingsPerGrow)return GardenNeed.Water;
            if(Growth<3)return GardenNeed.Fertilizer;
            return MatureNeed;
        }
        public static bool IsAquatic(PlantKind k) { return k==PlantKind.LilyPad || k==PlantKind.TangleKelp || k==PlantKind.SeaShroom || k==PlantKind.Cattail; }
        public static bool IsMushroom(PlantKind k) { return (int)k>=8&&(int)k<=15 || k==PlantKind.SeaShroom || k==PlantKind.MagnetShroom || k==PlantKind.GloomShroom; }
    }
    public sealed class PlayerAccount
    {
        public Guid ProfileId { get; internal set; }
        public int Coins { get; internal set; }
        public long DisplayCoins { get { return (long)Coins*10; } }
        public int TreeHeight { get; internal set; }
        public long SnailAwakeUntilUtc { get; internal set; }
        public bool GardenGiftClaimed { get; internal set; }
        public bool MinigamesUnlocked { get; internal set; }
        public bool PuzzleUnlocked { get; internal set; }
        public bool SurvivalUnlocked { get; internal set; }
        internal int RewardCycle;
        internal ulong RewardMask;
        internal int[] purchases=new int[80];
        internal int[] modeRecords=new int[100];
        internal List<GardenPlant> garden=new List<GardenPlant>();
        internal Dictionary<string,int> receipts=new Dictionary<string,int>();
        public IReadOnlyList<GardenPlant> Plants { get { return garden.AsReadOnly(); } }
        public int PurchaseCount(int originalStoreId) { return originalStoreId>=0&&originalStoreId<purchases.Length?purchases[originalStoreId]:0; }
        public int ModeRecord(int originalModeId) { return originalModeId>=0&&originalModeId<modeRecords.Length?modeRecords[originalModeId]:0; }
        public int PacketSlots { get { return 6+Math.Min(4,PurchaseCount(21)); } }
        public bool Owns(PlantKind kind) { return (int)kind>=40&&(int)kind<=48&&purchases[(int)kind-40]>0; }
        public AdventureSeedAccess SeedAccess(PlantKind? imitater=null)
        {
            var seeds=new List<PlantKind>();for(int i=0;i<9;i++)if(purchases[i]>0)seeds.Add((PlantKind)(40+i));
            return new AdventureSeedAccess(seeds,Owns(PlantKind.Imitater)?imitater:null);
        }
        internal PlayerAccount Copy()
        {
            var p=(PlayerAccount)MemberwiseClone();p.purchases=(int[])purchases.Clone();p.modeRecords=(int[])modeRecords.Clone();
            p.garden=new List<GardenPlant>();foreach(var plant in garden)p.garden.Add(plant.Copy());
            p.receipts=new Dictionary<string,int>(receipts);return p;
        }
    }
}
