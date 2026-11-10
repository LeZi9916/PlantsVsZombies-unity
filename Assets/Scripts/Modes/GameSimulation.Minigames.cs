using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using PvZReplica.Modes;
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private void InitializeModePackets()
        {
            var s=ModeSession;s.NextPacket=s.Definition.Mechanic==ModeMechanic.RainingSeeds?1f:4f;
            switch(s.Definition.Mechanic)
            {
                case ModeMechanic.Bowling:AddAdventurePacket(PlantKind.Wallnut);break;
                case ModeMechanic.Boss:AddAdventurePacket(PlantKind.CabbagePult);AddAdventurePacket(PlantKind.Jalapeno);AddAdventurePacket(PlantKind.CabbagePult);AddAdventurePacket(PlantKind.IceShroom);s.NextPacket=10f;break;
                case ModeMechanic.Columns:foreach(var k in new[]{PlantKind.PotatoMine,PlantKind.TallNut,PlantKind.MelonPult,PlantKind.MagnetShroom,PlantKind.CoffeeBean,PlantKind.MelonPult})AddAdventurePacket(k);s.NextPacket=10f;break;
                case ModeMechanic.Slots:s.SlotSymbols[0]=1;s.SlotSymbols[1]=0;s.SlotSymbols[2]=5;break;
                case ModeMechanic.Invisible:AddAdventurePacket(PlantKind.Peashooter);AddAdventurePacket(PlantKind.IceShroom);s.NextPacket=10f;Adventure.FogBlownRemaining=float.MaxValue;break;
            }
        }
        private void TickModePackets(float dt)
        {
            var s=ModeSession;var m=s.Definition.Mechanic;
            foreach(var rain in s.RainPackets.ToArray())
            {
                rain.Y=Math.Min(rain.TargetY,rain.Y+45f*dt);if(rain.Y>=rain.TargetY)rain.Age+=dt;
                if(!Adventure.packets.Exists(p=>p.Id==rain.PacketId)){s.RainPackets.Remove(rain);continue;}
                if(rain.Age>=7.5f){Adventure.packets.RemoveAll(p=>p.Id==rain.PacketId);s.RainPackets.Remove(rain);}
            }
            if(m==ModeMechanic.Vases||m==ModeMechanic.Slots)
            {
                foreach(var expiring in Adventure.packets.ToArray()){if(m==ModeMechanic.Vases)expiring.Age+=dt;if(expiring.Age>=15f){Adventure.packets.Remove(expiring);s.BackwardPackets.Remove(expiring.Id);}}return;
            }
            s.NextPacket-=dt;if(s.NextPacket>0||Adventure.packets.Count>=10)return;
            int[] weights;
            switch(m)
            {
                case ModeMechanic.Bowling:weights=s.Definition.Id==33?new[]{3,85,49,15,50,15}:new[]{3,85,49,15};break;
                case ModeMechanic.LittleZombies:weights=new[]{16,25,3,15,0,25,2,35};break;
                case ModeMechanic.Portals:weights=new[]{0,25,7,20,22,10,26,15,3,15,2,15};break;
                case ModeMechanic.Columns:weights=new[]{33,155,39,5,6,5,30,15,20,10,17,10};break;
                case ModeMechanic.Invisible:weights=new[]{0,25,3,15,34,5,17,15,16,30,14,10};break;
                case ModeMechanic.Boss:weights=new[]{33,55,39,10,20,12,32,10,34,5,14,8};break;
                case ModeMechanic.RainingSeeds:
                    var rainKinds=new List<int>();foreach(var seed in Adventure.AvailableSeeds){int id=(int)seed;if(id>=40||id==1||id==9||id==35||id==37||id==11||id==33)continue;rainKinds.Add(id);}
                    int rainKind=rainKinds[random.Next(rainKinds.Count)];int lilies=Plants.FindAll(p=>p.Kind==PlantKind.LilyPad).Count;
                    if(random.Next(100)<30-29*Math.Min(18,lilies)/18)rainKind=16;
                    weights=new[]{rainKind,1};break;
                default:return;
            }
            for(int i=0;i<weights.Length;i+=2)
            {
                int seed=weights[i],queued=Adventure.packets.FindAll(p=>(int)p.Kind==seed).Count,planted=Plants.FindAll(p=>(int)p.Kind==seed).Count;
                if(seed==16||seed==33){int limit=seed==16?18:m==ModeMechanic.Columns?45:35;weights[i+1]=(int)Math.Round(weights[i+1]+(1-weights[i+1])*Math.Min(limit,queued+planted)/(float)limit);}
                if(m==ModeMechanic.Boss&&(seed==32||seed==34||seed==39)){int empty=Plants.FindAll(p=>p.Kind==PlantKind.FlowerPot&&AdventurePlantAt(p.Row,p.Column,PlantLayer.Main)==null).Count;if(empty<=2)weights[i+1]/=5;else if(empty<=5)weights[i+1]/=3;}
                if(weights.Length>4){if(queued>=4)weights[i+1]=1;else if(queued>=3)weights[i+1]=5;else if(Adventure.LastConveyorSeed==(PlantKind)seed)weights[i+1]/=2;}
            }
            int total=0;for(int i=1;i<weights.Length;i+=2)total+=weights[i];if(total<=0)return;int ticket=random.Next(total),kind=weights[0];
            for(int i=0;i<weights.Length;i+=2){ticket-=weights[i+1];if(ticket<0){kind=weights[i];break;}}
            var packet=AddAdventurePacket(kind==50?PlantKind.Wallnut:(PlantKind)kind);if(kind==50)s.GiantPackets.Add(packet.Id);
            if(m==ModeMechanic.RainingSeeds)s.RainPackets.Add(new ModeRainPacket{PacketId=packet.Id,X=100f+random.Next(550),Y=60f,TargetY=110f+(float)random.NextDouble()*340f});
            float multiplier=m==ModeMechanic.Boss?0.875f:m==ModeMechanic.Portals?1.5f:m==ModeMechanic.Invisible?2f:m==ModeMechanic.Columns?3f:1f;
            Adventure.LastConveyorSeed=packet.Kind;
            s.NextPacket=m==ModeMechanic.RainingSeeds?5f+random.Next(500)*0.01f:multiplier*(Adventure.packets.Count>8?10f:Adventure.packets.Count>6?5f:Adventure.packets.Count>4?4.25f:4f);
        }
        public bool SpinModeSlots()
        {
            var s=ModeSession;if(s==null||s.Definition.Mechanic!=ModeMechanic.Slots||s.Phase!=ModePhase.Playing||State!=MatchState.Playing||s.SlotRollingRemaining>0||SunAmount<25)return false;
            SunAmount-=25;s.SlotRollingRemaining=4f;
            for(int reel=0;reel<3;reel++)
            {
                int[] kinds={1,0,5,3,56,57};int[] weights={100,200-Math.Min(5,Plants.FindAll(p=>p.Kind==PlantKind.Peashooter).Count)*20,100,100,100,30};
                if(reel==2)for(int i=0;i<kinds.Length;i++)if(kinds[i]!=57&&(kinds[i]==s.SlotSymbols[0]||kinds[i]==s.SlotSymbols[1]))weights[i]+=weights[i]/2;
                int total=0;foreach(int w in weights)total+=w;int ticket=random.Next(total);
                for(int i=0;i<kinds.Length;i++){ticket-=weights[i];if(ticket<0){s.SlotSymbols[reel]=kinds[i];break;}}
            }
            return true;
        }
        private void TickModeSlot(float dt)
        {
            var s=ModeSession;if(s.SlotRollingRemaining<=0)return;s.SlotRollingRemaining-=dt;if(s.SlotRollingRemaining>0)return;
            int a=s.SlotSymbols[0],b=s.SlotSymbols[1],c=s.SlotSymbols[2];int matches=a==b&&b==c?3:a==b||a==c||b==c?2:0;
            if(matches==0){s.Advice="没有配对，再拉一次。";return;}int kind=a==b||a==c?a:b;
            if(kind==56){int count=matches==3?20:4;for(int i=0;i<count;i++)Suns.Add(new Sun{X=3.5f+i*0.025f,Y=0f,TargetY=0.6f});s.Advice="阳光组合！";}
            else if(kind==57){for(int i=0;i<(matches==3?5:1);i++)SpawnAdventureCoin(AdventureCoinKind.Diamond,4f+i*0.2f,0);s.Advice="钻石组合！";}
            else{for(int i=0;i<(matches==3?3:1);i++)AddAdventurePacket((PlantKind)kind);s.Advice=matches==3?"三个相同植物！":"两个相同植物！";}
        }
        private void AddModeFish()
        {var s=ModeSession;s.Fish.Add(new ModeFish{X=100f+(float)random.NextDouble()*600f,Y=140f+(float)random.NextDouble()*300f,Angle=(float)random.NextDouble()*(float)Math.PI*2,SunTimer=10f+5f*(float)random.NextDouble()});}
        public bool BuyModeFish()
        {var s=ModeSession;if(s==null||s.Definition.Mechanic!=ModeMechanic.Aquarium||State!=MatchState.Playing||SunAmount<100||s.Fish.Count>100)return false;SunAmount-=100;AddModeFish();return true;}
        public bool FeedModeFish(float x,float y)
        {var s=ModeSession;if(s==null||s.Definition.Mechanic!=ModeMechanic.Aquarium||State!=MatchState.Playing||SunAmount<5||s.Food.Count>=3||x<80||x>720||y<90||y>430)return false;SunAmount-=5;s.Food.Add(new ModeFood{X=x,Y=y});return true;}
        public bool BuyModeAquariumTrophy()
        {var s=ModeSession;if(s==null||s.Definition.Mechanic!=ModeMechanic.Aquarium||State!=MatchState.Playing||SunAmount<1000)return false;SunAmount-=1000;ClearModeStage();return true;}
        private void TickModeAquarium(float dt)
        {
            var s=ModeSession;
            foreach(var food in s.Food.ToArray()){food.Age+=dt;food.Y+=15f*dt;if(food.Y>=500)s.Food.Remove(food);}
            foreach(var fish in s.Fish.ToArray())
            {
                fish.Hunger-=dt;fish.SunTimer-=dt;if(fish.Hunger<=0){s.Fish.Remove(fish);continue;}
                ModeFood closest=null;float dist=float.MaxValue;
                if(fish.Hunger<=15f)foreach(var food in s.Food)if(food.Age>=0.15f){float dx=food.X-fish.X,dy=food.Y-fish.Y;float d=dx*dx+dy*dy;if(d<dist){dist=d;closest=food;}}
                fish.PhaseRemaining=Math.Max(0,fish.PhaseRemaining-dt);
                if(closest!=null)
                {
                    if(dist<2500){fish.Hunger=Math.Min(27f,fish.Hunger+20f);s.Food.Remove(closest);fish.SwimPhase=1;fish.PhaseRemaining=1f;}
                    else{fish.Angle=(float)Math.Atan2(closest.Y-fish.Y,closest.X-fish.X);fish.SwimPhase=0;}
                }
                else if(fish.PhaseRemaining<=0)
                {
                    int choice=random.Next(7);fish.PhaseRemaining=3f+random.Next(701)*0.01f;
                    if(choice<=4){fish.SwimPhase=0;fish.Angle=(float)random.NextDouble()*(float)Math.PI*2;}
                    else{fish.SwimPhase=2;fish.Angle=choice==5?0f:(float)Math.PI;}
                }
                float vx=(float)Math.Cos(fish.Angle),vy=(float)Math.Sin(fish.Angle);
                bool wall=fish.X<50&&vx<0||fish.X>730&&vx>0||fish.Y<140&&vy<0||fish.Y>440&&vy>0;
                float maxSpeed=fish.SwimPhase==0?50f:fish.SwimPhase==2?30f:5f;
                if(wall){maxSpeed=fish.Speed*0.3f;fish.PhaseRemaining=Math.Min(1f,fish.PhaseRemaining);}
                else if(fish.SwimPhase==2){if(fish.X<250&&vx<0)fish.Angle=0;if(fish.X>600&&vx>0)fish.Angle=3.1416f;}
                fish.Speed=Math.Min(maxSpeed,fish.Speed+100f*dt);fish.X+=vx*fish.Speed*dt;fish.Y+=vy*fish.Speed*dt;
                if(fish.SunTimer<=0){Suns.Add(new Sun{X=(fish.X-40f)/80f,Y=(fish.Y-80f)/(Adventure.Level.HasPool||Adventure.Level.HasRoof?85f:100f),TargetY=(fish.Y-80f)/(Adventure.Level.HasPool||Adventure.Level.HasRoof?85f:100f)});fish.SunTimer=10f+(float)random.NextDouble()*5f;}
            }
            if(s.Fish.Count==0){State=MatchState.Lost;Adventure.Phase=AdventurePhase.Lost;s.Phase=ModePhase.Lost;Raise(GameEvent.Lost);}
        }
        private void InitializeModePortals()
        {
            var s=ModeSession;s.Portals.Clear();s.Portals.Add(new ModePortal{Column=2,Row=0,Pair=0});s.Portals.Add(new ModePortal{Column=9,Row=1,Pair=0});
            s.Portals.Add(new ModePortal{Column=9,Row=3,Pair=1});s.Portals.Add(new ModePortal{Column=2,Row=4,Pair=1});s.PortalTimer=90f;
        }
        private void TickModePortals(float dt)
        {
            var s=ModeSession;s.PortalTimer-=dt;
            if(s.PortalTimer<=0)
            {
                var portal=s.Portals[random.Next(s.Portals.Count)];var cells=new List<int>();
                for(int r=0;r<5;r++)for(int c=0;c<10;c++)if(!s.Portals.Exists(p=>p!=portal&&p.Row==r)&&!Plants.Exists(p=>p.Row==r&&p.Column==c))cells.Add(r*10+c);
                if(cells.Count>0){int cell=cells[random.Next(cells.Count)];portal.Row=cell/10;portal.Column=cell%10;}s.PortalTimer=60f;
            }
            foreach(var z in Zombies)
            {
                z.ModePortalCooldown=Math.Max(0,z.ModePortalCooldown-dt);if(z.ModePortalCooldown>0)continue;
                foreach(var entrance in s.Portals)if(z.Row==entrance.Row&&Math.Abs(z.X-(entrance.Column+0.5f))<0.12f)
                {var exit=s.Portals.Find(p=>p!=entrance&&p.Pair==entrance.Pair);if(exit!=null){z.Row=exit.Row;z.X=exit.Column+(z.Hypnotized||z.Kind==ZombieKind.Digger&&z.AbilityPhase==2?0.65f:0.35f);z.ModePortalCooldown=0f;}break;}
            }
            foreach(var pea in Peas)
            {
                foreach(var entrance in s.Portals)if(pea.Row==entrance.Row&&Math.Abs(pea.X-(entrance.Column+0.5f))<0.055f)
                {var exit=s.Portals.Find(p=>p!=entrance&&p.Pair==entrance.Pair);if(exit!=null){pea.Row=exit.Row;pea.FlightRow=exit.Row;pea.X=exit.Column+0.65f;}break;}
            }
        }
        public bool IsModeZombieVisible(Zombie zombie)
        {return ModeSession==null||ModeSession.Definition.Mechanic!=ModeMechanic.Invisible;}
        // PC types: 26 pea, 27 wall-nut, 28 jalapeno, 29 gatling, 30 squash, 31 tall-nut; 0x53F110..0x53F857.
        private void TickModeSpecialZombies(float dt)
        {
            var s=ModeSession;
            foreach(var z in Zombies.ToArray())
            {
                int raw=(int)z.Kind;
                if(raw>=26&&z.ModeHead<0)
                {
                    z.ModeHead=raw;z.Kind=raw==32?ZombieKind.Gargantuar:ZombieKind.Normal;
                    if(raw==27)z.ArmorHealth=1100f;else if(raw==31)z.ArmorHealth=2200f;else if(raw==28){z.Health=500f;z.StartingHealth=500f;z.ModeHeadTimer=2f*(275+random.Next(175))/(z.Speed*80f);}else if(raw==32){z.Health=6000f;z.StartingHealth=6000f;z.Speed=0.12f;}
                }
                if(s.Definition.Mechanic==ModeMechanic.Speed&&z.ModeHead!=-2){z.Speed*=2f;z.ModeHead=-2;}
                if(z.ModeHead<26||z.ModeHead>31||z.Headless||z.FrozenRemaining>0)continue;float headBefore=z.ModeHeadTimer;z.ModeHeadTimer-=dt*(z.ChillRemaining>0?0.5f:1f);
                if(z.ModeHead==26&&z.ModeHeadTimer<=0)
                {s.HeadShots.Add(new ModeHeadShot{X=z.X,Row=z.Row,Hypnotized=z.Hypnotized});z.ModeHeadTimer=1.5f;}
                else if(z.ModeHead==29)
                {foreach(float threshold in new[]{0.69f,0.51f,0.36f,0.18f})if(headBefore>threshold&&z.ModeHeadTimer<=threshold)s.HeadShots.Add(new ModeHeadShot{X=z.X,Row=z.Row,Hypnotized=z.Hypnotized});if(z.ModeHeadTimer<=0)z.ModeHeadTimer=1.5f;}
                else if(z.ModeHead==28&&z.ModeHeadTimer<=0)
                {foreach(var p in Plants.ToArray())if(p.Row==z.Row)RemoveAdventurePlantAndStack(p);KillAdventureZombie(z);EmitLaneFlame(z.Row);Raise(GameEvent.JalapenoExplosion);}
                else if(z.ModeHead==30&&Plants.Exists(p=>p.Row==z.Row&&Math.Abs(p.Column+0.5f-z.X)<0.8f))
                {foreach(var p in Plants.ToArray())if(p.Row==z.Row&&Math.Abs(p.Column+0.5f-z.X)<1f)RemoveAdventurePlantAndStack(p);KillAdventureZombie(z);Raise(GameEvent.Explosion);}
            }
            foreach(var shot in s.HeadShots.ToArray())
            {
                if(shot.Hypnotized)
                {shot.X+=4.5f*dt;var enemy=Zombies.Find(z=>!z.Hypnotized&&z.Row==(int)shot.Row&&Math.Abs(z.X-shot.X)<0.35f);if(enemy!=null){HitAdventureZombie(enemy,shot.Damage);s.HeadShots.Remove(shot);}else if(shot.X>12)s.HeadShots.Remove(shot);continue;}
                shot.X-=4.5f*dt;var plant=Plants.FindLast(p=>p.Row==(int)shot.Row&&Math.Abs(p.Column+0.5f-shot.X)<0.35f);
                if(plant!=null){plant.Health-=shot.Damage;if(plant.Health<=0)RemoveAdventurePlantAndStack(plant);s.HeadShots.Remove(shot);}else if(shot.X<-1)s.HeadShots.Remove(shot);
            }
        }
    }
}
