using System;
using System.Collections.Generic;
namespace PvZReplica.Adventure
{
    public sealed class AdventureSnowTrail
    {
        public int Id, Seed, Row;
        public float Age, X, Y, PreviousX, PreviousY, StopAge = -1f;
        public Pea Projectile;
        public bool Stopped { get { return StopAge >= 0f; } }
    }
    public sealed partial class AdventureImpact
    {
        public bool NativeIcePoint;
        public float NativeParticleX, NativeParticleY, NativeAttachedX, NativeAttachedY;
        public Zombie AttachedZombie;
    }
    public sealed partial class AdventurePresentationState
    {
        public readonly List<AdventureSnowTrail> SnowTrails = new List<AdventureSnowTrail>();
        public int IceLaunchSoundSerial, ChillOnsetSoundSerial;
        internal int IceShootSuppressions;
    }
    public static class AdventureIceRules
    {
        // PC 54D720, 54D7A0; these are PC phase numbers, not mobile particle enums.
        public static bool CanChill(int kind,int phase,bool dead,bool hypnotized,bool sled)
        {
            return kind!=12&&!dead&&!hypnotized&&!sled&&phase!=1&&phase!=2&&phase!=3&&
                phase!=32&&phase!=33&&phase!=34&&phase!=35&&phase!=14&&phase!=50&&
                (kind!=25||phase==87||phase==88||phase==89);
        }
        public static bool CanFreeze(int kind,int phase,bool canChill)
        {
            return canChill&&phase!=12&&phase!=52&&phase!=54&&phase!=58&&phase!=73&&phase!=74&&
                phase!=71&&phase!=72&&phase!=19&&phase!=16&&phase!=93&&phase!=94&&phase!=95&&
                !(phase>=20&&phase<=28)&&(kind!=20||phase==6);
        }
        public static int ChillTicks(int oldTicks,bool iceTrap) { return Math.Max(oldTicks,iceTrap?2000:1000); }
        public static int FreezeTicks(bool inPool,bool previouslyCold,int randomOffset)
        { return inPool?300:(previouslyCold?300:400)+randomOffset; }
        public static bool ProjectileReachesColdState(float damage,float shield,bool bypassShield)
        { return bypassShield||damage>shield; }
        // 47BB30 attaches particle 45 at top-left +(8,13). The host's 28x28 snow sprite uses a centre.
        public static void TrailFromSpriteCentre(float x,float y,out float nativeX,out float nativeY)
        { nativeX=x-6f;nativeY=y-1f; }
        // 47D410: SnowPeaSplat=43; the original attached-particle coordinates are intentionally +52.
        public static void SnowImpact(float projectileX,float projectileY,bool attached,int zombieX,int zombieY,
            int phase,bool backwards,out float x,out float y)
        {
            x=projectileX-3f;y=projectileY+12f;
            if(!attached)return;
            float dx=x+52f-zombieX,dy=y-zombieY;
            if(phase==59||phase==55)dy+=60f;
            if(backwards)dx-=80f;else if(projectileX>zombieX+40)dx-=60f;
            x=zombieX+(int)dx;y=zombieY+(int)Math.Max(20f,Math.Min(100f,dy));
        }
    }
}
namespace PvZReplica
{
    public sealed partial class Pea { public PvZReplica.Adventure.AdventureSnowTrail PresentationSnowTrail; }
    public sealed partial class GameSimulation
    {
        private void RequestAdventureIceLaunch(Plant plant,Pea shot)
        {
            if(plant.Kind!=PlantKind.SnowPea&&plant.Kind!=PlantKind.WinterMelon)return;
            // PC 4752E0: both types play native Foley3 AND Foley27, once at actual release.
            RequestAdventureZombieFoley(3);RequestAdventureZombieFoley(27);
            Presentation.IceShootSuppressions++;Presentation.IceLaunchSoundSerial++;
            if(plant.Kind==PlantKind.SnowPea)BeginAdventureSnowTrail(shot);
        }
        private bool CanAdventureChill(Zombie z)
        {
            bool sled=z.PresentationHasSled??(z.Kind==ZombieKind.Bobsled&&!z.UsedAbility);
            return PvZReplica.Adventure.AdventureIceRules.CanChill((int)z.Kind,AdventureZombieFeedbackPhase(z),z.Health<=0,z.Hypnotized,sled);
        }
        public bool ApplyAdventureChill(Zombie z,bool iceTrap=false)
        {
            if(z==null||!CanAdventureChill(z))return false;
            if(z.ChillRemaining<=0f){RequestAdventureZombieFoley(18);Presentation.ChillOnsetSoundSerial++;}
            z.ChillRemaining=Math.Max(z.ChillRemaining,iceTrap?20f:10f);return true;
        }
        public bool ApplyAdventureIceTrap(Zombie z)
        {
            if(z==null)return false;
            bool wasCold=z.ChillRemaining>0f||z.FrozenRemaining>0f&&z.PresentationButter<=0f;
            bool chilled=ApplyAdventureChill(z,true);
            if(!PvZReplica.Adventure.AdventureIceRules.CanFreeze((int)z.Kind,AdventureZombieFeedbackPhase(z),chilled))return false;
            bool pool=z.PresentationInPool??(Adventure.Level.IsWaterRow(z.Row)&&z.X>=0f&&z.X<9f);
            int offset=pool?0:random.Next(wasCold?101:201);
            z.FrozenRemaining=Math.Max(z.PresentationButter,PvZReplica.Adventure.AdventureIceRules.FreezeTicks(pool,wasCold,offset)*.01f);
            // PC 54E150 also applies 20 damage with bit1 (bypass held shield). Butter is a separate counter.
            HitAdventureZombie(z,20f,true,1U);return true;
        }
        private void ApplyAdventureProjectileCold(Pea shot,Zombie z)
        {
            if(shot.IsSnow&&PvZReplica.Adventure.AdventureIceRules.ProjectileReachesColdState(shot.Damage,z.ShieldHealth,shot.PiercesShield))
                ApplyAdventureChill(z);
            if(shot.Fire&&z.Kind!=ZombieKind.Zomboni&&z.Kind!=ZombieKind.Catapult&&
                (z.ShieldHealth<=0f||z.Kind!=ZombieKind.ScreenDoor&&z.Kind!=ZombieKind.Ladder))
            {
                z.ChillRemaining=0f;
                if(z.PresentationButter<=0f)z.FrozenRemaining=0f;
            }
        }
        private void AdventureIceProjectileTopLeft(Pea shot,out float x,out float y)
        {
            float pitch=Adventure.Level.HasPool||Adventure.Level.HasRoof?85f:100f;
            x=40f+shot.X*80f;
            if(shot.Lobbed){y=shot.PresentationLaunchY+shot.PresentationLobHeight;return;}
            float slope=Adventure.Level.HasRoof?Math.Max(0f,5f-shot.X)*20f-10f:0f;
            x-=14f;y=80f+shot.FlightRow*pitch+slope+16f;
        }
        private void BeginAdventureSnowTrail(Pea shot)
        {
            if(shot.PresentationSnowTrail!=null)return;
            float x,y;AdventureIceProjectileTopLeft(shot,out x,out y);
            var trail=new PvZReplica.Adventure.AdventureSnowTrail {Id=++Presentation.EffectId,Seed=random.Next(),Projectile=shot,Row=shot.Row,
                X=x+8f,Y=y+13f,PreviousX=x+8f,PreviousY=y+13f};
            shot.PresentationSnowTrail=trail;Presentation.SnowTrails.Add(trail);
        }
        private void UpdateAdventureSnowTrail(Pea shot)
        {
            if(shot.IsSnow&&!shot.Lobbed)BeginAdventureSnowTrail(shot);
            var trail=shot.PresentationSnowTrail;if(trail==null||trail.Stopped)return;
            float x,y;AdventureIceProjectileTopLeft(shot,out x,out y);trail.X=x+8f;trail.Y=y+13f;trail.Row=shot.Row;
            if(!shot.IsSnow||shot.Lobbed)
            {
                // PC ConvertToPea 47E230 calls AttachmentDie (40E620), NOT FadeOut.
                Presentation.SnowTrails.Remove(trail);shot.PresentationSnowTrail=null;
                RequestAdventureZombieFoley(3);
            }
        }
        private void StopAdventureSnowTrail(Pea shot)
        {
            var trail=shot.PresentationSnowTrail;
            if(trail!=null&&!trail.Stopped){trail.StopAge=trail.Age;trail.Projectile=null;}
        }
        private void AdvanceAdventureIcePresentation(float dt)
        {
            for(int i=Presentation.SnowTrails.Count-1;i>=0;i--)
            {
                var trail=Presentation.SnowTrails[i];trail.PreviousX=trail.X;trail.PreviousY=trail.Y;trail.Age+=dt;
                if(trail.Stopped&&trail.Age-trail.StopAge>=.5f)Presentation.SnowTrails.RemoveAt(i);
            }
        }
        private void RecordAdventureIceImpact(Pea shot,Zombie zombie,PvZReplica.Adventure.AdventureImpact impact)
        {
            StopAdventureSnowTrail(shot);
            if(!shot.IsSnow)return;
            float x,y;AdventureIceProjectileTopLeft(shot,out x,out y);
            if(shot.Lobbed)
            {
                // PC winter-melon impact is particle1 at the lobbed ground/contact coordinates +(30,30).
                // mPosX-mVelX and mPosY+mPosZ-mVelY-mVelZ are the previous native integration point.
                float px=40f+shot.PresentationPreviousX*80f;
                float py=shot.PresentationLaunchY+shot.PresentationPreviousLobHeight;
                x=px+30f;y=py+30f;
            }
            else
            {
                int zx=zombie==null?0:(int)PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(zombie.X,zombie.IsSmall?.5f:1f);
                int zy=zombie==null?0:(int)AdventureBiteNativeY(zombie,zx);
                PvZReplica.Adventure.AdventureIceRules.SnowImpact(x,y,zombie!=null,zx,zy,zombie==null?0:AdventureZombieFeedbackPhase(zombie),shot.Backward,out x,out y);
                if(zombie!=null){impact.AttachedZombie=zombie;impact.NativeAttachedX=x-zx;impact.NativeAttachedY=y-zy;}
            }
            impact.NativeIcePoint=true;impact.NativeParticleX=x;impact.NativeParticleY=y;
        }
        public void AdventureIceImpactPoint(PvZReplica.Adventure.AdventureImpact impact,float fraction,out float x,out float y)
        {
            x=impact.NativeParticleX;y=impact.NativeParticleY;
            var z=impact.AttachedZombie;if(z==null)return;
            float grid=z.PresentationPreviousX+(z.X-z.PresentationPreviousX)*Math.Max(0f,Math.Min(1f,fraction));
            x=PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(grid,z.IsSmall?.5f:1f);
            y=AdventureBiteNativeY(z,x)+impact.NativeAttachedY;x+=impact.NativeAttachedX;
        }
    }
}
