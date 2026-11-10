using System;
using System.IO;
using System.Collections.Generic;
using PvZReplica;
using PvZReplica.Adventure;
using PvZReplica.Profiles;
using PvZReplica.Progression;

internal static class Program
{
    private static int checks;
    private static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    private static void Put(byte[] b,int offset,int value){Array.Copy(BitConverter.GetBytes(value),0,b,offset,4);}
    private static void ExpectFailure(Action action,string message){bool failed=false;try{action();}catch(InvalidOperationException){failed=true;}Check(failed,message);}
    private static void Main()
    {
        string folder=Path.Combine(AppContext.BaseDirectory,"artifacts","integration-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        byte[] native=new byte[820+88+72];Put(native,0,12);Put(native,4,36);Put(native,8,10000);Put(native,12,1);
        Put(native,16+49*4,5);Put(native,416,1);Put(native,416+14*4,1005);Put(native,416+15*4,1005);Put(native,416+16*4,1);Put(native,416+27*4,1);Put(native,416+28*4,1005);Put(native,816,1);
        Put(native,820,38);Put(native,820+16,1);Put(native,820+20,0x12345678);Put(native,820+44,3);for(int i=0;i<40;i++)native[908+i]=(byte)(i%2);native[native.Length-1]=1;
        string original=Path.Combine(folder,"user7.dat");File.WriteAllBytes(original,native);
        using(var users=new BinaryWriter(File.Create(Path.Combine(folder,"users.dat")))){users.Write(14);users.Write((ushort)1);users.Write((ushort)3);users.Write(new byte[]{90,111,235});users.Write(7u);users.Write(7u);}
        var directory=OriginalSaveFormat.ReadDirectory(folder);Check(directory.Count==1&&directory[0].Name=="Zoë"&&directory[0].OriginalId==7,"Native v14 byte-wise username and user mapping");
        var source=OriginalSaveFormat.ReadUser(original);Check(source.Level==36&&source.Coins==10000&&source.Plants.Count==1,"Native v12 import");Check(source.Purchases[14]==5,"Native supply marker normalization");
        var profiles=new ProfileCatalog(Path.Combine(folder,"profiles.dat"));Guid id=Guid.NewGuid();profiles.CreateImported(id,"原版导入");
        var store=new PlayerAccountStore(Path.Combine(folder,"wallet.account"));store.Import(id,source.Coins,source.Purchases,source.ChallengeRecords,source.ChallengeRecords[49],source.Plants);
        var progress=new AdventureProgressStore(Path.Combine(folder,"campaign.adventure"));progress.Import(id,source.Level,source.FinishedAdventure);
        Check(store.Get(id).Owns(PlantKind.GatlingPea)&&progress.Get(id).Cycle==2,"Imported purchases and replay");
        ExpectFailure(()=>store.Purchase(id,1,1,0,1900000000),"Locked shop item must not charge");Check(store.Get(id).Coins==10000,"Failed purchase leaves wallet unchanged");
        int[] slotPrices={75,500,2000,8000};for(int i=0;i<3;i++){var p=store.Get(id);Check(ShopCatalog.Price(ShopCatalog.Get(21),p)==slotPrices[i],"Native slot price "+i);store.Purchase(id,21,36,1,1900000000);}
        int before=store.Get(id).Coins;ExpectFailure(()=>store.Purchase(id,21,36,1,1900000000),"Unaffordable slot cannot overdraw");Check(store.Get(id).Coins==before,"No partial debit");
        store.CreditBattleCoins(id,"pickups",2000);store.CreditBattleCoins(id,"pickups",2000);Check(store.Get(id).Coins==before+2000,"Coin receipt is idempotent");store.Purchase(id,21,36,1,1900000000);Check(store.Get(id).PacketSlots==10,"Purchased ten seed packets");
        store.Purchase(id,1,36,1,1900000000);Check(new PlayerAccountStore(Path.Combine(folder,"wallet.account")).Get(id).Owns(PlantKind.TwinSunflower),"Purchase survives reload");
        var campaign=new AdventureCampaign(progress,id,store);var game=campaign.BeginOrResume();Check(game.Adventure.Level.IsReplay&&game.Adventure.Level.PacketSlots==10&&game.Adventure.AvailableSeeds.Count==42,"Replay includes purchased seeds");Check(game.Adventure.RequiredSeeds.Count==3,"Dave fixes three seeds");
        var chosen=new List<PlantKind>(game.Adventure.RequiredSeeds);foreach(var k in new[]{PlantKind.GatlingPea,PlantKind.TwinSunflower,PlantKind.Imitater,PlantKind.Sunflower,PlantKind.Repeater,PlantKind.LilyPad,PlantKind.Wallnut,PlantKind.FumeShroom})if(k!=PlantKind.Imitater&&!chosen.Contains(k)&&chosen.Count<10)chosen.Add(k);foreach(var k in game.Adventure.AvailableSeeds)if(!chosen.Contains(k)&&chosen.Count<10)chosen.Add(k);
        Check(campaign.ChooseSeeds(chosen),"Owned upgraded loadout can be saved and played");var resumed=new AdventureCampaign(new AdventureProgressStore(Path.Combine(folder,"campaign.adventure")),id,store).BeginOrResume();Check(resumed.Adventure.SelectedSeeds.Count==10&&resumed.Adventure.Phase==AdventurePhase.Playing,"Saved upgraded loadout resumes");
        var coin=game.SpawnAdventureCoin(AdventureCoinKind.Gold,4,1);int money=store.Get(id).Coins;Check(game.CollectAdventureCoin(coin),"Battle collection");Check(store.Get(id).Coins==money+5&&!game.CollectAdventureCoin(coin),"Battle coin immediately durable once");
        var failure=new GameSimulation(new GameRules{AdventureLevel=8,AdventureCoinCommit=total=>false},17);failure.ChooseDefaultAdventureSeeds();var pending=failure.SpawnAdventureCoin(AdventureCoinKind.Silver,4,2);Check(!failure.CollectAdventureCoin(pending)&&pending.PersistencePending,"Persistence failure preserves coin");pending.Age=35;failure.Update(.01f);Check(failure.Adventure.Coins.Count==1,"Failed-save coin does not expire");failure.Rules.AdventureCoinCommit=total=>true;Check(failure.CollectAdventureCoin(pending)&&failure.Adventure.Coins.Count==0,"Pending pickup can be retried");
        Guid plant=store.Get(id).Plants[0].Id;long utc=1900000000;
        for(int stage=0;stage<3;stage++){for(int water=0;water<3;water++){var g=store.Get(id).Plants[0];utc=Math.Max(utc,g.NextCareUtc);store.TendGarden(id,plant,GardenTool.Water,utc);utc+=16;}utc=Math.Max(utc,store.Get(id).Plants[0].NextCareUtc);store.TendGarden(id,plant,GardenTool.Fertilizer,utc);utc=store.Get(id).Plants[0].NextCareUtc;}
        Check(store.Get(id).Plants[0].Growth==3&&store.Get(id).PurchaseCount(14)==2,"Garden growth consumes three fertilizer charges");
        for(int water=0;water<3;water++){utc=Math.Max(utc,store.Get(id).Plants[0].NextCareUtc);store.TendGarden(id,plant,GardenTool.Water,utc);utc+=16;}
        var mature=store.Get(id).Plants[0];utc=Math.Max(utc,mature.NextCareUtc);store.TendGarden(id,plant,mature.Need(utc)==GardenNeed.Music?GardenTool.Music:GardenTool.BugSpray,utc);Check(store.Get(id).Plants[0].Need(utc)==GardenNeed.Happy,"Mature plant daily care");
        int oldHeight=store.Get(id).TreeHeight;store.FeedWisdomTree(id);var reloaded=new PlayerAccountStore(Path.Combine(folder,"wallet.account")).Get(id);Check(reloaded.TreeHeight==oldHeight+1&&reloaded.PurchaseCount(28)==4,"Tree growth and supply persist");
        int wallet=store.Get(id).Coins;store.ClaimAdventureReward(id,2,36,4);int settled=store.Get(id).Coins;store.ClaimAdventureReward(id,2,36,4);Check(settled==wallet+70&&store.Get(id).Coins==settled,"Reward and mower settlement exactly once");
        string copy=Path.Combine(folder,"imported-copy.dat"),export=Path.Combine(folder,"export-user7.dat");OriginalSaveFormat.KeepImportedSource(source,copy);OriginalSaveFormat.ExportUser(copy,export,store.Get(id),progress.Get(id));var exported=OriginalSaveFormat.ReadUser(export);Check(exported.Coins==store.Get(id).Coins&&exported.Purchases[1]==1&&exported.ChallengeRecords[49]==oldHeight+1,"Native v12 export reflects progression");var output=File.ReadAllBytes(export);Check(BitConverter.ToInt32(output,840)==0x12345678&&output[output.Length-1]==1,"Opaque original plant/footer fields preserved");Check(File.ReadAllBytes(original).Length==native.Length&&BitConverter.ToInt32(File.ReadAllBytes(original),8)==10000,"Original source untouched");
        var bad=(byte[])native.Clone();Array.Resize(ref bad,bad.Length-1);string invalid=Path.Combine(folder,"truncated.dat");File.WriteAllBytes(invalid,bad);bool rejected=false;try{OriginalSaveFormat.ReadUser(invalid);}catch(InvalidDataException){rejected=true;}Check(rejected,"Truncated original footer rejected");
        Console.WriteLine("PASS "+checks+" focused progression checks");Console.WriteLine("Artifacts: "+folder);
    }
}
