using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        public AdventureBoard Adventure { get; private set; }

        partial void InitializeAdventure(ref bool initialized)
        {
            var level = AdventureLevels.ForRun(Rules.AdventureLevel,Rules.AdventureCycle,Rules.AdventurePacketSlots);
            RowCount = level.Rows;
            SunAmount = level.StartingSun;
            Adventure = new AdventureBoard {
                Level = level, Phase = level.ChoosesSeeds ? AdventurePhase.ChoosingSeeds : AdventurePhase.Playing,
                SelectedSeeds = level.DefaultSeeds, Waves = AdventureWavePlan.Build(level, random),
                Tutorial = level.IsReplay ? AdventureTutorial.Complete : level.Number == 1 ? AdventureTutorial.ChooseSeed : level.Number == 2 ? AdventureTutorial.PlantSunflower :
                    level.Number == 5 ? AdventureTutorial.Shovel : level.Number == 11 ? AdventureTutorial.NightMushrooms :
                    level.Number == 21 ? AdventureTutorial.PoolSupport : level.Number == 31 ? AdventureTutorial.FogVisibility :
                    level.Number == 41 ? AdventureTutorial.RoofPots : level.Mode == AdventureMode.Bowling ? AdventureTutorial.Bowling :
                    level.Mode == AdventureMode.WhackAZombie ? AdventureTutorial.Hammer : level.Mode == AdventureMode.Vasebreaker ? AdventureTutorial.Vases :
                    level.Mode == AdventureMode.Boss ? AdventureTutorial.Boss : level.HasConveyor ? AdventureTutorial.Conveyor : AdventureTutorial.Complete,
                WaveCountdown = !level.IsReplay && level.Number == 2 ? 50f : level.Mode == AdventureMode.Bowling || level.Mode == AdventureMode.WhackAZombie ||
                    level.Mode == AdventureMode.LittleTrouble || level.Mode == AdventureMode.Storm || level.Mode == AdventureMode.Bungee ? 2f : 18f,
                NextPacket = 4f, NextSkySun = !level.IsReplay && level.Number == 1 ? 7f : 4.25f + (float)random.NextDouble() * 2.75f,
                NextHammerSun = 8f, BossHealth = level.Mode == AdventureMode.Boss ? 40000f : 0f, BossTimer = 12f
            };
            InitializeAdventureSeedAccess();
            InitializeReplaySeedChoices();
            Adventure.WaveInitialCountdown = Adventure.WaveCountdown;
            Mowers.Clear();
            for (int r = 0; r < RowCount; r++) Mowers.Add(new Mower { Row = r, X = -0.5f,
                Available = Rules.MowersEnabled && level.IsLawnRow(r) && (!level.HasRoof || Rules.AdventureRoofCleaners) });
            if (level.HasRoof)
            {
                int columns = level.Number == 41 ? 5 : level.Number == 42 ? 4 : 3;
                for (int r = 0; r < RowCount; r++) for (int c = 0; c < columns; c++)
                    PlaceAdventurePlant(PlantKind.FlowerPot, r, c);
            }
            if (level.Number == 5 && !level.IsReplay) for (int c = 3; c <= 5; c++) PlaceAdventurePlant(PlantKind.Peashooter, 2, c);
            if (level.Terrain == AdventureTerrain.Night && level.Mode != AdventureMode.Vasebreaker) InitializeAdventureGraves();
            if (level.Mode == AdventureMode.Vasebreaker) InitializeVaseStage();
            if (level.Mode == AdventureMode.Bowling) AddAdventurePacket(PlantKind.Wallnut);
            if (level.Mode == AdventureMode.Boss)
            {
                AddAdventurePacket(PlantKind.CabbagePult); AddAdventurePacket(PlantKind.Jalapeno);
                AddAdventurePacket(PlantKind.CabbagePult); AddAdventurePacket(PlantKind.IceShroom);
                Adventure.NextPacket = 10f;
            }
            InitializeAdventurePurchases();
            InitializeAdventureWaveScheduler();
            initialized = true;
        }
        public bool ChooseAdventureSeeds(IEnumerable<PlantKind> seeds)
        {
            List<PlantKind> selected;
            if(!TryPrepareAdventureSeeds(seeds,out selected))return false;
            Adventure.SelectedSeeds=selected.AsReadOnly();
            Adventure.Phase=AdventurePhase.Playing;
            return true;
        }
        /// <summary>Pure validation for hosts that must persist a selection before publishing gameplay.</summary>
        public bool CanChooseAdventureSeeds(IEnumerable<PlantKind> seeds)
        {List<PlantKind> selected;return TryPrepareAdventureSeeds(seeds,out selected);}
        private bool TryPrepareAdventureSeeds(IEnumerable<PlantKind> seeds,out List<PlantKind> selected)
        {
            selected=null;
            if(!IsAdventure || State!=MatchState.Playing || Adventure.Phase!=AdventurePhase.ChoosingSeeds || seeds==null)return false;
            var next=new List<PlantKind>();
            foreach(var kind in seeds)
            {
                if(next.Contains(kind) || !ContainsSeed(Adventure.AvailableSeeds,kind) || kind==PlantKind.Imitater && !Adventure.ImitaterTarget.HasValue)return false;
                next.Add(kind);
            }
            if(next.Count!=Math.Min(Adventure.Level.PacketSlots,Adventure.AvailableSeeds.Count))return false;
            foreach(var kind in Adventure.RequiredSeeds)if(!next.Contains(kind))return false;
            selected=next;return true;
        }
        public bool ChooseDefaultAdventureSeeds() { return ChooseAdventureSeeds(Adventure == null ? null : Adventure.SelectedSeeds); }
        public bool SelectAdventureSeed(PlantKind kind)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing || !ContainsSeed(Adventure.SelectedSeeds, kind)) return false;
            Adventure.SelectedSeed = kind;
            if (Adventure.Level.Number == 1 && Adventure.Tutorial == AdventureTutorial.ChooseSeed)
            { Adventure.Tutorial = AdventureTutorial.PlantPeashooter; Adventure.Notice(AdventureNoticeKind.Tutorial, (int)Adventure.Tutorial); }
            return true;
        }
        public bool AcknowledgeAdventureTutorial()
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Tutorial == AdventureTutorial.Complete) return false;
            // Interactive first two tutorials complete through real actions.
            if (Adventure.Level.Number <= 2 || Adventure.Level.Number == 5) return false;
            Adventure.Tutorial = AdventureTutorial.Complete;
            return true;
        }
        partial void CollectAdventureSun()
        {
            SettleAdventureSunReservation();
            Adventure.CollectedSun = true;
            if (Adventure.Level.Number == 1 && Adventure.Tutorial == AdventureTutorial.CollectSun)
                Adventure.Tutorial = AdventureTutorial.Complete;
        }
        public bool CollectAdventureReward()
        {
            if (!IsAdventure || Adventure.Phase != AdventurePhase.Reward || State != MatchState.Won) return false;
            Adventure.Phase = AdventurePhase.Finished;
            if (Adventure.RewardPresentation != null) { Adventure.RewardPresentation.BeginCollection(); Raise(GameEvent.Won); }
            Adventure.Notice(AdventureNoticeKind.RewardCollected, Adventure.Level.Number);
            return true;
        }
        partial void AdvanceAdventure(float dt, ref bool handled)
        {
            handled = true;
            if (Adventure.Phase != AdventurePhase.Playing || Adventure.Level.Number == 5 && Adventure.Tutorial == AdventureTutorial.Shovel) return;
            Elapsed += dt; Adventure.Ticks++;
            AdventurePresentationTick(dt);
            AdvanceAdventurePacketFeedback(dt);
            for (int i = 0; i < seedCooldownTicks.Length; i++) if (seedCooldownTicks[i] > 0) { seedCooldownTicks[i]--; if (seedCooldownTicks[i] == 0) AdventurePacketReady((PlantKind)i); }
            Adventure.FogBlownRemaining = Math.Max(0f, Adventure.FogBlownRemaining - dt);
            TickCellTimers(Adventure.craters, dt); TickCellTimers(Adventure.ice, dt);
            AdvanceAdventureEconomy(dt);
            if (Adventure.Level.Mode == AdventureMode.Boss) AdvanceAdventureBoss(dt);
            else if (Adventure.Level.Mode != AdventureMode.Vasebreaker) AdvanceScheduledAdventureWaves(dt);
            UpdateSuns(dt);
            AdvanceAdventurePlants(dt);
            AdvanceAdventureProjectiles(dt);
            AdvanceAdventureZombies(dt);
            AdvanceAdventurePurchases(dt);
            if (State != MatchState.Playing) { Adventure.Phase = AdventurePhase.Lost; return; }
            AdvanceAdventureMowers(dt);
            AdvanceAdventureSpecials(dt);
            AdvanceAdventureUpgradeWorld(dt);
            for (int i = Flames.Count - 1; i >= 0; i--) { Flames[i].Age += dt; if (Flames[i].Age >= 1f) Flames.RemoveAt(i); }
            bool cleared = Zombies.Count == 0;
            if (Adventure.Level.Mode == AdventureMode.Boss) cleared &= Adventure.BossHealth <= 0f && Adventure.balls.Count == 0;
            else if (Adventure.Level.Mode == AdventureMode.Vasebreaker) cleared &= Adventure.VaseStage == 2 && Adventure.vases.Count == 0;
            else cleared &= Adventure.CurrentWave >= Adventure.Waves.Count && Adventure.FinalAmbush;
            if (cleared)
            {
                State = MatchState.Won; Adventure.Phase = AdventurePhase.Reward;
                if (ModeSession == null) SpawnAdventureRewardPresentation();
                Adventure.Notice(AdventureNoticeKind.RewardDropped, Adventure.Level.Number); if (ModeSession != null) Raise(GameEvent.Won);
            }
        }
        private static bool ContainsSeed(IReadOnlyList<PlantKind> seeds, PlantKind kind)
        { for (int i = 0; i < seeds.Count; i++) if (seeds[i] == kind) return true; return false; }
        private static void TickCellTimers(List<AdventureCellTimer> cells, float dt)
        { for (int i = cells.Count - 1; i >= 0; i--) { cells[i].Remaining -= dt; if (cells[i].Remaining <= 0f) cells.RemoveAt(i); } }
        private void AdvanceAdventureEconomy(float dt)
        {
            var a = Adventure;
            if (!a.Level.IsNight && !a.Level.HasConveyor && a.Level.Mode != AdventureMode.Vasebreaker)
            {
                a.NextSkySun -= dt;
                if (a.NextSkySun <= 0f)
                {
                    Suns.Add(CreateAdventurePresentationSun(null,25));
                    a.NextSkySun = 4.25f + Math.Min(9.5f, a.SkySunCount++ * 0.1f) + (float)random.NextDouble() * 2.75f;
                }
            }
            if (a.Level.HasConveyor)
            {
                a.NextPacket -= dt;
                if (a.NextPacket <= 0f)
                {
                    if (a.packets.Count < 10)
                    {
                        int total = 0; foreach (SeedWeight s in a.Level.ConveyorSeeds) total += AdventureConveyorWeight(s);
                        if(total>0)
                        {
                            int draw = random.Next(total);
                            foreach (SeedWeight s in a.Level.ConveyorSeeds)
                            { draw -= AdventureConveyorWeight(s); if (draw < 0) { AddAdventurePacket(s.Kind); a.LastConveyorSeed=s.Kind; break; } }
                        }
                    }
                    a.NextPacket += (a.packets.Count > 8 ? 10f : a.packets.Count > 6 ? 5f : a.packets.Count > 4 ? 4.25f : 4f) * (a.Level.Mode==AdventureMode.Boss?0.875f:1f);
                }
            }
            for (int i = a.packets.Count - 1; i >= 0; i--)
            { a.packets[i].Age += dt; if (a.Level.Mode == AdventureMode.Vasebreaker && a.packets[i].Age >= 75f) a.packets.RemoveAt(i); }
            a.StormFlash = a.Level.Mode == AdventureMode.Storm && a.Ticks % 600 < 150;
        }
        private int AdventureConveyorWeight(SeedWeight seed)
        {
            int queued=Adventure.packets.FindAll(p=>p.Kind==seed.Kind).Count;
            int planted=Plants.FindAll(p=>p.Kind==seed.Kind).Count;
            int weight=seed.Weight;
            if(seed.Kind==PlantKind.GraveBuster && Adventure.graves.Count<=queued+planted)return 0;
            if(seed.Kind==PlantKind.LilyPad || seed.Kind==PlantKind.FlowerPot)
            {int limit=seed.Kind==PlantKind.LilyPad?18:35;weight=(int)Math.Round(seed.Weight+(1f-seed.Weight)*Math.Min(limit,queued+planted)/limit);}
            if(Adventure.Level.Mode==AdventureMode.Boss && (seed.Kind==PlantKind.CabbagePult || seed.Kind==PlantKind.KernelPult || seed.Kind==PlantKind.MelonPult))
            {int empty=Plants.FindAll(p=>p.Kind==PlantKind.FlowerPot && AdventurePlantAt(p.Row,p.Column,PlantLayer.Main)==null).Count;if(empty<=2)weight/=5;else if(empty<=5)weight/=3;}
            if(Adventure.Level.ConveyorSeeds.Count>2)
            {if(queued>=4)weight=1;else if(queued>=3)weight=5;else if(Adventure.LastConveyorSeed==seed.Kind)weight/=2;}
            return Math.Max(0,weight);
        }
        private int ChooseAdventureZombieRow(ZombieKind kind)
        {
            var l = Adventure.Level;
            bool aquatic = kind == ZombieKind.Snorkel || kind == ZombieKind.DolphinRider;
            var choices = new List<int>();
            for (int r = 0; r < RowCount; r++)
                if (l.IsLawnRow(r) && (aquatic ? l.IsWaterRow(r) :
                    !l.IsWaterRow(r) || Adventure.CurrentWave>=5 && (kind == ZombieKind.Normal || kind == ZombieKind.Conehead || kind == ZombieKind.Buckethead || kind == ZombieKind.Flag || kind == ZombieKind.Balloon)))
                    if((kind!=ZombieKind.Gargantuar || r!=0) && (kind!=ZombieKind.Dancing || l.HasPool || l.IsLawnRow(r-1) && l.IsLawnRow(r+1))) choices.Add(r);
            if (choices.Count == 0) throw new InvalidOperationException("No legal spawn lane for " + kind);
            if(Adventure.RakeAttracting&&choices.Contains(Adventure.RakeRow)){Adventure.RakeAttracting=false;return Adventure.RakeRow;}
            // Approximate native smooth row picking by suppressing immediate lane repeats.
            if(choices.Count>1 && choices.Contains(Adventure.LastSpawnRow)) choices.Remove(Adventure.LastSpawnRow);
            int picked=choices[random.Next(choices.Count)];Adventure.LastSpawnRow=picked;return picked;
        }
        private void InitializeAdventureGraves()
        {
            int n = Adventure.Level.Number;
            if (Adventure.Level.Mode == AdventureMode.WhackAZombie)
            { for (int i = 0; i < 9; i++) AddGravesInColumn(4 + i / 3, 1); return; }
            if (n <= 13) { AddGravesInColumn(6,1); AddGravesInColumn(7,1); AddGravesInColumn(8,2); }
            else if (n == 14 || n == 16 || n == 18) { AddGravesInColumn(5,1); AddGravesInColumn(6,1); AddGravesInColumn(7,2); AddGravesInColumn(8,3); }
            else if (n < 20) { AddGravesInColumn(4,1); AddGravesInColumn(5,2); AddGravesInColumn(6,2); AddGravesInColumn(7,3); AddGravesInColumn(8,3); }
            else { AddGravesInColumn(3,1); AddGravesInColumn(4,2); AddGravesInColumn(5,2); AddGravesInColumn(6,2); AddGravesInColumn(7,3); AddGravesInColumn(8,3); }
        }
        private void AddGravesInColumn(int column, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var rows = new List<int>();
                for (int r = 0; r < 5; r++) if (!Adventure.graves.Exists(g => g.Column == column && g.Row == r)) rows.Add(r);
                if (rows.Count > 0) Adventure.graves.Add(new AdventureCellTimer { Column = column, Row = rows[random.Next(rows.Count)], Remaining = float.MaxValue });
            }
        }
        private AdventurePacket AddAdventurePacket(PlantKind kind, int? row = null, int? column = null)
        {
            var p = new AdventurePacket { Id = ++Adventure.PacketId, Kind = kind, FromVaseRow = row, FromVaseColumn = column };
            Adventure.packets.Add(p); InitializeAdventureConveyorPacket(p, Adventure.packets.Count - 1); Adventure.Notice(AdventureNoticeKind.ConveyorPacket, p.Id);
            return p;
        }
    }
}
