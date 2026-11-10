using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using PvZReplica;
using PvZReplica.Adventure;
using PvZReplica.Profiles;

internal static class AdventureUnitTests
{
    private static int assertions;
    private static void Check(bool condition,string name) {assertions++;if(!condition)throw new Exception("Adventure assertion: "+name);}
    private static void Throws<T>(Action action,string name) where T:Exception {bool threw=false;try{action();}catch(T){threw=true;}Check(threw,name);}
    private static object Invoke(GameSimulation game,string name,params object[] args)
    {return typeof(GameSimulation).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);}
    private static GameSimulation Fixture(int level,params PlantKind[] wanted)
    {
        var g=new GameSimulation(new GameRules{AdventureLevel=level},7529);
        if(g.Adventure.Phase==AdventurePhase.ChoosingSeeds)
        {var deck=wanted.ToList();foreach(var k in g.Adventure.Level.UnlockedSeeds)if(deck.Count<6&&!deck.Contains(k))deck.Add(k);Check(g.ChooseAdventureSeeds(deck),"fixture chooses legal earned deck");}
        if(level==5) for(int c=3;c<=5;c++)g.RemovePlant(2,c);
        g.Adventure.CurrentWave=g.Adventure.Waves.Count;g.Adventure.FinalAmbush=false;
        g.Adventure.NextSkySun=float.MaxValue;
        return g;
    }
    private static void CreditFixture(GameSimulation g)
    {var s=new Sun{Value=9990};g.Suns.Add(s);Check(g.CollectSun(s),"explicit unit fixture sun credit");}
    private static Zombie SpawnFixture(GameSimulation g,ZombieKind kind,int row,float x)
    {return (Zombie)Invoke(g,"SpawnAdventureZombie",kind,row,x,0);}
    public static int Run(string workspace)
    {
        assertions=0;NativeTables(workspace);SelectionAndTutorial();TerrainAndCombat();Specials();Progression(workspace);
        Console.WriteLine("PASS: "+assertions+" adventure behavior/persistence/native-data assertions (unit fixtures isolated from public-action playthrough).");return assertions;
    }
    private static int Offset(byte[] pe,int va)
    {
        int header=BitConverter.ToInt32(pe,0x3c),sections=BitConverter.ToUInt16(pe,header+6),optional=BitConverter.ToUInt16(pe,header+20),start=header+24+optional;
        int rva=va-0x400000;
        for(int i=0;i<sections;i++){int p=start+i*40,virtualAddress=BitConverter.ToInt32(pe,p+12),size=Math.Max(BitConverter.ToInt32(pe,p+8),BitConverter.ToInt32(pe,p+16));if(rva>=virtualAddress&&rva<virtualAddress+size)return BitConverter.ToInt32(pe,p+20)+rva-virtualAddress;}
        throw new Exception("VA not mapped.");
    }
    private static void NativeTables(string workspace)
    {
        byte[] pe=File.ReadAllBytes(Path.Combine(Directory.GetParent(workspace).FullName,"PlantsVsZombies.exe"));
        using(var hash=SHA256.Create())Check(BitConverter.ToString(hash.ComputeHash(pe)).Replace("-","")=="E1511201EC2AE9A7E30259F04BC2EC7272BADB2574A2E9B405D0E916AE5C082C","local executable identity, opened read-only");
        for(int n=1;n<=50;n++)
        {
            var spec=AdventureLevels.Get(n);Check(spec.BaseWaveCount==BitConverter.ToInt32(pe,Offset(pe,0x74EDD8)+(n-1)*4),"native base wave count "+n);
            Check(spec.WaveCount==(n==15?8:spec.BaseWaveCount),"runtime wave override "+n);
            var plan=AdventureWavePlan.Build(spec,new Random(n));Check(plan.Count==spec.WaveCount,"complete playable wave plan "+n);
            for(int z=0;z<34;z++)Check(PvZReplica.VerifiedData.VerifiedDataCatalog.AdventureLevels[n-1].ZombieFlagsRaw[z]==BitConverter.ToInt32(pe,Offset(pe,0x74EEA0)+z*204+n*4),"native row eligibility "+n+":"+z);
            Check(spec.DefaultSeeds.All(k=>spec.UnlockedSeeds.Contains(k)),"no default unearned seed "+n);
            Check(spec.UnlockedSeeds.Distinct().Count()==spec.UnlockedSeeds.Count,"unlocked cards have no duplicate awards "+n);
            Check(plan.All(w=>w.Zombies.Count>0 && w.Zombies.All(z=>(int)z<=24)),"executable zombie behavior for every planned spawn "+n);
        }
        Check(AdventureLevels.Get(5).Mode==AdventureMode.Bowling && AdventureLevels.Get(15).Mode==AdventureMode.WhackAZombie && AdventureLevels.Get(25).Mode==AdventureMode.LittleTrouble && AdventureLevels.Get(35).Mode==AdventureMode.Vasebreaker && AdventureLevels.Get(45).Mode==AdventureMode.Bungee,"special modes placed in actual adventure slots");
        Check(AdventureLevels.Get(10).RewardSeed==PlantKind.PuffShroom && AdventureLevels.Get(20).RewardSeed==PlantKind.LilyPad && AdventureLevels.Get(30).RewardSeed==PlantKind.SeaShroom && AdventureLevels.Get(40).RewardSeed==PlantKind.CabbagePult,"stage reward sequence");
        Check(AdventureLevels.Get(4).RewardKind==AdventureRewardKind.Shovel && AdventureLevels.Get(5).RewardSeed==PlantKind.PotatoMine && AdventureLevels.Get(9).RewardKind==AdventureRewardKind.Note && !AdventureLevels.Get(10).UnlockedSeeds.Contains(PlantKind.PuffShroom),"tools/notes do not unlock a seed before its actual reward");
        var bungee=AdventureWavePlan.Build(AdventureLevels.Get(45),new Random(8));
        Check(bungee[9].Zombies.Count==5 && bungee[9].Zombies.All(k=>k==ZombieKind.Bungee),"native first bungee flag skips ordinary filling");
    }
    private static void SelectionAndTutorial()
    {
        var one=new GameSimulation(new GameRules{AdventureLevel=1});
        Check(one.SunAmount==150 && one.RowCount==5 && one.Adventure.SelectedSeeds.SequenceEqual(new[]{PlantKind.Peashooter}),"1-1 first play rules");
        Check(!one.TryPlant(PlantKind.Peashooter,2,0),"1-1 needs seed-pick tutorial action");
        Check(one.SelectAdventureSeed(PlantKind.Peashooter),"select tutorial seed");
        Check(!one.TryPlant(PlantKind.Peashooter,1,0),"unsodded row rejects planting");
        Check(one.TryPlant(PlantKind.Peashooter,2,0),"single grass lane accepts pea");
        one.Update(17.9f);Check(one.SpawnedZombies==0,"first wave cannot use previous-wave early trigger");one.Update(0.2f);Check(one.SpawnedZombies>0,"actual first wave starts");
        var two=new GameSimulation(new GameRules{AdventureLevel=2});Check(!two.TryPlant(PlantKind.Peashooter,2,0),"sunflower tutorial requires sunflower first");
        CreditFixture(two);Check(two.TryPlant(PlantKind.Sunflower,1,0),"first tutorial sunflower");two.Update(8f);Check(two.TryPlant(PlantKind.Sunflower,3,0),"second tutorial sunflower after recharge");Check(two.Adventure.Tutorial==AdventureTutorial.Complete,"tutorial completed by actions");
        var five=new GameSimulation(new GameRules{AdventureLevel=5});Check(five.Plants.Count(p=>p.Kind==PlantKind.Peashooter)==3,"bowling opens with shovel tutorial peas");five.Update(30f);Check(five.SpawnedZombies==0 && five.Elapsed==0,"shovel tutorial blocks opening wave and belt");for(int c=3;c<=5;c++)Check(five.RemovePlant(2,c),"shovel accepts tutorial pea");Check(five.Adventure.Tutorial==AdventureTutorial.Complete,"shovel completes after all three removals");
        var selector=new GameSimulation(new GameRules{AdventureLevel=8});selector.Update(60);Check(selector.Elapsed==0&&selector.SpawnedZombies==0,"seed chooser suspends world");
        Check(!selector.ChooseAdventureSeeds(new[]{PlantKind.Peashooter,PlantKind.Peashooter}),"duplicate deck rejected");
        Check(!selector.ChooseAdventureSeeds(new[]{PlantKind.MelonPult,PlantKind.Peashooter,PlantKind.Sunflower,PlantKind.Wallnut,PlantKind.SnowPea,PlantKind.PotatoMine}),"unearned deck rejected");
        Check(selector.ChooseDefaultAdventureSeeds(),"default earned six-card deck");selector.TogglePause();selector.Update(5);Check(selector.Elapsed==0,"pause suspends adventure");selector.TogglePause();selector.Update(0.1f);Check(selector.Elapsed>0,"resume restarts simulation");
        Throws<ArgumentException>(()=>new GameSimulation(new GameRules{AdventureLevel=1,EnableExperimentalLevelOne=true}),"experimental and campaign modes cannot mix");
    }
    private static void TerrainAndCombat()
    {
        var pool=Fixture(29,PlantKind.LilyPad,PlantKind.Peashooter,PlantKind.CherryBomb,PlantKind.TangleKelp,PlantKind.SnowPea,PlantKind.Sunflower);CreditFixture(pool);
        Check(pool.RowCount==6 && pool.Mowers.Count==6,"pool has six live rows");
        Check(!pool.TryPlant(PlantKind.Peashooter,2,2)&&pool.LastAdventurePlantingFailure==AdventurePlantingFailure.NeedsLilyPad,"water needs support");
        Check(pool.TryPlant(PlantKind.LilyPad,2,2)&&pool.TryPlant(PlantKind.Peashooter,2,2),"lily and shooter share one cell");
        // Pumpkin is earned by 4-8, so use an explicit independent late-game fixture for armor checks below.
        Check(!pool.TryPlant(PlantKind.TangleKelp,0,2),"kelp requires water");
        Check(pool.RemovePlant(2,2)&&pool.AdventurePlantAt(2,2,PlantLayer.Support)!=null,"shovel removes main before support");Check(pool.RemovePlant(2,2)&&pool.PlantAt(2,2)==null,"support can then be removed");
        var roof=Fixture(49,PlantKind.FlowerPot,PlantKind.Peashooter,PlantKind.CabbagePult,PlantKind.Pumpkin,PlantKind.CoffeeBean,PlantKind.HypnoShroom);CreditFixture(roof);
        Check(roof.Plants.Count==15&&roof.Mowers.All(m=>!m.Available),"roof pre-pots and unpurchased cleaners");
        Check(!roof.TryPlant(PlantKind.CabbagePult,1,5)&&roof.LastAdventurePlantingFailure==AdventurePlantingFailure.NeedsFlowerPot,"roof needs a pot");
        Check(roof.TryPlant(PlantKind.FlowerPot,1,5)&&roof.TryPlant(PlantKind.CabbagePult,1,5),"pot and lobber stack");
        Check(roof.TryPlant(PlantKind.Pumpkin,1,5)&&roof.Plants.Count(p=>p.Row==1&&p.Column==5)==3,"armor is a third cell layer");
        Check(roof.TryPlant(PlantKind.HypnoShroom,2,1)&&roof.AdventurePlantAt(2,1,PlantLayer.Main).Asleep,"day mushroom sleeps");
        Check(roof.TryPlant(PlantKind.CoffeeBean,2,1),"coffee targets sleeping mushroom");roof.Update(2.1f);Check(!roof.AdventurePlantAt(2,1,PlantLayer.Main).Asleep,"coffee wakes after delay");
        var combat=Fixture(19,PlantKind.FumeShroom,PlantKind.GraveBuster,PlantKind.PuffShroom,PlantKind.IceShroom,PlantKind.HypnoShroom,PlantKind.SunShroom);CreditFixture(combat);
        Check(combat.TryPlant(PlantKind.FumeShroom,2,2),"night fume active");var door=SpawnFixture(combat,ZombieKind.ScreenDoor,2,5f);combat.Update(1f);Check(door.ShieldHealth==1100&&door.Health<270,"fume passes screen shield");
        var basic=Fixture(9,PlantKind.Peashooter,PlantKind.Sunflower,PlantKind.PotatoMine,PlantKind.SnowPea,PlantKind.CherryBomb,PlantKind.Wallnut);CreditFixture(basic);
        var normal=SpawnFixture(basic,ZombieKind.Normal,2,4f);Invoke(basic,"HitAdventureZombie",normal,200f,false);Check(normal.Headless,"body threshold causes real decapitation");float health=normal.Health;basic.Update(1f);Check(normal.Health<health||!basic.Zombies.Contains(normal),"headless zombie bleeds");
        var diver=SpawnFixture(pool,ZombieKind.Snorkel,3,5);Check(diver.Submerged,"snorkel has submerged state");
        var terrain=Fixture(39,PlantKind.LilyPad,PlantKind.Plantern,PlantKind.Blover,PlantKind.Cactus,PlantKind.PuffShroom,PlantKind.SunShroom);CreditFixture(terrain);
        Check(!terrain.IsAdventureCellVisible(1,7),"fog obscures right side");Check(terrain.TryPlant(PlantKind.Plantern,1,5)&&terrain.IsAdventureCellVisible(1,7),"plantern clears nearby fog");
        var balloon=SpawnFixture(terrain,ZombieKind.Balloon,0,7);Check(balloon.Airborne,"balloon flies");Check(terrain.TryPlant(PlantKind.Blover,0,1),"blover plants");terrain.Update(1);Check(!terrain.Zombies.Contains(balloon)&&terrain.Adventure.FogBlownRemaining>0,"blover removes air enemies and clears fog");
        var magnetic=Fixture(49,PlantKind.MagnetShroom,PlantKind.CoffeeBean);CreditFixture(magnetic);Check(magnetic.TryPlant(PlantKind.MagnetShroom,1,1)&&magnetic.TryPlant(PlantKind.CoffeeBean,1,1),"day magnet needs real coffee");var plastic=SpawnFixture(magnetic,ZombieKind.Conehead,1,3);var bucket=SpawnFixture(magnetic,ZombieKind.Buckethead,1,4);magnetic.Update(2.6f);Check(plastic.ArmorHealth==370&&bucket.ArmorHealth==0,"magnet attracts metal and leaves plastic cone");
        var protection=Fixture(49,PlantKind.UmbrellaLeaf);CreditFixture(protection);Check(protection.TryPlant(PlantKind.UmbrellaLeaf,1,1),"umbrella on prepot");int before=protection.SpawnedZombies;Invoke(protection,"DropAdventureZombie",ZombieKind.Normal,1,1,0);Check(protection.SpawnedZombies==before,"umbrella rejects nearby bungee payload");
    }
    private static void Specials()
    {
        var bowling=Fixture(5);var packet=bowling.Adventure.Packets[0];Check(!bowling.TryUseAdventurePacket(packet.Id,2,3),"bowling placement line enforced");Check(bowling.TryUseAdventurePacket(packet.Id,2,2),"bowling emits moving nut");Check(!bowling.TryUseAdventurePacket(packet.Id,2,2),"packet cannot be spent twice");var victim=SpawnFixture(bowling,ZombieKind.Normal,2,4);bowling.Update(1);Check(!bowling.Zombies.Contains(victim),"bowling collision kills actual zombie");
        var hammer=Fixture(15);var cone=SpawnFixture(hammer,ZombieKind.Conehead,2,5);hammer.Update(0.5f);Check(hammer.HammerAdventureZombie(cone.Id),"hammer accepts live emerged zombie");
        var vase=Fixture(35);Check(vase.Adventure.Vases.Count==15,"first vase stage has actual fifteen objects");var shown=vase.Adventure.Vases.First(v=>v.ShowsPlant);Check(vase.SmashAdventureVase(shown.Row,shown.Column)&&vase.Adventure.Packets.Count==1,"green vase creates usable seed packet");Check(!vase.SmashAdventureVase(shown.Row,shown.Column),"vase cannot be smashed twice");
        var boss=Fixture(50);Check(boss.Adventure.BossHealth==40000&&boss.Adventure.Packets.Count==4,"boss health and scripted opening packets");boss.Adventure.BossHeadVulnerable=true;Invoke(boss,"DamageAdventureBoss",1800f);Check(boss.Adventure.BossHealth==38200,"exposed boss takes real damage");boss.Adventure.BossHeadVulnerable=false;Invoke(boss,"DamageAdventureBoss",1800f);Check(boss.Adventure.BossHealth==38200,"hidden boss rejects damage");
        var a=new GameSimulation(new GameRules{AdventureLevel=5},96);var b=new GameSimulation(new GameRules{AdventureLevel=5},96);
        for(int c=3;c<=5;c++){a.RemovePlant(2,c);b.RemovePlant(2,c);}
        for(int i=0;i<100;i++)a.Update(0.1f);for(int i=0;i<1000;i++)b.Update(0.01f);
        Check(a.Elapsed>9.9f && a.Zombies.Count>0,"determinism fixture actually advances live enemies");
        Check(a.Adventure.CurrentWave==b.Adventure.CurrentWave&&a.Zombies.Count==b.Zombies.Count&&a.Adventure.Packets.Select(p=>p.Kind).SequenceEqual(b.Adventure.Packets.Select(p=>p.Kind)),"fixed-step grouping preserves RNG and wave schedule");
        Check(a.Zombies.Zip(b.Zombies,(x,y)=>Math.Abs(x.X-y.X)<0.00001f&&x.Kind==y.Kind&&x.Row==y.Row).All(v=>v),"fixed-step grouping preserves moving entities");
    }
    private static void Progression(string workspace)
    {
        string dir=Path.Combine(workspace,"tests","AdventureTests","artifacts",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        string old=Path.Combine(dir,"profiles.dat"),path=Path.Combine(dir,"campaign.adventure");
        var catalog=new ProfileCatalog(old);var profile=catalog.Create("冒险测试");byte[] oldBytes=File.ReadAllBytes(old);
        var store=new AdventureProgressStore(path);var campaign=new AdventureCampaign(store,profile.Id);var game=campaign.BeginOrResume();var bot=new PlaythroughBot(game);
        Check(!campaign.CollectReward()&&!campaign.StartNextLevel(),"running match cannot claim or skip reward");
        for(int i=0;i<3000&&game.State==MatchState.Playing;i++){bot.Act();game.Update(0.1f);}
        Check(game.State==MatchState.Won&&game.Adventure.Phase==AdventurePhase.Reward,"first real fight drops reward");Check(store.Get(profile.Id).NextLevel==1,"win alone does not unlock next level");Check(campaign.CollectReward(),"pickup commits reward");Check(!campaign.CollectReward(),"duplicate reward rejected");Check(store.Get(profile.Id).NextLevel==2,"pickup unlocks exactly next level");
        var restored=new AdventureProgressStore(path);Check(restored.Get(profile.Id).NextLevel==2&&restored.Get(profile.Id).CollectedRewards==1,"disk reload restores checkpoint");Check(File.ReadAllBytes(old).SequenceEqual(oldBytes),"old profile bytes are untouched");
        Check(campaign.StartNextLevel()&&campaign.Game.Adventure.Level.Number==2,"continue constructs the actual next board");
        store.SaveAttempt(profile.Id,8787,new PlantKind[0]);Check(Directory.GetFiles(dir,"campaign.adventure.backup-*").Length>=1,"replacing progress preserves previous version");
        Throws<IOException>(()=>restored.SaveAttempt(profile.Id,9999,new PlantKind[0]),"stale writer cannot overwrite newer progress");
        byte[] saved=File.ReadAllBytes(path);saved[saved.Length-1]^=1;string corrupt=Path.Combine(dir,"corrupt.adventure");File.WriteAllBytes(corrupt,saved);Throws<InvalidDataException>(()=>new AdventureProgressStore(corrupt),"corrupt checkpoint rejected");Check(File.ReadAllBytes(corrupt).SequenceEqual(saved),"corrupt bytes preserved for recovery");
        Throws<ArgumentException>(()=>new AdventureProgressStore(old),"cannot target original/catalog dat path");Check(new AdventureProgressStore(path).Get(Guid.NewGuid()).NextLevel==1,"different GUID gets independent new progress");
        string deckPath=Path.Combine(dir,"deck.adventure");var decks=new AdventureProgressStore(deckPath);Guid id=Guid.NewGuid();for(int n=1;n<8;n++)decks.RecordReward(id,n);var staleDecks=new AdventureProgressStore(deckPath);var staleCampaign=new AdventureCampaign(staleDecks,id);staleCampaign.BeginOrResume();
        var deckCampaign=new AdventureCampaign(decks,id);deckCampaign.BeginOrResume();Check(deckCampaign.ChooseSeeds(AdventureLevels.Get(8).DefaultSeeds),"campaign saves accepted loadout");var resume=new AdventureCampaign(new AdventureProgressStore(deckPath),id);resume.BeginOrResume();Check(resume.Game.Adventure.Phase==AdventurePhase.Playing&&resume.Game.Adventure.SelectedSeeds.SequenceEqual(AdventureLevels.Get(8).DefaultSeeds),"resume restarts current level with saved deck");
        var staleSelection=staleCampaign.Game.Adventure.SelectedSeeds;
        var staleSelectionValues=staleSelection.ToArray();
        Throws<IOException>(()=>staleCampaign.ChooseSeeds(AdventureLevels.Get(8).DefaultSeeds),"failed deck persistence rejects stale writer");
        Check(staleCampaign.Game.Adventure.Phase==AdventurePhase.ChoosingSeeds,"failed deck save leaves chooser unchanged");
        Check(object.ReferenceEquals(staleSelection,staleCampaign.Game.Adventure.SelectedSeeds) && staleSelectionValues.SequenceEqual(staleCampaign.Game.Adventure.SelectedSeeds),"failed deck save leaves selection snapshot and values unchanged");
        string lockedPath=Path.Combine(dir,"locked.adventure");var lockedStore=new AdventureProgressStore(lockedPath);Guid lockedId=Guid.NewGuid();for(int n=1;n<8;n++)lockedStore.RecordReward(lockedId,n);
        var lockedCampaign=new AdventureCampaign(lockedStore,lockedId);lockedCampaign.BeginOrResume();var selectionBefore=lockedCampaign.Game.Adventure.SelectedSeeds;
        using(var heldLock=new FileStream(lockedPath+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
        {
            Throws<IOException>(()=>lockedCampaign.ChooseSeeds(AdventureLevels.Get(8).DefaultSeeds),"locked progress file rejects selection commit");
            Check(lockedCampaign.Game.Adventure.Phase==AdventurePhase.ChoosingSeeds && object.ReferenceEquals(selectionBefore,lockedCampaign.Game.Adventure.SelectedSeeds),"IO failure preserves phase and selected deck");
            Check(lockedCampaign.Game.Elapsed==0f && lockedStore.Get(lockedId).SelectedSeeds.Count==0,"IO failure publishes neither world time nor saved deck");
        }
        Check(lockedCampaign.ChooseSeeds(AdventureLevels.Get(8).DefaultSeeds) && lockedCampaign.Game.Adventure.Phase==AdventurePhase.Playing,"same chooser retries successfully after IO lock releases");
        Check(new AdventureProgressStore(lockedPath).Get(lockedId).SelectedSeeds.SequenceEqual(AdventureLevels.Get(8).DefaultSeeds),"retry persists deck before entering play");
        Throws<ArgumentException>(()=>decks.SaveAttempt(id,2,new[]{PlantKind.Peashooter}),"partial chosen deck cannot become a checkpoint");
        byte[] inconsistent=File.ReadAllBytes(path);Array.Copy(BitConverter.GetBytes(1),0,inconsistent,32,4);Array.Copy(BitConverter.GetBytes((1UL<<50)-1),0,inconsistent,36,8);
        using(var hash=SHA256.Create()){var body=inconsistent.Take(inconsistent.Length-32).ToArray();Array.Copy(hash.ComputeHash(body),0,inconsistent,inconsistent.Length-32,32);}
        string inconsistentPath=Path.Combine(dir,"inconsistent.adventure");File.WriteAllBytes(inconsistentPath,inconsistent);Throws<InvalidDataException>(()=>new AdventureProgressStore(inconsistentPath),"completed checkpoint must point to level fifty");
    }
}
