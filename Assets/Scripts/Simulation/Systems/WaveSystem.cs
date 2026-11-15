#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns WaveSystem responsibilities for one deterministic match.</summary>
    internal sealed class WaveSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BungeeZombieActions operations.</summary>
        private BungeeZombieActions _bungeeZombieActions = null!;
        /// <summary>Provides WaterZombieActions operations.</summary>
        private WaterZombieActions _waterZombieActions = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal WaveSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="bungeeZombieActions">The BungeeZombieActions collaborator.</param>
        /// <param name="waterZombieActions">The WaterZombieActions collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BungeeZombieActions bungeeZombieActions, WaterZombieActions waterZombieActions, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _bungeeZombieActions = bungeeZombieActions;
            _waterZombieActions = waterZombieActions;
            _zombieSpawnSystem = zombieSpawnSystem;
        }

        /// <summary>Advances the original PC flag meter after this tick's combat and spawning updates.</summary>
        internal void AdvanceAdventureProgressMeter()
        {
            var board = _adventureModeRunner.Adventure;
            if (board.Level.Mode == AdventureMode.Boss)
            {
                _world.Presentation.ProgressMeter.SetBossHealth(board.BossHealth, board.BossMaxHealth);
                return;
            }

            var schedule = _world.WaveSchedule;
            if (schedule == null)
            {
                return;
            }

            var hasFlags = global::PlantsVsZombies.Adventure.AdventureProgressMeterState.HasFlags(board.Level.Number, board.Level.IsReplay);
            var wavesPerFlag = board.Level.IsReplay ? 10 : Math.Min(10, Math.Max(1, board.Waves.Count));
            _world.Presentation.ProgressMeter.Advance(board._ticks, board.CurrentWave, board.Waves.Count, wavesPerFlag, hasFlags,
                schedule.InitialCountdownTicks, schedule.CountdownTicks, schedule.HealthAtWaveStart,
                schedule.HealthToNextWave, ScheduledWaveHealth(board.CurrentWave - 1));
        }

        /// <summary>Gets the wave schedule, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal global::PlantsVsZombies.Waves.AdventureWaveScheduleStatus RequiredWaveSchedule => _world.WaveSchedule ?? throw new System.InvalidOperationException("The member 'WaveSchedule' is not available.");
        /// <summary>Performs notify adventure ice trap.</summary>
        /// <param name = "ticks">The ticks.</param>
        internal void NotifyAdventureIceTrap(int ticks = 300)
        {
            if (!_world.IsAdventure)
            {
                return;
            }

            _adventureModeRunner.Adventure.IceTrapTicks = Math.Max(_adventureModeRunner.Adventure.IceTrapTicks, Math.Max(0, ticks));
        }

        // Local EXE: 0x41FDD0 (100 Hz countdown), 0x41EF60 (health), 0x41F010 (200-tick final ambush).
        /// <summary>Performs initialize adventure wave scheduler.</summary>
        internal void InitializeAdventureWaveScheduler()
        {
            if (!_world.IsAdventure || _world._adventure == null)
            {
                return;
            }

            var firstCountdown = _adventureModeRunner.Adventure.Level.Mode == AdventureMode.WhackAZombie ? 200 : _adventureModeRunner.Adventure.Level.Number == 2 && !_adventureModeRunner.Adventure.Level.IsReplay ? 5000 : 1800;
            ResetAdventureWaveSchedule(firstCountdown, true, true);
        }

        /// <summary>After replacing Adventure.Waves/current wave, reset only the scheduler. It preserves CurrentWave and FlagsSpawned.</summary>
        internal void ResetAdventureWaveSchedule(int firstCountdownTicks = -1, bool includeFinalAmbush = true, bool honorAdventureTutorials = false)
        {
            if (!_world.IsAdventure || _world._adventure == null)
            {
                return;
            }

            var first = firstCountdownTicks < 0 ? Math.Max(1, (int)Math.Round(_adventureModeRunner.Adventure.WaveCountdown * 100.0)) : Math.Max(1, firstCountdownTicks);
            _world._scheduledFinalAmbushEnabled = includeFinalAmbush;
            _world._scheduledHonorTutorials = honorAdventureTutorials;
            _world.WaveSchedule = new global::PlantsVsZombies.Waves.AdventureWaveScheduleStatus
            {
                CountdownTicks = first,
                InitialCountdownTicks = first,
                HealthToNextWave = -1,
                AmbushComplete = !includeFinalAmbush || _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Boss || _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Vasebreaker
            };
            _world._scheduledWaveRemainder = 0;
            _world._firstTutorialPlantObserved = false;
            _adventureModeRunner.Adventure._finalAmbush = _world.WaveSchedule.AmbushComplete;
            _adventureModeRunner.Adventure._warned = false;
            SynchronizeScheduledWaveFields();
        }

        /// <summary>Performs advance scheduled adventure waves.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceScheduledAdventureWaves(float dt)
        {
            if (!_world.IsAdventure || _world._adventure == null)
            {
                return;
            }

            if (_world.WaveSchedule == null)
            {
                InitializeAdventureWaveScheduler();
            }

            if (_world.WaveSchedule == null || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                return;
            }

            var a = _adventureModeRunner.Adventure;
            // PC 0x41FDD0 blocks tutorial states 1/2/3, not the sunflower lesson (states 5/6/7).
            // The local tutorial enum is coarser; use the accepted live plant array to recognize the release.
            if (_world._scheduledHonorTutorials && !a.Level.IsReplay && a.CurrentWave == 0)
            {
                int peashooters = 0, sunflowers = 0;
                foreach (Plant plant in _world.Plants)
                {
                    if (plant.Kind == PlantKind.Peashooter)
                    {
                        peashooters++;
                    }

                    if (plant.Kind == PlantKind.Sunflower)
                    {
                        sunflowers++;
                    }
                }

                if (a.Level.Number == 1 && !_world.WaveSchedule.TutorialReleased)
                {
                    if (!_world._firstTutorialPlantObserved && peashooters > 0)
                    {
                        _world._firstTutorialPlantObserved = true;
                        a._nextSkySun = 2f; // PC0x41CBD0: +0x5550=200 after first accepted placement.
                    }

                    if (peashooters < 2)
                    {
                        return;
                    }

                    SetAdventureTutorialWaveCountdown(100); // local EXE 0x420970 state4 (mobile port says99).
                }
                else if (a.Level.Number == 2 && !_world.WaveSchedule.TutorialReleased && sunflowers >= 3)
                {
                    SetAdventureTutorialWaveCountdown(1000); // local EXE state8, while currentWave==0.
                }
                else if (a.Level.Number == 5 && a.Tutorial == AdventureTutorial.Shovel)
                {
                    return;
                }
            }

            _world._scheduledWaveRemainder += dt;
            while (_world._scheduledWaveRemainder + 0.00000001 >= 0.01)
            {
                _world._scheduledWaveRemainder -= 0.01;
                AdvanceScheduledAdventureWaveTick();
            }

            if (Math.Abs(_world._scheduledWaveRemainder) < 0.0000001)
            {
                _world._scheduledWaveRemainder = 0;
            }

            SynchronizeScheduledWaveFields();
        }

        /// <summary>Performs set adventure tutorial wave countdown.</summary>
        /// <param name = "ticks">The ticks.</param>
        internal void SetAdventureTutorialWaveCountdown(int ticks)
        {
            RequiredWaveSchedule.CountdownTicks = RequiredWaveSchedule.InitialCountdownTicks = ticks;
            RequiredWaveSchedule.HealthToNextWave = -1;
            RequiredWaveSchedule.TutorialReleased = true;
            SynchronizeScheduledWaveFields();
        }

        /// <summary>Performs advance scheduled adventure wave tick.</summary>
        internal void AdvanceScheduledAdventureWaveTick()
        {
            var s = RequiredWaveSchedule;
            var a = _adventureModeRunner.Adventure;
            s.ElapsedTicks++;
            if (s.FinalWaveSoundTicks > 0 && --s.FinalWaveSoundTicks == 0)
            {
                RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.FinalWaveSound, Math.Max(0, a.Waves.Count - 1));
            }

            if (s.AmbushTicks > 0 && --s.AmbushTicks == 0)
            {
                SpawnScheduledFinalAmbush();
                s.AmbushComplete = true;
                a._finalAmbush = true;
                RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.Ambush, a.Waves.Count - 1);
            }

            if (a.CurrentWave >= a.Waves.Count)
            {
                return;
            }

            if (s.WarningTicks > 0)
            {
                s.WarningTicks--;
                if (s.WarningTicks == 726)
                {
                    RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.HugeWaveSound, a.CurrentWave);
                }

                // Native 0x41FDD0 compares 401, or 701 for the night tune.
                if (s.WarningTicks == (a.Level.Terrain == AdventureTerrain.Night ? 701 : 401))
                {
                    RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.MusicBurst, a.CurrentWave);
                }

                if (s.WarningTicks > 0)
                {
                    return;
                }

                a._warned = false;
                RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.HugeWaveCleared, a.CurrentWave);
                AnnounceScheduledWave();
                s.CountdownTicks = 1;
            }

            if (s.CountdownTicks > 0)
            {
                s.CountdownTicks--;
            }

            if (s.CountdownTicks > 5 && s.InitialCountdownTicks - s.CountdownTicks > 400 && ScheduledWaveHealth(a.CurrentWave - 1) <= s.HealthToNextWave && s.CountdownTicks > 200)
            {
                s.CountdownTicks = 200;
            }

            if (s.CountdownTicks == 5)
            {
                if (a.Waves[a.CurrentWave].IsFlag)
                {
                    s.WarningTicks = 750;
                    a._warned = true;
                    a.Notice(AdventureNoticeKind.WaveWarning, a.CurrentWave);
                    RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.HugeWaveWarning, a.CurrentWave);
                    return;
                }

                AnnounceScheduledWave();
            }

            if (s.CountdownTicks != 0)
            {
                return;
            }

            SpawnScheduledAdventureWave(a.Waves[a.CurrentWave]);
            s.HealthAtWaveStart = ScheduledWaveHealth(a.CurrentWave - 1);
            s.IsFinalWave = a.CurrentWave == a.Waves.Count;
            var mode = _world.ModeSession == null ? 0 : _world.ModeSession.Definition.Id;
            var flagNext = a.CurrentWave < a.Waves.Count && a.Waves[a.CurrentWave].IsFlag;
            if (flagNext && a.Level.Mode != AdventureMode.Bowling && mode != 31)
            {
                s.HealthToNextWave = 0;
                s.CountdownTicks = 4500;
            }
            else
            {
                var threshold = AdventureSpawnRandom.NextRange(_world._random, 0.5f, 0.65f);
                s.HealthToNextWave = (int)((double)threshold * s.HealthAtWaveStart);
                s.CountdownTicks = a.Level.Mode == AdventureMode.LittleTrouble || mode == 27 || mode == 31
                    ? 750 : 2500 + _world._random.Next(600);
            }

            s.InitialCountdownTicks = s.CountdownTicks;
            if (s.IsFinalWave)
            {
                s.AmbushTicks = _world._scheduledFinalAmbushEnabled ? 200 : 0;
                s.AmbushComplete = !_world._scheduledFinalAmbushEnabled;
                a._finalAmbush = !_world._scheduledFinalAmbushEnabled;
            }
        }

        /// <summary>Performs scheduled wave health.</summary>
        /// <param name = "wave">The wave.</param>
        /// <returns>The resulting scheduled wave health.</returns>
        internal int ScheduledWaveHealth(int wave)
        {
            var total = 0;
            foreach (Zombie z in _world.Zombies)
            {
                if (z.Wave == wave && z.Health > 0 && !z.Headless && !z.Hypnotized && z.Kind != ZombieKind.Bungee && !z.BungeeCarrier && z.LinkedZombieId == 0)
                {
                    total += Math.Max(0, (int)z.Health) + Math.Max(0, (int)z.ArmorHealth)
                        + Math.Max(0, (int)(z.ShieldHealth * 0.2f)) + Math.Max(0, z.FlyingHealth);
                }
            }

            return total;
        }

        /// <summary>Performs announce scheduled wave.</summary>
        internal void AnnounceScheduledWave()
        {
            var index = _adventureModeRunner.Adventure.CurrentWave;
            var nativeMode = _world.ModeSession == null ? 0 : _world.ModeSession.Definition.Id;
            var finalSurvival = _world.ModeSession != null && _world.ModeSession.Definition.Survival && !_world.ModeSession.Definition.Endless && _world.ModeSession.Stage >= _world.ModeSession.Definition.Stages;
            if (index == _adventureModeRunner.Adventure.Waves.Count - 1 && global::PlantsVsZombies.Waves.AdventureWaveNoticeRules.AnnounceFinal(nativeMode, finalSurvival, nativeMode == 0 && _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Boss))
            {
                RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.FinalWaveApproaching, index);
                RequiredWaveSchedule.FinalWaveSoundTicks = 60; // 0x41FCD0: separate delayed sound, not this draw event.
            }

            if (index == 0)
            {
                RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.FirstWaveApproaching, index);
            }
            else
            {
                RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.WaveApproaching, index);
                var whack = nativeMode == 30 || nativeMode == 0 && _adventureModeRunner.Adventure.Level.Mode == AdventureMode.WhackAZombie;
                if (whack ? index == _adventureModeRunner.Adventure.Waves.Count - 1 : _adventureModeRunner.Adventure.Waves[index].IsFlag)
                {
                    RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.SirenSound, index);
                }
            }
        }

        /// <summary>Performs spawn scheduled adventure wave.</summary>
        /// <param name = "wave">The wave.</param>
        internal void SpawnScheduledAdventureWave(AdventureWave wave)
        {
            var a = _adventureModeRunner.Adventure;
            var dropWeights = a.Level.Mode == AdventureMode.Bungee ? BungeeZombieActions.CreateAdventureBungeeDropWeights() : null;
            foreach (var kind in wave.Zombies)
            {
                // Each actor samples PC Zombie::Init's native range independently.
                var x = 9.5f;
                if (kind == ZombieKind.Bobsled && !a._ice.Exists(ice => ice.RemainingTicks > 0 && ice.MinX < 700))
                {
                    for (var follower = 0; follower < 4; follower++)
                    {
                        var fallbackRow = _zombieSpawnSystem.ChooseAdventureZombieRow(ZombieKind.Normal);
                        _zombieSpawnSystem.SpawnAdventureBoardZombie(ZombieKind.Normal, fallbackRow, x, wave.Index);
                    }

                    continue;
                }

                if (a.Level.Mode == AdventureMode.Bungee && kind != ZombieKind.Bungee && kind != ZombieKind.Zomboni)
                {
                    var cell = _bungeeZombieActions.PickAdventureBungeeDropCell(dropWeights!);
                    _bungeeZombieActions.DropAdventureZombie(kind, cell % 10, cell / 10, wave.Index);
                    continue;
                }

                var row = kind == ZombieKind.Boss ? 0 : _zombieSpawnSystem.ChooseAdventureZombieRow(kind);
                if (a.Level.Mode == AdventureMode.WhackAZombie && a._graves.Count > 0)
                {
                    var grave = a._graves[_world._random.Next(a._graves.Count)];
                    row = grave.Row;
                    x = grave.Column + 0.5f;
                }

                var leader = _zombieSpawnSystem.SpawnAdventureBoardZombie(kind, row, x, wave.Index);
                if (kind == ZombieKind.Bobsled)
                {
                    for (var follower = 1; follower < 4; follower++)
                    {
                        var sled = _zombieSpawnSystem.SpawnAdventureZombie(kind, row, x, wave.Index);
                        sled.X = leader.X + follower * 50f / 80f;
                        sled.PresentationPreviousX = sled.X;
                        sled.LinkedZombieId = leader.Id;
                        sled.BobsledState = new AdventureBobsledState { LeaderId = leader.Id, Position = follower };
                        sled.PresentationAltitude = AdventureBobsledRules.InitialAltitude(follower);
                        sled.ArmorHealth = 0f;
                        sled.PresentationMaxArmor = 0f;
                    }
                }
            }

            if (wave.IsFlag)
            {
                a.FlagsSpawned++;
            }

            a.CurrentWave++;
            a.TotalWavesSpawned++;
            a._warned = false;
            a.Notice(AdventureNoticeKind.WaveSpawned, wave.Index);
            RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.WaveSpawned, wave.Index);
        }

        /// <summary>Performs spawn scheduled final ambush.</summary>
        internal void SpawnScheduledFinalAmbush()
        {
            var a = _adventureModeRunner.Adventure;
            var wave = Math.Max(0, a.Waves.Count - 1);
            // Ice-shroom freezes the native pool/roof ambush; no replacement timer or invented enemy is added.
            var iced = _adventureModeRunner.Adventure.IceTrapTicks > 0;
            if (a.Level.HasPool && !iced)
            {
                var n = a.Level.Number;
                var count = n == 21 || n == 22 || n == 31 || n == 32 ? 2 : 3;
                using var cellsLease = _world.IntegerScratch.Rent();
                var cells = cellsLease.Items;
                for (var c = 5; c < SimulationWorld.Columns; c++)
                {
                    for (var r = 2; r <= 3; r++)
                    {
                        cells.Add(c * 10 + r);
                    }
                }

                for (var i = 0; i < count && cells.Count > 0; i++)
                {
                    int pick = _world._random.Next(cells.Count), cell = cells[pick];
                    cells.RemoveAt(pick);
                    var kind = PickScheduledAmbushZombie(false);
                    var z = _zombieSpawnSystem.SpawnAdventureBoardZombie(kind, cell % 10, cell / 10 + 0.5f, wave);
                    _waterZombieActions.InitializeAdventurePoolZombie(z, cell / 10);
                    a.Notice(AdventureNoticeKind.PoolAmbush, (int)z.Kind, z.Row, cell / 10);
                }
            }
            else if (a.Level.HasRoof && !iced)
            {
                var count = a.Level.Number <= 42 ? 2 : 3;
                var dropWeights = BungeeZombieActions.CreateAdventureBungeeDropWeights();
                for (var index = 0; index < count; index++)
                {
                    var kind = PickScheduledAmbushZombie(false);
                    var cell = _bungeeZombieActions.PickAdventureBungeeDropCell(dropWeights);
                    _bungeeZombieActions.DropAdventureZombie(kind, cell % 10, cell / 10, wave);
                }
            }

            if (a.Level.Terrain == AdventureTerrain.Night && a.Level.Mode != AdventureMode.WhackAZombie)
            {
                foreach (var grave in a._graves)
                {
                    var kind = PickScheduledAmbushZombie(true);
                    var row = _zombieSpawnSystem.ChooseAdventureZombieRow(kind);
                    var z = _zombieSpawnSystem.SpawnAdventureBoardZombie(kind, row, grave.Column + 0.5f, wave);
                    z.Row = grave.Row;
                    _waterZombieActions.InitializeAdventureGraveZombie(z, grave.Column);
                    a.Notice(AdventureNoticeKind.GraveRise, (int)z.Kind, grave.Row, grave.Column);
                }
            }
        }

        /// <summary>Performs pick scheduled ambush zombie.</summary>
        /// <param name = "graves">The graves.</param>
        /// <returns>The resulting pick scheduled ambush zombie.</returns>
        internal ZombieKind PickScheduledAmbushZombie(bool graves)
        {
            var candidates = new List<ZombieDefinition>();
            var total = 0;
            var ids = graves ? new[]
            {
                0,
                2
            }

            : new[]
            {
                0,
                2,
                4
            };
            foreach (var id in ids)
            {
                var d = VerifiedDataCatalog.Zombies[id];
                if (_world.ModeSession == null && !_adventureModeRunner.Adventure.Level.IsReplay && _adventureModeRunner.Adventure.Level.Number < d.BaseLevelGateRaw
                    || id != 0 && !_adventureModeRunner.Adventure.EnabledZombieTypes[id])
                {
                    continue;
                }

                if (d.BaseSelectionWeightRaw <= 0)
                {
                    continue;
                }

                candidates.Add(d);
                total += d.BaseSelectionWeightRaw;
            }

            if (total == 0)
            {
                return ZombieKind.Normal;
            }

            var ticket = _world._random.Next(total);
            foreach (var d in candidates)
            {
                ticket -= d.BaseSelectionWeightRaw;
                if (ticket < 0)
                {
                    return (ZombieKind)d.Id;
                }
            }

            return ZombieKind.Normal;
        }

        /// <summary>Performs raise scheduled wave event.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "index">The index.</param>
        internal void RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind kind, int index)
        {
            if (_world.WaveScheduleEventRaised != null)
            {
                _world.WaveScheduleEventRaised(new global::PlantsVsZombies.Waves.WaveScheduleEvent { Kind = kind, WaveIndex = index, Tick = RequiredWaveSchedule.ElapsedTicks });
            }
        }

        /// <summary>Performs synchronize scheduled wave fields.</summary>
        internal void SynchronizeScheduledWaveFields()
        {
            _adventureModeRunner.Adventure.WaveCountdown = RequiredWaveSchedule.RemainingSeconds;
            _adventureModeRunner.Adventure._waveInitialCountdown = RequiredWaveSchedule.InitialCountdownTicks * 0.01f;
            _adventureModeRunner.Adventure._waveTriggerHealth = RequiredWaveSchedule.HealthToNextWave;
            _adventureModeRunner.Adventure._previousWaveHealth = RequiredWaveSchedule.HealthAtWaveStart;
        }
    }
}
