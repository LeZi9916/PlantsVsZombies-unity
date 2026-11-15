#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns WhackSystem responsibilities for one deterministic match.</summary>
    internal sealed class WhackSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides GraveSystem operations.</summary>
        private GraveSystem _graveSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides WaterZombieActions operations.</summary>
        private WaterZombieActions _waterZombieActions = null!;
        /// <summary>Provides WaveSystem operations.</summary>
        private WaveSystem _waveSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal WhackSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="graveSystem">The GraveSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="waterZombieActions">The WaterZombieActions collaborator.</param>
        /// <param name="waveSystem">The WaveSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, GraveSystem graveSystem, LegacySimulation legacySimulation, WaterZombieActions waterZombieActions, WaveSystem waveSystem, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _graveSystem = graveSystem;
            _legacySimulation = legacySimulation;
            _waterZombieActions = waterZombieActions;
            _waveSystem = waveSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
            _zombieSpawnSystem = zombieSpawnSystem;
        }

        /// <summary>Strikes the center of a zombie's hammer hit rectangle for simulation-only callers.</summary>
        /// <param name="zombieId">The live zombie to strike.</param>
        /// <returns>Whether the hammer strike was accepted.</returns>
        internal bool HammerAdventureZombie(int zombieId)
        {
            var zombie = _world.Zombies.FindById(zombieId);
            if (zombie == null)
            {
                return false;
            }

            var body = AdventureZombieTargetingRules.BodyRectangle(zombie, 50 + zombie.Row * 100);
            return HammerAdventureZombie(zombieId, body.X + body.Width * .5f, body.Y + (body.Height - 50) * .5f + 20f);
        }

        /// <summary>Applies the original helmet damage or immediate lethal hammer strike at a board click.</summary>
        /// <param name="zombieId">The live zombie selected by the hammer hit test.</param>
        /// <param name="hitX">The original board-pixel click x coordinate.</param>
        /// <param name="hitY">The original board-pixel click y coordinate.</param>
        /// <returns>Whether the hammer strike was accepted.</returns>
        internal bool HammerAdventureZombie(int zombieId, float hitX, float hitY)
        {
            var whack = _world.IsAdventure && (_adventureModeRunner.Adventure.Level.Mode == AdventureMode.WhackAZombie || _world.ModeSession != null && _world.ModeSession.Definition.Mechanic == ModeMechanic.Whack);
            if (!whack || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing)
            {
                return false;
            }

            var z = _world.Zombies.FindById(zombieId);
            if (z == null || z.Health <= 0f)
            {
                return false;
            }

            if (z.ArmorHealth > 0)
            {
                _zombieFeedbackSystem.EnsureAdventureZombieEquipment(z);
                _zombieFeedbackSystem.AdventurePresentationZombieHit(z, 900f, true);
                if (z.Kind == ZombieKind.Buckethead)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(46);
                }
                else if (z.Kind == ZombieKind.Conehead)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(58);
                }

                z.ArmorHealth = Math.Max(0, z.ArmorHealth - 900f);
                _zombieFeedbackSystem.AdventurePresentationZombieEquipmentDamaged(z, 0);
                _legacySimulation.Raise(GameEvent.ZombieHit);
            }
            else
            {
                // PC 0x42D616..0x42D65F: Pow at the click, followed by immediate death with loot.
                _world.Presentation.Explosions.Add(new AdventureExplosion
                {
                    Id = ++_world.Presentation._effectId,
                    Seed = _world._random.Next(),
                    Row = z.Row,
                    X = z.X,
                    NativeX = hitX - 3f,
                    NativeY = hitY + 9f,
                    Effect = AdventureExplosionEffect.Pow
                });
                _zombieLifecycle.KillAdventureZombie(z, 16U);
            }

            return true;
        }

        /// <summary>Advances the eight-wave adventure Whack-a-Zombie schedule.</summary>
        /// <param name="dt">The elapsed simulation seconds.</param>
        internal void TickAdventureWhack(float dt)
        {
            var a = _adventureModeRunner.Adventure;
            var ticks = Math.Max(1, (int)Math.Round(dt * 100f));
            if (_world.WaveSchedule != null)
            {
                _world.WaveSchedule.ElapsedTicks += ticks;
                _world.WaveSchedule.InitialCountdownTicks = a.CurrentWave > 0 ? 2000 : 200;
                _world.WaveSchedule.CountdownTicks = Math.Max(0, (int)Math.Round(a.WaveCountdown * 100f));
                _world.WaveSchedule.HealthAtWaveStart = 0;
                _world.WaveSchedule.HealthToNextWave = -1;
            }

            if (a._finalAmbush)
            {
                return;
            }

            var countdown = Math.Max(0, (int)Math.Round(a.WaveCountdown * 100f) - ticks);
            a.WaveCountdown = countdown * .01f;
            if (a.CurrentWave > 0 && countdown == 100 && !a._whackGravesRaised)
            {
                _graveSystem.WhackPlaceGraves(Math.Max(1, 5 - a._graves.Count));
                a._whackGravesRaised = true;
            }

            if (countdown == 0)
            {
                a.CurrentWave++;
                a.WaveCountdown = 20f;
                a._whackGravesRaised = false;
                // PC 0x43139E: the reset counter is decremented on this same native tick.
                a._whackSpawnTimer = a.CurrentWave == a.Waves.Count ? 3f : .03f;
                a.TotalWavesSpawned++;
                a.Notice(AdventureNoticeKind.WaveSpawned, a.CurrentWave - 1);
                _waveSystem.RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.WaveSpawned, a.CurrentWave - 1);
            }
            else if (countdown < 300)
            {
                return;
            }

            var spawnTicks = Math.Max(0, (int)Math.Round(a._whackSpawnTimer * 100f) - ticks);
            a._whackSpawnTimer = spawnTicks * .01f;
            if (spawnTicks > 0)
            {
                return;
            }

            var phase = Math.Max(0, Math.Min(5, (a.CurrentWave - 1) * 6 / 12));
            int[] triple =
            {
                0,
                0,
                0,
                0,
                10,
                13
            }, pair =
            {
                0,
                30,
                10,
                10,
                15,
                18
            }, bucket =
            {
                0,
                0,
                0,
                10,
                15,
                15
            }, cone =
            {
                0,
                0,
                30,
                30,
                30,
                30
            };
            var roll = _world._random.Next(100);
            var typeRoll = _world._random.Next(100);
            var final = a.CurrentWave == a.Waves.Count;
            var count = final ? 20 : roll < triple[phase] ? 3 : roll < triple[phase] + pair[phase] ? 2 : 1;
            var kind = typeRoll < bucket[phase] && count < 3 ? ZombieKind.Buckethead : typeRoll < bucket[phase] + cone[phase] ? ZombieKind.Conehead : ZombieKind.Normal;
            var graves = new List<AdventureCellTimer>();
            foreach (var grave in a._graves)
            {
                if (!_world.Plants.Exists(plant => plant.Row == grave.Row && plant.Column == grave.Column && plant.Kind == PlantKind.GraveBuster))
                {
                    graves.Add(grave);
                }
            }

            var progress = Math.Max(0f, Math.Min(1f, (a.CurrentWave - 1) / 11f));
            // PC 0x4315D8 calls the integer curve helper, then stores pixels per native tick.
            var speedMax = final ? 2f : (float)Math.Round(1f + 2f * progress * progress, MidpointRounding.AwayFromZero);
            for (var i = 0; i < count && graves.Count > 0; i++)
            {
                var index = _world._random.Next(graves.Count);
                var grave = graves[index];
                graves.RemoveAt(index);
                var zombieKind = final ? (_world._random.Next(2) == 0 ? ZombieKind.Conehead : ZombieKind.Buckethead) : kind;
                var row = _zombieSpawnSystem.ChooseAdventureZombieRow(zombieKind);
                var zombie = _zombieSpawnSystem.SpawnAdventureBoardZombie(zombieKind, row, grave.Column + 0.5f, a.CurrentWave - 1);
                zombie.Row = grave.Row;
                zombie.PresentationNativeVelocity = 0.5f + (float)_world._random.NextDouble() * (speedMax - 0.5f);
                zombie.Speed = AdventureZombieNativeRules.GridSpeed(zombie.PresentationNativeVelocity);
                zombie.PresentationWhackSpeed = true;
                zombie.DropRemaining = 0.5f;
                _waterZombieActions.InitializeAdventureGraveZombie(zombie, grave.Column);
            }

            var minTicks = (int)Math.Round(100f - 70f * progress, MidpointRounding.AwayFromZero);
            var maxTicks = (int)Math.Round(200f - 140f * progress, MidpointRounding.AwayFromZero);
            a._whackSpawnTimer = _world._random.Next(minTicks, maxTicks + 1) * 0.01f;
            if (final)
            {
                a.WaveCountdown = 0;
                a._finalAmbush = true;
            }

            if (_world.WaveSchedule != null)
            {
                _world.WaveSchedule.InitialCountdownTicks = a.CurrentWave > 0 ? 2000 : 200;
                _world.WaveSchedule.CountdownTicks = Math.Max(0, (int)Math.Round(a.WaveCountdown * 100f));
                _world.WaveSchedule.IsFinalWave = final;
                _world.WaveSchedule.AmbushComplete = a._finalAmbush;
            }
        }

        // PC 0x4312E0: twelve 2000-tick rounds, native chance arrays and 100..200 -> 30..60 spawn interval.
        /// <summary>Performs tick mode whack.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModeWhack(float dt)
        {
            var a = _adventureModeRunner.Adventure;
            var s = _world.ModeSession;
            if (a._finalAmbush)
            {
                return;
            }

            a.WaveCountdown -= dt;
            if (s == null)
            {
                throw new System.InvalidOperationException("The value 's' is not available.");
            }

            if (a.CurrentWave > 0 && a.WaveCountdown <= 1f && !s._whackGravesRaised)
            {
                ModeWhackAddGraves(Math.Max(1, 5 - a._graves.Count));
                s._whackGravesRaised = true;
            }

            if (a.WaveCountdown <= 0)
            {
                a.CurrentWave++;
                a.WaveCountdown = 20f;
                s._whackGravesRaised = false;
                s._whackSpawnTimer = a.CurrentWave == 12 ? 1f : 0.01f;
            }

            if (a.CurrentWave <= 0 || a.WaveCountdown < 3f)
            {
                return;
            }

            s._whackSpawnTimer -= dt;
            if (s._whackSpawnTimer > 0)
            {
                return;
            }

            var chance = Math.Max(0, Math.Min(5, (a.CurrentWave - 1) * 6 / 12));
            int[] triple =
            {
                0,
                0,
                0,
                0,
                10,
                13
            }, pair =
            {
                0,
                30,
                10,
                10,
                15,
                18
            }, bucket =
            {
                0,
                0,
                0,
                10,
                15,
                15
            }, cone =
            {
                0,
                0,
                30,
                30,
                30,
                30
            };
            int roll = _world._random.Next(100), typeRoll = _world._random.Next(100);
            var final = a.CurrentWave == 12;
            var count = final ? 20 : roll < triple[chance] ? 3 : roll < triple[chance] + pair[chance] ? 2 : 1;
            var kind = typeRoll < bucket[chance] && count < 3 ? ZombieKind.Buckethead : typeRoll < bucket[chance] + cone[chance] ? ZombieKind.Conehead : ZombieKind.Normal;
            var graves = new List<AdventureCellTimer>();
            foreach (var g in a._graves)
            {
                if (!_world.Plants.Exists(p => p.Row == g.Row && p.Column == g.Column && p.Kind == PlantKind.GraveBuster))
                {
                    graves.Add(g);
                }
            }

            var t = Math.Max(0, Math.Min(1, (a.CurrentWave - 1) / 11f));
            var speedMax = final ? 2f : 1f + 2f * t * t;
            for (var i = 0; i < count && graves.Count > 0; i++)
            {
                var index = _world._random.Next(graves.Count);
                var g = graves[index];
                graves.RemoveAt(index);
                var zombieKind = final ? (_world._random.Next(2) == 0 ? ZombieKind.Conehead : ZombieKind.Buckethead) : kind;
                var row = _zombieSpawnSystem.ChooseAdventureZombieRow(zombieKind);
                var z = _zombieSpawnSystem.SpawnAdventureBoardZombie(zombieKind, row, g.Column + 0.5f, a.CurrentWave - 1);
                z.Row = g.Row;
                z.Speed = 0.5f + (float)_world._random.NextDouble() * (speedMax - 0.5f);
                z.PresentationNativeVelocity = z.Speed * .8f;
                z.PresentationWhackSpeed = true;
                z.DropRemaining = 0.5f;
                _waterZombieActions.InitializeAdventureGraveZombie(z, g.Column);
            }

            var minTicks = (int)Math.Round(100f - 70f * t);
            var maxTicks = (int)Math.Round(200f - 140f * t);
            s._whackSpawnTimer = _world._random.Next(minTicks, maxTicks + 1) * 0.01f;
            if (final)
            {
                a.WaveCountdown = 0;
                a._finalAmbush = true;
            }
        }

        /// <summary>Performs mode whack add graves.</summary>
        /// <param name = "count">The count.</param>
        internal void ModeWhackAddGraves(int count)
        {
            _graveSystem.WhackPlaceGraves(count);
        }
    }
}
