using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        // Native counters are interpreted at the replica's 100 Hz. Animation/loading/flight durations are approximations.
        public const float AdventureCobFiringSeconds=2.06f;
        public const float AdventureCobLoadingSeconds=2.5f;
        public const float AdventureCobFlightSeconds=AdventurePresentationCobFlightSeconds;

        private bool AdvanceAdventureUpgradePlant(Plant p,float dt)
        {
            if(p.Kind==PlantKind.Imitater)
            {
                p.ImitaterTransformRemaining=Math.Max(0,p.ImitaterTransformRemaining-dt);
                if(p.ImitaterTransformRemaining<=0.000001f)MorphAdventureImitater(p);
                return true;
            }
            if(p.Kind==PlantKind.CobCannon) {AdvanceAdventureCannon(p,dt);return true;}
            if(p.Kind==PlantKind.GatlingPea || p.Kind==PlantKind.GloomShroom || p.Kind==PlantKind.Cattail)
            {
                if(p.UpgradeBurstTicks>0)
                {
                    int tick=--p.UpgradeBurstTicks;
                    if(p.Kind==PlantKind.GatlingPea && (tick==68 || tick==51 || tick==35 || tick==18))
                    {
                        if(!Adventure.Level.HasRoof || p.Column>=5)FireAdventurePea(p,p.Row,false,20f);
                    }
                    else if(p.Kind==PlantKind.GloomShroom && (tick==126 || tick==98 || tick==70 || tick==42))
                    {
                        foreach(Zombie z in Zombies.ToArray())if(IsUpgradeAreaTarget(z,p))HitAdventureZombie(z,20f,true);
                        p.ActionCount++;Raise(GameEvent.Shoot);
                    }
                    else if(p.Kind==PlantKind.Cattail && (tick==19 || tick==1))
                    {
                        Zombie target=FindAdventureCattailTarget(p.Column+0.5f,p.Row);
                        if(target!=null)
                        {Pea dart=FireAdventurePea(p,p.Row,false,20f);dart.Homing=true;dart.TargetZombieId=target.Id;dart.HitsAir=true;}
                    }
                }
                if(p.ActionTimer<=0.000001f)
                {
                    p.ActionTimer=(DefinitionFor(p.Kind).ActionPeriodRaw-random.Next(15))*0.01f;
                    bool target=p.Kind==PlantKind.GatlingPea?FindAdventureTarget(p.Row,p.Column+0.4f,20f,false)!=null:
                        p.Kind==PlantKind.Cattail?FindAdventureCattailTarget(p.Column+0.5f,p.Row)!=null:Zombies.Exists(z=>IsUpgradeAreaTarget(z,p));
                    if(target){p.UpgradeBurstTicks=p.Kind==PlantKind.GloomShroom?200:p.Kind==PlantKind.GatlingPea?100:50;p.ShootAge=0;}
                }
                return true;
            }
            if(p.Kind==PlantKind.TwinSunflower)
            {
                if(p.ActionTimer<=0.000001f)
                {
                    Suns.Add(CreateAdventurePresentationSun(p,25));
                    Suns.Add(CreateAdventurePresentationSun(p,25));
                    p.ActionCount++;p.ActionTimer=(2350+random.Next(151))*0.01f;
                }
                return true;
            }
            if(p.Kind==PlantKind.WinterMelon)
            {
                if(p.ActionTimer<=0.000001f && (FindAdventureTarget(p.Row,p.Column+0.4f,20f,false)!=null || CanShootAdventureBoss(p)))
                {
                    Pea melon=FireAdventurePea(p,p.Row,false,80f);melon.Lobbed=true;melon.PiercesShield=true;
                    melon.Splash=true;melon.IsSnow=true;melon.ChillSplash=true;p.ActionTimer=2.9f;
                }
                return true;
            }
            if(p.Kind==PlantKind.Spikerock)
            {
                if(p.ActionTimer<=0.000001f)
                {
                    foreach(Zombie z in Zombies.ToArray())if(!z.Hypnotized && !z.Airborne && !z.Submerged && z.DropRemaining<=0 && z.Kind!=ZombieKind.Bungee && z.Row==p.Row && Math.Abs(z.X-(p.Column+0.5f))<0.6f)
                    {
                        if(z.Kind==ZombieKind.Zomboni || z.Kind==ZombieKind.Catapult)
                        {HitAdventureZombie(z,1800f,true);p.Health-=50f;p.VehicleHitsTaken++;if(p.Health<=0){RemoveAdventurePlantAndStack(p);break;}}
                        else HitAdventureZombie(z,20f,true);
                    }
                    p.ActionTimer=0.5f;
                }
                return true;
            }
            if(p.Kind==PlantKind.GoldMagnet)
            {
                if(p.ActionTimer<=0.000001f)
                {
                    var choices=new List<AdventureCoin>();
                    foreach(var coin in Adventure.coins)if(!coin.IsBeingCollected && coin.Age>=0.5f && coin.ValueUnits>0)choices.Add(coin);
                    choices.Sort((a,b)=>CoinDistance(a,p).CompareTo(CoinDistance(b,p)));
                    int count=Math.Min(5,choices.Count);
                    for(int i=0;i<count;i++){choices[i].IsBeingCollected=true;choices[i].CollectorPlantId=p.Id;choices[i].CollectionRemaining=0.5f;}
                    if(count>0) {p.ActionCount++;p.ActionTimer=2f+random.Next(101)*0.01f;}
                    else p.ActionTimer=0.5f;
                }
                return true;
            }
            return false;
        }
        private static float CoinDistance(AdventureCoin c,Plant p)
        {float x=c.X-(p.Column+0.5f),y=c.Row-p.Row;return x*x+y*y;}
        private static bool IsUpgradeAreaTarget(Zombie z,Plant p)
        {return !z.Hypnotized && AdventureZombieTargetingRules.CanBeAutoTargeted(z,p.Kind) && !z.Airborne && !z.Submerged && z.DropRemaining<=0 && z.Kind!=ZombieKind.Bungee && Math.Abs(z.Row-p.Row)<=1 && Math.Abs(z.X-(p.Column+0.5f))<=1.5f;}
        private Zombie FindAdventureCattailTarget(float x,float row)
        {
            Zombie best=null;float distance=float.MaxValue;
            foreach(Zombie z in Zombies)
            {
                if(z.Hypnotized || !AdventureZombieTargetingRules.CanBeAutoTargeted(z,PlantKind.Cattail) || z.Submerged || z.DropRemaining>0 || z.Kind==ZombieKind.Bungee || z.X<0 || z.X>12f)continue;
                float dx=z.X-x,dy=z.Row-row,d=dx*dx+dy*dy;
                if(best==null || z.Airborne && !best.Airborne || z.Airborne==best.Airborne && d<distance)
                {best=z;distance=d;}
            }
            return best;
        }
        private void UpdateAdventureHomingVelocity(Pea p,float dt)
        {
            if(!p.Homing)return;
            Zombie target=Zombies.Find(z=>z.Id==p.TargetZombieId && !z.Hypnotized && AdventureZombieTargetingRules.CanBeAutoTargeted(z,PlantKind.Cattail) && !z.Submerged && z.DropRemaining<=0);
            if(target==null)target=FindAdventureCattailTarget(p.X,p.FlightRow);
            if(target==null){p.TargetZombieId=0;return;}
            p.TargetZombieId=target.Id;
            float dx=target.X-p.X,dy=target.Row-p.FlightRow,d=(float)Math.Sqrt(dx*dx+dy*dy);
            float speed=d<=0.000001f?0:Math.Min(5f,d/dt);
            p.VelocityX=d<=0.000001f?0:dx/d*speed;p.VelocityY=d<=0.000001f?0:dy/d*speed;
        }
        private void MorphAdventureImitater(Plant p)
        {
            PlantKind? target=p.ImitaterTarget;Plants.Remove(p);
            if(!target.HasValue)return;
            if(target.Value==PlantKind.CoffeeBean)
            {Plant main=AdventurePlantAt(p.Row,p.Column,PlantLayer.Main);if(main!=null && main.Asleep)PlaceAdventurePlant(target.Value,p.Row,p.Column);}
            else
            {
                Plant transformed=PlaceAdventurePlant(target.Value,p.Row,p.Column);
                transformed.Id=p.Id;transformed.IsImitated=true;transformed.ImitaterTarget=target;
            }
            Adventure.Notice(AdventureNoticeKind.ImitaterMorphed,(int)target.Value,p.Row,p.Column);
        }
        public bool TryAimAdventureCobCannon(int row,int column)
        {Plant p=IsAdventure?AdventurePlantAt(row,column,PlantLayer.Main):null;return p!=null && TryAimAdventureCobCannon(p.Id);}
        public bool TryAimAdventureCobCannon(int plantId)
        {
            if(!IsAdventure || State!=MatchState.Playing || Adventure.Phase!=AdventurePhase.Playing)return false;
            Plant cannon=Plants.Find(p=>p.Id==plantId && p.Kind==PlantKind.CobCannon);
            if(cannon==null || cannon.CannonPhase!=CobCannonPhase.Ready)return false;
            CancelAdventureCobCannonAim();cannon.CannonPhase=CobCannonPhase.Aiming;Adventure.SelectedCobCannonId=plantId;
            Adventure.Notice(AdventureNoticeKind.CobAimStarted,plantId,cannon.Row,cannon.Column);return true;
        }
        public bool CancelAdventureCobCannonAim()
        {
            if(!IsAdventure || !Adventure.SelectedCobCannonId.HasValue)return false;
            Plant p=Plants.Find(c=>c.Id==Adventure.SelectedCobCannonId.Value && c.Kind==PlantKind.CobCannon);
            if(p!=null && p.CannonPhase==CobCannonPhase.Aiming)p.CannonPhase=CobCannonPhase.Ready;
            Adventure.SelectedCobCannonId=null;return true;
        }
        public bool TryFireAdventureCobCannon(float targetX,float targetRow)
        {
            if(!IsAdventure || State!=MatchState.Playing || Adventure.Phase!=AdventurePhase.Playing || !Adventure.SelectedCobCannonId.HasValue ||
                float.IsNaN(targetX) || float.IsInfinity(targetX) || float.IsNaN(targetRow) || float.IsInfinity(targetRow) || targetX<0 || targetX>=Columns || targetRow<0 || targetRow>=RowCount)return false;
            Plant p=Plants.Find(c=>c.Id==Adventure.SelectedCobCannonId.Value && c.Kind==PlantKind.CobCannon && c.CannonPhase==CobCannonPhase.Aiming);
            if(p==null)return false;
            p.CannonTargetX=targetX;p.CannonTargetRow=targetRow;p.CannonPhase=CobCannonPhase.Firing;p.CannonReloadRemaining=AdventureCobFiringSeconds;
            Adventure.SelectedCobCannonId=null;return true;
        }
        public bool TryFireAdventureCobCannonAtCell(int row,int column)
        {return TryFireAdventureCobCannon(column+0.5f,row);}
        private void AdvanceAdventureCannon(Plant p,float dt)
        {
            if(p.CannonPhase==CobCannonPhase.Ready || p.CannonPhase==CobCannonPhase.Aiming)return;
            p.CannonReloadRemaining=Math.Max(0,p.CannonReloadRemaining-dt);
            if(p.CannonReloadRemaining>0.000001f)return;
            if(p.CannonPhase==CobCannonPhase.Arming || p.CannonPhase==CobCannonPhase.Reloading)
            {p.CannonPhase=CobCannonPhase.Loading;p.CannonReloadRemaining=AdventureCobLoadingSeconds;}
            else if(p.CannonPhase==CobCannonPhase.Loading)
            {p.CannonPhase=CobCannonPhase.Ready;Adventure.Notice(AdventureNoticeKind.CobReady,p.Id,p.Row,p.Column);}
            else if(p.CannonPhase==CobCannonPhase.Firing)
            {
                Adventure.cobShells.Add(new AdventureCobShell{SourcePlantId=p.Id,TargetX=p.CannonTargetX,TargetRow=p.CannonTargetRow,
                    LaunchX=p.Column+1f,LaunchRow=p.Row,FlightRemaining=AdventureCobFlightSeconds});
                p.CannonPhase=CobCannonPhase.Reloading;p.CannonReloadRemaining=30f;Raise(GameEvent.Shoot);
                Adventure.Notice(AdventureNoticeKind.CobFired,p.Id,p.Row,p.Column);
            }
        }
        /// <summary>Host loot input. Currency stays in this board; this is not a purchase or original profile mutation.</summary>
        public AdventureCoin SpawnAdventureCoin(AdventureCoinKind kind,float x,float row)
        {
            if(!IsAdventure || State!=MatchState.Playing || Adventure.Phase!=AdventurePhase.Playing || !Enum.IsDefined(typeof(AdventureCoinKind),kind) ||
                float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(row) || float.IsInfinity(row) || x<0 || x>=Columns || row<0 || row>=RowCount)return null;
            var coin=new AdventureCoin{Id=++Adventure.CoinId,Kind=kind,X=x,Row=row};Adventure.coins.Add(coin);
            Adventure.Notice(AdventureNoticeKind.CoinCreated,coin.Id,(int)row,(int)x);return coin;
        }
        public bool CollectAdventureCoin(AdventureCoin coin)
        {
            if(!IsAdventure || State!=MatchState.Playing || Adventure.Phase!=AdventurePhase.Playing || coin==null || coin.IsBeingCollected || !Adventure.coins.Contains(coin))return false;
            return FinishAdventureCoin(coin);
        }
        private bool FinishAdventureCoin(AdventureCoin coin)
        {
            if(!Adventure.coins.Contains(coin))return false;
            if(coin.ValueUnits==0 && (Rules.AdventureLootCommit==null || !Rules.AdventureLootCommit(coin))){coin.PersistencePending=true;return false;}
            int total=(int)Math.Min(int.MaxValue,(long)Adventure.MoneyCollectedUnits+coin.ValueUnits);
            if(Rules.AdventureCoinCommit!=null&&!Rules.AdventureCoinCommit(total)){coin.PersistencePending=true;return false;}
            Adventure.coins.Remove(coin);
            Adventure.MoneyCollectedUnits=(int)Math.Min(int.MaxValue,(long)Adventure.MoneyCollectedUnits+coin.ValueUnits);
            Adventure.Notice(AdventureNoticeKind.CoinCollected,coin.ValueUnits);return true;
        }
        private void AdvanceAdventureUpgradeWorld(float dt)
        {
            if(Adventure.SelectedCobCannonId.HasValue && !Plants.Exists(p=>p.Id==Adventure.SelectedCobCannonId.Value))Adventure.SelectedCobCannonId=null;
            for(int i=Adventure.coins.Count-1;i>=0;i--)
            {
                var coin=Adventure.coins[i];coin.Age+=dt;
                if(coin.IsBeingCollected)
                {
                    Plant collector=Plants.Find(p=>p.Id==coin.CollectorPlantId && p.Kind==PlantKind.GoldMagnet);
                    if(collector==null){coin.IsBeingCollected=false;coin.CollectorPlantId=0;continue;}
                    coin.CollectionRemaining-=dt;
                    if(coin.CollectionRemaining<=0.000001f)FinishAdventureCoin(coin);
                }
                else if(coin.Age>=30f&&!coin.PersistencePending)Adventure.coins.RemoveAt(i);
            }
            for(int i=Adventure.cobShells.Count-1;i>=0;i--)
            {
                var shell=Adventure.cobShells[i];shell.FlightRemaining-=dt;
                if(shell.FlightRemaining>0.000001f)continue;
                int row=(int)Math.Round(shell.TargetRow);BlastAdventure(shell.TargetX,row,1.5f,1,1800f,true);
                AdventurePresentationCobImpact(shell.TargetX,shell.TargetRow);
                if(shell.TargetX>=7.5f && Math.Abs(shell.TargetRow-2f)<=1f)DamageAdventureBoss(1800f);
                Adventure.cobShells.RemoveAt(i);Raise(GameEvent.Explosion);Adventure.Notice(AdventureNoticeKind.CobImpact,shell.SourcePlantId,row,(int)shell.TargetX);
            }
        }
    }
}
