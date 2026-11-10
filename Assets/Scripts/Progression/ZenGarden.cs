using System;
using System.Collections.Generic;
using PvZReplica.Randomness;

namespace PvZReplica.Progression
{
    public enum GardenTool { Water, Fertilizer, BugSpray, Music, Collect, Sell, Move, Chocolate }
    public sealed partial class PlayerAccountStore
    {
        private static OriginalRandom GardenRandom(GardenPlant plant,long utc)
        {return new OriginalRandom(unchecked((uint)plant.Id.GetHashCode()^(uint)utc^(uint)plant.CareCycle));}
        private static int EmptyGardenSlot(PlayerAccount p,GardenKind garden)
        {
            int capacity=garden==GardenKind.Greenhouse?32:8;
            for(int slot=0;slot<capacity;slot++)if(!p.garden.Exists(g=>g.Garden==garden&&g.Slot==slot))return slot;
            return -1;
        }
        private static void AddGardenPlant(PlayerAccount p,PlantKind kind,long utc)
        {
            int slot=EmptyGardenSlot(p,GardenKind.Greenhouse);if(slot<0)throw new InvalidOperationException("禅境花园已满。");
            var g=new GardenPlant{Id=Guid.NewGuid(),Kind=kind,Slot=slot,LastCoinUtc=utc};g.FeedingsPerGrow=3+(int)GardenRandom(g,utc).NextBounded(3);p.garden.Add(g);
        }
        public void CollectModeUnlock(Guid id,string receipt,bool puzzle)
        {
            Change(id,p=>{if(puzzle)p.PuzzleUnlocked=true;else p.MinigamesUnlocked=true;});
        }
        public void CollectBattleLoot(Guid id,string receipt,bool chocolate,PlantKind kind,long utc)
        {
            lock(gate){var p=Get(id);string key="gift:"+receipt;if(p.receipts.ContainsKey(key))return;
                if(chocolate)p.purchases[26]=Math.Min(10,p.purchases[26]+1);else AddGardenPlant(p,kind,utc);
                p.receipts[key]=1;TrimReceipts(p);Commit(p);}
        }
        public void GiveGardenPlant(Guid id,PlantKind kind,long utc,string receipt)
        {
            if((int)kind<0||(int)kind>39||string.IsNullOrEmpty(receipt)||receipt.Length>80)throw new ArgumentException();
            lock(gate){var p=Get(id);string key="gift:"+receipt;if(p.receipts.ContainsKey(key))return;AddGardenPlant(p,kind,utc);p.receipts[key]=1;TrimReceipts(p);Commit(p);}
        }
        public void TendGarden(Guid id,Guid plantId,GardenTool tool,long utc,GardenKind target=GardenKind.Greenhouse)
        {
            Change(id,p=>{
                var g=p.garden.Find(x=>x.Id==plantId);if(g==null)throw new InvalidOperationException("找不到这株植物。");
                if(tool==GardenTool.Collect){AddCoins(p,g.PendingCoins);g.PendingCoins=0;return;}
                if(tool==GardenTool.Sell){AddCoins(p,SellPrice(g));p.garden.Remove(g);return;}
                if(tool==GardenTool.Move)
                {
                    if(p.PurchaseCount(17)==0)throw new InvalidOperationException("请先购买园艺手套。");
                    if(target!=g.Garden&&p.PurchaseCount(19)==0)throw new InvalidOperationException("移动到其他花园需要手推车。");
                    if(target==GardenKind.Mushroom&&p.PurchaseCount(18)==0||target==GardenKind.Aquarium&&p.PurchaseCount(25)==0)throw new InvalidOperationException("尚未购买这个花园。");
                    int slot=EmptyGardenSlot(p,target);if(slot<0)throw new InvalidOperationException("目标花园已满。");g.Garden=target;g.Slot=slot;return;
                }
                var need=g.Need(utc);
                if(tool==GardenTool.Chocolate)
                {
                    if(g.Growth!=3||need!=GardenNeed.Happy||utc<g.ChocolateUntilUtc)throw new InvalidOperationException("只可给心满意足的成株喂巧克力。");
                    if(p.purchases[26]<=0)throw new InvalidOperationException("没有巧克力。");p.purchases[26]--;g.ChocolateUntilUtc=utc+300;g.LastCoinUtc=utc+2;return;
                }
                if(tool==GardenTool.Water&&need==GardenNeed.Water)
                {
                    var watered=new List<GardenPlant>{g};
                    if(p.PurchaseCount(13)>0)foreach(var other in p.garden)if(other!=g&&other.Garden==g.Garden&&Math.Abs(other.Slot%8-g.Slot%8)<=1&&Math.Abs(other.Slot/8-g.Slot/8)<=1&&other.Need(utc)==GardenNeed.Water)watered.Add(other);
                    foreach(var plant in watered){plant.Waterings++;plant.LastWaterUtc=utc-(int)GardenRandom(plant,utc).NextBounded(10);plant.NextCareUtc=plant.LastWaterUtc+16;if(plant.Growth==3&&plant.Waterings>=plant.FeedingsPerGrow)plant.MatureNeed=GardenRandom(plant,utc).NextBounded(2)==0?GardenNeed.BugSpray:GardenNeed.Music;}
                    return;
                }
                if(tool==GardenTool.Fertilizer&&need==GardenNeed.Fertilizer)
                {
                    if(p.purchases[14]<=0)throw new InvalidOperationException("肥料已用完。");p.purchases[14]--;g.Growth++;g.Waterings=0;g.LastFertilizedUtc=utc;g.NextCareUtc=utc+3600;
                    g.PendingCoins+=g.Growth==1?5:g.Growth==2?10:g.Kind==PlantKind.Marigold?100:200;return;
                }
                if(tool==GardenTool.BugSpray&&need==GardenNeed.BugSpray||tool==GardenTool.Music&&need==GardenNeed.Music)
                {
                    int item=tool==GardenTool.BugSpray?15:16;if(p.purchases[item]<=0)throw new InvalidOperationException(tool==GardenTool.BugSpray?"杀虫剂已用完。":"请先购买留声机。");
                    if(item==15)p.purchases[item]--;g.LastNeedFulfilledUtc=utc;g.HappyUntilUtc=utc+86400;g.LastCoinUtc=utc+18+(int)GardenRandom(g,utc).NextBounded(13);g.Waterings=0;g.CareCycle++;g.PendingCoins+=g.Kind==PlantKind.Marigold?5:15;return;
                }
                throw new InvalidOperationException(need==GardenNeed.Resting?"植物暂时不需要照料。":"请按植物的需求选择工具。");
            });
        }
        // Called only while the garden is open. Production does not simulate coins while the game is closed.
        public void UpdateGarden(Guid id,long utc,bool entering=false)
        {
            lock(gate)
            {
                var p=Get(id);bool changed=false;
                foreach(var g in p.garden)
                {
                    if(g.Growth!=3||!g.InPreferredGarden||g.Need(utc)!=GardenNeed.Happy)continue;
                    if(entering){g.LastCoinUtc=utc+18+(int)GardenRandom(g,utc).NextBounded(13);changed=true;continue;}
                    if(utc<g.LastCoinUtc)continue;
                    var rng=GardenRandom(g,utc);long happyMinutes=Math.Max(0,(utc-g.LastNeedFulfilledUtc)/60);
                    int rarity=(int)Math.Min(80,Math.Max(0,happyMinutes-5)*80/25);
                    g.PendingCoins+=rng.NextBounded(1000)+rarity<100?5:1;
                    int maximum=(int)(3000+Math.Min(25,Math.Max(0,happyMinutes-5))*480);
                    int seconds=18+(int)rng.NextBounded((uint)Math.Max(1,maximum/100-17));
                    g.LastCoinUtc=utc+(g.ChocolateUntilUtc>utc?Math.Max(1,seconds/2):seconds);changed=true;
                }
                if(p.PurchaseCount(20)>0&&p.SnailAwakeUntilUtc>utc)foreach(var g in p.garden)if(g.PendingCoins>0){AddCoins(p,g.PendingCoins);g.PendingCoins=0;changed=true;}
                if(changed)Commit(p);
            }
        }
        public void WakeSnail(Guid id,long utc){Change(id,p=>{if(p.PurchaseCount(20)==0)throw new InvalidOperationException("尚未购买蜗牛。");p.SnailAwakeUntilUtc=utc+180;});}
        public static int SellPrice(GardenPlant plant)
        {
            if(plant.Kind==PlantKind.Marigold)return new[]{150,200,250,300}[plant.Growth];
            if(plant.Growth<3)return new[]{150,300,500}[plant.Growth];
            return GardenPlant.IsMushroom(plant.Kind)||GardenPlant.IsAquatic(plant.Kind)?1000:800;
        }
    }
}
