using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using PvZReplica.Progression;
using PvZReplica.Adventure;

namespace PvZReplica.Profiles
{
    public sealed class OriginalUserSave
    {
        public string Name { get; internal set; }
        public uint OriginalId { get; internal set; }
        public uint UseSequence { get; internal set; }
        public int Level { get; internal set; }
        public int Coins { get; internal set; }
        public int FinishedAdventure { get; internal set; }
        public bool MinigamesUnlocked { get; internal set; }
        public bool PuzzleUnlocked { get; internal set; }
        public bool SurvivalUnlocked { get; internal set; }
        public int[] Purchases { get; internal set; }
        public int[] ChallengeRecords { get; internal set; }
        public IReadOnlyList<GardenPlant> Plants { get; internal set; }
        internal byte[] OriginalBytes;
    }
    // Local PC annual executable: SyncDetails 0x4769D0 (version 12),
    // ProfileMgr sync 0x4799A0 (version 14), 0x476960, 0x44CDE0 and 0x44E4F0.
    public static class OriginalSaveFormat
    {
        private const int PlantOffset=820,PlantSize=88;
        private static int Int(byte[] b,int offset){if(offset<0||offset>b.Length-4)throw new InvalidDataException("原版档案被截断。");return BitConverter.ToInt32(b,offset);}
        private static void Put(byte[] b,int offset,int value){Array.Copy(BitConverter.GetBytes(value),0,b,offset,4);}
        private static void PutLong(byte[] b,int offset,long value){Array.Copy(BitConverter.GetBytes(value),0,b,offset,8);}
        private static byte[] ReadLimited(string filename,int limit)
        {var info=new FileInfo(filename);if(info.Length<4||info.Length>limit)throw new InvalidDataException("原版文件大小异常。");return File.ReadAllBytes(filename);}
        // 0x5EF450 narrows one wchar to one byte; 0x5EF380 widens unsigned bytes. No multibyte decoder is involved.
        private static Encoding NamesEncoding(){return Encoding.GetEncoding(28591);}
        public static IReadOnlyList<OriginalUserSave> ReadDirectory(string directory)
        {
            string users=Path.Combine(Path.GetFullPath(directory),"users.dat");var bytes=ReadLimited(users,256*1024);
            using(var m=new MemoryStream(bytes))using(var r=new BinaryReader(m))
            {
                if(r.ReadInt32()!=14)throw new InvalidDataException("此文件不是已验证的年度版 users.dat v14。");
                int count=r.ReadUInt16();if(count>1024)throw new InvalidDataException("原版用户数异常。");
                var result=new List<OriginalUserSave>();var ids=new HashSet<uint>();var encoding=NamesEncoding();
                for(int i=0;i<count;i++)
                {
                    int length=r.ReadUInt16();if(length==0||length>256||length>m.Length-m.Position-8)throw new InvalidDataException("原版用户名称字段异常。");
                    string name=encoding.GetString(r.ReadBytes(length));uint sequence=r.ReadUInt32(),id=r.ReadUInt32();
                    if(id==0||!ids.Add(id))throw new InvalidDataException("原版用户编号异常。");
                    var p=ReadUser(Path.Combine(directory,"user"+id+".dat"));p.OriginalId=id;p.UseSequence=sequence;p.Name=name;result.Add(p);
                }
                if(m.Position!=m.Length)throw new InvalidDataException("原版用户目录尾部存在未知数据，未自动导入。");return result.AsReadOnly();
            }
        }
        public static OriginalUserSave ReadUser(string filename)
        {
            var bytes=ReadLimited(filename,2*1024*1024);if(bytes.Length<PlantOffset+67||Int(bytes,0)!=12)throw new InvalidDataException("仅支持本地逆向确认的 PC 年度版 user*.dat v12；局内 game*.dat 不是用户档案。");
            int level=Int(bytes,4),coins=Int(bytes,8),finished=Int(bytes,12),count=Int(bytes,816);
            if(level<1||level>50||coins<0||coins>99999||finished<0||finished>999999||count<0||count>200||PlantOffset+(long)count*PlantSize+67>bytes.Length)throw new InvalidDataException("原版用户数据超出有效范围。");
            int footer=PlantOffset+count*PlantSize,avatars=Int(bytes,footer+41);
            if(avatars<0||avatars>100)throw new InvalidDataException("原版大头贴数量异常。");
            long expected=footer+67L+avatars*72L;if(bytes.Length!=expected&&bytes.Length!=expected+5)throw new InvalidDataException("原版档案尾部结构不匹配，未导入未知布局。");
            for(int i=0;i<41;i++)if(bytes[footer+i]>1)throw new InvalidDataException("原版成就标志异常。");
            var purchases=new int[80];var records=new int[100];
            for(int i=0;i<100;i++){records[i]=Int(bytes,16+4*i);if(records[i]<0)throw new InvalidDataException("原版挑战记录异常。");}
            for(int i=0;i<80;i++){purchases[i]=Int(bytes,416+4*i);if(purchases[i]<0)throw new InvalidDataException("原版购买记录异常。");}
            foreach(int index in new[]{14,15,26,28})purchases[index]=Math.Max(0,purchases[index]-1000);
            var plants=new List<GardenPlant>();var occupied=new HashSet<int>();
            for(int i=0;i<count;i++)
            {
                int o=PlantOffset+i*PlantSize,kind=Int(bytes,o),garden=Int(bytes,o+4),x=Int(bytes,o+8),y=Int(bytes,o+12),growth=Int(bytes,o+36),waters=Int(bytes,o+40),feed=Int(bytes,o+44),need=Int(bytes,o+48);
                if(kind<0||kind>48||garden<0||garden>3||growth<0||growth>3||waters<0||waters>5||feed<3||feed>5||need<0||need>4)throw new InvalidDataException("原版盆栽字段异常。");
                if(garden==2)continue; // The original wheelbarrow retains its opaque record in the imported source copy.
                GardenKind mapped=garden==3?GardenKind.Aquarium:(GardenKind)garden;int slot=x+y*8;
                if(slot<0||slot>31||!occupied.Add((int)mapped*32+slot))throw new InvalidDataException("原版盆栽位置异常。");
                long watered=BitConverter.ToInt64(bytes,o+24),fulfilled=BitConverter.ToInt64(bytes,o+56),fertilized=BitConverter.ToInt64(bytes,o+64);
                plants.Add(new GardenPlant{Id=Guid.NewGuid(),OriginalIndex=i,Kind=(PlantKind)kind,Garden=mapped,Slot=slot,Growth=growth,Waterings=waters,FeedingsPerGrow=feed,
                    MatureNeed=need==4?GardenNeed.Music:GardenNeed.BugSpray,NextCareUtc=Math.Max(watered+16,fertilized+3600),HappyUntilUtc=fulfilled>0?fulfilled+3600:0,
                    LastWaterUtc=watered,LastNeedFulfilledUtc=fulfilled,LastFertilizedUtc=fertilized,ChocolateUntilUtc=BitConverter.ToInt64(bytes,o+72)+300});
            }
            return new OriginalUserSave{Name=Path.GetFileNameWithoutExtension(filename),Level=level,Coins=coins,FinishedAdventure=finished,MinigamesUnlocked=Int(bytes,768)!=0,PuzzleUnlocked=Int(bytes,772)!=0,SurvivalUnlocked=Int(bytes,792)!=0,Purchases=purchases,ChallengeRecords=records,Plants=plants.AsReadOnly(),OriginalBytes=(byte[])bytes.Clone()};
        }
        public static void KeepImportedSource(OriginalUserSave save,string destination)
        {
            if(save==null)throw new ArgumentNullException("save");using(var f=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(save.OriginalBytes,0,save.OriginalBytes.Length);f.Flush(true);}
        }
        public static void ExportUser(string importedCopy,string destination,PlayerAccount account,AdventureProgress adventure)
        {
            if(string.Equals(Path.GetFullPath(importedCopy),Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("请选择单独的导出文件。");
            var original=ReadUser(importedCopy);var source=original.OriginalBytes;int oldCount=Int(source,816);
            // Preserve original wheelbarrow plants and all unknown achievement/Zombatar/footer bytes verbatim.
            var wheelbarrow=new List<byte[]>();for(int i=0;i<oldCount;i++)if(Int(source,PlantOffset+i*88+4)==2){var b=new byte[88];Array.Copy(source,PlantOffset+i*88,b,0,88);wheelbarrow.Add(b);}
            int count=account.Plants.Count+wheelbarrow.Count;int oldTail=PlantOffset+oldCount*88,newTail=PlantOffset+count*88;
            var result=new byte[newTail+source.Length-oldTail];Array.Copy(source,result,PlantOffset);Array.Copy(source,oldTail,result,newTail,source.Length-oldTail);
            Put(result,4,adventure.Completed?1:adventure.NextLevel);Put(result,8,account.Coins);Put(result,12,adventure.CompletedRuns);Put(result,816,count);Put(result,768,account.MinigamesUnlocked?1:0);Put(result,772,account.PuzzleUnlocked?1:0);Put(result,792,account.SurvivalUnlocked||adventure.CompletedRuns>0?1:0);
            for(int i=0;i<100;i++)Put(result,16+i*4,i==49?account.TreeHeight:account.ModeRecord(i));
            for(int i=0;i<80;i++){int n=account.PurchaseCount(i);if((i==14||i==15||i==26||i==28)&&n>0)n+=1000;Put(result,416+i*4,n);}
            int index=0;foreach(var g in account.Plants)
            {
                int o=PlantOffset+index++*88;if(g.OriginalIndex>=0&&g.OriginalIndex<oldCount)Array.Copy(source,PlantOffset+g.OriginalIndex*88,result,o,88);Put(result,o,(int)g.Kind);Put(result,o+4,g.Garden==GardenKind.Aquarium?3:(int)g.Garden);Put(result,o+8,g.Slot%8);Put(result,o+12,g.Slot/8);
                PutLong(result,o+24,g.LastWaterUtc);Put(result,o+36,g.Growth);Put(result,o+40,g.Waterings);Put(result,o+44,g.FeedingsPerGrow);
                Put(result,o+48,g.MatureNeed==GardenNeed.Music?4:3);PutLong(result,o+56,g.LastNeedFulfilledUtc);PutLong(result,o+64,g.LastFertilizedUtc);PutLong(result,o+72,Math.Max(0,g.ChocolateUntilUtc-300));
            }
            foreach(var plant in wheelbarrow){Array.Copy(plant,0,result,PlantOffset+index++*88,88);}
            using(var f=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(result,0,result.Length);f.Flush(true);}
        }
    }
}
