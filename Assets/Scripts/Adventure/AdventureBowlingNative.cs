using System;
using PvZReplica.BoardUI;
namespace PvZReplica.Adventure
{
    public sealed partial class AdventureBowlingNut
    {
        public bool NativeMotionInitialized {get;internal set;}
        public int NativePixelX {get;internal set;}
        public int NativePixelY {get;internal set;}
        public int NativeDirection {get;internal set;}
        public float NativeAge {get;internal set;}
        internal float NativeTickRemainder;
    }
    public static class AdventureBowlingNativeRules
    {
        // Original Wallnut.reanim _ground frame43..55; negative ground movement rolls right.
        public static readonly float[] Ground={44.8f,27.6f,8.9f,-11.3f,-31.5f,-50.2f,-67.4f,-84.6f,-103.4f,-123.6f,-143.8f,-162.5f,-179.8f};
        public static int HorizontalStep(int x,float age,float rate,bool giant)
        {
            float phase=age*rate/Ground.Length;phase-=(float)Math.Floor(phase);
            int before=Math.Min(Ground.Length-1,(int)((Ground.Length-1)*phase));
            int after=Math.Min(Ground.Length-1,before+1);
            float velocity=(Ground[after]-Ground[before])*.01f*rate*(giant?2f:1f);
            // PC casts the resulting absolute position, not just the velocity (important for positive X).
            return (int)(x-velocity);
        }
        public static int VerticalStep(int y,int direction){return y+(direction==28?-2:direction==29?2:0);}
        public static bool AtRow(int y,int row,int pitch=100){return Math.Abs(80+row*pitch-y)<=2;}
        public static int NextDirection(int row,int direction,bool hit,bool giant,int randomBit)
        {
            int next=direction;
            if(direction==28&&row==0)next=29;else if(direction==29&&row==4)next=28;
            if(hit&&!giant)
            {
                if(row==4||direction==29)return 28;
                if(row==0||direction==28)return 29;
                return randomBit==0?29:28;
            }
            return next;
        }
        public static NativeRect ZombieBounds(int kind,int phase,int x,int y,bool backwards,int bodyY=0,int clip=-10000)
        {
            int dx=36,dy=0,w=42,h=115,actor=120;
            switch(kind){case 7:dx=50;w=57;break;case 17:dx=50;w=28;break;
                case 11:dx=12;w=62;break;case 12:case 22:dx=0;dy=-13;w=153;h=140;break;
                case 23:case 32:dx=-17;dy=-38;w=125;h=154;actor=180;break;
                case 16:dx=36;dy=30;w=42;break;case 20:dx=-20;dy=22;w=110;h=94;break;
                case 18:if(phase<20||phase>28)dy=17;break;}
            if(backwards)dx=actor-w-dx;
            return new NativeRect(x+dx,y+dy+bodyY,w,Math.Max(0,h-(clip>-100?clip:0)));
        }
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private void InitializeAdventureBowlingNative(PvZReplica.Adventure.AdventureBowlingNut nut,bool giant)
        {
            if(nut.NativeMotionInitialized)return;
            nut.BounceDelay=0f; // Legacy field retained for old scene probes; native collision has no invented cooldown.
            nut.NativeMotionInitialized=true;nut.NativePixelX=(int)(nut.X*80f);nut.NativePixelY=80+nut.Row*100;
            PvZReplica.Adventure.AdventureBowlingPresentation pose;
            if(!Presentation.Bowling.TryGetValue(nut,out pose))
            {pose=new PvZReplica.Adventure.AdventureBowlingPresentation {Rate=(giant?6f:12f)+(float)random.NextDouble()*(giant?4f:6f)};Presentation.Bowling.Add(nut,pose);}
            nut.NativeAge=0f;pose.Age=0f; // PC samples the initial ground pose before the first reanim update.
        }
        private Zombie FindAdventureBowlingNativeTarget(PvZReplica.Adventure.AdventureBowlingNut nut,bool giant)
        {
            // PC bowling attack rect uses plant mX, mWidth-20=60 and signed horizontal overlap>=0.
            var attack=new NativeRect(nut.NativePixelX,nut.NativePixelY,60,80);
            foreach(var z in Zombies)
            {
                int phase=AdventureZombieFeedbackPhase(z);
                if(z.Row!=nut.Row||z.Hypnotized||z.Health<=0||phase>=1&&phase<=3||z.Airborne||z.DropRemaining>0||
                    z.Kind==ZombieKind.Boss||z.Kind==ZombieKind.Bungee||z.Kind==ZombieKind.Digger&&z.AbilityPhase==0||
                    phase==12&&!giant||phase==54&&!giant)continue;
                int x=(int)PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(z.X,z.IsSmall?.5f:1f);
                bool backwards=z.PresentationWalkingBackwards??(z.Hypnotized||z.Kind==ZombieKind.Digger&&z.AbilityPhase==2);
                var body=PvZReplica.Adventure.AdventureBowlingNativeRules.ZombieBounds((int)z.Kind,phase,x,(int)AdventureBiteNativeY(z,x),backwards,
                    z.PresentationBiteBodyY,z.PresentationBiteClipHeight);
                if(body.X>800)continue;
                if(PvZReplica.Adventure.AdventureZombieBiteRules.HorizontalOverlap(attack,body)>=0)return z;
            }
            return null;
        }
        private void HitAdventureBowlingNative(PvZReplica.Adventure.AdventureBowlingNut nut,Zombie z,bool giant)
        {
            RequestAdventureZombieFoley(20);
            if(giant||z.Kind==ZombieKind.ScreenDoor&&z.ShieldHealth>0&&nut.NativeDirection!=0)HitAdventureZombie(z,1800f);
            else if(z.ShieldHealth>0)HitAdventureZombie(z,Math.Min(400f,z.ShieldHealth));
            else if(z.ArmorHealth>0)
            {
                if(z.Kind==ZombieKind.Buckethead)RequestAdventureZombieFoley(46);
                else if(z.Kind==ZombieKind.Conehead)RequestAdventureZombieFoley(58);
                HitAdventureZombie(z,Math.Min(900f,z.ArmorHealth));
            }
            else HitAdventureZombie(z,1800f);
            nut.LastHitId=z.Id;
        }
        private bool AdvanceAdventureBowlingNative(PvZReplica.Adventure.AdventureBowlingNut nut,float dt,bool giant)
        {
            InitializeAdventureBowlingNative(nut,giant);
            PvZReplica.Adventure.AdventureBowlingPresentation pose=Presentation.Bowling[nut];
            nut.NativeTickRemainder+=dt*100f;int ticks=(int)Math.Floor(nut.NativeTickRemainder+.00001f);nut.NativeTickRemainder-=ticks;
            for(int tick=0;tick<ticks;tick++)
            {
                nut.NativePixelX=PvZReplica.Adventure.AdventureBowlingNativeRules.HorizontalStep(nut.NativePixelX,nut.NativeAge,pose.Rate,giant);
                nut.NativePixelY=PvZReplica.Adventure.AdventureBowlingNativeRules.VerticalStep(nut.NativePixelY,nut.NativeDirection);
                nut.NativeAge+=.01f;nut.X=nut.NativePixelX/80f;
                bool removed=nut.NativePixelX>800;
                if(!PvZReplica.Adventure.AdventureBowlingNativeRules.AtRow(nut.NativePixelY,nut.Row))
                {if(removed){pose.Age=nut.NativeAge;return true;}continue;}
                Zombie hit=FindAdventureBowlingNativeTarget(nut,giant);
                if(hit!=null)
                {
                    if(nut.Explosive)
                    {
                        AdventurePresentationExplodeONut(nut.X,nut.Row);
                        BlastAdventure(nut.X,nut.Row,1.5f,1,1800f,true);Raise(GameEvent.Explosion);return true;
                    }
                    HitAdventureBowlingNative(nut,hit,giant);
                }
                bool choose=hit!=null&&!giant&&nut.Row>0&&nut.Row<4&&nut.NativeDirection==0;
                int direction=PvZReplica.Adventure.AdventureBowlingNativeRules.NextDirection(nut.Row,nut.NativeDirection,hit!=null,giant,choose?random.Next(2):0);
                nut.NativeDirection=direction;
                // Row is the next collision/render-order row; the visible Y remains continuous.
                if(direction==28)nut.Row--;else if(direction==29)nut.Row++;
                if(removed){pose.Age=nut.NativeAge;return true;}
            }
            pose.Age=nut.NativeAge;return false;
        }
    }
}
