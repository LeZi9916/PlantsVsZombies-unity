using System;
using PvZReplica.BoardUI;
namespace PvZReplica.Adventure
{
    public enum AdventureBiteAttack { Chew=0,DriveOver=1,Vault=2,Ladder=3 }
    public static class AdventureZombieBiteRules
    {
        public static NativeRect LocalAttackRect(int kind,int phase,bool bowling)
        {
            // PC 0x53DD90 then LoadPlainZombieReanim 0x53FE40. Plain load overrides the constructor's 50/20.
            if(phase==12||phase==54)return new NativeRect(-40,0,100,115);
            if(kind==3&&phase==11)return new NativeRect(bowling?-229:-29,0,bowling?270:70,115);
            if(kind==14&&(phase==52||phase==53))return new NativeRect(-29,0,70,115);
            if(kind==14&&phase==55)return new NativeRect(30,0,30,115);
            if(kind==18)return phase>=20&&phase<=28?new NativeRect(10,0,30,115):new NativeRect(20,17,50,115);
            if(kind==16)return new NativeRect(20,30,50,115);
            if(kind==23||kind==32)return new NativeRect(-30,-38,89,154);
            if(kind==12||kind==22)return new NativeRect(10,-13,133,140);
            if(kind==11)return new NativeRect(-5,0,55,115);
            if(kind==21)return new NativeRect(10,0,50,115);
            if(kind==20||kind==25)return new NativeRect(0,0,0,0);
            if(kind==0||kind==1||kind==2||kind==4||kind==6||kind==10||kind>=26&&kind<=31||kind==33||kind==5)
                return new NativeRect(20,0,50,115);
            return new NativeRect(50,0,20,115);
        }
        public static NativeRect AttackRect(int kind,int phase,bool bowling,bool backwards,int x,int y,int bodyY=0,int clipHeight=-10000,int width=120)
        {
            var b=LocalAttackRect(kind,phase,bowling);int left=backwards?width-b.Width-b.X:b.X;
            // PC 0x54DED0 does not shrink the attack rectangle when the reanim scale is .5.
            return new NativeRect(x+left,y+bodyY+b.Y,b.Width,Math.Max(0,b.Height-(clipHeight>-100?clipHeight:0)));
        }
        public static NativeRect PlantRect(int kind,int x,int y,int width=80,int height=80)
        {return kind==23?new NativeRect(x+10,y,width,height):kind==30?new NativeRect(x,y,width-20,height):
            kind==47?new NativeRect(x,y,140,80):new NativeRect(x+10,y,width-20,height);}
        public static int HorizontalOverlap(NativeRect a,NativeRect b)
        {return Math.Min(a.X+a.Width,b.X+b.Width)-Math.Max(a.X,b.X);}
        public static bool Reaches(NativeRect attack,NativeRect plant){return HorizontalOverlap(attack,plant)>=20;}
        public static bool CanChew(int kind,int phase,int height,bool flying,bool sled)
        {
            if(kind==20||kind==23||kind==32||kind==12||kind==22||kind==25||flying||sled)return false;
            if(height==9||height==6||height==1||height==2||height==7)return false;
            switch(phase){case 12:case 11:case 30:case 33:case 34:case 35:case 36:case 14:case 71:case 72:
                case 50:case 41:case 42:case 43:case 51:case 56:case 52:case 53:case 54:case 58:case 57:case 77:return false;}
            return !(phase>=20&&phase<=28);
        }
        public static void ChewPhases(int kind,out float first,out float second)
        {
            first=.14f;second=.68f;
            switch(kind){case 3:first=.38f;second=.8f;break;case 5:case 21:first=second=.42f;break;
                case 15:first=second=.53f;break;case 13:first=.33f;second=.83f;break;case 24:first=.33f;second=.79f;break;}
        }
        public static bool Crossed(double before,double after,float phase)
        {return after>=before&&Math.Floor(after-phase)>Math.Floor(before-phase);}
        public static int ChewFoley(int plant,bool hypnotized)
        {return plant==3||plant==23||plant==30||plant<0&&hypnotized?6:5;}
        public static int EatingChecks(float age,float dt,bool chilled)
        {int period=chilled?8:4;return Math.Max(0,(int)Math.Round(age*100)/period-(int)Math.Round((age-dt)*100)/period);}
    }
}
namespace PvZReplica
{
    public sealed partial class Plant
    { public int PresentationBiteNativeState=-1;public bool PresentationBiteNotOnGround; }
    public sealed partial class Zombie
    {
        public int PresentationBiteBodyY,PresentationBiteClipHeight=-10000;
        public int PresentationGarlicCounter=-1;
        internal string PresentationBiteClip;
        internal double PresentationBiteCycle;
        internal Plant PresentationGarlicPlant;
        internal float PresentationGarlicHeldAge,PresentationGarlicHeldHeadAge;
    }
    public sealed partial class GameSimulation
    {
        public NativeRect AdventureZombieAttackRectangle(Zombie z)
        {
            int kind=z.ModeHead>=26?z.ModeHead:(int)z.Kind,phase=AdventureZombieFeedbackPhase(z);
            int x=(int)PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(z.X,z.IsSmall?.5f:1f);
            int y=(int)AdventureBiteNativeY(z,x);
            bool backwards=z.PresentationWalkingBackwards??(z.Hypnotized||z.Kind==ZombieKind.Digger&&z.AbilityPhase==2);
            return PvZReplica.Adventure.AdventureZombieBiteRules.AttackRect(kind,phase,Adventure.Level.Mode==PvZReplica.Adventure.AdventureMode.Bowling,
                backwards,x,y,z.PresentationBiteBodyY,z.PresentationBiteClipHeight,z.Kind==ZombieKind.Gargantuar?180:120);
        }
        private float AdventureBiteNativeY(Zombie z,float x)
        {return 80f+z.Row*(Adventure.Level.HasPool||Adventure.Level.HasRoof?85f:100f)+
            (Adventure.Level.HasRoof?Math.Max(0f,5f-x/80f)*20f-10f:0f)-30f-(z.Kind==ZombieKind.Balloon?30f:0f)-(z.Kind==ZombieKind.Pogo&&!z.UsedAbility?16f:0f);}
        public NativeRect AdventurePlantEatingRectangle(Plant p)
        {return PvZReplica.Adventure.AdventureZombieBiteRules.PlantRect((int)p.Kind,40+p.Column*80,80+p.Row*(Adventure.Level.HasPool||Adventure.Level.HasRoof?85:100)+(Adventure.Level.HasRoof?(int)(Math.Max(0f,5f-p.Column)*20f-10f):0));}
        private bool AdventureBiteCanTarget(Zombie z,Plant p,PvZReplica.Adventure.AdventureBiteAttack attack,bool priority=true)
        {
            if(p.Health<=0||p.Row!=z.Row||p.Layer==PvZReplica.Adventure.PlantLayer.Effect||p.PresentationBiteNotOnGround||
                p.Kind==PlantKind.TangleKelp||p.Kind==PlantKind.Squash&&p.PresentationBiteNativeState>=5&&p.PresentationBiteNativeState<=7)return false;
            if(Adventure.Level.Mode==PvZReplica.Adventure.AdventureMode.Bowling&&attack!=PvZReplica.Adventure.AdventureBiteAttack.Vault)return false;
            bool pool=z.PresentationInPool??(Adventure.Level.IsWaterRow(z.Row)&&z.X>=0&&z.X<9);
            if(!pool&&Adventure.Level.IsWaterRow(p.Row))return false;
            if(z.Kind==ZombieKind.Digger&&z.AbilityPhase==0)return p.Kind==PlantKind.PotatoMine&&p.MinePhase==MinePhase.Buried;
            if(p.Kind==PlantKind.Spikeweed||p.Kind==PlantKind.Spikerock)
                return z.Kind==ZombieKind.Gargantuar||z.Kind==ZombieKind.Zomboni||Adventure.Level.IsWaterRow(p.Row)||
                    Plants.Exists(q=>q.Row==p.Row&&q.Column==p.Column&&q.Kind==PlantKind.FlowerPot);
            if(attack==PvZReplica.Adventure.AdventureBiteAttack.DriveOver)
            {
                if(p.Kind==PlantKind.CherryBomb||p.Kind==PlantKind.Jalapeno||p.Kind==PlantKind.Blover||p.Kind==PlantKind.Squash)return false;
                if((p.Kind==PlantKind.DoomShroom||p.Kind==PlantKind.IceShroom)&&!p.Asleep)return false;
            }
            if(z.Kind==ZombieKind.Ladder&&z.ShieldHealth>0)
            {
                bool barrier=(p.Kind==PlantKind.Wallnut||p.Kind==PlantKind.TallNut||p.Kind==PlantKind.Pumpkin)&&!p.HasLadder;
                if(attack==PvZReplica.Adventure.AdventureBiteAttack.Chew&&barrier)return false;
                if(attack==PvZReplica.Adventure.AdventureBiteAttack.Ladder&&!barrier)return false;
            }
            if(priority&&(attack==PvZReplica.Adventure.AdventureBiteAttack.Chew||attack==PvZReplica.Adventure.AdventureBiteAttack.Vault))
                foreach(var q in Plants)
                    if(q!=p&&q.Row==p.Row&&q.Column==p.Column&&q.Layer>p.Layer&&
                        (attack!=PvZReplica.Adventure.AdventureBiteAttack.Vault||q.Layer==PvZReplica.Adventure.PlantLayer.Main)&&
                        AdventureBiteCanTarget(z,q,attack,false))return false;
            return true;
        }
        private Plant FindAdventureNativePlantTarget(Zombie z,PvZReplica.Adventure.AdventureBiteAttack attack)
        {
            var box=AdventureZombieAttackRectangle(z);
            foreach(var p in Plants)if(AdventureBiteCanTarget(z,p,attack)&&
                PvZReplica.Adventure.AdventureZombieBiteRules.Reaches(box,AdventurePlantEatingRectangle(p)))return p;
            return null;
        }
        private bool AdventureNativeChewer(Zombie z)
        {return PvZReplica.Adventure.AdventureZombieBiteRules.CanChew((int)z.Kind,AdventureZombieFeedbackPhase(z),z.PresentationNativeHeight,
            z.Airborne,z.PresentationHasSled??(z.Kind==ZombieKind.Bobsled&&!z.UsedAbility));}
        private bool AdventurePlantBiteImmune(Plant p)
        {
            if(p.Kind==PlantKind.PotatoMine&&p.MinePhase!=MinePhase.Buried)return true;
            int state=p.PresentationBiteNativeState;
            return !p.Asleep&&(p.Kind==PlantKind.CherryBomb||p.Kind==PlantKind.Jalapeno||p.Kind==PlantKind.DoomShroom||
                p.Kind==PlantKind.IceShroom||p.Kind==PlantKind.HypnoShroom||state==47||state==48||state==3||state==4);
        }
        private bool AdvanceAdventureNativeEating(Zombie z,Plant target,float dt,bool zombieTarget=false)
        {
            if(z.Headless||!AdventureNativeChewer(z)||z.PresentationGarlicCounter>=0)return false;
            int checks=PvZReplica.Adventure.AdventureZombieBiteRules.EatingChecks(z.Age,dt,z.ChillRemaining>0);
            if(checks>0)z.Eating=target!=null||zombieTarget;
            if(!z.Eating){z.PresentationBiteClip=null;return false;}
            string clip=AdventurePresentationZombieAnimation(z);
            PvZReplica.Adventure.AdventureZombieGroundClip data;
            if(PvZReplica.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z),clip,out data))
            {
                double cycle=z.PresentationAnimationAge*AdventurePresentationZombieRate(z)/data.Count;
                if(z.PresentationBiteClip==clip)
                {
                    float first,second;PvZReplica.Adventure.AdventureZombieBiteRules.ChewPhases((int)z.Kind,out first,out second);
                    if(z.FrozenRemaining<=0&&!z.Headless&&AdventureZombieFeedbackPhase(z)!=60&&
                        (PvZReplica.Adventure.AdventureZombieBiteRules.Crossed(z.PresentationBiteCycle,cycle,first)||
                         PvZReplica.Adventure.AdventureZombieBiteRules.Crossed(z.PresentationBiteCycle,cycle,second)))
                        AnimateAdventureChew(z,target,zombieTarget);
                }
                z.PresentationBiteClip=clip;z.PresentationBiteCycle=cycle;
            }
            if(target!=null&&checks>0&&Plants.Contains(target)&&!AdventurePlantBiteImmune(target)&&z.PresentationGarlicCounter<0)
            {
                target.Health-=4f*checks;
                if(target.Health<=0){RequestAdventureZombieFoley(-1);RemoveAdventurePlantAndStack(target);}
            }
            return true;
        }
        private void AnimateAdventureChew(Zombie z,Plant target,bool zombieTarget)
        {
            if(target!=null&&target.Kind==PlantKind.HypnoShroom&&!target.Asleep)
            {
                RequestAdventureZombieFoley(68);RemoveAdventurePlantAndStack(target);z.Hypnotized=true;z.Submerged=false;z.Eating=false;
                z.PresentationNativeVelocity=.17f;z.Speed=PvZReplica.Adventure.AdventureZombieNativeRules.GridSpeed(.17f);
                Presentation.PlantBiteEffects.Add(new PvZReplica.Adventure.AdventurePlantBiteEffect { Id=++Presentation.EffectId,ParticleId=88,Row=z.Row,Kind=z.Kind,
                    NativeX=PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(z.X,z.IsSmall?.5f:1f),NativeY=AdventureBiteNativeY(z,PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(z.X,z.IsSmall?.5f:1f)),OffsetX=60,OffsetY=40,Seed=z.Id*397^Presentation.EffectId });
                return;
            }
            RequestAdventureZombieFoley(PvZReplica.Adventure.AdventureZombieBiteRules.ChewFoley(target==null?-1:(int)target.Kind,z.Hypnotized));
            RecordAdventureChewEffect(z,target);
            if(target!=null&&target.Kind==PlantKind.Garlic)
            {
                z.PresentationGarlicCounter=0;z.PresentationGarlicPlant=target;
                z.PresentationGarlicHeldAge=z.PresentationAnimationAge;z.PresentationGarlicHeldHeadAge=z.PresentationPlantHeadAge;
            }
        }
        private bool AdvanceAdventureGarlicBite(Zombie z,float dt)
        {
            if(z.PresentationGarlicCounter<0)return false;
            int ticks=Math.Max(0,(int)Math.Round(dt*100));
            for(int i=0;i<ticks&&z.PresentationGarlicCounter>=0;i++)
            {
                int c=++z.PresentationGarlicCounter;
                if(c>20&&c<170&&!Plants.Contains(z.PresentationGarlicPlant)){c=170;z.PresentationGarlicCounter=c;z.Eating=false;}
                if(c==70)
                {
                    z.Eating=false;AdventurePresentationZombieAnimation(z);z.PresentationGarlicHeldAge=0;
                    if(Zombies.Count<=5||Zombies.Count<=10&&random.Next(2)==0)RequestAdventureZombieFoley(72);
                }
                if(c==170)
                {
                    z.Eating=false;var rows=new System.Collections.Generic.List<int>();
                    for(int row=Math.Max(0,z.Row-1);row<=Math.Min(RowCount-1,z.Row+1);row++)
                        if(row!=z.Row&&Adventure.Level.IsLawnRow(row)&&Adventure.Level.IsWaterRow(row)==Adventure.Level.IsWaterRow(z.Row))rows.Add(row);
                    if(rows.Count>0)z.Row=rows[random.Next(rows.Count)];
                    z.PresentationBiteClip=null;AdventurePresentationZombieAnimation(z);
                }
                if(c>270){z.PresentationGarlicCounter=-1;z.PresentationGarlicPlant=null;}
            }
            if(z.PresentationGarlicCounter>=0&&z.PresentationGarlicCounter<170)
            {
                z.PresentationAnimationAge=z.PresentationGarlicHeldAge;z.PresentationPlantHeadAge=z.PresentationGarlicHeldHeadAge;
                PvZReplica.Adventure.AdventureZombieGroundClip data;string clip=AdventurePresentationZombieAnimation(z);
                if(PvZReplica.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z),clip,out data))
                {float phase=z.PresentationAnimationAge*AdventurePresentationZombieRate(z)/data.Count;z.PresentationWalkLoops=(int)Math.Floor(phase);z.PresentationWalkPhase=phase-z.PresentationWalkLoops;}
                return true;
            }
            return false;
        }
    }
}
