#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Adventure;

internal static class UpgradePlantTests
{
    private static int assertions;
    private static void Check(bool value,string name) {assertions++;if(!value)throw new Exception("Upgrade assertion: "+name);}
    private static void Throws<T>(Action act,string name) where T:Exception {bool caught=false;try{act();}catch(T){caught=true;}Check(caught,name);}
    private static GameSimulation Fixture(int level,PlantKind? target,params PlantKind[] wanted)
    {
        var g=new GameSimulation(new GameRules{AdventureLevel=level,AdventureSeedAccess=AdventureSeedAccess.DevelopmentAll(target)},3967);
        var deck=wanted.ToList();foreach(var k in g.Adventure.AvailableSeeds)if(deck.Count<6 && !deck.Contains(k))deck.Add(k);
        Check(g.ChooseAdventureSeeds(deck),"fixture selects six earned/explicitly granted seeds");
        g.Adventure.CurrentWave=g.Adventure.Waves.Count;g.Adventure._finalAmbush=false;g.Adventure._nextSkySun=float.MaxValue;
        // Explicit isolated fixture funding is independent of native collectible sun denominations.
        typeof(GameSimulation).GetProperty(nameof(GameSimulation.SunAmount))!.SetValue(g, 9990);
        Check(g.SunAmount == 9990, "isolated unit fixture credit");
        return g;
    }
    private static Zombie Spawn(GameSimulation g,ZombieKind kind,int row,float x)
    {
        var method=typeof(GameSimulation).GetMethod("SpawnAdventureZombie",BindingFlags.Instance|BindingFlags.NonPublic);
        var z=(Zombie)method.Invoke(g,new object[]{kind,row,x,0});z.Speed=0;return z;
    }
    public static int Run(string root)
    {
        assertions=0;AccessAndNativeData(root);UpgradeBoundaries();GatlingAndTwin();GloomAndCattail();WinterAndGold();Spikerock();Cannon();Imitater();ProjectileMetadata();
        Console.WriteLine("PASS: "+assertions+" upgrade/Imitater behavior assertions; test fixtures are separate from the public-action 50-level campaign.");return assertions;
    }
    private static void AccessAndNativeData(string root)
    {
        Check((int)PlantKind.GatlingPea==40&&(int)PlantKind.TwinSunflower==41&&(int)PlantKind.GloomShroom==42&&(int)PlantKind.Cattail==43&&
            (int)PlantKind.WinterMelon==44&&(int)PlantKind.GoldMagnet==45&&(int)PlantKind.Spikerock==46&&(int)PlantKind.CobCannon==47&&(int)PlantKind.Imitater==48,"original contiguous IDs 40..48");
        int[] costs={250,150,150,225,200,50,125,500};
        for(int i=0;i<8;i++){Check(GameSimulation.Cost((PlantKind)(40+i))==costs[i],"native upgrade base cost "+i);Check(Math.Abs(GameSimulation.RechargeTime((PlantKind)(40+i))-50.01f)<0.0001f,"native upgrade recharge threshold "+i);}
        var normal=new GameSimulation(new GameRules{AdventureLevel=49});Check(normal.Adventure.AvailableSeeds.SequenceEqual(normal.Adventure.Level.UnlockedSeeds),"default adventure access is unchanged");
        Check(!normal.Adventure.AvailableSeeds.Contains(PlantKind.GatlingPea)&&!normal.Adventure.AvailableSeeds.Contains(PlantKind.Imitater),"no implicit purchases");
        var granted=new List<PlantKind>{PlantKind.GatlingPea};var access=new AdventureSeedAccess(granted);granted.Clear();
        var g=new GameSimulation(new GameRules{AdventureLevel=49,AdventureSeedAccess=access});Check(g.Adventure.AvailableSeeds.Contains(PlantKind.GatlingPea)&&!g.Adventure.AvailableSeeds.Contains(PlantKind.Imitater),"captured explicit access is immutable and limited");
        var previousDeck=g.Adventure.SelectedSeeds;
        Check(g.CanChooseAdventureSeeds(new[]{PlantKind.GatlingPea,PlantKind.Repeater,PlantKind.Peashooter,PlantKind.Sunflower,PlantKind.Wallnut,PlantKind.PotatoMine}) && g.Adventure.Phase==AdventurePhase.ChoosingSeeds && object.ReferenceEquals(previousDeck,g.Adventure.SelectedSeeds) && g.Elapsed==0,"pure validation publishes no phase, selection or time");
        Check(!g.ChooseAdventureSeeds(new[]{PlantKind.Imitater,PlantKind.Peashooter,PlantKind.Sunflower,PlantKind.Wallnut,PlantKind.PotatoMine,PlantKind.Repeater}),"ungranted upgrade cannot enter the deck");
        Throws<ArgumentException>(()=>new AdventureSeedAccess(new[]{PlantKind.GatlingPea,PlantKind.GatlingPea}),"duplicate access rejected");
        Throws<ArgumentException>(()=>new AdventureSeedAccess(new[]{PlantKind.Peashooter}),"access cannot grant future basic seeds");
        Throws<ArgumentException>(()=>AdventureSeedAccess.DevelopmentAll(PlantKind.GatlingPea),"Imitater cannot imitate an upgrade");
        var mimic=new GameSimulation(new GameRules{AdventureLevel=49,AdventureSeedAccess=AdventureSeedAccess.DevelopmentAll()});
        Check(!mimic.ChooseAdventureSeeds(new[]{PlantKind.Imitater,PlantKind.Peashooter,PlantKind.Sunflower,PlantKind.Wallnut,PlantKind.PotatoMine,PlantKind.Repeater}),"Imitater requires choosing a target");
        Check(!mimic.SetAdventureImitaterTarget(PlantKind.Imitater)&&mimic.SetAdventureImitaterTarget(PlantKind.CherryBomb),"only basic target selection accepted");
        Check(mimic.AdventureSeedCost(PlantKind.Imitater)==150&&Math.Abs(mimic.AdventureSeedRechargeTime(PlantKind.Imitater)-50.01f)<0.0001f,"Imitater follows target cost and recharge");
        var low=new GameSimulation(new GameRules{AdventureLevel=39,AdventureSeedAccess=AdventureSeedAccess.DevelopmentAll()});Check(!low.SetAdventureImitaterTarget(PlantKind.MelonPult),"unearned basic target cannot bypass adventure progress");
        // Local PE offsets/rows are independently checked by AdventureUnitTests. Check these extra rows against the same mapped section.
        byte[] pe=File.ReadAllBytes(Path.Combine(Directory.GetParent(root).FullName,"PlantsVsZombies.exe"));
        int h=BitConverter.ToInt32(pe,0x3c),n=BitConverter.ToUInt16(pe,h+6),op=BitConverter.ToUInt16(pe,h+20),table=-1;
        for(int i=0;i<n;i++){int p=h+24+op+i*40,rva=BitConverter.ToInt32(pe,p+12),size=Math.Max(BitConverter.ToInt32(pe,p+8),BitConverter.ToInt32(pe,p+16));if(0x34aa00>=rva&&0x34aa00<rva+size)table=BitConverter.ToInt32(pe,p+20)+0x34aa00-rva;}
        Check(table>=0,"native plant table mapped");
        for(int id=40;id<=48;id++){Check(BitConverter.ToInt32(pe,table+id*36)==id,"native upgrade row ID "+id);Check(GameSimulation.Cost((PlantKind)id)==BitConverter.ToInt32(pe,table+id*36+16),"native raw table price "+id);}
        int[] addresses={0x46C604,0x46C88B,0x46D3BD,0x46D44B,0x473196,0x475252,0x476076,0x47646B};
        string[] hex={"C74354C8000000","C74354F4010000","C7869000000064000000","C78690000000C8000000","C74754B80B0000","C78690000000CE000000","8B049510AA7400","8B048514AA7400"};
        for(int i=0;i<addresses.Length;i++){byte[] expected=Enumerable.Range(0,hex[i].Length/2).Select(j=>Convert.ToByte(hex[i].Substring(j*2,2),16)).ToArray();Check(pe.Skip(PeOffset(pe,addresses[i])).Take(expected.Length).SequenceEqual(expected),"native counter/proxy instruction bytes "+addresses[i].ToString("X"));}

    }
    private static int PeOffset(byte[] pe,int va)
    {
        int h=BitConverter.ToInt32(pe,0x3c),n=BitConverter.ToUInt16(pe,h+6),op=BitConverter.ToUInt16(pe,h+20),rva=va-0x400000;
        for(int i=0;i<n;i++){int p=h+24+op+i*40,start=BitConverter.ToInt32(pe,p+12),size=Math.Max(BitConverter.ToInt32(pe,p+8),BitConverter.ToInt32(pe,p+16));if(rva>=start&&rva<start+size)return BitConverter.ToInt32(pe,p+20)+rva-start;}
        throw new Exception("Native VA not mapped.");
    }
    private static void UpgradeBoundaries()
    {
        foreach(var kind in new[]{PlantKind.GatlingPea,PlantKind.TwinSunflower,PlantKind.GloomShroom,PlantKind.Cattail,PlantKind.WinterMelon,PlantKind.GoldMagnet,PlantKind.Spikerock,PlantKind.CobCannon})
        {
            var g=Fixture(49,null,kind,PlantKind.Peashooter);Check(g.TryPlant(PlantKind.Peashooter,1,1),"wrong-prerequisite fixture "+kind);int sun=g.SunAmount;
            Check(!g.TryPlant(kind,1,1)&&g.SunAmount==sun&&g.CooldownRemaining(kind)==0&&g.AdventurePlantAt(1,1,PlantLayer.Main).Kind==PlantKind.Peashooter,"wrong plant cannot be upgraded or charged "+kind);
        }
        var edge=Fixture(49,null,PlantKind.CobCannon,PlantKind.KernelPult,PlantKind.FlowerPot);Check(edge.TryPlant(PlantKind.FlowerPot,1,8)&&edge.TryPlant(PlantKind.KernelPult,1,8),"edge kernel prerequisite on real pot");int cash=edge.SunAmount;Check(!edge.TryPlant(PlantKind.CobCannon,1,8)&&edge.LastAdventurePlantingFailure==AdventurePlantingFailure.CannonFootprint&&edge.SunAmount==cash,"cannon cannot overhang right edge");
        var poor=new GameSimulation(new GameRules{AdventureLevel=49,AdventureSeedAccess=AdventureSeedAccess.DevelopmentAll(PlantKind.CherryBomb)});Check(poor.ChooseAdventureSeeds(new[]{PlantKind.Imitater,PlantKind.CherryBomb,PlantKind.Peashooter,PlantKind.Sunflower,PlantKind.Wallnut,PlantKind.PotatoMine}),"poor Imitater fixture has legitimate copied card");Check(!poor.TryPlant(PlantKind.Imitater,1,1)&&poor.LastAdventurePlantingFailure==AdventurePlantingFailure.Sun&&poor.SunAmount==50&&poor.CooldownRemaining(PlantKind.Imitater)==0,"raw zero-price Imitater cannot bypass target's sun price");
        var eaten=Fixture(39,PlantKind.CherryBomb,PlantKind.Imitater);eaten.TryPlant(PlantKind.Imitater,1,2);Spawn(eaten,ZombieKind.Normal,1,2.5f);eaten.Update(3.3f);Check(eaten.AdventurePlantAt(1,2,PlantLayer.Main)==null,"Imitater can be eaten before morph and never grants free target");
        var copiedBase=Fixture(39,PlantKind.Repeater,PlantKind.Imitater,PlantKind.GatlingPea);copiedBase.TryPlant(PlantKind.Imitater,1,2);copiedBase.Update(3.3f);Check(copiedBase.TryPlant(PlantKind.GatlingPea,1,2)&&copiedBase.PlantAt(1,2).Kind==PlantKind.GatlingPea&&!copiedBase.PlantAt(1,2).IsImitated,"transformed basic copy is a valid upgrade prerequisite");
    }
    private static void GatlingAndTwin()
    {
        var g=Fixture(39,null,PlantKind.Repeater,PlantKind.GatlingPea);int before=g.SunAmount;
        Check(!g.TryPlant(PlantKind.GatlingPea,0,2)&&g.LastAdventurePlantingFailure==AdventurePlantingFailure.NeedsUpgrade,"Gatling requires Repeater");Check(g.SunAmount==before&&g.CooldownRemaining(PlantKind.GatlingPea)==0,"failed upgrade spends neither sun nor cooldown");
        Check(g.TryPlant(PlantKind.Repeater,0,2),"plant prerequisite Repeater");before=g.SunAmount;Check(g.TryPlant(PlantKind.GatlingPea,0,2),"replace Repeater with Gatling");
        Check(g.SunAmount==before-250&&g.Plants.Count(p=>p.Row==0&&p.Column==2)==1&&g.PlantAt(0,2).Kind==PlantKind.GatlingPea,"upgrade pays extra price and consumes prerequisite once");
        var target = Spawn(g, ZombieKind.Buckethead, 0, 9f);
        Check(AdventureZombieTargetingRules.CanBeAutoTargeted(target), "burst fixture starts with a visible zombie body instead of native right-edge birth");
        g.Update(1.4f);
        Check(g.Peas.Count==4&&g.Peas.All(p=>p.Damage==20&&p.SourceKind==PlantKind.GatlingPea&&!p.SourceIsImitated),"four distinct Gatling burst projectiles against a visible target");
        Check(g.CooldownRemaining(PlantKind.GatlingPea)>48f&&g.CooldownRemaining(PlantKind.Repeater)>6f,"base and upgrade packet cooldowns are independent");
        var twin=Fixture(39,null,PlantKind.Sunflower,PlantKind.TwinSunflower);Check(twin.TryPlant(PlantKind.Sunflower,0,1)&&twin.TryPlant(PlantKind.TwinSunflower,0,1),"Twin consumes Sunflower");twin.Update(8f);
        Check(twin.Suns.Count==2&&twin.Suns.Sum(s=>s.Value)==50,"Twin creates two collectible normal suns");int cash=twin.SunAmount;foreach(var s in twin.Suns.ToArray())Check(twin.CollectSun(s),"Twin sun can be collected");
        twin.Update(1f);
        Check(twin.SunAmount==Math.Min(9990,cash+50),"double sun enters normal economy after the original collection flight");
    }
    private static void GloomAndCattail()
    {
        var day=Fixture(49,null,PlantKind.FumeShroom,PlantKind.GloomShroom,PlantKind.CoffeeBean);Check(day.TryPlant(PlantKind.FumeShroom,2,1)&&day.TryPlant(PlantKind.GloomShroom,2,1),"Gloom consumes Fume");Plant gloom=day.AdventurePlantAt(2,1,PlantLayer.Main);Check(gloom.Asleep,"day upgrade preserves mushroom sleep");var door=Spawn(day,ZombieKind.ScreenDoor,2,2.5f);var rear=Spawn(day,ZombieKind.Buckethead,1,0.5f);float armor=rear.ArmorHealth;day.Update(2f);Check(door.Health==270&&rear.ArmorHealth==armor,"sleeping Gloom does not attack");
        Check(day.TryPlant(PlantKind.CoffeeBean,2,1),"coffee wakes Gloom");day.Update(4.5f);Check(!gloom.Asleep&&gloom.ActionCount==4&&door.Health==190&&door.ShieldHealth==1100&&rear.ArmorHealth==armor-80,"four radial Gloom pulses hit behind/adjacent and bypass screen shield");
        var awake=Fixture(49,null,PlantKind.FumeShroom,PlantKind.GloomShroom,PlantKind.CoffeeBean);awake.TryPlant(PlantKind.FumeShroom,1,1);awake.TryPlant(PlantKind.CoffeeBean,1,1);awake.Update(2.1f);Check(awake.TryPlant(PlantKind.GloomShroom,1,1)&&!awake.PlantAt(1,1).Asleep&&awake.PlantAt(1,1).AwakeByCoffee,"awake Fume upgrade keeps coffee effect");
        var cat=Fixture(39,null,PlantKind.LilyPad,PlantKind.Cattail,PlantKind.Pumpkin,PlantKind.Peashooter);Check(!cat.TryPlant(PlantKind.Cattail,2,3),"Cattail cannot be planted without lily");cat.TryPlant(PlantKind.LilyPad,2,3);cat.TryPlant(PlantKind.Peashooter,2,3);Check(!cat.TryPlant(PlantKind.Cattail,2,3),"Cattail needs an empty lily");cat.RemovePlant(2,3);cat.TryPlant(PlantKind.Pumpkin,2,3);Check(cat.TryPlant(PlantKind.Cattail,2,3),"Cattail consumes empty lily under pumpkin");Check(cat.AdventurePlantAt(2,3,PlantLayer.Support)==null&&cat.AdventurePlantAt(2,3,PlantLayer.Armor)!=null,"Cattail replaces support without losing armor");
        Spawn(cat,ZombieKind.Normal,0,4);var balloon=Spawn(cat,ZombieKind.Balloon,5,9);cat.Update(1.1f);Check(cat.Peas.Count==2&&cat.Peas.All(p=>p.Homing&&p.HitsAir&&p.TargetZombieId==balloon.Id&&p.SourceKind==PlantKind.Cattail),"Cattail fires two homing darts prioritizing balloon in another lane");cat.Update(2f);Check(!balloon.Airborne&&balloon.Health<270,"homing darts cross lanes, pop balloon and damage real target");
    }
    private static void WinterAndGold()
    {
        var winter=Fixture(49,null,PlantKind.MelonPult,PlantKind.WinterMelon);Check(winter.TryPlant(PlantKind.MelonPult,2,1)&&winter.TryPlant(PlantKind.WinterMelon,2,1),"Winter consumes Melon");var direct=Spawn(winter,ZombieKind.Normal,2,5);var nearby=Spawn(winter,ZombieKind.Normal,1,5);
        for (var tick = 0; tick < 300 && direct.Health == 270; tick++)
        {
            winter.Update(.01f);
        }

        Check(direct.Health==190&&nearby.Health==244&&direct.ChillRemaining>0&&nearby.ChillRemaining>0,"Winter direct/splash damage chills adjacent lane after the native lob reaches its target");
        var gold=Fixture(49,null,PlantKind.MagnetShroom,PlantKind.GoldMagnet,PlantKind.Marigold);gold.TryPlant(PlantKind.MagnetShroom,1,1);Check(gold.PlantAt(1,1).Asleep&&gold.TryPlant(PlantKind.GoldMagnet,1,1)&&!gold.PlantAt(1,1).Asleep,"Gold Magnet is active in daylight without coffee");var metal=Spawn(gold,ZombieKind.Buckethead,1,3);
        for (var i = 0; i < 6; i++)
        {
            Check(gold.SpawnAdventureCoin(AdventureCoinKind.Silver, .5f + i, 1) != null, "host coin fixture");
        }

        var sun = gold.SunAmount;
        gold.Update(1.1f);
        Check(gold.Adventure.Coins.Count(c => c.IsBeingCollected) == 5 && gold.Adventure.MoneyCollectedUnits == 0,
            "Gold Magnet reserves at most five coins before delivery");
        gold.Update(.6f);
        Check(gold.Adventure.MoneyCollectedUnits == 5 && gold.Adventure.Coins.Count == 1 && metal.ArmorHealth == 1100,
            "Gold Magnet collects money, not enemy metal");
        var coin = gold.Adventure.Coins[0];
        Check(gold.CollectAdventureCoin(coin) && !gold.CollectAdventureCoin(coin), "manual coin pickup cannot double collect");
        var gc = gold.SpawnAdventureCoin(AdventureCoinKind.Gold, 1, 1);
        var dc = gold.SpawnAdventureCoin(AdventureCoinKind.Diamond, 2, 1);
        gold.CollectAdventureCoin(gc);
        gold.CollectAdventureCoin(dc);
        gold.Update(1.5f);
        Check(gold.Adventure.MoneyCollectedUnits == 111 && gold.Adventure.MoneyDisplayValue == 1110 && gold.SunAmount == sun,
            "currency units are 1/5/100 and remain separate from sun after their collection flights");
        Check(gold.TryPlant(PlantKind.Marigold,0,0),"Marigold is available as real coin source");gold.Update(5f);Check(gold.Adventure.MoneyCollectedUnits>111,"Gold Magnet collects actual Marigold production");
    }
    private static void Spikerock()
    {
        var g=Fixture(29,null,PlantKind.Spikeweed,PlantKind.Spikerock);Check(g.TryPlant(PlantKind.Spikeweed,0,3)&&g.TryPlant(PlantKind.Spikerock,0,3),"Spikerock replaces ground Spikeweed");Plant rock=g.PlantAt(0,3);Check(rock.Health==450,"native Spikerock durability");var normal=Spawn(g,ZombieKind.Normal,0,3.5f);g.Update(1.01f);Check(normal.Health==230&&rock.Health==450&&!normal.Eating,"Spikerock damages twice per second and cannot be eaten");
        for(int i=1;i<=9;i++) {var z=Spawn(g,ZombieKind.Zomboni,0,3.5f);g.Update(0.05f);Check(!g.Zombies.Contains(z),"Spikerock punctures vehicle "+i);Check(i==9?!g.Plants.Contains(rock):g.Plants.Contains(rock)&&rock.Health==450-i*50,"vehicle durability hit "+i);}
        Check(rock.VehicleHitsTaken==9,"exactly nine vehicle hits consumed");
        var smash=Fixture(29,null,PlantKind.Spikeweed,PlantKind.Spikerock);smash.TryPlant(PlantKind.Spikeweed,0,3);smash.TryPlant(PlantKind.Spikerock,0,3);var garg=Spawn(smash,ZombieKind.Gargantuar,0,3.75f);
        Check(AdventureZombieBiteRules.Reaches(smash.AdventureZombieAttackRectangle(garg), smash.AdventurePlantEatingRectangle(smash.PlantAt(0,3))), "smash fixture starts with the original twenty-pixel attack overlap");
        smash.Update(0.05f);Check(smash.PlantAt(0,3).Health==400,"Gargantuar smash removes one rock durability hit");
    }
    private static void Cannon()
    {
        var g=Fixture(49,null,PlantKind.KernelPult,PlantKind.CobCannon,PlantKind.Pumpkin,PlantKind.Peashooter,PlantKind.FlowerPot);g.TryPlant(PlantKind.KernelPult,2,1);Check(!g.TryPlant(PlantKind.CobCannon,2,1),"cannon requires second adjacent Kernel");g.Update(8f);g.TryPlant(PlantKind.KernelPult,2,2);
        Check(g.TryPlant(PlantKind.Pumpkin,2,2),"pre-cannon kernel can have armor");int armorCash=g.SunAmount;Check(!g.TryPlant(PlantKind.CobCannon,2,1)&&g.SunAmount==armorCash&&g.AdventurePlantAt(2,1,PlantLayer.Main).Kind==PlantKind.KernelPult&&g.AdventurePlantAt(2,2,PlantLayer.Main).Kind==PlantKind.KernelPult,"armor prevents upgrade without consuming either kernel");Check(g.RemovePlant(2,2)&&g.AdventurePlantAt(2,2,PlantLayer.Main).Kind==PlantKind.KernelPult,"shovel removes armor before prerequisite");
        int cash=g.SunAmount;Check(g.TryPlant(PlantKind.CobCannon,2,1)&&g.SunAmount==cash-500,"cannon consumes both kernels and extra 500 sun");Plant cannon=g.AdventurePlantAt(2,1,PlantLayer.Main);Check(cannon.ColumnSpan==2&&object.ReferenceEquals(g.AdventurePlantAt(2,2,PlantLayer.Main),cannon)&&object.ReferenceEquals(g.PlantAt(2,2),cannon),"two-cell footprint resolves from either cell");Check(!g.TryPlant(PlantKind.Peashooter,2,2)&&!g.TryPlant(PlantKind.Pumpkin,2,2),"right cannon cell blocks plants and pumpkin");Check(!g.TryAimAdventureCobCannon(cannon.Id),"unloaded cannon cannot aim");g.Update(7.6f);Check(cannon.CannonPhase==CobCannonPhase.Ready&&g.TryAimAdventureCobCannon(2,2),"first arming/loading completes and right cell selects cannon");Check(g.CancelAdventureCobCannonAim()&&cannon.CannonPhase==CobCannonPhase.Ready,"cancel aim preserves loaded cannon");Check(!g.TryFireAdventureCobCannonAtCell(2,6)&&g.TryAimAdventureCobCannon(cannon.Id),"fire requires explicit aim");g.TogglePause();Check(!g.TryFireAdventureCobCannonAtCell(2,6),"paused cannon cannot fire");g.TogglePause();Check(!g.TryFireAdventureCobCannon(float.NaN,2),"invalid aim cannot consume loaded cannon");var target=Spawn(g,ZombieKind.Gargantuar,2,6.5f);target.ThrownImp=true; // Isolate reload from the unrelated imp-summoning behavior.
        var near=Spawn(g,ZombieKind.Buckethead,1,6.5f);var far=Spawn(g,ZombieKind.Normal,4,6.5f);Check(g.TryFireAdventureCobCannonAtCell(2,6)&&!g.TryFireAdventureCobCannonAtCell(2,6),"one aim emits one shot");g.Update(2f);Check(g.Adventure.CobShells.Count==0,"cannon waits for native 206-tick launch phase");g.Update(0.1f);Check(g.Adventure.CobShells.Count==1&&cannon.CannonPhase==CobCannonPhase.Reloading,"launch creates independent shell and reload");g.Update(GameSimulation.AdventureCobFlightSeconds);Check(target.Health==1200&&!g.Zombies.Contains(near)&&far.Health==270,"targeted 1800 blast hits target neighborhood only after the native shell flight");Check(!g.TryAimAdventureCobCannon(cannon.Id),"reloading cannon rejects aim");g.Update(31.1f);Check(cannon.CannonPhase==CobCannonPhase.Ready&&g.CooldownRemaining(PlantKind.CobCannon)>0&&g.TryAimAdventureCobCannon(cannon.Id),"weapon reload is independent of seed packet recharge");Check(g.RemovePlant(2,2)&&g.Adventure.SelectedCobCannonId==null&&g.AdventurePlantAt(2,1,PlantLayer.Main)==null&&g.AdventurePlantAt(2,1,PlantLayer.Support)!=null,"right-cell shovel removes entire cannon and clears aim while retaining pots");
    }
    private static void Imitater()
    {
        var g=Fixture(39,PlantKind.Peashooter,PlantKind.Imitater,PlantKind.Peashooter);int cash=g.SunAmount;Check(g.TryPlant(PlantKind.Imitater,1,1)&&g.SunAmount==cash-100,"Imitater pays copied seed price");Plant mimic=g.PlantAt(1,1);int id=mimic.Id;Check(mimic.Kind==PlantKind.Imitater&&mimic.ImitaterTarget==PlantKind.Peashooter&&g.CooldownRemaining(PlantKind.Imitater)>7.5f&&g.CooldownRemaining(PlantKind.Peashooter)==0,"morph placeholder has separate target-derived cooldown");Check(g.TryPlant(PlantKind.Peashooter,0,1),"original and imitation packets can be used independently");Spawn(g,ZombieKind.Normal,1,7);g.Update(3.1f);Check(g.PlantAt(1,1).Kind==PlantKind.Imitater&&!g.Peas.Any(p=>p.Row==1),"placeholder does not fire before transformation");g.Update(0.2f);Plant copy=g.PlantAt(1,1);Check(copy.Kind==PlantKind.Peashooter&&copy.Id==id&&copy.IsImitated&&copy.ImitaterTarget==PlantKind.Peashooter,"morph keeps stable identity and exposes gray-copy metadata");for (var tick = 0; tick < 200 && !g.Peas.Any(p => p.Row == 1); tick++)
        {
            g.Update(.01f);
        }
        Check(g.Peas.Any(p=>p.Row==1&&p.SourceKind==PlantKind.Peashooter&&p.SourceIsImitated),"transformed shooter attacks with immutable copied-source metadata after its native randomized initial countdown");Check(!g.SetAdventureImitaterTarget(PlantKind.CherryBomb),"cannot change copied type after deck confirmation");
        var bomb=Fixture(49,PlantKind.CherryBomb,PlantKind.Imitater,PlantKind.CherryBomb);Check(bomb.AdventureSeedCost(PlantKind.Imitater)==150&&bomb.AdventureSeedRechargeTime(PlantKind.Imitater)>50f,"slow copied packet uses Cherry price and recharge");var z=Spawn(bomb,ZombieKind.Normal,2,3.5f);bomb.TryPlant(PlantKind.Imitater,2,2);bomb.Update(3.1f);Check(bomb.Zombies.Contains(z)&&bomb.CooldownRemaining(PlantKind.CherryBomb)==0,"imitation bomb waits through morph and leaves original packet ready");bomb.Update(1.3f);Check(!bomb.Zombies.Contains(z),"copied instant plant starts its own fuse after morph");
        var water=Fixture(39,PlantKind.LilyPad,PlantKind.Imitater,PlantKind.LilyPad,PlantKind.Peashooter);Check(water.TryPlant(PlantKind.Imitater,2,3)&&!water.TryPlant(PlantKind.Peashooter,2,3)&&water.LastAdventurePlantingFailure==AdventurePlantingFailure.NeedsLilyPad,"imitation lily does not support plants while transforming");water.Update(3.3f);Check(water.AdventurePlantAt(2,3,PlantLayer.Support).Kind==PlantKind.LilyPad&&water.TryPlant(PlantKind.Peashooter,2,3),"transformed lily becomes real support");
        var coffee=Fixture(49,PlantKind.CoffeeBean,PlantKind.FumeShroom,PlantKind.Imitater);coffee.TryPlant(PlantKind.FumeShroom,1,1);Plant mushroom=coffee.AdventurePlantAt(1,1,PlantLayer.Main);Check(coffee.TryPlant(PlantKind.Imitater,1,1)&&coffee.AdventurePlantAt(1,1,PlantLayer.Effect)!=null&&object.ReferenceEquals(mushroom,coffee.AdventurePlantAt(1,1,PlantLayer.Main)),"imitation coffee occupies separate effect layer");coffee.Update(3.3f);Check(mushroom.Asleep&&mushroom.WakeTimer>0&&coffee.AdventurePlantAt(1,1,PlantLayer.Effect)==null,"imitation coffee morphs before waking target");coffee.Update(2.1f);Check(!mushroom.Asleep&&!mushroom.IsImitated,"coffee wakes real mushroom without incorrectly retagging it");
    }
    private static void ProjectileMetadata()
    {
        var g=Fixture(39,null,PlantKind.PuffShroom,PlantKind.SeaShroom,PlantKind.FumeShroom,PlantKind.Cactus,PlantKind.Starfruit,PlantKind.Peashooter);
        Check(g.TryPlant(PlantKind.PuffShroom,0,1)&&g.TryPlant(PlantKind.SeaShroom,2,1)&&g.TryPlant(PlantKind.FumeShroom,1,1)&&g.TryPlant(PlantKind.Cactus,4,1)&&g.TryPlant(PlantKind.Starfruit,5,1)&&g.TryPlant(PlantKind.Peashooter,4,2),"source metadata fixture plants six real projectile producers");
        Spawn(g,ZombieKind.Normal,0,3);Spawn(g,ZombieKind.Normal,2,3);Spawn(g,ZombieKind.ScreenDoor,1,3);Spawn(g,ZombieKind.Balloon,4,7);Spawn(g,ZombieKind.Normal,4,8);
        var sources = ObserveProjectileSources(g);
        foreach(var k in new[]{PlantKind.PuffShroom,PlantKind.SeaShroom,PlantKind.FumeShroom,PlantKind.Cactus,PlantKind.Starfruit,PlantKind.Peashooter})Check(sources.Contains(k),"projectile captures source "+k);
        var lob=Fixture(49,null,PlantKind.CabbagePult,PlantKind.KernelPult,PlantKind.MelonPult);lob.TryPlant(PlantKind.CabbagePult,0,1);lob.TryPlant(PlantKind.KernelPult,1,1);lob.TryPlant(PlantKind.MelonPult,2,1);for(int row=0;row<3;row++)Spawn(lob,ZombieKind.Buckethead,row,9f);
        var lobbedSources = ObserveProjectileSources(lob, true);
        foreach(var k in new[]{PlantKind.CabbagePult,PlantKind.KernelPult,PlantKind.MelonPult})Check(lobbedSources.Contains(k),"lobbed projectile captures source "+k);
        var legacy=new GameSimulation(new GameRules{StartingSun=1000,ZombieCount=0,SkySunInterval=0});legacy.TryPlant(PlantKind.SnowPea,2,2);legacy.Zombies.Add(new Zombie{Kind=ZombieKind.Normal,Row=2,X=9,Health=270});legacy.Update(2.1f);Check(legacy.Peas.Count>0&&legacy.Peas.All(p=>p.SourceKind==PlantKind.SnowPea&&p.IsSnow),"legacy timed projectile also records source");
    }

    /// <summary>Observes projectile metadata across native randomized launch times and transient impacts.</summary>
    /// <param name="game">The isolated producer fixture with visible targets.</param>
    /// <param name="lobbedOnly">Whether only native lobbed projectiles count toward the result.</param>
    /// <returns>The source species observed during the bounded three-second simulation window.</returns>
    private static HashSet<PlantKind> ObserveProjectileSources(GameSimulation game, bool lobbedOnly = false)
    {
        var sources = new HashSet<PlantKind>();
        for (var tick = 0; tick < 300; tick++)
        {
            game.Update(.01f);
            foreach (var projectile in game.Peas)
            {
                if (!lobbedOnly || projectile.Lobbed)
                {
                    sources.Add(projectile.SourceKind);
                }
            }
        }

        return sources;
    }

}
