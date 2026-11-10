using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using PvZReplica.Progression;

namespace PvZReplica.Adventure
{
    public sealed class AdventureProgress
    {
        public Guid ProfileId { get; internal set; }
        public int NextLevel { get; internal set; } = 1;
        public int Cycle { get; internal set; } = 1;
        public int CompletedRuns { get { return Cycle-1+(Completed?1:0); } }
        public int PacketSlots { get; internal set; } = 6;
        public PlantKind? ImitaterTarget { get; internal set; }
        public ulong CollectedRewards { get; internal set; }
        public bool Completed { get { return CollectedRewards == ((1UL << 50) - 1); } }
        public int AttemptSeed { get; internal set; } = 12345;
        public IReadOnlyList<PlantKind> SelectedSeeds { get; internal set; } = Array.AsReadOnly(new PlantKind[0]);
        internal AdventureProgress Copy() { return new AdventureProgress {ProfileId=ProfileId,NextLevel=NextLevel,Cycle=Cycle,PacketSlots=PacketSlots,ImitaterTarget=ImitaterTarget,CollectedRewards=CollectedRewards,AttemptSeed=AttemptSeed,SelectedSeeds=Array.AsReadOnly(ToArray(SelectedSeeds))}; }
        private static PlantKind[] ToArray(IReadOnlyList<PlantKind> values) {var result=new PlantKind[values.Count];for(int i=0;i<result.Length;i++)result[i]=values[i];return result;}
    }
    /// <summary>Separate replica-only sidecar keyed by the unchanged v1 ProfileCatalog GUID. Does not open original user files.</summary>
    public sealed class AdventureProgressStore
    {
        private static readonly byte[] Magic=Encoding.ASCII.GetBytes("PVZRADV1");
        private readonly string path;
        private byte[] savedBytes;
        private Dictionary<Guid,AdventureProgress> records;
        private readonly object gate=new object();
        public AdventureProgressStore(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath))throw new ArgumentException("A replica progress path is required.");
            path=Path.GetFullPath(filePath);
            if(!string.Equals(Path.GetExtension(path),".adventure",StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Use a dedicated .adventure sidecar, never an original .dat or profile file.");
            if(!Directory.Exists(Path.GetDirectoryName(path)))throw new DirectoryNotFoundException(Path.GetDirectoryName(path));
            savedBytes=ReadOrMissing(path);
            records=savedBytes==null?new Dictionary<Guid,AdventureProgress>():Decode(savedBytes);
        }
        public AdventureProgress Get(Guid profileId)
        {
            if(profileId==Guid.Empty)throw new ArgumentException("A profile GUID is required.");
            lock(gate) {AdventureProgress p;return records.TryGetValue(profileId,out p)?p.Copy():new AdventureProgress{ProfileId=profileId};}
        }
        public void SaveAttempt(Guid profileId,int seed,IReadOnlyList<PlantKind> selected,AdventureSeedAccess access=null,int slots=6,PlantKind? imitater=null)
        {
            lock(gate)
            {
                var p=Get(profileId);p.AttemptSeed=seed;
                if(selected==null)throw new ArgumentNullException("selected");
                var loadout=new List<PlantKind>();
                var level=AdventureLevels.ForRun(p.NextLevel,p.Cycle,slots);
                var available=new List<PlantKind>(level.UnlockedSeeds);if(access!=null)available.AddRange(access.AdditionalSeeds);
                foreach(var kind in selected)
                { if(loadout.Contains(kind) || !Has(available,kind))throw new ArgumentException("Invalid or unearned loadout.");loadout.Add(kind); }
                if(loadout.Count>level.PacketSlots || level.ChoosesSeeds && loadout.Count!=0 && loadout.Count!=Math.Min(level.PacketSlots,available.Count))throw new ArgumentException("A selected loadout must fill the available packet slots.");
                if(imitater.HasValue&&(!Has(available,PlantKind.Imitater)||!Has(level.UnlockedSeeds,imitater.Value)))throw new ArgumentException("Invalid Imitater target.");
                p.SelectedSeeds=loadout.AsReadOnly();p.PacketSlots=slots;p.ImitaterTarget=imitater;Commit(p);
            }
        }
        internal bool RecordReward(Guid profileId,int level)
        {
            lock(gate)
            {
                var p=Get(profileId);
                if(level<1 || level>50)throw new ArgumentOutOfRangeException("level");
                ulong bit=1UL<<(level-1);
                if((p.CollectedRewards&bit)!=0)return false;
                if(level!=p.NextLevel)throw new InvalidOperationException("Rewards must be collected in adventure order.");
                p.CollectedRewards|=bit;p.NextLevel=Math.Min(50,level+1);p.SelectedSeeds=Array.AsReadOnly(new PlantKind[0]);
                Commit(p);return true;
            }
        }
        public void StartNextCycle(Guid profileId)
        {
            lock(gate){var p=Get(profileId);if(!p.Completed)throw new InvalidOperationException("Complete the current adventure first.");
                p.Cycle++;p.NextLevel=1;p.CollectedRewards=0;p.SelectedSeeds=Array.AsReadOnly(new PlantKind[0]);p.ImitaterTarget=null;Commit(p);}
        }
        public void Import(Guid profileId,int level,int completedRuns)
        {
            if(level<1||level>50||completedRuns<0||completedRuns>999999)throw new ArgumentException("Invalid original adventure checkpoint.");
            lock(gate){var p=Get(profileId);p.NextLevel=level;p.Cycle=completedRuns+1;p.CollectedRewards=(1UL<<(level-1))-1;p.SelectedSeeds=Array.AsReadOnly(new PlantKind[0]);p.ImitaterTarget=null;Commit(p);}
        }
        private void Commit(AdventureProgress changed)
        {
            var next=new Dictionary<Guid,AdventureProgress>(records);next[changed.ProfileId]=changed;
            if(next.Count>1024)throw new InvalidOperationException("The adventure profile limit has been reached.");
            byte[] bytes=Encode(next);string temp=null;
            using(var lease=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                if(!Same(savedBytes,ReadOrMissing(path)))throw new IOException("Adventure progress changed in another session; reopen before saving.");
                try
                {
                    temp=path+".tmp-"+Guid.NewGuid().ToString("N");
                    using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {file.Write(bytes,0,bytes.Length);file.Flush(true);}
                    if(savedBytes==null)File.Move(temp,path);
                    else File.Replace(temp,path,path+".backup-"+Guid.NewGuid().ToString("N"));
                    temp=null;records=next;savedBytes=bytes;
                }
                finally {if(temp!=null && File.Exists(temp))File.Delete(temp);}
            }
        }
        private static bool Has(IReadOnlyList<PlantKind> values,PlantKind kind) {foreach(var v in values)if(v==kind)return true;return false;}
        private static bool Same(byte[] a,byte[] b) {if(a==null || b==null)return a==b;if(a.Length!=b.Length)return false;int diff=0;for(int i=0;i<a.Length;i++)diff|=a[i]^b[i];return diff==0;}
        private static byte[] ReadOrMissing(string p) {try{var info=new FileInfo(p);if(info.Length>1024*1024)throw new InvalidDataException("Oversized progress file.");return File.ReadAllBytes(p);}catch(FileNotFoundException){return null;} }
        private static byte[] Encode(Dictionary<Guid,AdventureProgress> items)
        {
            byte[] body;
            using(var m=new MemoryStream())using(var w=new BinaryWriter(m,Encoding.UTF8))
            {
                w.Write(Magic);w.Write(2);w.Write(items.Count);
                var ids=new List<Guid>(items.Keys);ids.Sort();
                foreach(var id in ids)
                {
                    var p=items[id];w.Write(id.ToByteArray());w.Write(p.NextLevel);w.Write(p.CollectedRewards);w.Write(p.AttemptSeed);w.Write(p.SelectedSeeds.Count);
                    foreach(var seed in p.SelectedSeeds)w.Write((int)seed);w.Write(p.Cycle);w.Write(p.PacketSlots);w.Write(p.ImitaterTarget.HasValue?(int)p.ImitaterTarget.Value:-1);
                }
                w.Flush();body=m.ToArray();
            }
            using(var sha=SHA256.Create())using(var m=new MemoryStream()) {m.Write(body,0,body.Length);byte[] sum=sha.ComputeHash(body);m.Write(sum,0,sum.Length);return m.ToArray();}
        }
        private static Dictionary<Guid,AdventureProgress> Decode(byte[] bytes)
        {
            if(bytes.Length<48 || bytes.Length>1024*1024)throw new InvalidDataException("Invalid replica adventure file.");
            byte[] body=new byte[bytes.Length-32],checksum=new byte[32];Array.Copy(bytes,body,body.Length);Array.Copy(bytes,body.Length,checksum,0,32);
            using(var sha=SHA256.Create())if(!Same(checksum,sha.ComputeHash(body)))throw new InvalidDataException("Adventure checksum mismatch; existing backups are preserved.");
            try
            {
                using(var m=new MemoryStream(body))using(var r=new BinaryReader(m,Encoding.UTF8))
                {
                    if(!Same(Magic,r.ReadBytes(Magic.Length)))throw new InvalidDataException("Unsupported adventure format.");
                    int version=r.ReadInt32();if(version!=1&&version!=2)throw new InvalidDataException("Unsupported adventure version.");
                    int count=r.ReadInt32();if(count<0 || count>1024)throw new InvalidDataException("Invalid profile count.");
                    var result=new Dictionary<Guid,AdventureProgress>();
                    for(int i=0;i<count;i++)
                    {
                        Guid id=new Guid(r.ReadBytes(16));int level=r.ReadInt32();ulong mask=r.ReadUInt64();int seed=r.ReadInt32(),cards=r.ReadInt32();
                        ulong valid=(1UL<<50)-1;
                        if(id==Guid.Empty || result.ContainsKey(id) || level<1 || level>50 || cards<0 || cards>(version==1?6:10) || mask>valid || mask==valid && level!=50 ||
                            mask!=(mask==valid?valid:(1UL<<(level-1))-1))throw new InvalidDataException("Inconsistent adventure checkpoint.");
                        var seeds=new List<PlantKind>();var spec=AdventureLevels.Get(level);
                        if(version==1 && spec.ChoosesSeeds && cards!=0 && cards!=Math.Min(spec.PacketSlots,spec.UnlockedSeeds.Count))throw new InvalidDataException("Incomplete saved loadout.");
                        for(int j=0;j<cards;j++) {var kind=(PlantKind)r.ReadInt32();if(seeds.Contains(kind) || (version==1?!Has(spec.UnlockedSeeds,kind):(int)kind<0||(int)kind>48))throw new InvalidDataException("Invalid saved seed.");seeds.Add(kind);}
                        int cycle=version==2?r.ReadInt32():1,slots=version==2?r.ReadInt32():6,target=version==2?r.ReadInt32():-1;
                        if(cycle<1||cycle>1000000||slots<6||slots>10||target< -1||target>39)throw new InvalidDataException("Invalid replay checkpoint.");
                        result.Add(id,new AdventureProgress{ProfileId=id,NextLevel=level,Cycle=cycle,PacketSlots=slots,ImitaterTarget=target<0?(PlantKind?)null:(PlantKind)target,CollectedRewards=mask,AttemptSeed=seed,SelectedSeeds=seeds.AsReadOnly()});
                    }
                    if(m.Position!=m.Length)throw new InvalidDataException("Trailing adventure data.");return result;
                }
            }
            catch(EndOfStreamException e){throw new InvalidDataException("Truncated adventure checkpoint.",e);}
        }
    }
    /// <summary>Owns a current attempt. Persistent reward commit precedes advancing the board or unlocking the next level.</summary>
    public sealed class AdventureCampaign
    {
        private readonly AdventureProgressStore store;
        private readonly PlayerAccountStore accounts;
        private AdventureSeedAccess seedAccess;
        private int packetSlots=6;
        private string attemptId;
        private long coinSaveRetryUtc;
        public string LastPersistenceError { get; private set; }
        private bool roofCleaners;
        private bool poolCleaners;
        public Guid ProfileId { get; private set; }
        public GameSimulation Game { get; private set; }
        public AdventureProgress Progress { get {return store.Get(ProfileId);} }
        public AdventureCampaign(AdventureProgressStore store,Guid profileId,PlayerAccountStore accounts=null)
        {if(store==null)throw new ArgumentNullException("store");this.store=store;this.accounts=accounts;ProfileId=profileId;store.Get(profileId); }
        public GameSimulation BeginOrResume(bool roofCleaners=false,bool poolCleaners=false)
        {
            var p=Progress;
            this.roofCleaners=roofCleaners;this.poolCleaners=poolCleaners;
            var account=accounts==null?null:accounts.Get(ProfileId);seedAccess=account==null?null:account.SeedAccess(p.ImitaterTarget);
            packetSlots=account==null?6:account.PacketSlots;attemptId=Guid.NewGuid().ToString("N");LastPersistenceError=null;
            Game=new GameSimulation(new GameRules{AdventureLevel=p.NextLevel,AdventureCycle=p.Cycle,AdventureDaveSeed=unchecked(p.AttemptSeed+p.NextLevel+(p.Cycle-1)*101+ProfileId.GetHashCode()),AdventurePacketSlots=packetSlots,AdventureSeedAccess=seedAccess,
                AdventureRoofCleaners=roofCleaners || account!=null&&account.PurchaseCount(23)>0,AdventurePoolCleaners=poolCleaners || account!=null&&account.PurchaseCount(22)>0,
                AdventureMinigamesUnlocked=account!=null&&account.MinigamesUnlocked,AdventurePuzzleUnlocked=account!=null&&account.PuzzleUnlocked,AdventurePottedLoot=account!=null&&p.CompletedRuns>0&&account.Plants.Count<32,AdventureChocolateLoot=account!=null&&account.PurchaseCount(20)>0&&account.PurchaseCount(26)<10,AdventureLootCommit=accounts==null?(Func<AdventureCoin,bool>)null:CommitLoot,
                AdventureFirstAid=account!=null&&account.PurchaseCount(29)>0,AdventurePlaceRake=account!=null&&account.PurchaseCount(24)>0?(Func<bool>)(()=>{accounts.ConsumeRake(ProfileId);return true;}):null,
                AdventureCoinCommit=accounts==null?(Func<int,bool>)null:CommitCoins},p.AttemptSeed);
            if(Game.Adventure.Phase==AdventurePhase.ChoosingSeeds && p.SelectedSeeds.Count>0)Game.ChooseAdventureSeeds(p.SelectedSeeds);
            return Game;
        }
        public bool ChooseSeeds(IReadOnlyList<PlantKind> seeds)
        {
            if(Game==null || seeds==null)return false;
            // Capture the caller's possibly mutable collection before validation or disk IO.
            var selected=new List<PlantKind>(seeds);
            if(!Game.CanChooseAdventureSeeds(selected))return false;
            store.SaveAttempt(ProfileId,Progress.AttemptSeed,selected,seedAccess,packetSlots,Game.Adventure.ImitaterTarget);
            return Game.ChooseAdventureSeeds(selected);
        }
        public bool CollectReward()
        {
            if(Game==null || Game.Adventure.Phase!=AdventurePhase.Reward || Game.State!=MatchState.Won)return false;
            if(!Game.BankAdventureCoins())return false;
            if(accounts!=null){int unused=0;foreach(var mower in Game.Mowers)if(mower.Available)unused++;accounts.ClaimAdventureReward(ProfileId,Progress.Cycle,Game.Adventure.Level.Number,unused);}
            store.RecordReward(ProfileId,Game.Adventure.Level.Number);
            return Game.CollectAdventureReward();
        }
        private bool CommitLoot(AdventureCoin coin)
        {
            try{if(coin.Kind==AdventureCoinKind.MinigameUnlock||coin.Kind==AdventureCoinKind.PuzzleUnlock)accounts.CollectModeUnlock(ProfileId,attemptId+":"+coin.Id,coin.Kind==AdventureCoinKind.PuzzleUnlock);else accounts.CollectBattleLoot(ProfileId,attemptId+":"+coin.Id,coin.Kind==AdventureCoinKind.Chocolate,coin.GiftPlant??PlantKind.Peashooter,DateTimeOffset.UtcNow.ToUnixTimeSeconds());LastPersistenceError=null;return true;}
            catch(IOException error){LastPersistenceError=error.Message;coinSaveRetryUtc=DateTime.UtcNow.Ticks+TimeSpan.TicksPerSecond;return false;}
            catch(InvalidOperationException error){LastPersistenceError=error.Message;return false;}
        }
        private bool CommitCoins(int total)
        {
            if(LastPersistenceError!=null&&DateTime.UtcNow.Ticks<coinSaveRetryUtc)return false;
            try{accounts.CreditBattleCoins(ProfileId,attemptId,total);LastPersistenceError=null;return true;}
            catch(IOException error){LastPersistenceError=error.Message;return false;}
            catch(UnauthorizedAccessException error){LastPersistenceError=error.Message;coinSaveRetryUtc=DateTime.UtcNow.Ticks+TimeSpan.TicksPerSecond;return false;}
        }
        public bool StartNextLevel()
        {
            if(Game==null || Game.Adventure.Phase!=AdventurePhase.Finished || Progress.Completed)return false;
            BeginOrResume(roofCleaners,poolCleaners);return true;
        }
    }
}
