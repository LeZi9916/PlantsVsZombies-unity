using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using PvZReplica.Modes;
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        // PC 0x4338B0: count,type,zombie,seed. Seed 52 is the left-facing repeater, not an invented plant.
        private static readonly int[][] ModeVaseRecipes={
            new[]{5,1,-1,0,5,1,-1,5,5,1,-1,17,6,2,0,-1,3,2,4,-1,1,2,15,-1},
            new[]{7,1,-1,52,3,1,-1,5,3,1,-1,3,2,1,-1,4,6,2,0,-1,3,2,4,-1,1,2,15,-1},
            new[]{6,1,-1,52,4,1,-1,5,2,1,-1,17,3,1,-1,12,3,1,-1,3,8,2,0,-1,2,2,4,-1,1,2,8,-1,1,2,15,-1},
            new[]{11,1,-1,8,4,1,-1,12,4,1,-1,52,8,2,15,-1,7,2,0,-1,1,2,7,-1},
            new[]{6,1,-1,52,3,1,-1,30,4,1,-1,17,2,1,-1,12,2,1,-1,5,3,1,-1,31,6,2,0,-1,5,2,4,-1,1,2,15,-1,3,2,7,-1},
            new[]{7,1,-1,52,2,1,-1,17,5,1,-1,23,2,1,-1,18,4,1,-1,22,7,2,0,-1,5,2,3,-1,2,2,7,-1,1,2,15,-1},
            new[]{13,1,-1,21,3,1,-1,3,3,1,-1,17,10,2,0,-1,1,2,4,-1},
            new[]{7,1,-1,8,3,1,-1,23,5,1,-1,17,4,1,-1,52,8,2,15,-1,4,2,0,-1,4,2,18,-1},
            new[]{6,1,-1,52,2,1,-1,5,2,1,-1,0,2,1,-1,18,5,1,-1,17,1,1,-1,4,1,1,-1,3,1,1,-1,25,8,2,0,-1,5,2,4,-1,1,2,15,-1,1,2,23,-1}
        };
        private void InitializeModeVases()
        {
            var s=ModeSession;s.VaseSeeds.Clear();s.VaseSun.Clear();Adventure.vases.Clear();Adventure.packets.Clear();
            int id=s.Definition.Id;int min=id==51?4:id==52||id==53||id==57?3:2;
            var cells=new List<int>();for(int r=0;r<5;r++)for(int c=min;c<9;c++)if(id!=52||c!=8)cells.Add(r*9+c);
            int[] recipe=id==60?new[]{6,1,-1,52,2,1,-1,5,1,1,-1,0,2,1,-1,18,5,1,-1,17,1,1,-1,4,1,1,-1,3,1,1,-1,25,1,3,-1,-1,8-Math.Min(8,(s.Stage-1)/10),2,0,-1,5,2,4,-1,1,2,15,-1,1+Math.Min(8,(s.Stage-1)/10),2,23,-1}:ModeVaseRecipes[id-51];
            for(int i=0;i<recipe.Length;i+=4)for(int n=0;n<recipe[i];n++)
            {
                if(cells.Count==0)throw new InvalidOperationException("PC vase recipe exceeded its legal columns.");
                int pick=random.Next(cells.Count),cell=cells[pick];cells.RemoveAt(pick);
                var vase=new AdventureVase{Row=cell/9,Column=cell%9};
                if(recipe[i+1]==1){int seed=recipe[i+3];vase.Seed=seed==52?PlantKind.Repeater:(PlantKind)seed;s.VaseSeeds.Add(cell,seed);}
                else if(recipe[i+1]==2)vase.Zombie=(ZombieKind)recipe[i+2];
                else s.VaseSun.Add(cell,(random.Next(3)+1)*25);
                Adventure.vases.Add(vase);
            }
            int green=0;foreach(var vase in Adventure.vases)if(vase.Seed.HasValue&&green<2){vase.ShowsPlant=true;green++;}
            if(id==52)for(int r=0;r<5;r++){int cell=r*9+8;Adventure.vases.Add(new AdventureVase{Row=r,Column=8,Seed=PlantKind.Repeater,ShowsPlant=true});s.VaseSeeds[cell]=52;}
            Adventure.VaseStage=2;Adventure.Tutorial=AdventureTutorial.Complete;
            // PC puzzles keep the cherry-bomb card; seed packets from vases remain free.
            Adventure.SelectedSeeds=Array.AsReadOnly(new[]{PlantKind.CherryBomb});
        }
        public bool SmashModeVase(int row,int column)
        {
            var s=ModeSession;if(s==null||s.Definition.Mechanic!=ModeMechanic.Vases||s.Phase!=ModePhase.Playing||State!=MatchState.Playing)return false;
            var vase=Adventure.vases.Find(v=>v.Row==row&&v.Column==column);if(vase==null)return false;
            int cell=row*9+column,seed,value;Adventure.vases.Remove(vase);
            if(s.VaseSeeds.TryGetValue(cell,out seed))
            {
                var packet=AddAdventurePacket(seed==52?PlantKind.Repeater:(PlantKind)seed,row,column);
                if(seed==52)s.BackwardPackets.Add(packet.Id);s.VaseSeeds.Remove(cell);
            }
            else if(s.VaseSun.TryGetValue(cell,out value)){for(int i=0;i<value/25;i++)Suns.Add(CreateAdventurePresentationSunFromPoint(40f+column*80f+i*15f,80f+row*100f,25));s.VaseSun.Remove(cell);}
            else if(vase.Zombie.HasValue)SpawnAdventureZombie(vase.Zombie.Value,row,column+0.5f,s.Stage-1);
            return true;
        }
        // PC 0x435B40 fixed and randomly placed plants. Coordinates differ from the mobile .NET port.
        private void InitializeModeIZombie()
        {
            var s=ModeSession;Array.Clear(s.Brains,0,s.Brains.Length);Array.Clear(s.BrainBite,0,s.BrainBite.Length);s.SunflowerHealth.Clear();s.SunflowerDrops.Clear();
            switch(s.Definition.Id)
            {
                case 61:ModeFixedPlant(1,3,1);ModeFixedPlant(1,3,3);ModeRandomPlants(1,7);ModeRandomPlants(17,3);ModeRandomPlants(0,6);ModeRandomPlants(5,2);break;
                case 62:ModeFixedPlant(21,3,0);ModeFixedPlant(1,2,0);ModeFixedPlant(1,3,3);ModeRandomPlants(21,1,0);ModeRandomPlants(0,1,0);ModeRandomPlants(5,2,3);ModeRandomPlants(1,1,3);ModeRandomPlants(1,4);ModeRandomPlants(21,2);ModeRandomPlants(5,2);ModeRandomPlants(0,4);break;
                case 63:ModeFixedPlant(4,3,0);ModeFixedPlant(1,2,0);ModeFixedPlant(4,2,2);ModeFixedPlant(1,2,4);ModeFixedPlant(22,3,3);ModeRandomPlants(22,2);ModeRandomPlants(1,5);ModeRandomPlants(0,7);ModeRandomPlants(28,1);break;
                case 64:for(int r=0;r<5;r++)ModeFixedPlant(3,3,r);ModeFixedPlant(1,2,0);ModeFixedPlant(1,2,2);ModeFixedPlant(1,2,4);ModeRandomPlants(0,1,0);ModeRandomPlants(5,1,1);ModeRandomPlants(10,2,2);ModeRandomPlants(5,1,3);ModeRandomPlants(0,1,4);ModeRandomPlants(0,2);ModeRandomPlants(1,4);break;
                case 65:ModeFixedPlant(1,3,2);ModeFixedPlant(1,3,3);ModeRandomPlants(26,1,1);ModeRandomPlants(26,1,4);ModeRandomPlants(31,1);ModeRandomPlants(1,5);ModeRandomPlants(0,8);ModeRandomPlants(5,2);break;
                case 66:ModeFixedPlant(36,4,1);ModeFixedPlant(36,4,3);ModeRandomPlants(1,3,1);ModeRandomPlants(1,3,3);ModeRandomPlants(22,2);ModeRandomPlants(1,2);ModeRandomPlants(21,3);ModeRandomPlants(5,1);ModeRandomPlants(0,5);ModeRandomPlants(17,2);ModeRandomPlants(34,2);break;
                case 67:ModeFixedPlant(1,4,2);ModeFixedPlant(1,4,4);ModeRandomPlants(1,6);ModeRandomPlants(4,9);ModeRandomPlants(6,8);break;
                case 68:ModeRandomPlants(3,3);ModeRandomPlants(31,2);ModeRandomPlants(0,8);ModeRandomPlants(17,2);ModeRandomPlants(4,2);ModeRandomPlants(1,8);break;
                case 69:ModeFixedPlant(23,5,1);ModeFixedPlant(22,5,3);ModeRandomPlants(4,4,0);ModeRandomPlants(1,2,0);ModeRandomPlants(1,2,1);ModeRandomPlants(18,1,1);ModeRandomPlants(5,1,1);ModeRandomPlants(28,1,1);ModeRandomPlants(6,3,2);ModeRandomPlants(1,2,2);ModeRandomPlants(17,1,2);ModeRandomPlants(0,3,3);ModeRandomPlants(1,2,3);ModeRandomPlants(1,1,4);ModeRandomPlants(10,1,4);ModeRandomPlants(13,1,4);ModeRandomPlants(29,1,4);ModeRandomPlants(28,1,4);ModeRandomPlants(31,1,4);break;
                case 70:InitializeModeIZombieEndless();break;
            }
            foreach(var p in Plants)
            {
                p.Asleep=false;p.ModeFireTimer=0;
                if(p.Kind==PlantKind.PotatoMine){p.ActionTimer=0;p.MinePhase=MinePhase.Armed;p.PhaseAge=20;}
                if(p.Kind==PlantKind.Sunflower){p.ActionTimer=100000;s.SunflowerHealth[p.Id]=p.Health;s.SunflowerDrops[p.Id]=0;}
            }
            int[][] cards={new[]{0,4,7},new[]{0,6,4},new[]{0,4,17},new[]{0,3,20},new[]{0,8,20,16},new[]{0,3,4,23},new[]{0,3,4,8},new[]{24,2,4,20,17,21},new[]{24,2,3,4,20,17,21,7},new[]{24,2,3,4,20,17,21,7,8}};
            var result=new List<ZombieKind>();foreach(int id in cards[s.Definition.Id-61])result.Add((ZombieKind)id);s.ZombieCards=result.AsReadOnly();
        }
        private int ModeIZombieColumns { get {int id=ModeSession.Definition.Id;return id<=65?4:id==69?6:5;} }
        private void ModeFixedPlant(int kind,int column,int row)
        {if(AdventurePlantAt(row,column,PlantLayer.Main)==null)PlaceAdventurePlant((PlantKind)kind,row,column);}
        private void ModeRandomPlants(int kind,int count,int row=-1)
        {
            var cells=new List<int>();for(int r=0;r<5;r++)if(row<0||r==row)for(int c=0;c<ModeIZombieColumns;c++)
                if(AdventurePlantAt(r,c,PlantLayer.Main)==null&&((kind!=3&&kind!=22)||ModeIZombieColumns-c<=3))cells.Add(r*9+c);
            for(int n=0;n<count&&cells.Count>0;n++){int p=random.Next(cells.Count),cell=cells[p];cells.RemoveAt(p);PlaceAdventurePlant((PlantKind)kind,cell/9,cell%9);}
        }
        private void InitializeModeIZombieEndless()
        {
            int stage=ModeSession.Stage-1;int style=random.Next(5);int lo=Math.Min(4,Math.Max(2,2+stage/3)),hi=Math.Min(6,Math.Max(2,3+stage/2));
            if(stage>=10){lo=Math.Min(5,Math.Max(2,2+stage/3));hi=Math.Min(7,Math.Max(2,3+stage/2));}
            int replace=stage==0?0:stage==1?1:random.Next(lo,hi+1);ModeRandomPlants(1,8-replace);ModeRandomPlants(8,replace);
            if(style==0&&stage>=1)
            {
                int branch=random.Next(5);int[][] kinds={new[]{5,9,28,4,7,4},new[]{4,9,6,8},new[]{21,9,29,8},new[]{10,9,31,8},new[]{13,12,1,5}};
                int[] pattern=kinds[branch];for(int i=0;i<pattern.Length;i+=2)ModeRandomPlants(pattern[i],pattern[i+1]);
            }
            else
            {
                int branch=random.Next(6);int[] pattern=branch<=2?new[]{3,1,22,1,4,1,6,2,0,1,28,1,34,1,18,1,5,1,17,1,10,1,37,1,29,1,31,1,21,2}:
                    branch<=4?new[]{22,1,28,3,7,1,34,3,18,1,5,3,37,1,31,1,21,3}:new[]{4,4,6,3,17,3,10,4,21,3};
                for(int i=0;i<pattern.Length;i+=2)ModeRandomPlants(pattern[i],pattern[i+1]);
            }
        }
        public static int ModeZombieCost(ZombieKind kind)
        {
            switch(kind){case ZombieKind.Normal:case ZombieKind.Imp:return 50;case ZombieKind.Conehead:case ZombieKind.PoleVaulter:return 75;
                case ZombieKind.ScreenDoor:return 100;case ZombieKind.Buckethead:case ZombieKind.Digger:case ZombieKind.Bungee:return 125;
                case ZombieKind.Ladder:case ZombieKind.Balloon:return 150;case ZombieKind.Football:case ZombieKind.Zomboni:return 175;case ZombieKind.Pogo:return 200;
                case ZombieKind.Gargantuar:return 300;case ZombieKind.Dancing:return 350;default:return int.MaxValue;}
        }
        public bool PlaceModeZombie(ZombieKind kind,int row,int column)
        {
            var s=ModeSession;if(s==null||s.Definition.Mechanic!=ModeMechanic.IZombie||s.Phase!=ModePhase.Playing||State!=MatchState.Playing||row<0||row>=5||column<0||column>=9)return false;
            bool offered=false;foreach(var k in s.ZombieCards)if(k==kind)offered=true;if(!offered)return false;
            if(kind!=ZombieKind.Bungee&&column<ModeIZombieColumns)return false;
            if(kind==ZombieKind.Bungee&&(column>=ModeIZombieColumns||AdventurePlantAt(row,column,PlantLayer.Main)==null))return false;
            int cost=ModeZombieCost(kind);if(AdventureSunAvailable<cost)return false;
            if(!SpendAdventureSun(cost))return false;var z=SpawnAdventureZombie(kind,row,column+0.5f,s.Stage-1);
            if(kind==ZombieKind.Bungee){z.Row=row;z.X=column+0.5f;z.TargetColumn=column;}
            if(kind==ZombieKind.Gargantuar)z.ThrownImp=true;
            Raise(GameEvent.Planted);return true;
        }
        private void BeforeModeIZombie()
        {
            var s=ModeSession;
            foreach(var p in Plants)if(p.Kind==PlantKind.Sunflower)p.ActionTimer=100000;
            foreach(var z in Zombies.ToArray())if(!z.Hypnotized&&!z.Headless&&z.Kind!=ZombieKind.Bungee&&!(z.Kind==ZombieKind.Digger&&z.AbilityPhase==2)&&z.X<0.2f)
            {
                if(s.Brains[z.Row]){Zombies.Remove(z);continue;}
                z.X=0.16f;z.Eating=true;s.BrainBite[z.Row]+=0.01f;
                if(s.BrainBite[z.Row]>=0.7f){s.Brains[z.Row]=true;Zombies.Remove(z);}
            }
        }
        private void AfterModeIZombie(float dt)
        {
            var s=ModeSession;
            foreach(var id in new List<int>(s.SunflowerHealth.Keys))
            {
                var p=Plants.Find(x=>x.Id==id);float lost=s.SunflowerHealth[id]-(p==null?0:p.Health);int drops=p==null?8:Math.Min(8,(int)(lost/40f));
                int old=s.SunflowerDrops[id];for(int i=old;i<drops;i++)Suns.Add(new Sun{X=p==null?1f:p.Column+0.5f,Y=p==null?2f:p.Row+0.5f,TargetY=p==null?2f:p.Row+0.7f});s.SunflowerDrops[id]=drops;
            }
            int available=SunAmount-adventureSunReservation;foreach(var sun in Suns)available+=sun.Value;
            if(Zombies.Count==0&&available<50){s.FailureDelay+=dt;if(s.FailureDelay>=0.5f){State=MatchState.Lost;Adventure.Phase=AdventurePhase.Lost;s.Phase=ModePhase.Lost;Raise(GameEvent.Lost);}}
            else s.FailureDelay=0;
        }
    }
}
