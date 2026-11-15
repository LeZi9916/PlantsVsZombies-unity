#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns LegacySimulation responsibilities for one deterministic match.</summary>
    internal sealed class LegacySimulation
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Advances shared seed cooldowns.</summary>
        private SeedBankSystem _seedBankSystem = null!;
        /// <summary>Reuses the native tick admission callback.</summary>
        private readonly Func<bool> _canAdvance;
        /// <summary>Reuses the native tick callback.</summary>
        private readonly Action<float> _advance;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardEnvironment operations.</summary>
        private BoardEnvironment _boardEnvironment = null!;
        /// <summary>Provides LegacyCombatSystem operations.</summary>
        private LegacyCombatSystem _legacyCombatSystem = null!;
        /// <summary>Provides LegacyPlantActions operations.</summary>
        private LegacyPlantActions _legacyPlantActions = null!;
        /// <summary>Provides LevelOneModeRunner operations.</summary>
        private LevelOneModeRunner _levelOneModeRunner = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides RewardSystem operations.</summary>
        private RewardSystem _rewardSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal LegacySimulation(SimulationWorld world)
        {
            _world = world;
            _canAdvance = () => _world.State == MatchState.Playing;
            _advance = Advance;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardEnvironment">The BoardEnvironment collaborator.</param>
        /// <param name="legacyCombatSystem">The LegacyCombatSystem collaborator.</param>
        /// <param name="legacyPlantActions">The LegacyPlantActions collaborator.</param>
        /// <param name="levelOneModeRunner">The LevelOneModeRunner collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="rewardSystem">The RewardSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        internal void Connect(SeedBankSystem seedBankSystem, AdventureModeRunner adventureModeRunner, BoardEnvironment boardEnvironment, LegacyCombatSystem legacyCombatSystem, LegacyPlantActions legacyPlantActions, LevelOneModeRunner levelOneModeRunner, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, RewardSystem rewardSystem, SunSystem sunSystem)
        {
            _seedBankSystem = seedBankSystem;
            _adventureModeRunner = adventureModeRunner;
            _boardEnvironment = boardEnvironment;
            _legacyCombatSystem = legacyCombatSystem;
            _legacyPlantActions = legacyPlantActions;
            _levelOneModeRunner = levelOneModeRunner;
            _plantingSystem = plantingSystem;
            _projectileSystem = projectileSystem;
            _rewardSystem = rewardSystem;
            _sunSystem = sunSystem;
        }

        /// <summary>Performs cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting cost.</returns>
        internal static int Cost(PlantKind kind)
        {
            return DefinitionFor(kind).SunCost;
        }

        /// <summary>Performs recharge time.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting recharge time.</returns>
        internal static float RechargeTime(PlantKind kind)
        {
            return (float)((DefinitionFor(kind).RechargeThresholdRaw + 1) * SimulationWorld.SimulationStep);
        }

        /// <summary>Performs definition for.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting definition for.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        internal static PlantDefinition DefinitionFor(PlantKind kind)
        {
            PlantDefinition? definition;
            if ((int)kind < (int)PlantKind.Peashooter || (int)kind > (int)PlantKind.ExplodeONut || !VerifiedDataCatalog.TryGetPlant((int)kind, out definition))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return definition;
        }

        /// <summary>Performs cooldown remaining.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting cooldown remaining.</returns>
        internal float CooldownRemaining(PlantKind kind)
        {
            DefinitionFor(kind);
            return (float)(_world._seedCooldownTicks[(int)kind] * SimulationWorld.SimulationStep);
        }

        /// <summary>Performs plant at.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting plant at.</returns>
        internal Plant PlantAt(int row, int column)
        {
            for (var i = _world.Plants.Count - 1; i >= 0; i--)
            {
                var plant = _world.Plants[i];
                if (plant.Row == row && plant.Column <= column && column < plant.Column + plant.ColumnSpan && plant.BungeeState != 2)
                {
                    return plant;
                }
            }

            return null!;
        }

        /// <summary>Performs try plant.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try plant.</returns>
        internal bool TryPlant(PlantKind kind, int row, int column)
        {
            if (_world.IsAdventure)
            {
                bool handled = false, planted = false;
                _plantingSystem.PlantAdventure(kind, row, column, ref handled, ref planted);
                return handled && planted;
            }

            // Expanded IDs belong to the opt-in Adventure module. The legacy demo still implements its original nine plants.
            if (kind != PlantKind.Peashooter && kind != PlantKind.Sunflower && kind != PlantKind.CherryBomb && kind != PlantKind.Wallnut && kind != PlantKind.PotatoMine && kind != PlantKind.SnowPea && kind != PlantKind.Repeater && kind != PlantKind.Jalapeno && kind != PlantKind.TallNut)
            {
                return false;
            }

            var index = (int)kind;
            if (index < 0 || index >= _world._seedCooldownTicks.Length || _world.State != MatchState.Playing || row < 0 || row >= SimulationWorld.Rows || column < 0 || column >= SimulationWorld.Columns || _world.SunAmount < Cost(kind) || _world._seedCooldownTicks[index] > 0 || PlantAt(row, column) != null)
            {
                return false;
            }

            if (_world.IsExperimentalLevelOne)
            {
                var allowed = false;
                _levelOneModeRunner.CanPlantExperimentalLevelOne(kind, row, column, ref allowed);
                if (!allowed)
                {
                    return false;
                }
            }

            _world.SunAmount -= Cost(kind);
            _world._seedCooldownTicks[index] = DefinitionFor(kind).RechargeThresholdRaw + 1;
            var plant = new Plant
            {
                Kind = kind,
                Row = row,
                Column = column,
                Health = kind == PlantKind.Wallnut ? 4000f : kind == PlantKind.TallNut ? 8000f : 300f,
                ActionTimer = kind == PlantKind.CherryBomb ? 1f : kind == PlantKind.PotatoMine ? 15f : kind == PlantKind.Jalapeno ? 1f : 0.5f
            };
            if (LegacyPlantActions.HasOriginalTiming(kind))
            {
                var timingKind = (global::PlantsVsZombies.Timing.PlantKind)(int)kind;
                plant.Timing = global::PlantsVsZombies.Timing.PlantTiming.Initialize(timingKind, _world._random.Next(global::PlantsVsZombies.Timing.PlantTiming.InitialRandomBound(timingKind)));
            }

            _world.Plants.Add(plant);
            if (_world.IsExperimentalLevelOne)
            {
                _levelOneModeRunner.OnExperimentalLevelOnePlantPlaced();
            }

            Raise(GameEvent.Planted);
            return true;
        }

        /// <summary>Performs remove plant.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting remove plant.</returns>
        internal bool RemovePlant(int row, int column)
        {
            if (_world.State != MatchState.Playing)
            {
                return false;
            }

            if (_world.IsAdventure)
            {
                bool handled = false, removed = false;
                _plantingSystem.RemoveAdventurePlant(row, column, ref handled, ref removed);
                return handled && removed;
            }

            if (_world.IsExperimentalLevelOne)
            {
                var allowed = false;
                _levelOneModeRunner.CanRemoveExperimentalLevelOnePlant(ref allowed);
                if (!allowed)
                {
                    return false;
                }
            }

            var plant = PlantAt(row, column);
            return plant != null && _world.Plants.Remove(plant);
        }

        /// <summary>Performs toggle pause.</summary>
        internal void TogglePause()
        {
            if (_world.State == MatchState.Playing)
            {
                _world.State = MatchState.Paused;
            }
            else if (_world.State == MatchState.Paused)
            {
                _world.State = MatchState.Playing;
            }
        }

        /// <summary>Performs update.</summary>
        /// <param name = "deltaTime">The delta time.</param>
        internal void Update(float deltaTime)
        {
            if (_world.IsAdventure && (_world.State == MatchState.Paused || _world.State == MatchState.Lost))
            {
                _boardEnvironment.AdvanceAdventureStormPresentation(deltaTime, _world.State == MatchState.Paused);
                return;
            }

            if (_world.IsAdventure && _world.State == MatchState.Won)
            {
                _rewardSystem.AdvanceAdventureRewardPresentation(deltaTime);
                return;
            }

            if (_world.State != MatchState.Playing || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            FixedStepClock.Advance(ref _world._accumulatedTime, deltaTime, _canAdvance, _advance);
        }

        /// <summary>Performs advance.</summary>
        /// <param name = "dt">The dt.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void Advance(float dt)
        {
            if (_world.IsAdventure)
            {
                var handled = false;
                _adventureModeRunner.AdvanceAdventure(dt, ref handled);
                if (!handled)
                {
                    throw new InvalidOperationException("Adventure tick handler is missing.");
                }

                return;
            }

            _world.Elapsed += dt;
            _world._nativeFeedbackTick++;
            _seedBankSystem.AdvanceCooldowns(false);

            if (_world.IsExperimentalLevelOne)
            {
                _levelOneModeRunner.BeforeExperimentalLevelOneTick();
            }

            var nativeSkySunHandled = false;
            if (_world.IsExperimentalLevelOne)
            {
                _levelOneModeRunner.AdvanceExperimentalLevelOneNativeSkySun(ref nativeSkySunHandled);
            }

            if (nativeSkySunHandled && _world.State != MatchState.Playing)
            {
                return;
            }

            if (!nativeSkySunHandled)
            {
                var skySunAllowed = true;
                if (_world.IsExperimentalLevelOne)
                {
                    _levelOneModeRunner.CanDropExperimentalLevelOneSkySun(ref skySunAllowed);
                }

                if (skySunAllowed)
                {
                    _world._nextSkySun -= dt;
                    if (_world._nextSkySun <= 0f && _world.Rules.SkySunInterval > 0f)
                    {
                        var handled = false;
                        if (_world.IsExperimentalLevelOne)
                        {
                            _levelOneModeRunner.SpawnExperimentalLevelOneSkySun(ref handled);
                        }

                        if (!handled)
                        {
                            _world.Suns.Add(_sunSystem.CreateSun(0.4f + (float)_world._random.NextDouble() * 8.2f, -1f, 0.5f + (float)_world._random.NextDouble() * 3.8f));
                        }

                        _world._nextSkySun += _world.Rules.SkySunInterval;
                    }
                }
            }

            if (_world.IsExperimentalLevelOne)
            {
                _levelOneModeRunner.AdvanceExperimentalLevelOneWaves();
            }
            else
            {
                _world._nextZombie -= dt;
                if (_world._nextZombie <= 0f && _world.SpawnedZombies < _world.Rules.ZombieCount)
                {
                    _legacyCombatSystem.SpawnZombie();
                    _world._nextZombie += Math.Max(3.5f, _world.Rules.ZombieInterval - _world.SpawnedZombies * 0.18f);
                }
            }

            _sunSystem.UpdateSuns(dt);
            _legacyPlantActions.UpdatePlants(dt);
            _legacyCombatSystem.UpdatePeas(dt);
            _legacyCombatSystem.UpdateZombies(dt);
            _legacyCombatSystem.UpdateMowers(dt);
            for (var i = _world.Flames.Count - 1; i >= 0; i--)
            {
                _world.Flames[i].Age += dt;
                if (_world.Flames[i].Age >= 1f)
                {
                    _projectileSystem.RemoveFlameAt(i);
                }
            }

            if (_world.State == MatchState.Playing && _world.IsExperimentalLevelOne)
            {
                _levelOneModeRunner.CheckExperimentalLevelOneCompletion();
            }
            else if (_world.State == MatchState.Playing && _world.Rules.ZombieCount > 0 && _world.SpawnedZombies >= _world.Rules.ZombieCount && _world.Zombies.Count == 0)
            {
                _world.State = MatchState.Won;
                Raise(GameEvent.Won);
            }
        }

        /// <summary>Checks an upgraded plant's existing area-target rules without a captured delegate.</summary>
        /// <param name="plant">The upgraded plant testing its attack range.</param>
        /// <returns>True when an eligible zombie is in range.</returns>
        internal bool HasUpgradeAreaTarget(Plant plant)
        {
            foreach (var zombie in _world.Zombies)
            {
                if (UpgradePlantActions.IsUpgradeAreaTarget(zombie, plant))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Checks whether an umbrella protects a cell.</summary>
        /// <param name="row">The protected lane.</param>
        /// <param name="column">The protected column.</param>
        /// <returns>True when an umbrella is within one row and column.</returns>
        internal bool HasUmbrellaNear(int row, int column)
        {
            foreach (var plant in _world.Plants)
            {
                if (plant.Kind == PlantKind.UmbrellaLeaf && plant.Health > 0f && plant.BungeeState != 2
                    && Math.Abs(plant.Row - row) <= 1 && Math.Abs(plant.Column - column) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Checks whether a timer list contains a specific board cell.</summary>
        /// <param name="timers">The timers to inspect.</param>
        /// <param name="row">The requested lane.</param>
        /// <param name="column">The requested column.</param>
        /// <returns>True when the cell is already represented.</returns>
        internal static bool HasCellTimer(List<AdventureCellTimer> timers, int row, int column)
        {
            foreach (var timer in timers)
            {
                if (timer.Row == row && timer.Column == column)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs raise.</summary>
        /// <param name = "gameEvent">The game event.</param>
        internal void Raise(GameEvent gameEvent)
        {
            if (_world.EventRaised != null)
            {
                _world.EventRaised(gameEvent);
            }
        }
    }
}
