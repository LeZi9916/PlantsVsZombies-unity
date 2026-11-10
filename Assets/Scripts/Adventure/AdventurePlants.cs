using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private void AdvanceAdventurePlants(float dt)
        {
            foreach (Plant p in Plants.ToArray())
            {
                bool producerAdvanced = false;
                int producerSunCount = Suns.Count, producerActions = p.ActionCount;
                try
                {
                if (!Plants.Contains(p)) continue;
                p.Age += dt; p.PhaseAge += dt;
                if (p.ShootAge >= 0) p.ShootAge += dt;
                if (p.WakeTimer > 0f)
                { p.WakeTimer -= dt; if (p.WakeTimer <= 0f) { p.Asleep = false; p.AwakeByCoffee = true; } }
                if (p.Asleep) continue;
                if(AdvanceAdventureNativeTimedPlant(p)) { producerAdvanced = p.Kind == PlantKind.Sunflower; continue; }
                p.SpecialTimer = Math.Max(0f,p.SpecialTimer-dt);
                if (p.Kind == PlantKind.SunShroom && p.PresentationAwakeAge >= 120f && p.PresentationAwakeAge < 121f) continue;
                p.ActionTimer -= dt;
                producerAdvanced = AdventureProducerGlow.HasNativeProductionHint(p.Kind);
                if (AdvanceAdventureUpgradePlant(p,dt)) continue;
                if(p.Kind==PlantKind.KernelPult&&!p.KernelWeaponPrepared&&p.ActionTimer<=AdventurePresentationRules.LaunchAge(p.Kind)&&(FindAdventureTarget(p.Row,p.Column+.4f,20f,false,true)!=null||CanShootAdventureBoss(p))){p.PresentationButter=random.Next(4)==0;p.KernelWeaponPrepared=true;}
                if (p.ActionTimer > 0.000001f) continue;
                float x = p.Column + 0.5f;
                switch (p.Kind)
                {
                    case PlantKind.Sunflower:
                    case PlantKind.SunShroom:
                        Suns.Add(CreateAdventurePresentationSun(p,p.Kind==PlantKind.SunShroom&&p.PresentationAwakeAge<120f?15:25));
                        p.ActionTimer=24f; break;
                    case PlantKind.Marigold:
                        p.ActionCount++; SpawnAdventureCoin(random.Next(100)<10?AdventureCoinKind.Gold:AdventureCoinKind.Silver,p.Column+0.5f,p.Row);p.ActionTimer=24f;break;
                    case PlantKind.CherryBomb:
                        DetonateAdventureCherryBomb(p); break;
                    case PlantKind.Jalapeno:
                        BlastAdventure(4.5f,p.Row,20f,0,1800f,true);
                        Adventure.ice.RemoveAll(c=>c.Row==p.Row);
                        Adventure.balls.RemoveAll(b=>b.Row==p.Row && b.IsIce);
                        if (Adventure.BossHeadVulnerable) DamageAdventureBoss(1800f);
                        AdventurePresentationExplosion(PlantKind.Jalapeno, x, p.Row); RemoveAdventurePlantAndStack(p); Raise(GameEvent.JalapenoExplosion); break;
                    case PlantKind.IceShroom:
                        NotifyAdventureIceTrap();
                        foreach (Zombie z in Zombies.ToArray()) ApplyAdventureIceTrap(z);
                        Adventure.balls.RemoveAll(b=>!b.IsIce);
                        if (Adventure.BossHeadVulnerable) DamageAdventureBoss(20f);
                        RemoveAdventurePlantAndStack(p); break;
                    case PlantKind.DoomShroom:
                        BlastAdventure(x,p.Row,3.5f,2,1800f,true); AdventurePresentationExplosion(PlantKind.DoomShroom,x,p.Row); RemoveAdventurePlantAndStack(p);
                        Adventure.craters.Add(new AdventureCellTimer {Row=p.Row,Column=p.Column,Remaining=180f}); Raise(GameEvent.Explosion); break;
                    case PlantKind.GraveBuster:
                        Adventure.graves.RemoveAll(g=>g.Row==p.Row && g.Column==p.Column); RemoveAdventurePlantAndStack(p); break;
                    case PlantKind.Blover:
                        for(int i=Zombies.Count-1;i>=0;i--) if(Zombies[i].Airborne){Zombies[i].PresentationDeath=AdventureZombieDeathPresentation.Removed;KillAdventureZombie(Zombies[i]);}
                        Adventure.FogBlownRemaining=24f; RemoveAdventurePlantAndStack(p); break;
                    case PlantKind.PotatoMine:
                        AdvanceAdventurePotatoMine(p); break;
                    case PlantKind.Squash:
                        Zombie squash=FindAdventureTarget(p.Row,x-1f,x+1.3f,false);
                        if(squash!=null) {BlastAdventure(squash.X,p.Row,0.5f,0,1800f);RemoveAdventurePlantAndStack(p);Raise(GameEvent.Explosion);} break;
                    case PlantKind.TangleKelp:
                        Zombie kelp=FindAdventureTarget(p.Row,x-0.5f,x+0.7f,false,true,PlantKind.TangleKelp);
                        if(kelp!=null) {kelp.PresentationDeath=AdventureZombieDeathPresentation.Eaten;KillAdventureZombie(kelp);RemoveAdventurePlantAndStack(p);} break;
                    case PlantKind.Chomper:
                        Zombie meal=FindAdventureTarget(p.Row,x-0.2f,x+1.2f,false,false,PlantKind.Chomper);
                        if(meal!=null)
                        {if(meal.Kind==ZombieKind.Gargantuar || meal.Kind==ZombieKind.Zomboni || meal.Kind==ZombieKind.Catapult) {HitAdventureZombie(meal,40f,true);p.ActionTimer=1.5f;}
                         else {meal.PresentationDeath=AdventureZombieDeathPresentation.Eaten;KillAdventureZombie(meal);p.ActionTimer=42f;} p.ShootAge=0;} break;
                    case PlantKind.Spikeweed:
                        foreach(Zombie z in Zombies.ToArray()) if(!z.Airborne && !z.Hypnotized && z.Row==p.Row && Math.Abs(z.X-x)<0.6f)
                        {if(z.Kind==ZombieKind.Zomboni || z.Kind==ZombieKind.Catapult) {KillAdventureZombie(z);RemoveAdventurePlantAndStack(p);break;} HitAdventureZombie(z,20f,true);}
                        p.ActionTimer=1f; break;
                    case PlantKind.MagnetShroom:
                        Zombie metal=Zombies.Find(z=>!z.Hypnotized && Math.Abs(z.Row-p.Row)<=2 && Math.Abs(z.X-x)<=3f &&
                            (z.ArmorHealth>0f && (z.Kind==ZombieKind.Buckethead || z.Kind==ZombieKind.Football || z.Kind==ZombieKind.Digger) ||
                             (z.Kind==ZombieKind.ScreenDoor || z.Kind==ZombieKind.Ladder) && z.ShieldHealth>0f ||
                             (z.Kind==ZombieKind.Pogo || z.Kind==ZombieKind.Ladder || z.Kind==ZombieKind.Digger || z.Kind==ZombieKind.JackInTheBox) && !z.UsedAbility));
                        if(metal!=null) {metal.ArmorHealth=0;metal.ShieldHealth=0;metal.UsedAbility=true;metal.Submerged=false;metal.AbilityPhase=2;if(metal.Kind==ZombieKind.Pogo)metal.Speed=0.18f;if(metal.Kind==ZombieKind.JackInTheBox)metal.AbilityTimer=float.MaxValue;p.ActionTimer=15f;} break;
                    case PlantKind.Peashooter: case PlantKind.SnowPea: case PlantKind.Repeater: case PlantKind.PuffShroom:
                    case PlantKind.SeaShroom: case PlantKind.FumeShroom: case PlantKind.ScaredyShroom: case PlantKind.Cactus:
                    case PlantKind.CabbagePult: case PlantKind.KernelPult: case PlantKind.MelonPult:
                        float range=p.Kind==PlantKind.PuffShroom || p.Kind==PlantKind.SeaShroom ? 3f : p.Kind==PlantKind.FumeShroom ? 4f : 20f;
                        bool air=p.Kind==PlantKind.Cactus;
                        if(p.Kind==PlantKind.ScaredyShroom && Zombies.Exists(z=>!z.Hypnotized && Math.Abs(z.Row-p.Row)<=1 && Math.Abs(z.X-x)<1.5f)) break;
                        if(FindAdventureTarget(p.Row,x-0.1f,x+range,air)!=null || CanShootAdventureBoss(p))
                        {
                            bool lob=p.Kind==PlantKind.CabbagePult || p.Kind==PlantKind.KernelPult || p.Kind==PlantKind.MelonPult;
                            // Straight projectiles from the sloped left roof strike the slope, while lobbers clear it.
                            if(!Adventure.Level.HasRoof || p.Column>=5 || lob || p.Kind==PlantKind.FumeShroom)
                            {
                                var shot=FireAdventurePea(p,p.Row,false,lob ? p.Kind==PlantKind.MelonPult ? 80f : p.Kind==PlantKind.CabbagePult ? 40f : 20f : 20f);
                                shot.IsSnow=p.Kind==PlantKind.SnowPea;shot.HitsAir=air;shot.Range=range;shot.Lobbed=lob;
                                shot.PiercesShield=p.Kind==PlantKind.FumeShroom || lob;shot.PiercesZombies=p.Kind==PlantKind.FumeShroom;
                                shot.Splash=p.Kind==PlantKind.MelonPult;
                                if(p.Kind==PlantKind.KernelPult){shot.Butter=p.KernelWeaponPrepared?p.PresentationButter:random.Next(4)==0;p.KernelWeaponPrepared=false;if(shot.Butter)shot.Damage=40;}
                                if(p.Kind==PlantKind.Repeater) FireAdventurePea(p,p.Row,false,20f).X-=0.3f;
                            }
                            p.ActionTimer=lob ? 2.9f : 1.425f;
                        } break;
                    case PlantKind.Threepeater:
                        bool target=false;
                        for(int r=Math.Max(0,p.Row-1);r<=Math.Min(RowCount-1,p.Row+1);r++) if(FindAdventureTarget(r,x-0.1f,20f,false)!=null) target=true;
                        if(target) {for(int r=Math.Max(0,p.Row-1);r<=Math.Min(RowCount-1,p.Row+1);r++) FireAdventurePea(p,r,false,20f);p.ActionTimer=1.425f;} break;
                    case PlantKind.SplitPea:
                        bool front=FindAdventureTarget(p.Row,x,20f,false)!=null, back=FindAdventureTarget(p.Row,-1f,x,false)!=null;
                        if(front) FireAdventurePea(p,p.Row,false,20f);
                        if(back) {FireAdventurePea(p,p.Row,true,20f);FireAdventurePea(p,p.Row,true,20f).X+=0.3f;}
                        if(front||back) p.ActionTimer=1.425f; break;
                    case PlantKind.Starfruit:
                        if(Zombies.Exists(z=>!z.Hypnotized && !z.Airborne && z.X<10f))
                        {
                            float[] dx={-4.5f,0,0,3.5f,3.5f},dy={0,-4.5f,4.5f,-2.5f,2.5f};
                            for(int i=0;i<5;i++){var star=FireAdventurePea(p,p.Row,false,20f);star.X=x;star.VelocityX=dx[i];star.VelocityY=dy[i];star.Backward=dx[i]<0;}
                            p.ActionTimer=1.425f;
                        } break;
                }
                }
                finally
                {
                    if (AdventureProducerGlow.HasNativeProductionHint(p.Kind))
                        AdventureProducerGlow.Tick(p, producerAdvanced, p.Kind == PlantKind.Sunflower && p.Timing.HasValue ? p.Timing.Value.Countdown : AdventureProducerGlow.CounterFromSeconds(p.ActionTimer),
                            p.Kind == PlantKind.Marigold ? p.ActionCount > producerActions : Suns.Count > producerSunCount);
                }
            }
        }
        private Pea FireAdventurePea(Plant p,int row,bool back,float damage)
        {
            var shot=new Pea {SourceKind=p.Kind,SourceIsImitated=p.IsImitated,Row=row,FlightRow=row,X=p.Column+(back?0.15f:0.85f),StartX=p.Column+0.5f,VelocityX=back?-4.5f:4.5f,Damage=damage,Backward=back};
            Peas.Add(shot);AdventurePresentationShot(p,shot);p.ShootAge=0;Raise(GameEvent.Shoot);return shot;
        }
        private Zombie FindAdventureTarget(int row,float min,float max,bool air,bool submerged=false,PlantKind sourceKind=PlantKind.Peashooter)
        {
            Zombie best=null;
            foreach(Zombie z in Zombies) if(!z.Hypnotized && AdventureZombieTargetingRules.CanBeAutoTargeted(z,sourceKind) && z.DropRemaining<=0f && z.Kind!=ZombieKind.Bungee && z.Row==row && z.X>=min && z.X<=max && (!z.Airborne||air) && (!z.Submerged||submerged))
                if(best==null || z.X<best.X) best=z;
            return best;
        }
        private bool CanShootAdventureBoss(Plant p)
        {return Adventure.Level.Mode==AdventureMode.Boss && Adventure.BossHeadVulnerable && p.Row>=1 && p.Row<=3 && Adventure.BossHealth>0;}
        private void BlastAdventure(float x,int row,float width,int rows,float damage,bool burned=false,uint damageFlags=0)
        {foreach(Zombie z in Zombies.ToArray()) if(!z.Hypnotized && Math.Abs(z.Row-row)<=rows && Math.Abs(z.X-x)<=width)HitAdventureExplosionZombie(z,damage,burned,damageFlags);}
        private void AdvanceAdventureProjectiles(float dt)
        {
            for(int i=Peas.Count-1;i>=0;i--)
            {
                Pea p=Peas[i];UpdateAdventureHomingVelocity(p,dt);float before=p.X;p.X+=p.VelocityX*dt;p.FlightRow+=p.VelocityY*dt;AdvanceAdventureProjectilePresentation(p,dt);
                p.Row=(int)Math.Round(p.FlightRow);
                if(!p.Lobbed && !p.Homing && p.VelocityY==0)
                {
                    Plant torch=Plants.Find(t=>t.Kind==PlantKind.Torchwood && t.Row==p.Row && t.Column!=p.LastTorchColumn && Math.Abs(p.X-(t.Column+0.5f))<0.2f);
                    if(torch!=null) {p.LastTorchColumn=torch.Column;if(p.IsSnow)p.IsSnow=false;else {p.Fire=true;p.Damage=40;p.Splash=true;}}
                }
                UpdateAdventureSnowTrail(p);
                Zombie hit=null;
                foreach(Zombie z in Zombies)
                    if(p.PresentationCanImpact && (!p.Homing || z.Id==p.TargetZombieId) && !z.Hypnotized && Math.Abs(z.Row-p.FlightRow)<0.45f && !p.HitIds.Contains(z.Id) && (!z.Airborne||p.HitsAir) && (!z.Submerged||p.Lobbed) &&
                        z.X>=Math.Min(before,p.X)-0.25f && z.X<=Math.Max(before,p.X)+0.25f)
                        if(hit==null || Math.Abs(z.X-before)<Math.Abs(hit.X-before)) hit=z;
                bool remove=false;
                if(hit!=null)
                {
                    AdventurePresentationImpact(p,hit);
                    p.HitIds.Add(hit.Id);
                    if(p.HitsAir && hit.Airborne) {hit.Airborne=false;hit.Speed=0.18f;}
                    ApplyAdventureProjectileCold(p,hit);
                    if(p.Butter) hit.FrozenRemaining=Math.Max(hit.FrozenRemaining,4f);
                    HitAdventureZombie(hit,p.Damage,p.PiercesShield);
                    if(p.Splash)
                        foreach(Zombie other in Zombies.ToArray())
                            if(other!=hit && !other.Hypnotized && !other.Airborne && Math.Abs(other.Row-hit.Row)<=(p.Fire?0:1) && Math.Abs(other.X-hit.X)<=(p.Fire?0.5f:1f))
                            {if(p.ChillSplash)ApplyAdventureChill(other);HitAdventureZombie(other,p.Fire?20f:26f,p.PiercesShield);}
                    remove=!p.PiercesZombies;
                }
                if(!remove && p.X>=8.25f && p.Lobbed && p.PresentationCanImpact && Adventure.BossHeadVulnerable && p.Row>=1 && p.Row<=3)
                {AdventurePresentationImpact(p,null);DamageAdventureBoss(p.Damage);remove=true;}
                if(remove || p.X>12f || p.X<-2f || p.FlightRow<-1f || p.FlightRow>RowCount || Math.Abs(p.X-p.StartX)>p.Range)
                {StopAdventureSnowTrail(p);Peas.RemoveAt(i);}
            }
        }
        private void HitAdventureZombie(Zombie z,float damage,bool bypassShield=false,uint damageFlags=0)
        {
            if(!Zombies.Contains(z)) return;
            AdventurePresentationZombieHit(z, damage, bypassShield, damageFlags);
            if(!bypassShield) {float shield=Math.Min(z.ShieldHealth,damage);z.ShieldHealth-=shield;damage-=shield;}
            float armor=Math.Min(z.ArmorHealth,damage);z.ArmorHealth-=armor;z.Health-=damage-armor;
            AdventurePresentationZombieEquipmentDamaged(z, damageFlags);
            AdventurePresentationZombieBodyDamaged(z, damageFlags);
            Raise(GameEvent.ZombieHit);
            if(z.Health<=0) KillAdventureZombie(z);
        }
        private void KillAdventureZombie(Zombie z)
        {
            if(!Zombies.Remove(z)) return;
            if (ModeSession == null) RememberAdventureRewardPosition(z);
            AdventurePresentationZombieDeath(z);
            if(z.PresentationDeath!=AdventureZombieDeathPresentation.Removed)DropAdventureLoot(z);
            Raise(GameEvent.ZombieDied);
        }
    }
}
