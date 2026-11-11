#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Progression;
internal static class ModeCoreProbe
{
    private static int count;
    private static void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
    private static void AddSun(GameSimulation game,int value)
    {
        // Valid native sun values; collection credits only after the flight reaches the counter.
        while(value>0){int piece=value>=25?25:15;var sun=new Sun{Value=piece};game.Suns.Add(sun);if(!game.CollectSun(sun))throw new Exception("Sun click refused");
            for(int tick=0;tick<500&&game.Suns.Contains(sun);tick++)game.UpdateMode(.01f);if(game.Suns.Contains(sun))throw new Exception("Collected sun never reached bank");value-=piece;}
    }
    private static void Main()
    {
        PlantsVsZombies.Tests.Modes.ModeAdviceRegressionTests.Run();
        int modes=0;foreach(var mode in ModeCatalog.All){Check(mode.RecordIndex==mode.Id-1,"PC record index "+mode.Id);modes++;}Check(modes==55,"55 independent PC catalog entries");
        var survival=new ModeSession(1);Check(survival.ChooseSeeds(survival.Game.Adventure.SelectedSeeds),"survival seed selection");
        var veteran=new Plant{Kind=PlantKind.Peashooter,Row=2,Column=0,Layer=PlantLayer.Main,Health=211};survival.Game.Plants.Add(veteran);AddSun(survival.Game,200);int sunBefore=survival.Game.SunAmount;survival.Game.Mowers[0].Available=false;
        survival.Game.Adventure.CurrentWave=survival.Game.Adventure.Waves.Count;survival.Game.Adventure._finalAmbush=true;survival.Update(0.01f);
        Check(survival.HasNextStage&&survival.FlagsCompleted==1,"survival flag/round completion");survival.Game.Adventure.TotalWavesSpawned=80;Check(survival.Continue(),"survival next round");Check(survival.Game.Plants.Contains(veteran)&&veteran.Health==211,"survival preserves actual plants/health");Check(survival.Game.SunAmount==sunBefore&&!survival.Game.Mowers[0].Available&&survival.Game.Adventure.TotalWavesSpawned==0,"survival carries sun and used mower; native wave-loot counter resets");Check(survival.Phase==ModePhase.ChoosingSeeds&&survival.Stage==2,"survival reselects seeds");
        var vase=new ModeSession(52);int leftCell=-1;foreach(var pair in vase._vaseSeeds)if(pair.Value==52){leftCell=pair.Key;break;}Check(leftCell>=0,"PC leftpeater recipe");int before=vase.Game.Adventure.Vases.Count;Check(vase.SmashVase(leftCell/9,leftCell%9),"smash original vase");Check(vase.Game.Adventure.Vases.Count==before-1,"vase actually removed");int packet=vase.Game.Adventure.Packets[0].Id;Check(vase.UsePacket(packet,0,0),"free vase packet plant");Check(vase.Game.Plants.Exists(p=>p.Row==0&&p.Column==0&&p.ModeFacesLeft),"PC seed52 faces left");
        var iz=new ModeSession(61);Check(iz.Game.Plants.Exists(p=>p.Kind==PlantKind.Sunflower&&p.Column==3&&p.Row==1),"PC layout differs from mobile");Check(!iz.PlaceZombie(ZombieKind.Normal,0,3),"I Zombie planting boundary");Check(iz.PlaceZombie(ZombieKind.Normal,0,4)&&iz.Game.SunAmount==100,"I Zombie purchase debits sun");
        var flower=iz.Game.Plants.Find(p=>p.Kind==PlantKind.Sunflower);flower.Health-=80;iz.Update(0.01f);Check(iz.Game.Suns.Count==2&&iz.Game.SunAmount==100,"I Zombie chewing releases collectible sun");AddSun(iz.Game,25);Check(iz.Game.SunAmount==125,"I Zombie sun collection economy");
        iz.Game.Plants.RemoveAll(p=>p.Row==0);var brainZombie=iz.Game.Zombies[0];brainZombie.X=0.16f;iz.Update(0.72f);Check(iz.Brains[0],"I Zombie brain eaten via actual zombie arrival");
        var match=new ModeSession(20);match.Game.Plants.RemoveAll(p=>p.Row==0&&p.Column<3||p.Row==1&&p.Column==1);match.Game.Plants.Add(new Plant{Kind=PlantKind.Peashooter,Row=0,Column=0,Layer=PlantLayer.Main,Health=300});match.Game.Plants.Add(new Plant{Kind=PlantKind.SnowPea,Row=0,Column=1,Layer=PlantLayer.Main,Health=300});match.Game.Plants.Add(new Plant{Kind=PlantKind.Peashooter,Row=0,Column=2,Layer=PlantLayer.Main,Health=300});match.Game.Plants.Add(new Plant{Kind=PlantKind.Peashooter,Row=1,Column=1,Layer=PlantLayer.Main,Health=300});match._matchDelay=0;Check(match.ClickMatchCell(1,1)&&match.ClickMatchCell(0,1),"Beghouled playable adjacent swap");Check(match.Score>=1&&match.Game.Plants.Count==40,"matching scores and refills board");
        var slot=new ModeSession(18);Check(slot.Spin()&&slot.Game.SunAmount==25,"native 25-sun slot purchase");Check(!slot.Spin(),"slot cannot pay twice while rolling");slot.Update(4.01f);Check(slot.SlotRollingRemaining<=0,"native slot roll resolves");
        var aqua=new ModeSession(23);Check(aqua.Fish.Count==2,"aquarium initial fish");Check(aqua.Feed(150,200)&&aqua.Game.SunAmount==45,"aquarium five-sun feeding");Check(aqua.Feed(200,250)&&aqua.Feed(250,250)&&!aqua.Feed(300,300),"native maximum three brains");AddSun(aqua.Game,150);Check(aqua.BuyFish()&&aqua.Fish.Count==3,"aquarium purchasing adds real fish");
        int starCells=0;for(int r=0;r<5;r++)for(int c=0;c<9;c++)if(GameSimulation.IsModeStarCell(r,c))starCells++;Check(starCells==14,"PC 0x74ED00 star pattern");
        string save=Path.Combine(Path.GetTempPath(),"pvz-mode-probe-"+Guid.NewGuid().ToString("N")+".account");Guid profile=Guid.NewGuid();string receipt=Guid.NewGuid().ToString("N");var store=new PlayerAccountStore(save);store.RecordModeResult(profile,ModeCatalog.Get(16).RecordIndex,1,receipt,100);store.RecordModeResult(profile,ModeCatalog.Get(16).RecordIndex,1,receipt,100);var reloaded=new PlayerAccountStore(save).Get(profile);Check(reloaded.ModeRecord(15)==1&&reloaded.ModeRecord(16)==0,"native records use modeId-1");Check(reloaded.Coins==100,"atomic reward once across reload");
        var records=new int[100];Func<int,int> read=i=>records[i];
        Check(!ModeUnlockRules.CategoryUnlocked(16,1,false)&&ModeUnlockRules.CategoryUnlocked(16,23,false),"native mini-game category gift gate");
        Check(!ModeUnlockRules.CategoryUnlocked(51,36,false)&&ModeUnlockRules.CategoryUnlocked(51,37,false),"native puzzle category gift gate");
        Check(!ModeUnlockRules.CategoryUnlocked(1,50,false)&&ModeUnlockRules.CategoryUnlocked(1,1,true),"survival category requires adventure completion");
        Check(ModeUnlockRules.MoreTrophiesNeeded(16,false,read)==0&&ModeUnlockRules.MoreTrophiesNeeded(18,false,read)==0&&ModeUnlockRules.MoreTrophiesNeeded(19,false,read)>0,"PC first three mini-game cards");
        Check(ModeUnlockRules.MoreTrophiesNeeded(51,false,read)==0&&ModeUnlockRules.MoreTrophiesNeeded(52,false,read)==1,"PC puzzle sequential unlock");
        records[50]=records[51]=records[52]=1;Check(ModeUnlockRules.MoreTrophiesNeeded(54,false,read)>0&&ModeUnlockRules.MoreTrophiesNeeded(54,true,read)==0,"fourth puzzle requires finished adventure after first three");
        for(int i=50;i<59;i++)records[i]=1;Check(ModeUnlockRules.MoreTrophiesNeeded(60,true,read)==0&&ModeUnlockRules.MoreTrophiesNeeded(61,true,read)==0,"puzzle groups and endless are separate");
        for(int i=0;i<5;i++)records[i]=5;for(int i=5;i<10;i++)records[i]=10;Check(ModeUnlockRules.MoreTrophiesNeeded(13,true,read)==0,"ten survival trophies unlock endless");records[9]=9;Check(ModeUnlockRules.MoreTrophiesNeeded(13,true,read)==1,"unfinished hard survival is not a trophy");
        var bowling=new ModeSession(17);bowling.Update(1.99f);Check(bowling.Game.Adventure.CurrentWave==0,"mode native scheduler before 200 ticks");bowling.Update(.01f);Check(bowling.Game.Adventure.CurrentWave==1&&bowling.Game.Zombies.Count>0,"mode native scheduler at 200 ticks");
        var whack=new ModeSession(30);whack.Update(2.51f);Check(whack.Game.Adventure.CurrentWave==1&&whack.Game.Zombies.Count>0,"native hammer schedule remains active without generic ambush");int hammerTarget=whack.Game.Zombies[0].Id;Check(whack.Game.HammerAdventureZombie(hammerTarget),"standalone hammer actually hits spawned zombie");
        var roof=new ModeSession(5);Check(roof.Game.Mowers.TrueForAll(m=>!m.Available),"roof cleaners are not gifted");var boughtRoof=new ModeSession(5,null,6,123,true,true);Check(boughtRoof.Game.Mowers.TrueForAll(m=>m.Available)&&boughtRoof.Game.Rules.AdventurePoolCleaners,"purchased cleaners carried into mode rules");
        int rakeCharges=0;bowling.Game.ApplyModePurchases(true,()=>{rakeCharges++;return true;});Check(rakeCharges==1&&bowling.Game.Adventure.RakeRow>=0,"rake charges once when actually placed");aqua.Game.ApplyModePurchases(true,()=>{rakeCharges++;return true;});Check(rakeCharges==1&&aqua.Game.Adventure.RakeRow<0,"special aquarium consumes no rake");
        var gold = bowling.Game.SpawnAdventureCoin(AdventureCoinKind.Gold, 1, 1);
        bowling.Game.Rules.AdventureCoinCommit = total => false;
        Check(bowling.Game.CollectAdventureCoin(gold), "Mode money starts its original collection flight");
        bowling.Update(1.5f);
        Check(gold.PersistencePending && !gold.IsBeingCollected && bowling.Game.Adventure.Coins.Contains(gold), "Failed arrival-time persistence preserves a collectible");
        var committed = 0;
        bowling.Game.Rules.AdventureCoinCommit = total =>
        {
            committed = total;
            return true;
        };
        Check(bowling.Game.CollectAdventureCoin(gold), "The pending mode coin can be retried");
        bowling.Update(.01f);
        Check(committed == 5 && !bowling.Game.Adventure.Coins.Contains(gold), "Coin retry commits cumulative units once");
        vase.Game.ApplyModePurchases(true,null);var nutCells=new List<int>();foreach(var pair in vase._vaseSeeds)if(pair.Value==3)nutCells.Add(pair.Key);Check(nutCells.Count>=2,"PC vase wall-nut packets available");
        vase.SmashVase(nutCells[0]/9,nutCells[0]%9);int firstNut=vase.Game.Adventure.Packets[vase.Game.Adventure.Packets.Count-1].Id;Check(vase.UsePacket(firstNut,1,0),"first-aid initial wall-nut");vase.Game.AdventurePlantAt(1,0,PlantLayer.Main).Health=1000;vase.SmashVase(nutCells[1]/9,nutCells[1]%9);int nextNut=vase.Game.Adventure.Packets[vase.Game.Adventure.Packets.Count-1].Id;Check(vase.UsePacket(nextNut,1,0)&&vase.Game.AdventurePlantAt(1,0,PlantLayer.Main).Health==4000,"purchased first-aid heals through mode packet planting");
        var variants=new ModeSession(32);variants.ChooseSeeds(variants.Game.Adventure.SelectedSeeds);int nextId=5000;
        Func<int,int,Zombie> addVariant=(kind,row)=>{var z=new Zombie{Id=++nextId,Kind=(ZombieKind)kind,Row=row,X=8f,Health=270,StartingHealth=270,Speed=.18f};variants.Game.Zombies.Add(z);return z;};
        var jalapeno=addVariant(28,0);var gatling=addVariant(29,1);var squash=addVariant(30,2);var tall=addVariant(31,3);variants.Update(.01f);
        Check(jalapeno.ModeHead==28&&jalapeno.Health==500&&jalapeno._modeHeadTimer>1.5f,"PC28 is jalapeno, not tall-nut");
        Check(tall.ModeHead==31&&tall.ArmorHealth==2200,"PC31 is tall-nut armor");gatling._modeHeadTimer=.7f;int shots=variants.HeadShots.Count;variants.Update(.02f);Check(variants.HeadShots.Count>shots,"PC29 fires gatling burst");
        var crush=new Plant{Kind=PlantKind.Wallnut,Row=2,Column=7,Health=4000,Layer=PlantLayer.Main};variants.Game.Plants.Add(crush);variants.Update(.01f);Check(!variants.Game.Plants.Contains(crush)&&!variants.Game.Zombies.Contains(squash),"PC30 performs squash impact");
        var burnedRowPlant=new Plant{Kind=PlantKind.Wallnut,Row=0,Column=0,Health=4000,Layer=PlantLayer.Main};variants.Game.Plants.Add(burnedRowPlant);jalapeno._modeHeadTimer=.001f;variants.Update(.01f);Check(!variants.Game.Plants.Contains(burnedRowPlant)&&!variants.Game.Zombies.Contains(jalapeno),"PC28 explodes its lane");
        Console.WriteLine("PASS: "+count+" targeted assertions; no Unity build, no whole-mode playthrough.");
    }
}
