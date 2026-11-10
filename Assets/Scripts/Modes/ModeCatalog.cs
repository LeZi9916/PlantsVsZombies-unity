using System;
using System.Collections.Generic;
using PvZReplica.VerifiedData;

namespace PvZReplica.Modes
{
    public enum ModeMechanic { Survival, PeaHeads, Bowling, Slots, RainingSeeds, Beghouled, Invisible,
        Stars, Aquarium, BeghouledTwist, LittleZombies, Portals, Columns, Bobsled, Speed,
        Whack, LastStand, Pogo, Boss, Vases, IZombie }
    public sealed class ModeDefinition
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public int RecordIndex { get { return Id-1; } }
        public ModeMechanic Mechanic { get; internal set; }
        public int BoardLevel { get; internal set; }
        public int WaveCount { get; internal set; }
        public bool Endless { get { return Id>=11&&Id<=15 || Id==60 || Id==70; } }
        public bool Survival { get { return Mechanic==ModeMechanic.Survival; } }
        public bool Hard { get { return Id>=6&&Id<=15; } }
        public bool Puzzle { get { return Id>=51&&Id<=70; } }
        public int FlagsPerStage { get { return Survival&&Hard?2:1; } }
        public int Stages { get { return Survival?5:Mechanic==ModeMechanic.LastStand?5:1; } }
        public bool UsesPackets { get { return Mechanic==ModeMechanic.Bowling || Mechanic==ModeMechanic.Slots ||
            Mechanic==ModeMechanic.RainingSeeds || Mechanic==ModeMechanic.Invisible || Mechanic==ModeMechanic.LittleZombies ||
            Mechanic==ModeMechanic.Columns || Mechanic==ModeMechanic.Portals || Mechanic==ModeMechanic.Boss || Mechanic==ModeMechanic.Vases; } }
        public bool SupportsRake { get { return Mechanic!=ModeMechanic.IZombie&&Mechanic!=ModeMechanic.Aquarium&&Mechanic!=ModeMechanic.Boss&&Mechanic!=ModeMechanic.Whack&&Mechanic!=ModeMechanic.Beghouled&&Mechanic!=ModeMechanic.BeghouledTwist&&Mechanic!=ModeMechanic.Bobsled; } }
        public bool ChoosesSeeds { get { return !UsesPackets && Mechanic!=ModeMechanic.Beghouled && Mechanic!=ModeMechanic.BeghouledTwist &&
            Mechanic!=ModeMechanic.Aquarium && Mechanic!=ModeMechanic.Whack && Mechanic!=ModeMechanic.IZombie; } }
    }
    /// <summary>IDs and directory positions: local PC table 0x74E490. Rules: Evidence/pc, not mobile enums.</summary>
    public static class ModeCatalog
    {
        private static readonly Dictionary<int,ModeDefinition> definitions=Build();
        public static IEnumerable<ModeDefinition> All { get { return definitions.Values; } }
        public static bool TryGet(int id,out ModeDefinition definition) { return definitions.TryGetValue(id,out definition); }
        public static ModeDefinition Get(int id) { ModeDefinition d;if(!TryGet(id,out d))throw new ArgumentOutOfRangeException("id");return d; }
        private static Dictionary<int,ModeDefinition> Build()
        {
            var d=new Dictionary<int,ModeDefinition>();
            string[] terrain={"白天","黑夜","泳池","浓雾","屋顶"};int[] level={8,18,28,38,48};
            for(int id=1;id<=15;id++)d.Add(id,new ModeDefinition{Id=id,Name="生存："+terrain[(id-1)%5]+(id<=5?"":"（"+(id<=10?"困难":"无尽")+"）"),Mechanic=ModeMechanic.Survival,BoardLevel=level[(id-1)%5],WaveCount=id<=5?10:20});
            string[] names={"植物僵尸","坚果保龄球","老虎机","种子雨","宝石迷阵","隐形食脑者","看星星","僵尸水族馆","宝石迷阵转转看","小僵尸大麻烦","传送门之战","植物列阵","雪橇区","僵尸快跑","锤僵尸","坚不可摧","植物僵尸2","坚果保龄球2","蹦蹦舞会","僵王博士的复仇"};
            ModeMechanic[] rules={ModeMechanic.PeaHeads,ModeMechanic.Bowling,ModeMechanic.Slots,ModeMechanic.RainingSeeds,ModeMechanic.Beghouled,ModeMechanic.Invisible,ModeMechanic.Stars,ModeMechanic.Aquarium,ModeMechanic.BeghouledTwist,ModeMechanic.LittleZombies,ModeMechanic.Portals,ModeMechanic.Columns,ModeMechanic.Bobsled,ModeMechanic.Speed,ModeMechanic.Whack,ModeMechanic.LastStand,ModeMechanic.PeaHeads,ModeMechanic.Bowling,ModeMechanic.Pogo,ModeMechanic.Boss};
            int[] boards={8,5,8,38,18,38,8,28,18,25,18,48,28,28,15,28,28,5,48,50};
            // PC 0x4150C0: 17/26/16/21 have 20 waves; 30 has 12; remaining special predicates select 30 or 40.
            int[] counts={20,20,40,40,40,20,30,0,40,30,20,30,30,30,12,10,30,30,30,0};
            for(int i=0;i<20;i++)d.Add(i+16,new ModeDefinition{Id=i+16,Name=names[i],Mechanic=rules[i],BoardLevel=boards[i],WaveCount=counts[i]});
            string[] vase={"砸罐者","未知领域","第三个罐子","连锁反应","M的意思是金属","胆怯的陶艺师","震天锤","另一个连锁反应","王牌组合","砸罐者无尽版"};
            string[] zombie={"我是僵尸","我也是僵尸","你能铲了它吗","完全傻了","死亡之舞","交换","罐子头","所有你的脑子都属于我们","完全疯狂","我是僵尸无尽版"};
            for(int i=0;i<10;i++)
            {
                d.Add(51+i,new ModeDefinition{Id=51+i,Name=vase[i],Mechanic=ModeMechanic.Vases,BoardLevel=35,WaveCount=0});
                d.Add(61+i,new ModeDefinition{Id=61+i,Name=zombie[i],Mechanic=ModeMechanic.IZombie,BoardLevel=18,WaveCount=0});
            }
            return d;
        }
    }
}
