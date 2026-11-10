using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private Zombie SpawnAdventureZombie(ZombieKind kind,int row,float x,int wave)
        {
            float health=kind==ZombieKind.Yeti?1350f:kind==ZombieKind.Bungee?450f:kind==ZombieKind.Ladder?500f:kind==ZombieKind.Gargantuar?3000f:kind==ZombieKind.Zomboni?1350f:kind==ZombieKind.Catapult?850f:
                kind==ZombieKind.PoleVaulter || kind==ZombieKind.DolphinRider || kind==ZombieKind.JackInTheBox || kind==ZombieKind.Pogo || kind==ZombieKind.Dancing ? 500f:270f;
            float armor=kind==ZombieKind.Conehead?370f:kind==ZombieKind.Buckethead?1100f:kind==ZombieKind.Football?1400f:kind==ZombieKind.Digger?100f:kind==ZombieKind.Bobsled?300f:0f;
            float shield=kind==ZombieKind.ScreenDoor?1100f:kind==ZombieKind.Newspaper?150f:kind==ZombieKind.Ladder?500f:0f;
            var z=new Zombie {Id=++Adventure.ZombieId,Kind=kind,Row=row,X=x,Wave=wave,Health=health,StartingHealth=health,
                ArmorHealth=armor,ShieldHealth=shield,Airborne=kind==ZombieKind.Balloon,
                Submerged=kind==ZombieKind.Snorkel || kind==ZombieKind.Digger,AbilityTimer=kind==ZombieKind.JackInTheBox?10f+(float)random.NextDouble()*15f:kind==ZombieKind.Dancing?4f:kind==ZombieKind.Bungee?4f:5f};
            if(kind==ZombieKind.Bungee)
            {
                Plant target=Plants.Count>0?Plants[random.Next(Plants.Count)]:null;
                z.Row=target==null?row:target.Row;z.TargetColumn=target==null?random.Next(2,9):target.Column;z.X=z.TargetColumn+0.5f;
            }
            if(Adventure.Level.Mode==AdventureMode.LittleTrouble) {z.IsSmall=true;z.Health=(int)z.Health/4;z.ArmorHealth=(int)z.ArmorHealth/4;z.ShieldHealth=(int)z.ShieldHealth/4;z.StartingHealth=z.Health;}
            InitializeAdventureZombieNative(z, x >= 9.5f && kind != ZombieKind.Bungee);
            Zombies.Add(z);SpawnedZombies++;return z;
        }
        private void DropAdventureZombie(ZombieKind kind,int row,int column,int wave)
        {
            if(Plants.Exists(p=>p.Kind==PlantKind.UmbrellaLeaf && Math.Abs(p.Row-row)<=1 && Math.Abs(p.Column-column)<=1))return;
            var passenger=SpawnAdventureZombie(kind,row,column+0.5f,wave);passenger.DropRemaining=4f;
            var carrier=SpawnAdventureZombie(ZombieKind.Bungee,row,column+0.5f,wave);
            carrier.BungeeCarrier=true;carrier.Row=row;carrier.TargetColumn=column;carrier.X=column+0.5f;carrier.LinkedZombieId=passenger.Id;
            Adventure.Notice(AdventureNoticeKind.RoofDrop,(int)kind,row,column);
        }
        private void AdvanceAdventureZombies(float dt)
        {
            AdvanceAdventurePlantHitCounters(dt);
            foreach(Zombie z in Zombies.ToArray())
            {
                if(!Zombies.Contains(z)) continue;
                z.Age+=dt;
                if(z.Headless)
                {
                    if(random.Next(5)==0)z.Health-=z.StartingHealth>=500f?3f:1f;
                    if(z.Health<=0){KillAdventureZombie(z);continue;}
                }
                if(z.DropRemaining>0f) {z.DropRemaining-=dt;continue;}
                if(z.FrozenRemaining>0f) {z.FrozenRemaining=Math.Max(0,z.FrozenRemaining-dt);continue;}
                float slow=z.ChillRemaining>0f?0.5f:1f;
                z.ChillRemaining=Math.Max(0,z.ChillRemaining-dt);
                if(AdvanceAdventureGarlicBite(z,dt))continue;
                // Head loss does not call StopEating in PC. Preserve a latched meal, but deal no new bite damage.
                if(z.Headless&&z.Eating)continue;
                if(z.PresentationNewspaperGaspRemaining>0f)continue;
                z.AbilityTimer-=dt*slow;
                if(z.Kind==ZombieKind.Bungee)
                {
                    if(Plants.Exists(p=>p.Kind==PlantKind.UmbrellaLeaf && Math.Abs(p.Row-z.Row)<=1 && Math.Abs(p.Column-z.TargetColumn)<=1))
                    {Zombie passenger=Zombies.Find(other=>other.Id==z.LinkedZombieId);if(passenger!=null){passenger.PresentationDeath=AdventureZombieDeathPresentation.Removed;KillAdventureZombie(passenger);}z.PresentationDeath=AdventureZombieDeathPresentation.Removed;KillAdventureZombie(z);continue;}
                    if(z.AbilityTimer<=0)
                    {if(!z.BungeeCarrier){Plant victim=PlantAt(z.Row,z.TargetColumn);if(victim!=null)RemoveAdventurePlantAndStack(victim);}z.PresentationDeath=AdventureZombieDeathPresentation.Removed;KillAdventureZombie(z);}
                    continue;
                }
                if(z.Kind==ZombieKind.JackInTheBox && z.AbilityTimer<=0)
                {
                    foreach(Plant p in Plants.ToArray()) if(Math.Abs(p.Row-z.Row)<=1 && Math.Abs(p.Column+0.5f-z.X)<1.5f) RemoveAdventurePlantAndStack(p);
                    KillAdventureZombie(z);Raise(GameEvent.Explosion);continue;
                }
                if(z.Kind==ZombieKind.Dancing && z.AbilityTimer<=0 && !z.Hypnotized)
                {
                    for(int dr=-1;dr<=1;dr++) for(int dx=-1;dx<=1;dx++) if(Math.Abs(dr)+Math.Abs(dx)==1)
                    {
                        int row=z.Row+dr;float x=z.X+dx*0.9f;
                        if(Adventure.Level.IsLawnRow(row) && !Adventure.Level.IsWaterRow(row) && x>0.5f &&
                            !Zombies.Exists(b=>b.Kind==ZombieKind.BackupDancer && b.Row==row && Math.Abs(b.X-x)<1.2f))
                            SpawnAdventureZombie(ZombieKind.BackupDancer,row,x,z.Wave);
                    }
                    z.AbilityTimer=12f;
                }
                if(z.Kind==ZombieKind.Gargantuar && !z.ThrownImp && z.Health<=1500 && z.X>2.5f && !z.Hypnotized)
                {z.ThrownImp=true;SpawnAdventureZombie(ZombieKind.Imp,z.Row,Math.Max(1.2f,z.X-4f),z.Wave);}
                if(z.Kind==ZombieKind.Newspaper && z.ShieldHealth<=0 && !z.UsedAbility) {z.UsedAbility=true;PickAdventureZombieNativeSpeed(z,31);}
                if(z.Kind==ZombieKind.Digger && z.AbilityPhase==0)
                {
                    z.X-=z.Speed*dt; // PC tunnelling uses mVelX directly, without the ground/chill branch.
                    if(z.X<=0.5f) {z.Submerged=false;z.AbilityPhase=1;z.AbilityTimer=2f;}
                    continue;
                }
                if(z.Kind==ZombieKind.Digger && z.AbilityPhase==1)
                {if(z.AbilityTimer<=0){z.AbilityPhase=2;PickAdventureZombieNativeSpeed(z,37);}else continue;}
                if(z.Kind==ZombieKind.Catapult && z.X<=7.5f && z.AbilityPhase<12 && !z.Hypnotized)
                {
                    if(z.AbilityTimer<=0)
                    {
                        Plant victim=null;
                        foreach(Plant p in Plants) if(p.Row==z.Row && (victim==null || p.Column<victim.Column)) victim=p;
                        if(victim!=null && !Plants.Exists(p=>p.Kind==PlantKind.UmbrellaLeaf && Math.Abs(p.Row-victim.Row)<=1 && Math.Abs(p.Column-victim.Column)<=1))
                        {victim.Health-=80f;RecordAdventurePlantHit(victim);if(victim.Health<=0)RemoveAdventurePlantAndStack(victim);}
                        z.AbilityPhase++;z.AbilityTimer=5f;
                    }
                    continue;
                }
                if(z.Kind==ZombieKind.Zomboni)
                {
                    int col=(int)Math.Floor(z.X);
                    if(col>=0 && col<9 && !Adventure.ice.Exists(c=>c.Row==z.Row && c.Column==col))
                        Adventure.ice.Add(new AdventureCellTimer{Row=z.Row,Column=col,Remaining=30f});
                }
                Zombie opponent=Zombies.Find(other=>other!=z && other.Row==z.Row && other.Hypnotized!=z.Hypnotized && !other.Airborne && Math.Abs(other.X-z.X)<0.5f);
                if(opponent!=null && (z.Hypnotized || opponent.Hypnotized)&&!z.Headless)
                {
                    if(AdvanceAdventureNativeEating(z,null,dt,true))
                    {int bites=PvZReplica.Adventure.AdventureZombieBiteRules.EatingChecks(z.Age,dt,z.ChillRemaining>0);if(bites>0)HitAdventureZombie(opponent,4f*bites,true,9U);}
                    continue;
                }
                Plant target=null;
                bool vault=z.Kind==ZombieKind.PoleVaulter || z.Kind==ZombieKind.DolphinRider || z.Kind==ZombieKind.Pogo;
                var attack=vault&&!z.UsedAbility?AdventureBiteAttack.Vault:z.Kind==ZombieKind.Zomboni||z.Kind==ZombieKind.Catapult?AdventureBiteAttack.DriveOver:AdventureBiteAttack.Chew;
                if(!z.Airborne&&!z.Hypnotized&&!z.Headless)
                {
                    if(z.Kind==ZombieKind.Ladder&&!z.UsedAbility)target=FindAdventureNativePlantTarget(z,AdventureBiteAttack.Ladder);
                    if(target==null)target=FindAdventureNativePlantTarget(z,attack);
                }
                if(z.Kind==ZombieKind.Snorkel) z.Submerged=target==null;
                if(target!=null && target.HasLadder && target.Layer!=PlantLayer.Support) target=null;
                bool tallBarrier=target!=null && (target.Kind==PlantKind.TallNut || AdventurePlantAt(target.Row,target.Column,PlantLayer.Main)!=null && AdventurePlantAt(target.Row,target.Column,PlantLayer.Main).Kind==PlantKind.TallNut);
                if(target!=null && vault && !z.UsedAbility && !tallBarrier && z.AbilityTimer>0)continue;
                if(target!=null && vault && !z.UsedAbility && !tallBarrier)
                {
                    z.X-=1.1f;z.AbilityTimer=0.75f;
                    if(z.Kind!=ZombieKind.Pogo) {z.UsedAbility=true;RefreshAdventureZombieNativeSpeed(z);}
                    continue;
                }
                if(target!=null&&z.Kind==ZombieKind.PoleVaulter&&!z.UsedAbility&&tallBarrier){z.UsedAbility=true;RefreshAdventureZombieNativeSpeed(z);}
                if(target!=null && z.Kind==ZombieKind.Pogo && tallBarrier) {z.UsedAbility=true;RefreshAdventureZombieNativeSpeed(z);}
                if(target!=null && z.Kind==ZombieKind.Ladder && !z.UsedAbility && (target.Kind==PlantKind.Wallnut || target.Kind==PlantKind.TallNut || target.Kind==PlantKind.Pumpkin))
                {target.HasLadder=true;z.UsedAbility=true;z.ShieldHealth=0;RefreshAdventureZombieNativeSpeed(z);z.X-=0.8f;continue;}
                if(!z.Headless&&AdvanceAdventureNativeEating(z,target,dt))continue;
                if(z.Kind!=ZombieKind.Gargantuar&&z.Kind!=ZombieKind.Zomboni&&z.Kind!=ZombieKind.Catapult)target=null;
                z.Eating=target!=null;
                if(target!=null)
                {
                    if(z.Kind==ZombieKind.Zomboni || z.Kind==ZombieKind.Catapult)
                    {if(target.Kind==PlantKind.Spikerock){HitAdventureZombie(z,1800f,true);target.Health-=50;target.VehicleHitsTaken++;if(target.Health<=0)RemoveAdventurePlantAndStack(target);}else RemoveAdventurePlantAndStack(target);continue;}
                    z.BiteTimer-=dt*slow;
                    if(z.BiteTimer<=0)
                    {
                        if(target.Kind==PlantKind.HypnoShroom && !target.Asleep && z.Kind!=ZombieKind.Gargantuar)
                        {z.Hypnotized=true;z.Submerged=false;RemoveAdventurePlantAndStack(target);continue;}
                        target.Health-=z.Kind==ZombieKind.Gargantuar?target.Kind==PlantKind.Spikerock?50f:9999f:4f;
                        z.BiteTimer=z.Kind==ZombieKind.Gargantuar?2f:0.04f;
                        if(target.Kind==PlantKind.Garlic && z.Kind!=ZombieKind.Gargantuar)
                        {
                            var rows=new List<int>();
                            for(int r=Math.Max(0,z.Row-1);r<=Math.Min(RowCount-1,z.Row+1);r++)
                                if(r!=z.Row && Adventure.Level.IsLawnRow(r) && Adventure.Level.IsWaterRow(r)==Adventure.Level.IsWaterRow(z.Row)) rows.Add(r);
                            if(rows.Count>0) z.Row=rows[random.Next(rows.Count)];
                        }
                        if(target.Health<=0) RemoveAdventurePlantAndStack(target);
                    }
                }
                else
                {
                    z.BiteTimer=0;
                    float direction=z.Hypnotized || z.Kind==ZombieKind.Digger && z.AbilityPhase==2 ? 1f:-1f;
                    z.X+=direction*AdventurePresentationWalkingDistance(z, dt, z.Speed * dt * slow);
                }
                if(z.X>12f && (z.Hypnotized || z.Kind==ZombieKind.Digger && z.AbilityPhase==2)) {z.PresentationDeath=AdventureZombieDeathPresentation.Removed;KillAdventureZombie(z);continue;}
                if(!z.Hypnotized && z.X<0.15f && Mowers[z.Row].Available)
                {Mowers[z.Row].Available=false;Mowers[z.Row].Active=true;Raise(GameEvent.Mower);}
                else if(!z.Hypnotized && z.X<-0.65f && !Mowers[z.Row].Active)
                {State=MatchState.Lost;Adventure.Phase=AdventurePhase.Lost;Raise(GameEvent.Lost);return;}
            }
        }
        private void AdvanceAdventureMowers(float dt)
        {
            foreach(Mower m in Mowers)
            {
                if(!m.Active)continue;
                m.X+=5f*dt;
                foreach(Zombie z in Zombies.ToArray()) if(!z.Hypnotized && z.Row==m.Row && Math.Abs(z.X-m.X)<0.7f){z.PresentationDeath=AdventureZombieDeathPresentation.Mowed;KillAdventureZombie(z);}
                if(m.X>10f || Adventure.Level.IsWaterRow(m.Row) && !Rules.AdventurePoolCleaners && m.X>1.5f) m.Active=false;
            }
        }
    }
}
