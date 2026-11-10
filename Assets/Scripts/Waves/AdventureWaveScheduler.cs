using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using PvZReplica.VerifiedData;

namespace PvZReplica.Waves
{
    public enum WaveScheduleEventKind
    { FirstWaveApproaching, WaveApproaching, HugeWaveWarning, HugeWaveSound, MusicBurst, FinalWaveApproaching, WaveSpawned, Ambush, HugeWaveCleared, FinalWaveSound, SirenSound }
    public sealed class WaveScheduleEvent
    {
        public WaveScheduleEventKind Kind { get; internal set; }
        public int WaveIndex { get; internal set; }
        public long Tick { get; internal set; }
    }
    public sealed class AdventureWaveScheduleStatus
    {
        public int CountdownTicks { get; internal set; }
        public int InitialCountdownTicks { get; internal set; }
        public int WarningTicks { get; internal set; }
        public int AmbushTicks { get; internal set; }
        public int FinalWaveSoundTicks { get; internal set; }
        public bool TutorialReleased { get; internal set; }
        public int HealthAtWaveStart { get; internal set; }
        public int HealthToNextWave { get; internal set; }
        public bool IsFinalWave { get; internal set; }
        public bool AmbushComplete { get; internal set; }
        public long ElapsedTicks { get; internal set; }
        public float RemainingSeconds { get { return (WarningTicks > 0 ? WarningTicks : CountdownTicks) * 0.01f; } }
    }
}

namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        public Waves.AdventureWaveScheduleStatus WaveSchedule { get; private set; }
        public Action<Waves.WaveScheduleEvent> WaveScheduleEventRaised;
        private double scheduledWaveRemainder;
        private int scheduledIceTrapTicks;
        private bool scheduledFinalAmbushEnabled = true;
        private bool scheduledHonorTutorials = true;
        private bool firstTutorialPlantObserved;

        public void NotifyAdventureIceTrap(int ticks = 300)
        { scheduledIceTrapTicks = Math.Max(scheduledIceTrapTicks, Math.Max(0, ticks)); }

        // Local EXE: 0x41FDD0 (100 Hz countdown), 0x41EF60 (health), 0x41F010 (200-tick final ambush).
        private void InitializeAdventureWaveScheduler()
        {
            if (!IsAdventure || Adventure == null) return;
            ResetAdventureWaveSchedule(Adventure.Level.Number == 2 && !Adventure.Level.IsReplay ? 5000 : 1800, true, true);
        }

        /// <summary>After replacing Adventure.Waves/current wave, reset only the scheduler. It preserves CurrentWave and FlagsSpawned.</summary>
        public void ResetAdventureWaveSchedule(int firstCountdownTicks = -1, bool includeFinalAmbush = true, bool honorAdventureTutorials = false)
        {
            if (!IsAdventure || Adventure == null) return;
            int first = firstCountdownTicks < 0 ? Math.Max(1, (int)Math.Round(Adventure.WaveCountdown * 100.0)) : Math.Max(1, firstCountdownTicks);
            scheduledFinalAmbushEnabled = includeFinalAmbush; scheduledHonorTutorials = honorAdventureTutorials;
            WaveSchedule = new Waves.AdventureWaveScheduleStatus {
                CountdownTicks = first, InitialCountdownTicks = first, HealthToNextWave = -1,
                AmbushComplete = !includeFinalAmbush || Adventure.Level.Mode == AdventureMode.Boss || Adventure.Level.Mode == AdventureMode.Vasebreaker
            };
            scheduledWaveRemainder = 0; scheduledIceTrapTicks = 0; firstTutorialPlantObserved = false;
            Adventure.FinalAmbush = WaveSchedule.AmbushComplete; Adventure.Warned = false;
            SynchronizeScheduledWaveFields();
        }

        public void AdvanceScheduledAdventureWaves(float dt)
        {
            if (!IsAdventure || Adventure == null) return;
            if (WaveSchedule == null) InitializeAdventureWaveScheduler();
            if (WaveSchedule == null || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            var a = Adventure;
            // PC 0x41FDD0 blocks tutorial states 1/2/3, not the sunflower lesson (states 5/6/7).
            // The local tutorial enum is coarser; use the accepted live plant array to recognize the release.
            if (scheduledHonorTutorials && !a.Level.IsReplay && a.CurrentWave == 0)
            {
                int peashooters = 0, sunflowers = 0;
                foreach (Plant plant in Plants)
                { if (plant.Kind == PlantKind.Peashooter) peashooters++; if (plant.Kind == PlantKind.Sunflower) sunflowers++; }
                if (a.Level.Number == 1 && !WaveSchedule.TutorialReleased)
                {
                    if (!firstTutorialPlantObserved && peashooters > 0)
                    {
                        firstTutorialPlantObserved = true;
                        a.NextSkySun = 2f; // PC0x41CBD0: +0x5550=200 after first accepted placement.
                    }
                    if (peashooters < 2) return;
                    SetAdventureTutorialWaveCountdown(100); // local EXE 0x420970 state4 (mobile port says99).
                }
                else if (a.Level.Number == 2 && !WaveSchedule.TutorialReleased && sunflowers >= 3)
                    SetAdventureTutorialWaveCountdown(1000); // local EXE state8, while currentWave==0.
                else if (a.Level.Number == 5 && a.Tutorial == AdventureTutorial.Shovel) return;
            }
            scheduledWaveRemainder += dt;
            while (scheduledWaveRemainder + 0.00000001 >= 0.01)
            {
                scheduledWaveRemainder -= 0.01;
                AdvanceScheduledAdventureWaveTick();
            }
            if (Math.Abs(scheduledWaveRemainder) < 0.0000001) scheduledWaveRemainder = 0;
            SynchronizeScheduledWaveFields();
        }

        private void SetAdventureTutorialWaveCountdown(int ticks)
        {
            WaveSchedule.CountdownTicks = WaveSchedule.InitialCountdownTicks = ticks;
            WaveSchedule.HealthToNextWave = -1;
            WaveSchedule.TutorialReleased = true;
            SynchronizeScheduledWaveFields();
        }

        private void AdvanceScheduledAdventureWaveTick()
        {
            var s = WaveSchedule; var a = Adventure;
            s.ElapsedTicks++;
            if (s.FinalWaveSoundTicks > 0 && --s.FinalWaveSoundTicks == 0)
                RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.FinalWaveSound, Math.Max(0, a.Waves.Count - 1));
            if (scheduledIceTrapTicks > 0) scheduledIceTrapTicks--;
            if (s.AmbushTicks > 0 && --s.AmbushTicks == 0)
            {
                SpawnScheduledFinalAmbush();
                s.AmbushComplete = true; a.FinalAmbush = true;
                RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.Ambush, a.Waves.Count - 1);
            }
            if (a.CurrentWave >= a.Waves.Count) return;
            if (s.WarningTicks > 0)
            {
                s.WarningTicks--;
                if (s.WarningTicks == 726) RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.HugeWaveSound, a.CurrentWave);
                // Native 0x41FDD0 compares 401, or 701 for the night tune.
                if (s.WarningTicks == (a.Level.Terrain == AdventureTerrain.Night ? 701 : 401))
                    RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.MusicBurst, a.CurrentWave);
                if (s.WarningTicks > 0) return;
                a.Warned = false;
                RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.HugeWaveCleared, a.CurrentWave);
                AnnounceScheduledWave();
                s.CountdownTicks = 1;
            }
            if (s.CountdownTicks > 0) s.CountdownTicks--;
            if (s.CountdownTicks > 5 && s.InitialCountdownTicks - s.CountdownTicks > 400 &&
                ScheduledWaveHealth(a.CurrentWave - 1) <= s.HealthToNextWave && s.CountdownTicks > 200)
                s.CountdownTicks = 200;
            if (s.CountdownTicks == 5)
            {
                if (a.Waves[a.CurrentWave].IsFlag)
                {
                    s.WarningTicks = 750; a.Warned = true;
                    a.Notice(AdventureNoticeKind.WaveWarning, a.CurrentWave);
                    RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.HugeWaveWarning, a.CurrentWave);
                    return;
                }
                AnnounceScheduledWave();
            }
            if (s.CountdownTicks != 0) return;
            SpawnScheduledAdventureWave(a.Waves[a.CurrentWave]);
            s.HealthAtWaveStart = ScheduledWaveHealth(a.CurrentWave - 1);
            s.IsFinalWave = a.CurrentWave == a.Waves.Count;
            bool flagNext = a.CurrentWave < a.Waves.Count && a.Waves[a.CurrentWave].IsFlag;
            if (flagNext && a.Level.Mode != AdventureMode.Bowling)
            { s.HealthToNextWave = 0; s.CountdownTicks = 4500; }
            else
            {
                s.HealthToNextWave = (int)((0.5 + random.NextDouble() * 0.15) * s.HealthAtWaveStart);
                s.CountdownTicks = a.Level.Mode == AdventureMode.LittleTrouble ? 750 : 2500 + random.Next(600);
            }
            s.InitialCountdownTicks = s.CountdownTicks;
            if (s.IsFinalWave)
            {
                s.AmbushTicks = scheduledFinalAmbushEnabled ? 200 : 0;
                s.AmbushComplete = !scheduledFinalAmbushEnabled; a.FinalAmbush = !scheduledFinalAmbushEnabled;
            }
        }

        private int ScheduledWaveHealth(int wave)
        {
            int total = 0;
            foreach (Zombie z in Zombies)
                if (z.Wave == wave && z.Health > 0 && !z.Headless && !z.Hypnotized &&
                    z.Kind != ZombieKind.Bungee && !z.BungeeCarrier && z.LinkedZombieId == 0)
                    total += Math.Max(0, (int)z.Health) + Math.Max(0, (int)z.ArmorHealth) + Math.Max(0, (int)(z.ShieldHealth * 0.2f));
            return total;
        }

        private void AnnounceScheduledWave()
        {
            int index = Adventure.CurrentWave;
            int nativeMode = ModeSession == null ? 0 : ModeSession.Definition.Id;
            bool finalSurvival = ModeSession != null && ModeSession.Definition.Survival &&
                !ModeSession.Definition.Endless && ModeSession.Stage >= ModeSession.Definition.Stages;
            if (index == Adventure.Waves.Count - 1 && Waves.AdventureWaveNoticeRules.AnnounceFinal(nativeMode,
                finalSurvival, nativeMode == 0 && Adventure.Level.Mode == AdventureMode.Boss))
            {
                RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.FinalWaveApproaching, index);
                WaveSchedule.FinalWaveSoundTicks = 60; // 0x41FCD0: separate delayed sound, not this draw event.
            }
            if (index == 0) RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.FirstWaveApproaching, index);
            else
            {
                RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.WaveApproaching, index);
                bool whack = nativeMode == 30 || nativeMode == 0 && Adventure.Level.Mode == AdventureMode.WhackAZombie;
                if (whack ? index == Adventure.Waves.Count - 1 : Adventure.Waves[index].IsFlag)
                    RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.SirenSound, index);
            }
        }

        private void SpawnScheduledAdventureWave(AdventureWave wave)
        {
            var a = Adventure; int spawned = 0;
            foreach (ZombieKind entry in wave.Zombies)
            {
                ZombieKind kind = entry == ZombieKind.Bobsled && !a.ice.Exists(c => c.Column < 8) ? ZombieKind.Normal : entry;
                int row = ChooseAdventureZombieRow(kind);
                float x = 9.5f + spawned++ * 0.12f;
                if (a.Level.Mode == AdventureMode.WhackAZombie && a.graves.Count > 0)
                { var grave = a.graves[random.Next(a.graves.Count)]; row = grave.Row; x = grave.Column + 0.5f; }
                if (entry == ZombieKind.Bobsled)
                { for (int i = 0; i < 4; i++) SpawnAdventureZombie(kind, row, x + i * 0.2f, wave.Index); }
                else if (a.Level.Mode == AdventureMode.Bungee && kind != ZombieKind.Bungee && kind != ZombieKind.Zomboni)
                    DropAdventureZombie(kind, row, random.Next(4, 9), wave.Index);
                else SpawnAdventureZombie(kind, row, x, wave.Index);
            }
            if (wave.IsFlag) a.FlagsSpawned++;
            a.CurrentWave++; a.TotalWavesSpawned++; a.Warned = false;
            a.Notice(AdventureNoticeKind.WaveSpawned, wave.Index);
            RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind.WaveSpawned, wave.Index);
        }

        private void SpawnScheduledFinalAmbush()
        {
            var a = Adventure; int wave = Math.Max(0, a.Waves.Count - 1);
            // Ice-shroom freezes the native pool/roof ambush; no replacement timer or invented enemy is added.
            bool iced = scheduledIceTrapTicks > 0;
            if (a.Level.HasPool && !iced)
            {
                int n = a.Level.Number; int count = n == 21 || n == 22 || n == 31 || n == 32 ? 2 : 3;
                var cells = new List<int>(); for (int c = 5; c < Columns; c++) for (int r = 2; r <= 3; r++) cells.Add(c * 10 + r);
                for (int i = 0; i < count && cells.Count > 0; i++)
                {
                    int pick = random.Next(cells.Count), cell = cells[pick]; cells.RemoveAt(pick);
                    var z = SpawnAdventureZombie(PickScheduledAmbushZombie(false), cell % 10, cell / 10 + 0.5f, wave);
                    z.DropRemaining = 0.5f;
                    a.Notice(AdventureNoticeKind.PoolAmbush, (int)z.Kind, z.Row, cell / 10);
                }
            }
            else if (a.Level.HasRoof && !iced)
            {
                int count = a.Level.Number <= 42 ? 2 : 3;
                var cells = new List<int>();
                for (int c = 4; c < Columns; c++) for (int r = 0; r < RowCount; r++) cells.Add(c * 10 + r);
                for (int i = 0; i < count && cells.Count > 0; i++)
                {
                    int pick = random.Next(cells.Count), cell = cells[pick]; cells.RemoveAt(pick);
                    DropAdventureZombie(PickScheduledAmbushZombie(false), cell % 10, cell / 10, wave);
                }
            }
            if (a.Level.Terrain == AdventureTerrain.Night && a.Level.Mode != AdventureMode.WhackAZombie)
                foreach (var grave in a.graves)
                {
                    var z = SpawnAdventureZombie(PickScheduledAmbushZombie(true), grave.Row, grave.Column + 0.5f, wave);
                    z.DropRemaining = 0.5f;
                    a.Notice(AdventureNoticeKind.GraveRise, (int)z.Kind, grave.Row, grave.Column);
                }
        }

        private ZombieKind PickScheduledAmbushZombie(bool graves)
        {
            var candidates = new List<ZombieDefinition>(); int total = 0;
            int[] ids = graves ? new[] { 0, 2 } : new[] { 0, 2, 4 };
            var table = VerifiedDataCatalog.AdventureLevels[Adventure.Level.Number - 1];
            foreach (int id in ids)
            {
                var d = VerifiedDataCatalog.Zombies[id];
                if (id != 0 && ((!Adventure.Level.IsReplay && Adventure.Level.Number < d.BaseLevelGateRaw) || table.ZombieFlagsRaw[id] == 0)) continue;
                if (d.BaseSelectionWeightRaw <= 0) continue;
                candidates.Add(d); total += d.BaseSelectionWeightRaw;
            }
            if (total == 0) return ZombieKind.Normal;
            int ticket = random.Next(total);
            foreach (var d in candidates) { ticket -= d.BaseSelectionWeightRaw; if (ticket < 0) return (ZombieKind)d.Id; }
            return ZombieKind.Normal;
        }

        private void RaiseScheduledWaveEvent(Waves.WaveScheduleEventKind kind, int index)
        {
            if (WaveScheduleEventRaised != null)
                WaveScheduleEventRaised(new Waves.WaveScheduleEvent { Kind = kind, WaveIndex = index, Tick = WaveSchedule.ElapsedTicks });
        }
        private void SynchronizeScheduledWaveFields()
        {
            Adventure.WaveCountdown = WaveSchedule.RemainingSeconds;
            Adventure.WaveInitialCountdown = WaveSchedule.InitialCountdownTicks * 0.01f;
            Adventure.WaveTriggerHealth = WaveSchedule.HealthToNextWave;
            Adventure.PreviousWaveHealth = WaveSchedule.HealthAtWaveStart;
        }
    }
}
