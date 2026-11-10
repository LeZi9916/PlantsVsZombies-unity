using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PvZReplica.Progression
{
    // All monetary changes publish their snapshot only after an atomic, checksummed save.
    public sealed partial class PlayerAccountStore
    {
        private static readonly byte[] Magic=Encoding.ASCII.GetBytes("PVZRACC1");
        private readonly string path;
        private readonly object gate=new object();
        private byte[] savedBytes;
        private Dictionary<Guid,PlayerAccount> records;
        public PlayerAccountStore(string filePath)
        {
            path=Path.GetFullPath(filePath);
            if(!string.Equals(Path.GetExtension(path),".account",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Use a dedicated .account file.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));savedBytes=ReadFile(path);
            records=savedBytes==null?new Dictionary<Guid,PlayerAccount>():Decode(savedBytes);
        }
        public PlayerAccount Get(Guid id)
        {
            if(id==Guid.Empty)throw new ArgumentException("A profile ID is required.");
            lock(gate){PlayerAccount p;return records.TryGetValue(id,out p)?p.Copy():new PlayerAccount{ProfileId=id};}
        }
        private void Change(Guid id,Action<PlayerAccount> change)
        {
            lock(gate){var p=Get(id);change(p);Commit(p);}
        }
        public void CreditBattleCoins(Guid id,string attempt,int cumulativeUnits)
        {
            if(string.IsNullOrEmpty(attempt)||attempt.Length>80||cumulativeUnits<0)throw new ArgumentException("Invalid battle receipt.");
            lock(gate)
            {
                var p=Get(id);int old;p.receipts.TryGetValue("battle:"+attempt,out old);
                if(cumulativeUnits<=old)return;
                AddCoins(p,cumulativeUnits-old);p.receipts["battle:"+attempt]=cumulativeUnits;TrimReceipts(p);Commit(p);
            }
        }
        public void ClaimAdventureReward(Guid id,int cycle,int level,int unusedMowers=0)
        {
            if(cycle<1||level<1||level>50)throw new ArgumentOutOfRangeException();
            lock(gate)
            {
                var p=Get(id);if(cycle<p.RewardCycle)return;
                if(cycle>p.RewardCycle){p.RewardCycle=cycle;p.RewardMask=0;}
                ulong bit=1UL<<(level-1);if((p.RewardMask&bit)!=0)return;
                p.RewardMask|=bit;
                if(level>=11||cycle>1)AddCoins(p,Math.Max(0,Math.Min(6,unusedMowers))*5);
                // First-run plants/tools are earned by campaign progress; later runs award money bags.
                if(cycle>1)AddCoins(p,level==50?300:50);
                else if(level==50)AddCoins(p,300);
                if(level==34)AddCoins(p,100);
                if(level>=44&&!p.GardenGiftClaimed)
                {
                    AddGardenPlant(p,PlantKind.Marigold,0);AddGardenPlant(p,PlantKind.Marigold,0);
                    p.GardenGiftClaimed=true;p.purchases[14]=Math.Max(5,p.purchases[14]);
                }
                Commit(p);
            }
        }
        public void RecordModeResult(Guid id,int mode,int score,string runId,int rewardUnits)
        {
            if(mode<0||mode>=100||score<0||rewardUnits<0||rewardUnits>1000||string.IsNullOrEmpty(runId)||runId.Length>80)throw new ArgumentException("Invalid challenge result.");
            lock(gate)
            {
                var p=Get(id);string key="mode:"+runId;if(p.receipts.ContainsKey(key))return;
                p.modeRecords[mode]=Math.Max(score,p.modeRecords[mode]);AddCoins(p,rewardUnits);
                p.receipts[key]=1;TrimReceipts(p);Commit(p);
            }
        }
        public void Import(Guid id,int coins,int[] purchases,int[] challenges,int treeHeight,IEnumerable<GardenPlant> plants,bool minigamesUnlocked=false,bool puzzleUnlocked=false,bool survivalUnlocked=false)
        {
            if(coins<0||coins>99999||treeHeight<0||treeHeight>10000000||purchases==null||purchases.Length!=80||challenges==null||challenges.Length!=100)throw new InvalidDataException("Invalid original account.");
            Change(id,p=>{p.Coins=coins;p.purchases=(int[])purchases.Clone();p.modeRecords=(int[])challenges.Clone();p.TreeHeight=treeHeight;
                p.garden=new List<GardenPlant>();foreach(var plant in plants)p.garden.Add(plant.Copy());p.GardenGiftClaimed=p.garden.Count>0;p.MinigamesUnlocked=minigamesUnlocked;p.PuzzleUnlocked=puzzleUnlocked;p.SurvivalUnlocked=survivalUnlocked;});
        }
        private static void AddCoins(PlayerAccount p,int amount) { p.Coins=(int)Math.Min(99999L,(long)p.Coins+amount); }
        private static void TrimReceipts(PlayerAccount p)
        {
            while(p.receipts.Count>256){string oldest=null;foreach(var key in p.receipts.Keys){oldest=key;break;}p.receipts.Remove(oldest);}
        }
        private void Commit(PlayerAccount account)
        {
            var next=new Dictionary<Guid,PlayerAccount>(records);next[account.ProfileId]=account;
            if(next.Count>1024)throw new InvalidOperationException("Too many player accounts.");
            byte[] data=Encode(next);string temp=path+".tmp-"+Guid.NewGuid().ToString("N");
            using(var lease=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            {
                if(!Same(savedBytes,ReadFile(path)))throw new IOException("存档已被另一个游戏进程修改，请重新启动游戏后重试。");
                try
                {
                    using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(data,0,data.Length);file.Flush(true);}
                    if(savedBytes==null)File.Move(temp,path);else File.Replace(temp,path,path+".backup",true);
                    records=next;savedBytes=data;
                }
                finally{if(File.Exists(temp))File.Delete(temp);}
            }
        }
        private static byte[] ReadFile(string filename)
        {
            try{if(new FileInfo(filename).Length>8*1024*1024)throw new InvalidDataException("Account file is too large.");return File.ReadAllBytes(filename);}
            catch(FileNotFoundException){return null;}
        }
        private static bool Same(byte[] a,byte[] b)
        {if(a==null||b==null)return a==b;if(a.Length!=b.Length)return false;int diff=0;for(int i=0;i<a.Length;i++)diff|=a[i]^b[i];return diff==0;}
        private static byte[] Encode(Dictionary<Guid,PlayerAccount> values)
        {
            byte[] body;
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream,Encoding.UTF8))
            {
                w.Write(Magic);w.Write(1);w.Write(values.Count);
                var ids=new List<Guid>(values.Keys);ids.Sort();
                foreach(var id in ids)
                {
                    var p=values[id];w.Write(id.ToByteArray());w.Write(p.Coins);w.Write(p.TreeHeight);w.Write(p.SnailAwakeUntilUtc);w.Write(p.GardenGiftClaimed);w.Write(p.MinigamesUnlocked);w.Write(p.PuzzleUnlocked);w.Write(p.SurvivalUnlocked);w.Write(p.RewardCycle);w.Write(p.RewardMask);
                    foreach(int n in p.purchases)w.Write(n);foreach(int n in p.modeRecords)w.Write(n);
                    w.Write(p.garden.Count);
                    foreach(var g in p.garden){w.Write(g.Id.ToByteArray());w.Write(g.OriginalIndex);w.Write((int)g.Kind);w.Write((int)g.Garden);w.Write(g.Slot);w.Write(g.Growth);w.Write(g.Waterings);w.Write(g.FeedingsPerGrow);w.Write((int)g.MatureNeed);w.Write(g.NextCareUtc);w.Write(g.HappyUntilUtc);w.Write(g.LastWaterUtc);w.Write(g.LastNeedFulfilledUtc);w.Write(g.LastFertilizedUtc);w.Write(g.ChocolateUntilUtc);w.Write(g.LastCoinUtc);w.Write(g.PendingCoins);w.Write(g.CareCycle);}
                    w.Write(p.receipts.Count);foreach(var r in p.receipts){w.Write(r.Key);w.Write(r.Value);}
                }
                w.Flush();body=stream.ToArray();
            }
            using(var sha=SHA256.Create())using(var stream=new MemoryStream()){stream.Write(body,0,body.Length);var sum=sha.ComputeHash(body);stream.Write(sum,0,sum.Length);return stream.ToArray();}
        }
        private static Dictionary<Guid,PlayerAccount> Decode(byte[] bytes)
        {
            if(bytes.Length<48)throw new InvalidDataException("Truncated account file.");
            var body=new byte[bytes.Length-32];var sum=new byte[32];Array.Copy(bytes,body,body.Length);Array.Copy(bytes,body.Length,sum,0,32);
            using(var sha=SHA256.Create())if(!Same(sum,sha.ComputeHash(body)))throw new InvalidDataException("金币档案校验失败；请保留文件及 .backup 备份。");
            try
            {
                using(var stream=new MemoryStream(body))using(var r=new BinaryReader(stream,Encoding.UTF8))
                {
                    if(!Same(Magic,r.ReadBytes(8))||r.ReadInt32()!=1)throw new InvalidDataException("Unsupported account format.");
                    int count=Bound(r.ReadInt32(),0,1024);var result=new Dictionary<Guid,PlayerAccount>();
                    for(int i=0;i<count;i++)
                    {
                        var p=new PlayerAccount{ProfileId=new Guid(r.ReadBytes(16)),Coins=Bound(r.ReadInt32(),0,99999),TreeHeight=Bound(r.ReadInt32(),0,10000000),SnailAwakeUntilUtc=r.ReadInt64(),GardenGiftClaimed=r.ReadBoolean(),MinigamesUnlocked=r.ReadBoolean(),PuzzleUnlocked=r.ReadBoolean(),SurvivalUnlocked=r.ReadBoolean(),RewardCycle=Bound(r.ReadInt32(),0,1000000),RewardMask=r.ReadUInt64()};
                        if(p.ProfileId==Guid.Empty||result.ContainsKey(p.ProfileId)||p.RewardMask>=1UL<<50)throw new InvalidDataException("Invalid account identity or rewards.");
                        for(int j=0;j<80;j++)p.purchases[j]=Bound(r.ReadInt32(),0,int.MaxValue);
                        for(int j=0;j<100;j++)p.modeRecords[j]=Bound(r.ReadInt32(),0,int.MaxValue);
                        int plants=Bound(r.ReadInt32(),0,200);var plantIds=new HashSet<Guid>();var slots=new HashSet<int>();
                        for(int j=0;j<plants;j++)
                        {
                            var g=new GardenPlant{Id=new Guid(r.ReadBytes(16)),OriginalIndex=Bound(r.ReadInt32(),-1,199),Kind=(PlantKind)Bound(r.ReadInt32(),0,48),Garden=(GardenKind)Bound(r.ReadInt32(),0,2),Slot=Bound(r.ReadInt32(),0,31),Growth=Bound(r.ReadInt32(),0,3),Waterings=Bound(r.ReadInt32(),0,5),FeedingsPerGrow=Bound(r.ReadInt32(),3,5),MatureNeed=(GardenNeed)Bound(r.ReadInt32(),2,3),NextCareUtc=r.ReadInt64(),HappyUntilUtc=r.ReadInt64(),LastWaterUtc=r.ReadInt64(),LastNeedFulfilledUtc=r.ReadInt64(),LastFertilizedUtc=r.ReadInt64(),ChocolateUntilUtc=r.ReadInt64(),LastCoinUtc=r.ReadInt64(),PendingCoins=Bound(r.ReadInt32(),0,99999),CareCycle=Bound(r.ReadInt32(),0,int.MaxValue)};
                            if(g.Id==Guid.Empty||!plantIds.Add(g.Id)||!slots.Add((int)g.Garden*32+g.Slot))throw new InvalidDataException("Invalid garden layout.");p.garden.Add(g);
                        }
                        int receipts=Bound(r.ReadInt32(),0,256);for(int j=0;j<receipts;j++){string key=r.ReadString();if(key.Length>90||p.receipts.ContainsKey(key))throw new InvalidDataException("Invalid receipt.");p.receipts.Add(key,Bound(r.ReadInt32(),0,int.MaxValue));}
                        result.Add(p.ProfileId,p);
                    }
                    if(stream.Position!=stream.Length)throw new InvalidDataException("Trailing account data.");return result;
                }
            }
            catch(EndOfStreamException e){throw new InvalidDataException("Truncated account file.",e);}
        }
        private static int Bound(int n,int min,int max){if(n<min||n>max)throw new InvalidDataException("Account value out of range.");return n;}
    }
}
