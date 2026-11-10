using System;
using System.Collections.Generic;

namespace PvZReplica.Progression
{
    public sealed class ShopItem
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        public int PriceUnits { get; internal set; }
        public int UnlockLevel { get; internal set; }
        public bool AfterAdventure { get; internal set; }
        public bool GardenItem { get; internal set; }
        public bool Consumable { get; internal set; }
        public string Icon { get; internal set; }
    }
    public static class ShopCatalog
    {
        private static ShopItem I(int id,string name,int price,int level=25,bool after=false,bool garden=false,bool consumable=false,string icon=null)
        {return new ShopItem{Id=id,Name=name,PriceUnits=price,UnlockLevel=level,AfterAdventure=after,GardenItem=garden,Consumable=consumable,Icon=icon};}
        public static readonly IReadOnlyList<ShopItem> Items=Array.AsReadOnly(new[]{
            I(0,"机枪射手",500),I(1,"双子向日葵",500),I(2,"忧郁菇",750,35),I(3,"香蒲",1000,35),
            I(4,"冰瓜",1000,51,true),I(5,"吸金磁",300,41),I(6,"地刺王",750,41),I(7,"玉米加农炮",2000,51,true),I(8,"模仿者",3000,51,true),
            I(21,"种子槽升级",75,25,false,false,false,"Store_PacketUpgrade.png"),
            I(22,"池塘清扫车",100,25,false,false,false,"icon_poolcleaner.png"),I(23,"屋顶清扫车",300,42,false,false,false,"icon_roofcleaner.png"),
            I(24,"草耙",20,25,false,false,true,"icon_rake.png"),I(29,"坚果包扎术",200,51,true,false,false,"Store_FirstAidWallnutIcon.png"),
            I(10,"金盏花幼苗",250,51,true,true,true),I(11,"金盏花幼苗",250,51,true,true,true),I(12,"金盏花幼苗",250,51,true,true,true),
            I(13,"黄金浇水壶",1000,45,false,true,false,"WateringCanGold.png"),I(14,"肥料（5份）",75,45,false,true,true,"Fertilizer.png"),
            I(15,"杀虫剂（5份）",100,45,false,true,true,"BugSpray.png"),I(16,"留声机",1500,45,false,true,false,"Phonograph.png"),
            I(17,"园艺手套",100,45,false,true,false,"Zen_GardenGlove.png"),I(18,"蘑菇园",3000,51,true,true,false,"Store_MushroomGardenIcon.png"),
            I(19,"手推车",20,51,true,true,false,"Zen_WheelBarrow.png"),I(20,"蜗牛",300,51,true,true,false,"Stinky.png"),
            I(25,"水族馆",3000,51,true,true,false,"Store_AquariumGardenIcon.png"),I(27,"智慧树",1000,51,true,true,false,"Store_TreeOfWisdomIcon.png"),
            I(28,"树肥（1份）",250,51,true,true,true,"TreeFood.png")});
        public static ShopItem Get(int id){foreach(var item in Items)if(item.Id==id)return item;throw new ArgumentOutOfRangeException("id");}
        public static int Price(ShopItem item,PlayerAccount p)
        {
            if(item.Id!=21)return item.PriceUnits;
            int[] prices={75,500,2000,8000};return prices[Math.Min(3,p.PurchaseCount(21))];
        }
        public static bool IsUnlocked(ShopItem item,int nextLevel,int completedRuns)
        {return completedRuns>0 || !item.AfterAdventure&&nextLevel>=item.UnlockLevel;}
        public static bool SoldOut(ShopItem item,PlayerAccount p,long utc)
        {
            if(item.Id==21)return p.PurchaseCount(21)>=4;
            if(item.Id>=10&&item.Id<=12)return p.PurchaseCount(item.Id)==(int)(utc/86400) || p.Plants.Count>=32;
            if(item.Id==14||item.Id==15)return p.PurchaseCount(item.Id)>15;
            if(item.Id==24)return p.PurchaseCount(24)>0;
            if(item.Id==28)return p.PurchaseCount(28)>=10;
            return !item.Consumable&&p.PurchaseCount(item.Id)>0;
        }
    }
    public sealed partial class PlayerAccountStore
    {
        public void Purchase(Guid id,int itemId,int nextLevel,int completedRuns,long utc)
        {
            var item=ShopCatalog.Get(itemId);
            Change(id,p=>{
                if(!ShopCatalog.IsUnlocked(item,nextLevel,completedRuns))throw new InvalidOperationException("此商品尚未解锁。");
                if(ShopCatalog.SoldOut(item,p,utc))throw new InvalidOperationException("该商品已售完。");
                if(itemId==19&&p.PurchaseCount(18)==0&&p.PurchaseCount(25)==0)throw new InvalidOperationException("购买其他花园后才能使用手推车。");
                if(itemId==28&&p.PurchaseCount(27)==0)throw new InvalidOperationException("请先购买智慧树。");
                int price=ShopCatalog.Price(item,p);if(p.Coins<price)throw new InvalidOperationException("金币不足。");
                p.Coins-=price;
                if(itemId>=10&&itemId<=12){AddGardenPlant(p,PlantKind.Marigold,utc);p.purchases[itemId]=(int)(utc/86400);}
                else if(itemId==14||itemId==15)p.purchases[itemId]+=5;
                else if(itemId==24)p.purchases[itemId]=3;
                else p.purchases[itemId]++;
                if(itemId==27){p.TreeHeight=1;p.purchases[28]+=5;}
            });
        }
        public void FeedWisdomTree(Guid id)
        {
            Change(id,p=>{if(p.PurchaseCount(27)==0)throw new InvalidOperationException("尚未种下智慧树。");if(p.purchases[28]<=0)throw new InvalidOperationException("没有树肥，请到商店购买。");
                if(p.TreeHeight>=10000000)throw new InvalidOperationException("智慧树已经长得足够高了。");p.purchases[28]--;p.TreeHeight++;});
        }
        public void ConsumeRake(Guid id) {Change(id,p=>{if(p.purchases[24]>0)p.purchases[24]--;});}
    }
}
