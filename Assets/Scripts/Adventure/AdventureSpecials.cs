using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        public bool HammerAdventureZombie(int zombieId)
        {
            if(!IsAdventure || State!=MatchState.Playing || Adventure.Phase!=AdventurePhase.Playing || Adventure.Level.Mode!=AdventureMode.WhackAZombie)return false;
            Zombie z=Zombies.Find(x=>x.Id==zombieId);
            if(z==null || z.Age<0.4f)return false;
            if(z.ArmorHealth>0) {z.ArmorHealth=Math.Max(0,z.ArmorHealth-900f);Raise(GameEvent.ZombieHit);}
            else KillAdventureZombie(z);
            return true;
        }
        private void InitializeVaseStage()
        {
            Adventure.packets.Clear();Adventure.vases.Clear();Plants.Clear();Peas.Clear();Suns.Clear();
            int stage=Adventure.VaseStage;
            var contents=new List<AdventureVase>();
            AddVaseSeeds(contents,PlantKind.Peashooter,stage==1?4:5);
            if(stage>0)AddVaseSeeds(contents,PlantKind.SnowPea,5);
            AddVaseSeeds(contents,stage==2?PlantKind.HypnoShroom:PlantKind.Squash,stage==0?5:stage==1?4:5);
            AddVaseZombies(contents,ZombieKind.Normal,stage==0?4:stage==1?5:6);
            AddVaseZombies(contents,ZombieKind.Buckethead,stage==2?2:1);
            if(stage==1)AddVaseZombies(contents,ZombieKind.Football,1);
            if(stage==2) {AddVaseZombies(contents,ZombieKind.Dancing,1);AddVaseZombies(contents,ZombieKind.JackInTheBox,1);}
            var cells=new List<int>();
            for(int r=0;r<5;r++)for(int c=stage==0?6:stage==1?5:4;c<9;c++)cells.Add(r*9+c);
            foreach(var vase in contents)
            {
                int i=random.Next(cells.Count),cell=cells[i];cells.RemoveAt(i);vase.Row=cell/9;vase.Column=cell%9;Adventure.vases.Add(vase);
            }
            int show=stage+1;
            foreach(var vase in Adventure.vases) if(vase.Seed.HasValue && show-->0)vase.ShowsPlant=true;
            Adventure.Notice(AdventureNoticeKind.VaseStage,stage);
        }
        private static void AddVaseSeeds(List<AdventureVase> v,PlantKind kind,int count) {for(int i=0;i<count;i++)v.Add(new AdventureVase{Seed=kind});}
        private static void AddVaseZombies(List<AdventureVase> v,ZombieKind kind,int count) {for(int i=0;i<count;i++)v.Add(new AdventureVase{Zombie=kind});}
        public bool SmashAdventureVase(int row,int column)
        {
            if(!IsAdventure || State!=MatchState.Playing || Adventure.Phase!=AdventurePhase.Playing || Adventure.Level.Mode!=AdventureMode.Vasebreaker)return false;
            var vase=Adventure.vases.Find(v=>v.Row==row && v.Column==column);if(vase==null)return false;
            Adventure.vases.Remove(vase);
            if(vase.Seed.HasValue)AddAdventurePacket(vase.Seed.Value,row,column);
            if(vase.Zombie.HasValue)SpawnAdventureZombie(vase.Zombie.Value,row,column+0.5f,Adventure.VaseStage);
            return true;
        }
        private void AdvanceAdventureSpecials(float dt)
        {
            if(Adventure.Level.Mode==AdventureMode.Vasebreaker && Adventure.vases.Count==0 && Zombies.Count==0 && Adventure.VaseStage<2)
            {Adventure.VaseStage++;InitializeVaseStage();}
            for(int i=Adventure.nuts.Count-1;i>=0;i--)
            {
                var nut=Adventure.nuts[i];
                if(AdvanceAdventureBowlingNative(nut,dt,false))Adventure.nuts.RemoveAt(i);
            }
        }
        private void AdvanceAdventureBoss(float dt)
        {
            var a=Adventure;
            if(a.BossHealth<=0) {a.BossHeadVulnerable=false;return;}
            a.BossTimer-=dt;
            if(a.BossHeadVulnerable)
            {
                if(a.BossTimer<=0) {a.BossHeadVulnerable=false;a.BossTimer=8f;}
            }
            else if(a.BossTimer<=0)
            {
                a.BossCycle++;
                if(a.BossCycle%3==0)
                {
                    a.BossHeadVulnerable=true;a.BossTimer=14f;
                    a.balls.Add(new AdventureBossBall{Row=random.Next(5),X=9f,IsIce=a.BossCycle%2==0});
                    a.Notice(AdventureNoticeKind.BossAttack,a.BossCycle%2==0?1:0);
                }
                else if(a.BossCycle%5==0)
                {
                    int row=random.Next(4),col=random.Next(2,7);
                    foreach(Plant p in Plants.ToArray())if(Math.Abs(p.Row-row)<=1 && Math.Abs(p.Column-col)<=1)RemoveAdventurePlantAndStack(p);
                    a.Notice(AdventureNoticeKind.BossAttack,2,row,col);a.BossTimer=10f;
                }
                else
                {
                    int count=2+a.BossCycle/8;
                    for(int i=0;i<Math.Min(6,count);i++)
                    {
                        ZombieKind kind=a.BossHealth<15000f && i==0?ZombieKind.Gargantuar:a.BossHealth<30000f && i==0?ZombieKind.Catapult:
                            i%3==0?ZombieKind.Buckethead:i%3==1?ZombieKind.Conehead:ZombieKind.Normal;
                        SpawnAdventureZombie(kind,random.Next(5),9.5f+i*0.2f,a.BossCycle);
                    }
                    a.Notice(AdventureNoticeKind.BossAttack,3);a.BossTimer=10f;
                }
            }
            for(int i=a.balls.Count-1;i>=0;i--)
            {
                var ball=a.balls[i];ball.X-=1.25f*dt;
                foreach(Plant p in Plants.ToArray())if(p.Row==ball.Row && Math.Abs(p.Column+0.5f-ball.X)<0.4f)RemoveAdventurePlantAndStack(p);
                if(ball.X<0) a.balls.RemoveAt(i);
            }
        }
        private void DamageAdventureBoss(float damage)
        {
            if(Adventure.Level.Mode!=AdventureMode.Boss || Adventure.BossHealth<=0 || !Adventure.BossHeadVulnerable)return;
            Adventure.BossHealth=Math.Max(0,Adventure.BossHealth-damage);
            Raise(GameEvent.ZombieHit);
            if(Adventure.BossHealth<=0) {Adventure.balls.Clear();Adventure.Notice(AdventureNoticeKind.BossAttack,4);}
        }
    }
}
