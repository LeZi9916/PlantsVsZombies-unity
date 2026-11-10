using System;
using System.Collections.Generic;
using System.Linq;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Adventure;

// Test-only player. All world changes go through the same public actions available to the host UI.
// It cannot add sun, place free units, kill zombies, edit waves, or mark a match won.
internal sealed class PlaythroughBot
{
    private readonly GameSimulation g;
    private readonly List<PlantKind> deck;
    private float lastSmash=-10;
    public int PlantActions {get;private set;}
    public int PacketActions {get;private set;}
    public int ToolActions {get;private set;}
    public PlaythroughBot(GameSimulation game,AdventureCampaign campaign=null)
    {
        g=game;
        if(g.Adventure.Phase==AdventurePhase.ChoosingSeeds)
        {
            var l=g.Adventure.Level;
            PlantKind[] order;
            if(l.HasRoof) order=new[]{PlantKind.Sunflower,PlantKind.FlowerPot,PlantKind.CabbagePult,l.Number==48 || l.Number==44?PlantKind.Squash:PlantKind.KernelPult,PlantKind.CherryBomb,l.Number==44?PlantKind.TallNut:PlantKind.Jalapeno,PlantKind.Squash,PlantKind.PotatoMine};
            else if(l.HasPool && l.IsNight) order=new[]{PlantKind.SunShroom,PlantKind.LilyPad,PlantKind.PuffShroom,PlantKind.SeaShroom,PlantKind.Cactus,
                l.Number==38?PlantKind.TallNut:l.Number>=36?PlantKind.HypnoShroom:PlantKind.FumeShroom,PlantKind.CherryBomb,PlantKind.Peashooter,PlantKind.FumeShroom};
            else if(l.HasPool) order=new[]{PlantKind.Sunflower,PlantKind.LilyPad,PlantKind.Peashooter,PlantKind.SnowPea,PlantKind.TangleKelp,PlantKind.Squash,PlantKind.Wallnut,PlantKind.PotatoMine,PlantKind.CherryBomb};
            else if(l.IsNight) order=new[]{PlantKind.SunShroom,PlantKind.PuffShroom,PlantKind.FumeShroom,PlantKind.SnowPea,PlantKind.CherryBomb,PlantKind.GraveBuster,PlantKind.Sunflower,PlantKind.Peashooter};
            else order=new[]{PlantKind.Sunflower,PlantKind.Peashooter,PlantKind.PotatoMine,PlantKind.SnowPea,PlantKind.CherryBomb,PlantKind.Wallnut,PlantKind.Repeater};
            var cards=new List<PlantKind>();
            foreach(var k in order)if(l.UnlockedSeeds.Contains(k) && !cards.Contains(k) && cards.Count<6)cards.Add(k);
            foreach(var k in l.UnlockedSeeds)if(!cards.Contains(k) && cards.Count<6)cards.Add(k);
            if(!(campaign==null?g.ChooseAdventureSeeds(cards):campaign.ChooseSeeds(cards)))throw new Exception("Bot loadout refused "+l.Label);
        }
        deck=g.Adventure.SelectedSeeds.ToList();
        if(g.Adventure.Level.Number==1)g.SelectAdventureSeed(PlantKind.Peashooter);
    }
    public void Act()
    {
        foreach(var sun in g.Suns.ToArray())g.CollectSun(sun);
        if(g.State!=MatchState.Playing)return;
        if(g.Adventure.Tutorial==AdventureTutorial.Shovel)
            for(int c=3;c<=5;c++)if(g.RemovePlant(2,c))ToolActions++;
        var mode=g.Adventure.Level.Mode;
        if(mode==AdventureMode.WhackAZombie)
        {foreach(var z in g.Zombies.ToArray())if(g.HammerAdventureZombie(z.Id))ToolActions++;return;}
        if(mode==AdventureMode.Vasebreaker) {Vases();return;}
        if(g.Adventure.Level.HasConveyor) {Conveyor();return;}
        NormalLawn();
    }
    private bool Has(PlantKind k) {return deck.Contains(k);}
    private bool Ready(PlantKind k) {return Has(k) && g.CooldownRemaining(k)<=0 && g.SunAmount>=GameSimulation.Cost(k);}
    private bool Try(PlantKind k,int row,int col)
    {
        if(!Ready(k))return false;
        if(g.Adventure.Level.Number==48 && g.Adventure.CurrentWave>=8 && (Attacker(k) || k==PlantKind.Sunflower) && g.SunAmount<GameSimulation.Cost(k)+150)return false;
        var reason=g.CanPlantAdventure(k,row,col);
        if(reason==AdventurePlantingFailure.NeedsLilyPad && Ready(PlantKind.LilyPad) && g.SunAmount>=GameSimulation.Cost(k)+GameSimulation.Cost(PlantKind.LilyPad))
        {if(g.TryPlant(PlantKind.LilyPad,row,col))PlantActions++;}
        if(reason==AdventurePlantingFailure.NeedsFlowerPot && Ready(PlantKind.FlowerPot) && g.SunAmount>=GameSimulation.Cost(k)+GameSimulation.Cost(PlantKind.FlowerPot))
        {if(g.TryPlant(PlantKind.FlowerPot,row,col))PlantActions++;}
        if(g.TryPlant(k,row,col)){PlantActions++;return true;}return false;
    }
    private int AttackCount(int row)
    {return g.Plants.Count(p=>p.Row==row && Attacker(p.Kind) && !p.Asleep);}
    private static bool Attacker(PlantKind k)
    {return k==PlantKind.Chomper || k==PlantKind.Peashooter || k==PlantKind.SnowPea || k==PlantKind.Repeater || k==PlantKind.PuffShroom || k==PlantKind.FumeShroom || k==PlantKind.SeaShroom || k==PlantKind.Cactus || k==PlantKind.CabbagePult || k==PlantKind.KernelPult || k==PlantKind.MelonPult || k==PlantKind.Threepeater || k==PlantKind.ScaredyShroom || k==PlantKind.SplitPea || k==PlantKind.Starfruit;}
    private IEnumerable<int> RowsByNeed()
    {return Enumerable.Range(0,g.RowCount).Where(r=>g.Adventure.Level.IsLawnRow(r)).OrderByDescending(r=>
        g.Zombies.Where(z=>z.Row==r && !z.Hypnotized && !z.Headless && !z.Airborne).Sum(z=>(z.Health+z.ArmorHealth+z.ShieldHealth)/(Math.Max(0.5f,z.X)+1f)) - AttackCount(r)*(g.Adventure.Level.Mode==AdventureMode.Boss?100f:60f));}
    private bool PlaceAttacker(PlantKind k,int row)
    {
        int[] columns=g.Adventure.Level.HasRoof?new[]{2,3,1,0,4,5,6,7}:k==PlantKind.PuffShroom || k==PlantKind.SeaShroom?new[]{4,3,5,2,6,1}:k==PlantKind.FumeShroom?new[]{3,2,4,1,5,6}:new[]{2,3,1,0,4,5,6};
        foreach(int c in columns)if(Try(k,row,c))return true;return false;
    }
    private void NormalLawn()
    {
        var l=g.Adventure.Level;
        var live=g.Zombies.Where(z=>!z.Hypnotized && !z.Headless).OrderBy(z=>z.X).ToArray();
        if(l.Number==1) {if(g.Plants.Count==0)Try(PlantKind.Peashooter,2,0);else foreach(int c in new[]{1,2,3,4,5})if(Try(PlantKind.Peashooter,2,c))break;return;}
        // Zero-cost mushrooms still obey recharge and terrain support.
        foreach(var free in new[]{PlantKind.PuffShroom,PlantKind.SeaShroom}) if(Ready(free))
            foreach(int row in RowsByNeed()) if((free==PlantKind.SeaShroom)==l.IsWaterRow(row) && PlaceAttacker(free,row))break;
        foreach(Zombie z in live)
        {
            if(l.IsWaterRow(z.Row) && Ready(PlantKind.LilyPad) && AttackCount(z.Row)<2 && g.AdventurePlantAt(z.Row,2,PlantLayer.Support)==null)
                if(g.TryPlant(PlantKind.LilyPad,z.Row,2))PlantActions++;
            if(z.Kind==ZombieKind.Pogo && z.X>3.8f && z.X<9f && Ready(PlantKind.TallNut) && !g.Plants.Any(p=>p.Row==z.Row && p.Kind==PlantKind.TallNut))
            {var old=g.AdventurePlantAt(z.Row,3,PlantLayer.Main);if(old!=null && old.Kind!=PlantKind.TallNut)if(g.RemovePlant(z.Row,3))ToolActions++;Try(PlantKind.TallNut,z.Row,3);}
            if(z.X<2.7f && Ready(PlantKind.Squash))
            {int c=Math.Max(0,(int)z.X);var old=g.AdventurePlantAt(z.Row,c,PlantLayer.Main);if(old!=null)if(g.RemovePlant(z.Row,c))ToolActions++;Try(PlantKind.Squash,z.Row,c);}
            if(Ready(PlantKind.Wallnut) && z.X>3.8f && z.X<6.5f && AttackCount(z.Row)>0 && (z.Health+z.ArmorHealth+z.ShieldHealth>300f || g.Zombies.Count(q=>q.Row==z.Row && !q.Hypnotized)>1))
                for(int c=Math.Min(5,(int)z.X);c>=3;c--)if(Try(PlantKind.Wallnut,z.Row,c))break;
            if(z.Airborne && Ready(PlantKind.Cactus))PlaceAttacker(PlantKind.Cactus,z.Row);
            if(z.Kind==ZombieKind.Digger && z.AbilityPhase>=1 && Ready(PlantKind.HypnoShroom))
            {if(g.AdventurePlantAt(z.Row,0,PlantLayer.Main)!=null)g.RemovePlant(z.Row,0);Try(PlantKind.HypnoShroom,z.Row,0);}
            if(l.IsWaterRow(z.Row) && z.X<5f && !z.Airborne && Ready(PlantKind.TangleKelp))Try(PlantKind.TangleKelp,z.Row,Math.Max(0,(int)z.X));
            bool danger=z.X<3f || z.Kind==ZombieKind.Pogo && z.X<6f || z.Kind==ZombieKind.Gargantuar && z.X<6f || z.Kind==ZombieKind.Zomboni && z.X<7f;
            if(danger)
            {
                if(!(l.Number==48 && z.Kind==ZombieKind.Gargantuar))
                {
                    if(TryBlast(PlantKind.CherryBomb,z,false))continue;
                    if(TryBlast(PlantKind.Jalapeno,z,true))continue;
                }
                int col=Math.Max(0,Math.Min(8,(int)z.X));
                if(Ready(PlantKind.Squash))for(int c=col;c>=Math.Max(0,col-1);c--)if(Try(PlantKind.Squash,z.Row,c))break;
                if(l.IsWaterRow(z.Row) && Ready(PlantKind.TangleKelp))Try(PlantKind.TangleKelp,z.Row,col);
                if(!l.IsWaterRow(z.Row) && z.X>3.5f && Ready(PlantKind.Wallnut))for(int c=Math.Min(5,col);c>=3;c--)if(Try(PlantKind.Wallnut,z.Row,c))break;
            }
            if(z.X>6f && z.X<10f && !l.IsWaterRow(z.Row) && AttackCount(z.Row)<2 && Ready(PlantKind.PotatoMine))Try(PlantKind.PotatoMine,z.Row,3);
        }
        if(l.Number==38 && g.Adventure.CurrentWave>=3 && g.SunAmount<250)return;
        PlantKind solar=Has(PlantKind.SunShroom)?PlantKind.SunShroom:PlantKind.Sunflower;
        int activeRows=Enumerable.Range(0,g.RowCount).Count(l.IsLawnRow);
        int solarCount=g.Plants.Count(p=>p.Kind==solar),solarGoal=Math.Min(10,activeRows*(l.Number<=5?1:2)),initialSolarGoal=Math.Min(3,activeRows);
        bool urgent=live.Any(z=>!z.Airborne && z.X<6f && AttackCount(z.Row)<2);
        if(solarCount<initialSolarGoal && !urgent || l.Number==2 && solarCount<2)PlantSolar(solar);
        if(l.HasPool && !l.IsNight && Has(PlantKind.SnowPea) && g.Adventure.CurrentWave>=4)
        {
            foreach(int r in new[]{2,3}) if(!g.Plants.Any(p=>p.Row==r && p.Kind==PlantKind.SnowPea))
            { if(Ready(PlantKind.SnowPea))PlaceAttacker(PlantKind.SnowPea,r); if(g.SunAmount<175)return; }
        }
        if(l.Number==44 && live.Any(z=>z.Kind==ZombieKind.Pogo && !g.Plants.Any(p=>p.Row==z.Row && p.Kind==PlantKind.TallNut)) && g.SunAmount<150)return;
        PlantKind[] attack=l.HasRoof?new[]{PlantKind.CabbagePult,PlantKind.KernelPult}:l.IsNight?new[]{PlantKind.FumeShroom,PlantKind.Cactus,PlantKind.Peashooter,PlantKind.SnowPea}:new[]{PlantKind.Peashooter,PlantKind.SnowPea,PlantKind.Repeater};
        foreach(int row in RowsByNeed())
        {
            if(AttackCount(row)>0 && solarCount<initialSolarGoal && !urgent)continue;
            foreach(var k in attack)if(Ready(k) && PlaceAttacker(k,row))break;
        }
        if(solarCount<solarGoal && (!urgent || g.SunAmount>=175))PlantSolar(solar);
        if(Ready(PlantKind.GraveBuster) && g.SunAmount>200 && g.Adventure.Graves.Count>0)
        {var grave=g.Adventure.Graves.OrderBy(x=>x.Column).First();Try(PlantKind.GraveBuster,grave.Row,grave.Column);}
    }
    private void PlantSolar(PlantKind solar)
    {
        if(!Ready(solar))return;
        for(int c=0;c<2;c++)foreach(int row in RowsByNeed())if(!g.Adventure.Level.IsWaterRow(row) && Try(solar,row,c))return;
    }
    private bool TryBlast(PlantKind kind,Zombie target,bool lane)
    {
        if(!Ready(kind))return false;
        for(int c=0;c<9;c++)if(lane || Math.Abs(c+0.5f-target.X)<1.4f)if(Try(kind,target.Row,c))return true;
        return false;
    }
    private bool Packet(AdventurePacket p,int row,int col)
    {if(g.TryUseAdventurePacket(p.Id,row,col)){PacketActions++;return true;}return false;}
    private bool PlacePacketAttacker(AdventurePacket p)
    {
        var l=g.Adventure.Level;
        foreach(int r in RowsByNeed())
        {
            if(p.Kind==PlantKind.SeaShroom && !l.IsWaterRow(r))continue;
            int[] cols=l.HasRoof?new[]{2,1,0,3,4,5,6,7,8}:new[]{3,4,5,2,6,1,0,7,8};
            foreach(int c in cols)if(Packet(p,r,c))return true;
        }
        return false;
    }
    private void Conveyor()
    {
        var l=g.Adventure.Level;
        foreach(var p in g.Adventure.Packets.ToArray())
        {
            if(l.Mode==AdventureMode.Bowling)
            {
                var row=RowsByNeed().First();
                if(g.Zombies.Any(z=>z.Row==row && z.X<11f) || g.Adventure.Packets.Count>7)Packet(p,row,2);
            }
            else if(p.Kind==PlantKind.LilyPad || p.Kind==PlantKind.FlowerPot)
            {
                bool used=false;
                int[] cols=p.Kind==PlantKind.LilyPad?new[]{3,4,2,5,1,6,0,7,8}:l.Mode==AdventureMode.Boss?new[]{0,1,2,3,4,5,6,7,8}:new[]{3,4,5,6,7,8,2,1,0};
                foreach(int r in RowsByNeed())
                {if(p.Kind==PlantKind.LilyPad && !l.IsWaterRow(r))continue;foreach(int c in cols)if(Packet(p,r,c)){used=true;break;}if(used)break;}
            }
            else if(p.Kind==PlantKind.CherryBomb || p.Kind==PlantKind.Jalapeno || p.Kind==PlantKind.DoomShroom || p.Kind==PlantKind.IceShroom || p.Kind==PlantKind.Blover)
            {
                if(p.Kind==PlantKind.IceShroom && l.Mode==AdventureMode.Boss && !g.Adventure.BossHeadVulnerable && !g.Adventure.BossBalls.Any(b=>!b.IsIce))continue;
                if(p.Kind==PlantKind.Blover && !g.Zombies.Any(z=>z.Airborne) && g.Adventure.Packets.Count<8)continue;
                int bestR=-1,bestC=-1;float best=0;
                for(int r=0;r<g.RowCount;r++)for(int c=0;c<9;c++)
                {
                    if(g.CanPlantAdventure(p.Kind,r,c)!=AdventurePlantingFailure.None)continue;
                    float value=0;
                    foreach(var z in g.Zombies)
                        if(!z.Hypnotized && (p.Kind==PlantKind.IceShroom || p.Kind==PlantKind.Blover || Math.Abs(z.Row-r)<=(p.Kind==PlantKind.Jalapeno?0:p.Kind==PlantKind.DoomShroom?2:1) &&
                            Math.Abs(z.X-(c+0.5f))<=(p.Kind==PlantKind.Jalapeno?20:p.Kind==PlantKind.DoomShroom?3.5f:1.5f)))value+=Math.Min(1800,z.Health+z.ArmorHealth+z.ShieldHealth)/(Math.Max(1f,z.X));
                    if(l.Mode==AdventureMode.Boss && g.Adventure.BossHeadVulnerable && p.Kind==PlantKind.Jalapeno)value+=10000;
                    if(l.Mode==AdventureMode.Boss && (p.Kind==PlantKind.IceShroom && g.Adventure.BossBalls.Any(b=>!b.IsIce) ||
                        p.Kind==PlantKind.Jalapeno && g.Adventure.BossBalls.Any(b=>b.IsIce && b.Row==r)))value+=100000;
                    if(value>best){best=value;bestR=r;bestC=c;}
                }
                if(bestR>=0 && best>0)Packet(p,bestR,bestC);
            }
            else if(p.Kind==PlantKind.GraveBuster)
            {foreach(var grave in g.Adventure.Graves.ToArray())if(Packet(p,grave.Row,grave.Column))break;}
            else if(p.Kind==PlantKind.Pumpkin)
            {
                foreach(var plant in g.Plants.Where(x=>x.Layer==PlantLayer.Main).OrderByDescending(x=>x.Column).ToArray())if(Packet(p,plant.Row,plant.Column))break;
            }
            else if(p.Kind==PlantKind.TangleKelp || p.Kind==PlantKind.Squash || p.Kind==PlantKind.HypnoShroom)
            {
                foreach(var z in g.Zombies.Where(z=>!z.Hypnotized && !z.Airborne && z.X<9f).OrderBy(z=>z.X).ToArray())
                {int col=Math.Max(0,Math.Min(8,(int)z.X));if(Packet(p,z.Row,col)||Packet(p,z.Row,Math.Max(0,col-1)))break;}
            }
            else if(l.Mode==AdventureMode.Boss && p.Kind==PlantKind.MelonPult)
            {
                bool used=false;
                foreach(int r in Enumerable.Range(0,5).OrderBy(r=>g.Plants.Any(x=>x.Row==r && x.Column==0 && x.Kind==PlantKind.MelonPult)?1:0))
                {
                    var old=g.AdventurePlantAt(r,0,PlantLayer.Main);
                    if(old!=null && old.Kind==PlantKind.MelonPult)continue;
                    if(old!=null)if(g.RemovePlant(r,0))ToolActions++;
                    if(Packet(p,r,0)){used=true;break;}
                }
                if(!used)PlacePacketAttacker(p);
            }
            else PlacePacketAttacker(p);
        }
    }
    private void Vases()
    {
        foreach(var p in g.Adventure.Packets.ToArray())
        {
            if(p.Kind==PlantKind.Squash || p.Kind==PlantKind.HypnoShroom)
            {
                foreach(var z in g.Zombies.Where(z=>!z.Hypnotized && !z.Headless && z.DropRemaining<=0 && (p.Kind==PlantKind.HypnoShroom ? z.Kind==ZombieKind.Dancing || z.Kind==ZombieKind.Buckethead || z.Kind==ZombieKind.JackInTheBox || z.X<2f : z.Health+z.ArmorHealth>400 || z.X<3.5f)).OrderByDescending(z=>z.Health+z.ArmorHealth).ThenBy(z=>z.X).ToArray())
                {int col=Math.Max(0,Math.Min(8,(int)z.X));if(Packet(p,z.Row,col)||Packet(p,z.Row,Math.Max(0,col-1)))break;}
            }
            else
            {
                bool used=false;
                foreach(int r in Enumerable.Range(0,5).OrderBy(r=>AttackCount(r)).ThenByDescending(r=>g.Zombies.Where(z=>z.Row==r && !z.Hypnotized && !z.Headless).Sum(z=>z.Health+z.ArmorHealth)))
                {foreach(int c in new[]{2,3,1,0,4,5})if(Packet(p,r,c)){used=true;break;}if(used)break;}
            }
        }
        if(g.Elapsed-lastSmash<1f)return;
        var visible=g.Adventure.Vases.Where(v=>v.ShowsPlant).OrderByDescending(v=>v.Column).FirstOrDefault();
        var vase=visible??g.Adventure.Vases.OrderByDescending(v=>v.Column).ThenBy(v=>AttackCount(v.Row)==0?1:0).FirstOrDefault();
        if(vase!=null && (visible!=null || g.Zombies.Count<2))
        {if(g.SmashAdventureVase(vase.Row,vase.Column)){ToolActions++;lastSmash=g.Elapsed;}}
    }
}
