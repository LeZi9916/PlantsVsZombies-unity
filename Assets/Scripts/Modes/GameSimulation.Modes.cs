using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using PvZReplica.Modes;

namespace PvZReplica
{
    public sealed partial class Plant { public bool ModeFacesLeft; internal float ModeFireTimer; }
    public sealed partial class Zombie { public int ModeHead=-1; internal float ModeHeadTimer=1.5f; internal float ModePortalCooldown; }
    public sealed partial class GameSimulation
    {
        public ModeSession ModeSession { get; private set; }
        private double modeAccumulated;
        public bool IsIndependentMode { get { return ModeSession!=null; } }
        public float ModeSimulationRemainder { get { return (float)Math.Max(0d,Math.Min(SimulationStep,modeAccumulated)); } }
        private bool ModeNeedsFinalAmbush
        {get{var m=ModeSession.Definition.Mechanic;return m!=ModeMechanic.PeaHeads&&m!=ModeMechanic.Stars&&m!=ModeMechanic.Slots&&m!=ModeMechanic.Beghouled&&m!=ModeMechanic.BeghouledTwist&&m!=ModeMechanic.Whack&&m!=ModeMechanic.Vases&&m!=ModeMechanic.IZombie&&m!=ModeMechanic.Boss&&m!=ModeMechanic.Aquarium;}}
        internal void ConfigureMode(ModeSession session,AdventureSeedAccess access)
        {
            if(!IsAdventure||session==null)throw new InvalidOperationException("Modes require a prepared battle board.");
            ModeSession=session;var a=Adventure;var d=session.Definition;Rules.AdventureDisableLoot=d.Mechanic==ModeMechanic.IZombie;
            Plants.Clear();Zombies.Clear();Peas.Clear();Suns.Clear();Flames.Clear();
            adventureSunReservation=0;a.ConveyorTicks=0;
            a.packets.Clear();a.vases.Clear();a.graves.Clear();a.craters.Clear();a.ice.Clear();a.nuts.Clear();a.coins.Clear();a.cobShells.Clear();
            a.Tutorial=AdventureTutorial.Complete;a.Phase=AdventurePhase.Playing;a.CurrentWave=0;a.FinalAmbush=false;a.VaseStage=2;
            a.NextSkySun=4.25f+(float)random.NextDouble()*2.75f;a.NextPacket=4f;a.NextHammerSun=8f;a.WaveCountdown=d.ChoosesSeeds?18f:2f;
            var available=new List<PlantKind>();for(int id=0;id<session.UnlockedBasicSeeds;id++)available.Add((PlantKind)id);
            if(access!=null){foreach(var kind in access.AdditionalSeeds)available.Add(kind);a.ImitaterTarget=access.ImitaterTarget;}
            if(d.Mechanic==ModeMechanic.LastStand)available.RemoveAll(k=>k==PlantKind.Sunflower||k==PlantKind.SunShroom||k==PlantKind.TwinSunflower);
            a.AvailableSeeds=available.AsReadOnly();
            var defaults=new List<PlantKind>{PlantKind.Sunflower,PlantKind.Peashooter,PlantKind.Wallnut,PlantKind.PotatoMine,PlantKind.CherryBomb,PlantKind.SnowPea};
            if(a.Level.IsNight)defaults=new List<PlantKind>{PlantKind.SunShroom,PlantKind.PuffShroom,PlantKind.FumeShroom,PlantKind.GraveBuster,PlantKind.HypnoShroom,PlantKind.IceShroom};
            if(a.Level.HasPool){defaults[2]=PlantKind.LilyPad;defaults[3]=PlantKind.TangleKelp;}
            if(a.Level.HasRoof)defaults=new List<PlantKind>{PlantKind.Sunflower,PlantKind.FlowerPot,PlantKind.CabbagePult,PlantKind.KernelPult,PlantKind.Wallnut,PlantKind.CherryBomb};
            if(d.Mechanic==ModeMechanic.Stars)defaults[1]=PlantKind.Starfruit;
            if(d.Mechanic==ModeMechanic.LastStand)defaults=new List<PlantKind>{PlantKind.LilyPad,PlantKind.Repeater,PlantKind.Torchwood,PlantKind.TallNut,PlantKind.Pumpkin,PlantKind.Cattail};
            defaults.RemoveAll(k=>!available.Contains(k));foreach(var k in available)if(defaults.Count<session.PacketSlots&&!defaults.Contains(k))defaults.Add(k);
            a.SelectedSeeds=defaults.AsReadOnly();
            if(d.Mechanic==ModeMechanic.Whack)a.SelectedSeeds=Array.AsReadOnly(new[]{PlantKind.PotatoMine,PlantKind.CherryBomb,PlantKind.GraveBuster});
            SunAmount=d.Mechanic==ModeMechanic.LastStand?5000:d.Mechanic==ModeMechanic.IZombie?150:d.Puzzle||d.Mechanic==ModeMechanic.Whack||IsModeMatch?0:50;
            for(int r=0;r<RowCount;r++)Mowers[r].Available=!d.Puzzle&&d.Mechanic!=ModeMechanic.Aquarium&&(!a.Level.HasRoof||Rules.AdventureRoofCleaners);
            if(a.Level.HasRoof)for(int r=0;r<RowCount;r++)for(int c=0;c<3;c++)PlaceAdventurePlant(PlantKind.FlowerPot,r,c);
            if(a.Level.Terrain==AdventureTerrain.Night&&d.Mechanic!=ModeMechanic.Vases&&d.Mechanic!=ModeMechanic.IZombie&&d.Mechanic!=ModeMechanic.Beghouled&&d.Mechanic!=ModeMechanic.BeghouledTwist&&d.Mechanic!=ModeMechanic.Portals)InitializeAdventureGraves();
            if(d.Mechanic==ModeMechanic.Vases)InitializeModeVases();
            else if(d.Mechanic==ModeMechanic.IZombie)InitializeModeIZombie();
            else if(IsModeMatch)InitializeModeMatch();
            else if(d.Mechanic==ModeMechanic.Aquarium){SunAmount=50;AddModeFish();AddModeFish();}
            else if(d.Mechanic==ModeMechanic.Portals)InitializeModePortals();
            else if(d.Mechanic==ModeMechanic.Bobsled)for(int r=0;r<RowCount;r++)if(!a.Level.IsWaterRow(r))for(int c=3;c<9;c++)a.ice.Add(new AdventureCellTimer{Row=r,Column=c,Remaining=float.MaxValue});
            InitializeModePackets();
            a.Waves=ModeWavePlan.Build(session,random);
            ResetAdventureWaveSchedule((int)Math.Round(a.WaveCountdown*100),ModeNeedsFinalAmbush);
            if(d.Mechanic==ModeMechanic.Whack)a.FinalAmbush=false;
            session.Phase=d.ChoosesSeeds?ModePhase.ChoosingSeeds:ModePhase.Playing;
            if(d.ChoosesSeeds)a.Phase=AdventurePhase.ChoosingSeeds;
            session.Advice=ModeInstruction();
        }
        private bool IsModeMatch { get { return ModeSession.Definition.Mechanic==ModeMechanic.Beghouled||ModeSession.Definition.Mechanic==ModeMechanic.BeghouledTwist; } }
        public bool ChooseModeSeeds(IEnumerable<PlantKind> seeds)
        {
            if(ModeSession==null||ModeSession.Phase!=ModePhase.ChoosingSeeds||seeds==null)return false;
            var next=new List<PlantKind>();
            foreach(var kind in seeds){if(next.Contains(kind)||!ContainsSeed(Adventure.AvailableSeeds,kind)||kind==PlantKind.Imitater&&!Adventure.ImitaterTarget.HasValue)return false;next.Add(kind);}
            if(next.Count!=Math.Min(ModeSession.PacketSlots,Adventure.AvailableSeeds.Count))return false;
            Adventure.SelectedSeeds=next.AsReadOnly();Adventure.Phase=AdventurePhase.Playing;
            ModeSession.Phase=ModeSession.Definition.Mechanic==ModeMechanic.LastStand?ModePhase.Preparing:ModePhase.Playing;return true;
        }
        public void ApplyModePurchases(bool firstAid,Func<bool> placeRake)
        {
            if(ModeSession==null)return;Rules.AdventureFirstAid=firstAid;
            Rules.AdventurePlaceRake=ModeSession.Definition.SupportsRake?placeRake:null;
            if(Adventure.RakeRow<0)InitializeAdventurePurchases();
        }
        public bool SetModeImitaterTarget(PlantKind kind)
        {if(ModeSession==null||ModeSession.Phase!=ModePhase.ChoosingSeeds||(int)kind<0||(int)kind>39||!ContainsSeed(Adventure.AvailableSeeds,PlantKind.Imitater))return false;Adventure.ImitaterTarget=kind;return true;}
        public bool PlantModeVaseCherry(int row,int column)
        {
            if(ModeSession==null||ModeSession.Definition.Mechanic!=ModeMechanic.Vases||ModeSession.Phase!=ModePhase.Playing)return false;
            LastAdventurePlantingFailure=CheckAdventurePlanting(PlantKind.CherryBomb,row,column,false);
            if(LastAdventurePlantingFailure!=AdventurePlantingFailure.None)return false;
            SpendAdventureSun(Cost(PlantKind.CherryBomb));seedCooldownTicks[(int)PlantKind.CherryBomb]=DefinitionFor(PlantKind.CherryBomb).RechargeThresholdRaw+1;PlaceAdventurePlant(PlantKind.CherryBomb,row,column);return true;
        }
        public bool BeginModeDefense()
        {if(ModeSession==null||ModeSession.Phase!=ModePhase.Preparing)return false;ModeSession.Phase=ModePhase.Playing;Adventure.WaveCountdown=3f;ResetAdventureWaveSchedule(300,ModeNeedsFinalAmbush);return true;}
        public void UpdateMode(float dt)
        {
            if(ModeSession==null||dt<0f||float.IsNaN(dt)||float.IsInfinity(dt))return;
            if(State!=MatchState.Playing||ModeSession.Phase!=ModePhase.Playing)return;
            modeAccumulated+=dt;
            while(modeAccumulated+0.0000001>=SimulationStep&&State==MatchState.Playing&&ModeSession.Phase==ModePhase.Playing)
            {TickMode((float)SimulationStep);modeAccumulated-=SimulationStep;if(Math.Abs(modeAccumulated)<0.0000001)modeAccumulated=0d;}
        }
        private void TickMode(float dt)
        {
            var s=ModeSession;var d=s.Definition;Elapsed+=dt;Adventure.Ticks++;AdventurePresentationTick(dt);AdvanceAdventurePacketFeedback(dt);
            for(int i=0;i<seedCooldownTicks.Length;i++)if(seedCooldownTicks[i]>0){seedCooldownTicks[i]--;if(seedCooldownTicks[i]==0)AdventurePacketReady((PlantKind)i);}
            Adventure.FogBlownRemaining=Math.Max(0f,Adventure.FogBlownRemaining-dt);TickCellTimers(Adventure.craters,dt);TickCellTimers(Adventure.ice,dt);
            if(d.Mechanic==ModeMechanic.Aquarium){TickModeAquarium(dt);UpdateSuns(dt);return;}
            if(!d.Puzzle&&d.Mechanic!=ModeMechanic.LastStand&&(!d.UsesPackets||d.Mechanic==ModeMechanic.Slots)&&!IsModeMatch)AdvanceAdventureEconomy(dt);
            if(d.Mechanic==ModeMechanic.Slots)TickModeSlot(dt);
            if(d.UsesPackets)TickModePackets(dt);
            if(d.Mechanic==ModeMechanic.Boss)AdvanceAdventureBoss(dt);
            else if(!d.Puzzle)
            {
                if((d.Mechanic==ModeMechanic.Stars||d.Mechanic==ModeMechanic.Slots||IsModeMatch)&&Adventure.CurrentWave>=Adventure.Waves.Count&&Adventure.Waves.Count>0)
                {
                    if(s.ContinuousWaveTimer<0)s.ContinuousWaveTimer=25f;
                    s.ContinuousWaveTimer-=dt;float health=0;foreach(var z in Zombies)if(z.Wave==Adventure.Waves.Count-1&&!z.Hypnotized&&!z.Headless)health+=z.Health+z.ArmorHealth+z.ShieldHealth*0.2f;
                    if(s.ContinuousWaveTimer<21f&&WaveSchedule!=null&&health<=WaveSchedule.HealthToNextWave)s.ContinuousWaveTimer=Math.Min(2f,s.ContinuousWaveTimer);
                    if(s.ContinuousWaveTimer<=0){int last=Adventure.Waves.Count-1;var repeats=new List<ZombieKind>();foreach(var z in Adventure.Waves[last].Zombies)repeats.Add(z==ZombieKind.Flag?ZombieKind.Normal:z);var waves=new List<AdventureWave>(Adventure.Waves);waves[last]=new AdventureWave{Index=last,IsFlag=false,Zombies=repeats.AsReadOnly()};Adventure.Waves=waves.AsReadOnly();Adventure.CurrentWave=last;s.ContinuousWaveTimer=-1f;ResetAdventureWaveSchedule(1,false);}
                }
                if(d.Mechanic==ModeMechanic.Whack)TickModeWhack(dt);else AdvanceScheduledAdventureWaves(dt);
            }
            if(d.Mechanic==ModeMechanic.IZombie)BeforeModeIZombie();
            TickModeBackwardPlants(dt);UpdateSuns(dt);
            AdvanceAdventurePlants(dt);AdvanceAdventureProjectiles(dt);AdvanceAdventureZombies(dt);
            if(State==MatchState.Lost){s.Phase=ModePhase.Lost;return;}
            AdvanceAdventureMowers(dt);AdvanceAdventurePurchases(dt);
            if(d.Mechanic==ModeMechanic.Bowling)TickModeBowling(dt);
            else if(d.Mechanic!=ModeMechanic.Vases)AdvanceAdventureSpecials(dt);
            AdvanceAdventureUpgradeWorld(dt);TickModeSpecialZombies(dt);
            if(d.Mechanic==ModeMechanic.Portals)TickModePortals(dt);
            if(d.Mechanic==ModeMechanic.IZombie)AfterModeIZombie(dt);
            if(IsModeMatch)TickModeMatch(dt);
            for(int i=Flames.Count-1;i>=0;i--){Flames[i].Age+=dt;if(Flames[i].Age>=1f)Flames.RemoveAt(i);}
            bool cleared=false;
            if(d.Mechanic==ModeMechanic.Vases)cleared=Adventure.vases.Count==0&&Zombies.TrueForAll(z=>z.Hypnotized);
            else if(d.Mechanic==ModeMechanic.IZombie){cleared=true;foreach(bool brain in s.Brains)if(!brain)cleared=false;}
            else if(d.Mechanic==ModeMechanic.Stars)cleared=ModeStarCount()==14;
            else if(d.Mechanic==ModeMechanic.Slots)cleared=SunAmount>=2000;
            else if(IsModeMatch)cleared=s.Score>=75;
            else if(d.Mechanic==ModeMechanic.Boss)cleared=Adventure.BossHealth<=0f&&Zombies.Count==0&&Adventure.balls.Count==0;
            else cleared=Adventure.CurrentWave>=Adventure.Waves.Count&&Adventure.FinalAmbush&&Zombies.Count==0;
            if(cleared)ClearModeStage();
        }
        private void ClearModeStage()
        {
            var s=ModeSession;if(s.Phase!=ModePhase.Playing)return;s.ClearedStages=s.Stage;
            if(s.Definition.Survival)s.Score=s.FlagsCompleted;else if(s.Definition.Endless)s.Score=s.ClearedStages;else s.Score=Math.Max(1,s.Score);
            bool next=s.Definition.Endless||s.Stage<s.Definition.Stages;
            s.Phase=next?ModePhase.StageCleared:ModePhase.Won;Adventure.Phase=AdventurePhase.Reward;State=MatchState.Won;Raise(GameEvent.Won);
        }
        public bool ContinueModeStage()
        {
            var s=ModeSession;if(s==null||s.Phase!=ModePhase.StageCleared)return false;
            s.Stage++;s.SelectedCell=-1;s.FailureDelay=0;State=MatchState.Playing;Adventure.Phase=AdventurePhase.Playing;
            Zombies.Clear();Peas.Clear();Flames.Clear();Adventure.nuts.Clear();Adventure.balls.Clear();Adventure.packets.Clear();s.BackwardPackets.Clear();s.GiantPackets.Clear();s.GiantNuts.Clear();s.RainPackets.Clear();s.HeadShots.Clear();modeAccumulated=0;
            Adventure.CurrentWave=0;Adventure.TotalWavesSpawned=0;Adventure.FlagsSpawned=0;Adventure.FinalAmbush=false;Adventure.Warned=false;Adventure.PreviousWaveHealth=0;Adventure.WaveCountdown=18f;
            if(s.Definition.Mechanic==ModeMechanic.Vases){Plants.Clear();Suns.Clear();InitializeModeVases();s.Phase=ModePhase.Playing;}
            else if(s.Definition.Mechanic==ModeMechanic.IZombie){Plants.Clear();InitializeModeIZombie();s.Phase=ModePhase.Playing;}
            else if(s.Definition.Mechanic==ModeMechanic.LastStand){SunAmount+=250;s.Phase=ModePhase.Preparing;}
            else {s.Phase=ModePhase.ChoosingSeeds;Adventure.Phase=AdventurePhase.ChoosingSeeds;}
            Adventure.Waves=ModeWavePlan.Build(s,random);ResetAdventureWaveSchedule((int)Math.Round(Adventure.WaveCountdown*100),ModeNeedsFinalAmbush);s.ContinuousWaveTimer=-1f;s.Advice=ModeInstruction();return true;
        }
        public bool TryPlantMode(PlantKind kind,int row,int column)
        {
            var s=ModeSession;if(s==null||State!=MatchState.Playing||(s.Phase!=ModePhase.Playing&&s.Phase!=ModePhase.Preparing)||s.Definition.Puzzle||IsModeMatch||s.Definition.Mechanic==ModeMechanic.Aquarium)return false;
            if(s.Definition.UsesPackets)return false;
            if(s.Definition.Mechanic==ModeMechanic.Stars&&IsModeStarCell(row,column)&&kind!=PlantKind.Starfruit&&kind!=PlantKind.Pumpkin)return false;
            return TryPlant(kind,row,column);
        }
        public bool TryUseModePacket(int id,int row,int column)
        {
            var s=ModeSession;if(s==null||s.Phase!=ModePhase.Playing||State!=MatchState.Playing)return false;
            var p=Adventure.packets.Find(x=>x.Id==id);if(p==null)return false;
            if(s.Definition.Mechanic==ModeMechanic.Columns)
            {
                bool placed=false;for(int r=0;r<RowCount;r++)if(CheckAdventurePlanting(p.Kind,r,column,true)==AdventurePlantingFailure.None){PlaceAdventurePlant(p.Kind,r,column);placed=true;}
                if(placed)RemoveAdventurePacket(p);return placed;
            }
            bool giant=s.GiantPackets.Contains(id);
            if(!TryUseAdventurePacket(id,row,column))return false;
            if(giant){s.GiantPackets.Remove(id);if(Adventure.nuts.Count>0)s.GiantNuts.Add(Adventure.nuts[Adventure.nuts.Count-1]);}
            if(s.BackwardPackets.Remove(id)){var plant=AdventurePlantAt(row,column,PlantLayer.Main);if(plant!=null)plant.ModeFacesLeft=true;}
            return true;
        }
        private void TickModeBackwardPlants(float dt)
        {
            foreach(var p in Plants)if(p.ModeFacesLeft&&p.Kind!=PlantKind.Repeater)
            {
                p.ActionTimer=100000f;p.ModeFireTimer-=dt;
                if(p.ModeFireTimer<=0f&&Zombies.Exists(z=>z.Row==p.Row&&z.X<p.Column+0.5f&&!z.Hypnotized))
                {FireAdventurePea(p,p.Row,true,20f);FireAdventurePea(p,p.Row,true,20f).X+=0.3f;p.ModeFireTimer=1.45f;}
            }
        }
        public string ModeInstruction()
        {
            var d=ModeSession.Definition;
            if(d.Survival)return "生存第 "+ModeSession.Stage+" 轮；保留植物、阳光和割草机，完成本轮后重新选卡。";
            switch(d.Mechanic)
            {
                case ModeMechanic.Vases:return "点击罐子打开；拾取并使用种子包，消灭罐中的全部僵尸。";
                case ModeMechanic.IZombie:return "购买僵尸并放在线右侧；吃掉向日葵获得阳光，吃完五路大脑。";
                case ModeMechanic.Beghouled:return "点击相邻两株植物交换；横向或纵向三个相同植物消除，完成75次配对。";
                case ModeMechanic.BeghouledTwist:return "点击2×2植物块顺时针转动，连成三个相同植物，完成75次配对。";
                case ModeMechanic.Stars:return "在星形轮廓的14个位置种满杨桃。";
                case ModeMechanic.Aquarium:return "点击水中投放大脑（5阳光）；购买潜水僵尸，攒1000阳光购买奖杯。";
                case ModeMechanic.LastStand:return "用5000阳光布阵后开始；不能种阳光植物，守住五旗。";
                case ModeMechanic.Slots:return "每次拉动花费25阳光；收集2000阳光获胜。";
                case ModeMechanic.RainingSeeds:return "使用从天空落下的种子包保护草坪。";
                case ModeMechanic.Columns:return "在一格使用传送带植物，同一列的合法格子一起种植。";
                case ModeMechanic.Portals:return "植物子弹与僵尸会穿过成对传送门；留意传送门移动。";
                case ModeMechanic.PeaHeads:return "植物僵尸会发射豌豆，或携带坚果与爆炸植物的能力。";
                case ModeMechanic.Invisible:return "僵尸隐形，利用被啃咬的植物与命中特效判断位置。";
                default:return d.Name;
            }
        }
        public static bool IsModeStarCell(int row,int col)
        {return row==0&&col==3||row==1&&(col==3||col==4)||row==2&&col>=1&&col<=6||row==3&&col>=3&&col<=5||row==4&&(col==3||col==6);}
        public int ModeStarCount(){int n=0;for(int r=0;r<5;r++)for(int c=0;c<9;c++)if(IsModeStarCell(r,c)&&Plants.Exists(p=>p.Row==r&&p.Column==c&&p.Kind==PlantKind.Starfruit))n++;return n;}
    }
}
