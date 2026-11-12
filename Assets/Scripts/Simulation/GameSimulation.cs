#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Levels;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Randomness;

using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;

using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Advances deterministic board state and applies the rules shared by game modes.</summary>
    public sealed class GameSimulation
    {
        /// <summary>Stores rows.</summary>
        public const int Rows = 5;
        /// <summary>Stores columns.</summary>
        public const int Columns = 9;
        /// <summary>Stores plants.</summary>
        public readonly List<Plant> Plants = new List<Plant>();
        /// <summary>Stores zombies.</summary>
        public readonly List<Zombie> Zombies = new List<Zombie>();
        /// <summary>Stores peas.</summary>
        public readonly List<Pea> Peas = new List<Pea>(64);
        /// <summary>Stores suns.</summary>
        public readonly List<global::PlantsVsZombies.Simulation.Sun> Suns = new List<global::PlantsVsZombies.Simulation.Sun>(16);
        /// <summary>Stores mowers.</summary>
        public readonly List<Mower> Mowers = new List<Mower>();
        /// <summary>Stores flames.</summary>
        public readonly List<LaneFlame> Flames = new List<LaneFlame>(5);
        /// <summary>Retains reusable projectiles and their piercing-hit buffers for this match.</summary>
        private readonly SimulationObjectPool<Pea> _peaPool = new SimulationObjectPool<Pea>(64, 256);
        /// <summary>Retains reusable normal and native-motion suns for this match.</summary>
        private readonly SimulationObjectPool<global::PlantsVsZombies.Simulation.Sun> _sunPool = new SimulationObjectPool<global::PlantsVsZombies.Simulation.Sun>(16, 128);
        /// <summary>Retains short-lived lane flames for this match.</summary>
        private readonly SimulationObjectPool<LaneFlame> _flamePool = new SimulationObjectPool<LaneFlame>(5, 16);
        /// <summary>Stores rules.</summary>
        public readonly GameRules Rules;
        /// <summary>Gets or sets state.</summary>
        public MatchState State { get; private set; }
        /// <summary>Gets or sets sun amount.</summary>
        public int SunAmount { get; private set; }
        /// <summary>Gets or sets spawned zombies.</summary>
        public int SpawnedZombies { get; private set; }
        /// <summary>Gets or sets elapsed.</summary>
        public float Elapsed { get; private set; }
        /// <summary>The mode selected at construction; changing the rules later does not switch modes.</summary>
        public bool IsExperimentalLevelOne { get; private set; }
        /// <summary>Gets or sets is adventure.</summary>
        public bool IsAdventure { get; private set; }
        /// <summary>Gets or sets row count.</summary>
        public int RowCount { get; private set; } = Rows;

        /// <summary>Stores event raised.</summary>
        public Action<GameEvent>? EventRaised;
        /// <summary>Reuses the synchronous native reward animation callback.</summary>
        private readonly Action _rewardPresentationTick;
        /// <summary>Accumulates decorative board ticks after independent modes finish without an adventure reward object.</summary>
        private double _completedBoardAccumulated;
        /// <summary>Reuses the immediately consumed packet-ready feedback context.</summary>
        private readonly AdventureSeedBankContext _packetFeedbackContext = new AdventureSeedBankContext();
        /// <summary>Stores random.</summary>
        private readonly Random _random;
        /// <summary>Samples original decorative grave choices without changing the combat random stream.</summary>
        private readonly OriginalRandomAdapter _gravePresentationRandom;
        /// <summary>Stores seed cooldown ticks.</summary>
        private readonly int[] _seedCooldownTicks = new int[VerifiedDataCatalog.Plants.Count];
        /// <summary>Stores next zombie.</summary>
        private float _nextZombie;
        /// <summary>Stores next sky sun.</summary>
        private float _nextSkySun;
        /// <summary>Stores accumulated time.</summary>
        private double _accumulatedTime;
        /// <summary>Stores simulation step.</summary>
        private const double SimulationStep = 0.01;
        /// <summary>Initializes game simulation.</summary>
        /// <param name = "rules">The rules.</param>
        /// <param name = "randomSeed">The random seed.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public GameSimulation(GameRules? rules = null, int randomSeed = 12345)
        {
            _rewardPresentationTick = AdvanceAdventureRewardTick;
            Rules = rules ?? new GameRules();
            IsExperimentalLevelOne = Rules.EnableExperimentalLevelOne;
            IsAdventure = Rules.AdventureLevel != 0;
            if (IsAdventure && IsExperimentalLevelOne)
            {
                throw new ArgumentException("Adventure and the legacy 1-1 experiment are separate modes.");
            }

            _random = new global::PlantsVsZombies.Randomness.OriginalRandomAdapter(unchecked((uint)randomSeed));
            _gravePresentationRandom = new OriginalRandomAdapter(unchecked((uint)randomSeed));
            State = MatchState.Playing;
            SunAmount = Rules.StartingSun;
            _nextZombie = Rules.FirstZombieDelay;
            _nextSkySun = Rules.SkySunInterval;
            for (var row = 0; row < Rows; row++)
            {
                Mowers.Add(new Mower { Row = row, X = -0.5f, Available = Rules.MowersEnabled });
            }

            if (IsAdventure)
            {
                var initialized = false;
                InitializeAdventure(ref initialized);
                if (!initialized)
                {
                    throw new InvalidOperationException("Adventure requires the Adventure module.");
                }
            }

            if (IsExperimentalLevelOne)
            {
                var initialized = false;
                InitializeExperimentalLevelOne(ref initialized);
                if (!initialized)
                {
                    throw new InvalidOperationException("Experimental level 1-1 requires the Levels module.");
                }
            }
        }

        /// <summary>Performs cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting cost.</returns>
        public static int Cost(PlantKind kind)
        {
            return DefinitionFor(kind).SunCost;
        }

        /// <summary>Performs recharge time.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting recharge time.</returns>
        public static float RechargeTime(PlantKind kind)
        {
            return (float)((DefinitionFor(kind).RechargeThresholdRaw + 1) * SimulationStep);
        }

        /// <summary>Performs definition for.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting definition for.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        private static PlantDefinition DefinitionFor(PlantKind kind)
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
        public float CooldownRemaining(PlantKind kind)
        {
            DefinitionFor(kind);
            return (float)(_seedCooldownTicks[(int)kind] * SimulationStep);
        }

        /// <summary>Performs plant at.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting plant at.</returns>
        public Plant PlantAt(int row, int column)
        {
            for (var i = Plants.Count - 1; i >= 0; i--)
            {
                var plant = Plants[i];
                if (plant.Row == row && plant.Column <= column && column < plant.Column + plant.ColumnSpan)
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
        public bool TryPlant(PlantKind kind, int row, int column)
        {
            if (IsAdventure)
            {
                bool handled = false, planted = false;
                PlantAdventure(kind, row, column, ref handled, ref planted);
                return handled && planted;
            }

            // Expanded IDs belong to the opt-in Adventure module. The legacy demo still implements its original nine plants.
            if (kind != PlantKind.Peashooter && kind != PlantKind.Sunflower && kind != PlantKind.CherryBomb && kind != PlantKind.Wallnut && kind != PlantKind.PotatoMine && kind != PlantKind.SnowPea && kind != PlantKind.Repeater && kind != PlantKind.Jalapeno && kind != PlantKind.TallNut)
            {
                return false;
            }

            var index = (int)kind;
            if (index < 0 || index >= _seedCooldownTicks.Length || State != MatchState.Playing || row < 0 || row >= Rows || column < 0 || column >= Columns || SunAmount < Cost(kind) || _seedCooldownTicks[index] > 0 || PlantAt(row, column) != null)
            {
                return false;
            }

            if (IsExperimentalLevelOne)
            {
                var allowed = false;
                CanPlantExperimentalLevelOne(kind, row, column, ref allowed);
                if (!allowed)
                {
                    return false;
                }
            }

            SunAmount -= Cost(kind);
            _seedCooldownTicks[index] = DefinitionFor(kind).RechargeThresholdRaw + 1;
            var plant = new Plant
            {
                Kind = kind,
                Row = row,
                Column = column,
                Health = kind == PlantKind.Wallnut ? 4000f : kind == PlantKind.TallNut ? 8000f : 300f,
                ActionTimer = kind == PlantKind.CherryBomb ? 1f : kind == PlantKind.PotatoMine ? 15f : kind == PlantKind.Jalapeno ? 1f : 0.5f
            };
            if (HasOriginalTiming(kind))
            {
                var timingKind = (global::PlantsVsZombies.Timing.PlantKind)(int)kind;
                plant.Timing = global::PlantsVsZombies.Timing.PlantTiming.Initialize(timingKind, _random.Next(global::PlantsVsZombies.Timing.PlantTiming.InitialRandomBound(timingKind)));
            }

            Plants.Add(plant);
            if (IsExperimentalLevelOne)
            {
                OnExperimentalLevelOnePlantPlaced();
            }

            Raise(GameEvent.Planted);
            return true;
        }

        /// <summary>Performs remove plant.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting remove plant.</returns>
        public bool RemovePlant(int row, int column)
        {
            if (State != MatchState.Playing)
            {
                return false;
            }

            if (IsAdventure)
            {
                bool handled = false, removed = false;
                RemoveAdventurePlant(row, column, ref handled, ref removed);
                return handled && removed;
            }

            if (IsExperimentalLevelOne)
            {
                var allowed = false;
                CanRemoveExperimentalLevelOnePlant(ref allowed);
                if (!allowed)
                {
                    return false;
                }
            }

            var plant = PlantAt(row, column);
            return plant != null && Plants.Remove(plant);
        }

        /// <summary>Performs collect sun.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting collect sun.</returns>
        public bool CollectSun(global::PlantsVsZombies.Simulation.Sun sun)
        {
            bool handled = false, collected = false;
            if (IsAdventure)
            {
                CollectAdventureSunMotion(sun, ref handled, ref collected);
            }

            if (handled)
            {
                return collected;
            }

            if (State != MatchState.Playing || !Suns.Remove(sun))
            {
                return false;
            }

            Raise(GameEvent.SunPickupStarted);
            SunAmount = IsAdventure ? Math.Min(9990, SunAmount + sun.Value) : SunAmount + sun.Value;
            if (IsAdventure)
            {
                CollectAdventureSun();
            }

            if (IsExperimentalLevelOne)
            {
                OnExperimentalLevelOneSunCollected();
            }

            Raise(GameEvent.SunCollected);
            _sunPool.Return(sun);
            return true;
        }

        /// <summary>Performs toggle pause.</summary>
        public void TogglePause()
        {
            if (State == MatchState.Playing)
            {
                State = MatchState.Paused;
            }
            else if (State == MatchState.Paused)
            {
                State = MatchState.Playing;
            }
        }

        /// <summary>Performs update.</summary>
        /// <param name = "deltaTime">The delta time.</param>
        public void Update(float deltaTime)
        {
            if (IsAdventure && State == MatchState.Won)
            {
                AdvanceAdventureRewardPresentation(deltaTime);
                return;
            }

            if (State != MatchState.Playing || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            _accumulatedTime += deltaTime;
            while (_accumulatedTime + 0.0000001 >= SimulationStep && State == MatchState.Playing)
            {
                Advance((float)SimulationStep);
                _accumulatedTime -= SimulationStep;
                if (Math.Abs(_accumulatedTime) < 0.0000001)
                {
                    _accumulatedTime = 0d;
                }
            }
        }

        /// <summary>Performs advance.</summary>
        /// <param name = "dt">The dt.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private void Advance(float dt)
        {
            if (IsAdventure)
            {
                var handled = false;
                AdvanceAdventure(dt, ref handled);
                if (!handled)
                {
                    throw new InvalidOperationException("Adventure tick handler is missing.");
                }

                return;
            }

            Elapsed += dt;
            for (var i = 0; i < _seedCooldownTicks.Length; i++)
            {
                if (_seedCooldownTicks[i] > 0)
                {
                    _seedCooldownTicks[i]--;
                }
            }

            if (IsExperimentalLevelOne)
            {
                BeforeExperimentalLevelOneTick();
            }

            var nativeSkySunHandled = false;
            if (IsExperimentalLevelOne)
            {
                AdvanceExperimentalLevelOneNativeSkySun(ref nativeSkySunHandled);
            }

            if (nativeSkySunHandled && State != MatchState.Playing)
            {
                return;
            }

            if (!nativeSkySunHandled)
            {
                var skySunAllowed = true;
                if (IsExperimentalLevelOne)
                {
                    CanDropExperimentalLevelOneSkySun(ref skySunAllowed);
                }

                if (skySunAllowed)
                {
                    _nextSkySun -= dt;
                    if (_nextSkySun <= 0f && Rules.SkySunInterval > 0f)
                    {
                        var handled = false;
                        if (IsExperimentalLevelOne)
                        {
                            SpawnExperimentalLevelOneSkySun(ref handled);
                        }

                        if (!handled)
                        {
                            Suns.Add(CreateSun(0.4f + (float)_random.NextDouble() * 8.2f, -1f, 0.5f + (float)_random.NextDouble() * 3.8f));
                        }

                        _nextSkySun += Rules.SkySunInterval;
                    }
                }
            }

            if (IsExperimentalLevelOne)
            {
                AdvanceExperimentalLevelOneWaves();
            }
            else
            {
                _nextZombie -= dt;
                if (_nextZombie <= 0f && SpawnedZombies < Rules.ZombieCount)
                {
                    SpawnZombie();
                    _nextZombie += Math.Max(3.5f, Rules.ZombieInterval - SpawnedZombies * 0.18f);
                }
            }

            UpdateSuns(dt);
            UpdatePlants(dt);
            UpdatePeas(dt);
            UpdateZombies(dt);
            UpdateMowers(dt);
            for (var i = Flames.Count - 1; i >= 0; i--)
            {
                Flames[i].Age += dt;
                if (Flames[i].Age >= 1f)
                {
                    RemoveFlameAt(i);
                }
            }

            if (State == MatchState.Playing && IsExperimentalLevelOne)
            {
                CheckExperimentalLevelOneCompletion();
            }
            else if (State == MatchState.Playing && Rules.ZombieCount > 0 && SpawnedZombies >= Rules.ZombieCount && Zombies.Count == 0)
            {
                State = MatchState.Won;
                Raise(GameEvent.Won);
            }
        }

        /// <summary>Performs spawn zombie.</summary>
        private void SpawnZombie()
        {
            var number = ++SpawnedZombies;
            var kind = number == Rules.ZombieCount ? ZombieKind.Flag : number >= 10 && number % 5 == 0 ? ZombieKind.ScreenDoor : number >= 8 && number % 4 == 0 ? ZombieKind.Buckethead : number >= 4 && number % 3 == 0 ? ZombieKind.Conehead : ZombieKind.Normal;
            var armor = kind == ZombieKind.Buckethead ? 1100f : kind == ZombieKind.Conehead ? 370f : 0f;
            var shield = kind == ZombieKind.ScreenDoor ? 1100f : 0f;
            Zombies.Add(new Zombie {
                Kind = kind,
                Row = _random.Next(Rows),
                X = 9.5f,
                Health = 270f,
                ArmorHealth = armor,
                ShieldHealth = shield
            });
        }

        /// <summary>Takes a projectile with clean gameplay and presentation state.</summary>
        /// <returns>A reusable projectile with the same defaults as a newly constructed entry.</returns>
        private Pea RentPea()
        {
            var pea = _peaPool.Rent();
            pea.Reset();
            return pea;
        }

        /// <summary>Takes a sun with clean pickup, motion, and lifetime state.</summary>
        /// <param name="x">The initial board x coordinate.</param>
        /// <param name="y">The initial board y coordinate.</param>
        /// <param name="targetY">The landing y coordinate.</param>
        /// <param name="value">The credited sunlight value.</param>
        /// <returns>A reusable sun initialized for its next appearance.</returns>
        private global::PlantsVsZombies.Simulation.Sun CreateSun(float x, float y, float targetY, int value = 25)
        {
            var sun = _sunPool.Rent();
            sun.Reset();
            sun.X = x;
            sun.Y = y;
            sun.TargetY = targetY;
            sun.Value = value;
            return sun;
        }

        /// <summary>Retires a projectile after detaching its active snow trail.</summary>
        /// <param name="index">The index of the active projectile to retire.</param>
        private void RemovePeaAt(int index)
        {
            var pea = Peas[index];
            if (IsAdventure)
            {
                StopAdventureSnowTrail(pea);
            }

            Peas.RemoveAt(index);
            pea.PresentationSnowTrail = null!;
            _peaPool.Return(pea);
        }

        /// <summary>Returns all active projectiles before a mode or stage clears its board.</summary>
        private void ClearPeas()
        {
            for (var i = Peas.Count - 1; i >= 0; i--)
            {
                RemovePeaAt(i);
            }
        }

        /// <summary>Retires a sun after its collection or disappearance finishes.</summary>
        /// <param name="index">The index of the active sun to retire.</param>
        private void RemoveSunAt(int index)
        {
            var sun = Suns[index];
            Suns.RemoveAt(index);
            _sunPool.Return(sun);
        }

        /// <summary>Returns all active suns before a mode or stage clears its board.</summary>
        private void ClearSuns()
        {
            for (var i = Suns.Count - 1; i >= 0; i--)
            {
                RemoveSunAt(i);
            }
        }

        /// <summary>Retires an expired lane flame.</summary>
        /// <param name="index">The index of the active flame to retire.</param>
        private void RemoveFlameAt(int index)
        {
            var flame = Flames[index];
            Flames.RemoveAt(index);
            _flamePool.Return(flame);
        }

        /// <summary>Returns all active flames before a mode or stage clears its board.</summary>
        private void ClearFlames()
        {
            for (var i = Flames.Count - 1; i >= 0; i--)
            {
                RemoveFlameAt(i);
            }
        }

        /// <summary>Performs update suns.</summary>
        /// <param name = "dt">The dt.</param>
        private void UpdateSuns(float dt)
        {
            var handled = false;
            if (IsAdventure)
            {
                AdvanceAdventureSunMotion(dt, ref handled);
            }

            if (handled)
            {
                return;
            }

            for (var i = Suns.Count - 1; i >= 0; i--)
            {
                var sun = Suns[i];
                sun.Age += dt;
                sun.Y = Math.Min(sun.TargetY, sun.Y + dt * 0.75f);
                if (sun.Age > 14f)
                {
                    RemoveSunAt(i);
                }
            }
        }

        /// <summary>Performs update plants.</summary>
        /// <param name = "dt">The dt.</param>
        private void UpdatePlants(float dt)
        {
            for (var i = Plants.Count - 1; i >= 0; i--)
            {
                var plant = Plants[i];
                plant.Age += dt;
                plant.PhaseAge += dt;
                if (plant.ShootAge >= 0f)
                {
                    plant.ShootAge += dt;
                }

                if (plant.Timing.HasValue)
                {
                    UpdateTimedPlant(plant);
                    continue;
                }

                plant.ActionTimer -= dt;
                if (plant.Kind == PlantKind.PotatoMine)
                {
                    UpdateMine(i, plant);
                    continue;
                }

                if (UpdateExtendedPlant(i, plant))
                {
                    continue;
                }

                if (plant.ActionTimer > 0.000001f)
                {
                    continue;
                }

                if (plant.Kind == PlantKind.CherryBomb)
                {
                    for (var z = Zombies.Count - 1; z >= 0; z--)
                    {
                        var zombie = Zombies[z];
                        if (Math.Abs(zombie.Row - plant.Row) <= 1 && Math.Abs(zombie.X - (plant.Column + 0.5f)) <= 1.5f)
                        {
                            DamageZombie(z, 1800f);
                        }
                    }

                    Plants.RemoveAt(i);
                    Raise(GameEvent.Explosion);
                }
            }
        }

        /// <summary>Performs has original timing.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting has original timing.</returns>
        private static bool HasOriginalTiming(PlantKind kind)
        {
            return kind == PlantKind.Peashooter || kind == PlantKind.SnowPea || kind == PlantKind.Repeater || kind == PlantKind.Sunflower;
        }

        /// <summary>Checks for a nearby zombie without allocating a captured predicate.</summary>
        /// <param name="row">The center lane.</param>
        /// <param name="x">The center board x coordinate.</param>
        /// <param name="rowRange">The maximum lane difference.</param>
        /// <param name="distance">The horizontal distance threshold.</param>
        /// <param name="hostileOnly">Whether to exclude hypnotized zombies and use a strict distance threshold.</param>
        /// <returns>True when a matching zombie is present.</returns>
        private bool HasZombieNear(int row, float x, int rowRange, float distance, bool hostileOnly)
        {
            foreach (var zombie in Zombies)
            {
                var separation = Math.Abs(zombie.X - x);
                if ((!hostileOnly || !zombie.Hypnotized) && Math.Abs(zombie.Row - row) <= rowRange && (hostileOnly ? separation < distance : separation <= distance))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Finds a living plant by its per-match identifier without creating a predicate.</summary>
        /// <param name="id">The plant identifier to locate.</param>
        /// <returns>The matching plant, or null when it is no longer active.</returns>
        private Plant? FindPlantById(int id)
        {
            foreach (var plant in Plants)
            {
                if (plant.Id == id)
                {
                    return plant;
                }
            }

            return null;
        }

        /// <summary>Checks whether a plant kind occupies a specific cell origin.</summary>
        /// <param name="kind">The plant kind to locate.</param>
        /// <param name="row">The requested lane.</param>
        /// <param name="column">The requested origin column.</param>
        /// <returns>True when a matching plant is active.</returns>
        private bool HasPlantKindAt(PlantKind kind, int row, int column)
        {
            foreach (var plant in Plants)
            {
                if (plant.Kind == kind && plant.Row == row && plant.Column == column)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Checks an upgraded plant's existing area-target rules without a captured delegate.</summary>
        /// <param name="plant">The upgraded plant testing its attack range.</param>
        /// <returns>True when an eligible zombie is in range.</returns>
        private bool HasUpgradeAreaTarget(Plant plant)
        {
            foreach (var zombie in Zombies)
            {
                if (IsUpgradeAreaTarget(zombie, plant))
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
        private bool HasUmbrellaNear(int row, int column)
        {
            foreach (var plant in Plants)
            {
                if (plant.Kind == PlantKind.UmbrellaLeaf && Math.Abs(plant.Row - row) <= 1 && Math.Abs(plant.Column - column) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Checks whether a dancer already occupies its intended summon position.</summary>
        /// <param name="row">The summon lane.</param>
        /// <param name="x">The summon board x coordinate.</param>
        /// <returns>True when a backup dancer is nearby.</returns>
        private bool HasBackupDancerNear(int row, float x)
        {
            foreach (var zombie in Zombies)
            {
                if (zombie.Kind == ZombieKind.BackupDancer && zombie.Row == row && Math.Abs(zombie.X - x) < 1.2f)
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
        private static bool HasCellTimer(List<AdventureCellTimer> timers, int row, int column)
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

        /// <summary>Performs update timed plant.</summary>
        /// <param name = "plant">The plant.</param>
        private void UpdateTimedPlant(Plant plant)
        {
            var state = plant.RequiredTiming;
            var target = false;
            foreach (var zombie in Zombies)
            {
                if (zombie.Row == plant.Row && zombie.X > plant.Column + 0.4f)
                {
                    target = true;
                    break;
                }
            }
            var input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, true, true, target, global::PlantsVsZombies.Timing.ShootingTrack.Head, true, 0);
            var bound = global::PlantsVsZombies.Timing.PlantTiming.RequiredRandomBound(state, input);
            if (bound > 0)
            {
                input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, true, true, target, global::PlantsVsZombies.Timing.ShootingTrack.Head, true, _random.Next(bound));
            }

            var result = global::PlantsVsZombies.Timing.PlantTiming.Tick(state, input);
            plant.Timing = result.State;
            if (result.AnimationStarted)
            {
                plant.ShootAge = 0f;
            }

            if (result.SunCreated)
            {
                Suns.Add(CreateSun(plant.Column + 0.5f, plant.Row + 0.45f, plant.Row + 0.7f));
            }

            for (var i = 0; i < result.ProjectilesCreated; i++)
            {
                var shot = RentPea();
                shot.SourceKind = plant.Kind;
                shot.Row = plant.Row;
                shot.X = plant.Column + 0.85f;
                shot.IsSnow = plant.Kind == PlantKind.SnowPea;
                Peas.Add(shot);
                Raise(GameEvent.Shoot);
            }
        }

        /// <summary>Performs update mine.</summary>
        /// <param name = "plantIndex">The plant index.</param>
        /// <param name = "mine">The mine.</param>
        private void UpdateMine(int plantIndex, Plant mine)
        {
            if (mine.MinePhase == MinePhase.Buried && mine.ActionTimer <= 0f)
            {
                mine.MinePhase = MinePhase.Rising;
                mine.PhaseAge = 0f;
                mine.ActionTimer = 19f / 18f;
            }
            else if (mine.MinePhase == MinePhase.Rising && mine.ActionTimer <= 0f)
            {
                mine.MinePhase = MinePhase.Armed;
                mine.PhaseAge = 0f;
            }
            else if (mine.MinePhase == MinePhase.Armed && HasZombieNear(mine.Row, mine.Column + 0.5f, 0, 0.75f, false))
            {
                for (var z = Zombies.Count - 1; z >= 0; z--)
                {
                    if (Zombies[z].Row == mine.Row && Math.Abs(Zombies[z].X - (mine.Column + 0.5f)) <= 0.75f)
                    {
                        DamageZombie(z, 1800f);
                    }
                }

                Plants.RemoveAt(plantIndex);
                Raise(GameEvent.MineExplosion);
            }
        }

        /// <summary>Performs update peas.</summary>
        /// <param name = "dt">The dt.</param>
        private void UpdatePeas(float dt)
        {
            for (var i = Peas.Count - 1; i >= 0; i--)
            {
                var pea = Peas[i];
                var oldX = pea.X;
                pea.X += dt * 4.5f;
                var hit = -1;
                var closest = float.MaxValue;
                for (var j = 0; j < Zombies.Count; j++)
                {
                    var zombie = Zombies[j];
                    if (zombie.Row == pea.Row && zombie.X >= oldX - 0.15f && zombie.X <= pea.X + 0.35f && zombie.X < closest)
                    {
                        hit = j;
                        closest = zombie.X;
                    }
                }

                if (hit >= 0)
                {
                    DamageZombie(hit, 20f, pea.IsSnow);
                    RemovePeaAt(i);
                }
                else if (pea.X > 10f)
                {
                    RemovePeaAt(i);
                }
            }
        }

        /// <summary>Performs update zombies.</summary>
        /// <param name = "dt">The dt.</param>
        private void UpdateZombies(float dt)
        {
            for (var i = Zombies.Count - 1; i >= 0; i--)
            {
                var zombie = Zombies[i];
                var speedMultiplier = zombie.ChillRemaining > 0f ? 0.5f : 1f;
                zombie.ChillRemaining = Math.Max(0f, zombie.ChillRemaining - dt);
                zombie.Age += dt * speedMultiplier;
                Plant? target = null;
                for (var j = 0; j < Plants.Count; j++)
                {
                    var candidate = Plants[j];
                    if (candidate.Row == zombie.Row && Math.Abs(zombie.X - (candidate.Column + 0.5f)) < 0.38f)
                    {
                        target = candidate;
                        break;
                    }
                }

                zombie.Eating = target != null;
                if (target != null)
                {
                    zombie.BiteTimer -= dt * speedMultiplier;
                    if (zombie.BiteTimer <= 0f && !(target.Kind == PlantKind.PotatoMine && target.MinePhase != MinePhase.Buried) && target.Kind != PlantKind.CherryBomb && target.Kind != PlantKind.Jalapeno)
                    {
                        target.Health -= 4f;
                        zombie.BiteTimer += 0.04f;
                        if (target.Health <= 0f)
                        {
                            Plants.Remove(target);
                        }
                    }
                }
                else
                {
                    zombie.BiteTimer = 0f;
                    zombie.X -= Rules.ZombieSpeed * dt * speedMultiplier;
                }

                if (zombie.X < 0.15f && Mowers[zombie.Row].Available)
                {
                    Mowers[zombie.Row].Available = false;
                    Mowers[zombie.Row].Active = true;
                    Raise(GameEvent.Mower);
                }
                else if (zombie.X < -0.65f && !Mowers[zombie.Row].Active)
                {
                    State = MatchState.Lost;
                    Raise(GameEvent.Lost);
                    return;
                }
            }
        }

        /// <summary>Performs update mowers.</summary>
        /// <param name = "dt">The dt.</param>
        private void UpdateMowers(float dt)
        {
            foreach (Mower mower in Mowers)
            {
                if (!mower.Active)
                {
                    continue;
                }

                mower.X += dt * 5f;
                for (var i = Zombies.Count - 1; i >= 0; i--)
                {
                    if (Zombies[i].Row == mower.Row && Math.Abs(Zombies[i].X - mower.X) < 0.7f)
                    {
                        DamageZombie(i, 99999f);
                    }
                }

                if (mower.X > 10f)
                {
                    mower.Active = false;
                }
            }
        }

        /// <summary>Performs damage zombie.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "chill">The chill.</param>
        private void DamageZombie(int index, float damage, bool chill = false)
        {
            var zombie = Zombies[index];
            if (chill)
            {
                zombie.ChillRemaining = Math.Max(zombie.ChillRemaining, 10f);
            }

            var shieldDamage = Math.Min(zombie.ShieldHealth, damage);
            zombie.ShieldHealth -= shieldDamage;
            damage -= shieldDamage;
            var armorDamage = Math.Min(zombie.ArmorHealth, damage);
            zombie.ArmorHealth -= armorDamage;
            zombie.Health -= damage - armorDamage;
            Raise(GameEvent.ZombieHit);
            if (zombie.Health <= 0f)
            {
                Zombies.RemoveAt(index);
                Raise(GameEvent.ZombieDied);
            }
        }

        /// <summary>Performs emit lane flame.</summary>
        /// <param name = "row">The row.</param>
        private void EmitLaneFlame(int row)
        {
            var flame = _flamePool.Rent();
            flame.Row = row;
            flame.Age = 0f;
            Flames.Add(flame);
        }

        /// <summary>Performs raise.</summary>
        /// <param name = "gameEvent">The game event.</param>
        private void Raise(GameEvent gameEvent)
        {
            if (EventRaised != null)
            {
                EventRaised(gameEvent);
            }
        }

        /// <summary>Performs initialize adventure bowling native.</summary>
        /// <param name = "nut">The nut.</param>
        /// <param name = "giant">The giant.</param>
        private void InitializeAdventureBowlingNative(global::PlantsVsZombies.Adventure.AdventureBowlingNut nut, bool giant)
        {
            if (nut.NativeMotionInitialized)
            {
                return;
            }

            nut._bounceDelay = 0f; // Legacy field retained for old scene probes; native collision has no invented cooldown.
            nut.NativeMotionInitialized = true;
            nut.NativePixelX = (int)(nut.X * 80f);
            nut.NativePixelY = 80 + nut.Row * 100;
            global::PlantsVsZombies.Adventure.AdventureBowlingPresentation? pose;
            if (!Presentation.Bowling.TryGetValue(nut, out pose))
            {
                pose = new global::PlantsVsZombies.Adventure.AdventureBowlingPresentation
                {
                    Rate = (giant ? 6f : 12f) + (float)_random.NextDouble() * (giant ? 4f : 6f)
                };
                Presentation.Bowling.Add(nut, pose);
            }

            nut.NativeAge = 0f;
            pose.Age = 0f; // PC samples the initial ground pose before the first reanim update.
        }

        /// <summary>Performs find adventure bowling native target.</summary>
        /// <param name = "nut">The nut.</param>
        /// <param name = "giant">The giant.</param>
        /// <returns>The resulting find adventure bowling native target.</returns>
        private Zombie? FindAdventureBowlingNativeTarget(global::PlantsVsZombies.Adventure.AdventureBowlingNut nut, bool giant)
        {
            // PC bowling attack rect uses plant mX, mWidth-20=60 and signed horizontal overlap>=0.
            var attack = new NativeRect(nut.NativePixelX, nut.NativePixelY, 60, 80);
            foreach (var z in Zombies)
            {
                var phase = AdventureZombieFeedbackPhase(z);
                if (z.Row != nut.Row || z.Hypnotized || z.Health <= 0 || phase >= 1 && phase <= 3 || z.Airborne || z.DropRemaining > 0 || z.Kind == ZombieKind.Boss || z.Kind == ZombieKind.Bungee || z.Kind == ZombieKind.Digger && z.AbilityPhase == 0 || phase == 12 && !giant || phase == 54 && !giant)
                {
                    continue;
                }

                var x = (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f);
                var backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2);
                var body = global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.ZombieBounds((int)z.Kind, phase, x, (int)AdventureBiteNativeY(z, x), backwards, z.PresentationBiteBodyY, z.PresentationBiteClipHeight);
                if (body.X > 800)
                {
                    continue;
                }

                if (global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.HorizontalOverlap(attack, body) >= 0)
                {
                    return z;
                }
            }

            return null;
        }

        /// <summary>Performs hit adventure bowling native.</summary>
        /// <param name = "nut">The nut.</param>
        /// <param name = "z">The z.</param>
        /// <param name = "giant">The giant.</param>
        private void HitAdventureBowlingNative(global::PlantsVsZombies.Adventure.AdventureBowlingNut nut, Zombie z, bool giant)
        {
            RequestAdventureZombieFoley(20);
            if (giant || z.Kind == ZombieKind.ScreenDoor && z.ShieldHealth > 0 && nut.NativeDirection != 0)
            {
                HitAdventureZombie(z, 1800f);
            }
            else if (z.ShieldHealth > 0)
            {
                HitAdventureZombie(z, Math.Min(400f, z.ShieldHealth));
            }
            else if (z.ArmorHealth > 0)
            {
                if (z.Kind == ZombieKind.Buckethead)
                {
                    RequestAdventureZombieFoley(46);
                }
                else if (z.Kind == ZombieKind.Conehead)
                {
                    RequestAdventureZombieFoley(58);
                }

                HitAdventureZombie(z, Math.Min(900f, z.ArmorHealth));
            }
            else
            {
                HitAdventureZombie(z, 1800f);
            }

            nut._lastHitId = z.Id;
        }

        /// <summary>Performs advance adventure bowling native.</summary>
        /// <param name = "nut">The nut.</param>
        /// <param name = "dt">The dt.</param>
        /// <param name = "giant">The giant.</param>
        /// <returns>The resulting advance adventure bowling native.</returns>
        private bool AdvanceAdventureBowlingNative(global::PlantsVsZombies.Adventure.AdventureBowlingNut nut, float dt, bool giant)
        {
            InitializeAdventureBowlingNative(nut, giant);
            var pose = Presentation.Bowling[nut];
            nut._nativeTickRemainder += dt * 100f;
            var ticks = (int)Math.Floor(nut._nativeTickRemainder + .00001f);
            nut._nativeTickRemainder -= ticks;
            for (var tick = 0; tick < ticks; tick++)
            {
                nut.NativePixelX = global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.HorizontalStep(nut.NativePixelX, nut.NativeAge, pose.Rate, giant);
                nut.NativePixelY = global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.VerticalStep(nut.NativePixelY, nut.NativeDirection);
                nut.NativeAge += .01f;
                nut.X = nut.NativePixelX / 80f;
                var removed = nut.NativePixelX > 800;
                if (!global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.AtRow(nut.NativePixelY, nut.Row))
                {
                    if (removed)
                    {
                        pose.Age = nut.NativeAge;
                        return true;
                    }

                    continue;
                }

                var hit = FindAdventureBowlingNativeTarget(nut, giant);
                if (hit != null)
                {
                    if (nut.Explosive)
                    {
                        AdventurePresentationExplodeONut(nut.X, nut.Row);
                        BlastAdventure(nut.X, nut.Row, 1.5f, 1, 1800f, true);
                        Raise(GameEvent.Explosion);
                        return true;
                    }

                    HitAdventureBowlingNative(nut, hit, giant);
                }

                var choose = hit != null && !giant && nut.Row > 0 && nut.Row < 4 && nut.NativeDirection == 0;
                var direction = global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.NextDirection(nut.Row, nut.NativeDirection, hit != null, giant, choose ? _random.Next(2) : 0);
                nut.NativeDirection = direction;
                // Row is the next collision/render-order row; the visible Y remains continuous.
                if (direction == 28)
                {
                    nut.Row--;
                }
                else if (direction == 29)
                {
                    nut.Row++;
                }

                if (removed)
                {
                    pose.Age = nut.NativeAge;
                    return true;
                }
            }

            pose.Age = nut.NativeAge;
            return false;
        }

        /// <summary>Stores adventure cherry fuse sounds.</summary>
        private readonly Queue<global::PlantsVsZombies.Adventure.AdventureCherryBombSound> _adventureCherryFuseSounds = new Queue<global::PlantsVsZombies.Adventure.AdventureCherryBombSound>();
        /// <summary>Stores adventure cherry detonation sounds.</summary>
        private readonly Queue<global::PlantsVsZombies.Adventure.AdventureCherryBombSound> _adventureCherryDetonationSounds = new Queue<global::PlantsVsZombies.Adventure.AdventureCherryBombSound>();
        // Explicit notifications survive a headless tick or a host attaching before its first
        // Update. The host drains them at the corresponding existing Planted/Explosion event.
        /// <summary>Performs try take adventure cherry bomb sound.</summary>
        /// <param name = "trigger">The trigger.</param>
        /// <param name = "sound">The sound.</param>
        /// <returns>The resulting try take adventure cherry bomb sound.</returns>
        public bool TryTakeAdventureCherryBombSound(GameEvent trigger, out global::PlantsVsZombies.Adventure.AdventureCherryBombSound sound)
        {
            var queue = trigger == GameEvent.Planted ? _adventureCherryFuseSounds : trigger == GameEvent.Explosion ? _adventureCherryDetonationSounds : null;
            if (queue == null || queue.Count == 0)
            {
                sound = default(global::PlantsVsZombies.Adventure.AdventureCherryBombSound);
                return false;
            }

            sound = queue.Dequeue();
            return true;
        }

        // 5E7060 rounds after scaling the native 31-bit draw. NextDouble would round
        // a scale-1 result first, which changes some rate/shake/pitch results by one ULP.
        /// <summary>Performs adventure cherry random float.</summary>
        /// <param name = "range">The range.</param>
        /// <returns>The resulting adventure cherry random float.</returns>
        private float AdventureCherryRandomFloat(float range)
        {
            return (float)(_random.Next() / 2147483647.0 * range);
        }

        /// <summary>Performs initialize adventure cherry bomb.</summary>
        /// <param name = "plant">The plant.</param>
        private void InitializeAdventureCherryBomb(Plant plant)
        {
            if (plant.CherryBombPresentation != null)
            {
                return;
            }

            var state = new global::PlantsVsZombies.Adventure.AdventureCherryBombState
            {
                Rate = 10f + AdventureCherryRandomFloat(5f)
            };
            plant.CherryBombPresentation = state;
            plant._presentationKind = PlantKind.CherryBomb;
            plant.PresentationRate = state.Rate;
            plant.ActionTimer = global::PlantsVsZombies.Adventure.AdventureCherryBombState.FuseTicks * .01f;
            // 52A5A0 selects the only original variation with Rand(1), even though its
            // choice is fixed. Keep that draw on the replayable simulation stream.
            _random.Next(1);
            _adventureCherryFuseSounds.Enqueue(new global::PlantsVsZombies.Adventure.AdventureCherryBombSound());
        }

        /// <summary>Performs advance adventure cherry bomb.</summary>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting advance adventure cherry bomb.</returns>
        private bool AdvanceAdventureCherryBomb(Plant plant)
        {
            InitializeAdventureCherryBomb(plant);
            var state = plant.RequiredCherryBombPresentation;
            if (state.Detonated)
            {
                return true;
            }

            // PC 471815..471825 decrements the special counter before testing zero.
            if (state.Countdown > 0)
            {
                state.Countdown--;
            }

            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 0)
            {
                DetonateAdventureCherryBomb(plant);
            }

            // PC Update 47230D..472317 still calls Animate after DoSpecial/Die.
            // Animate 47321B..47324D consumes two Rand(2)-1 offsets, including tick 100.
            state.ShakeX = AdventureCherryRandomFloat(2f) - 1f;
            state.ShakeY = AdventureCherryRandomFloat(2f) - 1f;
            state.AnimationTicks++;
            state.NormalizedTime = Math.Min(.999999f, state.NormalizedTime + .01f * state.Rate / global::PlantsVsZombies.Adventure.AdventureCherryBombState.ExplodeFrameCount);
            return true;
        }

        /// <summary>Performs detonate adventure cherry bomb.</summary>
        /// <param name = "plant">The plant.</param>
        private void DetonateAdventureCherryBomb(Plant plant)
        {
            InitializeAdventureCherryBomb(plant);
            var state = plant.RequiredCherryBombPresentation;
            if (state.Detonated)
            {
                return;
            }

            state.Detonated = true;
            state.Countdown = 0;
            plant.ActionTimer = 0;
            // Local PC 474B83..474C32: Foley 55 (boom), then Foley 65 (juicy).
            // Foley table 74B3B8 gives pitch ranges 0 and 2 respectively.
            _random.Next(1);
            var juicyPitch = AdventureCherryRandomFloat(2f);
            _random.Next(1);
            _adventureCherryDetonationSounds.Enqueue(new global::PlantsVsZombies.Adventure.AdventureCherryBombSound { Detonation = true, JuicyPitchSteps = juicyPitch });
            Raise(GameEvent.Explosion);
            BlastAdventure(plant.Column + .5f, plant.Row, 1.5f, 1, 1800f, true);
            AdventurePresentationExplosion(PlantKind.CherryBomb, plant.Column + .5f, plant.Row);
            RemoveAdventurePlantAndStack(plant);
        }

        /// <summary>Gets or sets uses native adventure conveyor.</summary>
        public bool UsesNativeAdventureConveyor
        {
            get
            {
                if (!IsAdventure)
                {
                    return false;
                }

                if (ModeSession == null)
                {
                    return Adventure.Level.HasConveyor;
                }

                var definition = ModeSession.Definition;
                return definition.UsesPackets && definition.Mechanic != ModeMechanic.Vases && definition.Mechanic != ModeMechanic.RainingSeeds && definition.Mechanic != ModeMechanic.Slots;
            }
        }

        /// <summary>Performs initialize adventure conveyor packet.</summary>
        /// <param name = "packet">The packet.</param>
        /// <param name = "index">The index.</param>
        private void InitializeAdventureConveyorPacket(global::PlantsVsZombies.Adventure.AdventurePacket packet, int index)
        {
            if (!UsesNativeAdventureConveyor || packet._conveyorInitialized)
            {
                return;
            }

            // PC AddSeed 0x49F4C0 sets mOffsetX=515-51*i; retain the original 50px slot pitch.
            var offset = 515 - 51 * index;
            if (index > 0 && offset < Adventure._packets[index - 1].ConveyorOffsetX)
            {
                offset = Adventure._packets[index - 1].ConveyorOffsetX + 40;
            }

            packet.ConveyorOffsetX = offset;
            packet._conveyorInitialized = true;
        }

        /// <summary>Performs advance adventure conveyor packets.</summary>
        private void AdvanceAdventureConveyorPackets()
        {
            if (!UsesNativeAdventureConveyor)
            {
                return;
            }

            Adventure.ConveyorTicks++;
            for (var i = 0; i < Adventure._packets.Count; i++)
            {
                var packet = Adventure._packets[i];
                InitializeAdventureConveyorPacket(packet, i);
                if (Adventure.ConveyorTicks % 4 == 0 && packet.ConveyorOffsetX > 0)
                {
                    packet.ConveyorOffsetX--;
                }
            }
        }

        /// <summary>Performs adventure conveyor card x.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting adventure conveyor card x.</returns>
        public float AdventureConveyorCardX(int index)
        {
            return 91 + 50 * index + Adventure._packets[index].ConveyorOffsetX;
        }

        /// <summary>Performs remove adventure packet.</summary>
        /// <param name = "packet">The packet.</param>
        private void RemoveAdventurePacket(global::PlantsVsZombies.Adventure.AdventurePacket packet)
        {
            var index = Adventure._packets.IndexOf(packet);
            if (index < 0)
            {
                return;
            }

            // PC RemoveSeed 0x49F530 moves the next type forward and adds51 to its offset.
            if (UsesNativeAdventureConveyor)
            {
                for (var i = index + 1; i < Adventure._packets.Count; i++)
                {
                    Adventure._packets[i].ConveyorOffsetX += 51;
                }
            }

            Adventure._packets.RemoveAt(index);
        }

        /// <summary>Performs adventure presentation explosion.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        private void AdventurePresentationExplosion(PlantKind kind, float x, int row)
        {
            global::PlantsVsZombies.Adventure.AdventureExplosionEffect effect;
            switch (kind)
            {
                case PlantKind.CherryBomb:
                case PlantKind.ExplodeONut:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.Powie;
                    break;
                case PlantKind.DoomShroom:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.Doom;
                    break;
                case PlantKind.PotatoMine:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.PotatoMine;
                    break;
                case PlantKind.Jalapeno:
                    AdventurePresentationJalapenoFire(row);
                    return;
                default:
                    return; // Cob already creates BlastMark + PopcornSplash, not Powie.
            }

            Presentation.Explosions.Add(new global::PlantsVsZombies.Adventure.AdventureExplosion {
                Id = ++Presentation._effectId,
                Seed = _random.Next(),
                Row = row,
                X = x,
                Effect = effect
            });
        }

        /// <summary>Performs adventure presentation potato mine rise.</summary>
        /// <param name = "plant">The plant.</param>
        private void AdventurePresentationPotatoMineRise(Plant plant)
        {
            Presentation.Explosions.Add(new global::PlantsVsZombies.Adventure.AdventureExplosion {
                Id = ++Presentation._effectId,
                Seed = _random.Next(),
                Row = plant.Row,
                X = plant.Column + .5f,
                Effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.PotatoMineRise
            });
        }

        /// <summary>Creates one of the original Grave Buster particle systems above the plant's cell.</summary>
        /// <param name="plant">The Grave Buster that owns the particles.</param>
        /// <param name="effect">Either the eating (GraveBuster) or the finishing (GraveBusterDie) effect.</param>
        private void AdventurePresentationGraveBuster(Plant plant, global::PlantsVsZombies.Adventure.AdventureExplosionEffect effect)
        {
            Presentation.Explosions.Add(new global::PlantsVsZombies.Adventure.AdventureExplosion {
                Id = ++Presentation._effectId,
                Seed = _random.Next(),
                Row = plant.Row,
                X = plant.Column + .5f,
                Effect = effect
            });
        }

        /// <summary>Starts the attached rock particles when the Grave Buster finishes landing (Plant::UpdateGraveBuster).</summary>
        /// <param name="plant">The Grave Buster whose timer was just advanced.</param>
        private void StartAdventureGraveBusterEating(Plant plant)
        {
            if (plant.GraveBusterEatingStarted || !global::PlantsVsZombies.Adventure.AdventureGraveBusterRules.IsEating(plant.ActionTimer))
            {
                return;
            }

            plant.GraveBusterEatingStarted = true;
            AdventurePresentationGraveBuster(plant, global::PlantsVsZombies.Adventure.AdventureExplosionEffect.GraveBuster);
        }

        /// <summary>Performs adventure explosion random float.</summary>
        /// <param name = "range">The range.</param>
        /// <returns>The resulting adventure explosion random float.</returns>
        private float AdventureExplosionRandomFloat(float range)
        {
            return (float)(_random.Next() / 2147483647.0 * range);
        }

        /// <summary>Performs adventure presentation jalapeno fire.</summary>
        /// <param name = "row">The row.</param>
        private void AdventurePresentationJalapenoFire(int row)
        {
            // PC Board::DoFwoosh 429060: replace a previous row's active flame tracks,
            // twelve placements from x=10 to 760, original fire (reanim type 34).
            Presentation.JalapenoFires.RemoveAll(f => f.Row == row);
            var fire = new global::PlantsVsZombies.Adventure.AdventureJalapenoFire
            {
                Id = ++Presentation._effectId,
                Row = row
            };
            for (var i = 0; i < 12; i++)
            {
                var speed = (float)(AdventureExplosionRandomFloat(.59999996f) + .699999988079071);
                var scale = (float)(AdventureExplosionRandomFloat(.20000005f) + .8999999761581421);
                if (_random.Next(2) == 0)
                {
                    scale = -scale;
                }

                fire.Flames.Add(new global::PlantsVsZombies.Adventure.AdventureJalapenoFlame { X = (float)(750.0 * i / 11.0 + 10.0), Rate = 12f * speed, ScaleX = scale });
            }

            Presentation.JalapenoFires.Add(fire);
        }

        /// <summary>Performs advance adventure explosion presentation.</summary>
        private void AdvanceAdventureExplosionPresentation()
        {
            for (var i = Presentation.Explosions.Count - 1; i >= 0; i--)
            {
                if (++Presentation.Explosions[i].Ticks >= Presentation.Explosions[i].DurationTicks)
                {
                    Presentation.Explosions.RemoveAt(i);
                }
            }

            foreach (var fire in Presentation.JalapenoFires)
            {
                fire.Ticks++;
                var remaining = Math.Max(0, 100 - fire.Ticks);
                // PC 429250 -> TodAnimateCurve(50,0,remaining,12,0,Linear).
                // 526F20 rounds positive results by +.5 then truncation.
                var stillFlaming = (int)(12.0 * Math.Min(50, remaining) / 50.0 + .5);
                for (var j = 0; j < 12 - stillFlaming; j++)
                {
                    if (fire.Flames[j].DoneTick < 0)
                    {
                        fire.Flames[j].DoneTick = fire.Ticks;
                    }
                }
            }

            // anim_done has ten frames; PlayOnceFullLastFrame at the PC's fixed 15 fps.
            for (var i = Presentation.JalapenoFires.Count - 1; i >= 0; i--)
            {
                var fire = Presentation.JalapenoFires[i];
                var finished = true;
                foreach (var flame in fire.Flames)
                {
                    if (flame.DoneTick < 0 || (fire.Ticks - flame.DoneTick) * .01f < 10f / 15f)
                    {
                        finished = false;
                        break;
                    }
                }

                if (finished)
                {
                    Presentation.JalapenoFires.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs hit adventure explosion zombie.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "burned">The burned.</param>
        /// <param name = "flags">The flags.</param>
        private void HitAdventureExplosionZombie(Zombie zombie, float damage, bool burned, uint flags)
        {
            if (zombie.GraveRiseTicks > 0)
            {
                return;
            }

            // PC 42954D uses BurnBody (54E920); Potato takes the separate damage(1800,0x12)
            // branch. BurnBody compares BODY health, so thick armor cannot absorb an ash kill.
            if (burned && damage == 1800f && zombie.Kind != ZombieKind.Boss && zombie.Health < 1800f)
            {
                zombie.PresentationDeath = global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned;
                zombie.ArmorHealth = 0;
                zombie.ShieldHealth = 0;
                HitAdventureZombie(zombie, damage, true, 0x12);
            }
            else
            {
                HitAdventureZombie(zombie, damage, true, burned ? 0x12U : flags);
            }
        }

        // Hook for the modes owner: PC explosive bowling nut also calls Powie (not PotatoMine).
        /// <summary>Performs adventure presentation explode onut.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        private void AdventurePresentationExplodeONut(float x, int row)
        {
            AdventurePresentationExplosion(PlantKind.ExplodeONut, x, row);
        }

        /// <summary>Performs request adventure ice launch.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "shot">The shot.</param>
        private void RequestAdventureIceLaunch(Plant plant, Pea shot)
        {
            if (plant.Kind != PlantKind.SnowPea && plant.Kind != PlantKind.WinterMelon)
            {
                return;
            }

            // PC 4752E0: both types play native Foley3 AND Foley27, once at actual release.
            RequestAdventureZombieFoley(3);
            RequestAdventureZombieFoley(27);
            Presentation._iceShootSuppressions++;
            Presentation.IceLaunchSoundSerial++;
            if (plant.Kind == PlantKind.SnowPea)
            {
                BeginAdventureSnowTrail(shot);
            }
        }

        /// <summary>Performs can adventure chill.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting can adventure chill.</returns>
        private bool CanAdventureChill(Zombie z)
        {
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            return global::PlantsVsZombies.Adventure.AdventureIceRules.CanChill((int)z.Kind, AdventureZombieFeedbackPhase(z), z.Health <= 0, z.Hypnotized, sled);
        }

        /// <summary>Performs apply adventure chill.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "iceTrap">The ice trap.</param>
        /// <returns>The resulting apply adventure chill.</returns>
        public bool ApplyAdventureChill(Zombie z, bool iceTrap = false)
        {
            if (z == null || !CanAdventureChill(z))
            {
                return false;
            }

            if (z.ChillRemaining <= 0f)
            {
                RequestAdventureZombieFoley(18);
                Presentation.ChillOnsetSoundSerial++;
            }

            z.ChillRemaining = Math.Max(z.ChillRemaining, iceTrap ? 20f : 10f);
            return true;
        }

        /// <summary>Performs apply adventure ice trap.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting apply adventure ice trap.</returns>
        public bool ApplyAdventureIceTrap(Zombie z)
        {
            if (z == null)
            {
                return false;
            }

            var wasCold = z.ChillRemaining > 0f || z.FrozenRemaining > 0f && z.PresentationButter <= 0f;
            var chilled = ApplyAdventureChill(z, true);
            if (!global::PlantsVsZombies.Adventure.AdventureIceRules.CanFreeze((int)z.Kind, AdventureZombieFeedbackPhase(z), chilled))
            {
                return false;
            }

            var pool = z.PresentationInPool ?? (Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            var offset = pool ? 0 : _random.Next(wasCold ? 101 : 201);
            z.FrozenRemaining = Math.Max(z.PresentationButter, global::PlantsVsZombies.Adventure.AdventureIceRules.FreezeTicks(pool, wasCold, offset) * .01f);
            // PC 54E150 also applies 20 damage with bit1 (bypass held shield). Butter is a separate counter.
            HitAdventureZombie(z, 20f, true, 1U);
            return true;
        }

        /// <summary>Performs apply adventure projectile cold.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "z">The z.</param>
        private void ApplyAdventureProjectileCold(Pea shot, Zombie z)
        {
            if (shot.IsSnow && global::PlantsVsZombies.Adventure.AdventureIceRules.ProjectileReachesColdState(shot.Damage, z.ShieldHealth, shot.PiercesShield))
            {
                ApplyAdventureChill(z);
            }

            if (shot.Fire && z.Kind != ZombieKind.Zomboni && z.Kind != ZombieKind.Catapult && (z.ShieldHealth <= 0f || z.Kind != ZombieKind.ScreenDoor && z.Kind != ZombieKind.Ladder))
            {
                z.ChillRemaining = 0f;
                if (z.PresentationButter <= 0f)
                {
                    z.FrozenRemaining = 0f;
                }
            }
        }

        /// <summary>Performs adventure ice projectile top left.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        private void AdventureIceProjectileTopLeft(Pea shot, out float x, out float y)
        {
            var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            x = 40f + shot.X * 80f;
            if (shot.Lobbed)
            {
                y = shot.PresentationLaunchY + shot.PresentationLobHeight;
                return;
            }

            var slope = Adventure.Level.HasRoof ? Math.Max(0f, 5f - shot.X) * 20f - 10f : 0f;
            x -= 14f;
            y = 80f + shot.FlightRow * pitch + slope + 16f;
        }

        /// <summary>Performs begin adventure snow trail.</summary>
        /// <param name = "shot">The shot.</param>
        private void BeginAdventureSnowTrail(Pea shot)
        {
            if (shot.PresentationSnowTrail != null)
            {
                return;
            }

            float x, y;
            AdventureIceProjectileTopLeft(shot, out x, out y);
            var trail = new global::PlantsVsZombies.Adventure.AdventureSnowTrail
            {
                Id = ++Presentation._effectId,
                Seed = _random.Next(),
                Projectile = shot,
                Row = shot.Row,
                X = x + 8f,
                Y = y + 13f,
                PreviousX = x + 8f,
                PreviousY = y + 13f
            };
            shot.PresentationSnowTrail = trail;
            Presentation.SnowTrails.Add(trail);
        }

        /// <summary>Performs update adventure snow trail.</summary>
        /// <param name = "shot">The shot.</param>
        private void UpdateAdventureSnowTrail(Pea shot)
        {
            if (shot.IsSnow && !shot.Lobbed)
            {
                BeginAdventureSnowTrail(shot);
            }

            var trail = shot.PresentationSnowTrail;
            if (trail == null || trail.Stopped)
            {
                return;
            }

            float x, y;
            AdventureIceProjectileTopLeft(shot, out x, out y);
            trail.X = x + 8f;
            trail.Y = y + 13f;
            trail.Row = shot.Row;
            if (!shot.IsSnow || shot.Lobbed)
            {
                // PC ConvertToPea 47E230 calls AttachmentDie (40E620), NOT FadeOut.
                Presentation.SnowTrails.Remove(trail);
                shot.PresentationSnowTrail = null;
                RequestAdventureZombieFoley(3);
            }
        }

        /// <summary>Performs stop adventure snow trail.</summary>
        /// <param name = "shot">The shot.</param>
        private void StopAdventureSnowTrail(Pea shot)
        {
            var trail = shot.PresentationSnowTrail;
            if (trail != null && !trail.Stopped)
            {
                trail.StopAge = trail.Age;
                trail.Projectile = null;
            }
        }

        /// <summary>Performs advance adventure ice presentation.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureIcePresentation(float dt)
        {
            for (var i = Presentation.SnowTrails.Count - 1; i >= 0; i--)
            {
                var trail = Presentation.SnowTrails[i];
                trail.PreviousX = trail.X;
                trail.PreviousY = trail.Y;
                trail.Age += dt;
                if (trail.Stopped && trail.Age - trail.StopAge >= .5f)
                {
                    Presentation.SnowTrails.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs record adventure ice impact.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "impact">The impact.</param>
        private void RecordAdventureIceImpact(Pea shot, Zombie? zombie, global::PlantsVsZombies.Adventure.AdventureImpact impact)
        {
            StopAdventureSnowTrail(shot);
            if (!shot.IsSnow)
            {
                return;
            }

            float x, y;
            AdventureIceProjectileTopLeft(shot, out x, out y);
            if (shot.Lobbed)
            {
                // PC winter-melon impact is particle1 at the lobbed ground/contact coordinates +(30,30).
                // mPosX-mVelX and mPosY+mPosZ-mVelY-mVelZ are the previous native integration point.
                var px = 40f + shot.PresentationPreviousX * 80f;
                var py = shot.PresentationLaunchY + shot.PresentationPreviousLobHeight;
                x = px + 30f;
                y = py + 30f;
            }
            else
            {
                var zx = zombie == null ? 0 : (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(zombie.X, zombie.IsSmall ? .5f : 1f);
                var zy = zombie == null ? 0 : (int)AdventureBiteNativeY(zombie, zx);
                global::PlantsVsZombies.Adventure.AdventureIceRules.SnowImpact(x, y, zombie != null, zx, zy, zombie == null ? 0 : AdventureZombieFeedbackPhase(zombie), shot.Backward, out x, out y);
                if (zombie != null)
                {
                    impact.AttachedZombie = zombie;
                    impact.NativeAttachedX = x - zx;
                    impact.NativeAttachedY = y - zy;
                }
            }

            impact.NativeIcePoint = true;
            impact.NativeParticleX = x;
            impact.NativeParticleY = y;
        }

        /// <summary>Performs adventure ice impact point.</summary>
        /// <param name = "impact">The impact.</param>
        /// <param name = "fraction">The fraction.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        public void AdventureIceImpactPoint(global::PlantsVsZombies.Adventure.AdventureImpact impact, float fraction, out float x, out float y)
        {
            x = impact.NativeParticleX;
            y = impact.NativeParticleY;
            var z = impact.AttachedZombie;
            if (z == null)
            {
                return;
            }

            var grid = z.PresentationPreviousX + (z.X - z.PresentationPreviousX) * Math.Max(0f, Math.Min(1f, fraction));
            x = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(grid, z.IsSmall ? .5f : 1f);
            y = AdventureBiteNativeY(z, x) + impact.NativeAttachedY;
            x += impact.NativeAttachedX;
        }

        // PC Board.DropLootPiece 0x4289C0: 30000 tickets; +14/+250/+2500 currency cutoffs.
        /// <summary>Performs drop adventure loot.</summary>
        /// <param name = "z">The z.</param>
        private void DropAdventureLoot(Zombie z)
        {
            var a = Adventure;
            if (Rules.AdventureDisableLoot || (ModeSession == null && !a.Level.IsReplay && a.Level.Number < 11) || z.BungeeCarrier)
            {
                return;
            }

            if (ModeSession == null && !a.Level.IsReplay && a.CurrentWave > 5 && !a._droppedModeGift && (a.Level.Number == 22 && !Rules.AdventureMinigamesUnlocked || a.Level.Number == 36 && !Rules.AdventurePuzzleUnlocked))
            {
                SpawnAdventureCoin(a.Level.Number == 22 ? AdventureCoinKind.MinigameUnlock : AdventureCoinKind.PuzzleUnlock, Math.Max(.1f, Math.Min(8.8f, z.X)), z.Row);
                a._droppedModeGift = true;
                return;
            }

            if (a.Level.Mode == AdventureMode.LittleTrouble && _random.Next(4) != 0)
            {
                return;
            }

            var x = Math.Max(.1f, Math.Min(8.8f, z.X));
            if (z.Kind == ZombieKind.Yeti)
            {
                for (var i = 0; i < 4; i++)
                {
                    SpawnAdventureCoin(AdventureCoinKind.Diamond, Math.Max(.1f, x - i * .125f), z.Row);
                }

                return;
            }

            var factor = Math.Max(0, VerifiedDataCatalog.Zombies[(int)z.Kind].WaveBudgetCostRaw);
            if (factor == 0)
            {
                return;
            }

            DropAdventureLootPiece(x, z.Row, factor);
        }

        /// <summary>Rolls the original Board::DropLootPiece tickets and spawns the resulting sun, coin or gift.</summary>
        /// <param name="x">The board column position of the drop, already clamped to the lawn.</param>
        /// <param name="row">The lawn row of the drop.</param>
        /// <param name="factor">The drop factor that scales every ticket cutoff.</param>
        private void DropAdventureLootPiece(float x, int row, int factor)
        {
            var a = Adventure;
            if (Rules.AdventureDisableLoot)
            {
                return;
            }

            var roll = _random.Next(30000);
            if (ModeSession == null && !a.Level.IsReplay && a.Level.Number == 11 && !a._droppedFirstCoin && a.CurrentWave > 5)
            {
                roll = 1000;
            }

            if (ModeSession != null && ModeSession.Definition.Mechanic == global::PlantsVsZombies.Modes.ModeMechanic.Columns)
            {
                roll *= 5;
            }

            if (a.Level.Mode == AdventureMode.WhackAZombie)
            {
                var cutoff = SunAmount > 500 ? 2800 : SunAmount > 350 ? 3100 : SunAmount > 200 ? 3700 : 5000;
                if (roll >= 2500 * factor && roll < cutoff * factor)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        Suns.Add(CreateSun(x - i * .25f, row, row + .3f));
                    }

                    return;
                }
            }

            if (a.TotalWavesSpawned > 70)
            {
                return;
            }

            var endless = ModeSession != null && ModeSession.Definition.Survival && ModeSession.Definition.Endless;
            var plant = Rules.AdventurePottedLoot ? (endless ? 3 : a.Level.IsReplay ? 24 : 12) : 0;
            var chocolate = plant + (Rules.AdventureChocolateLoot ? (endless ? 9 : a.Level.IsReplay ? 72 : 36) : 0);
            AdventureCoinKind kind;
            if (roll < plant * factor)
            {
                kind = AdventureCoinKind.PlantGift;
            }
            else if (roll < chocolate * factor)
            {
                kind = AdventureCoinKind.Chocolate;
            }
            else if (roll < (chocolate + 14) * factor)
            {
                kind = Rules.AdventurePacketSlots > 6 ? AdventureCoinKind.Diamond : AdventureCoinKind.Gold;
            }
            else if (roll < (chocolate + 250) * factor)
            {
                kind = AdventureCoinKind.Gold;
            }
            else if (roll < (chocolate + 2500) * factor)
            {
                kind = AdventureCoinKind.Silver;
            }
            else
            {
                return;
            }

            // Original bowling coins come from ricochets rather than every zombie death.
            if (a.Level.Mode == AdventureMode.Bowling && kind <= AdventureCoinKind.Diamond)
            {
                return;
            }

            var coin = SpawnAdventureCoin(kind, x, row);
            if (coin == null)
            {
                return;
            }

            if (kind == AdventureCoinKind.PlantGift)
            {
                coin.GiftPlant = PickAdventureGardenGift();
            }

            a._droppedFirstCoin = true;
        }

        /// <summary>Performs pick adventure garden gift.</summary>
        /// <returns>The resulting pick adventure garden gift.</returns>
        private PlantKind PickAdventureGardenGift()
        {
            if (ModeSession != null && ModeSession.Definition.Endless)
            {
                var all = new List<PlantKind>();
                for (var i = 0; i < 40; i++)
                {
                    if (i != 33 && i != 38)
                    {
                        all.Add((PlantKind)i);
                    }
                }

                return all[_random.Next(all.Count)];
            }

            int[][] plants =
            {
                new[]
                {
                    0,
                    1,
                    2,
                    3,
                    7,
                    4,
                    5,
                    6
                },
                new[]
                {
                    8,
                    9,
                    10,
                    11,
                    12,
                    13,
                    14,
                    15
                },
                new[]
                {
                    16,
                    17,
                    18,
                    19,
                    20,
                    21,
                    22,
                    23
                },
                new[]
                {
                    24,
                    25,
                    26,
                    27,
                    28,
                    29,
                    30,
                    31
                },
                new[]
                {
                    32,
                    34,
                    35,
                    36,
                    37,
                    39
                }
            };
            var index = (int)Adventure.Level.Terrain;
            if (index >= 5)
            {
                index = 4;
            }

            var list = plants[index];
            return (PlantKind)list[_random.Next(list.Length)];
        }

        /// <summary>Performs initialize adventure native plant timing.</summary>
        /// <param name = "plant">The plant.</param>
        private void InitializeAdventureNativePlantTiming(Plant plant)
        {
            if (plant.Kind == PlantKind.CherryBomb)
            {
                InitializeAdventureCherryBomb(plant);
                return;
            }

            if (plant.Kind == PlantKind.PotatoMine)
            {
                InitializeAdventurePotatoMine(plant);
                return;
            }

            if (plant.Kind == PlantKind.Starfruit)
            {
                plant.StarfruitLaunchCountdown = _random.Next(151);
                plant.ActionTimer = plant.StarfruitLaunchCountdown * .01f;
                return;
            }

            if (plant.Kind == PlantKind.Threepeater || plant.Kind == PlantKind.SplitPea)
            {
                plant.MultiHeadTiming = AdventureMultiHeadTimingRules.Initialize(plant.Kind, _random.Next(151));
                plant.ActionTimer = plant.MultiHeadTiming.Value.Countdown * .01f;
                return;
            }

            if (plant.Kind != PlantKind.Peashooter && plant.Kind != PlantKind.SnowPea && plant.Kind != PlantKind.Repeater && plant.Kind != PlantKind.Sunflower
                && plant.Kind != PlantKind.FumeShroom && plant.Kind != PlantKind.ScaredyShroom && plant.Kind != PlantKind.Cactus
                && plant.Kind != PlantKind.CabbagePult && plant.Kind != PlantKind.KernelPult && plant.Kind != PlantKind.MelonPult
                && plant.Kind != PlantKind.WinterMelon && !AdventurePuffShroomRules.IsShortRange(plant.Kind))
            {
                return;
            }

            var kind = (global::PlantsVsZombies.Timing.PlantKind)(int)plant.Kind;
            plant.Timing = global::PlantsVsZombies.Timing.PlantTiming.Initialize(kind, _random.Next(global::PlantsVsZombies.Timing.PlantTiming.InitialRandomBound(kind)));
            plant.ActionTimer = plant.Timing.Value.Countdown * .01f;
        }

        /// <summary>Performs advance adventure native timed plant.</summary>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting advance adventure native timed plant.</returns>
        private bool AdvanceAdventureNativeTimedPlant(Plant plant)
        {
            if (plant.Kind == PlantKind.Chomper)
            {
                return AdvanceAdventureChomper(plant);
            }

            if (plant.Kind == PlantKind.CherryBomb)
            {
                return AdvanceAdventureCherryBomb(plant);
            }

            if (plant.Kind == PlantKind.PotatoMine)
            {
                return AdvanceAdventurePotatoMine(plant);
            }

            if (plant.Kind == PlantKind.Starfruit)
            {
                AdvanceAdventureStarfruit(plant);
                return true;
            }

            if ((plant.Kind == PlantKind.Threepeater || plant.Kind == PlantKind.SplitPea) && plant.ActionTimer < 100000f)
            {
                AdvanceAdventureMultiHeadPlant(plant);
                return true;
            }

            if (plant.Kind != PlantKind.Peashooter && plant.Kind != PlantKind.SnowPea && plant.Kind != PlantKind.Repeater && plant.Kind != PlantKind.Sunflower
                && plant.Kind != PlantKind.FumeShroom && plant.Kind != PlantKind.ScaredyShroom && plant.Kind != PlantKind.Cactus
                && plant.Kind != PlantKind.CabbagePult && plant.Kind != PlantKind.KernelPult && plant.Kind != PlantKind.MelonPult
                && plant.Kind != PlantKind.WinterMelon && !AdventurePuffShroomRules.IsShortRange(plant.Kind))
            {
                return false;
            }

            if (plant.ActionTimer >= 100000f)
            {
                return false;
            }

            if (!plant.Timing.HasValue)
            {
                InitializeAdventureNativePlantTiming(plant);
            }

            var sunflower = plant.Kind == PlantKind.Sunflower;
            var shortPuff = AdventurePuffShroomRules.IsShortRange(plant.Kind);
            var fume = plant.Kind == PlantKind.FumeShroom;
            var cactus = plant.Kind == PlantKind.Cactus;
            var kernel = plant.Kind == PlantKind.KernelPult;
            var scaredy = plant.Kind == PlantKind.ScaredyShroom;
            var targetZombie = fume || shortPuff || sunflower ? null : FindAdventureTarget(plant.Row,
                plant.ModeFacesLeft ? -1f : plant.Column + .4f, plant.ModeFacesLeft ? plant.Column + .5f : 20f, cactus);
            var target = fume ? FindAdventureFumeTarget(plant) != null : shortPuff ? FindAdventurePuffTarget(plant) != null
                : !sunflower && (targetZombie != null || !plant.ModeFacesLeft && CanShootAdventureBoss(plant))
                    && (!scaredy || !HasZombieNear(plant.Row, plant.Column + .5f, 1, 1.5f, true));
            var state = plant.RequiredTiming;
            // PC 0x46D240 starts the body animation and its species-specific shooting counter together.
            var track = shortPuff || fume || scaredy || cactus || plant.Kind == PlantKind.CabbagePult || kernel
                || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon
                ? global::PlantsVsZombies.Timing.ShootingTrack.Body : global::PlantsVsZombies.Timing.ShootingTrack.Head;
            var input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, true, true, target, track, true, 0);
            var bound = global::PlantsVsZombies.Timing.PlantTiming.RequiredRandomBound(state, input);
            var draw = bound == 0 ? 0 : _random.Next(bound);
            var butter = kernel && state.Countdown <= 1 && target && _random.Next(4) == 0;
            var result = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, true, true, target, track, true, draw, butter));
            plant.Timing = result.State;
            plant.ActionTimer = result.State.Countdown * .01f;
            if (result.AnimationStarted)
            {
                if (kernel)
                {
                    plant.PresentationButter = butter;
                    plant._kernelWeaponPrepared = true;
                }

                plant.PresentationShotAge = 0;
                plant.PresentationHeadAge[0] = 0;
                plant._presentationPrefiring = true;
                plant.ShootAge = 0;
            }

            if (result.SunCreated)
            {
                Suns.Add(CreateAdventurePresentationSun(plant, 25));
            }

            for (var i = 0; i < result.ProjectilesCreated; i++)
            {
                if (fume)
                {
                    FireAdventureFume(plant);
                    continue;
                }

                if (scaredy || cactus || plant.Kind == PlantKind.CabbagePult || kernel || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon)
                {
                    FireAdventureNativeTimedProjectile(plant);
                    continue;
                }

                var shot = FireAdventurePea(plant, plant.Row, plant.ModeFacesLeft, 20f);
                shot.IsSnow = plant.Kind == PlantKind.SnowPea;
                if (shortPuff)
                {
                    shot.Range = float.MaxValue;
                }

                // Source 0x472C70 queues the shot independently of later target loss.
                if (!plant.ModeFacesLeft && Adventure.Level.HasRoof && plant.Column < 5)
                {
                    shot.Range = Math.Max(.1f, 5 - plant.Column - .5f);
                }
            }

            return true;
        }

        /// <summary>Advances the original shared shooting countdown of a multi-head plant.</summary>
        /// <param name="plant">The Threepeater or Split Pea being updated.</param>
        private void AdvanceAdventureMultiHeadPlant(Plant plant)
        {
            if (!plant.MultiHeadTiming.HasValue)
            {
                InitializeAdventureNativePlantTiming(plant);
            }

            var available = plant.Kind == PlantKind.SplitPea ? AdventureMultiHeadTimingRules.SplitPeaHeads : AdventureShootingHeads.Second;
            var targets = AdventureShootingHeads.None;
            if (plant.Kind == PlantKind.Threepeater)
            {
                if (plant.Row + 1 < RowCount)
                {
                    available |= AdventureShootingHeads.First;
                }

                if (plant.Row > 0)
                {
                    available |= AdventureShootingHeads.Third;
                }
            }

            foreach (var zombie in Zombies)
            {
                if (zombie.Hypnotized || zombie.Airborne || zombie.Submerged || zombie.DropRemaining > 0f || zombie.Kind == ZombieKind.Bungee
                    || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, plant.Kind))
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                var plantX = 40 + plant.Column * 80;
                var forward = body.X + body.Width >= plantX + 60 && body.X <= plantX + 860;
                if (plant.Kind == PlantKind.SplitPea && zombie.Row == plant.Row)
                {
                    if (forward)
                    {
                        targets |= AdventureShootingHeads.First;
                    }

                    if (body.X + body.Width >= 0 && body.X <= plantX + 16)
                    {
                        targets |= AdventureShootingHeads.Second;
                    }
                }
                else if (plant.Kind == PlantKind.Threepeater && forward)
                {
                    if (zombie.Row == plant.Row + 1)
                    {
                        targets |= AdventureShootingHeads.First;
                    }
                    else if (zombie.Row == plant.Row)
                    {
                        targets |= AdventureShootingHeads.Second;
                    }
                    else if (zombie.Row == plant.Row - 1)
                    {
                        targets |= AdventureShootingHeads.Third;
                    }
                }
            }

            var state = plant.RequiredMultiHeadTiming;
            var input = new AdventureMultiHeadTimingInput(true, true, true, targets, available, true, 0);
            var bound = AdventureMultiHeadTimingRules.RequiredRandomBound(state, input);
            var result = AdventureMultiHeadTimingRules.Tick(state,
                new AdventureMultiHeadTimingInput(true, true, true, targets, available, true, bound == 0 ? 0 : _random.Next(bound)));
            plant.MultiHeadTiming = result.State;
            plant.ActionTimer = result.State.Countdown * .01f;
            for (var head = 0; head < 3; head++)
            {
                var mask = (AdventureShootingHeads)(1 << head);
                if ((result.FiredHeads & mask) != 0)
                {
                    var row = plant.Kind == PlantKind.Threepeater ? plant.Row + 1 - head : plant.Row;
                    var back = plant.Kind == PlantKind.SplitPea && head == 1;
                    var shot = FireAdventurePea(plant, row, back, 20f);
                    if (!back && Adventure.Level.HasRoof && plant.Column < 5)
                    {
                        shot.Range = Math.Max(.1f, 5 - plant.Column - .5f);
                    }
                }

                if ((result.StoppedHeads & mask) != 0)
                {
                    plant.PresentationHeadAge[head] = -1f;
                }

                if ((result.StartedHeads & mask) != 0)
                {
                    plant.PresentationHeadAge[head] = 0f;
                    plant.PresentationShotAge = 0f;
                    plant.ShootAge = 0f;
                }
            }
        }

        /// <summary>Advances the original Starfruit launch and forty-tick body shooting counters.</summary>
        /// <param name="plant">The Starfruit being updated.</param>
        private void AdvanceAdventureStarfruit(Plant plant)
        {
            if (plant.StarfruitLaunchCountdown < 0)
            {
                InitializeAdventureNativePlantTiming(plant);
            }

            if (plant.StarfruitShootingCountdown > 0)
            {
                var countdown = plant.StarfruitShootingCountdown;
                if (AdventureStarfruitRules.AdvanceAttack(ref countdown, true))
                {
                    FireAdventureStarfruit(plant);
                }

                plant.StarfruitShootingCountdown = countdown;
            }

            plant.StarfruitLaunchCountdown--;
            plant.ActionTimer = Math.Max(0, plant.StarfruitLaunchCountdown) * .01f;
            if (plant.StarfruitLaunchCountdown > 0)
            {
                return;
            }

            plant.StarfruitLaunchCountdown = 150 - _random.Next(15);
            plant.ActionTimer = plant.StarfruitLaunchCountdown * .01f;
            if (plant.RecentlyEatenTicks <= 0 && FindAdventureStarfruitTarget(plant) == null)
            {
                return;
            }

            plant.StarfruitShootingCountdown = AdventureStarfruitRules.ShootingCountdown;
            plant.PresentationShotAge = 0;
            plant.PresentationHeadAge[0] = 0;
            plant._presentationPrefiring = true;
            plant.ShootAge = 0;
        }

        /// <summary>Stores native plant hit tick remainder.</summary>
        private float _nativePlantHitTickRemainder;
        /// <summary>Performs record adventure plant hit.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "nativeTicks">The native ticks.</param>
        public void RecordAdventurePlantHit(Plant plant, int nativeTicks = 25)
        {
            if (plant != null)
            {
                plant.PresentationEatenFlashTicks = Math.Max(plant.PresentationEatenFlashTicks, nativeTicks);
            }
        }

        /// <summary>Performs advance adventure plant hit counters.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventurePlantHitCounters(float dt)
        {
            _nativePlantHitTickRemainder += dt * 100f;
            var ticks = (int)Math.Floor(_nativePlantHitTickRemainder + .00001f);
            _nativePlantHitTickRemainder -= ticks;
            foreach (var p in Plants)
            {
                p.PresentationEatenFlashTicks = Math.Max(0, p.PresentationEatenFlashTicks - ticks);
                p.RecentlyEatenTicks = Math.Max(0, p.RecentlyEatenTicks - ticks);
            }

            for (var i = Presentation.PlantBiteEffects.Count - 1; i >= 0; i--)
            {
                var e = Presentation.PlantBiteEffects[i];
                e.Age += dt;
                if (e.Age >= 2f)
                {
                    Presentation.PlantBiteEffects.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs record adventure chew effect.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "p">The p.</param>
        private void RecordAdventureChewEffect(Zombie z, Plant? p)
        {
            if (p == null)
            {
                return;
            }

            RecordAdventurePlantHit(p);
            if (p.Kind != PlantKind.Wallnut && p.Kind != PlantKind.TallNut)
            {
                return;
            }

            float x = 37f, y = 40f;
            if (z.Kind == ZombieKind.Snorkel || z.Kind == ZombieKind.DolphinRider)
            {
                x -= 7f;
                y += 70f;
            }
            else if (z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2))
            {
                x += 47f;
            }
            else if (z.Kind == ZombieKind.Balloon)
            {
                y += 47f;
            }
            else if (z.Kind == ZombieKind.Imp)
            {
                x += 24f;
                y += 40f;
            }

            Presentation.PlantBiteEffects.Add(new global::PlantsVsZombies.Adventure.AdventurePlantBiteEffect {
                Id = ++Presentation._effectId,
                ParticleId = 35,
                Row = z.Row,
                Kind = z.Kind,
                NativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f),
                NativeY = AdventureBiteNativeY(z, global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f)),
                OffsetX = x,
                OffsetY = y + z.PresentationBiteBodyY,
                Seed = z.Id * 397 ^ Presentation._effectId
            });
        }

        /// <summary>Gets or sets last adventure planting failure.</summary>
        public AdventurePlantingFailure LastAdventurePlantingFailure { get; private set; }

        /// <summary>Performs adventure plant at.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "layer">The layer.</param>
        /// <returns>The resulting adventure plant at.</returns>
        public Plant AdventurePlantAt(int row, int column, PlantLayer layer)
        {
            foreach (var plant in Plants)
            {
                if (plant.Row == row && plant.Column <= column && column < plant.Column + plant.ColumnSpan && plant.Layer == layer)
                {
                    return plant;
                }
            }

            return null!;
        }

        /// <summary>Performs plant adventure.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "handled">The handled.</param>
        /// <param name = "planted">The planted.</param>
        private void PlantAdventure(PlantKind kind, int row, int column, ref bool handled, ref bool planted)
        {
            handled = true;
            if (Adventure.Level.HasConveyor || Adventure.Level.Mode == AdventureMode.Vasebreaker)
            {
                var packet = Adventure._packets.Find(p => p.Kind == kind);
                planted = packet != null && TryUseAdventurePacket(packet.Id, row, column);
                if (packet == null)
                {
                    LastAdventurePlantingFailure = AdventurePlantingFailure.SeedUnavailable;
                }

                return;
            }

            LastAdventurePlantingFailure = CheckAdventurePlanting(kind, row, column, false);
            if (LastAdventurePlantingFailure != AdventurePlantingFailure.None)
            {
                return;
            }

            if (!SpendAdventureSun(AdventureSeedCost(kind)))
            {
                return;
            }

            _seedCooldownTicks[(int)kind] = DefinitionFor(ResolveAdventureSeed(kind)).RechargeThresholdRaw + 1;
            PlaceAdventurePlant(kind, row, column);
            planted = true;
        }

        /// <summary>Performs can plant adventure.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting can plant adventure.</returns>
        public AdventurePlantingFailure CanPlantAdventure(PlantKind kind, int row, int column)
        {
            return !IsAdventure ? AdventurePlantingFailure.NotPlaying : CheckAdventurePlanting(kind, row, column, Adventure.Level.HasConveyor || Adventure.Level.Mode == AdventureMode.Vasebreaker);
        }

        /// <summary>Performs try use adventure packet.</summary>
        /// <param name = "packetId">The packet id.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try use adventure packet.</returns>
        public bool TryUseAdventurePacket(int packetId, int row, int column)
        {
            if (!IsAdventure)
            {
                return false;
            }

            var p = Adventure._packets.Find(x => x.Id == packetId);
            if (p == null)
            {
                return false;
            }

            LastAdventurePlantingFailure = CheckAdventurePlanting(p.Kind, row, column, true);
            if (LastAdventurePlantingFailure != AdventurePlantingFailure.None)
            {
                return false;
            }

            if (Adventure.Level.Mode == AdventureMode.Bowling)
            {
                Adventure._nuts.Add(new AdventureBowlingNut { Row = row, X = column + 0.5f, Explosive = p.Kind == PlantKind.ExplodeONut });
            }
            else
            {
                PlaceAdventurePlant(p.Kind, row, column);
            }

            RemoveAdventurePacket(p);
            return true;
        }

        /// <summary>Performs check adventure planting.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "packet">The packet.</param>
        /// <returns>The resulting check adventure planting.</returns>
        private AdventurePlantingFailure CheckAdventurePlanting(PlantKind kind, int row, int column, bool packet)
        {
            var a = Adventure;
            var l = a.Level;
            if (State != MatchState.Playing || a.Phase != AdventurePhase.Playing)
            {
                return AdventurePlantingFailure.NotPlaying;
            }

            if (!l.IsLawnRow(row) || column < 0 || column >= Columns || kind < PlantKind.Peashooter || kind > PlantKind.ExplodeONut)
            {
                return AdventurePlantingFailure.InvalidCell;
            }

            if (l.Number == 5 && a.Tutorial == AdventureTutorial.Shovel)
            {
                return AdventurePlantingFailure.Tutorial;
            }

            if (l.Mode == AdventureMode.Bowling)
            {
                return packet && (kind == PlantKind.Wallnut || kind == PlantKind.ExplodeONut) ? column < 3 ? AdventurePlantingFailure.None : AdventurePlantingFailure.BowlingLine : AdventurePlantingFailure.SeedUnavailable;
            }

            if (kind == PlantKind.Imitater && !a.ImitaterTarget.HasValue)
            {
                return AdventurePlantingFailure.ImitaterTarget;
            }

            var whackSeed = l.Mode == AdventureMode.WhackAZombie && (kind == PlantKind.CherryBomb || kind == PlantKind.GraveBuster || kind == PlantKind.PotatoMine);
            if (!packet && !whackSeed && !ContainsSeed(a.SelectedSeeds, kind))
            {
                return AdventurePlantingFailure.SeedUnavailable;
            }

            if (!packet && _seedCooldownTicks[(int)kind] > 0)
            {
                return AdventurePlantingFailure.Cooldown;
            }

            if (!packet && AdventureSunAvailable < AdventureSeedCost(kind))
            {
                return AdventurePlantingFailure.Sun;
            }

            kind = ResolveAdventureSeed(kind);
            if (!l.IsReplay && (l.Number == 1 && a.Tutorial == AdventureTutorial.ChooseSeed || l.Number == 2 && a._plantedSunflowers < 2 && kind != PlantKind.Sunflower || l.Number == 5 && a.Tutorial == AdventureTutorial.Shovel))
            {
                return AdventurePlantingFailure.Tutorial;
            }

            if (a._vases.Exists(v => v.Row == row && v.Column == column))
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (a._craters.Exists(v => v.Row == row && v.Column == column))
            {
                return AdventurePlantingFailure.Crater;
            }

            if (a._ice.Exists(v => v.Row == row && v.Column == column))
            {
                return AdventurePlantingFailure.Ice;
            }

            var grave = a._graves.Exists(g => g.Row == row && g.Column == column);
            if (kind == PlantKind.GraveBuster)
            {
                return grave && AdventurePlantAt(row, column, PlantLayer.Main) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.Grave;
            }

            if (grave)
            {
                return AdventurePlantingFailure.Grave;
            }

            if (IsAdventureFirstAid(kind, row, column))
            {
                return AdventurePlantingFailure.None;
            }

            var main = AdventurePlantAt(row, column, PlantLayer.Main);
            if (UpgradePrerequisite(kind).HasValue)
            {
                return CheckAdventureUpgrade(kind, row, column);
            }

            if (kind == PlantKind.Pumpkin && main != null && main.Kind == PlantKind.CobCannon)
            {
                return AdventurePlantingFailure.CannonFootprint;
            }

            if (kind == PlantKind.CoffeeBean)
            {
                return main != null && main.Asleep && main.WakeTimer <= 0 && AdventurePlantAt(row, column, PlantLayer.Effect) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.NeedsSleepingMushroom;
            }

            var aquatic = kind == PlantKind.LilyPad || kind == PlantKind.TangleKelp || kind == PlantKind.SeaShroom;
            if (aquatic && !l.IsWaterRow(row))
            {
                return AdventurePlantingFailure.NeedsWater;
            }

            if (l.HasRoof && kind == PlantKind.Spikeweed)
            {
                return AdventurePlantingFailure.RoofRestriction;
            }

            var layer = LayerFor(kind);
            if (AdventurePlantAt(row, column, layer) != null)
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (l.IsWaterRow(row) && kind == PlantKind.PotatoMine)
            {
                return AdventurePlantingFailure.NeedsWater;
            }

            var support = AdventurePlantAt(row, column, PlantLayer.Support);
            if (l.IsWaterRow(row) && !aquatic && !(kind == PlantKind.Pumpkin && main != null && main.Kind == PlantKind.Cattail) && (support == null || support.Kind != PlantKind.LilyPad))
            {
                return AdventurePlantingFailure.NeedsLilyPad;
            }

            if (kind == PlantKind.LilyPad && main != null)
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (kind == PlantKind.FlowerPot && (l.IsWaterRow(row) || main != null))
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (l.HasRoof && kind != PlantKind.FlowerPot && (support == null || support.Kind != PlantKind.FlowerPot))
            {
                return AdventurePlantingFailure.NeedsFlowerPot;
            }

            return AdventurePlantingFailure.None;
        }

        /// <summary>Performs layer for.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting layer for.</returns>
        private static PlantLayer LayerFor(PlantKind kind)
        {
            return kind == PlantKind.LilyPad || kind == PlantKind.FlowerPot ? PlantLayer.Support : kind == PlantKind.Pumpkin ? PlantLayer.Armor : PlantLayer.Main;
        }

        /// <summary>Performs nocturnal.</summary>
        /// <param name = "k">The k.</param>
        /// <returns>The resulting nocturnal.</returns>
        private static bool Nocturnal(PlantKind k)
        {
            return (int)k >= 8 && (int)k <= 15 && k != PlantKind.GraveBuster || k == PlantKind.SeaShroom || k == PlantKind.MagnetShroom || k == PlantKind.GloomShroom;
        }

        /// <summary>Performs place adventure plant.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting place adventure plant.</returns>
        private Plant? PlaceAdventurePlant(PlantKind kind, int row, int column)
        {
            if ((int)kind >= 40 && (int)kind <= 48)
            {
                var upgraded = PlaceAdventureUpgrade(kind, row, column);
                Adventure.Notice(AdventureNoticeKind.UpgradePlanted, (int)kind, row, column);
                return upgraded;
            }

            if (kind == PlantKind.CoffeeBean)
            {
                var target = AdventurePlantAt(row, column, PlantLayer.Main);
                if (target != null)
                {
                    target.WakeTimer = 2f;
                }

                Raise(GameEvent.Planted);
                return target;
            }

            if (IsAdventureFirstAid(kind, row, column))
            {
                var old = AdventurePlantAt(row, column, kind == PlantKind.Pumpkin ? PlantLayer.Armor : PlantLayer.Main);
                Plants.Remove(old);
            }

            var plant = new Plant
            {
                Id = ++Adventure._plantId,
                Kind = kind,
                Row = row,
                Column = column,
                Layer = LayerFor(kind),
                Health = kind == PlantKind.Wallnut || kind == PlantKind.Pumpkin ? 4000f : kind == PlantKind.TallNut ? 8000f : kind == PlantKind.Garlic ? 400f : 300f,
                Asleep = Nocturnal(kind) && !Adventure.Level.IsNight,
                ActionTimer = kind == PlantKind.PotatoMine ? 15f : kind == PlantKind.CherryBomb || kind == PlantKind.Jalapeno || kind == PlantKind.DoomShroom || kind == PlantKind.IceShroom ? 1f : kind == PlantKind.GraveBuster ? global::PlantsVsZombies.Adventure.AdventureGraveBusterRules.TotalSeconds : kind == PlantKind.Sunflower || kind == PlantKind.SunShroom ? 5f + (float)_random.NextDouble() * 2.5f : 0.5f,
                IsSmall = false
            };
            InitializeAdventureNativePlantTiming(plant);
            Plants.Add(plant);
            if (kind == PlantKind.GraveBuster)
            {
                // Plant::PlantInit plays FOLEY_GRAVEBUSTERCHOMP when the buster lands in play.
                Presentation.GraveBusterChompSoundSerial++;
            }

            if (kind == PlantKind.Sunflower)
            {
                Adventure._plantedSunflowers++;
                if (!Adventure.Level.IsReplay && Adventure.Level.Number == 2)
                {
                    Adventure.Tutorial = Adventure._plantedSunflowers >= 2 ? AdventureTutorial.Complete : AdventureTutorial.RechargeSunflower;
                }
            }

            if (!Adventure.Level.IsReplay && Adventure.Level.Number == 1)
            {
                Adventure.Tutorial = Adventure._collectedSun ? AdventureTutorial.Complete : AdventureTutorial.CollectSun;
            }

            if (Adventure.Tutorial == AdventureTutorial.PoolSupport && kind == PlantKind.LilyPad || Adventure.Tutorial == AdventureTutorial.RoofPots && kind == PlantKind.CabbagePult || Adventure.Tutorial == AdventureTutorial.NightMushrooms && Nocturnal(kind))
            {
                Adventure.Tutorial = AdventureTutorial.Complete;
            }

            Raise(GameEvent.Planted);
            return plant;
        }

        /// <summary>Calls <see cref="PlaceAdventurePlant"/> where a result must exist, failing with a descriptive exception instead of returning null.</summary>
        /// <param name="kind">The <c>kind</c> argument forwarded to <see cref="PlaceAdventurePlant"/>.</param>
        /// <param name="row">The <c>row</c> argument forwarded to <see cref="PlaceAdventurePlant"/>.</param>
        /// <param name="column">The <c>column</c> argument forwarded to <see cref="PlaceAdventurePlant"/>.</param>
        /// <returns>The non-null place adventure plant result.</returns>
        /// <exception cref="System.InvalidOperationException">The method returned no result.</exception>
        private Plant RequiredPlaceAdventurePlant(PlantKind kind, int row, int column) => PlaceAdventurePlant(kind, row, column) ?? throw new System.InvalidOperationException("'PlaceAdventurePlant' returned no result.");

        /// <summary>Performs remove adventure plant.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "handled">The handled.</param>
        /// <param name = "removed">The removed.</param>
        private void RemoveAdventurePlant(int row, int column, ref bool handled, ref bool removed)
        {
            handled = true;
            if (Adventure.Phase != AdventurePhase.Playing || !Adventure.Level.IsReplay && Adventure.Level.Number < 5 || Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                return;
            }

            var p = AdventurePlantAt(row, column, PlantLayer.Effect) ?? AdventurePlantAt(row, column, PlantLayer.Armor) ?? AdventurePlantAt(row, column, PlantLayer.Main) ?? AdventurePlantAt(row, column, PlantLayer.Support);
            if (p == null)
            {
                return;
            }

            RemoveAdventurePlantAndStack(p);
            removed = true;
            if (Adventure.Tutorial == AdventureTutorial.Shovel && !Plants.Exists(x => x.Row == 2 && x.Column >= 3 && x.Column <= 5 && x.Kind == PlantKind.Peashooter))
            {
                Adventure._removalTutorialDone = true;
                Adventure.Tutorial = AdventureTutorial.Complete;
            }
        }

        /// <summary>Removes the plant selected by the native shovel hit test.</summary>
        /// <param name="plant">The highlighted tool target, or null when the pointer has no target.</param>
        /// <returns>Whether the target was present and could be removed from the playing board.</returns>
        public bool TryShovelAdventurePlant(Plant? plant)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing
                || !Adventure.Level.IsReplay && Adventure.Level.Number < 5 || Adventure.Level.Mode == AdventureMode.WhackAZombie
                || plant == null || plant.Kind == PlantKind.GraveBuster || !Plants.Contains(plant))
            {
                return false;
            }

            RemoveAdventurePlantAndStack(plant);
            if (Adventure.Tutorial == AdventureTutorial.Shovel && !Plants.Exists(x => x.Row == 2 && x.Column >= 3 && x.Column <= 5 && x.Kind == PlantKind.Peashooter))
            {
                Adventure._removalTutorialDone = true;
                Adventure.Tutorial = AdventureTutorial.Complete;
            }

            return true;
        }

        /// <summary>Performs remove adventure plant and stack.</summary>
        /// <param name = "p">The p.</param>
        private void RemoveAdventurePlantAndStack(Plant p)
        {
            Plants.Remove(p);
            if (p.Layer == PlantLayer.Support)
            {
                Plants.RemoveAll(x => x.Row == p.Row && x.Column <= p.Column && p.Column < x.Column + x.ColumnSpan);
            }

            if (Adventure.SelectedCobCannonId.HasValue && FindPlantById(Adventure.SelectedCobCannonId.Value) == null)
            {
                Adventure.SelectedCobCannonId = null;
            }
        }

        /// <summary>Performs is adventure cell visible.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting is adventure cell visible.</returns>
        public bool IsAdventureCellVisible(int row, int column)
        {
            if (!IsAdventure || !Adventure.Level.IsLawnRow(row) || column < 0 || column >= 9)
            {
                return false;
            }

            if (Adventure.Level.Mode == AdventureMode.Storm && !Adventure.StormFlash)
            {
                return column < 2;
            }

            if (Adventure.Level.Terrain != AdventureTerrain.Fog || Adventure.FogBlownRemaining > 0f || column < 4)
            {
                return true;
            }

            foreach (var plant in Plants)
            {
                if (plant.Kind == PlantKind.Plantern && Math.Abs(plant.Row - row) <= 1 && Math.Abs(plant.Column - column) <= 2)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs advance adventure plants.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventurePlants(float dt)
        {
            foreach (Plant p in new PooledSnapshot<Plant>(Plants))
            {
                var producerAdvanced = false;
                int producerSunCount = Suns.Count, producerActions = p.ActionCount;
                try
                {
                    if (!Plants.Contains(p))
                    {
                        continue;
                    }

                    p.Age += dt;
                    p.PhaseAge += dt;
                    if (p.ShootAge >= 0)
                    {
                        p.ShootAge += dt;
                    }

                    if (p.WakeTimer > 0f)
                    {
                        p.WakeTimer -= dt;
                        if (p.WakeTimer <= 0f)
                        {
                            p.Asleep = false;
                            p.AwakeByCoffee = true;
                        }
                    }

                    if (p.Asleep)
                    {
                        continue;
                    }

                    if (AdvanceAdventureNativeTimedPlant(p))
                    {
                        producerAdvanced = p.Kind == PlantKind.Sunflower;
                        continue;
                    }

                    p.SpecialTimer = Math.Max(0f, p.SpecialTimer - dt);
                    if (p.Kind == PlantKind.SunShroom && p.PresentationAwakeAge >= 120f && p.PresentationAwakeAge < 121f)
                    {
                        continue;
                    }

                    p.ActionTimer -= dt;
                    if (p.Kind == PlantKind.GraveBuster)
                    {
                        StartAdventureGraveBusterEating(p);
                    }

                    producerAdvanced = AdventureProducerGlow.HasNativeProductionHint(p.Kind);
                    if (AdvanceAdventureUpgradePlant(p, dt))
                    {
                        continue;
                    }

                    if (p.ActionTimer > 0.000001f)
                    {
                        continue;
                    }

                    var x = p.Column + 0.5f;
                    switch (p.Kind)
                    {
                        case PlantKind.Sunflower:
                        case PlantKind.SunShroom:
                            Suns.Add(CreateAdventurePresentationSun(p, p.Kind == PlantKind.SunShroom && p.PresentationAwakeAge < 120f ? 15 : 25));
                            p.ActionTimer = 24f;
                            break;
                        case PlantKind.Marigold:
                            p.ActionCount++;
                            SpawnAdventureCoin(_random.Next(100) < 10 ? AdventureCoinKind.Gold : AdventureCoinKind.Silver, p.Column + 0.5f, p.Row);
                            p.ActionTimer = 24f;
                            break;
                        case PlantKind.CherryBomb:
                            DetonateAdventureCherryBomb(p);
                            break;
                        case PlantKind.Jalapeno:
                            BlastAdventure(4.5f, p.Row, 20f, 0, 1800f, true);
                            for (var i = Adventure._ice.Count - 1; i >= 0; i--)
                            {
                                if (Adventure._ice[i].Row == p.Row)
                                {
                                    Adventure._ice.RemoveAt(i);
                                }
                            }

                            for (var i = Adventure._balls.Count - 1; i >= 0; i--)
                            {
                                if (Adventure._balls[i].Row == p.Row && Adventure._balls[i].IsIce)
                                {
                                    Adventure._balls.RemoveAt(i);
                                }
                            }
                            if (Adventure.BossHeadVulnerable)
                            {
                                DamageAdventureBoss(1800f);
                            }

                            AdventurePresentationExplosion(PlantKind.Jalapeno, x, p.Row);
                            RemoveAdventurePlantAndStack(p);
                            Raise(GameEvent.JalapenoExplosion);
                            break;
                        case PlantKind.IceShroom:
                            NotifyAdventureIceTrap();
                            foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
                            {
                                ApplyAdventureIceTrap(z);
                            }

                            Adventure._balls.RemoveAll(b => !b.IsIce);
                            if (Adventure.BossHeadVulnerable)
                            {
                                DamageAdventureBoss(20f);
                            }

                            RemoveAdventurePlantAndStack(p);
                            break;
                        case PlantKind.DoomShroom:
                            BlastAdventure(x, p.Row, 3.5f, 2, 1800f, true);
                            AdventurePresentationExplosion(PlantKind.DoomShroom, x, p.Row);
                            RemoveAdventurePlantAndStack(p);
                            Adventure._craters.Add(new AdventureCellTimer { Row = p.Row, Column = p.Column, Remaining = 180f });
                            Raise(GameEvent.Explosion);
                            break;
                        case PlantKind.GraveBuster:
                            for (var i = Adventure._graves.Count - 1; i >= 0; i--)
                            {
                                var grave = Adventure._graves[i];
                                if (grave.Row == p.Row && grave.Column == p.Column)
                                {
                                    Adventure._graves.RemoveAt(i);
                                }
                            }

                            // Plant::UpdateGraveBuster: GraveBusterDie particles, Die(), then DropLootPiece(mX + 40, mY, 12).
                            AdventurePresentationGraveBuster(p, global::PlantsVsZombies.Adventure.AdventureExplosionEffect.GraveBusterDie);
                            RemoveAdventurePlantAndStack(p);
                            DropAdventureLootPiece(x, p.Row, global::PlantsVsZombies.Adventure.AdventureGraveBusterRules.LootDropFactor);
                            break;
                        case PlantKind.Blover:
                            for (var i = Zombies.Count - 1; i >= 0; i--)
                            {
                                if (Zombies[i].Airborne)
                                {
                                    Zombies[i].PresentationDeath = AdventureZombieDeathPresentation.Removed;
                                    KillAdventureZombie(Zombies[i]);
                                }
                            }

                            Adventure.FogBlownRemaining = 24f;
                            RemoveAdventurePlantAndStack(p);
                            break;
                        case PlantKind.PotatoMine:
                            AdvanceAdventurePotatoMine(p);
                            break;
                        case PlantKind.Squash:
                            var squash = FindAdventureTarget(p.Row, x - 1f, x + 1.3f, false);
                            if (squash != null)
                            {
                                BlastAdventure(squash.X, p.Row, 0.5f, 0, 1800f);
                                RemoveAdventurePlantAndStack(p);
                                Raise(GameEvent.Explosion);
                            }

                            break;
                        case PlantKind.TangleKelp:
                            var kelp = FindAdventureTarget(p.Row, x - 0.5f, x + 0.7f, false, true, PlantKind.TangleKelp);
                            if (kelp != null)
                            {
                                kelp.PresentationDeath = AdventureZombieDeathPresentation.Eaten;
                                KillAdventureZombie(kelp);
                                RemoveAdventurePlantAndStack(p);
                            }

                            break;
                        case PlantKind.Spikeweed:
                            foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
                            {
                                if (!z.Airborne && !z.Hypnotized && z.Row == p.Row && Math.Abs(z.X - x) < 0.6f)
                                {
                                    if (z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult)
                                    {
                                        KillAdventureZombie(z);
                                        RemoveAdventurePlantAndStack(p);
                                        break;
                                    }

                                    HitAdventureZombie(z, 20f, true);
                                }
                            }

                            p.ActionTimer = 1f;
                            break;
                        case PlantKind.MagnetShroom:
                            Zombie? metal = null;
                            foreach (var zombie in Zombies)
                            {
                                if (!zombie.Hypnotized && Math.Abs(zombie.Row - p.Row) <= 2 && Math.Abs(zombie.X - x) <= 3f && (zombie.ArmorHealth > 0f && (zombie.Kind == ZombieKind.Buckethead || zombie.Kind == ZombieKind.Football || zombie.Kind == ZombieKind.Digger) || (zombie.Kind == ZombieKind.ScreenDoor || zombie.Kind == ZombieKind.Ladder) && zombie.ShieldHealth > 0f || (zombie.Kind == ZombieKind.Pogo || zombie.Kind == ZombieKind.Ladder || zombie.Kind == ZombieKind.Digger || zombie.Kind == ZombieKind.JackInTheBox) && !zombie.UsedAbility))
                                {
                                    metal = zombie;
                                    break;
                                }
                            }
                            if (metal != null)
                            {
                                metal.ArmorHealth = 0;
                                metal.ShieldHealth = 0;
                                metal.UsedAbility = true;
                                metal.Submerged = false;
                                metal.AbilityPhase = 2;
                                if (metal.Kind == ZombieKind.Pogo)
                                {
                                    metal.Speed = 0.18f;
                                }

                                if (metal.Kind == ZombieKind.JackInTheBox)
                                {
                                    metal.AbilityTimer = float.MaxValue;
                                }

                                p.ActionTimer = 15f;
                            }

                            break;
                        case PlantKind.Peashooter:
                        case PlantKind.SnowPea:
                        case PlantKind.Repeater:
                        case PlantKind.PuffShroom:
                        case PlantKind.SeaShroom:
                            var range = p.Kind == PlantKind.PuffShroom || p.Kind == PlantKind.SeaShroom ? 3f : 20f;
                            var air = p.Kind == PlantKind.Cactus;
                            if (p.Kind == PlantKind.ScaredyShroom && HasZombieNear(p.Row, x, 1, 1.5f, true))
                            {
                                break;
                            }

                            var shortPuff = AdventurePuffShroomRules.IsShortRange(p.Kind);
                            var shooterTarget = shortPuff ? FindAdventurePuffTarget(p) : FindAdventureTarget(p.Row, x - 0.1f, x + range, air);
                            if (shooterTarget != null || !shortPuff && CanShootAdventureBoss(p))
                            {
                                var lob = p.Kind == PlantKind.CabbagePult || p.Kind == PlantKind.KernelPult || p.Kind == PlantKind.MelonPult;
                                // Straight projectiles from the sloped left roof strike the slope, while lobbers clear it.
                                if (!Adventure.Level.HasRoof || p.Column >= 5 || lob)
                                {
                                    var shot = FireAdventurePea(p, p.Row, false, lob ? p.Kind == PlantKind.MelonPult ? 80f : p.Kind == PlantKind.CabbagePult ? 40f : 20f : 20f);
                                    shot.IsSnow = p.Kind == PlantKind.SnowPea;
                                    shot.HitsAir = air;
                                    shot.Range = shortPuff ? float.MaxValue : range;
                                    shot.Lobbed = lob;
                                    shot.PiercesShield = lob;
                                    shot.Splash = p.Kind == PlantKind.MelonPult;
                                    if (p.Kind == PlantKind.KernelPult)
                                    {
                                        shot.Butter = p._kernelWeaponPrepared ? p.PresentationButter : _random.Next(4) == 0;
                                        p._kernelWeaponPrepared = false;
                                        if (shot.Butter)
                                        {
                                            shot.Damage = 40;
                                        }
                                    }

                                    if (p.Kind == PlantKind.Repeater)
                                    {
                                        FireAdventurePea(p, p.Row, false, 20f).X -= 0.3f;
                                    }
                                }

                                p.ActionTimer = lob ? 2.9f : 1.425f;
                            }

                            break;
                        case PlantKind.Threepeater:
                            var target = false;
                            for (var r = Math.Max(0, p.Row - 1); r <= Math.Min(RowCount - 1, p.Row + 1); r++)
                            {
                                if (FindAdventureTarget(r, x - 0.1f, 20f, false) != null)
                                {
                                    target = true;
                                }
                            }

                            if (target)
                            {
                                for (var r = Math.Max(0, p.Row - 1); r <= Math.Min(RowCount - 1, p.Row + 1); r++)
                                {
                                    FireAdventurePea(p, r, false, 20f);
                                }

                                p.ActionTimer = 1.425f;
                            }

                            break;
                        case PlantKind.SplitPea:
                            bool front = FindAdventureTarget(p.Row, x, 20f, false) != null, back = FindAdventureTarget(p.Row, -1f, x, false) != null;
                            if (front)
                            {
                                FireAdventurePea(p, p.Row, false, 20f);
                            }

                            if (back)
                            {
                                FireAdventurePea(p, p.Row, true, 20f);
                                FireAdventurePea(p, p.Row, true, 20f).X += 0.3f;
                            }

                            if (front || back)
                            {
                                p.ActionTimer = 1.425f;
                            }

                            break;
                    }
                }
                finally
                {
                    if (AdventureProducerGlow.HasNativeProductionHint(p.Kind))
                    {
                        AdventureProducerGlow.Tick(p, producerAdvanced, p.Kind == PlantKind.Sunflower && p.Timing.HasValue ? p.Timing.Value.Countdown : AdventureProducerGlow.CounterFromSeconds(p.ActionTimer), p.Kind == PlantKind.Marigold ? p.ActionCount > producerActions : Suns.Count > producerSunCount);
                    }
                }
            }
        }

        /// <summary>Performs fire adventure pea.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "back">The back.</param>
        /// <param name = "damage">The damage.</param>
        /// <returns>The resulting fire adventure pea.</returns>
        private Pea FireAdventurePea(Plant p, int row, bool back, float damage)
        {
            var shot = RentPea();
            shot.SourceKind = p.Kind;
            shot.SourceIsImitated = p.IsImitated;
            shot.Row = row;
            shot.FlightRow = row;
            shot.X = p.Column + (back ? 0.15f : 0.85f);
            shot.StartX = p.Column + 0.5f;
            shot.VelocityX = back ? -4.5f : 4.5f;
            shot.Damage = damage;
            shot.Backward = back;
            Peas.Add(shot);
            AdventurePresentationShot(p, shot);
            if (AdventurePuffShroomRules.IsShortRange(p.Kind))
            {
                shot.X = AdventurePuffShroomRules.LaunchCenterX(p.Kind, p.Column);
                shot.StartX = shot.X;
                shot.PresentationPreviousX = shot.X;
                shot.VelocityX = AdventurePuffShroomRules.GridSpeed;
                var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
                var plantY = 80f + p.Row * pitch + (Adventure.Level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f);
                shot.PresentationLaunchY = AdventurePuffShroomRules.LaunchCenterY(p.Kind, plantY,
                    HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column));
            }

            p.ShootAge = 0;
            Raise(GameEvent.Shoot);
            return shot;
        }

        /// <summary>Finds a short-range mushroom target using the original PC plant and zombie rectangles.</summary>
        /// <param name="plant">The mushroom acquiring a target.</param>
        /// <returns>The nearest eligible target, or null when its native attack interval is empty.</returns>
        private Zombie? FindAdventurePuffTarget(Plant plant)
        {
            Zombie? target = null;
            foreach (var zombie in Zombies)
            {
                if (AdventurePuffShroomRules.CanTarget(plant, zombie) && (target == null || zombie.X < target.X))
                {
                    target = zombie;
                }
            }

            return target;
        }

        /// <summary>Finds a Fume-shroom target using the original PC attack rectangle.</summary>
        /// <param name="plant">The Fume-shroom acquiring a target.</param>
        /// <returns>The nearest eligible target, or null when its attack rectangle is empty.</returns>
        private Zombie? FindAdventureFumeTarget(Plant plant)
        {
            Zombie? target = null;
            foreach (var zombie in Zombies)
            {
                if (AdventureFumeShroomRules.CanTarget(plant, zombie)
                    && (target == null || AdventureZombieTargetingRules.BodyRectangle(zombie).X < AdventureZombieTargetingRules.BodyRectangle(target).X))
                {
                    target = zombie;
                }
            }

            return target;
        }

        /// <summary>Finds a Starfruit target using the original five projectile paths.</summary>
        /// <param name="plant">The Starfruit acquiring a target.</param>
        /// <returns>The nearest eligible target, or null when no path intersects a target.</returns>
        private Zombie? FindAdventureStarfruitTarget(Plant plant)
        {
            var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            var plantY = 80f + plant.Row * pitch + (Adventure.Level.HasRoof ? Math.Max(0f, 5f - plant.Column) * 20f - 10f : 0f);
            Zombie? target = null;
            var targetLeft = float.MaxValue;
            foreach (var zombie in Zombies)
            {
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, plant.Kind) || zombie.Hypnotized
                    || zombie.Airborne || zombie.Submerged || zombie.DropRemaining > 0f
                    || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0)
                {
                    continue;
                }

                var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, zombie.IsSmall ? .5f : 1f);
                var bodyY = (int)AdventureBiteNativeY(zombie, nativeX);
                var body = AdventureZombieTargetingRules.BodyRectangle(zombie, bodyY);
                var velocity = zombie.Eating || zombie.FrozenRemaining > 0f || zombie.PresentationButter > 0f
                    ? 0f : zombie.PresentationNativeVelocity * (zombie.ChillRemaining > 0f ? .5f : 1f);
                if (zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 2))
                {
                    velocity = -velocity;
                }

                if (AdventureStarfruitRules.CanTarget(plant, zombie, body, (int)plantY, velocity, true)
                    && body.X < targetLeft)
                {
                    target = zombie;
                    targetLeft = body.X;
                }
            }

            return target;
        }

        /// <summary>Applies the original Fume-shroom row-area attack at the scheduled firing tick.</summary>
        /// <param name="plant">The Fume-shroom whose attack has reached its firing tick.</param>
        private void FireAdventureFume(Plant plant)
        {
            foreach (var zombie in new PooledSnapshot<Zombie>(Zombies))
            {
                if (AdventureFumeShroomRules.CanDamage(plant, zombie))
                {
                    HitAdventureZombie(zombie, 20f, true, 2U);
                }
            }

            plant._presentationPrefiring = false;
            Raise(GameEvent.Shoot);
        }

        /// <summary>Creates a projectile when a native body shooting countdown reaches its firing tick.</summary>
        /// <param name="plant">The plant with a pending attack.</param>
        private void FireAdventureNativeTimedProjectile(Plant plant)
        {
            var lob = plant.Kind == PlantKind.CabbagePult || plant.Kind == PlantKind.KernelPult
                || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon;
            var melon = plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon;
            var shot = FireAdventurePea(plant, plant.Row, false, melon ? 80f : plant.Kind == PlantKind.CabbagePult ? 40f : 20f);
            shot.HitsAir = plant.Kind == PlantKind.Cactus;
            shot.Range = 20f;
            shot.Lobbed = lob;
            shot.PiercesShield = lob;
            shot.Splash = melon;
            if (plant.Kind == PlantKind.WinterMelon)
            {
                shot.IsSnow = true;
                shot.ChillSplash = true;
            }

            if (plant.Kind == PlantKind.KernelPult)
            {
                shot.Butter = plant._kernelWeaponPrepared && plant.PresentationButter;
                plant._kernelWeaponPrepared = false;
                if (shot.Butter)
                {
                    shot.Damage = 40f;
                }

                plant.PresentationShotAge = .29f;
            }

            if (!lob && Adventure.Level.HasRoof && plant.Column < 5)
            {
                shot.Range = Math.Max(.1f, 5 - plant.Column - .5f);
            }
        }

        /// <summary>Fires the five original Starfruit projectiles at the scheduled tick.</summary>
        /// <param name="plant">The Starfruit whose body shooting counter has expired.</param>
        private void FireAdventureStarfruit(Plant plant)
        {
            var rowPitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            for (var index = 0; index < 5; index++)
            {
                var star = FireAdventurePea(plant, plant.Row, false, 20f);
                star.X = plant.Column + 25f / 80f;
                star.StartX = star.X;
                star.PresentationPreviousX = star.X;
                star.VelocityX = AdventureStarfruitRules.VelocityX(index) * 100f / 80f;
                star.VelocityY = AdventureStarfruitRules.VelocityY(index) * 100f / rowPitch;
                star.Backward = star.VelocityX < 0f;
                star.Range = float.MaxValue;
            }

            plant._presentationPrefiring = false;
        }

        /// <summary>Performs find adventure target.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "min">The min.</param>
        /// <param name = "max">The max.</param>
        /// <param name = "air">The air.</param>
        /// <param name = "submerged">The submerged.</param>
        /// <param name = "sourceKind">The source kind.</param>
        /// <returns>The resulting find adventure target.</returns>
        private Zombie? FindAdventureTarget(int row, float min, float max, bool air, bool submerged = false, PlantKind sourceKind = PlantKind.Peashooter)
        {
            Zombie? best = null;
            foreach (Zombie z in Zombies)
            {
                if (!z.Hypnotized && AdventureZombieTargetingRules.CanBeAutoTargeted(z, sourceKind) && z.DropRemaining <= 0f && z.Kind != ZombieKind.Bungee && z.Row == row && z.X >= min && z.X <= max && (!z.Airborne || air) && (!z.Submerged || submerged))
                {
                    if (best == null || z.X < best.X)
                    {
                        best = z;
                    }
                }
            }

            return best;
        }

        /// <summary>Performs can shoot adventure boss.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting can shoot adventure boss.</returns>
        private bool CanShootAdventureBoss(Plant p)
        {
            return Adventure.Level.Mode == AdventureMode.Boss && Adventure.BossHeadVulnerable && p.Row >= 1 && p.Row <= 3 && Adventure.BossHealth > 0;
        }

        /// <summary>Performs blast adventure.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "rows">The rows.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "burned">The burned.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        private void BlastAdventure(float x, int row, float width, int rows, float damage, bool burned = false, uint damageFlags = 0)
        {
            foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
            {
                if (!z.Hypnotized && Math.Abs(z.Row - row) <= rows && Math.Abs(z.X - x) <= width)
                {
                    HitAdventureExplosionZombie(z, damage, burned, damageFlags);
                }
            }
        }

        /// <summary>Performs advance adventure projectiles.</summary>
        /// <param name = "dt">The dt.</param>
        /// <param name="allowDamage">Whether live combat targets can receive hits before the level has completed.</param>
        private void AdvanceAdventureProjectiles(float dt, bool allowDamage = true)
        {
            for (var i = Peas.Count - 1; i >= 0; i--)
            {
                var p = Peas[i];
                UpdateAdventureHomingVelocity(p, dt);
                var before = p.X;
                p.X += p.VelocityX * dt;
                p.FlightRow += p.VelocityY * dt;
                AdvanceAdventureProjectilePresentation(p, dt);
                if (AdventurePuffShroomRules.IsShortRange(p.SourceKind) && AdventurePuffShroomRules.Expired(p.PresentationNativeAge))
                {
                    RemovePeaAt(i);
                    continue;
                }

                p.Row = (int)Math.Round(p.FlightRow);
                if (!p.Lobbed && !p.Homing && p.VelocityY == 0 && !AdventurePuffShroomRules.IsShortRange(p.SourceKind))
                {
                    Plant? torch = null;
                    foreach (var plant in Plants)
                    {
                        if (plant.Kind == PlantKind.Torchwood && plant.Row == p.Row && plant.Column != p.LastTorchColumn && Math.Abs(p.X - (plant.Column + 0.5f)) < 0.2f)
                        {
                            torch = plant;
                            break;
                        }
                    }
                    if (torch != null)
                    {
                        p.LastTorchColumn = torch.Column;
                        if (p.IsSnow)
                        {
                            p.IsSnow = false;
                        }
                        else
                        {
                            p.Fire = true;
                            p.Damage = 40;
                            p.Splash = true;
                        }
                    }
                }

                UpdateAdventureSnowTrail(p);
                Zombie? hit = null;
                foreach (Zombie z in Zombies)
                {
                    if (allowDamage && p.PresentationCanImpact && z.GraveRiseTicks <= 0 && (!p.Homing || z.Id == p.TargetZombieId) && !z.Hypnotized && Math.Abs(z.Row - p.FlightRow) < 0.45f && !p.HitIds.Contains(z.Id) && (!z.Airborne || p.HitsAir) && (!z.Submerged || p.Lobbed) && (AdventurePuffShroomRules.IsShortRange(p.SourceKind)
                        ? AdventurePuffShroomRules.Collides(p, z)
                        : z.X >= Math.Min(before, p.X) - 0.25f && z.X <= Math.Max(before, p.X) + 0.25f))
                    {
                        if (hit == null || Math.Abs(z.X - before) < Math.Abs(hit.X - before))
                        {
                            hit = z;
                        }
                    }
                }

                var remove = false;
                if (hit != null)
                {
                    AdventurePresentationImpact(p, hit);
                    p.HitIds.Add(hit.Id);
                    if (p.HitsAir && hit.Airborne)
                    {
                        hit.Airborne = false;
                        hit.Speed = 0.18f;
                    }

                    ApplyAdventureProjectileCold(p, hit);
                    if (p.Butter)
                    {
                        hit.FrozenRemaining = Math.Max(hit.FrozenRemaining, 4f);
                    }

                    HitAdventureZombie(hit, p.Damage, p.PiercesShield);
                    if (p.Splash)
                    {
                        foreach (Zombie other in new PooledSnapshot<Zombie>(Zombies))
                        {
                            if (other != hit && !other.Hypnotized && !other.Airborne && Math.Abs(other.Row - hit.Row) <= (p.Fire ? 0 : 1) && Math.Abs(other.X - hit.X) <= (p.Fire ? 0.5f : 1f))
                            {
                                if (p.ChillSplash)
                                {
                                    ApplyAdventureChill(other);
                                }

                                HitAdventureZombie(other, p.Fire ? 20f : 26f, p.PiercesShield);
                            }
                        }
                    }

                    remove = !p.PiercesZombies;
                }

                if (allowDamage && !remove && p.X >= 8.25f && p.Lobbed && p.PresentationCanImpact && Adventure.BossHeadVulnerable && p.Row >= 1 && p.Row <= 3)
                {
                    AdventurePresentationImpact(p, null);
                    DamageAdventureBoss(p.Damage);
                    remove = true;
                }

                if (remove || p.X > 12f || p.X < -2f || p.FlightRow < -1f || p.FlightRow > RowCount || Math.Abs(p.X - p.StartX) > p.Range)
                {
                    RemovePeaAt(i);
                }
            }
        }

        /// <summary>Performs hit adventure zombie.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "bypassShield">The bypass shield.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        private void HitAdventureZombie(Zombie z, float damage, bool bypassShield = false, uint damageFlags = 0)
        {
            if (!Zombies.Contains(z) || z.GraveRiseTicks > 0)
            {
                return;
            }

            AdventurePresentationZombieHit(z, damage, bypassShield, damageFlags);
            // PC TakeDamage bit 1 damages the held shield and also sends the full
            // original damage to the helmet/body; ordinary piercing skips the shield.
            if (!bypassShield || (damageFlags & 2U) != 0)
            {
                var shield = Math.Min(z.ShieldHealth, damage);
                z.ShieldHealth -= shield;
                if ((damageFlags & 2U) == 0)
                {
                    damage -= shield;
                }
            }

            var armor = Math.Min(z.ArmorHealth, damage);
            z.ArmorHealth -= armor;
            z.Health -= damage - armor;
            AdventurePresentationZombieEquipmentDamaged(z, damageFlags);
            AdventurePresentationZombieBodyDamaged(z, damageFlags);
            Raise(GameEvent.ZombieHit);
            if (z.Health <= 0)
            {
                z.Health = 0f;
                KillAdventureZombie(z, damageFlags);
            }
        }

        /// <summary>Performs kill adventure zombie.</summary>
        /// <param name = "z">The z.</param>
        /// <param name="damageFlags">The original damage flags controlling whether death animation is permitted.</param>
        private void KillAdventureZombie(Zombie z, uint damageFlags = 0)
        {
            if (!Zombies.Remove(z))
            {
                return;
            }

            if (ModeSession == null)
            {
                RememberAdventureRewardPosition(z);
            }

            AdventurePresentationZombieDeath(z, damageFlags);
            if (z.PresentationDeath != AdventureZombieDeathPresentation.Removed)
            {
                DropAdventureLoot(z);
            }

            Raise(GameEvent.ZombieDied);
        }

        /// <summary>Performs initialize adventure potato mine.</summary>
        /// <param name = "p">The p.</param>
        private void InitializeAdventurePotatoMine(Plant p)
        {
            if (p.PotatoMinePresentation != null)
            {
                return;
            }

            p.PotatoMinePresentation = new global::PlantsVsZombies.Adventure.AdventurePotatoMineState();
            p._presentationKind = PlantKind.PotatoMine;
            p.PresentationRate = 12f;
            p.ActionTimer = 15f;
        }

        /// <summary>Advances the original chomper state machine once per native simulation tick.</summary>
        /// <param name="plant">The live chomper being updated.</param>
        /// <returns>True because the native state machine owns this plant's attack timing.</returns>
        private bool AdvanceAdventureChomper(Plant plant)
        {
            var state = plant.ChomperPresentation;
            if (state == null)
            {
                state = new AdventureChomperState();
                plant.ChomperPresentation = state;
            }

            state.PhaseTicks++;
            if (state.Countdown > 0)
            {
                state.Countdown--;
            }

            // PC 0x46F6E0: bite at 24 fps, commit the meal after 70 ticks,
            // chew for 4000 ticks at 15 fps, then swallow at 12 fps.
            switch (state.NativePhase)
            {
                case 1:
                    if (FindAdventureChomperTarget(plant, false) != null)
                    {
                        state.NativePhase = 10;
                        state.Animation = "anim_bite";
                        state.Rate = 24f;
                        state.PhaseTicks = 0;
                        state.Countdown = 70;
                    }

                    break;
                case 10:
                    if (state.Countdown != 0)
                    {
                        break;
                    }

                    RequestAdventureZombieFoley(61);
                    var target = FindAdventureChomperTarget(plant, true);
                    var phase = target == null ? -1 : AdventureZombieFeedbackPhase(target);
                    if (target != null && (target.Kind == ZombieKind.Gargantuar || target.Kind == ZombieKind.Boss))
                    {
                        RequestAdventureZombieFoley(1);
                        HitAdventureZombie(target, 40f, true);
                        state.NativePhase = 12;
                    }
                    else if (target == null || target.FrozenRemaining <= 0 && target.PresentationButter <= 0 && (phase >= 20 && phase <= 28 || phase == 11 || phase == 12))
                    {
                        state.NativePhase = 12;
                    }
                    else
                    {
                        target.PresentationDeath = AdventureZombieDeathPresentation.Eaten;
                        KillAdventureZombie(target);
                        state.NativePhase = 11;
                    }

                    break;
                case 11:
                case 12:
                    if (state.PhaseTicks * 24 >= 2500)
                    {
                        if (state.NativePhase == 11)
                        {
                            state.NativePhase = 13;
                            state.Animation = "anim_chew";
                            state.Rate = 15f;
                            state.Countdown = 4000;
                        }
                        else
                        {
                            state.NativePhase = 1;
                            state.Animation = "anim_idle";
                            state.Rate = 0f;
                        }

                        state.PhaseTicks = 0;
                    }

                    break;
                case 13:
                    if (state.Countdown == 0)
                    {
                        state.NativePhase = 14;
                        state.Animation = "anim_swallow";
                        state.Rate = 12f;
                        state.PhaseTicks = 0;
                    }

                    break;
                case 14:
                    if (state.PhaseTicks * 12 >= 2800)
                    {
                        state.NativePhase = 1;
                        state.Animation = "anim_idle";
                        state.Rate = 0f;
                        state.PhaseTicks = 0;
                    }

                    break;
            }

            plant.PresentationBiteNativeState = state.NativePhase;
            plant.ActionTimer = state.Countdown * .01f;
            return true;
        }

        /// <summary>Finds the headed zombie overlapping the original chomper attack interval.</summary>
        /// <param name="plant">The chomper performing the native target check.</param>
        /// <param name="biting">Whether the active bite permits the original sixty-pixel tolerance.</param>
        /// <returns>The leftmost eligible zombie, or null when the bite has no target.</returns>
        private Zombie? FindAdventureChomperTarget(Plant plant, bool biting)
        {
            Zombie? best = null;
            var left = 40 + plant.Column * 80 + 80;
            var right = left + 40;
            foreach (var zombie in Zombies)
            {
                var phase = AdventureZombieFeedbackPhase(zombie);
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, PlantKind.Chomper) || zombie.Hypnotized || zombie.Row != plant.Row || zombie.Airborne || zombie.Submerged || zombie.DropRemaining > 0 || zombie.Kind == ZombieKind.Bungee || phase == 20 || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0)
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                var attackLeft = left + (phase == 37 ? 20 : 0);
                var overlap = Math.Min(right, body.X + body.Width) - Math.Max(attackLeft, body.X);
                if (overlap < (biting || zombie.Eating ? -60 : 0))
                {
                    continue;
                }

                if (best == null || body.X < AdventureZombieTargetingRules.BodyRectangle(best).X)
                {
                    best = zombie;
                }
            }

            return best;
        }

        /// <summary>Updates defensive body animation without changing its phase when chewing slows it.</summary>
        /// <param name="plant">The plant whose body and damage presentation is updated.</param>
        /// <param name="delta">Elapsed native simulation time.</param>
        private void AdvanceAdventureDefensivePlantPresentation(Plant plant, float delta)
        {
            if (AdventureDefensivePlantRules.DamageTrack(plant.Kind) == null)
            {
                return;
            }

            var stage = AdventureDefensivePlantRules.DamageStage(plant.Kind, plant.Health);
            if (plant.Kind != PlantKind.Pumpkin && stage > 0 && stage != plant._presentationDefensiveDamageStage)
            {
                Presentation.PlantBiteEffects.Add(new AdventurePlantBiteEffect
                {
                    Id = ++Presentation._effectId,
                    ParticleId = 36,
                    Row = plant.Row,
                    NativeX = 40 + plant.Column * 80 + 40,
                    NativeY = 80 + plant.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85 : 100) + (Adventure.Level.HasRoof ? Math.Max(0f, 5f - plant.Column) * 20f - 10f : 0f) + (plant.Kind == PlantKind.TallNut ? -22 : 10),
                    Seed = plant.Id * 397 ^ Presentation._effectId
                });
            }

            plant._presentationDefensiveDamageStage = stage;
            if (plant.Kind == PlantKind.Pumpkin)
            {
                return;
            }

            var chewing = plant.PresentationEatenFlashTicks > 0;
            if (!chewing && plant._presentationDefensiveChewing)
            {
                plant.PresentationRate = 10f + (float)_random.NextDouble() * 5f;
            }

            plant._presentationDefensiveChewing = chewing;
            plant.PresentationBodyRate = chewing ? .1f : plant.PresentationRate;
            plant.PresentationBodyFrames += delta * plant.PresentationBodyRate;
        }

        /// <summary>Performs advance adventure potato mine.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting advance adventure potato mine.</returns>
        private bool AdvanceAdventurePotatoMine(Plant p)
        {
            InitializeAdventurePotatoMine(p);
            var state = p.RequiredPotatoMinePresentation;
            switch (p.MinePhase)
            {
                case MinePhase.Buried:
                    if (state.Countdown > 0)
                    {
                        state.Countdown--;
                    }

                    p.ActionTimer = state.Countdown * .01f;
                    if (state.Countdown == 0)
                    {
                        p.MinePhase = MinePhase.Rising;
                        p.PhaseAge = 0;
                        state.PhaseTicks = 0;
                        state.BodyRate = 18f;
                        p.PresentationRate = 18f;
                        AdventurePresentationPotatoMineRise(p);
                        Presentation.PotatoMineRiseSoundSerial++;
                    }

                    break;
                case MinePhase.Rising:
                    state.PhaseTicks++;
                    // The local reanim's anim_rise is 19 frames at native 18 fps.
                    if (state.PhaseTicks * 18 >= 1900)
                    {
                        p.MinePhase = MinePhase.Armed;
                        p.PhaseAge = 0;
                        state.PhaseTicks = 0;
                        state.BodyRate = 12f + AdventureExplosionRandomFloat(3f);
                        p.PresentationRate = state.BodyRate;
                        state.LightCreated = true;
                        state.GlowFrames = 10;
                        state.GlowTime = 0;
                        p.ActionTimer = 0;
                        _random.Next(400); // PC initializes blink countdown as 400+Rand(400).
                    }

                    break;
                case MinePhase.Armed:
                    state.PhaseTicks++;
                    bool touching;
                    var gap = AdventurePotatoMineDistance(p, out touching);
                    state.NearestDistancePixels = gap;
                    if (touching)
                    {
                        var x = p.Column + .5f;
                        DamageAdventurePotatoMine(p);
                        AdventurePresentationExplosion(PlantKind.PotatoMine, x, p.Row);
                        RemoveAdventurePlantAndStack(p);
                        Raise(GameEvent.MineExplosion);
                        return true;
                    }

                    if (state.LightCreated)
                    {
                        // PC 46E405..46E423 -> curve(200,50,gap,10,3,Linear), rounded.
                        var t = Math.Max(0f, Math.Min(1f, (gap - 200f) / -150f));
                        state.GlowFrames = (int)(10f - 7f * t + .5f);
                        state.GlowTime += .01f * (state.BodyRate - 2f) / state.GlowFrames;
                        state.GlowTime -= (float)Math.Floor(state.GlowTime);
                    }

                    break;
            }

            return true;
        }

        /// <summary>Applies the original mine's sixty-pixel blast to eligible same-row body rectangles.</summary>
        /// <param name="plant">The armed mine whose native detonation center is used.</param>
        private void DamageAdventurePotatoMine(Plant plant)
        {
            // PC 0x474EE0 / 0x4294C0 / 0x428460: center is (mX+20,mY+40),
            // radius is 60, row range is zero, and the damage target mask is 0x4D.
            var centerX = 40 + plant.Column * 80 + 20;
            var centerY = 80 + plant.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85 : 100)
                + (Adventure.Level.HasRoof ? (int)(Math.Max(0f, 5f - plant.Column) * 20f - 10f) : 0) + 40;
            foreach (var zombie in new PooledSnapshot<Zombie>(Zombies))
            {
                if (zombie.Row != plant.Row || zombie.Hypnotized || zombie.Airborne || zombie.DropRemaining > 0
                    || zombie.Kind == ZombieKind.Bungee || zombie.Kind == ZombieKind.Boss || zombie.PresentationNativeHeight == 9
                    || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie))
                {
                    continue;
                }

                var phase = AdventureZombieFeedbackPhase(zombie);
                if (phase == 12 || phase == 71 || phase == 33 || phase == 34 || phase == 35 || phase == 52
                    || phase == 54 || phase == 58 || phase == 74 || phase == 14 || phase == 19 || phase == 50)
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, zombie.IsSmall ? .5f : 1f);
                var top = (int)AdventureBiteNativeY(zombie, nativeX) + body.Y + zombie.PresentationBiteBodyY;
                var height = Math.Max(0, body.Height - (zombie.PresentationBiteClipHeight > -100 ? zombie.PresentationBiteClipHeight : 0));
                var dx = Math.Max(0, Math.Max(body.X - centerX, centerX - body.X - body.Width));
                var dy = Math.Max(0, Math.Max(top - centerY, centerY - top - height));
                if (dx * dx + dy * dy <= 60 * 60)
                {
                    HitAdventureExplosionZombie(zombie, 1800f, false, 0x12);
                }
            }
        }

        /// <summary>Performs adventure potato mine distance.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "touching">The touching.</param>
        /// <returns>The resulting adventure potato mine distance.</returns>
        private int AdventurePotatoMineDistance(Plant p, out bool touching)
        {
            touching = false;
            // PC 475D90 measures the light's distance. The separate 475AA0 trigger
            // admits edge contact and a 30-pixel gap for a zombie already eating.
            var left = 40 + p.Column * 80;
            var right = left + 55;
            var closest = 1000;
            foreach (var z in Zombies)
            {
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(z) || z.Hypnotized || z.Airborne || z.Kind == ZombieKind.Bungee || z.Kind == ZombieKind.Boss || z.Row != p.Row || z.DropRemaining > 0 || z.PresentationNativeHeight == 9 || z.Kind == ZombieKind.Digger && z.AbilityPhase == 0)
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(z);
                var overlap = Math.Min(right, body.X + body.Width) - Math.Max(left, body.X);
                var phase = AdventureZombieFeedbackPhase(z);
                var triggerLeft = left + (z.Kind == ZombieKind.PoleVaulter ? 40 : 0);
                var triggerOverlap = Math.Min(right, body.X + body.Width) - Math.Max(triggerLeft, body.X);
                if (!z.Headless && phase != 11 && phase != 12 && !(z.Kind == ZombieKind.Pogo && !z.UsedAbility) && triggerOverlap >= (z.Eating ? -30 : 0))
                {
                    touching = true;
                }

                var gap = Math.Max(0, -overlap);
                if (gap < closest)
                {
                    closest = gap;
                }
            }

            return closest;
        }

        /// <summary>Gets or sets adventure presentation remainder.</summary>
        public float AdventurePresentationRemainder
        {
            get
            {
                return State == MatchState.Playing && IsAdventure && Adventure.Phase == global::PlantsVsZombies.Adventure.AdventurePhase.Playing ? (float)Math.Max(0d, Math.Min(SimulationStep, ModeSession == null ? _accumulatedTime : _modeAccumulated)) : 0f;
            }
        }

        /// <summary>Gets or sets presentation.</summary>
        public global::PlantsVsZombies.Adventure.AdventurePresentationState Presentation { get; private set; } = new global::PlantsVsZombies.Adventure.AdventurePresentationState();

        /// <summary>Performs adventure presentation tick.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdventurePresentationTick(float dt)
        {
            for (var i = Presentation.ZombieRiseEffects.Count - 1; i >= 0; i--)
            {
                var effect = Presentation.ZombieRiseEffects[i];
                effect.Age += dt;
                if (effect.Age >= 2f)
                {
                    Presentation.ZombieRiseEffects.RemoveAt(i);
                }
            }

            AdvanceAdventureGraves();
            AdvanceAdventureExplosionPresentation();
            AdvanceAdventureIcePresentation(dt);
            AdvanceAdventureZombieFeedback(dt);
            for (var i = Presentation.Corpses.Count - 1; i >= 0; i--)
            {
                var corpse = Presentation.Corpses[i];
                corpse.Age += dt;
                AdvanceAdventureZombieCorpseSound(corpse);
                corpse.RequiredZombie.PresentationFlash = Math.Max(0, corpse.RequiredZombie.PresentationFlash - dt);
                corpse.RequiredZombie.PresentationArmorFlash = Math.Max(0, corpse.RequiredZombie.PresentationArmorFlash - dt);
                if (corpse.Age >= corpse.Duration)
                {
                    Presentation.Corpses.RemoveAt(i);
                }
            }

            for (var i = Presentation.Impacts.Count - 1; i >= 0; i--)
            {
                Presentation.Impacts[i].Age += dt;
                if (Presentation.Impacts[i].Age > 3f)
                {
                    Presentation.Impacts.RemoveAt(i);
                }
            }

            foreach (Mower mower in Mowers)
            {
                float time;
                Presentation.MowerTimes.TryGetValue(mower, out time);
                Presentation.MowerTimes[mower] = mower.Active ? time + dt : 0f;
            }

            foreach (var nut in Adventure.BowlingNuts)
            {
                global::PlantsVsZombies.Adventure.AdventureBowlingPresentation? pose;
                if (!Presentation.Bowling.TryGetValue(nut, out pose))
                {
                    var giant = ModeSession != null && ModeSession._giantNuts.Contains(nut);
                    pose = new global::PlantsVsZombies.Adventure.AdventureBowlingPresentation
                    {
                        Rate = (giant ? 6f : 12f) + (float)_random.NextDouble() * (giant ? 4f : 6f)
                    };
                    Presentation.Bowling.Add(nut, pose);
                }

                if (!nut.NativeMotionInitialized)
                {
                    pose.Age += dt;
                }
            }

            foreach (Pea shot in Peas)
            {
                shot.PresentationAge += dt;
                shot.PresentationPreviousX = shot.X;
                shot.PresentationPreviousRow = shot.FlightRow;
                shot.PresentationPreviousLobHeight = shot.PresentationLobHeight;
            }

            foreach (Zombie zombie in Zombies)
            {
                zombie.PresentationPreviousX = zombie.X;
                if (!zombie._presentationInitialized)
                {
                    zombie._presentationInitialized = true;
                    zombie.PresentationWalk2 = zombie.Kind == ZombieKind.Flag || _random.Next(2) == 0;
                }

                AdventurePresentationZombieAnimation(zombie);
                zombie.PresentationFlash = Math.Max(0, zombie.PresentationFlash - dt);
                zombie.PresentationArmorFlash = Math.Max(0, zombie.PresentationArmorFlash - dt);
                zombie.PresentationButter = Math.Max(0, zombie.PresentationButter - dt);
                AdvanceAdventureZombieClock(zombie, dt);
            }

            foreach (Plant plant in Plants)
            {
                if (plant._presentationKind != plant.Kind)
                {
                    plant._presentationKind = plant.Kind;
                    var minimum = global::PlantsVsZombies.Adventure.AdventurePresentationRules.AttachedHead(plant.Kind) ? 15f : plant.Kind == PlantKind.CherryBomb || plant.Kind == PlantKind.Imitater ? 25f : 10f;
                    plant.PresentationRate = minimum + (float)_random.NextDouble() * 5f;
                }

                if (plant.Asleep)
                {
                    continue;
                }

                AdvanceAdventureDefensivePlantPresentation(plant, dt);
                plant.PresentationAwakeAge += dt;
                if (plant.PresentationShotAge >= 0)
                {
                    plant.PresentationShotAge += dt;
                }

                for (var head = 0; head < 3; head++)
                {
                    if (plant.PresentationHeadAge[head] >= 0)
                    {
                        plant.PresentationHeadAge[head] += dt;
                    }
                }

                if (plant.Kind == PlantKind.GatlingPea || plant.Kind == PlantKind.GloomShroom || plant.Kind == PlantKind.Cattail)
                {
                    if (plant.UpgradeBurstTicks > 0)
                    {
                        var elapsed = ((plant.Kind == PlantKind.GloomShroom ? 200 : plant.Kind == PlantKind.GatlingPea ? 100 : 50) - plant.UpgradeBurstTicks) * .01f;
                        plant.PresentationShotAge = elapsed;
                        plant.PresentationHeadAge[0] = elapsed;
                    }

                    continue;
                }

                if (plant.MultiHeadTiming.HasValue)
                {
                    continue;
                }

                if (plant.Kind == PlantKind.Starfruit && plant.StarfruitLaunchCountdown >= 0)
                {
                    continue;
                }

                if (plant.Timing.HasValue && (plant.Kind == PlantKind.Peashooter || plant.Kind == PlantKind.SnowPea || plant.Kind == PlantKind.Repeater || plant.Kind == PlantKind.Sunflower
                    || plant.Kind == PlantKind.FumeShroom || plant.Kind == PlantKind.ScaredyShroom || plant.Kind == PlantKind.Cactus
                    || plant.Kind == PlantKind.CabbagePult || plant.Kind == PlantKind.KernelPult || plant.Kind == PlantKind.MelonPult
                    || plant.Kind == PlantKind.WinterMelon || AdventurePuffShroomRules.IsShortRange(plant.Kind)))
                {
                    continue;
                }

                var lead = global::PlantsVsZombies.Adventure.AdventurePresentationRules.LaunchAge(plant.Kind);
                if (plant.ActionTimer > 0 && plant.ActionTimer <= lead && !plant._presentationPrefiring && CanPresentAdventureWindup(plant))
                {
                    plant._presentationPrefiring = true;
                    plant.PresentationShotAge = Math.Max(0, lead - plant.ActionTimer);
                    for (var h = 0; h < 3; h++)
                    {
                        plant.PresentationHeadAge[h] = plant.PresentationShotAge;
                    }
                }
            }
        }

        /// <summary>Checks whether the plant's next adventure attack can launch a projectile.</summary>
        /// <param name = "plant">The plant whose attack animation may begin.</param>
        /// <returns>Whether the current firing rules admit an attack.</returns>
        private bool CanPresentAdventureWindup(Plant plant)
        {
            if (AdventurePuffShroomRules.IsShortRange(plant.Kind))
            {
                // PC Plant::FindTargetZombie 0x475AA0 gates the body shooting animation with its attack rectangle.
                return FindAdventurePuffTarget(plant) != null;
            }

            var x = plant.Column + 0.5f;
            switch (plant.Kind)
            {
                case PlantKind.Peashooter:
                case PlantKind.SnowPea:
                case PlantKind.Repeater:
                    return plant.ModeFacesLeft
                        ? FindAdventureTarget(plant.Row, -1f, x, false) != null
                        : FindAdventureTarget(plant.Row, plant.Column + 0.4f, 20f, false) != null || CanShootAdventureBoss(plant);
                case PlantKind.Threepeater:
                    for (var row = Math.Max(0, plant.Row - 1); row <= Math.Min(RowCount - 1, plant.Row + 1); row++)
                    {
                        if (FindAdventureTarget(row, x - 0.1f, 20f, false) != null)
                        {
                            return true;
                        }
                    }

                    return false;
                case PlantKind.SplitPea:
                    return FindAdventureTarget(plant.Row, x, 20f, false) != null
                        || FindAdventureTarget(plant.Row, -1f, x, false) != null;
                case PlantKind.Starfruit:
                    return FindAdventureStarfruitTarget(plant) != null;
                default:
                    return false;
            }
        }

        /// <summary>Performs adventure presentation shot.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "shot">The shot.</param>
        private void AdventurePresentationShot(Plant plant, Pea shot)
        {
            shot.PresentationInitialized = true;
            shot.PresentationPlantId = plant.Id;
            shot.PresentationLaunchRow = plant.Row;
            shot.PresentationLaunchColumn = plant.Column;
            shot.PresentationPreviousX = shot.X;
            shot.PresentationPreviousRow = shot.FlightRow;
            plant._presentationPrefiring = false;
            var age = global::PlantsVsZombies.Adventure.AdventurePresentationRules.LaunchAge(plant.Kind);
            if (plant.Kind == PlantKind.GatlingPea || plant.Kind == PlantKind.Cattail)
            {
                age = Math.Max(0, ((plant.Kind == PlantKind.GatlingPea ? 100 : 50) - plant.UpgradeBurstTicks) * .01f);
            }

            plant.PresentationShotAge = age;
            var head = plant.Kind == PlantKind.Threepeater ? Math.Max(0, Math.Min(2, 1 - (shot.Row - plant.Row))) : plant.Kind == PlantKind.SplitPea && shot.Backward ? 1 : 0;
            plant.PresentationHeadAge[head] = age;
            RequestAdventureIceLaunch(plant, shot);
        }

        /// <summary>Performs adventure presentation zombie hit.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "armor">The armor.</param>
        private void AdventurePresentationZombieHit(Zombie zombie, bool armor)
        {
            // PC body/armor shot counter 25; additive channel = counter * 10 (0..255).
            zombie.PresentationHealthBeforeHit = zombie.Health;
            if (armor && global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShieldType(zombie) != 0)
            {
                zombie.PresentationArmorFlash = .25f;
                zombie.PresentationShieldRecoil = .12f;
            }
            else
            {
                zombie.PresentationFlash = .25f;
            }
        }

        /// <summary>Performs adventure presentation impact.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "zombie">The zombie.</param>
        private void AdventurePresentationImpact(Pea shot, Zombie? zombie)
        {
            RequestAdventureProjectileImpactSound(shot, zombie);
            if (shot.Butter && zombie != null)
            {
                zombie.PresentationButter = Math.Max(zombie.PresentationButter, 4f);
            }

            var effect = new global::PlantsVsZombies.Adventure.AdventureImpact
            {
                Id = ++Presentation._effectId,
                Seed = _random.Next(),
                Kind = shot.SourceKind,
                X = zombie == null ? shot.X : zombie.X,
                Row = shot.FlightRow,
                Snow = shot.IsSnow,
                Fire = shot.Fire,
                Butter = shot.Butter
            };
            RecordAdventureIceImpact(shot, zombie, effect);
            Presentation.Impacts.Add(effect);
        }

        /// <summary>Performs adventure presentation zombie death.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name="damageFlags">The original lethal hit flags passed to Zombie::DieWithLoot.</param>
        private void AdventurePresentationZombieDeath(Zombie zombie, uint damageFlags = 0)
        {
            if (zombie.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Mowed)
            {
                AdventurePresentationZombieMowedParts(zombie);
                Presentation.Impacts.Add(new global::PlantsVsZombies.Adventure.AdventureImpact {
                    Id = ++Presentation._effectId,
                    Seed = _random.Next(),
                    X = zombie.X,
                    Row = zombie.Row,
                    Mowed = true
                });
                return;
            }

            if (zombie.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Eaten || zombie.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Removed || zombie.BungeeCarrier && zombie.PresentationDeath != global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned)
            {
                return;
            }

            if (zombie.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned && CreateAdventureBurnPresentation(zombie))
            {
                return;
            }

            // PC 0x54F0F0..0x54F107 sends no-body-part deaths straight to Die.
            // Gargantuars and the boss explicitly retain their own death animations.
            var kind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
            if ((damageFlags & 16U) != 0 && kind != 23 && kind != 32 && kind != 25)
            {
                return;
            }

            var rate = zombie.Kind == ZombieKind.Football ? 24f : zombie.Kind == ZombieKind.Gargantuar || zombie.Kind == ZombieKind.Snorkel || zombie.Kind == ZombieKind.Yeti ? 14f : zombie.Kind == ZombieKind.Digger || zombie.Kind == ZombieKind.Boss ? 18f : 24f + (float)_random.NextDouble() * 6f;
            var roll = _random.Next(100);
            var clip = Adventure.Level.IsWaterRow(zombie.Row) ? "anim_waterdeath" : roll == 99 && Adventure.Level.Number > 5 && zombie.ChillRemaining <= 0 && Zombies.Count <= 5 ? "anim_superlongdeath" : roll > 50 ? "anim_death2" : "anim_death";
            if (clip == "anim_superlongdeath")
            {
                rate = 14f;
            }

            Presentation.Corpses.Add(InitializeAdventureZombieCorpse(new global::PlantsVsZombies.Adventure.AdventureZombieCorpse { Zombie = zombie, Animation = clip, Rate = rate }));
        }

        /// <summary>Performs create adventure burn presentation.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <returns>The resulting create adventure burn presentation.</returns>
        private bool CreateAdventureBurnPresentation(Zombie zombie)
        {
            var kind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
            var phase = zombie.PresentationNativePhase;
            if (phase < 0)
            {
                phase = 0;
                if (zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase < 2)
                {
                    phase = zombie.AbilityPhase == 0 ? 33 : 35;
                }
                else if (zombie.Kind == ZombieKind.PoleVaulter && zombie.UsedAbility && zombie.AbilityTimer > 0 && zombie.AbilityTimer <= .75f)
                {
                    phase = 12;
                }
                else if (zombie.Kind == ZombieKind.DolphinRider && zombie.UsedAbility && zombie.AbilityTimer > 0 && zombie.AbilityTimer <= .75f)
                {
                    phase = 54;
                }
            }

            var inPool = zombie.PresentationInPool ?? (Adventure.Level.IsWaterRow(zombie.Row) && zombie.X >= 0 && zombie.X < 9);
            var backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase >= 1 && phase != 38);
            var hasSled = zombie.PresentationHasSled ?? (zombie.Kind == ZombieKind.Bobsled && !zombie.UsedAbility);
            var scale = zombie.IsSmall ? .5f : 1f;
            var pose = global::PlantsVsZombies.Adventure.AdventureBurnPresentationRules.Resolve(kind, phase, inPool, zombie.Airborne, !zombie.Headless, hasSled, backwards, !zombie.ThrownImp, scale, zombie.PresentationHealthBeforeHit);
            // PC removes ice/butter and stops hit coloration before choosing the burn visual.
            zombie.FrozenRemaining = 0;
            zombie.PresentationButter = 0;
            zombie.PresentationFlash = 0;
            zombie.PresentationArmorFlash = 0;
            if (pose.Visual == global::PlantsVsZombies.Adventure.AdventureBurnVisual.NormalDeath)
            {
                return false;
            }

            if (pose.Visual == global::PlantsVsZombies.Adventure.AdventureBurnVisual.None)
            {
                return true;
            }

            Presentation.Corpses.Add(new global::PlantsVsZombies.Adventure.AdventureZombieCorpse {
                Zombie = zombie,
                Burn = pose,
                Animation = zombie.PresentationAnimation,
                FrozenSeconds = zombie.PresentationAnimationAge,
                FrozenHeadSeconds = zombie.PresentationPlantHeadAge,
                RateScale = pose.Visual == global::PlantsVsZombies.Adventure.AdventureBurnVisual.CharredDefinition ? .9f + (float)_random.NextDouble() * .2f : 1f,
                Duration = pose.Visual == global::PlantsVsZombies.Adventure.AdventureBurnVisual.FrozenOriginal ? 3f : float.PositiveInfinity
            });
            return true;
        }

        // Called after horizontal integration. Snow trail sampling follows the torch conversion in the caller.
        // Plant::Fire native lob velocity uses a 120-tick horizontal lead and acceleration .115.
        /// <summary>Performs advance adventure projectile presentation.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureProjectilePresentation(Pea shot, float dt)
        {
            if (!shot.PresentationMotionInitialized)
            {
                shot.PresentationMotionInitialized = true;
                shot.PresentationRotation = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? 0f : shot.Splash ? -1.2566371f : -.87964594f;
                var minimum = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? -.2f : -.08f;
                var maximum = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? -.08f : -.02f;
                shot.PresentationSpin = shot.Lobbed ? minimum + (float)_random.NextDouble() * (maximum - minimum) : 0;
                if (shot.Lobbed)
                {
                    var launch = shot.PresentationLaunchColumn + (shot.SourceKind == PlantKind.CabbagePult ? 5f : shot.SourceKind == PlantKind.KernelPult ? shot.Butter ? 12f : 19f : 25f) / 80f;
                    var target = FindAdventureTarget(shot.Row, launch, 20f, false, true);
                    var distance = target == null ? 700f - (40f + launch * 80f) : (target.X - launch - target.Speed * .5f) * 80f - 30f;
                    shot.PresentationLobX = launch;
                    shot.PresentationPreviousX = launch;
                    shot.VelocityX = Math.Max(40f, distance) / 120f * 100f / 80f;
                    var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
                    var launchOffset = shot.SourceKind == PlantKind.KernelPult ? shot.Butter ? -56f : -37f : shot.Splash ? -46f : -12f;
                    var launchY = 80 + shot.PresentationLaunchRow * pitch + (Adventure.Level.HasRoof ? Math.Max(0, 5 - shot.PresentationLaunchColumn) * 20 - 10 : 0) + launchOffset;
                    if (HasPlantKindAt(PlantKind.FlowerPot, shot.PresentationLaunchRow, shot.PresentationLaunchColumn))
                    {
                        launchY -= 5;
                    }

                    shot.PresentationLaunchY = launchY;
                    float heightDifference = 0;
                    if (target != null)
                    {
                        var floorX = 40 + target.X * 80 - 57f * (target.IsSmall ? .5f : 1f) + 40;
                        var targetY = 80 + target.Row * pitch + (Adventure.Level.HasRoof ? Math.Max(0, 440 - floorX) * .25f - 10 : 0) - 30;
                        if (target.Kind == ZombieKind.Gargantuar || target.Kind == ZombieKind.Catapult)
                        {
                            targetY -= 13;
                        }

                        if (target.Kind == ZombieKind.Zomboni)
                        {
                            targetY += 22;
                        }

                        heightDifference = targetY - launchY;
                    }

                    shot.PresentationLobInitialVelocity = -7f + heightDifference / 120f;
                    shot.PresentationLobVelocity = shot.PresentationLobInitialVelocity;
                }
            }

            shot.PresentationNativeAge += dt;
            if (!shot.Lobbed)
            {
                return;
            }

            var ticks = shot.PresentationNativeAge * 100f;
            shot.X = shot.PresentationLobX + shot.VelocityX * shot.PresentationNativeAge;
            shot.PresentationLobHeight = shot.PresentationLobInitialVelocity * ticks + .115f * ticks * (ticks + 1f) * .5f;
            shot.PresentationLobVelocity = shot.PresentationLobInitialVelocity + .115f * ticks;
        }

        /// <summary>Performs create adventure presentation sun.</summary>
        /// <param name = "producer">The producer.</param>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting create adventure presentation sun.</returns>
        private global::PlantsVsZombies.Simulation.Sun CreateAdventurePresentationSun(Plant? producer, int value = 25)
        {
            var sun = CreateSun(0f, 0f, 0f, value);
            AdventurePresentationSunSpawn(sun, producer);
            return sun;
        }

        /// <summary>Performs adventure presentation sun spawn.</summary>
        /// <param name = "sun">The sun.</param>
        /// <param name = "producer">The producer.</param>
        private void AdventurePresentationSunSpawn(global::PlantsVsZombies.Simulation.Sun sun, Plant? producer)
        {
            var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            if (producer != null)
            {
                float nativeX = 40 + producer.Column * 80;
                var nativeY = 80 + producer.Row * pitch + (Adventure.Level.HasRoof ? Math.Max(0, 5 - producer.Column) * 20 - 10 : 0);
                InitializeAdventurePresentationPlantSunAtPoint(sun, nativeX, nativeY);
                return;
            }

            var kind = sun.Value == 15 ? global::PlantsVsZombies.Sun.SunKind.Small : sun.Value == 50 ? global::PlantsVsZombies.Sun.SunKind.Large : global::PlantsVsZombies.Sun.SunKind.Normal;
            float x = 100 + _random.Next(550), y = 60;
            sun.PresentationMotion = global::PlantsVsZombies.Adventure.AdventureSunMotion.Sky;
            sun.PresentationFromPlant = false;
            sun.PresentationVelocityX = 0;
            sun.PresentationLandingY = 300 + _random.Next(250);
            sun.PresentationVelocityY = .67f;
            sun.PresentationNative = true;
            sun.PresentationScale = kind == global::PlantsVsZombies.Sun.SunKind.Small ? .5f : kind == global::PlantsVsZombies.Sun.SunKind.Large ? 2f : 1f;
            sun.PresentationPickup = new global::PlantsVsZombies.Sun.SunPickupState(kind, x, y);
            sun.PresentationPreviousX = x;
            sun.PresentationPreviousY = y;
            sun.X = (x + 30 - 40) / 80f;
            sun.Y = (y + 30 - 80) / pitch;
        }

        // Inputs are original Coin top-left pixel coordinates, not the reanim centre.
        /// <summary>Performs create adventure presentation sun from point.</summary>
        /// <param name = "nativeX">The native x.</param>
        /// <param name = "nativeY">The native y.</param>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting create adventure presentation sun from point.</returns>
        private global::PlantsVsZombies.Simulation.Sun CreateAdventurePresentationSunFromPoint(float nativeX, float nativeY, int value = 25)
        {
            var sun = CreateSun(0f, 0f, 0f, value);
            InitializeAdventurePresentationPlantSunAtPoint(sun, nativeX, nativeY);
            return sun;
        }

        /// <summary>Performs initialize adventure presentation plant sun at point.</summary>
        /// <param name = "sun">The sun.</param>
        /// <param name = "nativeX">The native x.</param>
        /// <param name = "nativeY">The native y.</param>
        private void InitializeAdventurePresentationPlantSunAtPoint(global::PlantsVsZombies.Simulation.Sun sun, float nativeX, float nativeY)
        {
            var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            var kind = sun.Value == 15 ? global::PlantsVsZombies.Sun.SunKind.Small : sun.Value == 50 ? global::PlantsVsZombies.Sun.SunKind.Large : global::PlantsVsZombies.Sun.SunKind.Normal;
            sun.PresentationMotion = global::PlantsVsZombies.Adventure.AdventureSunMotion.Plant;
            sun.PresentationFromPlant = true;
            sun.PresentationVelocityY = -1.7f - (float)_random.NextDouble() * 1.7f;
            sun.PresentationVelocityX = -.4f + (float)_random.NextDouble() * .8f;
            sun.PresentationLandingY = (int)(nativeY + 15 + _random.Next(20));
            sun.PresentationScale = .4f * (kind == global::PlantsVsZombies.Sun.SunKind.Small ? .5f : kind == global::PlantsVsZombies.Sun.SunKind.Large ? 2f : 1f);
            sun.PresentationPickup = new global::PlantsVsZombies.Sun.SunPickupState(kind, nativeX, nativeY);
            sun.PresentationPreviousX = nativeX;
            sun.PresentationPreviousY = nativeY;
            sun.X = (nativeX + 30 - 40) / 80f;
            sun.Y = (nativeY + 30 - 80) / pitch;
            sun.PresentationNative = true;
        }

        // Existing mode drops use board-space centres. Only the explicit sky factory may
        // randomize their birth position. PC 0x43BE43: motion 3, -3-R(2), R(1)-.5,
        // birthY+45+R(20), clamped to 80..521; 0x43C1F0 applies gravity .15.
        /// <summary>Performs initialize adventure presentation dropped sun.</summary>
        /// <param name = "sun">The sun.</param>
        private void InitializeAdventurePresentationDroppedSun(global::PlantsVsZombies.Simulation.Sun sun)
        {
            if (sun == null || sun.PresentationNative)
            {
                return;
            }

            var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            float x = 40f + sun.X * 80f - 30f, y = 80f + sun.Y * pitch - 30f;
            var kind = sun.Value == 15 ? global::PlantsVsZombies.Sun.SunKind.Small : sun.Value == 50 ? global::PlantsVsZombies.Sun.SunKind.Large : global::PlantsVsZombies.Sun.SunKind.Normal;
            sun.PresentationMotion = global::PlantsVsZombies.Adventure.AdventureSunMotion.Coin;
            sun.PresentationFromPlant = false;
            sun.PresentationVelocityY = -3f - (float)_random.NextDouble() * 2f;
            sun.PresentationVelocityX = (float)_random.NextDouble() - .5f;
            sun.PresentationLandingY = Math.Max(80, Math.Min(521, (int)(y + 45f + _random.Next(20))));
            sun.PresentationScale = kind == global::PlantsVsZombies.Sun.SunKind.Small ? .5f : kind == global::PlantsVsZombies.Sun.SunKind.Large ? 2f : 1f;
            sun.PresentationPickup = new global::PlantsVsZombies.Sun.SunPickupState(kind, x, y);
            sun.PresentationPreviousX = x;
            sun.PresentationPreviousY = y;
            sun.PresentationNative = true;
        }

        /// <summary>Performs create adventure presentation dropped sun.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting create adventure presentation dropped sun.</returns>
        private global::PlantsVsZombies.Simulation.Sun CreateAdventurePresentationDroppedSun(float x, float row, int value = 25)
        {
            global::PlantsVsZombies.Simulation.Sun sun = CreateSun(x, row, row, value);
            InitializeAdventurePresentationDroppedSun(sun);
            return sun;
        }

        /// <summary>Performs try collect adventure presentation sun.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting try collect adventure presentation sun.</returns>
        private bool TryCollectAdventurePresentationSun(global::PlantsVsZombies.Simulation.Sun sun)
        {
            if (!IsAdventure || State != MatchState.Playing || !Suns.Contains(sun))
            {
                return false;
            }

            if (!sun.PresentationNative)
            {
                InitializeAdventurePresentationDroppedSun(sun);
            }

            global::PlantsVsZombies.Sun.SunPickupState clicked;
            if (!global::PlantsVsZombies.Sun.SunPickupRules.TryClick(sun.PresentationPickup, (int)sun.PresentationPickup.X + 30, (int)sun.PresentationPickup.Y + 30, 0, true, false, sun.PresentationSpecialExpansion, out clicked))
            {
                return false;
            }

            sun.PresentationPickup = clicked;
            // PC Coin::MouseDown 0x43D6E0 plays the sun foley before collection flight.
            Raise(GameEvent.SunPickupStarted);
            AdventureSunSelectedForPacketFeedback(sun);
            return true;
        }

        /// <summary>Performs advance adventure presentation suns.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventurePresentationSuns(float dt)
        {
            var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            for (var i = Suns.Count - 1; i >= 0; i--)
            {
                var sun = Suns[i];
                if (!sun.PresentationNative)
                {
                    InitializeAdventurePresentationDroppedSun(sun);
                }

                sun.Age += dt;
                var pickup = sun.PresentationPickup;
                sun.PresentationPreviousX = pickup.X;
                sun.PresentationPreviousY = pickup.Y;
                var nativeScale = pickup.Kind == global::PlantsVsZombies.Sun.SunKind.Small ? .5f : pickup.Kind == global::PlantsVsZombies.Sun.SunKind.Large ? 2f : 1f;
                var ground = false;
                if (!pickup.Collecting && pickup.FadeCountdown == 0 && !pickup.Dead)
                {
                    float x = pickup.X, y = pickup.Y + sun.PresentationVelocityY;
                    ground = y >= sun.PresentationLandingY;
                    if (ground)
                    {
                        y = sun.PresentationLandingY;
                        x = (float)(x >= 0 ? Math.Floor(x + .5f) : Math.Ceiling(x - .5f));
                    }
                    else
                    {
                        if (sun.PresentationMotion == global::PlantsVsZombies.Adventure.AdventureSunMotion.Plant)
                        {
                            sun.PresentationVelocityY += .09f;
                        }
                        else if (sun.PresentationMotion == global::PlantsVsZombies.Adventure.AdventureSunMotion.Coin)
                        {
                            sun.PresentationVelocityY += .15f;
                        }

                        x += sun.PresentationVelocityX;
                        if (x > 740)
                        {
                            x = 740;
                            sun.PresentationVelocityX = -.4f - (float)_random.NextDouble() * .4f;
                        }
                        else if (x < 0)
                        {
                            x = 0;
                            sun.PresentationVelocityX = .4f + (float)_random.NextDouble() * .4f;
                        }
                    }

                    pickup = pickup.WithPosition(x, y);
                    if (sun.PresentationFromPlant)
                    {
                        sun.PresentationScale = sun.PresentationScale < nativeScale ? sun.PresentationScale + .02f : nativeScale;
                    }
                }

                if (pickup.Collecting)
                {
                    float dx = pickup.X - 15f, dy = pickup.Y;
                    sun.PresentationScale = Math.Max(.5f, Math.Min(1f, (float)Math.Sqrt(dx * dx + dy * dy) * .05f)) * nativeScale;
                }

                var mode = ModeSession == null ? 0 : ModeSession.Definition.Id;
                var tick = global::PlantsVsZombies.Sun.SunPickupRules.Tick(pickup, SunAmount, true, ground, true, false, mode);
                sun.PresentationPickup = tick.State;
                sun.X = (tick.State.X + 30 - 40) / 80f;
                sun.Y = (tick.State.Y + 30 - 80) / pitch;
                if (tick.Credited)
                {
                    SunAmount = tick.Balance;
                    CollectAdventureSun();
                    Raise(GameEvent.SunCollected);
                }

                if (tick.State.Dead)
                {
                    RemoveSunAt(i);
                }
            }
        }

        // 88 ascending updates to -704, then 84 descending updates until Z > -40.
        /// <summary>Stores adventure presentation cob flight seconds.</summary>
        public const float AdventurePresentationCobFlightSeconds = 1.72f;
        // Cob impacts are produced by the actual shell explosion, without a second damage source.
        /// <summary>Performs adventure presentation cob impact.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        private void AdventurePresentationCobImpact(float x, float row)
        {
            Presentation.Impacts.Add(new global::PlantsVsZombies.Adventure.AdventureImpact {
                Id = ++Presentation._effectId,
                Seed = _random.Next(),
                Kind = PlantKind.CobCannon,
                X = x,
                Row = row,
                Cob = true
            });
        }

        // Board.PlaceRake / GridItem.UpdateRake: column 7, 20 fps, .8 hit event, 200-tick lifetime.
        /// <summary>Performs initialize adventure purchases.</summary>
        private void InitializeAdventurePurchases()
        {
            if (Rules.AdventurePlaceRake == null || Adventure.Level.Mode == AdventureMode.Boss || Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                return;
            }

            var rows = new List<int>();
            for (var r = 0; r < Math.Min(5, RowCount); r++)
            {
                if (Adventure.Level.IsLawnRow(r) && !Adventure.Level.IsWaterRow(r))
                {
                    rows.Add(r);
                }
            }

            if (rows.Count == 0)
            {
                return;
            }

            var row = rows[_random.Next(rows.Count)];
            var column = 7;
            if (Adventure.Level.Mode == AdventureMode.Vasebreaker)
            {
                foreach (var vase in Adventure._vases)
                {
                    if (vase.Column > 0 && vase.Column <= column)
                    {
                        column = vase.Column - 1;
                    }
                }
            }

            if (!Rules.AdventurePlaceRake())
            {
                return;
            }

            Adventure.RakeRow = row;
            Adventure.RakeColumn = column;
            Adventure.RakeAttracting = true;
        }

        /// <summary>Performs advance adventure purchases.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventurePurchases(float dt)
        {
            var a = Adventure;
            if (a.RakeRow < 0)
            {
                return;
            }

            Zombie? target = null;
            foreach (var z in Zombies)
            {
                if (z.Row == a.RakeRow && !z.Airborne && !z.Submerged && !z.Hypnotized && z.Kind != ZombieKind.Bungee && z.Kind != ZombieKind.Bobsled && z.X - 0.3f <= a.RakeColumn + 63f / 80f && z.X + 0.3f >= a.RakeColumn)
                {
                    target = z;
                    break;
                }
            }

            if (!a.RakeTriggered)
            {
                if (target != null)
                {
                    a.RakeTriggered = true;
                    a.RakeAge = 0f;
                }

                return;
            }

            a.RakeAge += dt;
            if (!a._rakeDamageDone && a.RakeAge >= .32f)
            {
                a._rakeDamageDone = true;
                if (target != null)
                {
                    HitAdventureZombie(target, 1800f);
                }
            }

            if (a.RakeAge >= 2f)
            {
                a.RakeRow = -1;
            }
        }

        /// <summary>Performs is adventure first aid.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting is adventure first aid.</returns>
        private bool IsAdventureFirstAid(PlantKind kind, int row, int column)
        {
            if (!Rules.AdventureFirstAid || kind != PlantKind.Wallnut && kind != PlantKind.TallNut && kind != PlantKind.Pumpkin)
            {
                return false;
            }

            var old = AdventurePlantAt(row, column, kind == PlantKind.Pumpkin ? PlantLayer.Armor : PlantLayer.Main);
            return old != null && old.Kind == kind && old.Health < (kind == PlantKind.TallNut ? 8000f : 4000f) * 2f / 3f;
        }

        /// <summary>Performs remember adventure reward position.</summary>
        /// <param name = "zombie">The zombie.</param>
        private void RememberAdventureRewardPosition(Zombie zombie)
        {
            // PC 0x54DE40: GetZombieRect centre. The basic body rect is (36,0,42,115).
            var scale = zombie.IsSmall ? .5f : 1f;
            var sourceX = 40f + zombie.X * 80f - 57f * scale - 20f * (1f - scale);
            var floor = 80f + zombie.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f);
            if (Adventure.Level.HasRoof)
            {
                floor += Math.Max(0, 440f - (sourceX + 40f)) * .25f - 10f;
            }

            var sourceY = floor - 30f;
            if (zombie.Kind == ZombieKind.Balloon)
            {
                sourceY -= 30f;
            }

            int rectX = 36, rectY = 0, rectW = 42, rectH = 115;
            // Literal initial body rectangles from PC ZombieInitialize 0x53DD90.
            switch (zombie.Kind)
            {
                case ZombieKind.Football:
                    rectX = 50;
                    rectW = 57;
                    break;
                case ZombieKind.Digger:
                    rectX = 50;
                    rectW = 28;
                    break;
                case ZombieKind.Gargantuar:
                    rectX = -17;
                    rectY = -38;
                    rectW = 125;
                    rectH = 154;
                    break;
                case ZombieKind.Zomboni:
                case ZombieKind.Catapult:
                    rectX = 0;
                    rectY = -13;
                    rectW = 153;
                    rectH = 140;
                    break;
                case ZombieKind.Snorkel:
                    rectX = 12;
                    rectW = 62;
                    break;
                case ZombieKind.Bobsled:
                    if (zombie.PresentationHasSled ?? !zombie.UsedAbility)
                    {
                        rectX = -50;
                        rectW = 275;
                    }

                    break;
                case ZombieKind.Balloon:
                    rectY = 30;
                    break;
                case ZombieKind.Bungee:
                    rectX = -20;
                    rectY = 22;
                    rectW = 110;
                    rectH = 94;
                    break;
                case ZombieKind.Boss:
                    rectX = 700;
                    rectY = 80;
                    rectW = 90;
                    rectH = 430;
                    break;
            }

            Adventure._lastRewardX = (int)sourceX + (rectX + rectW / 2) * scale + 20f * (1f - scale);
            Adventure._lastRewardY = (int)sourceY + (rectY + rectH / 2) * scale + 120f * (1f - scale);
        }

        /// <summary>Performs spawn adventure reward presentation.</summary>
        private void SpawnAdventureRewardPresentation()
        {
            var kind = AdventureRewardPresentation.NativeTypeFor(Adventure.Level);
            int width = 60, height = 60;
            if (kind == 7)
            {
                width = 50;
                height = 70;
            }
            else if (kind == 15)
            {
                width = 78;
                height = 52;
            }
            else if (kind >= 9 && kind <= 14)
            {
                width = 80;
                height = 80;
            }
            else if (kind == 21)
            {
                width = 157;
                height = 269;
            }
            else if (kind == 18)
            {
                width = 91;
                height = 78;
            }

            Adventure.RewardPresentation = new AdventureRewardPresentation(kind, Adventure._lastRewardX, Adventure._lastRewardY, width, height, (float)_random.NextDouble(), (float)_random.NextDouble(), _random.Next(20), Adventure.Level.Mode == AdventureMode.Boss);
        }

        /// <summary>Performs advance adventure reward presentation.</summary>
        /// <param name = "delta">The delta.</param>
        private void AdvanceAdventureRewardPresentation(float delta)
        {
            if (!IsAdventure || State != MatchState.Won || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            if (ModeSession != null)
            {
                _completedBoardAccumulated += delta;
                while (_completedBoardAccumulated + 0.0000001 >= SimulationStep)
                {
                    AdvanceAdventureRewardTick();
                    _completedBoardAccumulated -= SimulationStep;
                }

                return;
            }

            if (Adventure.RewardPresentation == null)
            {
                return;
            }

            Adventure.RewardPresentation.Update(delta, _rewardPresentationTick);
            if (!Adventure._rewardLightfillPlayed && Adventure.RewardPresentation.NativeType != 15 && Adventure.RewardPresentation.CollectionTicks >= 300)
            {
                Adventure._rewardLightfillPlayed = true;
                Adventure.Notice(AdventureNoticeKind.RewardFade);
            }
        }

        /// <summary>Advances the existing board artwork for one native reward-presentation tick.</summary>
        private void AdvanceAdventureRewardTick()
        {
            Elapsed += .01f;
            AdventurePresentationTick(.01f);
            // Original Board::UpdateGameObjects keeps existing projectiles moving through the award sequence.
            AdvanceAdventureProjectiles(.01f, allowDamage: false);
            AdvanceAdventurePresentationSuns(.01f);
            AdvanceAdventureCoins(.01f);
            foreach (var plant in Plants)
            {
                plant.Age += .01f;
                if (AdventureProducerGlow.HasNativeProductionHint(plant.Kind))
                {
                    AdventureProducerGlow.Tick(plant, false, 0, false);
                }
            }
        }

        /// <summary>Performs advance adventure sun motion.</summary>
        /// <param name = "dt">The dt.</param>
        /// <param name = "handled">The handled.</param>
        private void AdvanceAdventureSunMotion(float dt, ref bool handled)
        {
            handled = true;
            AdvanceAdventurePresentationSuns(dt);
        }

        /// <summary>Performs collect adventure sun motion.</summary>
        /// <param name = "sun">The sun.</param>
        /// <param name = "handled">The handled.</param>
        /// <param name = "collected">The collected.</param>
        private void CollectAdventureSunMotion(global::PlantsVsZombies.Simulation.Sun sun, ref bool handled, ref bool collected)
        {
            handled = true;
            collected = TryCollectAdventurePresentationSun(sun);
        }

        /// <summary>Performs initialize replay seed choices.</summary>
        private void InitializeReplaySeedChoices()
        {
            if (!Adventure.Level.IsReplay || !Adventure.Level.ChoosesSeeds)
            {
                return;
            }

            var level = Adventure.Level;
            var candidates = new List<PlantKind>();
            bool balloons = false, roofRaid = false;
            foreach (var wave in Adventure.Waves)
            {
                foreach (var z in wave.Zombies)
                {
                    if (z == ZombieKind.Balloon)
                    {
                        balloons = true;
                    }

                    if (z == ZombieKind.Bungee || z == ZombieKind.Catapult)
                    {
                        roofRaid = true;
                    }
                }
            }

            foreach (var kind in level.UnlockedSeeds)
            {
                if (!level.IsNight && Nocturnal(kind) || level.IsNight && kind == PlantKind.CoffeeBean)
                {
                    continue;
                }

                if (kind == PlantKind.GraveBuster && level.Terrain != AdventureTerrain.Night || kind == PlantKind.Plantern && level.Terrain != AdventureTerrain.Fog || kind == PlantKind.FlowerPot && !level.HasRoof)
                {
                    continue;
                }

                if (!level.HasPool && (kind == PlantKind.LilyPad || kind == PlantKind.TangleKelp || kind == PlantKind.SeaShroom))
                {
                    continue;
                }

                if (level.HasRoof && (kind == PlantKind.Spikeweed || kind == PlantKind.Torchwood))
                {
                    continue;
                }

                if (kind == PlantKind.UmbrellaLeaf && !roofRaid || kind == PlantKind.Blover && !balloons && level.Terrain != AdventureTerrain.Fog)
                {
                    continue;
                }

                candidates.Add(kind);
            }

            var rng = new global::PlantsVsZombies.Randomness.OriginalRandom(unchecked((uint)Rules.AdventureDaveSeed));
            var required = new List<PlantKind>();
            for (var i = 0; i < 3 && candidates.Count > 0; i++)
            {
                var pick = (int)rng.NextBounded((uint)candidates.Count);
                required.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }

            Adventure.RequiredSeeds = required.AsReadOnly();
            var defaults = new List<PlantKind>(required);
            foreach (var k in level.DefaultSeeds)
            {
                if (defaults.Count < level.PacketSlots && !defaults.Contains(k))
                {
                    defaults.Add(k);
                }
            }

            foreach (var k in Adventure.AvailableSeeds)
            {
                if (defaults.Count < level.PacketSlots && !defaults.Contains(k) && k != PlantKind.Imitater)
                {
                    defaults.Add(k);
                }
            }

            Adventure.SelectedSeeds = defaults.AsReadOnly();
        }

        /// <summary>Performs bank adventure coins.</summary>
        /// <returns>The resulting bank adventure coins.</returns>
        public bool BankAdventureCoins()
        {
            if (!IsAdventure)
            {
                return false;
            }

            foreach (var coin in new PooledSnapshot<AdventureCoin>(Adventure._coins))
            {
                if (!FinishAdventureCoin(coin))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Performs adventure packet context.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "playing">The playing.</param>
        /// <param name = "darken">The darken.</param>
        /// <param name = "anticipatedSun">The anticipated sun.</param>
        /// <param name="destination">An optional immediately consumed context to overwrite; null creates an independent snapshot.</param>
        /// <returns>The supplied destination or a newly allocated snapshot of the current packet state.</returns>
        public AdventureSeedBankContext AdventurePacketContext(PlantKind kind, bool playing = true, int darken = 255, int anticipatedSun = -1, AdventureSeedBankContext? destination = null)
        {
            int left = _seedCooldownTicks[(int)kind], duration = DefinitionFor(ResolveAdventureSeed(kind)).RechargeThresholdRaw;
            var context = destination ?? new AdventureSeedBankContext();
            context.Reset();
            context.PacketKind = (int)kind;
            context.ImitaterKind = kind == PlantKind.Imitater && Adventure.ImitaterTarget.HasValue ? (int)Adventure.ImitaterTarget.Value : -1;
            context.Active = left <= 0;
            context.Refreshing = left > 0;
            context.RefreshTime = duration;
            context.RefreshCounter = left > 0 ? Math.Max(0, duration + 1 - left) : 0;
            context.Playing = playing && State == MatchState.Playing && Adventure.Phase == AdventurePhase.Playing;
            context.Paused = State == MatchState.Paused;
            context.TutorialState = Adventure.Tutorial == AdventureTutorial.ChooseSeed ? 1 : Adventure.Tutorial == AdventureTutorial.PlantSunflower ? 5 : Adventure.Tutorial == AdventureTutorial.RechargeSunflower ? 9 : 0;
            context.TutorialTimer = -1;
            context.MainCounter = Adventure._ticks;
            context.CutsceneDarken = darken;
            context.NativeMode = ModeSession == null ? 0 : ModeSession.Definition.Id;
            context.CanAfford = (anticipatedSun >= 0 ? anticipatedSun : AdventureSunAvailable) >= AdventureSeedCost(kind);
            context.Conveyor = UsesNativeAdventureConveyor;
            context.PlantingRequirementsMet = AdventurePacketRequirements(kind);
            return context;
        }

        /// <summary>Performs adventure packet requirements.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure packet requirements.</returns>
        private bool AdventurePacketRequirements(PlantKind kind)
        {
            var resolved = ResolveAdventureSeed(kind);
            if (kind == PlantKind.Imitater && !Adventure.ImitaterTarget.HasValue)
            {
                return false;
            }

            if (resolved == PlantKind.CoffeeBean)
            {
                return Plants.Exists(p => p.Asleep && p.WakeTimer <= 0);
            }

            if (resolved == PlantKind.GraveBuster)
            {
                return Adventure.Graves.Count > 0;
            }

            var prerequisite = UpgradePrerequisite(resolved);
            if (!prerequisite.HasValue)
            {
                return true;
            }

            foreach (Plant p in Plants)
            {
                if (p.Kind == prerequisite.Value && (resolved != PlantKind.CobCannon || HasPlantKindAt(PlantKind.KernelPult, p.Row, p.Column + 1)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs advance adventure packet feedback.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventurePacketFeedback(float dt)
        {
            AdvanceAdventureConveyorPackets();
            for (var i = Adventure.SeedFlashes.Count - 1; i >= 0; i--)
            {
                Adventure.SeedFlashes[i].Age += dt;
                if (Adventure.SeedFlashes[i].Age >= .25f)
                {
                    Adventure.SeedFlashes.RemoveAt(i);
                }
            }

            if (Adventure._ticks == 1)
            {
                foreach (PlantKind kind in Adventure.SelectedSeeds)
                {
                    AdventurePacketReady(kind);
                }
            }
        }

        /// <summary>Performs adventure packet ready.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "anticipatedSun">The anticipated sun.</param>
        private void AdventurePacketReady(PlantKind kind, int anticipatedSun = -1)
        {
            if (UsesNativeAdventureConveyor || !ContainsSeed(Adventure.SelectedSeeds, kind))
            {
                return;
            }

            var flash = AdventureSeedBankPresentation.ReadyCheck(AdventurePacketContext(kind, anticipatedSun: anticipatedSun, destination: _packetFeedbackContext), 0, 0, 0, 0);
            if (flash.Spawn)
            {
                Adventure.SeedFlashes.Add(new AdventureSeedFlashState { Id = ++Adventure._seedFlashId, Kind = kind });
            }
        }

        /// <summary>Performs adventure sun selected for packet feedback.</summary>
        /// <param name = "added">The added.</param>
        private void AdventureSunSelectedForPacketFeedback(global::PlantsVsZombies.Simulation.Sun added)
        {
            var collecting = 0;
            foreach (global::PlantsVsZombies.Simulation.Sun sun in Suns)
            {
                if (sun.PresentationNative && sun.PresentationPickup.Collecting)
                {
                    collecting += sun.Value;
                }
            }

            foreach (PlantKind kind in Adventure.SelectedSeeds)
            {
                if (AdventureSeedBankPresentation.SunCrossesAffordability(SunAmount - _adventureSunReservation, collecting, AdventureSeedCost(kind), added.Value))
                {
                    AdventurePacketReady(kind);
                }
            }
        }

        /// <summary>Stores the adventure board, or null when this simulation does not run an adventure level.</summary>
        private AdventureBoard? _adventure;
        /// <summary>Gets the adventure board, which exists exactly when <see cref="IsAdventure"/> is true.</summary>
        /// <exception cref="InvalidOperationException">This simulation does not run an adventure level.</exception>
        public AdventureBoard Adventure => _adventure ?? throw new InvalidOperationException("This simulation does not run an adventure level.");

        /// <summary>Performs initialize adventure.</summary>
        /// <param name = "initialized">The initialized.</param>
        private void InitializeAdventure(ref bool initialized)
        {
            var level = AdventureLevels.ForRun(Rules.AdventureLevel, Rules.AdventureCycle, Rules.AdventurePacketSlots);
            RowCount = level.Rows;
            SunAmount = level.StartingSun;
            _adventure = new AdventureBoard(level)
            {
                Phase = level.ChoosesSeeds ? AdventurePhase.ChoosingSeeds : AdventurePhase.Playing,
                SelectedSeeds = level.DefaultSeeds,
                Waves = AdventureWavePlan.Build(level, _random),
                Tutorial = level.IsReplay ? AdventureTutorial.Complete : level.Number == 1 ? AdventureTutorial.ChooseSeed : level.Number == 2 ? AdventureTutorial.PlantSunflower : level.Number == 5 ? AdventureTutorial.Shovel : level.Number == 11 ? AdventureTutorial.NightMushrooms : level.Number == 21 ? AdventureTutorial.PoolSupport : level.Number == 31 ? AdventureTutorial.FogVisibility : level.Number == 41 ? AdventureTutorial.RoofPots : level.Mode == AdventureMode.Bowling ? AdventureTutorial.Bowling : level.Mode == AdventureMode.WhackAZombie ? AdventureTutorial.Hammer : level.Mode == AdventureMode.Vasebreaker ? AdventureTutorial.Vases : level.Mode == AdventureMode.Boss ? AdventureTutorial.Boss : level.HasConveyor ? AdventureTutorial.Conveyor : AdventureTutorial.Complete,
                WaveCountdown = !level.IsReplay && level.Number == 2 ? 50f : level.Mode == AdventureMode.Bowling || level.Mode == AdventureMode.WhackAZombie || level.Mode == AdventureMode.LittleTrouble || level.Mode == AdventureMode.Storm || level.Mode == AdventureMode.Bungee ? 2f : 18f,
                _nextPacket = 4f,
                _nextSkySun = !level.IsReplay && level.Number == 1 ? 7f : 4.25f + (float)_random.NextDouble() * 2.75f,
                _nextHammerSun = 8f,
                BossHealth = level.Mode == AdventureMode.Boss ? 40000f : 0f,
                _bossTimer = 12f
            };
            Adventure._whackSeedBankY = level.Mode == AdventureMode.WhackAZombie ? -87f : 0f;
            InitializeAdventureSeedAccess();
            InitializeReplaySeedChoices();
            Adventure._waveInitialCountdown = Adventure.WaveCountdown;
            Mowers.Clear();
            for (var r = 0; r < RowCount; r++)
            {
                Mowers.Add(new Mower {
                    Row = r,
                    X = -0.5f,
                    Available = Rules.MowersEnabled && level.IsLawnRow(r) && (!level.HasRoof || Rules.AdventureRoofCleaners)
                });
            }

            if (level.HasRoof)
            {
                var columns = level.Number == 41 ? 5 : level.Number == 42 ? 4 : 3;
                for (var r = 0; r < RowCount; r++)
                {
                    for (var c = 0; c < columns; c++)
                    {
                        PlaceAdventurePlant(PlantKind.FlowerPot, r, c);
                    }
                }
            }

            if (level.Number == 5 && !level.IsReplay)
            {
                for (var c = 3; c <= 5; c++)
                {
                    PlaceAdventurePlant(PlantKind.Peashooter, 2, c);
                }
            }

            if (level.Terrain == AdventureTerrain.Night && level.Mode != AdventureMode.Vasebreaker)
            {
                InitializeAdventureGraves();
            }

            if (level.Mode == AdventureMode.Vasebreaker)
            {
                InitializeVaseStage();
            }

            if (level.Mode == AdventureMode.Bowling)
            {
                AddAdventurePacket(PlantKind.Wallnut);
            }

            if (level.Mode == AdventureMode.Boss)
            {
                AddAdventurePacket(PlantKind.CabbagePult);
                AddAdventurePacket(PlantKind.Jalapeno);
                AddAdventurePacket(PlantKind.CabbagePult);
                AddAdventurePacket(PlantKind.IceShroom);
                Adventure._nextPacket = 10f;
            }

            InitializeAdventurePurchases();
            InitializeAdventureWaveScheduler();
            initialized = true;
        }

        /// <summary>Performs choose adventure seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <returns>The resulting choose adventure seeds.</returns>
        public bool ChooseAdventureSeeds(IEnumerable<PlantKind>? seeds)
        {
            List<PlantKind>? selected;
            if (!TryPrepareAdventureSeeds(seeds, out selected))
            {
                return false;
            }

            Adventure.SelectedSeeds = selected.AsReadOnly();
            Adventure.Phase = AdventurePhase.Playing;
            return true;
        }

        /// <summary>Pure validation for hosts that must persist a selection before publishing gameplay.</summary>
        public bool CanChooseAdventureSeeds(IEnumerable<PlantKind> seeds)
        {
            List<PlantKind>? selected;
            return TryPrepareAdventureSeeds(seeds, out selected);
        }

        /// <summary>Performs try prepare adventure seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <param name = "selected">The selected.</param>
        /// <returns>The resulting try prepare adventure seeds.</returns>
        private bool TryPrepareAdventureSeeds(IEnumerable<PlantKind>? seeds, [NotNullWhen(true)] out List<PlantKind>? selected)
        {
            selected = null;
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.ChoosingSeeds || seeds == null)
            {
                return false;
            }

            var next = new List<PlantKind>();
            foreach (var kind in seeds)
            {
                if (next.Contains(kind) || !ContainsSeed(Adventure.AvailableSeeds, kind) || kind == PlantKind.Imitater && !Adventure.ImitaterTarget.HasValue)
                {
                    return false;
                }

                next.Add(kind);
            }

            if (next.Count != Math.Min(Adventure.Level.PacketSlots, Adventure.AvailableSeeds.Count))
            {
                return false;
            }

            foreach (var kind in Adventure.RequiredSeeds)
            {
                if (!next.Contains(kind))
                {
                    return false;
                }
            }

            selected = next;
            return true;
        }

        /// <summary>Performs choose default adventure seeds.</summary>
        /// <returns>The resulting choose default adventure seeds.</returns>
        public bool ChooseDefaultAdventureSeeds()
        {
            return ChooseAdventureSeeds(_adventure == null ? null : _adventure.SelectedSeeds);
        }

        /// <summary>Performs select adventure seed.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting select adventure seed.</returns>
        public bool SelectAdventureSeed(PlantKind kind)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing || !ContainsSeed(Adventure.SelectedSeeds, kind))
            {
                return false;
            }

            Adventure.SelectedSeed = kind;
            if (Adventure.Level.Number == 1 && Adventure.Tutorial == AdventureTutorial.ChooseSeed)
            {
                Adventure.Tutorial = AdventureTutorial.PlantPeashooter;
                Adventure.Notice(AdventureNoticeKind.Tutorial, (int)Adventure.Tutorial);
            }

            return true;
        }

        /// <summary>Performs acknowledge adventure tutorial.</summary>
        /// <returns>The resulting acknowledge adventure tutorial.</returns>
        public bool AcknowledgeAdventureTutorial()
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Tutorial == AdventureTutorial.Complete)
            {
                return false;
            }

            // Interactive first two tutorials complete through real actions.
            if (Adventure.Level.Number <= 2 || Adventure.Level.Number == 5)
            {
                return false;
            }

            Adventure.Tutorial = AdventureTutorial.Complete;
            return true;
        }

        /// <summary>Performs collect adventure sun.</summary>
        private void CollectAdventureSun()
        {
            SettleAdventureSunReservation();
            Adventure._collectedSun = true;
            if (Adventure.Level.Number == 1 && Adventure.Tutorial == AdventureTutorial.CollectSun)
            {
                Adventure.Tutorial = AdventureTutorial.Complete;
            }
        }

        /// <summary>Performs collect adventure reward.</summary>
        /// <returns>The resulting collect adventure reward.</returns>
        public bool CollectAdventureReward()
        {
            if (!IsAdventure || Adventure.Phase != AdventurePhase.Reward || State != MatchState.Won)
            {
                return false;
            }

            Adventure.Phase = AdventurePhase.Finished;
            if (Adventure.RewardPresentation != null)
            {
                Adventure.RewardPresentation.BeginCollection();
                Raise(GameEvent.Won);
            }

            Adventure.Notice(AdventureNoticeKind.RewardCollected, Adventure.Level.Number);
            return true;
        }

        /// <summary>Performs advance adventure.</summary>
        /// <param name = "dt">The dt.</param>
        /// <param name = "handled">The handled.</param>
        private void AdvanceAdventure(float dt, ref bool handled)
        {
            handled = true;
            if (Adventure.Phase != AdventurePhase.Playing || Adventure.Level.Number == 5 && Adventure.Tutorial == AdventureTutorial.Shovel)
            {
                return;
            }

            Elapsed += dt;
            Adventure._ticks++;
            AdventurePresentationTick(dt);
            AdvanceAdventurePacketFeedback(dt);
            for (var i = 0; i < _seedCooldownTicks.Length; i++)
            {
                if (_seedCooldownTicks[i] > 0)
                {
                    _seedCooldownTicks[i]--;
                    if (_seedCooldownTicks[i] == 0)
                    {
                        AdventurePacketReady((PlantKind)i);
                    }
                }
            }

            Adventure.FogBlownRemaining = Math.Max(0f, Adventure.FogBlownRemaining - dt);
            TickCellTimers(Adventure._craters, dt);
            TickCellTimers(Adventure._ice, dt);
            AdvanceAdventureEconomy(dt);
            AdvanceAdventureWhackSeedBank(dt);
            if (Adventure.Level.Mode == AdventureMode.Boss)
            {
                AdvanceAdventureBoss(dt);
            }
            else if (Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                TickAdventureWhack(dt);
            }
            else if (Adventure.Level.Mode != AdventureMode.Vasebreaker)
            {
                AdvanceScheduledAdventureWaves(dt);
            }

            UpdateSuns(dt);
            AdvanceAdventurePlants(dt);
            AdvanceAdventureProjectiles(dt);
            AdvanceAdventureZombies(dt);
            AdvanceAdventurePurchases(dt);
            if (State != MatchState.Playing)
            {
                Adventure.Phase = AdventurePhase.Lost;
                return;
            }

            AdvanceAdventureMowers(dt);
            AdvanceAdventureSpecials(dt);
            AdvanceAdventureUpgradeWorld(dt);
            AdvanceAdventureProgressMeter();
            for (var i = Flames.Count - 1; i >= 0; i--)
            {
                Flames[i].Age += dt;
                if (Flames[i].Age >= 1f)
                {
                    RemoveFlameAt(i);
                }
            }

            var cleared = Zombies.Count == 0;
            if (Adventure.Level.Mode == AdventureMode.Boss)
            {
                cleared &= Adventure.BossHealth <= 0f && Adventure._balls.Count == 0;
            }
            else if (Adventure.Level.Mode == AdventureMode.Vasebreaker)
            {
                cleared &= Adventure.VaseStage == 2 && !Adventure.VaseInterludeActive && Adventure._vases.Count == 0;
            }
            else
            {
                cleared &= Adventure.CurrentWave >= Adventure.Waves.Count && Adventure._finalAmbush;
            }

            if (cleared)
            {
                State = MatchState.Won;
                Adventure.Phase = AdventurePhase.Reward;
                if (ModeSession == null)
                {
                    SpawnAdventureRewardPresentation();
                }

                Adventure.Notice(AdventureNoticeKind.RewardDropped, Adventure.Level.Number);
                if (ModeSession != null)
                {
                    Raise(GameEvent.Won);
                }
            }
        }

        /// <summary>Advances the original PC flag meter after this tick's combat and spawning updates.</summary>
        private void AdvanceAdventureProgressMeter()
        {
            var board = Adventure;
            if (board.Level.Mode == AdventureMode.Boss)
            {
                Presentation.ProgressMeter.SetBossHealth(board.BossHealth, board.BossMaxHealth);
                return;
            }

            var schedule = WaveSchedule;
            if (schedule == null)
            {
                return;
            }

            var hasFlags = global::PlantsVsZombies.Adventure.AdventureProgressMeterState.HasFlags(board.Level.Number, board.Level.IsReplay);
            var wavesPerFlag = board.Level.IsReplay ? 10 : Math.Min(10, Math.Max(1, board.Waves.Count));
            Presentation.ProgressMeter.Advance(board._ticks, board.CurrentWave, board.Waves.Count, wavesPerFlag, hasFlags,
                schedule.InitialCountdownTicks, schedule.CountdownTicks, schedule.HealthAtWaveStart,
                schedule.HealthToNextWave, ScheduledWaveHealth(board.CurrentWave - 1));
        }

        /// <summary>Performs contains seed.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting contains seed.</returns>
        private static bool ContainsSeed(IReadOnlyList<PlantKind> seeds, PlantKind kind)
        {
            for (var i = 0; i < seeds.Count; i++)
            {
                if (seeds[i] == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs tick cell timers.</summary>
        /// <param name = "cells">The cells.</param>
        /// <param name = "dt">The dt.</param>
        private static void TickCellTimers(List<AdventureCellTimer> cells, float dt)
        {
            for (var i = cells.Count - 1; i >= 0; i--)
            {
                cells[i].Remaining -= dt;
                if (cells[i].Remaining <= 0f)
                {
                    cells.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs advance adventure economy.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureEconomy(float dt)
        {
            var a = Adventure;
            if (!a.Level.IsNight && !a.Level.HasConveyor && a.Level.Mode != AdventureMode.Vasebreaker)
            {
                a._nextSkySun -= dt;
                if (a._nextSkySun <= 0f)
                {
                    Suns.Add(CreateAdventurePresentationSun(null, 25));
                    a._nextSkySun = 4.25f + Math.Min(9.5f, a._skySunCount++ * 0.1f) + (float)_random.NextDouble() * 2.75f;
                }
            }

            if (a.Level.HasConveyor)
            {
                a._nextPacket -= dt;
                if (a._nextPacket <= 0f)
                {
                    if (a._packets.Count < 10)
                    {
                        var total = 0;
                        foreach (SeedWeight s in a.Level.ConveyorSeeds)
                        {
                            total += AdventureConveyorWeight(s);
                        }

                        if (total > 0)
                        {
                            var draw = _random.Next(total);
                            foreach (SeedWeight s in a.Level.ConveyorSeeds)
                            {
                                draw -= AdventureConveyorWeight(s);
                                if (draw < 0)
                                {
                                    AddAdventurePacket(s.Kind);
                                    a._lastConveyorSeed = s.Kind;
                                    break;
                                }
                            }
                        }
                    }

                    a._nextPacket += (a._packets.Count > 8 ? 10f : a._packets.Count > 6 ? 5f : a._packets.Count > 4 ? 4.25f : 4f) * (a.Level.Mode == AdventureMode.Boss ? 0.875f : 1f);
                }
            }

            for (var i = a._packets.Count - 1; i >= 0; i--)
            {
                a._packets[i].Age += dt;
                if (a.Level.Mode == AdventureMode.Vasebreaker && a._packets[i].Age >= 75f)
                {
                    a._packets.RemoveAt(i);
                }
            }

            a.StormFlash = a.Level.Mode == AdventureMode.Storm && a._ticks % 600 < 150;
        }

        /// <summary>Advances the original Whack-a-Zombie seed-bank reveal after a sun is available.</summary>
        /// <param name="dt">The elapsed simulation seconds.</param>
        private void AdvanceAdventureWhackSeedBank(float dt)
        {
            var board = Adventure;
            if (board.Level.Mode != AdventureMode.WhackAZombie || board._whackSeedBankY >= 0f)
            {
                return;
            }

            var collecting = false;
            foreach (var sun in Suns)
            {
                if (sun.PresentationPickup.Collecting)
                {
                    collecting = true;
                    break;
                }
            }

            if (SunAmount <= 0 && !collecting && board._whackSeedBankY <= -87f)
            {
                return;
            }

            board._whackSeedBankY = Math.Min(0f, board._whackSeedBankY + dt * 200f);
        }

        /// <summary>Performs adventure conveyor weight.</summary>
        /// <param name = "seed">The seed.</param>
        /// <returns>The resulting adventure conveyor weight.</returns>
        private int AdventureConveyorWeight(SeedWeight seed)
        {
            var queued = Adventure._packets.FindAll(p => p.Kind == seed.Kind).Count;
            var planted = Plants.FindAll(p => p.Kind == seed.Kind).Count;
            var weight = seed.Weight;
            if (seed.Kind == PlantKind.GraveBuster && Adventure._graves.Count <= queued + planted)
            {
                return 0;
            }

            if (seed.Kind == PlantKind.LilyPad || seed.Kind == PlantKind.FlowerPot)
            {
                var limit = seed.Kind == PlantKind.LilyPad ? 18 : 35;
                weight = (int)Math.Round(seed.Weight + (1f - seed.Weight) * Math.Min(limit, queued + planted) / limit);
            }

            if (Adventure.Level.Mode == AdventureMode.Boss && (seed.Kind == PlantKind.CabbagePult || seed.Kind == PlantKind.KernelPult || seed.Kind == PlantKind.MelonPult))
            {
                var empty = Plants.FindAll(p => p.Kind == PlantKind.FlowerPot && AdventurePlantAt(p.Row, p.Column, PlantLayer.Main) == null).Count;
                if (empty <= 2)
                {
                    weight /= 5;
                }
                else if (empty <= 5)
                {
                    weight /= 3;
                }
            }

            if (Adventure.Level.ConveyorSeeds.Count > 2)
            {
                if (queued >= 4)
                {
                    weight = 1;
                }
                else if (queued >= 3)
                {
                    weight = 5;
                }
                else if (Adventure._lastConveyorSeed == seed.Kind)
                {
                    weight /= 2;
                }
            }

            return Math.Max(0, weight);
        }

        /// <summary>Performs choose adventure zombie row.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting choose adventure zombie row.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private int ChooseAdventureZombieRow(ZombieKind kind)
        {
            var l = Adventure.Level;
            var aquatic = kind == ZombieKind.Snorkel || kind == ZombieKind.DolphinRider;
            var choices = new List<int>();
            for (var r = 0; r < RowCount; r++)
            {
                if (l.IsLawnRow(r) && (aquatic ? l.IsWaterRow(r) : !l.IsWaterRow(r) || Adventure.CurrentWave >= 5 && (kind == ZombieKind.Normal || kind == ZombieKind.Conehead || kind == ZombieKind.Buckethead || kind == ZombieKind.Flag || kind == ZombieKind.Balloon)))
                {
                    if ((kind != ZombieKind.Gargantuar || r != 0) && (kind != ZombieKind.Dancing || l.HasPool || l.IsLawnRow(r - 1) && l.IsLawnRow(r + 1)))
                    {
                        choices.Add(r);
                    }
                }
            }

            if (choices.Count == 0)
            {
                throw new InvalidOperationException("No legal spawn lane for " + kind);
            }

            if (Adventure.RakeAttracting && choices.Contains(Adventure.RakeRow))
            {
                Adventure.RakeAttracting = false;
                return Adventure.RakeRow;
            }

            // Approximate native smooth row picking by suppressing immediate lane repeats.
            if (choices.Count > 1 && choices.Contains(Adventure._lastSpawnRow))
            {
                choices.Remove(Adventure._lastSpawnRow);
            }

            var picked = choices[_random.Next(choices.Count)];
            Adventure._lastSpawnRow = picked;
            return picked;
        }

        /// <summary>Performs initialize adventure graves.</summary>
        private void InitializeAdventureGraves()
        {
            var n = Adventure.Level.Number;
            if (Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                WhackPlaceGraves(9);
                PickAdventureSpecialGrave();
                return;
            }

            if (n <= 13)
            {
                AddGravesInColumn(6, 1);
                AddGravesInColumn(7, 1);
                AddGravesInColumn(8, 2);
            }
            else if (n == 14 || n == 16 || n == 18)
            {
                AddGravesInColumn(5, 1);
                AddGravesInColumn(6, 1);
                AddGravesInColumn(7, 2);
                AddGravesInColumn(8, 3);
            }
            else if (n < 20)
            {
                AddGravesInColumn(4, 1);
                AddGravesInColumn(5, 2);
                AddGravesInColumn(6, 2);
                AddGravesInColumn(7, 3);
                AddGravesInColumn(8, 3);
            }
            else
            {
                AddGravesInColumn(3, 1);
                AddGravesInColumn(4, 2);
                AddGravesInColumn(5, 2);
                AddGravesInColumn(6, 2);
                AddGravesInColumn(7, 3);
                AddGravesInColumn(8, 3);
            }

            PickAdventureSpecialGrave();
        }

        /// <summary>Places the original Whack-a-Zombie graves using its weighted empty-cell picker.</summary>
        /// <param name="count">The number of graves to place.</param>
        private void WhackPlaceGraves(int count)
        {
            var cells = new List<(int Row, int Column, int Weight)>();
            for (var column = 3; column < Columns; column++)
            {
                for (var row = 0; row < RowCount; row++)
                {
                    if (Adventure._graves.Exists(grave => grave.Row == row && grave.Column == column))
                    {
                        continue;
                    }

                    var occupied = Plants.Exists(plant => plant.Row == row && plant.Column == column);
                    cells.Add((row, column, occupied ? 1 : 100000));
                }
            }

            count = Math.Min(count, cells.Count);
            for (var i = 0; i < count; i++)
            {
                var total = 0;
                foreach (var candidate in cells)
                {
                    total += candidate.Weight;
                }

                var pick = _random.Next(total);
                var selected = 0;
                for (var index = 0; index < cells.Count; index++)
                {
                    pick -= cells[index].Weight;
                    if (pick < 0)
                    {
                        selected = index;
                        break;
                    }
                }

                var cell = cells[selected];
                cells.RemoveAt(selected);
                foreach (var plant in new PooledSnapshot<Plant>(Plants))
                {
                    if (plant.Row == cell.Row && plant.Column == cell.Column)
                    {
                        RemoveAdventurePlantAndStack(plant);
                    }
                }

                Adventure._graves.Add(CreateAdventureGrave(cell.Row, cell.Column));
            }
        }

        /// <summary>Marks one initial grave with the original special atlas row.</summary>
        private void PickAdventureSpecialGrave()
        {
            if (Adventure._graves.Count > 0)
            {
                var appearance = Adventure._graves[_gravePresentationRandom.Next(Adventure._graves.Count)].GravePresentation;
                if (appearance != null)
                {
                    appearance.Special = true;
                }
            }
        }

        /// <summary>Performs add graves in column.</summary>
        /// <param name = "column">The column.</param>
        /// <param name = "count">The count.</param>
        private void AddGravesInColumn(int column, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var rows = new List<int>();
                for (var r = 0; r < 5; r++)
                {
                    if (!Adventure._graves.Exists(g => g.Column == column && g.Row == r))
                    {
                        rows.Add(r);
                    }
                }

                if (rows.Count > 0)
                {
                    Adventure._graves.Add(CreateAdventureGrave(rows[_random.Next(rows.Count)], column));
                }
            }
        }

        /// <summary>Creates a grave with original atlas selection, jitter, and staggered emergence.</summary>
        /// <param name="row">The lawn row.</param>
        /// <param name="column">The lawn column.</param>
        /// <returns>A grave using original artwork parameters.</returns>
        private AdventureCellTimer CreateAdventureGrave(int row, int column)
        {
            return new AdventureCellTimer
            {
                Row = row,
                Column = column,
                Remaining = float.MaxValue,
                GravePresentation = new AdventureGravePresentation(_gravePresentationRandom.Next(20),
                    _gravePresentationRandom.Next(10) - 5, _gravePresentationRandom.Next(10) - 5,
                    _gravePresentationRandom.Next(50))
            };
        }

        /// <summary>Advances the original grid-item emergence counter during introduction or battle.</summary>
        internal void AdvanceAdventureGraves()
        {
            if (!IsAdventure || !Adventure.GravesEnabled)
            {
                return;
            }

            foreach (var grave in Adventure._graves)
            {
                grave.GravePresentation?.Tick();
            }
        }

        /// <summary>Performs add adventure packet.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting add adventure packet.</returns>
        private AdventurePacket AddAdventurePacket(PlantKind kind, int? row = null, int? column = null)
        {
            var p = new AdventurePacket
            {
                Id = ++Adventure._packetId,
                Kind = kind,
                FromVaseRow = row,
                FromVaseColumn = column
            };
            Adventure._packets.Add(p);
            InitializeAdventureConveyorPacket(p, Adventure._packets.Count - 1);
            Adventure.Notice(AdventureNoticeKind.ConveyorPacket, p.Id);
            return p;
        }

        /// <summary>Strikes the center of a zombie's hammer hit rectangle for simulation-only callers.</summary>
        /// <param name="zombieId">The live zombie to strike.</param>
        /// <returns>Whether the hammer strike was accepted.</returns>
        public bool HammerAdventureZombie(int zombieId)
        {
            var zombie = Zombies.Find(candidate => candidate.Id == zombieId);
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
        public bool HammerAdventureZombie(int zombieId, float hitX, float hitY)
        {
            var whack = IsAdventure && (Adventure.Level.Mode == AdventureMode.WhackAZombie || ModeSession != null && ModeSession.Definition.Mechanic == ModeMechanic.Whack);
            if (!whack || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing)
            {
                return false;
            }

            var z = Zombies.Find(x => x.Id == zombieId);
            if (z == null || z.Health <= 0f)
            {
                return false;
            }

            if (z.ArmorHealth > 0)
            {
                EnsureAdventureZombieEquipment(z);
                AdventurePresentationZombieHit(z, 900f, true);
                if (z.Kind == ZombieKind.Buckethead)
                {
                    RequestAdventureZombieFoley(46);
                }
                else if (z.Kind == ZombieKind.Conehead)
                {
                    RequestAdventureZombieFoley(58);
                }

                z.ArmorHealth = Math.Max(0, z.ArmorHealth - 900f);
                AdventurePresentationZombieEquipmentDamaged(z, 0);
                Raise(GameEvent.ZombieHit);
            }
            else
            {
                // PC 0x42D616..0x42D65F: Pow at the click, followed by immediate death with loot.
                Presentation.Explosions.Add(new AdventureExplosion
                {
                    Id = ++Presentation._effectId,
                    Seed = _random.Next(),
                    Row = z.Row,
                    X = z.X,
                    NativeX = hitX - 3f,
                    NativeY = hitY + 9f,
                    Effect = AdventureExplosionEffect.Pow
                });
                KillAdventureZombie(z, 16U);
            }

            return true;
        }

        /// <summary>Performs initialize vase stage.</summary>
        private void InitializeVaseStage()
        {
            Adventure._packets.Clear();
            Adventure._vases.Clear();
            Plants.Clear();
            ClearPeas();
            ClearSuns();
            var stage = Adventure.VaseStage;
            var contents = new List<AdventureVase>();
            AddVaseSeeds(contents, PlantKind.Peashooter, stage == 1 ? 4 : 5);
            if (stage > 0)
            {
                AddVaseSeeds(contents, PlantKind.SnowPea, 5);
            }

            AddVaseSeeds(contents, stage == 2 ? PlantKind.HypnoShroom : PlantKind.Squash, stage == 0 ? 5 : stage == 1 ? 4 : 5);
            AddVaseZombies(contents, ZombieKind.Normal, stage == 0 ? 4 : stage == 1 ? 5 : 6);
            AddVaseZombies(contents, ZombieKind.Buckethead, stage == 2 ? 2 : 1);
            if (stage == 1)
            {
                AddVaseZombies(contents, ZombieKind.Football, 1);
            }

            if (stage == 2)
            {
                AddVaseZombies(contents, ZombieKind.Dancing, 1);
                AddVaseZombies(contents, ZombieKind.JackInTheBox, 1);
            }

            var cells = new List<int>();
            for (var r = 0; r < 5; r++)
            {
                for (var c = stage == 0 ? 6 : stage == 1 ? 5 : 4; c < 9; c++)
                {
                    cells.Add(r * 9 + c);
                }
            }

            foreach (var vase in contents)
            {
                int i = _random.Next(cells.Count), cell = cells[i];
                cells.RemoveAt(i);
                vase.Row = cell / 9;
                vase.Column = cell % 9;
                Adventure._vases.Add(vase);
            }

            var show = stage + 1;
            foreach (var vase in Adventure._vases)
            {
                if (vase._seed.HasValue && show-- > 0)
                {
                    vase.ShowsPlant = true;
                }
            }

            Adventure.Notice(AdventureNoticeKind.VaseStage, stage);
        }

        /// <summary>Performs add vase seeds.</summary>
        /// <param name = "v">The v.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "count">The count.</param>
        private static void AddVaseSeeds(List<AdventureVase> v, PlantKind kind, int count)
        {
            for (var i = 0; i < count; i++)
            {
                v.Add(new AdventureVase { _seed = kind });
            }
        }

        /// <summary>Performs add vase zombies.</summary>
        /// <param name = "v">The v.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "count">The count.</param>
        private static void AddVaseZombies(List<AdventureVase> v, ZombieKind kind, int count)
        {
            for (var i = 0; i < count; i++)
            {
                v.Add(new AdventureVase { _zombie = kind });
            }
        }

        /// <summary>Performs smash adventure vase.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting smash adventure vase.</returns>
        public bool SmashAdventureVase(int row, int column)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing || Adventure.Level.Mode != AdventureMode.Vasebreaker
                || Adventure.VaseInterludeActive && Adventure._vaseInterludePopulated)
            {
                return false;
            }

            var vase = Adventure._vases.Find(v => v.Row == row && v.Column == column);
            if (vase == null)
            {
                return false;
            }

            Adventure._vases.Remove(vase);
            if (vase._seed.HasValue)
            {
                AddAdventurePacket(vase._seed.Value, row, column);
            }

            if (vase._zombie.HasValue)
            {
                SpawnAdventureZombie(vase._zombie.Value, row, column + 0.5f, Adventure.VaseStage);
            }

            return true;
        }

        /// <summary>Performs advance adventure specials.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureSpecials(float dt)
        {
            if (ModeSession == null && Adventure.Level.Mode == AdventureMode.Vasebreaker && Adventure.VaseInterludeActive)
            {
                if (Adventure._vaseInterludeTicks > 0 && --Adventure._vaseInterludeTicks == 0)
                {
                    var message = Adventure.VaseStage == 0 ? 2700 : 2800;
                    Adventure.VaseStage++;
                    Adventure._packets.Clear();
                    Plants.Clear();
                    ClearPeas();
                    ClearSuns();
                    Adventure.RakeRow = -1;
                    Adventure.RakeAttracting = false;
                    Adventure.RakeTriggered = false;
                    Adventure.RakeAge = 0f;
                    Adventure._rakeDamageDone = false;
                    Adventure.Notice(AdventureNoticeKind.VaseInterludeStarted, message);
                    if (!Adventure.VaseInterludeDialogueEnabled)
                    {
                        PopulateAdventureVaseInterlude(message == 2700 ? 2702 : 2801);
                        FinishAdventureVaseInterlude();
                    }
                }
            }
            else if (ModeSession == null && Adventure.Level.Mode == AdventureMode.Vasebreaker && Adventure._vases.Count == 0 && Zombies.Count == 0 && Adventure.VaseStage < 2)
            {
                // Board::UpdateLevelEndSequence starts at 500 and presents Dave at 300.
                Adventure.VaseInterludeActive = true;
                Adventure._vaseInterludeTicks = 200;
                Adventure._vaseInterludePopulated = false;
            }

            for (var i = Adventure._nuts.Count - 1; i >= 0; i--)
            {
                var nut = Adventure._nuts[i];
                if (AdvanceAdventureBowlingNative(nut, dt, false))
                {
                    Adventure._nuts.RemoveAt(i);
                }
            }
        }

        /// <summary>Populates the next adventure vase stage at the original dialogue entry.</summary>
        /// <param name="messageIndex">The original Crazy Dave message being presented.</param>
        /// <returns>Whether this entry populated the next stage.</returns>
        public bool PopulateAdventureVaseInterlude(int messageIndex)
        {
            if (!Adventure.VaseInterludeActive || Adventure._vaseInterludeTicks != 0 || Adventure._vaseInterludePopulated
                || (Adventure.VaseStage == 1 ? messageIndex != 2702 : Adventure.VaseStage == 2 ? messageIndex != 2801 : true))
            {
                return false;
            }

            Adventure._vaseInterludePopulated = true;
            InitializeVaseStage();
            InitializeAdventurePurchases();
            return true;
        }

        /// <summary>Finishes the vase transition after Dave has departed.</summary>
        /// <returns>Whether an already populated transition was finished.</returns>
        public bool FinishAdventureVaseInterlude()
        {
            if (!Adventure.VaseInterludeActive || !Adventure._vaseInterludePopulated)
            {
                return false;
            }

            Adventure.VaseInterludeActive = false;
            return true;
        }

        /// <summary>Performs advance adventure boss.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureBoss(float dt)
        {
            var a = Adventure;
            if (a.BossHealth <= 0)
            {
                a.BossHeadVulnerable = false;
                return;
            }

            a._bossTimer -= dt;
            if (a.BossHeadVulnerable)
            {
                if (a._bossTimer <= 0)
                {
                    a.BossHeadVulnerable = false;
                    a._bossTimer = 8f;
                }
            }
            else if (a._bossTimer <= 0)
            {
                a._bossCycle++;
                if (a._bossCycle % 3 == 0)
                {
                    a.BossHeadVulnerable = true;
                    a._bossTimer = 14f;
                    a._balls.Add(new AdventureBossBall { Row = _random.Next(5), X = 9f, IsIce = a._bossCycle % 2 == 0 });
                    a.Notice(AdventureNoticeKind.BossAttack, a._bossCycle % 2 == 0 ? 1 : 0);
                }
                else if (a._bossCycle % 5 == 0)
                {
                    int row = _random.Next(4), col = _random.Next(2, 7);
                    foreach (Plant p in new PooledSnapshot<Plant>(Plants))
                    {
                        if (Math.Abs(p.Row - row) <= 1 && Math.Abs(p.Column - col) <= 1)
                        {
                            RemoveAdventurePlantAndStack(p);
                        }
                    }

                    a.Notice(AdventureNoticeKind.BossAttack, 2, row, col);
                    a._bossTimer = 10f;
                }
                else
                {
                    var count = 2 + a._bossCycle / 8;
                    for (var i = 0; i < Math.Min(6, count); i++)
                    {
                        var kind = a.BossHealth < 15000f && i == 0 ? ZombieKind.Gargantuar : a.BossHealth < 30000f && i == 0 ? ZombieKind.Catapult : i % 3 == 0 ? ZombieKind.Buckethead : i % 3 == 1 ? ZombieKind.Conehead : ZombieKind.Normal;
                        SpawnAdventureZombie(kind, _random.Next(5), 9.5f + i * 0.2f, a._bossCycle);
                    }

                    a.Notice(AdventureNoticeKind.BossAttack, 3);
                    a._bossTimer = 10f;
                }
            }

            for (var i = a._balls.Count - 1; i >= 0; i--)
            {
                var ball = a._balls[i];
                ball.X -= 1.25f * dt;
                foreach (Plant p in new PooledSnapshot<Plant>(Plants))
                {
                    if (p.Row == ball.Row && Math.Abs(p.Column + 0.5f - ball.X) < 0.4f)
                    {
                        RemoveAdventurePlantAndStack(p);
                    }
                }

                if (ball.X < 0)
                {
                    a._balls.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs damage adventure boss.</summary>
        /// <param name = "damage">The damage.</param>
        private void DamageAdventureBoss(float damage)
        {
            if (Adventure.Level.Mode != AdventureMode.Boss || Adventure.BossHealth <= 0 || !Adventure.BossHeadVulnerable)
            {
                return;
            }

            Adventure.BossHealth = Math.Max(0, Adventure.BossHealth - damage);
            Raise(GameEvent.ZombieHit);
            if (Adventure.BossHealth <= 0)
            {
                Adventure._balls.Clear();
                Adventure.Notice(AdventureNoticeKind.BossAttack, 4);
            }
        }

        // PC Board::CanTakeSunMoney includes suns already flying to the bank (0x4275A0/4276D0).
        // Keep the existing nonnegative pickup-rule contract by representing native negative
        // mSunMoney as visible money plus a reservation settled by the arriving pickups.
        /// <summary>Stores adventure sun reservation.</summary>
        private int _adventureSunReservation;
        /// <summary>Gets or sets adventure sun available.</summary>
        public int AdventureSunAvailable
        {
            get
            {
                var total = SunAmount - _adventureSunReservation;
                if (IsAdventure)
                {
                    foreach (global::PlantsVsZombies.Simulation.Sun sun in Suns)
                    {
                        if (sun.PresentationNative && sun.PresentationPickup.Collecting && !sun.PresentationPickup.Dead)
                        {
                            total += sun.Value;
                        }
                    }
                }

                return total;
            }
        }

        /// <summary>Performs spend adventure sun.</summary>
        /// <param name = "cost">The cost.</param>
        /// <returns>The resulting spend adventure sun.</returns>
        private bool SpendAdventureSun(int cost)
        {
            if (cost < 0 || AdventureSunAvailable < cost)
            {
                return false;
            }

            var visible = Math.Min(SunAmount, cost);
            SunAmount -= visible;
            _adventureSunReservation += cost - visible;
            return true;
        }

        /// <summary>Performs settle adventure sun reservation.</summary>
        private void SettleAdventureSunReservation()
        {
            var paid = Math.Min(SunAmount, _adventureSunReservation);
            SunAmount -= paid;
            _adventureSunReservation -= paid;
        }

        /// <summary>Performs initialize adventure seed access.</summary>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        private void InitializeAdventureSeedAccess()
        {
            var values = new List<PlantKind>();
            foreach (var k in Adventure.Level.UnlockedSeeds)
            {
                values.Add(k);
            }

            var access = Rules.AdventureSeedAccess;
            if (access != null)
            {
                foreach (var k in access.AdditionalSeeds)
                {
                    if (!values.Contains(k))
                    {
                        values.Add(k);
                    }
                }

                if (access.ImitaterTarget.HasValue)
                {
                    if (!ContainsSeed(Adventure.Level.UnlockedSeeds, access.ImitaterTarget.Value))
                    {
                        throw new ArgumentException("Imitater target is not yet earned on this level.");
                    }

                    Adventure.ImitaterTarget = access.ImitaterTarget;
                }
            }

            Adventure.AvailableSeeds = Array.AsReadOnly(values.ToArray());
        }

        /// <summary>Performs set adventure imitater target.</summary>
        /// <param name = "target">The target.</param>
        /// <returns>The resulting set adventure imitater target.</returns>
        public bool SetAdventureImitaterTarget(PlantKind target)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.ChoosingSeeds || !ContainsSeed(Adventure.AvailableSeeds, PlantKind.Imitater) || (int)target < 0 || (int)target > 39 || !ContainsSeed(Adventure.Level.UnlockedSeeds, target))
            {
                return false;
            }

            Adventure.ImitaterTarget = target;
            return true;
        }

        /// <summary>Performs adventure seed cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure seed cost.</returns>
        public int AdventureSeedCost(PlantKind kind)
        {
            return Cost(ResolveAdventureSeed(kind));
        }

        /// <summary>Performs adventure seed recharge time.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure seed recharge time.</returns>
        public float AdventureSeedRechargeTime(PlantKind kind)
        {
            return RechargeTime(ResolveAdventureSeed(kind));
        }

        /// <summary>Performs resolve adventure seed.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting resolve adventure seed.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private PlantKind ResolveAdventureSeed(PlantKind kind)
        {
            if (kind != PlantKind.Imitater)
            {
                return kind;
            }

            if (!IsAdventure || !Adventure.ImitaterTarget.HasValue)
            {
                throw new InvalidOperationException("Select an earned basic plant as the Imitater target first.");
            }

            return Adventure.ImitaterTarget.Value;
        }

        /// <summary>Performs upgrade prerequisite.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting upgrade prerequisite.</returns>
        private static PlantKind? UpgradePrerequisite(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.GatlingPea:
                    return PlantKind.Repeater;
                case PlantKind.TwinSunflower:
                    return PlantKind.Sunflower;
                case PlantKind.GloomShroom:
                    return PlantKind.FumeShroom;
                case PlantKind.Cattail:
                    return PlantKind.LilyPad;
                case PlantKind.WinterMelon:
                    return PlantKind.MelonPult;
                case PlantKind.GoldMagnet:
                    return PlantKind.MagnetShroom;
                case PlantKind.Spikerock:
                    return PlantKind.Spikeweed;
                case PlantKind.CobCannon:
                    return PlantKind.KernelPult;
                default:
                    return null;
            }
        }

        /// <summary>Performs check adventure upgrade.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting check adventure upgrade.</returns>
        private AdventurePlantingFailure CheckAdventureUpgrade(PlantKind kind, int row, int column)
        {
            var prerequisite = UpgradePrerequisite(kind);
            if (!prerequisite.HasValue)
            {
                return AdventurePlantingFailure.NeedsUpgrade;
            }

            var l = Adventure.Level;
            if (kind == PlantKind.Cattail)
            {
                var lily = AdventurePlantAt(row, column, PlantLayer.Support);
                return l.IsWaterRow(row) && lily != null && lily.Kind == PlantKind.LilyPad && AdventurePlantAt(row, column, PlantLayer.Main) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.NeedsUpgrade;
            }

            var source = AdventurePlantAt(row, column, PlantLayer.Main);
            if (source == null || source.Column != column || source.Kind != prerequisite.Value)
            {
                return AdventurePlantingFailure.NeedsUpgrade;
            }

            if (kind == PlantKind.Spikerock && (l.IsWaterRow(row) || l.HasRoof || AdventurePlantAt(row, column, PlantLayer.Support) != null))
            {
                return AdventurePlantingFailure.RoofRestriction;
            }

            if (kind == PlantKind.CobCannon)
            {
                if (column >= Columns - 1)
                {
                    return AdventurePlantingFailure.CannonFootprint;
                }

                var right = AdventurePlantAt(row, column + 1, PlantLayer.Main);
                if (right == null || right.Kind != PlantKind.KernelPult || right.Column != column + 1 || AdventurePlantAt(row, column, PlantLayer.Armor) != null || AdventurePlantAt(row, column + 1, PlantLayer.Armor) != null)
                {
                    return AdventurePlantingFailure.NeedsUpgrade;
                }

                Plant leftPot = AdventurePlantAt(row, column, PlantLayer.Support), rightPot = AdventurePlantAt(row, column + 1, PlantLayer.Support);
                bool a = leftPot != null && leftPot.Kind == PlantKind.FlowerPot, b = rightPot != null && rightPot.Kind == PlantKind.FlowerPot;
                if (a != b)
                {
                    return AdventurePlantingFailure.CannonFootprint;
                }
            }

            return AdventurePlantingFailure.None;
        }

        /// <summary>Performs place adventure upgrade.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting place adventure upgrade.</returns>
        private Plant PlaceAdventureUpgrade(PlantKind kind, int row, int column)
        {
            if (kind == PlantKind.Imitater)
            {
                var target = ResolveAdventureSeed(kind);
                var mimic = new Plant
                {
                    Id = ++Adventure._plantId,
                    Kind = kind,
                    Row = row,
                    Column = column,
                    Health = 300f,
                    Layer = target == PlantKind.CoffeeBean ? PlantLayer.Effect : LayerFor(target),
                    ImitaterTarget = target,
                    ImitaterTransformRemaining = 3.2f,
                    ActionTimer = float.MaxValue
                };
                Plants.Add(mimic);
                Raise(GameEvent.Planted);
                return mimic;
            }

            var source = AdventurePlantAt(row, column, kind == PlantKind.Cattail ? PlantLayer.Support : PlantLayer.Main);
            bool asleep = source != null && source.Asleep, coffee = source != null && source.AwakeByCoffee;
            var wake = source == null ? 0 : source.WakeTimer;
            if (source != null)
            {
                Plants.Remove(source);
            }

            if (kind == PlantKind.CobCannon)
            {
                var right = AdventurePlantAt(row, column + 1, PlantLayer.Main);
                if (right != null)
                {
                    Plants.Remove(right);
                }
            }

            var result = new Plant
            {
                Id = ++Adventure._plantId,
                Kind = kind,
                Row = row,
                Column = column,
                Layer = PlantLayer.Main,
                Health = kind == PlantKind.Spikerock ? 450f : 300f,
                ColumnSpan = kind == PlantKind.CobCannon ? 2 : 1,
                Asleep = kind == PlantKind.GloomShroom && asleep,
                AwakeByCoffee = kind == PlantKind.GloomShroom && coffee,
                WakeTimer = kind == PlantKind.GloomShroom ? wake : 0,
                ActionTimer = kind == PlantKind.TwinSunflower ? Math.Max(0.01f, source == null ? 5f : source.ActionTimer) : 0.5f,
                CannonPhase = CobCannonPhase.Arming,
                CannonReloadRemaining = kind == PlantKind.CobCannon ? 5f : 0
            };
            InitializeAdventureNativePlantTiming(result);
            Plants.Add(result);
            Raise(GameEvent.Planted);
            return result;
        }

        // Native counters are interpreted at the replica's 100 Hz. Animation/loading/flight durations are approximations.
        /// <summary>Stores adventure cob firing seconds.</summary>
        public const float AdventureCobFiringSeconds = 2.06f;
        /// <summary>Stores adventure cob loading seconds.</summary>
        public const float AdventureCobLoadingSeconds = 2.5f;
        /// <summary>Stores adventure cob flight seconds.</summary>
        public const float AdventureCobFlightSeconds = AdventurePresentationCobFlightSeconds;
        /// <summary>Performs advance adventure upgrade plant.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting advance adventure upgrade plant.</returns>
        private bool AdvanceAdventureUpgradePlant(Plant p, float dt)
        {
            if (p.Kind == PlantKind.Imitater)
            {
                p.ImitaterTransformRemaining = Math.Max(0, p.ImitaterTransformRemaining - dt);
                if (p.ImitaterTransformRemaining <= 0.000001f)
                {
                    MorphAdventureImitater(p);
                }

                return true;
            }

            if (p.Kind == PlantKind.CobCannon)
            {
                AdvanceAdventureCannon(p, dt);
                return true;
            }

            if (p.Kind == PlantKind.GatlingPea || p.Kind == PlantKind.GloomShroom || p.Kind == PlantKind.Cattail)
            {
                if (p.UpgradeBurstTicks > 0)
                {
                    var tick = --p.UpgradeBurstTicks;
                    if (p.Kind == PlantKind.GatlingPea && (tick == 68 || tick == 51 || tick == 35 || tick == 18))
                    {
                        if (!Adventure.Level.HasRoof || p.Column >= 5)
                        {
                            FireAdventurePea(p, p.Row, false, 20f);
                        }
                    }
                    else if (p.Kind == PlantKind.GloomShroom && (tick == 126 || tick == 98 || tick == 70 || tick == 42))
                    {
                        foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
                        {
                            if (IsUpgradeAreaTarget(z, p))
                            {
                                HitAdventureZombie(z, 20f, true);
                            }
                        }

                        p.ActionCount++;
                        Raise(GameEvent.Shoot);
                    }
                    else if (p.Kind == PlantKind.Cattail && (tick == 19 || tick == 1))
                    {
                        var target = FindAdventureCattailTarget(p.Column + 0.5f, p.Row);
                        if (target != null)
                        {
                            var dart = FireAdventurePea(p, p.Row, false, 20f);
                            dart.Homing = true;
                            dart.TargetZombieId = target.Id;
                            dart.HitsAir = true;
                        }
                    }
                }

                if (p.ActionTimer <= 0.000001f)
                {
                    p.ActionTimer = (DefinitionFor(p.Kind).ActionPeriodRaw - _random.Next(15)) * 0.01f;
                    var target = p.Kind == PlantKind.GatlingPea ? FindAdventureTarget(p.Row, p.Column + 0.4f, 20f, false) != null : p.Kind == PlantKind.Cattail ? FindAdventureCattailTarget(p.Column + 0.5f, p.Row) != null : HasUpgradeAreaTarget(p);
                    if (target)
                    {
                        p.UpgradeBurstTicks = p.Kind == PlantKind.GloomShroom ? 200 : p.Kind == PlantKind.GatlingPea ? 100 : 50;
                        p.ShootAge = 0;
                    }
                }

                return true;
            }

            if (p.Kind == PlantKind.TwinSunflower)
            {
                if (p.ActionTimer <= 0.000001f)
                {
                    Suns.Add(CreateAdventurePresentationSun(p, 25));
                    Suns.Add(CreateAdventurePresentationSun(p, 25));
                    p.ActionCount++;
                    p.ActionTimer = (2350 + _random.Next(151)) * 0.01f;
                }

                return true;
            }

            if (p.Kind == PlantKind.Spikerock)
            {
                if (p.ActionTimer <= 0.000001f)
                {
                    foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
                    {
                        if (!z.Hypnotized && !z.Airborne && !z.Submerged && z.DropRemaining <= 0 && z.Kind != ZombieKind.Bungee && z.Row == p.Row && Math.Abs(z.X - (p.Column + 0.5f)) < 0.6f)
                        {
                            if (z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult)
                            {
                                HitAdventureZombie(z, 1800f, true);
                                p.Health -= 50f;
                                p.VehicleHitsTaken++;
                                if (p.Health <= 0)
                                {
                                    RemoveAdventurePlantAndStack(p);
                                    break;
                                }
                            }
                            else
                            {
                                HitAdventureZombie(z, 20f, true);
                            }
                        }
                    }

                    p.ActionTimer = 0.5f;
                }

                return true;
            }

            if (p.Kind == PlantKind.GoldMagnet)
            {
                if (p.ActionTimer <= 0.000001f)
                {
                    var choices = new List<AdventureCoin>();
                    foreach (var coin in Adventure._coins)
                    {
                        if (!coin.IsBeingCollected && coin.Age >= 0.5f && coin.ValueUnits > 0)
                        {
                            choices.Add(coin);
                        }
                    }

                    for (var i = 1; i < choices.Count; i++)
                    {
                        var coin = choices[i];
                        var distance = CoinDistance(coin, p);
                        var index = i - 1;
                        while (index >= 0 && CoinDistance(choices[index], p) > distance)
                        {
                            choices[index + 1] = choices[index];
                            index--;
                        }

                        choices[index + 1] = coin;
                    }
                    var count = Math.Min(5, choices.Count);
                    for (var i = 0; i < count; i++)
                    {
                        choices[i].IsBeingCollected = true;
                        choices[i].CollectorPlantId = p.Id;
                        choices[i]._collectionRemaining = 0.5f;
                    }

                    if (count > 0)
                    {
                        p.ActionCount++;
                        p.ActionTimer = 2f + _random.Next(101) * 0.01f;
                    }
                    else
                    {
                        p.ActionTimer = 0.5f;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>Performs coin distance.</summary>
        /// <param name = "c">The c.</param>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting coin distance.</returns>
        private static float CoinDistance(AdventureCoin c, Plant p)
        {
            float x = c.X - (p.Column + 0.5f), y = c.Row - p.Row;
            return x * x + y * y;
        }

        /// <summary>Performs is upgrade area target.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting is upgrade area target.</returns>
        private static bool IsUpgradeAreaTarget(Zombie z, Plant p)
        {
            return !z.Hypnotized && AdventureZombieTargetingRules.CanBeAutoTargeted(z, p.Kind) && !z.Airborne && !z.Submerged && z.DropRemaining <= 0 && z.Kind != ZombieKind.Bungee && Math.Abs(z.Row - p.Row) <= 1 && Math.Abs(z.X - (p.Column + 0.5f)) <= 1.5f;
        }

        /// <summary>Performs find adventure cattail target.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        /// <returns>The resulting find adventure cattail target.</returns>
        private Zombie? FindAdventureCattailTarget(float x, float row)
        {
            Zombie? best = null;
            var distance = float.MaxValue;
            foreach (Zombie z in Zombies)
            {
                if (z.Hypnotized || !AdventureZombieTargetingRules.CanBeAutoTargeted(z, PlantKind.Cattail) || z.Submerged || z.DropRemaining > 0 || z.Kind == ZombieKind.Bungee || z.X < 0 || z.X > 12f)
                {
                    continue;
                }

                float dx = z.X - x, dy = z.Row - row, d = dx * dx + dy * dy;
                if (best == null || z.Airborne && !best.Airborne || z.Airborne == best.Airborne && d < distance)
                {
                    best = z;
                    distance = d;
                }
            }

            return best;
        }

        /// <summary>Performs update adventure homing velocity.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "dt">The dt.</param>
        private void UpdateAdventureHomingVelocity(Pea p, float dt)
        {
            if (!p.Homing)
            {
                return;
            }

            Zombie? target = null;
            foreach (var zombie in Zombies)
            {
                if (zombie.Id == p.TargetZombieId && !zombie.Hypnotized && AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, PlantKind.Cattail) && !zombie.Submerged && zombie.DropRemaining <= 0)
                {
                    target = zombie;
                    break;
                }
            }
            if (target == null)
            {
                target = FindAdventureCattailTarget(p.X, p.FlightRow);
            }

            if (target == null)
            {
                p.TargetZombieId = 0;
                return;
            }

            p.TargetZombieId = target.Id;
            float dx = target.X - p.X, dy = target.Row - p.FlightRow, d = (float)Math.Sqrt(dx * dx + dy * dy);
            var speed = d <= 0.000001f ? 0 : Math.Min(5f, d / dt);
            p.VelocityX = d <= 0.000001f ? 0 : dx / d * speed;
            p.VelocityY = d <= 0.000001f ? 0 : dy / d * speed;
        }

        /// <summary>Performs morph adventure imitater.</summary>
        /// <param name = "p">The p.</param>
        private void MorphAdventureImitater(Plant p)
        {
            var target = p.ImitaterTarget;
            Plants.Remove(p);
            if (!target.HasValue)
            {
                return;
            }

            if (target.Value == PlantKind.CoffeeBean)
            {
                var main = AdventurePlantAt(p.Row, p.Column, PlantLayer.Main);
                if (main != null && main.Asleep)
                {
                    PlaceAdventurePlant(target.Value, p.Row, p.Column);
                }
            }
            else
            {
                var transformed = RequiredPlaceAdventurePlant(target.Value, p.Row, p.Column);
                transformed.Id = p.Id;
                transformed.IsImitated = true;
                transformed.ImitaterTarget = target;
            }

            Adventure.Notice(AdventureNoticeKind.ImitaterMorphed, (int)target.Value, p.Row, p.Column);
        }

        /// <summary>Performs try aim adventure cob cannon.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try aim adventure cob cannon.</returns>
        public bool TryAimAdventureCobCannon(int row, int column)
        {
            var p = IsAdventure ? AdventurePlantAt(row, column, PlantLayer.Main) : null;
            return p != null && TryAimAdventureCobCannon(p.Id);
        }

        /// <summary>Performs try aim adventure cob cannon.</summary>
        /// <param name = "plantId">The plant id.</param>
        /// <returns>The resulting try aim adventure cob cannon.</returns>
        public bool TryAimAdventureCobCannon(int plantId)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing)
            {
                return false;
            }

            var cannon = Plants.Find(p => p.Id == plantId && p.Kind == PlantKind.CobCannon);
            if (cannon == null || cannon.CannonPhase != CobCannonPhase.Ready)
            {
                return false;
            }

            CancelAdventureCobCannonAim();
            cannon.CannonPhase = CobCannonPhase.Aiming;
            Adventure.SelectedCobCannonId = plantId;
            Adventure.Notice(AdventureNoticeKind.CobAimStarted, plantId, cannon.Row, cannon.Column);
            return true;
        }

        /// <summary>Performs cancel adventure cob cannon aim.</summary>
        /// <returns>The resulting cancel adventure cob cannon aim.</returns>
        public bool CancelAdventureCobCannonAim()
        {
            if (!IsAdventure || !Adventure.SelectedCobCannonId.HasValue)
            {
                return false;
            }

            var p = Plants.Find(c => c.Id == Adventure.SelectedCobCannonId.Value && c.Kind == PlantKind.CobCannon);
            if (p != null && p.CannonPhase == CobCannonPhase.Aiming)
            {
                p.CannonPhase = CobCannonPhase.Ready;
            }

            Adventure.SelectedCobCannonId = null;
            return true;
        }

        /// <summary>Performs try fire adventure cob cannon.</summary>
        /// <param name = "targetX">The target x.</param>
        /// <param name = "targetRow">The target row.</param>
        /// <returns>The resulting try fire adventure cob cannon.</returns>
        public bool TryFireAdventureCobCannon(float targetX, float targetRow)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing || !Adventure.SelectedCobCannonId.HasValue || float.IsNaN(targetX) || float.IsInfinity(targetX) || float.IsNaN(targetRow) || float.IsInfinity(targetRow) || targetX < 0 || targetX >= Columns || targetRow < 0 || targetRow >= RowCount)
            {
                return false;
            }

            var p = Plants.Find(c => c.Id == Adventure.SelectedCobCannonId.Value && c.Kind == PlantKind.CobCannon && c.CannonPhase == CobCannonPhase.Aiming);
            if (p == null)
            {
                return false;
            }

            p.CannonTargetX = targetX;
            p.CannonTargetRow = targetRow;
            p.CannonPhase = CobCannonPhase.Firing;
            p.CannonReloadRemaining = AdventureCobFiringSeconds;
            Adventure.SelectedCobCannonId = null;
            return true;
        }

        /// <summary>Performs try fire adventure cob cannon at cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try fire adventure cob cannon at cell.</returns>
        public bool TryFireAdventureCobCannonAtCell(int row, int column)
        {
            return TryFireAdventureCobCannon(column + 0.5f, row);
        }

        /// <summary>Performs advance adventure cannon.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureCannon(Plant p, float dt)
        {
            if (p.CannonPhase == CobCannonPhase.Ready || p.CannonPhase == CobCannonPhase.Aiming)
            {
                return;
            }

            p.CannonReloadRemaining = Math.Max(0, p.CannonReloadRemaining - dt);
            if (p.CannonReloadRemaining > 0.000001f)
            {
                return;
            }

            if (p.CannonPhase == CobCannonPhase.Arming || p.CannonPhase == CobCannonPhase.Reloading)
            {
                p.CannonPhase = CobCannonPhase.Loading;
                p.CannonReloadRemaining = AdventureCobLoadingSeconds;
            }
            else if (p.CannonPhase == CobCannonPhase.Loading)
            {
                p.CannonPhase = CobCannonPhase.Ready;
                Adventure.Notice(AdventureNoticeKind.CobReady, p.Id, p.Row, p.Column);
            }
            else if (p.CannonPhase == CobCannonPhase.Firing)
            {
                Adventure._cobShells.Add(new AdventureCobShell {
                    SourcePlantId = p.Id,
                    TargetX = p.CannonTargetX,
                    TargetRow = p.CannonTargetRow,
                    LaunchX = p.Column + 1f,
                    LaunchRow = p.Row,
                    FlightRemaining = AdventureCobFlightSeconds
                });
                p.CannonPhase = CobCannonPhase.Reloading;
                p.CannonReloadRemaining = 30f;
                Raise(GameEvent.Shoot);
                Adventure.Notice(AdventureNoticeKind.CobFired, p.Id, p.Row, p.Column);
            }
        }

        /// <summary>Creates a board pickup using the original currency units and logical pixel position.</summary>
        /// <param name="kind">The currency or loot type to create.</param>
        /// <param name="x">The pickup's horizontal lawn coordinate.</param>
        /// <param name="row">The pickup's lawn row.</param>
        /// <returns>The new pickup, or null when the board or coordinates cannot accept it.</returns>
        public AdventureCoin? SpawnAdventureCoin(AdventureCoinKind kind, float x, float row)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing || !Enum.IsDefined(typeof(AdventureCoinKind), kind) || float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(row) || float.IsInfinity(row) || x < 0 || x >= Columns || row < 0 || row >= RowCount)
            {
                return null;
            }

            var coin = new AdventureCoin
            {
                Id = ++Adventure._coinId,
                Kind = kind,
                X = x,
                Row = row
            };
            var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            var slope = Adventure.Level.HasRoof ? Math.Max(0f, 5f - x) * 20f - 10f : 0f;
            coin.PresentationX = coin.PresentationPreviousX = 40f + x * 80f - 20f;
            coin.PresentationY = coin.PresentationPreviousY = 80f + (int)row * pitch + slope + 36f;
            Adventure._coins.Add(coin);
            Adventure.Notice(AdventureNoticeKind.CoinCreated, coin.Id, (int)row, (int)x);
            return coin;
        }

        /// <summary>Calls <see cref="SpawnAdventureCoin"/> where a result must exist, failing with a descriptive exception instead of returning null.</summary>
        /// <param name="kind">The <c>kind</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <param name="x">The <c>x</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <param name="row">The <c>row</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <returns>The non-null spawn adventure coin result.</returns>
        /// <exception cref="System.InvalidOperationException">The method returned no result.</exception>
        public AdventureCoin RequiredSpawnAdventureCoin(AdventureCoinKind kind, float x, float row) => SpawnAdventureCoin(kind, x, row) ?? throw new System.InvalidOperationException("'SpawnAdventureCoin' returned no result.");

        /// <summary>Starts the original manual money collection flight or commits noncurrency loot.</summary>
        /// <param name="coin">The live pickup clicked by the player.</param>
        /// <returns>Whether collection began; currency is persisted and credited when its flight arrives.</returns>
        public bool CollectAdventureCoin(AdventureCoin? coin)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing || coin == null || coin.IsBeingCollected || !Adventure._coins.Contains(coin))
            {
                return false;
            }

            if (coin.ValueUnits == 0)
            {
                return FinishAdventureCoin(coin);
            }

            coin.IsBeingCollected = true;
            coin.CollectorPlantId = 0;
            coin.PersistencePending = false;
            Adventure.Notice(AdventureNoticeKind.CoinCollectionStarted, (int)coin.Kind);
            return true;
        }

        /// <summary>Performs finish adventure coin.</summary>
        /// <param name = "coin">The coin.</param>
        /// <returns>The resulting finish adventure coin.</returns>
        private bool FinishAdventureCoin(AdventureCoin coin)
        {
            if (!Adventure._coins.Contains(coin))
            {
                return false;
            }

            if (coin.ValueUnits == 0 && (Rules.AdventureLootCommit == null || !Rules.AdventureLootCommit(coin)))
            {
                coin.PersistencePending = true;
                Adventure.Notice(AdventureNoticeKind.CoinCollectionFailed, coin.Id);
                return false;
            }

            var total = (int)Math.Min(int.MaxValue, (long)Adventure.MoneyCollectedUnits + coin.ValueUnits);
            if (Rules.AdventureCoinCommit != null && !Rules.AdventureCoinCommit(total))
            {
                coin.PersistencePending = true;
                coin.IsBeingCollected = false;
                coin.CollectorPlantId = 0;
                Adventure.Notice(AdventureNoticeKind.CoinCollectionFailed, coin.Id);
                return false;
            }

            Adventure._coins.Remove(coin);
            Adventure.MoneyCollectedUnits = (int)Math.Min(int.MaxValue, (long)Adventure.MoneyCollectedUnits + coin.ValueUnits);
            Adventure.Notice(AdventureNoticeKind.CoinCollected, coin.ValueUnits);
            return true;
        }

        /// <summary>Advances existing money pickups through combat and the visible reward sequence.</summary>
        /// <param name="dt">The duration of one native simulation tick in seconds.</param>
        private void AdvanceAdventureCoins(float dt)
        {
            for (var i = Adventure._coins.Count - 1; i >= 0; i--)
            {
                var coin = Adventure._coins[i];
                coin.Age += dt;
                if (coin.IsBeingCollected)
                {
                    if (coin.CollectorPlantId == 0)
                    {
                        if (coin.TickCollection())
                        {
                            FinishAdventureCoin(coin);
                        }

                        continue;
                    }

                    var collector = FindPlantById(coin.CollectorPlantId);
                    if (collector == null || collector.Kind != PlantKind.GoldMagnet)
                    {
                        coin.IsBeingCollected = false;
                        coin.CollectorPlantId = 0;
                        continue;
                    }

                    coin._collectionRemaining -= dt;
                    if (coin._collectionRemaining <= 0.000001f)
                    {
                        FinishAdventureCoin(coin);
                    }
                }
                else if (coin.Age >= 30f && !coin.PersistencePending)
                {
                    Adventure._coins.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs advance adventure upgrade world.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureUpgradeWorld(float dt)
        {
            if (Adventure.SelectedCobCannonId.HasValue && FindPlantById(Adventure.SelectedCobCannonId.Value) == null)
            {
                Adventure.SelectedCobCannonId = null;
            }

            AdvanceAdventureCoins(dt);

            for (var i = Adventure._cobShells.Count - 1; i >= 0; i--)
            {
                var shell = Adventure._cobShells[i];
                shell.FlightRemaining -= dt;
                if (shell.FlightRemaining > 0.000001f)
                {
                    continue;
                }

                var row = (int)Math.Round(shell.TargetRow);
                BlastAdventure(shell.TargetX, row, 1.5f, 1, 1800f, true);
                AdventurePresentationCobImpact(shell.TargetX, shell.TargetRow);
                if (shell.TargetX >= 7.5f && Math.Abs(shell.TargetRow - 2f) <= 1f)
                {
                    DamageAdventureBoss(1800f);
                }

                Adventure._cobShells.RemoveAt(i);
                Raise(GameEvent.Explosion);
                Adventure.Notice(AdventureNoticeKind.CobImpact, shell.SourcePlantId, row, (int)shell.TargetX);
            }
        }

        /// <summary>Performs adventure zombie attack rectangle.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie attack rectangle.</returns>
        public NativeRect AdventureZombieAttackRectangle(Zombie z)
        {
            int kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind, phase = AdventureZombieFeedbackPhase(z);
            var x = (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f);
            var y = (int)AdventureBiteNativeY(z, x);
            var backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2);
            return global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.AttackRect(kind, phase, Adventure.Level.Mode == global::PlantsVsZombies.Adventure.AdventureMode.Bowling, backwards, x, y, z.PresentationBiteBodyY, z.PresentationBiteClipHeight, z.Kind == ZombieKind.Gargantuar ? 180 : 120);
        }

        /// <summary>Performs adventure bite native y.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "x">The x.</param>
        /// <returns>The resulting adventure bite native y.</returns>
        private float AdventureBiteNativeY(Zombie z, float x)
        {
            return 80f + z.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f) + (Adventure.Level.HasRoof ? Math.Max(0f, 5f - x / 80f) * 20f - 10f : 0f) - 30f - (z.Kind == ZombieKind.Balloon ? 30f : 0f) - (z.Kind == ZombieKind.Pogo && !z.UsedAbility ? 16f : 0f);
        }

        /// <summary>Performs adventure plant eating rectangle.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting adventure plant eating rectangle.</returns>
        public NativeRect AdventurePlantEatingRectangle(Plant p)
        {
            return global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.PlantRect((int)p.Kind, 40 + p.Column * 80, 80 + p.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85 : 100) + (Adventure.Level.HasRoof ? (int)(Math.Max(0f, 5f - p.Column) * 20f - 10f) : 0));
        }

        /// <summary>Performs adventure bite can target.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "p">The p.</param>
        /// <param name = "attack">The attack.</param>
        /// <param name = "priority">The priority.</param>
        /// <returns>The resulting adventure bite can target.</returns>
        private bool AdventureBiteCanTarget(Zombie z, Plant p, global::PlantsVsZombies.Adventure.AdventureBiteAttack attack, bool priority = true)
        {
            if (p.Health <= 0 || p.Row != z.Row || p.Layer == global::PlantsVsZombies.Adventure.PlantLayer.Effect || p.PresentationBiteNotOnGround || p.Kind == PlantKind.TangleKelp || p.Kind == PlantKind.Squash && p.PresentationBiteNativeState >= 5 && p.PresentationBiteNativeState <= 7)
            {
                return false;
            }

            if (Adventure.Level.Mode == global::PlantsVsZombies.Adventure.AdventureMode.Bowling && attack != global::PlantsVsZombies.Adventure.AdventureBiteAttack.Vault)
            {
                return false;
            }

            var pool = z.PresentationInPool ?? (Adventure.Level.IsWaterRow(z.Row) && z.X >= 0 && z.X < 9);
            if (!pool && Adventure.Level.IsWaterRow(p.Row))
            {
                return false;
            }

            if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 0)
            {
                return p.Kind == PlantKind.PotatoMine && p.MinePhase == MinePhase.Buried;
            }

            if (p.Kind == PlantKind.Spikeweed || p.Kind == PlantKind.Spikerock)
            {
                return z.Kind == ZombieKind.Gargantuar || z.Kind == ZombieKind.Zomboni || Adventure.Level.IsWaterRow(p.Row) || HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column);
            }

            if (attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.DriveOver)
            {
                if (p.Kind == PlantKind.CherryBomb || p.Kind == PlantKind.Jalapeno || p.Kind == PlantKind.Blover || p.Kind == PlantKind.Squash)
                {
                    return false;
                }

                if ((p.Kind == PlantKind.DoomShroom || p.Kind == PlantKind.IceShroom) && !p.Asleep)
                {
                    return false;
                }
            }

            if (z.Kind == ZombieKind.Ladder && z.ShieldHealth > 0)
            {
                var barrier = (p.Kind == PlantKind.Wallnut || p.Kind == PlantKind.TallNut || p.Kind == PlantKind.Pumpkin) && !p.HasLadder;
                if (attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.Chew && barrier)
                {
                    return false;
                }

                if (attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.Ladder && !barrier)
                {
                    return false;
                }
            }

            if (priority && (attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.Chew || attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.Vault))
            {
                foreach (var q in Plants)
                {
                    if (q != p && q.Row == p.Row && q.Column == p.Column && q.Layer > p.Layer && (attack != global::PlantsVsZombies.Adventure.AdventureBiteAttack.Vault || q.Layer == global::PlantsVsZombies.Adventure.PlantLayer.Main) && AdventureBiteCanTarget(z, q, attack, false))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Performs find adventure native plant target.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "attack">The attack.</param>
        /// <returns>The resulting find adventure native plant target.</returns>
        private Plant? FindAdventureNativePlantTarget(Zombie z, global::PlantsVsZombies.Adventure.AdventureBiteAttack attack)
        {
            var box = AdventureZombieAttackRectangle(z);
            foreach (var p in Plants)
            {
                if (AdventureBiteCanTarget(z, p, attack) && global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.Reaches(box, AdventurePlantEatingRectangle(p)))
                {
                    return p;
                }
            }

            return null;
        }

        /// <summary>Performs adventure native chewer.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure native chewer.</returns>
        private bool AdventureNativeChewer(Zombie z)
        {
            return global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.CanChew((int)z.Kind, AdventureZombieFeedbackPhase(z), z.PresentationNativeHeight, z.Airborne, z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility));
        }

        /// <summary>Performs adventure plant bite immune.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting adventure plant bite immune.</returns>
        private bool AdventurePlantBiteImmune(Plant p)
        {
            if (p.Kind == PlantKind.PotatoMine && p.MinePhase != MinePhase.Buried)
            {
                return true;
            }

            var state = p.PresentationBiteNativeState;
            return !p.Asleep && (p.Kind == PlantKind.CherryBomb || p.Kind == PlantKind.Jalapeno || p.Kind == PlantKind.DoomShroom || p.Kind == PlantKind.IceShroom || p.Kind == PlantKind.HypnoShroom || state == 47 || state == 48 || state == 3 || state == 4);
        }

        /// <summary>Performs advance adventure native eating.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "target">The target.</param>
        /// <param name = "dt">The dt.</param>
        /// <param name = "zombieTarget">The zombie target.</param>
        /// <returns>The resulting advance adventure native eating.</returns>
        private bool AdvanceAdventureNativeEating(Zombie z, Plant? target, float dt, bool zombieTarget = false)
        {
            if (z.Headless || !AdventureNativeChewer(z) || z.PresentationGarlicCounter >= 0)
            {
                return false;
            }

            var checks = global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.EatingChecks(z.Age, dt, z.ChillRemaining > 0);
            if (checks > 0)
            {
                z.Eating = target != null || zombieTarget;
            }

            if (!z.Eating)
            {
                z._presentationBiteClip = null;
                return false;
            }

            var clip = AdventurePresentationZombieAnimation(z);
            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data))
            {
                double cycle = z.PresentationAnimationAge * AdventurePresentationZombieRate(z) / data.Count;
                if (z._presentationBiteClip == clip)
                {
                    float first, second;
                    global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.ChewPhases((int)z.Kind, out first, out second);
                    if (z.FrozenRemaining <= 0 && !z.Headless && AdventureZombieFeedbackPhase(z) != 60 && (global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.Crossed(z._presentationBiteCycle, cycle, first) || global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.Crossed(z._presentationBiteCycle, cycle, second)))
                    {
                        AnimateAdventureChew(z, target, zombieTarget);
                    }
                }

                z._presentationBiteClip = clip;
                z._presentationBiteCycle = cycle;
            }

            if (target != null && checks > 0 && Plants.Contains(target) && !AdventurePlantBiteImmune(target) && z.PresentationGarlicCounter < 0)
            {
                target.Health -= 4f * checks;
                target.RecentlyEatenTicks = 50;
                if (target.Health <= 0)
                {
                    RequestAdventureZombieFoley(-1);
                    RemoveAdventurePlantAndStack(target);
                }
            }

            return true;
        }

        /// <summary>Performs animate adventure chew.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "target">The target.</param>
        /// <param name = "zombieTarget">The zombie target.</param>
        private void AnimateAdventureChew(Zombie z, Plant? target, bool zombieTarget)
        {
            if (target != null && target.Kind == PlantKind.HypnoShroom && !target.Asleep)
            {
                RequestAdventureZombieFoley(68);
                RemoveAdventurePlantAndStack(target);
                z.Hypnotized = true;
                z.Submerged = false;
                z.Eating = false;
                z.PresentationNativeVelocity = .17f;
                z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(.17f);
                Presentation.PlantBiteEffects.Add(new global::PlantsVsZombies.Adventure.AdventurePlantBiteEffect {
                    Id = ++Presentation._effectId,
                    ParticleId = 88,
                    Row = z.Row,
                    Kind = z.Kind,
                    NativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f),
                    NativeY = AdventureBiteNativeY(z, global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f)),
                    OffsetX = 60,
                    OffsetY = 40,
                    Seed = z.Id * 397 ^ Presentation._effectId
                });
                return;
            }

            RequestAdventureZombieFoley(global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.ChewFoley(target == null ? -1 : (int)target.Kind, z.Hypnotized));
            RecordAdventureChewEffect(z, target);
            if (target != null && target.Kind == PlantKind.Garlic)
            {
                z.PresentationGarlicCounter = 0;
                z._presentationGarlicPlant = target;
                z._presentationGarlicHeldAge = z.PresentationAnimationAge;
                z._presentationGarlicHeldHeadAge = z.PresentationPlantHeadAge;
            }
        }

        /// <summary>Performs advance adventure garlic bite.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting advance adventure garlic bite.</returns>
        private bool AdvanceAdventureGarlicBite(Zombie z, float dt)
        {
            if (z.PresentationGarlicCounter < 0)
            {
                return false;
            }

            var ticks = Math.Max(0, (int)Math.Round(dt * 100));
            for (var i = 0; i < ticks && z.PresentationGarlicCounter >= 0; i++)
            {
                var c = ++z.PresentationGarlicCounter;
                if (c > 20 && c < 170 && (z._presentationGarlicPlant == null || !Plants.Contains(z._presentationGarlicPlant)))
                {
                    c = 170;
                    z.PresentationGarlicCounter = c;
                    z.Eating = false;
                }

                if (c == 70)
                {
                    z.Eating = false;
                    AdventurePresentationZombieAnimation(z);
                    z._presentationGarlicHeldAge = 0;
                    if (Zombies.Count <= 5 || Zombies.Count <= 10 && _random.Next(2) == 0)
                    {
                        RequestAdventureZombieFoley(72);
                    }
                }

                if (c == 170)
                {
                    z.Eating = false;
                    var rows = new System.Collections.Generic.List<int>();
                    for (var row = Math.Max(0, z.Row - 1); row <= Math.Min(RowCount - 1, z.Row + 1); row++)
                    {
                        if (row != z.Row && Adventure.Level.IsLawnRow(row) && Adventure.Level.IsWaterRow(row) == Adventure.Level.IsWaterRow(z.Row))
                        {
                            rows.Add(row);
                        }
                    }

                    if (rows.Count > 0)
                    {
                        z.Row = rows[_random.Next(rows.Count)];
                    }

                    z._presentationBiteClip = null;
                    AdventurePresentationZombieAnimation(z);
                }

                if (c > 270)
                {
                    z.PresentationGarlicCounter = -1;
                    z._presentationGarlicPlant = null;
                }
            }

            if (z.PresentationGarlicCounter >= 0 && z.PresentationGarlicCounter < 170)
            {
                z.PresentationAnimationAge = z._presentationGarlicHeldAge;
                z.PresentationPlantHeadAge = z._presentationGarlicHeldHeadAge;
                global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
                var clip = AdventurePresentationZombieAnimation(z);
                if (global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data))
                {
                    var phase = z.PresentationAnimationAge * AdventurePresentationZombieRate(z) / data.Count;
                    z._presentationWalkLoops = (int)Math.Floor(phase);
                    z._presentationWalkPhase = phase - z._presentationWalkLoops;
                }

                return true;
            }

            return false;
        }

        /// <summary>Performs initialize adventure zombie corpse.</summary>
        /// <param name = "corpse">The corpse.</param>
        /// <returns>The resulting initialize adventure zombie corpse.</returns>
        private global::PlantsVsZombies.Adventure.AdventureZombieCorpse InitializeAdventureZombieCorpse(global::PlantsVsZombies.Adventure.AdventureZombieCorpse corpse)
        {
            int frames;
            var asset = AdventureZombiePresentationAsset(corpse.RequiredZombie);
            if (!global::PlantsVsZombies.Adventure.AdventureZombieDeathData.TryGet(asset, corpse.Animation, out frames))
            {
                corpse.Animation = "anim_death";
                if (!global::PlantsVsZombies.Adventure.AdventureZombieDeathData.TryGet(asset, corpse.Animation, out frames))
                {
                    corpse.Duration = 0f;
                    return corpse;
                }
            }

            corpse.AnimationDuration = global::PlantsVsZombies.Adventure.AdventureZombieDeathRules.AnimationSeconds(frames, corpse.Rate);
            corpse.InPool = corpse.RequiredZombie.PresentationInPool ?? (Adventure.Level.IsWaterRow(corpse.RequiredZombie.Row) && corpse.RequiredZombie.X >= 0 && corpse.RequiredZombie.X < 9);
            corpse.FadeDuration = global::PlantsVsZombies.Adventure.AdventureZombieDeathRules.FadeSeconds(corpse.InPool);
            corpse.Duration = corpse.AnimationDuration + corpse.FadeDuration;
            return corpse;
        }

        /// <summary>Performs advance adventure zombie corpse sound.</summary>
        /// <param name = "corpse">The corpse.</param>
        private void AdvanceAdventureZombieCorpseSound(global::PlantsVsZombies.Adventure.AdventureZombieCorpse corpse)
        {
            if (corpse.Burn != null || corpse.FallSoundPlayed || corpse.InPool)
            {
                return;
            }

            var kind = corpse.RequiredZombie.ModeHead >= 26 ? corpse.RequiredZombie.ModeHead : (int)corpse.RequiredZombie.Kind;
            var phase = global::PlantsVsZombies.Adventure.AdventureZombieDeathRules.FallPhase(kind, corpse.Animation);
            if (phase < 0 || corpse.Age < corpse.AnimationDuration * phase)
            {
                return;
            }

            corpse.FallSoundPlayed = true;
            RequestAdventureZombieFoley(28);
            if (kind == 23 || kind == 32)
            {
                RequestAdventureZombieFoley(40);
            }
        }

        /// <summary>Performs ensure adventure zombie equipment.</summary>
        /// <param name = "z">The z.</param>
        private void EnsureAdventureZombieEquipment(Zombie z)
        {
            if (z.PresentationMaxArmor <= 0 && z.ArmorHealth > 0)
            {
                z.PresentationMaxArmor = z.ArmorHealth;
            }

            if (z.PresentationMaxShield <= 0 && z.ShieldHealth > 0)
            {
                z.PresentationMaxShield = z.ShieldHealth;
            }
        }

        /// <summary>Performs adventure presentation zombie equipment damaged.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "flags">The flags.</param>
        private void AdventurePresentationZombieEquipmentDamaged(Zombie z, uint flags)
        {
            if (z.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned && z.Health <= 0)
            {
                return;
            }

            var suppressParts = (flags & 16U) != 0;
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            if (!suppressParts && z._presentationArmorBeforeHit > 0 && z.ArmorHealth <= 0)
            {
                int helm = global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.HelmType(z), id = global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.HelmParticle(helm);
                if (id >= 0)
                {
                    AddAdventureZombiePart(z, kind, id, global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.HelmTrack(helm), null);
                }
            }

            if (z._presentationShieldBeforeHit > 0 && z.ShieldHealth <= 0)
            {
                var id = z.Kind == ZombieKind.ScreenDoor ? 12 : z.Kind == ZombieKind.Newspaper ? 13 : z.Kind == ZombieKind.Ladder ? 54 : -1;
                if (id >= 0 && !suppressParts)
                {
                    AddAdventureZombiePart(z, kind, id, global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.ShieldTrack(z), null);
                }

                if (z.Kind == ZombieKind.Newspaper)
                {
                    // PC 0x54C7A0: 13 original gasp frames, 8fps, then phase 31.
                    z.PresentationNativePhase = 30;
                    z.PresentationNewspaperGaspRemaining = global::PlantsVsZombies.Adventure.AdventureZombieDeathRules.AnimationSeconds(13, 8f);
                    z.UsedAbility = true;
                    z.Eating = false;
                    if (!suppressParts && (flags & 1U) == 0)
                    {
                        RequestAdventureZombieFoley(67);
                        // PC 0x54C8C2 attaches reanimation115 at the zombie's (-11,0).
                        z.PresentationNewspaperSurpriseAge = 0f;
                    }
                }
            }
        }

        /// <summary>Gets or sets adventure zombie is izombie.</summary>
        private bool AdventureZombieIsIZombie
        {
            get
            {
                return ModeSession != null && ModeSession.Definition.Mechanic == global::PlantsVsZombies.Modes.ModeMechanic.IZombie;
            }
        }

        /// <summary>Performs pick adventure zombie native speed.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "phase">The phase.</param>
        private void PickAdventureZombieNativeSpeed(Zombie z, int phase)
        {
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var iZombie = AdventureZombieIsIZombie;
            var low = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.PickSpeed(kind, phase, iZombie, 0f);
            var high = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.PickSpeed(kind, phase, iZombie, 1f);
            z.PresentationNativeVelocity = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.PickSpeed(kind, phase, iZombie, low == high ? 0f : (float)_random.NextDouble());
            z._presentationSpeedPhase = phase;
            z._presentationSpeedKind = kind;
            z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
            z.PresentationMovementRate = -1f;
            z._presentationMovementAnimation = null;
        }

        /// <summary>Performs initialize adventure zombie native.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "rightEdge">The right edge.</param>
        private void InitializeAdventureZombieNative(Zombie z, bool rightEdge)
        {
            // Explicit grave/vase/IZombie/summoned positions remain at their requested world cell.
            if (rightEdge)
            {
                int r40 = _random.Next(40), r10 = 0;
                if (z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult || z.Kind == ZombieKind.Gargantuar)
                {
                    r10 = _random.Next(10);
                }

                z.X = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridX(global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.SpawnSourceX((int)z.Kind, r40, r10), z.IsSmall ? .5f : 1f);
            }

            PickAdventureZombieNativeSpeed(z, AdventureZombieFeedbackPhase(z));
            // These assignments occur after the initial PickRandomSpeed in PC Init.
            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility)
            {
                z.PresentationNativeVelocity = .6f;
                z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(.6f);
            }
        }

        /// <summary>Performs refresh adventure zombie native speed.</summary>
        /// <param name = "z">The z.</param>
        private void RefreshAdventureZombieNativeSpeed(Zombie z)
        {
            if (z.PresentationWhackSpeed)
            {
                return;
            }

            var phase = AdventureZombieFeedbackPhase(z);
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            if (z._presentationSpeedPhase != phase || z._presentationSpeedKind != kind)
            {
                PickAdventureZombieNativeSpeed(z, phase);
            }

            if (z.Kind == ZombieKind.Zomboni)
            {
                var nativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f);
                z.PresentationNativeVelocity = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.ZamboniVelocity(nativeX, z.PresentationNativeVelocity);
                z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
            }
        }

        /// <summary>Performs advance adventure zombie parts.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureZombieParts(float dt)
        {
            for (var i = Presentation.ZombiePartDrops.Count - 1; i >= 0; i--)
            {
                var part = Presentation.ZombiePartDrops[i];
                part.Age += dt;
                if (part.Age >= part.Duration)
                {
                    Presentation.ZombiePartDrops.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs add adventure zombie part.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "particleId">The particle id.</param>
        /// <param name = "anchor">The anchor.</param>
        /// <param name = "image">The image.</param>
        private void AddAdventureZombiePart(Zombie z, int kind, int particleId, string? anchor, string? image)
        {
            var clip = AdventurePresentationZombieAnimation(z);
            Presentation.ZombiePartDrops.Add(new global::PlantsVsZombies.Adventure.AdventureZombiePartDrop {
                Id = ++Presentation._effectId,
                Seed = z.Id * 397 ^ Presentation._effectId,
                NativeKind = kind,
                ParticleId = particleId,
                X = z.X,
                Row = z.Row,
                Scale = z.IsSmall ? .5f : 1f,
                Asset = AdventureZombiePresentationAsset(z),
                Animation = clip,
                AnimationAge = z.PresentationAnimationAge,
                Rate = AdventurePresentationZombieRate(z),
                Anchor = anchor,
                Image = image,
                Backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2),
                Chilled = z.ChillRemaining > 0f || z.FrozenRemaining > 0f,
                Hypnotized = z.Hypnotized
            });
        }

        /// <summary>Performs lose adventure zombie head.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "inPool">The in pool.</param>
        /// <param name = "mowed">The mowed.</param>
        /// <param name = "flags">The flags.</param>
        private void LoseAdventureZombieHead(Zombie z, int kind, bool inPool, bool mowed, uint flags)
        {
            // Keep the intact-pose animation/anchor snapshot before changing the walk/eat state.
            if ((flags & 16U) == 0 && (kind < 26 || kind > 31))
            {
                if (kind == 1 || kind == 33)
                {
                    AddAdventureZombiePart(z, kind, 11, "Zombie_flaghand", null);
                }

                AddAdventureZombiePart(z, kind, global::PlantsVsZombies.Adventure.AdventureZombiePartsRules.HeadParticle(kind, mowed, inPool), "anim_head1", global::PlantsVsZombies.Adventure.AdventureZombiePartsRules.HeadImage(kind));
                Presentation.ZombiePartSoundSerial++;
                RequestAdventureZombieFoley(25);
            }

            z.Headless = true;
            z.PresentationButter = 0f;
            if (z.Kind == ZombieKind.Pogo)
            {
                z.UsedAbility = true;
            }
        }

        /// <summary>Performs adventure presentation zombie mowed parts.</summary>
        /// <param name = "z">The z.</param>
        private void AdventurePresentationZombieMowedParts(Zombie z)
        {
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var height = z.PresentationNativeHeight;
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            if (!global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.CanLoseParts(kind, height, z.Airborne, sled))
            {
                return;
            }

            var inPool = z.PresentationInPool ?? (Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            if (!z.Headless)
            {
                LoseAdventureZombieHead(z, kind, inPool, true, 0);
            }

            if (global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShouldLoseArm(kind, 3, height, z.Airborne, sled, !z.PresentationArmLost, global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShieldType(z), 1f, (int)z.StartingHealth))
            {
                LoseAdventureZombieArm(z, kind, 3, inPool, 0);
            }
        }

        /// <summary>Performs advance adventure zombie feedback.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureZombieFeedback(float dt)
        {
            for (var i = Presentation.ArmDrops.Count - 1; i >= 0; i--)
            {
                Presentation.ArmDrops[i].Age += dt;
                if (Presentation.ArmDrops[i].Age >= .6f)
                {
                    Presentation.ArmDrops.RemoveAt(i);
                }
            }

            AdvanceAdventureZombieParts(dt);
            foreach (Zombie z in Zombies)
            {
                z.PresentationShieldRecoil = Math.Max(0f, z.PresentationShieldRecoil - dt);
                if (z.PresentationNewspaperSurpriseAge >= 0f)
                {
                    // Attachments keep their own source-rate clock, including while the body is chilled or frozen.
                    z.PresentationNewspaperSurpriseAge += dt;
                    if (z.PresentationNewspaperSurpriseAge >= 1.25f)
                    {
                        z.PresentationNewspaperSurpriseAge = -1f;
                    }
                }

                if (z.PresentationNewspaperGaspRemaining > 0f && z.FrozenRemaining <= 0f)
                {
                    z.PresentationNewspaperGaspRemaining = Math.Max(0f, z.PresentationNewspaperGaspRemaining - dt * (z.ChillRemaining > 0 ? .5f : 1f));
                    if (z.PresentationNewspaperGaspRemaining <= 0f)
                    {
                        z.PresentationNativePhase = 31;
                        RefreshAdventureZombieNativeSpeed(z);
                        // PC 0x541770: roar only after the gasp finishes, with a head and at most ten live opponents.
                        if (!z.Headless)
                        {
                            var count = 0;
                            foreach (var other in Zombies)
                            {
                                if (other.Health > 0f && !other.Headless && !other.Hypnotized)
                                {
                                    count++;
                                }
                            }

                            if (count <= 10)
                            {
                                RequestAdventureZombieFoley(66);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Performs advance adventure zombie clock.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureZombieClock(Zombie z, float dt)
        {
            EnsureAdventureZombieEquipment(z);
            RefreshAdventureZombieNativeSpeed(z);
            var clip = AdventurePresentationZombieAnimation(z);
            z._presentationTickSlow = z.ChillRemaining > 0f ? .5f : 1f;
            if (z.FrozenRemaining > 0f)
            {
                return;
            }

            var step = dt * z._presentationTickSlow;
            var rate = AdventurePresentationZombieRate(z);
            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (rate > 0f && global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data))
            {
                z._presentationWalkPhase += step * rate / data.Count;
                var loops = (int)Math.Floor(z._presentationWalkPhase);
                z._presentationWalkPhase -= loops;
                z._presentationWalkLoops += loops;
                z.PresentationAnimationAge = (z._presentationWalkLoops + z._presentationWalkPhase) * data.Count / rate;
            }
            else
            {
                z.PresentationAnimationAge += step;
            }

            z.PresentationPlantHeadAge += step;
        }

        /// <summary>Performs adventure zombie feedback phase.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie feedback phase.</returns>
        private int AdventureZombieFeedbackPhase(Zombie z)
        {
            if (z.PresentationNativePhase >= 0)
            {
                return z.PresentationNativePhase;
            }

            if (z.Kind == ZombieKind.Newspaper)
            {
                return z.ShieldHealth > 0 ? 29 : 31;
            }

            if (z.Kind == ZombieKind.Ladder && z.ShieldHealth > 0)
            {
                return 76;
            }

            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility)
            {
                return 17;
            }

            if (z.Kind == ZombieKind.Pogo && !z.UsedAbility)
            {
                return 20;
            }

            if (z.Kind == ZombieKind.Balloon && z.Airborne)
            {
                return 73;
            }

            if (z.Kind == ZombieKind.Digger)
            {
                return z.AbilityPhase == 0 ? 32 : z.AbilityPhase == 1 ? 36 : 37;
            }

            if (z.Kind == ZombieKind.DolphinRider)
            {
                return z.UsedAbility ? (Adventure.Level.IsWaterRow(z.Row) ? 55 : 56) : Adventure.Level.IsWaterRow(z.Row) ? 53 : 51;
            }

            if (z.Kind == ZombieKind.Snorkel && z.Submerged && Adventure.Level.IsWaterRow(z.Row))
            {
                return 59;
            }

            if (z.Kind == ZombieKind.PoleVaulter && z.UsedAbility && z.AbilityTimer > 0)
            {
                return 12;
            }

            if (z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility)
            {
                return 11;
            }

            return 0;
        }

        /// <summary>Stores normalized immutable catalog asset names for every supported zombie identifier.</summary>
        private static readonly string[] s_zombiePresentationAssets = CreateZombiePresentationAssets();

        /// <summary>Normalizes catalog paths once before any per-zombie simulation updates.</summary>
        /// <returns>Asset basenames indexed by the original zombie identifier.</returns>
        private static string[] CreateZombiePresentationAssets()
        {
            var assets = new string[VerifiedDataCatalog.Zombies.Count];
            for (var i = 0; i < assets.Length; i++)
            {
                assets[i] = VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Zombies[i].ReanimationId, out var definition)
                    ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/'))
                    : "Zombie";
            }

            return assets;
        }

        /// <summary>Performs adventure zombie presentation asset.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie presentation asset.</returns>
        public static string AdventureZombiePresentationAsset(Zombie z)
        {
            var kind = (int)z.Kind;
            return kind >= 0 && kind < s_zombiePresentationAssets.Length ? s_zombiePresentationAssets[kind] : "Zombie";
        }

        /// <summary>Performs adventure presentation zombie animation.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure presentation zombie animation.</returns>
        public string AdventurePresentationZombieAnimation(Zombie z)
        {
            string asset = AdventureZombiePresentationAsset(z), clip;
            if (z.PresentationNewspaperGaspRemaining > 0f)
            {
                clip = "anim_gasp";
            }
            else if (z.Kind == ZombieKind.Newspaper && z.ShieldHealth <= 0)
            {
                clip = z.Eating ? "anim_eat_nopaper" : "anim_walk_nopaper";
            }
            else if (z.Kind == ZombieKind.Ladder && z.ShieldHealth > 0)
            {
                clip = z.Eating ? "anim_laddereat" : "anim_ladderwalk";
            }
            else if (z.Eating)
            {
                clip = "anim_eat";
            }
            else if (z.Kind == ZombieKind.PoleVaulter && z.UsedAbility && z.AbilityTimer > 0)
            {
                clip = "anim_jump";
            }
            else if (z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility)
            {
                clip = "anim_run";
            }
            else if (z.Kind == ZombieKind.Pogo && !z.UsedAbility)
            {
                clip = "anim_pogo";
            }
            else if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 0)
            {
                clip = "anim_dig";
            }
            else if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 1)
            {
                clip = "anim_dizzy";
            }
            else if (z.Kind == ZombieKind.DolphinRider && !z.UsedAbility)
            {
                clip = Adventure.Level.IsWaterRow(z.Row) ? "anim_ride" : "anim_walkdolphin";
            }
            else if (z.Kind == ZombieKind.Snorkel && z.Submerged)
            {
                clip = "anim_swim";
            }
            else if (z.Kind == ZombieKind.DuckyTube && Adventure.Level.IsWaterRow(z.Row))
            {
                clip = "anim_swim";
            }
            else if (z.Kind == ZombieKind.Balloon && z.Airborne)
            {
                clip = "anim_idle";
            }
            else if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility)
            {
                clip = "anim_push";
            }
            else if (z.Kind == ZombieKind.Zomboni)
            {
                clip = "anim_drive";
            }
            else if (z.Kind == ZombieKind.Catapult)
            {
                clip = "anim_walk";
            }
            else
            {
                clip = z.PresentationWalk2 ? "anim_walk2" : "anim_walk";
            }

            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (!global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(asset, clip, out data))
            {
                clip = global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(asset, "anim_walk", out data) ? "anim_walk" : "anim_idle";
            }

            if (!string.Equals(z.PresentationAnimation, clip, StringComparison.Ordinal))
            {
                z.PresentationAnimation = clip;
                z.PresentationAnimationAge = 0f;
                z._presentationWalkPhase = 0f;
                z._presentationWalkLoops = 0;
                z._presentationMovementAnimation = null;
                z.PresentationMovementRate = -1f;
            }

            return clip;
        }

        /// <summary>Performs adventure presentation zombie rate.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure presentation zombie rate.</returns>
        public float AdventurePresentationZombieRate(Zombie z)
        {
            var clip = AdventurePresentationZombieAnimation(z);
            if (z.PresentationNewspaperGaspRemaining > 0f)
            {
                return 8f;
            }

            if (z.Kind == ZombieKind.PoleVaulter && z.UsedAbility && z.AbilityTimer > 0)
            {
                return 24f;
            }

            if (z.Eating)
            {
                return z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.Balloon || z.Kind == ZombieKind.Imp || z.Kind == ZombieKind.Digger || z.Kind == ZombieKind.JackInTheBox || z.Kind == ZombieKind.Snorkel || z.Kind == ZombieKind.Yeti ? 20f : 36f;
            }

            if (z.Kind == ZombieKind.Catapult)
            {
                return 5.5f;
            }

            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility)
            {
                return 30f;
            }

            if (z.Kind == ZombieKind.Balloon && z.Airborne || z.Kind == ZombieKind.Zomboni)
            {
                return 12f;
            }

            if (z.PresentationMovementRate >= 0 && z._presentationMovementAnimation == clip)
            {
                return z.PresentationMovementRate;
            }

            return AdventureZombiePlaybackRate(z, clip, z.Speed);
        }

        /// <summary>Performs adventure zombie playback rate.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "speed">The speed.</param>
        /// <returns>The resulting adventure zombie playback rate.</returns>
        private static float AdventureZombiePlaybackRate(Zombie z, string clip, float speed)
        {
            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (!global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data))
            {
                return 12f;
            }

            return data.GroundDistance > .000001f ? speed * 80f * .01f * 47f * data.Count / data.GroundDistance / (z.IsSmall ? .5f : 1f) : data.SourceRate;
        }

        /// <summary>Performs adventure presentation walking distance.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "dt">The dt.</param>
        /// <param name = "fallbackDistance">The fallback distance.</param>
        /// <returns>The resulting adventure presentation walking distance.</returns>
        private float AdventurePresentationWalkingDistance(Zombie z, float dt, float fallbackDistance)
        {
            if (dt <= 0f || z.PresentationNewspaperGaspRemaining > 0f || z.FrozenRemaining > 0f || z.DropRemaining > 0f || z.Eating)
            {
                return 0f;
            }

            var clip = AdventurePresentationZombieAnimation(z);
            int phase = AdventureZombieFeedbackPhase(z), kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (!global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.UsesGround(kind, phase, sled) || !global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data) || data.Ground == null)
            {
                return fallbackDistance;
            }

            var slow = z._presentationTickSlow;
            var rate = AdventureZombiePlaybackRate(z, clip, fallbackDistance / (dt * slow));
            z._presentationMovementAnimation = clip;
            z.PresentationMovementRate = rate;
            if (rate > 0f)
            {
                z.PresentationAnimationAge = (z._presentationWalkLoops + z._presentationWalkPhase) * data.Count / rate;
            }

            return data.VelocityAtPhase(z._presentationWalkPhase, rate * slow) * (z.IsSmall ? .5f : 1f) * (dt * 100f) / 80f;
        }

        /// <summary>Performs adventure presentation zombie hit.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "bypassShield">The bypass shield.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        private void AdventurePresentationZombieHit(Zombie z, float damage, bool bypassShield = false, uint damageFlags = 0)
        {
            EnsureAdventureZombieEquipment(z);
            z._presentationArmorBeforeHit = z.ArmorHealth;
            z._presentationShieldBeforeHit = z.ShieldHealth;
            z.PresentationHealthBeforeHit = z.Health;
            if (damage <= 0f || (damageFlags & 8U) != 0)
            {
                return;
            }

            var remaining = damage;
            if ((!bypassShield || (damageFlags & 2U) != 0) && z.ShieldHealth > 0f)
            {
                if ((damageFlags & 2U) == 0 && (z.Kind == ZombieKind.ScreenDoor || z.Kind == ZombieKind.Ladder))
                {
                    RequestAdventureZombieFoley(46);
                }

                z.PresentationArmorFlash = .25f;
                if ((damageFlags & 2U) == 0)
                {
                    z.PresentationShieldRecoil = .12f;
                }

                if ((damageFlags & 2U) == 0)
                {
                    remaining -= Math.Min(remaining, z.ShieldHealth);
                }
            }

            // Helm damage uses the body's additive group, including the still attached helm.
            if (remaining > 0f)
            {
                z.PresentationFlash = .25f;
            }
        }

        /// <summary>Performs adventure presentation zombie body damaged.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        private void AdventurePresentationZombieBodyDamaged(Zombie z, uint damageFlags = 0)
        {
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var height = z.PresentationNativeHeight;
            if (height < 0 && ModeSession != null && ModeSession.Definition.Mechanic == global::PlantsVsZombies.Modes.ModeMechanic.Aquarium)
            {
                height = 10;
            }

            var phase = AdventureZombieFeedbackPhase(z);
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            var inPool = z.PresentationInPool ?? (Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            // Burn death selects its own native charred attachment; don't turn it into flesh parts.
            if (z.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned && z.Health <= 0f)
            {
                return;
            }

            if (global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShouldLoseArm(kind, phase, height, z.Airborne, sled, !z.PresentationArmLost, global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShieldType(z), z.Health, (int)z.StartingHealth))
            {
                LoseAdventureZombieArm(z, kind, phase, inPool, damageFlags);
            }

            // Unlike the arm test this also runs after a lethal body hit (PC 0x546160).
            if (global::PlantsVsZombies.Adventure.AdventureZombiePartsRules.ShouldLoseHead(kind, height, z.Airborne, sled, !z.Headless, z.Health, (int)z.StartingHealth))
            {
                LoseAdventureZombieHead(z, kind, inPool, phase == 3, damageFlags);
            }
        }

        /// <summary>Performs lose adventure zombie arm.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "inPool">The in pool.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        private void LoseAdventureZombieArm(Zombie z, int kind, int phase, bool inPool, uint damageFlags)
        {
            z.PresentationArmLost = true;
            if (inPool || (damageFlags & 16U) != 0)
            {
                return;
            }

            Presentation.ZombiePartSoundSerial++;
            RequestAdventureZombieFoley(25);
            var clip = AdventurePresentationZombieAnimation(z);
            Presentation.ArmDrops.Add(new global::PlantsVsZombies.Adventure.AdventureZombieArmDrop {
                Id = ++Presentation._effectId,
                Seed = z.Id * 397 ^ Presentation._effectId,
                NativeKind = kind,
                X = z.X,
                Row = z.Row,
                Scale = z.IsSmall ? .5f : 1f,
                Asset = AdventureZombiePresentationAsset(z),
                Animation = clip,
                AnimationAge = z.PresentationAnimationAge,
                Rate = AdventurePresentationZombieRate(z),
                Anchor = global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ArmAnchor(kind),
                Image = global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ArmImage(kind),
                Mowed = phase == 3,
                Backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2),
                Chilled = z.ChillRemaining > 0f || z.FrozenRemaining > 0f,
                Hypnotized = z.Hypnotized
            });
        }

        /// <summary>Performs request adventure zombie foley.</summary>
        /// <param name = "id">The id.</param>
        private void RequestAdventureZombieFoley(int id)
        {
            Presentation.ZombieSounds.Add(new global::PlantsVsZombies.Adventure.AdventureZombieSoundEvent { Id = ++Presentation._effectId, Foley = id, Tick = (int)System.Math.Round(Elapsed * 100f) });
        }

        /// <summary>Performs request adventure projectile impact sound.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "z">The z.</param>
        private void RequestAdventureProjectileImpactSound(Pea p, Zombie? z)
        {
            var type = p.Butter ? 12 : p.Fire ? 6 : p.SourceKind == PlantKind.KernelPult ? 10 : p.ChillSplash ? 5 : p.Splash ? 3 : 0;
            var helm = z != null && z.ArmorHealth > 0 ? global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.HelmType(z) : 0;
            // PC fire splash is disabled against vehicles and door/ladder shields.
            var fireSplash = z != null && z.Kind != ZombieKind.Zomboni && z.Kind != ZombieKind.Catapult && (z.ShieldHealth <= 0 || z.Kind != ZombieKind.ScreenDoor && z.Kind != ZombieKind.Ladder);
            foreach (int id in global::PlantsVsZombies.Adventure.AdventureZombieSoundRules.ProjectileFoley(type, helm, fireSplash))
            {
                RequestAdventureZombieFoley(id);
            }
        }

        /// <summary>Performs spawn adventure zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "wave">The wave.</param>
        /// <returns>The resulting spawn adventure zombie.</returns>
        private Zombie SpawnAdventureZombie(ZombieKind kind, int row, float x, int wave)
        {
            var health = kind == ZombieKind.Yeti ? 1350f : kind == ZombieKind.Bungee ? 450f : kind == ZombieKind.Ladder ? 500f : kind == ZombieKind.Gargantuar ? 3000f : kind == ZombieKind.Zomboni ? 1350f : kind == ZombieKind.Catapult ? 850f : kind == ZombieKind.PoleVaulter || kind == ZombieKind.DolphinRider || kind == ZombieKind.JackInTheBox || kind == ZombieKind.Pogo || kind == ZombieKind.Dancing ? 500f : 270f;
            var armor = global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.StartingArmor(kind);
            var shield = global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.StartingShield(kind);
            var z = new Zombie
            {
                Id = ++Adventure._zombieId,
                Kind = kind,
                Row = row,
                X = x,
                Wave = wave,
                Health = health,
                StartingHealth = health,
                ArmorHealth = armor,
                ShieldHealth = shield,
                Airborne = kind == ZombieKind.Balloon,
                Submerged = kind == ZombieKind.Snorkel || kind == ZombieKind.Digger,
                AbilityTimer = kind == ZombieKind.JackInTheBox ? 10f + (float)_random.NextDouble() * 15f : kind == ZombieKind.Dancing ? 4f : kind == ZombieKind.Bungee ? 4f : 5f
            };
            if (kind == ZombieKind.Bungee)
            {
                var target = Plants.Count > 0 ? Plants[_random.Next(Plants.Count)] : null;
                z.Row = target == null ? row : target.Row;
                z.TargetColumn = target == null ? _random.Next(2, 9) : target.Column;
                z.X = z.TargetColumn + 0.5f;
            }

            if (Adventure.Level.Mode == AdventureMode.LittleTrouble)
            {
                z.IsSmall = true;
                z.Health = (int)z.Health / 4;
                z.ArmorHealth = (int)z.ArmorHealth / 4;
                z.ShieldHealth = (int)z.ShieldHealth / 4;
                z.StartingHealth = z.Health;
            }

            InitializeAdventureZombieNative(z, x >= 9.5f && kind != ZombieKind.Bungee);
            Zombies.Add(z);
            SpawnedZombies++;
            return z;
        }

        /// <summary>Performs drop adventure zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "wave">The wave.</param>
        private void DropAdventureZombie(ZombieKind kind, int row, int column, int wave)
        {
            if (HasUmbrellaNear(row, column))
            {
                return;
            }

            var passenger = SpawnAdventureZombie(kind, row, column + 0.5f, wave);
            passenger.DropRemaining = 4f;
            var carrier = SpawnAdventureZombie(ZombieKind.Bungee, row, column + 0.5f, wave);
            carrier.BungeeCarrier = true;
            carrier.Row = row;
            carrier.TargetColumn = column;
            carrier.X = column + 0.5f;
            carrier.LinkedZombieId = passenger.Id;
            Adventure.Notice(AdventureNoticeKind.RoofDrop, (int)kind, row, column);
        }

        /// <summary>Performs advance adventure zombies.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureZombies(float dt)
        {
            AdvanceAdventurePlantHitCounters(dt);
            foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
            {
                if (!Zombies.Contains(z))
                {
                    continue;
                }

                z.Age += dt;
                if (z.Headless)
                {
                    if (_random.Next(5) == 0)
                    {
                        z.Health -= z.StartingHealth >= 500f ? 3f : 1f;
                    }

                    if (z.Health <= 0)
                    {
                        KillAdventureZombie(z);
                        continue;
                    }
                }

                if (z.GraveRiseTicks > 0)
                {
                    // PC 0x54DA20 / 0x544970: no movement or eating during the 150-update grave phase.
                    if (z.FrozenRemaining <= 0f)
                    {
                        z.GraveRiseTicks--;
                        if (z.GraveRiseTicks == 0)
                        {
                            z.PresentationNativePhase = -1;
                        }
                    }
                    else
                    {
                        z.FrozenRemaining = Math.Max(0f, z.FrozenRemaining - dt);
                    }

                    continue;
                }

                if (z.DropRemaining > 0f)
                {
                    z.DropRemaining -= dt;
                    continue;
                }

                if (z.FrozenRemaining > 0f)
                {
                    z.FrozenRemaining = Math.Max(0, z.FrozenRemaining - dt);
                    continue;
                }

                var slow = z.ChillRemaining > 0f ? 0.5f : 1f;
                z.ChillRemaining = Math.Max(0, z.ChillRemaining - dt);
                if (AdvanceAdventureGarlicBite(z, dt))
                {
                    continue;
                }

                // Head loss does not call StopEating in PC. Preserve a latched meal, but deal no new bite damage.
                if (z.Headless && z.Eating)
                {
                    continue;
                }

                if (z.PresentationNewspaperGaspRemaining > 0f)
                {
                    continue;
                }

                z.AbilityTimer -= dt * slow;
                if (z.Kind == ZombieKind.Bungee)
                {
                    if (HasUmbrellaNear(z.Row, z.TargetColumn))
                    {
                        Zombie? passenger = null;
                        foreach (var other in Zombies)
                        {
                            if (other.Id == z.LinkedZombieId)
                            {
                                passenger = other;
                                break;
                            }
                        }
                        if (passenger != null)
                        {
                            passenger.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                            KillAdventureZombie(passenger);
                        }

                        z.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                        KillAdventureZombie(z);
                        continue;
                    }

                    if (z.AbilityTimer <= 0)
                    {
                        if (!z.BungeeCarrier)
                        {
                            var victim = PlantAt(z.Row, z.TargetColumn);
                            if (victim != null)
                            {
                                RemoveAdventurePlantAndStack(victim);
                            }
                        }

                        z.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                        KillAdventureZombie(z);
                    }

                    continue;
                }

                if (z.Kind == ZombieKind.JackInTheBox && z.AbilityTimer <= 0)
                {
                    foreach (Plant p in new PooledSnapshot<Plant>(Plants))
                    {
                        if (Math.Abs(p.Row - z.Row) <= 1 && Math.Abs(p.Column + 0.5f - z.X) < 1.5f)
                        {
                            RemoveAdventurePlantAndStack(p);
                        }
                    }

                    KillAdventureZombie(z);
                    Raise(GameEvent.Explosion);
                    continue;
                }

                if (z.Kind == ZombieKind.Dancing && z.AbilityTimer <= 0 && !z.Hypnotized)
                {
                    for (var dr = -1; dr <= 1; dr++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (Math.Abs(dr) + Math.Abs(dx) == 1)
                            {
                                var row = z.Row + dr;
                                var x = z.X + dx * 0.9f;
                                if (Adventure.Level.IsLawnRow(row) && !Adventure.Level.IsWaterRow(row) && x > 0.5f && !HasBackupDancerNear(row, x))
                                {
                                    SpawnAdventureZombie(ZombieKind.BackupDancer, row, x, z.Wave);
                                }
                            }
                        }
                    }

                    z.AbilityTimer = 12f;
                }

                if (z.Kind == ZombieKind.Gargantuar && !z.ThrownImp && z.Health <= 1500 && z.X > 2.5f && !z.Hypnotized)
                {
                    z.ThrownImp = true;
                    SpawnAdventureZombie(ZombieKind.Imp, z.Row, Math.Max(1.2f, z.X - 4f), z.Wave);
                }

                if (z.Kind == ZombieKind.Newspaper && z.ShieldHealth <= 0 && !z.UsedAbility)
                {
                    z.UsedAbility = true;
                    PickAdventureZombieNativeSpeed(z, 31);
                }

                if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 0)
                {
                    z.X -= z.Speed * dt; // PC tunnelling uses mVelX directly, without the ground/chill branch.
                    if (z.X <= 0.5f)
                    {
                        z.Submerged = false;
                        z.AbilityPhase = 1;
                        z.AbilityTimer = 2f;
                    }

                    continue;
                }

                if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 1)
                {
                    if (z.AbilityTimer <= 0)
                    {
                        z.AbilityPhase = 2;
                        PickAdventureZombieNativeSpeed(z, 37);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (z.Kind == ZombieKind.Catapult && z.X <= 7.5f && z.AbilityPhase < 12 && !z.Hypnotized)
                {
                    if (z.AbilityTimer <= 0)
                    {
                        Plant? victim = null;
                        foreach (Plant p in Plants)
                        {
                            if (p.Row == z.Row && (victim == null || p.Column < victim.Column))
                            {
                                victim = p;
                            }
                        }

                        if (victim != null && !HasUmbrellaNear(victim.Row, victim.Column))
                        {
                            victim.Health -= 80f;
                            RecordAdventurePlantHit(victim);
                            if (victim.Health <= 0)
                            {
                                RemoveAdventurePlantAndStack(victim);
                            }
                        }

                        z.AbilityPhase++;
                        z.AbilityTimer = 5f;
                    }

                    continue;
                }

                if (z.Kind == ZombieKind.Zomboni)
                {
                    var col = (int)Math.Floor(z.X);
                    if (col >= 0 && col < 9 && !HasCellTimer(Adventure._ice, z.Row, col))
                    {
                        Adventure._ice.Add(new AdventureCellTimer { Row = z.Row, Column = col, Remaining = 30f });
                    }
                }

                Zombie? opponent = null;
                foreach (var other in Zombies)
                {
                    if (other != z && other.Row == z.Row && other.Hypnotized != z.Hypnotized && !other.Airborne && Math.Abs(other.X - z.X) < 0.5f)
                    {
                        opponent = other;
                        break;
                    }
                }
                if (opponent != null && (z.Hypnotized || opponent.Hypnotized) && !z.Headless)
                {
                    if (AdvanceAdventureNativeEating(z, null, dt, true))
                    {
                        var bites = global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.EatingChecks(z.Age, dt, z.ChillRemaining > 0);
                        if (bites > 0)
                        {
                            HitAdventureZombie(opponent, 4f * bites, true, 9U);
                        }
                    }

                    continue;
                }

                Plant? target = null;
                var vault = z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo;
                var attack = vault && !z.UsedAbility ? AdventureBiteAttack.Vault : z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult ? AdventureBiteAttack.DriveOver : AdventureBiteAttack.Chew;
                if (!z.Airborne && !z.Hypnotized && !z.Headless)
                {
                    if (z.Kind == ZombieKind.Ladder && !z.UsedAbility)
                    {
                        target = FindAdventureNativePlantTarget(z, AdventureBiteAttack.Ladder);
                    }

                    if (target == null)
                    {
                        target = FindAdventureNativePlantTarget(z, attack);
                    }
                }

                if (z.Kind == ZombieKind.Snorkel)
                {
                    z.Submerged = target == null;
                }

                if (target != null && target.HasLadder && target.Layer != PlantLayer.Support)
                {
                    target = null;
                }

                var tallBarrier = target != null && (target.Kind == PlantKind.TallNut || AdventurePlantAt(target.Row, target.Column, PlantLayer.Main) != null && AdventurePlantAt(target.Row, target.Column, PlantLayer.Main).Kind == PlantKind.TallNut);
                if (target != null && vault && !z.UsedAbility && !tallBarrier && z.AbilityTimer > 0)
                {
                    continue;
                }

                if (target != null && vault && !z.UsedAbility && !tallBarrier)
                {
                    z.X -= 1.1f;
                    z.AbilityTimer = 0.75f;
                    if (z.Kind != ZombieKind.Pogo)
                    {
                        z.UsedAbility = true;
                        RefreshAdventureZombieNativeSpeed(z);
                    }

                    continue;
                }

                if (target != null && z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility && tallBarrier)
                {
                    z.UsedAbility = true;
                    RefreshAdventureZombieNativeSpeed(z);
                }

                if (target != null && z.Kind == ZombieKind.Pogo && tallBarrier)
                {
                    z.UsedAbility = true;
                    RefreshAdventureZombieNativeSpeed(z);
                }

                if (target != null && z.Kind == ZombieKind.Ladder && !z.UsedAbility && (target.Kind == PlantKind.Wallnut || target.Kind == PlantKind.TallNut || target.Kind == PlantKind.Pumpkin))
                {
                    target.HasLadder = true;
                    z.UsedAbility = true;
                    z.ShieldHealth = 0;
                    RefreshAdventureZombieNativeSpeed(z);
                    z.X -= 0.8f;
                    continue;
                }

                if (!z.Headless && AdvanceAdventureNativeEating(z, target, dt))
                {
                    continue;
                }

                if (z.Kind != ZombieKind.Gargantuar && z.Kind != ZombieKind.Zomboni && z.Kind != ZombieKind.Catapult)
                {
                    target = null;
                }

                z.Eating = target != null;
                if (target != null)
                {
                    if (z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult)
                    {
                        if (target.Kind == PlantKind.Spikerock)
                        {
                            HitAdventureZombie(z, 1800f, true);
                            target.Health -= 50;
                            target.VehicleHitsTaken++;
                            if (target.Health <= 0)
                            {
                                RemoveAdventurePlantAndStack(target);
                            }
                        }
                        else
                        {
                            RemoveAdventurePlantAndStack(target);
                        }

                        continue;
                    }

                    z.BiteTimer -= dt * slow;
                    if (z.BiteTimer <= 0)
                    {
                        if (target.Kind == PlantKind.HypnoShroom && !target.Asleep && z.Kind != ZombieKind.Gargantuar)
                        {
                            z.Hypnotized = true;
                            z.Submerged = false;
                            RemoveAdventurePlantAndStack(target);
                            continue;
                        }

                        target.Health -= z.Kind == ZombieKind.Gargantuar ? target.Kind == PlantKind.Spikerock ? 50f : 9999f : 4f;
                        z.BiteTimer = z.Kind == ZombieKind.Gargantuar ? 2f : 0.04f;
                        if (target.Kind == PlantKind.Garlic && z.Kind != ZombieKind.Gargantuar)
                        {
                            var rows = new List<int>();
                            for (var r = Math.Max(0, z.Row - 1); r <= Math.Min(RowCount - 1, z.Row + 1); r++)
                            {
                                if (r != z.Row && Adventure.Level.IsLawnRow(r) && Adventure.Level.IsWaterRow(r) == Adventure.Level.IsWaterRow(z.Row))
                                {
                                    rows.Add(r);
                                }
                            }

                            if (rows.Count > 0)
                            {
                                z.Row = rows[_random.Next(rows.Count)];
                            }
                        }

                        if (target.Health <= 0)
                        {
                            RemoveAdventurePlantAndStack(target);
                        }
                    }
                }
                else
                {
                    z.BiteTimer = 0;
                    var direction = z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2 ? 1f : -1f;
                    z.X += direction * AdventurePresentationWalkingDistance(z, dt, z.Speed * dt * slow);
                }

                if (z.X > 12f && (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2))
                {
                    z.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                    KillAdventureZombie(z);
                    continue;
                }

                if (!z.Hypnotized && z.X < 0.15f && Mowers[z.Row].Available)
                {
                    Mowers[z.Row].Available = false;
                    Mowers[z.Row].Active = true;
                    Raise(GameEvent.Mower);
                }
                else if (!z.Hypnotized && z.X < -0.65f && !Mowers[z.Row].Active)
                {
                    State = MatchState.Lost;
                    Adventure.Phase = AdventurePhase.Lost;
                    Raise(GameEvent.Lost);
                    return;
                }
            }
        }

        /// <summary>Performs advance adventure mowers.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureMowers(float dt)
        {
            foreach (Mower m in Mowers)
            {
                if (!m.Active)
                {
                    continue;
                }

                m.X += 5f * dt;
                foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
                {
                    if (!z.Hypnotized && z.Row == m.Row && Math.Abs(z.X - m.X) < 0.7f)
                    {
                        z.PresentationDeath = AdventureZombieDeathPresentation.Mowed;
                        KillAdventureZombie(z);
                    }
                }

                if (m.X > 10f || Adventure.Level.IsWaterRow(m.Row) && !Rules.AdventurePoolCleaners && m.X > 1.5f)
                {
                    m.Active = false;
                }
            }
        }

        /// <summary>Stores level one tutorial.</summary>
        private LevelOneTutorial? _levelOneTutorial;

        /// <summary>Gets the level one tutorial, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private LevelOneTutorial RequiredLevelOneTutorial => _levelOneTutorial ?? throw new System.InvalidOperationException("The member '_levelOneTutorial' is not available.");
        /// <summary>Stores level one seed selected.</summary>
        private bool _levelOneSeedSelected;
        /// <summary>Stores level one allow plant removal.</summary>
        private bool _levelOneAllowPlantRemoval;
        /// <summary>Stores level one seconds per native tick.</summary>
        private double _levelOneSecondsPerNativeTick;
        /// <summary>Stores level one wave countdown.</summary>
        private double _levelOneWaveCountdown;
        /// <summary>Stores level one between wave delay.</summary>
        private double _levelOneBetweenWaveDelay;
        /// <summary>Stores level one zombie spawn x.</summary>
        private float _levelOneZombieSpawnX;
        /// <summary>Stores level one zombie spacing.</summary>
        private float _levelOneZombieSpacing;
        /// <summary>Stores level one zombie health.</summary>
        private float _levelOneZombieHealth;
        /// <summary>Stores level one sky sun target y.</summary>
        private float _levelOneSkySunTargetY;
        /// <summary>Stores level one delay provider.</summary>
        private Func<GameSimulation, int, double>? _levelOneDelayProvider;
        /// <summary>Stores level one completion condition.</summary>
        private Func<GameSimulation, bool>? _levelOneCompletionCondition;
        /// <summary>Stores level one sky sun factory.</summary>
        private Func<GameSimulation, Random, global::PlantsVsZombies.Simulation.Sun>? _levelOneSkySunFactory;
        /// <summary>Stores level one use native sky sun.</summary>
        private bool _levelOneUseNativeSkySun;
        /// <summary>Stores level one native sky sun random draw.</summary>
        private Func<int, int>? _levelOneNativeSkySunRandomDraw;
        /// <summary>Stores level one native sky sun state.</summary>
        private global::PlantsVsZombies.Sun.SkySunState _levelOneNativeSkySunState;
        /// <summary>Stores level one native sky sun elapsed.</summary>
        private double _levelOneNativeSkySunElapsed;
        /// <summary>Stores level one native sky suns.</summary>
        private readonly List<LevelOneNativeSkySun> _levelOneNativeSkySuns = new List<LevelOneNativeSkySun>();
        /// <summary>Stores level one native sky suns view.</summary>
        private IReadOnlyList<LevelOneNativeSkySun>? _levelOneNativeSkySunsView;
        /// <summary>Captured at construction; always false outside experimental 1-1.</summary>
        public bool LevelOneUsesNativeSkySunRules
        {
            get
            {
                return IsExperimentalLevelOne && _levelOneUseNativeSkySun;
            }
        }

        /// <summary>
        /// Read-only native pixel-space objects, distinct from the legacy grid-space Suns list.
        /// A reference remains readable after removal, but is no longer clickable.
        /// </summary>
        public IReadOnlyList<LevelOneNativeSkySun> LevelOneNativeSkySuns
        {
            get
            {
                if (_levelOneNativeSkySunsView == null)
                {
                    _levelOneNativeSkySunsView = _levelOneNativeSkySuns.AsReadOnly();
                }

                return _levelOneNativeSkySunsView;
            }
        }

        /// <summary>Gets or sets level one native sky sun countdown ticks.</summary>
        public int? LevelOneNativeSkySunCountdownTicks
        {
            get
            {
                return LevelOneUsesNativeSkySunRules ? (int? )_levelOneNativeSkySunState.Countdown : null;
            }
        }

        /// <summary>Gets or sets level one native sky sun drop count.</summary>
        public int? LevelOneNativeSkySunDropCount
        {
            get
            {
                return LevelOneUsesNativeSkySunRules ? (int? )_levelOneNativeSkySunState.Drops : null;
            }
        }

        /// <summary>Tests one live object in native pixels; it does not select between overlapping objects.</summary>
        public bool HitTestLevelOneNativeSkySun(LevelOneNativeSkySun sun, int pixelX, int pixelY)
        {
            return LevelOneUsesNativeSkySunRules && sun != null && _levelOneNativeSkySuns.Contains(sun) && global::PlantsVsZombies.Sun.SunPickupRules.HitTest(sun._fallState.Pickup, pixelX, pixelY, false);
        }

        /// <summary>
        /// Begins flight for the selected object. Credit and SunCollected occur
        /// only when a later effective native update reaches the target.
        /// </summary>
        public bool TryClickLevelOneNativeSkySun(LevelOneNativeSkySun sun, int pixelX, int pixelY, int button = 0)
        {
            if (!LevelOneUsesNativeSkySunRules || State != MatchState.Playing || sun == null || !_levelOneNativeSkySuns.Contains(sun))
            {
                return false;
            }

            global::PlantsVsZombies.Sun.SunPickupState pickup;
            if (!global::PlantsVsZombies.Sun.SunPickupRules.TryClick(sun._fallState.Pickup, pixelX, pixelY, button, true, false, false, out pickup))
            {
                return false;
            }

            sun._fallState = sun._fallState.WithPickup(pickup);
            Raise(GameEvent.SunPickupStarted);
            return true;
        }

        /// <summary>Mode detection for the unsodded background and single-row grass.</summary>
        public bool LevelOneUsesSingleRowLawn
        {
            get
            {
                return IsExperimentalLevelOne;
            }
        }

        /// <summary>Null outside this experiment; only native tutorial states 0-4 are modeled.</summary>
        public LevelOneTutorialStage? CurrentLevelOneTutorialStage
        {
            get
            {
                return _levelOneTutorial == null ? (LevelOneTutorialStage? )null : _levelOneTutorial.Stage;
            }
        }

        /// <summary>Gets or sets level one selected seed.</summary>
        public PlantKind? LevelOneSelectedSeed
        {
            get
            {
                return _levelOneSeedSelected ? (PlantKind? )PlantKind.Peashooter : null;
            }
        }

        /// <summary>Only the verified tutorial gate, not every reason a wave can be delayed.</summary>
        public bool LevelOneTutorialBlocksWaveCountdown
        {
            get
            {
                return _levelOneTutorial != null && _levelOneTutorial.TutorialBlocksWaveCountdown;
            }
        }

        /// <summary>Gets or sets level one waves dispatched.</summary>
        public int LevelOneWavesDispatched { get; private set; }

        /// <summary>Gets or sets level one total waves.</summary>
        public int LevelOneTotalWaves
        {
            get
            {
                return IsExperimentalLevelOne ? LevelOneData.WaveCount : 0;
            }
        }

        /// <summary>Gets or sets level one total zombies.</summary>
        public int LevelOneTotalZombies
        {
            get
            {
                if (!IsExperimentalLevelOne)
                {
                    return 0;
                }

                var total = 0;
                for (var i = 0; i < LevelOneData.WaveCount; i++)
                {
                    total += LevelOneData.GetZombieCount(i);
                }

                return total;
            }
        }

        /// <summary>Coarse dispatched-batch fraction, not the original health-based HUD meter.</summary>
        public float LevelOneWaveProgress
        {
            get
            {
                return IsExperimentalLevelOne ? (float)LevelOneWavesDispatched / LevelOneData.WaveCount : 0f;
            }
        }

        /// <summary>Approximate scheduler time; null when inactive or all batches are dispatched.</summary>
        public double? LevelOneWaveCountdownSeconds
        {
            get
            {
                return !IsExperimentalLevelOne || LevelOneWavesDispatched >= LevelOneData.WaveCount ? (double? )null : Math.Max(0.0, _levelOneWaveCountdown);
            }
        }

        /// <summary>Uses the same zero-based row, column order as TryPlant.</summary>
        public bool IsLevelOnePlantingCell(int row, int column)
        {
            return IsExperimentalLevelOne && row >= 0 && row < Rows && column >= 0 && column < Columns && LevelOneData.IsGrassCell(column, row);
        }

        /// <summary>Checks experimental card selection without changing state.</summary>
        public bool CanSelectLevelOneSeed(PlantKind kind)
        {
            if (!IsExperimentalLevelOne || State != MatchState.Playing || _levelOneSeedSelected || kind != PlantKind.Peashooter || _seedCooldownTicks[LevelOneData.PeashooterSeedId] > 0 || SunAmount < LevelOneData.PeashooterCost)
            {
                return false;
            }

            return RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.AwaitingSeedSelection || RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.ZombieOnslaughtPrompt;
        }

        /// <summary>Checks current experimental placement conditions without planting.</summary>
        public bool CanPlantLevelOneSeedAt(int row, int column)
        {
            return IsExperimentalLevelOne && State == MatchState.Playing && _levelOneSeedSelected && IsLevelOnePlantingCell(row, column) && SunAmount >= LevelOneData.PeashooterCost && _seedCooldownTicks[LevelOneData.PeashooterSeedId] == 0 && PlantAt(row, column) == null;
        }

        /// <summary>
        /// Selects the only available seed when the card is usable. The host
        /// must call this before TryPlant in the experimental mode.
        /// </summary>
        public bool TrySelectLevelOneSeed(PlantKind kind)
        {
            if (!CanSelectLevelOneSeed(kind))
            {
                return false;
            }

            if (RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.AwaitingSeedSelection)
            {
                if (!RequiredLevelOneTutorial.OnSeedPickedUp().Changed)
                {
                    return false;
                }
            }

            _levelOneSeedSelected = true;
            return true;
        }

        /// <summary>Cancel a held experimental seed; returns false if none is held or play is paused.</summary>
        public bool CancelLevelOneSeedSelection()
        {
            if (!IsExperimentalLevelOne || State != MatchState.Playing || !_levelOneSeedSelected)
            {
                return false;
            }

            _levelOneSeedSelected = false;
            RequiredLevelOneTutorial.OnPlantingCursorCancelled();
            return true;
        }

        /// <summary>Performs initialize experimental level one.</summary>
        /// <param name = "initialized">The initialized.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        private void InitializeExperimentalLevelOne(ref bool initialized)
        {
            var options = Rules.LevelOneOptions;
            if (options == null)
            {
                throw new ArgumentNullException("LevelOneOptions");
            }

            if (!Finite(options.SecondsPerNativeTick) || options.SecondsPerNativeTick <= 0.0 || options.SecondsPerNativeTick * 200.0 > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException("SecondsPerNativeTick");
            }

            if (!Finite(options.InitialWaveDelaySeconds) || options.InitialWaveDelaySeconds < 0.0)
            {
                throw new ArgumentOutOfRangeException("InitialWaveDelaySeconds");
            }

            if (!Finite(options.BetweenWaveDelaySeconds) || options.BetweenWaveDelaySeconds < 0.0)
            {
                throw new ArgumentOutOfRangeException("BetweenWaveDelaySeconds");
            }

            if (!Finite(options.ZombieSpawnX))
            {
                throw new ArgumentOutOfRangeException("ZombieSpawnX");
            }

            if (!Finite(options.ZombieSpacingInWave) || options.ZombieSpacingInWave < 0f || Math.Abs((double)options.ZombieSpawnX) + options.ZombieSpacingInWave > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException("ZombieSpacingInWave");
            }

            if (!Finite(options.ZombieHealth) || options.ZombieHealth <= 0f)
            {
                throw new ArgumentOutOfRangeException("ZombieHealth");
            }

            if (!Finite(options.SkySunTargetY))
            {
                throw new ArgumentOutOfRangeException("SkySunTargetY");
            }

            if (options.UseNativeSkySunRules && options.SecondsPerNativeTick < SimulationStep / 1024.0)
            {
                throw new ArgumentOutOfRangeException("SecondsPerNativeTick", "Native sky rules allow at most 1024 updates per simulation step.");
            }

            _levelOneSecondsPerNativeTick = options.SecondsPerNativeTick;
            _levelOneWaveCountdown = options.InitialWaveDelaySeconds;
            _levelOneBetweenWaveDelay = options.BetweenWaveDelaySeconds;
            _levelOneZombieSpawnX = options.ZombieSpawnX;
            _levelOneZombieSpacing = options.ZombieSpacingInWave;
            _levelOneZombieHealth = options.ZombieHealth;
            _levelOneSkySunTargetY = options.SkySunTargetY;
            _levelOneAllowPlantRemoval = options.AllowPlantRemoval;
            _levelOneDelayProvider = options.BetweenWaveDelay;
            _levelOneCompletionCondition = options.CompletionCondition;
            _levelOneSkySunFactory = options.SkySunFactory;
            _levelOneTutorial = new LevelOneTutorial();
            _levelOneUseNativeSkySun = options.UseNativeSkySunRules;
            if (_levelOneUseNativeSkySun)
            {
                _levelOneNativeSkySunRandomDraw = options.NativeSkySunRandomDraw;
                _levelOneNativeSkySunState = global::PlantsVsZombies.Sun.SkySunRules.Initialize(0, DrawLevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunRules.InitialDrawBound));
            }

            SunAmount = LevelOneData.StartingSun;
            for (var row = 0; row < Mowers.Count; row++)
            {
                if (row != LevelOneData.GrassRow)
                {
                    Mowers[row].Available = false;
                }
            }

            initialized = true;
        }

        /// <summary>Performs can plant experimental level one.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "allowed">The allowed.</param>
        private void CanPlantExperimentalLevelOne(PlantKind kind, int row, int column, ref bool allowed)
        {
            allowed = kind == PlantKind.Peashooter && CanPlantLevelOneSeedAt(row, column);
        }

        /// <summary>Performs can remove experimental level one plant.</summary>
        /// <param name = "allowed">The allowed.</param>
        private void CanRemoveExperimentalLevelOnePlant(ref bool allowed)
        {
            allowed = _levelOneAllowPlantRemoval;
        }

        /// <summary>Performs on experimental level one plant placed.</summary>
        private void OnExperimentalLevelOnePlantPlaced()
        {
            _levelOneSeedSelected = false;
            // This simulation removes plants immediately; Plants.Count is only
            // an approximation of the original data-array allocation count.
            if (RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.AwaitingPlantPlacement)
            {
                ApplyLevelOneTutorialEffect(RequiredLevelOneTutorial.OnPlantPlaced(Plants.Count));
            }
        }

        /// <summary>Performs on experimental level one sun collected.</summary>
        private void OnExperimentalLevelOneSunCollected()
        {
            UpdateLevelOneSeedUsability();
        }

        /// <summary>Performs before experimental level one tick.</summary>
        private void BeforeExperimentalLevelOneTick()
        {
            if (RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.NotStarted)
            {
                RequiredLevelOneTutorial.OnFirstActiveGameplayUpdate();
            }

            UpdateLevelOneSeedUsability();
        }

        /// <summary>Performs advance experimental level one native sky sun.</summary>
        /// <param name = "handled">The handled.</param>
        private void AdvanceExperimentalLevelOneNativeSkySun(ref bool handled)
        {
            if (!_levelOneUseNativeSkySun)
            {
                return;
            }

            handled = true;
            _levelOneNativeSkySunElapsed += SimulationStep;
            var tolerance = Math.Min(_levelOneSecondsPerNativeTick * 1e-9, 1e-10);
            while (_levelOneNativeSkySunElapsed + tolerance >= _levelOneSecondsPerNativeTick && State == MatchState.Playing)
            {
                _levelOneNativeSkySunElapsed -= _levelOneSecondsPerNativeTick;
                if (_levelOneNativeSkySunElapsed < 0.0)
                {
                    _levelOneNativeSkySunElapsed = 0.0;
                }

                TickLevelOneNativeSkySun();
                if (State != MatchState.Playing)
                {
                    // The outer Update subtracts one simulation step after Advance returns.
                    _levelOneNativeSkySunElapsed = 0.0;
                    _accumulatedTime = SimulationStep;
                    return;
                }
            }
        }

        /// <summary>Performs tick level one native sky sun.</summary>
        private void TickLevelOneNativeSkySun()
        {
            // In the examined board update, existing pickups precede the sky scheduler.
            for (var i = _levelOneNativeSkySuns.Count - 1; i >= 0; i--)
            {
                var sun = _levelOneNativeSkySuns[i];
                var boundedBalance = Math.Min(global::PlantsVsZombies.Sun.SunPickupRules.BalanceCap, Math.Max(0, SunAmount));
                var tick = global::PlantsVsZombies.Sun.SkySunFall.Tick(sun._fallState, boundedBalance, true, true, false, 0);
                sun._fallState = tick.State;
                if (tick.State.Pickup.Dead)
                {
                    _levelOneNativeSkySuns.RemoveAt(i);
                }

                if (tick.Credited)
                {
                    SunAmount = tick.Balance;
                    OnExperimentalLevelOneSunCollected();
                    Raise(GameEvent.SunCollected);
                }

                if (State != MatchState.Playing)
                {
                    return;
                }
            }

            // This host assumes its unmodeled end-state and mode predicates are clear.
            var context = new global::PlantsVsZombies.Sun.SkySunContext(0, 0, true, false, 0, -1, (int)RequiredLevelOneTutorial.Stage, Plants.Count, false);
            var needsDraws = global::PlantsVsZombies.Sun.SkySunRules.NeedsDraws(_levelOneNativeSkySunState, context);
            var xDraw = needsDraws ? DrawLevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunRules.XDrawBound) : -1;
            var intervalDraw = needsDraws ? DrawLevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunRules.IntervalDrawBound) : -1;
            var next = global::PlantsVsZombies.Sun.SkySunRules.Tick(_levelOneNativeSkySunState, context, xDraw, intervalDraw);
            if (next.Spawned)
            {
                var landingDraw = DrawLevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunFall.LandingDrawBound);
                _levelOneNativeSkySuns.Add(new LevelOneNativeSkySun(global::PlantsVsZombies.Sun.SkySunFall.Initialize(next, landingDraw)));
            }

            _levelOneNativeSkySunState = next.State;
        }

        /// <summary>Performs draw level one native sky sun.</summary>
        /// <param name = "bound">The bound.</param>
        /// <returns>The resulting draw level one native sky sun.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private int DrawLevelOneNativeSkySun(int bound)
        {
            var draw = _levelOneNativeSkySunRandomDraw == null ? _random.Next(bound) : _levelOneNativeSkySunRandomDraw(bound);
            if (draw < 0 || draw >= bound)
            {
                throw new InvalidOperationException("Native sky-sun random draw is outside its requested bound.");
            }

            return draw;
        }

        /// <summary>Performs can drop experimental level one sky sun.</summary>
        /// <param name = "allowed">The allowed.</param>
        private void CanDropExperimentalLevelOneSkySun(ref bool allowed)
        {
            var stage = RequiredLevelOneTutorial.Stage;
            allowed = (stage != LevelOneTutorialStage.AwaitingSeedSelection && stage != LevelOneTutorialStage.AwaitingPlantPlacement) || Plants.Count > 0;
        }

        /// <summary>Performs spawn experimental level one sky sun.</summary>
        /// <param name = "handled">The handled.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private void SpawnExperimentalLevelOneSkySun(ref bool handled)
        {
            var sun = _levelOneSkySunFactory == null ? CreateSun(0.4f + (float)_random.NextDouble() * 8.2f, -1f, _levelOneSkySunTargetY)

            : _levelOneSkySunFactory(this, _random);
            if (sun == null || !Finite(sun.X) || !Finite(sun.Y) || !Finite(sun.TargetY) || !Finite(sun.Age))
            {
                throw new InvalidOperationException("The experimental sky-sun factory must return a sun with finite fields.");
            }

            Suns.Add(sun);
            handled = true;
        }

        /// <summary>Performs advance experimental level one waves.</summary>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private void AdvanceExperimentalLevelOneWaves()
        {
            if (RequiredLevelOneTutorial.TutorialBlocksWaveCountdown || LevelOneWavesDispatched >= LevelOneData.WaveCount)
            {
                return;
            }

            _levelOneWaveCountdown -= SimulationStep;
            if (_levelOneWaveCountdown > 0.000000001)
            {
                return;
            }

            var wave = LevelOneWavesDispatched;
            var count = LevelOneData.GetZombieCount(wave);
            for (var index = 0; index < count; index++)
            {
                Zombies.Add(new Zombie {
                    Kind = (ZombieKind)LevelOneData.GetZombieId(wave, index),
                    Row = LevelOneData.ZombieSpawnRow,
                    X = _levelOneZombieSpawnX + index * _levelOneZombieSpacing,
                    Health = _levelOneZombieHealth
                });
                SpawnedZombies++;
            }

            LevelOneWavesDispatched++;
            if (LevelOneWavesDispatched < LevelOneData.WaveCount)
            {
                var delay = _levelOneDelayProvider == null ? _levelOneBetweenWaveDelay : _levelOneDelayProvider(this, wave);
                if (!Finite(delay) || delay < 0.0)
                {
                    throw new InvalidOperationException("The experimental wave-delay callback must return a finite nonnegative value.");
                }

                _levelOneWaveCountdown = delay;
            }
        }

        /// <summary>Performs check experimental level one completion.</summary>
        private void CheckExperimentalLevelOneCompletion()
        {
            if (LevelOneWavesDispatched < LevelOneData.WaveCount)
            {
                return;
            }

            var complete = _levelOneCompletionCondition == null ? Zombies.Count == 0 : _levelOneCompletionCondition(this);
            if (!complete || State != MatchState.Playing)
            {
                return;
            }

            State = MatchState.Won;
            Raise(GameEvent.Won);
        }

        /// <summary>Performs update level one seed usability.</summary>
        private void UpdateLevelOneSeedUsability()
        {
            if (RequiredLevelOneTutorial.Stage == LevelOneTutorialStage.AwaitingUsableSeed && _seedCooldownTicks[LevelOneData.PeashooterSeedId] == 0 && SunAmount >= LevelOneData.PeashooterCost)
            {
                RequiredLevelOneTutorial.OnSeedBecameUsable();
            }
        }

        /// <summary>Performs apply level one tutorial effect.</summary>
        /// <param name = "effect">The effect.</param>
        private void ApplyLevelOneTutorialEffect(LevelOneTutorialEffect effect)
        {
            if (effect.SkySunCountdownNativeTicks.HasValue)
            {
                if (_levelOneUseNativeSkySun)
                {
                    _levelOneNativeSkySunState = new global::PlantsVsZombies.Sun.SkySunState(effect.SkySunCountdownNativeTicks.Value, _levelOneNativeSkySunState.Drops);
                }
                else
                {
                    _nextSkySun = (float)(effect.SkySunCountdownNativeTicks.Value * _levelOneSecondsPerNativeTick);
                }
            }

            if (effect.WaveCountdownAndInitialNativeTicks.HasValue)
            {
                _levelOneWaveCountdown = effect.WaveCountdownAndInitialNativeTicks.Value * _levelOneSecondsPerNativeTick;
            }
        }

        /// <summary>Performs finite.</summary>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting finite.</returns>
        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        /// <summary>Performs mode match kinds.</summary>
        /// <returns>The resulting mode match kinds.</returns>
        private PlantKind[] ModeMatchKinds()
        {
            return new[]
            {
                RequiredModeSession.MatchUpgrades[1] ? PlantKind.FumeShroom : PlantKind.PuffShroom,
                PlantKind.Starfruit,
                PlantKind.MagnetShroom,
                PlantKind.SnowPea,
                RequiredModeSession.MatchUpgrades[2] ? PlantKind.TallNut : PlantKind.Wallnut,
                RequiredModeSession.MatchUpgrades[0] ? PlantKind.Repeater : PlantKind.Peashooter
            };
        }

        /// <summary>Performs initialize mode match.</summary>
        private void InitializeModeMatch()
        {
            Adventure._graves.Clear();
            Plants.Clear();
            for (var r = 0; r < 5; r++)
            {
                for (var c = 0; c < 8; c++)
                {
                    PlaceAdventurePlant(ModePickMatchSeed(r, c, false), r, c);
                }
            }

            RequiredModeSession._matchDelay = 0.2f;
        }

        /// <summary>Performs mode pick match seed.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "col">The col.</param>
        /// <param name = "allowMatches">The allow matches.</param>
        /// <returns>The resulting mode pick match seed.</returns>
        private PlantKind ModePickMatchSeed(int row, int col, bool allowMatches)
        {
            var allowed = new List<PlantKind>();
            foreach (var kind in ModeMatchKinds())
            {
                if (!allowMatches && (col >= 2 && ModeMatchKindAt(row, col - 1) == kind && ModeMatchKindAt(row, col - 2) == kind || row >= 2 && ModeMatchKindAt(row - 1, col) == kind && ModeMatchKindAt(row - 2, col) == kind))
                {
                    continue;
                }

                allowed.Add(kind);
            }

            return allowed[_random.Next(allowed.Count)];
        }

        /// <summary>Performs mode match kind at.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "col">The col.</param>
        /// <returns>The resulting mode match kind at.</returns>
        private PlantKind? ModeMatchKindAt(int row, int col)
        {
            var plant = AdventurePlantAt(row, col, PlantLayer.Main);
            return plant == null ? (PlantKind? )null : plant.Kind;
        }

        /// <summary>Performs mode find matches.</summary>
        /// <param name = "groups">The groups.</param>
        /// <returns>The resulting mode find matches.</returns>
        private HashSet<Plant> ModeFindMatches(out int groups)
        {
            groups = 0;
            var found = new HashSet<Plant>();
            for (var r = 0; r < 5; r++)
            {
                for (var c = 0; c < 8;)
                {
                    var p = AdventurePlantAt(r, c, PlantLayer.Main);
                    var end = c + 1;
                    if (p != null)
                    {
                        while (end < 8 && ModeMatchKindAt(r, end) == p.Kind)
                        {
                            end++;
                        }
                    }

                    if (p != null && end - c >= 3)
                    {
                        groups++;
                        for (var x = c; x < end; x++)
                        {
                            found.Add(AdventurePlantAt(r, x, PlantLayer.Main));
                        }
                    }

                    c = end;
                }
            }

            for (var c = 0; c < 8; c++)
            {
                for (var r = 0; r < 5;)
                {
                    var p = AdventurePlantAt(r, c, PlantLayer.Main);
                    var end = r + 1;
                    if (p != null)
                    {
                        while (end < 5 && ModeMatchKindAt(end, c) == p.Kind)
                        {
                            end++;
                        }
                    }

                    if (p != null && end - r >= 3)
                    {
                        groups++;
                        for (var y = r; y < end; y++)
                        {
                            found.Add(AdventurePlantAt(y, c, PlantLayer.Main));
                        }
                    }

                    r = end;
                }
            }

            return found;
        }

        /// <summary>Performs click mode match cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting click mode match cell.</returns>
        public bool ClickModeMatchCell(int row, int column)
        {
            var s = ModeSession;
            if (s == null || !IsModeMatch || s.Phase != ModePhase.Playing || State != MatchState.Playing || row < 0 || row >= 5 || column < 0 || column >= 8 || s._matchDelay > 0)
            {
                return false;
            }

            var a = AdventurePlantAt(row, column, PlantLayer.Main);
            if (a == null)
            {
                return false;
            }

            if (s.Definition.Mechanic == ModeMechanic.BeghouledTwist)
            {
                if (row >= 4 || column >= 7)
                {
                    return false;
                }

                var b = AdventurePlantAt(row, column + 1, PlantLayer.Main);
                var c = AdventurePlantAt(row + 1, column + 1, PlantLayer.Main);
                var d = AdventurePlantAt(row + 1, column, PlantLayer.Main);
                if (b == null || c == null || d == null)
                {
                    return false;
                }

                a.Column++;
                b.Row++;
                c.Column--;
                d.Row--;
                s.SelectedCell = -1;
                ResolveModeMatch();
                s._matchDelay = 0.25f;
                return true;
            }

            var cell = row * 9 + column;
            if (s.SelectedCell < 0)
            {
                s.SelectedCell = cell;
                return true;
            }

            var old = s.SelectedCell;
            s.SelectedCell = -1;
            var other = AdventurePlantAt(old / 9, old % 9, PlantLayer.Main);
            if (other == null || Math.Abs(other.Row - row) + Math.Abs(other.Column - column) != 1)
            {
                s.SelectedCell = cell;
                return true;
            }

            int r = other.Row, cx = other.Column;
            other.Row = row;
            other.Column = column;
            a.Row = r;
            a.Column = cx;
            int groups;
            ModeFindMatches(out groups);
            if (groups == 0)
            {
                a.Row = row;
                a.Column = column;
                other.Row = r;
                other.Column = cx;
                s.Advice = s.Score == 0 ? "[ADVICE_BEGHOULED_DRAG_TO_MATCH_3]" : "";
                return false;
            }

            ResolveModeMatch();
            s._matchDelay = 0.25f;
            return true;
        }

        /// <summary>Performs resolve mode match.</summary>
        /// <returns>The resulting resolve mode match.</returns>
        private bool ResolveModeMatch()
        {
            int groups;
            var matched = ModeFindMatches(out groups);
            if (groups == 0)
            {
                return false;
            }

            RequiredModeSession.Score += groups;
            RequiredModeSession.SelectedCell = -1;
            foreach (var p in matched)
            {
                Plants.Remove(p);
            }

            // PC score function 0x42C7D0 / .NET BeghouledScore: a group awards 1..5 sun objects.
            var suns = Math.Max(groups, Math.Min(groups * 5, matched.Count - groups * 2));
            for (var i = 0; i < suns; i++)
            {
                Suns.Add(CreateSun(2.5f + (float)_random.NextDouble() * 3f, 1f, 1.5f + (float)_random.NextDouble() * 2f));
            }

            for (var c = 0; c < 8; c++)
            {
                for (var r = 4; r >= 0; r--)
                {
                    if (AdventurePlantAt(r, c, PlantLayer.Main) != null || Adventure._craters.Exists(x => x.Row == r && x.Column == c))
                    {
                        continue;
                    }

                    Plant? falling = null;
                    for (var above = r - 1; above >= 0; above--)
                    {
                        if (Adventure._craters.Exists(x => x.Row == above && x.Column == c))
                        {
                            break;
                        }

                        falling = AdventurePlantAt(above, c, PlantLayer.Main);
                        if (falling != null)
                        {
                            break;
                        }
                    }

                    if (falling != null)
                    {
                        falling.Row = r;
                    }
                    else
                    {
                        PlaceAdventurePlant(ModePickMatchSeed(r, c, true), r, c);
                    }
                }
            }

            return true;
        }

        /// <summary>Performs tick mode match.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModeMatch(float dt)
        {
            var s = RequiredModeSession;
            s._matchDelay = Math.Max(0, s._matchDelay - dt);
            if (s._matchDelay <= 0 && ResolveModeMatch())
            {
                s._matchDelay = 0.25f;
            }

            if (s._matchDelay <= 0)
            {
                for (var r = 0; r < 5; r++)
                {
                    for (var c = 0; c < 8; c++)
                    {
                        if (AdventurePlantAt(r, c, PlantLayer.Main) == null && !Adventure._craters.Exists(x => x.Row == r && x.Column == c))
                        {
                            Adventure._craters.Add(new AdventureCellTimer { Row = r, Column = c, Remaining = float.MaxValue });
                        }
                    }
                }
            }
        }

        /// <param name = "action">0 repeater, 1 fume-shroom, 2 tall-nut, 3 shuffle, 4 fill a crater.</param>
        public bool BuyModeMatchAction(int action)
        {
            var s = ModeSession;
            if (s == null || !IsModeMatch || s.Phase != ModePhase.Playing || State != MatchState.Playing || action < 0 || action > 4 || s._matchDelay > 0)
            {
                return false;
            }

            int[] costs =
            {
                1000,
                500,
                250,
                100,
                200
            };
            if (SunAmount < costs[action] || action < 3 && s.MatchUpgrades[action])
            {
                return false;
            }

            if (action == 4 && Adventure._craters.Count == 0)
            {
                return false;
            }

            SunAmount -= costs[action];
            if (action < 3)
            {
                s.MatchUpgrades[action] = true;
                PlantKind[] from =
                {
                    PlantKind.Peashooter,
                    PlantKind.PuffShroom,
                    PlantKind.Wallnut
                };
                PlantKind[] to =
                {
                    PlantKind.Repeater,
                    PlantKind.FumeShroom,
                    PlantKind.TallNut
                };
                foreach (var p in new PooledSnapshot<Plant>(Plants))
                {
                    if (p.Kind == from[action])
                    {
                        Plants.Remove(p);
                        PlaceAdventurePlant(to[action], p.Row, p.Column);
                    }
                }
            }
            else if (action == 3)
            {
                foreach (var p in new PooledSnapshot<Plant>(Plants))
                {
                    if (p.Column < 8)
                    {
                        Plants.Remove(p);
                    }
                }

                for (var r = 0; r < 5; r++)
                {
                    for (var c = 0; c < 8; c++)
                    {
                        if (!Adventure._craters.Exists(x => x.Row == r && x.Column == c))
                        {
                            PlaceAdventurePlant(ModePickMatchSeed(r, c, false), r, c);
                        }
                    }
                }
            }
            else
            {
                var crater = Adventure._craters[0];
                Adventure._craters.RemoveAt(0);
                PlaceAdventurePlant(ModePickMatchSeed(crater.Row, crater.Column, true), crater.Row, crater.Column);
            }

            s._matchDelay = 0.25f;
            return true;
        }

        /// <summary>Performs tick mode bowling.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModeBowling(float dt)
        {
            var s = RequiredModeSession;
            foreach (var nut in new PooledSnapshot<AdventureBowlingNut>(Adventure._nuts))
            {
                var giant = s._giantNuts.Contains(nut);
                if (AdvanceAdventureBowlingNative(nut, dt, giant))
                {
                    Adventure._nuts.Remove(nut);
                    s._giantNuts.Remove(nut);
                }
            }
        }

        /// <summary>Advances the eight-wave adventure Whack-a-Zombie schedule.</summary>
        /// <param name="dt">The elapsed simulation seconds.</param>
        private void TickAdventureWhack(float dt)
        {
            var a = Adventure;
            var ticks = Math.Max(1, (int)Math.Round(dt * 100f));
            if (WaveSchedule != null)
            {
                WaveSchedule.ElapsedTicks += ticks;
                WaveSchedule.InitialCountdownTicks = a.CurrentWave > 0 ? 2000 : 200;
                WaveSchedule.CountdownTicks = Math.Max(0, (int)Math.Round(a.WaveCountdown * 100f));
                WaveSchedule.HealthAtWaveStart = 0;
                WaveSchedule.HealthToNextWave = -1;
            }

            if (a._finalAmbush)
            {
                return;
            }

            var countdown = Math.Max(0, (int)Math.Round(a.WaveCountdown * 100f) - ticks);
            a.WaveCountdown = countdown * .01f;
            if (a.CurrentWave > 0 && countdown == 100 && !a._whackGravesRaised)
            {
                WhackPlaceGraves(Math.Max(1, 5 - a._graves.Count));
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
                RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.WaveSpawned, a.CurrentWave - 1);
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
            var roll = _random.Next(100);
            var typeRoll = _random.Next(100);
            var final = a.CurrentWave == a.Waves.Count;
            var count = final ? 20 : roll < triple[phase] ? 3 : roll < triple[phase] + pair[phase] ? 2 : 1;
            var kind = typeRoll < bucket[phase] && count < 3 ? ZombieKind.Buckethead : typeRoll < bucket[phase] + cone[phase] ? ZombieKind.Conehead : ZombieKind.Normal;
            var graves = new List<AdventureCellTimer>();
            foreach (var grave in a._graves)
            {
                if (!Plants.Exists(plant => plant.Row == grave.Row && plant.Column == grave.Column && plant.Kind == PlantKind.GraveBuster))
                {
                    graves.Add(grave);
                }
            }

            var progress = Math.Max(0f, Math.Min(1f, (a.CurrentWave - 1) / 11f));
            // PC 0x4315D8 calls the integer curve helper, then stores pixels per native tick.
            var speedMax = final ? 2f : (float)Math.Round(1f + 2f * progress * progress, MidpointRounding.AwayFromZero);
            for (var i = 0; i < count && graves.Count > 0; i++)
            {
                var index = _random.Next(graves.Count);
                var grave = graves[index];
                graves.RemoveAt(index);
                var zombieKind = final ? (_random.Next(2) == 0 ? ZombieKind.Conehead : ZombieKind.Buckethead) : kind;
                var zombie = SpawnAdventureZombie(zombieKind, grave.Row, grave.Column + 0.5f, a.CurrentWave - 1);
                zombie.PresentationNativeVelocity = 0.5f + (float)_random.NextDouble() * (speedMax - 0.5f);
                zombie.Speed = AdventureZombieNativeRules.GridSpeed(zombie.PresentationNativeVelocity);
                zombie.PresentationWhackSpeed = true;
                zombie.DropRemaining = 0.5f;
                InitializeAdventureGraveZombie(zombie, grave.Column);
            }

            var minTicks = (int)Math.Round(100f - 70f * progress, MidpointRounding.AwayFromZero);
            var maxTicks = (int)Math.Round(200f - 140f * progress, MidpointRounding.AwayFromZero);
            a._whackSpawnTimer = _random.Next(minTicks, maxTicks + 1) * 0.01f;
            if (final)
            {
                a.WaveCountdown = 0;
                a._finalAmbush = true;
            }

            if (WaveSchedule != null)
            {
                WaveSchedule.InitialCountdownTicks = a.CurrentWave > 0 ? 2000 : 200;
                WaveSchedule.CountdownTicks = Math.Max(0, (int)Math.Round(a.WaveCountdown * 100f));
                WaveSchedule.IsFinalWave = final;
                WaveSchedule.AmbushComplete = a._finalAmbush;
            }
        }

        // PC 0x4312E0: twelve 2000-tick rounds, native chance arrays and 100..200 -> 30..60 spawn interval.
        /// <summary>Performs tick mode whack.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModeWhack(float dt)
        {
            var a = Adventure;
            var s = ModeSession;
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
            int roll = _random.Next(100), typeRoll = _random.Next(100);
            var final = a.CurrentWave == 12;
            var count = final ? 20 : roll < triple[chance] ? 3 : roll < triple[chance] + pair[chance] ? 2 : 1;
            var kind = typeRoll < bucket[chance] && count < 3 ? ZombieKind.Buckethead : typeRoll < bucket[chance] + cone[chance] ? ZombieKind.Conehead : ZombieKind.Normal;
            var graves = new List<AdventureCellTimer>();
            foreach (var g in a._graves)
            {
                if (!Plants.Exists(p => p.Row == g.Row && p.Column == g.Column && p.Kind == PlantKind.GraveBuster))
                {
                    graves.Add(g);
                }
            }

            var t = Math.Max(0, Math.Min(1, (a.CurrentWave - 1) / 11f));
            var speedMax = final ? 2f : 1f + 2f * t * t;
            for (var i = 0; i < count && graves.Count > 0; i++)
            {
                var index = _random.Next(graves.Count);
                var g = graves[index];
                graves.RemoveAt(index);
                var z = SpawnAdventureZombie(final ? (_random.Next(2) == 0 ? ZombieKind.Conehead : ZombieKind.Buckethead) : kind, g.Row, g.Column + 0.5f, a.CurrentWave - 1);
                z.Speed = 0.5f + (float)_random.NextDouble() * (speedMax - 0.5f);
                z.PresentationNativeVelocity = z.Speed * .8f;
                z.PresentationWhackSpeed = true;
                z.DropRemaining = 0.5f;
                InitializeAdventureGraveZombie(z, g.Column);
            }

            var minTicks = (int)Math.Round(100f - 70f * t);
            var maxTicks = (int)Math.Round(200f - 140f * t);
            s._whackSpawnTimer = _random.Next(minTicks, maxTicks + 1) * 0.01f;
            if (final)
            {
                a.WaveCountdown = 0;
                a._finalAmbush = true;
            }
        }

        /// <summary>Performs mode whack add graves.</summary>
        /// <param name = "count">The count.</param>
        private void ModeWhackAddGraves(int count)
        {
            WhackPlaceGraves(count);
        }

        /// <summary>Performs initialize mode packets.</summary>
        private void InitializeModePackets()
        {
            var s = RequiredModeSession;
            s._nextPacket = s.Definition.Mechanic == ModeMechanic.RainingSeeds ? 1f : 4f;
            switch (s.Definition.Mechanic)
            {
                case ModeMechanic.Bowling:
                    AddAdventurePacket(PlantKind.Wallnut);
                    break;
                case ModeMechanic.Boss:
                    AddAdventurePacket(PlantKind.CabbagePult);
                    AddAdventurePacket(PlantKind.Jalapeno);
                    AddAdventurePacket(PlantKind.CabbagePult);
                    AddAdventurePacket(PlantKind.IceShroom);
                    s._nextPacket = 10f;
                    break;
                case ModeMechanic.Columns:
                    foreach (var k in new[]
                    {
                        PlantKind.PotatoMine,
                        PlantKind.TallNut,
                        PlantKind.MelonPult,
                        PlantKind.MagnetShroom,
                        PlantKind.CoffeeBean,
                        PlantKind.MelonPult
                    }

                    )
                    {
                        AddAdventurePacket(k);
                    }

                    s._nextPacket = 10f;
                    break;
                case ModeMechanic.Slots:
                    s.SlotSymbols[0] = 1;
                    s.SlotSymbols[1] = 0;
                    s.SlotSymbols[2] = 5;
                    break;
                case ModeMechanic.Invisible:
                    AddAdventurePacket(PlantKind.Peashooter);
                    AddAdventurePacket(PlantKind.IceShroom);
                    s._nextPacket = 10f;
                    Adventure.FogBlownRemaining = float.MaxValue;
                    break;
            }
        }

        /// <summary>Performs tick mode packets.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModePackets(float dt)
        {
            var s = RequiredModeSession;
            var m = s.Definition.Mechanic;
            foreach (var rain in new PooledSnapshot<ModeRainPacket>(s.RainPackets))
            {
                rain.Y = Math.Min(rain.TargetY, rain.Y + 45f * dt);
                if (rain.Y >= rain.TargetY)
                {
                    rain.Age += dt;
                }

                if (!Adventure._packets.Exists(p => p.Id == rain.PacketId))
                {
                    s.RainPackets.Remove(rain);
                    continue;
                }

                if (rain.Age >= 7.5f)
                {
                    Adventure._packets.RemoveAll(p => p.Id == rain.PacketId);
                    s.RainPackets.Remove(rain);
                }
            }

            if (m == ModeMechanic.Vases || m == ModeMechanic.Slots)
            {
                foreach (var expiring in new PooledSnapshot<AdventurePacket>(Adventure._packets))
                {
                    if (m == ModeMechanic.Vases)
                    {
                        expiring.Age += dt;
                    }

                    if (expiring.Age >= 15f)
                    {
                        Adventure._packets.Remove(expiring);
                        s._backwardPackets.Remove(expiring.Id);
                    }
                }

                return;
            }

            s._nextPacket -= dt;
            if (s._nextPacket > 0 || Adventure._packets.Count >= 10)
            {
                return;
            }

            int[] weights;
            switch (m)
            {
                case ModeMechanic.Bowling:
                    weights = s.Definition.Id == 33 ? new[]
                    {
                        3,
                        85,
                        49,
                        15,
                        50,
                        15
                    }

                    : new[]
                    {
                        3,
                        85,
                        49,
                        15
                    };
                    break;
                case ModeMechanic.LittleZombies:
                    weights = new[]
                    {
                        16,
                        25,
                        3,
                        15,
                        0,
                        25,
                        2,
                        35
                    };
                    break;
                case ModeMechanic.Portals:
                    weights = new[]
                    {
                        0,
                        25,
                        7,
                        20,
                        22,
                        10,
                        26,
                        15,
                        3,
                        15,
                        2,
                        15
                    };
                    break;
                case ModeMechanic.Columns:
                    weights = new[]
                    {
                        33,
                        155,
                        39,
                        5,
                        6,
                        5,
                        30,
                        15,
                        20,
                        10,
                        17,
                        10
                    };
                    break;
                case ModeMechanic.Invisible:
                    weights = new[]
                    {
                        0,
                        25,
                        3,
                        15,
                        34,
                        5,
                        17,
                        15,
                        16,
                        30,
                        14,
                        10
                    };
                    break;
                case ModeMechanic.Boss:
                    weights = new[]
                    {
                        33,
                        55,
                        39,
                        10,
                        20,
                        12,
                        32,
                        10,
                        34,
                        5,
                        14,
                        8
                    };
                    break;
                case ModeMechanic.RainingSeeds:
                    var rainKinds = new List<int>();
                    foreach (var seed in Adventure.AvailableSeeds)
                    {
                        var id = (int)seed;
                        if (id >= 40 || id == 1 || id == 9 || id == 35 || id == 37 || id == 11 || id == 33)
                        {
                            continue;
                        }

                        rainKinds.Add(id);
                    }

                    var rainKind = rainKinds[_random.Next(rainKinds.Count)];
                    var lilies = Plants.FindAll(p => p.Kind == PlantKind.LilyPad).Count;
                    if (_random.Next(100) < 30 - 29 * Math.Min(18, lilies) / 18)
                    {
                        rainKind = 16;
                    }

                    weights = new[]
                    {
                        rainKind,
                        1
                    };
                    break;
                default:
                    return;
            }

            for (var i = 0; i < weights.Length; i += 2)
            {
                int seed = weights[i], queued = Adventure._packets.FindAll(p => (int)p.Kind == seed).Count, planted = Plants.FindAll(p => (int)p.Kind == seed).Count;
                if (seed == 16 || seed == 33)
                {
                    var limit = seed == 16 ? 18 : m == ModeMechanic.Columns ? 45 : 35;
                    weights[i + 1] = (int)Math.Round(weights[i + 1] + (1 - weights[i + 1]) * Math.Min(limit, queued + planted) / (float)limit);
                }

                if (m == ModeMechanic.Boss && (seed == 32 || seed == 34 || seed == 39))
                {
                    var empty = Plants.FindAll(p => p.Kind == PlantKind.FlowerPot && AdventurePlantAt(p.Row, p.Column, PlantLayer.Main) == null).Count;
                    if (empty <= 2)
                    {
                        weights[i + 1] /= 5;
                    }
                    else if (empty <= 5)
                    {
                        weights[i + 1] /= 3;
                    }
                }

                if (weights.Length > 4)
                {
                    if (queued >= 4)
                    {
                        weights[i + 1] = 1;
                    }
                    else if (queued >= 3)
                    {
                        weights[i + 1] = 5;
                    }
                    else if (Adventure._lastConveyorSeed == (PlantKind)seed)
                    {
                        weights[i + 1] /= 2;
                    }
                }
            }

            var total = 0;
            for (var i = 1; i < weights.Length; i += 2)
            {
                total += weights[i];
            }

            if (total <= 0)
            {
                return;
            }

            int ticket = _random.Next(total), kind = weights[0];
            for (var i = 0; i < weights.Length; i += 2)
            {
                ticket -= weights[i + 1];
                if (ticket < 0)
                {
                    kind = weights[i];
                    break;
                }
            }

            var packet = AddAdventurePacket(kind == 50 ? PlantKind.Wallnut : (PlantKind)kind);
            if (kind == 50)
            {
                s._giantPackets.Add(packet.Id);
            }

            if (m == ModeMechanic.RainingSeeds)
            {
                s.RainPackets.Add(new ModeRainPacket {
                    PacketId = packet.Id,
                    X = 100f + _random.Next(550),
                    Y = 60f,
                    TargetY = 110f + (float)_random.NextDouble() * 340f
                });
            }

            var multiplier = m == ModeMechanic.Boss ? 0.875f : m == ModeMechanic.Portals ? 1.5f : m == ModeMechanic.Invisible ? 2f : m == ModeMechanic.Columns ? 3f : 1f;
            Adventure._lastConveyorSeed = packet.Kind;
            s._nextPacket = m == ModeMechanic.RainingSeeds ? 5f + _random.Next(500) * 0.01f : multiplier * (Adventure._packets.Count > 8 ? 10f : Adventure._packets.Count > 6 ? 5f : Adventure._packets.Count > 4 ? 4.25f : 4f);
        }

        /// <summary>Performs spin mode slots.</summary>
        /// <returns>The resulting spin mode slots.</returns>
        public bool SpinModeSlots()
        {
            var s = ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Slots || s.Phase != ModePhase.Playing || State != MatchState.Playing || s.SlotRollingRemaining > 0 || SunAmount < 25)
            {
                return false;
            }

            SunAmount -= 25;
            s.SlotRollingRemaining = 4f;
            for (var reel = 0; reel < 3; reel++)
            {
                int[] kinds =
                {
                    1,
                    0,
                    5,
                    3,
                    56,
                    57
                };
                int[] weights =
                {
                    100,
                    200 - Math.Min(5, Plants.FindAll(p => p.Kind == PlantKind.Peashooter).Count) * 20,
                    100,
                    100,
                    100,
                    30
                };
                if (reel == 2)
                {
                    for (var i = 0; i < kinds.Length; i++)
                    {
                        if (kinds[i] != 57 && (kinds[i] == s.SlotSymbols[0] || kinds[i] == s.SlotSymbols[1]))
                        {
                            weights[i] += weights[i] / 2;
                        }
                    }
                }

                var total = 0;
                foreach (int w in weights)
                {
                    total += w;
                }

                var ticket = _random.Next(total);
                for (var i = 0; i < kinds.Length; i++)
                {
                    ticket -= weights[i];
                    if (ticket < 0)
                    {
                        s.SlotSymbols[reel] = kinds[i];
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>Performs tick mode slot.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModeSlot(float dt)
        {
            var s = RequiredModeSession;
            if (s.SlotRollingRemaining <= 0)
            {
                return;
            }

            s.SlotRollingRemaining -= dt;
            if (s.SlotRollingRemaining > 0)
            {
                return;
            }

            int a = s.SlotSymbols[0], b = s.SlotSymbols[1], c = s.SlotSymbols[2];
            var matches = a == b && b == c ? 3 : a == b || a == c || b == c ? 2 : 0;
            if (matches == 0)
            {
                s.Advice = "[ADVICE_SLOT_MACHINE_SPIN_AGAIN]";
                return;
            }

            var kind = a == b || a == c ? a : b;
            if (kind == 56)
            {
                var count = matches == 3 ? 20 : 4;
                for (var i = 0; i < count; i++)
                {
                    Suns.Add(CreateSun(3.5f + i * 0.025f, 0f, 0.6f));
                }

                s.Advice = matches == 3 ? "[ADVICE_SLOT_MACHINE_SUN_JACKPOT]" : "[ADVICE_SLOT_MACHINE_2_SUNS]";
            }
            else if (kind == 57)
            {
                for (var i = 0; i < (matches == 3 ? 5 : 1); i++)
                {
                    SpawnAdventureCoin(AdventureCoinKind.Diamond, 4f + i * 0.2f, 0);
                }

                s.Advice = matches == 3 ? "[ADVICE_SLOT_MACHINE_DIAMOND_JACKPOT]" : "[ADVICE_SLOT_MACHINE_2_DIAMONDS]";
            }
            else
            {
                for (var i = 0; i < (matches == 3 ? 3 : 1); i++)
                {
                    AddAdventurePacket((PlantKind)kind);
                }

                s.Advice = matches == 3 ? "[ADVICE_SLOT_MACHINE_3_OF_A_KIND]" : "[ADVICE_SLOT_MACHINE_2_OF_A_KIND]";
            }
        }

        /// <summary>Performs add mode fish.</summary>
        private void AddModeFish()
        {
            var s = RequiredModeSession;
            s.Fish.Add(new ModeFish {
                X = 100f + (float)_random.NextDouble() * 600f,
                Y = 140f + (float)_random.NextDouble() * 300f,
                Angle = (float)_random.NextDouble() * (float)Math.PI * 2,
                SunTimer = 10f + 5f * (float)_random.NextDouble()
            });
        }

        /// <summary>Performs buy mode fish.</summary>
        /// <returns>The resulting buy mode fish.</returns>
        public bool BuyModeFish()
        {
            var s = ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Aquarium || State != MatchState.Playing || SunAmount < 100 || s.Fish.Count > 100)
            {
                return false;
            }

            SunAmount -= 100;
            AddModeFish();
            return true;
        }

        /// <summary>Performs feed mode fish.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <returns>The resulting feed mode fish.</returns>
        public bool FeedModeFish(float x, float y)
        {
            var s = ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Aquarium || State != MatchState.Playing || SunAmount < 5 || s.Food.Count >= 3 || x < 80 || x > 720 || y < 90 || y > 430)
            {
                return false;
            }

            SunAmount -= 5;
            s.Food.Add(new ModeFood { X = x, Y = y });
            return true;
        }

        /// <summary>Performs buy mode aquarium trophy.</summary>
        /// <returns>The resulting buy mode aquarium trophy.</returns>
        public bool BuyModeAquariumTrophy()
        {
            var s = ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Aquarium || State != MatchState.Playing || SunAmount < 1000)
            {
                return false;
            }

            SunAmount -= 1000;
            ClearModeStage();
            return true;
        }

        /// <summary>Performs tick mode aquarium.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModeAquarium(float dt)
        {
            var s = RequiredModeSession;
            foreach (var food in new PooledSnapshot<ModeFood>(s.Food))
            {
                food.Age += dt;
                food.Y += 15f * dt;
                if (food.Y >= 500)
                {
                    s.Food.Remove(food);
                }
            }

            foreach (var fish in new PooledSnapshot<ModeFish>(s.Fish))
            {
                fish.Hunger -= dt;
                fish.SunTimer -= dt;
                if (fish.Hunger <= 0)
                {
                    s.Fish.Remove(fish);
                    continue;
                }

                ModeFood? closest = null;
                var dist = float.MaxValue;
                if (fish.Hunger <= 15f)
                {
                    foreach (var food in s.Food)
                    {
                        if (food.Age >= 0.15f)
                        {
                            float dx = food.X - fish.X, dy = food.Y - fish.Y;
                            var d = dx * dx + dy * dy;
                            if (d < dist)
                            {
                                dist = d;
                                closest = food;
                            }
                        }
                    }
                }

                fish.PhaseRemaining = Math.Max(0, fish.PhaseRemaining - dt);
                if (closest != null)
                {
                    if (dist < 2500)
                    {
                        fish.Hunger = Math.Min(27f, fish.Hunger + 20f);
                        s.Food.Remove(closest);
                        fish.SwimPhase = 1;
                        fish.PhaseRemaining = 1f;
                    }
                    else
                    {
                        fish.Angle = (float)Math.Atan2(closest.Y - fish.Y, closest.X - fish.X);
                        fish.SwimPhase = 0;
                    }
                }
                else if (fish.PhaseRemaining <= 0)
                {
                    var choice = _random.Next(7);
                    fish.PhaseRemaining = 3f + _random.Next(701) * 0.01f;
                    if (choice <= 4)
                    {
                        fish.SwimPhase = 0;
                        fish.Angle = (float)_random.NextDouble() * (float)Math.PI * 2;
                    }
                    else
                    {
                        fish.SwimPhase = 2;
                        fish.Angle = choice == 5 ? 0f : (float)Math.PI;
                    }
                }

                float vx = (float)Math.Cos(fish.Angle), vy = (float)Math.Sin(fish.Angle);
                var wall = fish.X < 50 && vx < 0 || fish.X > 730 && vx > 0 || fish.Y < 140 && vy < 0 || fish.Y > 440 && vy > 0;
                var maxSpeed = fish.SwimPhase == 0 ? 50f : fish.SwimPhase == 2 ? 30f : 5f;
                if (wall)
                {
                    maxSpeed = fish.Speed * 0.3f;
                    fish.PhaseRemaining = Math.Min(1f, fish.PhaseRemaining);
                }
                else if (fish.SwimPhase == 2)
                {
                    if (fish.X < 250 && vx < 0)
                    {
                        fish.Angle = 0;
                    }

                    if (fish.X > 600 && vx > 0)
                    {
                        fish.Angle = 3.1416f;
                    }
                }

                fish.Speed = Math.Min(maxSpeed, fish.Speed + 100f * dt);
                fish.X += vx * fish.Speed * dt;
                fish.Y += vy * fish.Speed * dt;
                if (fish.SunTimer <= 0)
                {
                    Suns.Add(CreateSun((fish.X - 40f) / 80f, (fish.Y - 80f) / (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f), (fish.Y - 80f) / (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f)));
                    fish.SunTimer = 10f + (float)_random.NextDouble() * 5f;
                }
            }

            if (s.Fish.Count == 0)
            {
                State = MatchState.Lost;
                Adventure.Phase = AdventurePhase.Lost;
                s.Phase = ModePhase.Lost;
                Raise(GameEvent.Lost);
            }
        }

        /// <summary>Performs initialize mode portals.</summary>
        private void InitializeModePortals()
        {
            var s = RequiredModeSession;
            s.Portals.Clear();
            s.Portals.Add(new ModePortal { Column = 2, Row = 0, Pair = 0 });
            s.Portals.Add(new ModePortal { Column = 9, Row = 1, Pair = 0 });
            s.Portals.Add(new ModePortal { Column = 9, Row = 3, Pair = 1 });
            s.Portals.Add(new ModePortal { Column = 2, Row = 4, Pair = 1 });
            s._portalTimer = 90f;
        }

        /// <summary>Performs tick mode portals.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModePortals(float dt)
        {
            var s = RequiredModeSession;
            s._portalTimer -= dt;
            if (s._portalTimer <= 0)
            {
                var portal = s.Portals[_random.Next(s.Portals.Count)];
                var cells = new List<int>();
                for (var r = 0; r < 5; r++)
                {
                    for (var c = 0; c < 10; c++)
                    {
                        if (!s.Portals.Exists(p => p != portal && p.Row == r) && !Plants.Exists(p => p.Row == r && p.Column == c))
                        {
                            cells.Add(r * 10 + c);
                        }
                    }
                }

                if (cells.Count > 0)
                {
                    var cell = cells[_random.Next(cells.Count)];
                    portal.Row = cell / 10;
                    portal.Column = cell % 10;
                }

                s._portalTimer = 60f;
            }

            foreach (var z in Zombies)
            {
                z._modePortalCooldown = Math.Max(0, z._modePortalCooldown - dt);
                if (z._modePortalCooldown > 0)
                {
                    continue;
                }

                foreach (var entrance in s.Portals)
                {
                    if (z.Row == entrance.Row && Math.Abs(z.X - (entrance.Column + 0.5f)) < 0.12f)
                    {
                        var exit = s.Portals.Find(p => p != entrance && p.Pair == entrance.Pair);
                        if (exit != null)
                        {
                            z.Row = exit.Row;
                            z.X = exit.Column + (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2 ? 0.65f : 0.35f);
                            z._modePortalCooldown = 0f;
                        }

                        break;
                    }
                }
            }

            foreach (var pea in Peas)
            {
                foreach (var entrance in s.Portals)
                {
                    if (pea.Row == entrance.Row && Math.Abs(pea.X - (entrance.Column + 0.5f)) < 0.055f)
                    {
                        var exit = s.Portals.Find(p => p != entrance && p.Pair == entrance.Pair);
                        if (exit != null)
                        {
                            pea.Row = exit.Row;
                            pea.FlightRow = exit.Row;
                            pea.X = exit.Column + 0.65f;
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>Performs is mode zombie visible.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <returns>The resulting is mode zombie visible.</returns>
        public bool IsModeZombieVisible(Zombie zombie)
        {
            return ModeSession == null || ModeSession.Definition.Mechanic != ModeMechanic.Invisible;
        }

        // PC types: 26 pea, 27 wall-nut, 28 jalapeno, 29 gatling, 30 squash, 31 tall-nut; 0x53F110..0x53F857.
        /// <summary>Performs tick mode special zombies.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModeSpecialZombies(float dt)
        {
            var s = RequiredModeSession;
            foreach (var z in new PooledSnapshot<Zombie>(Zombies))
            {
                var raw = (int)z.Kind;
                if (raw >= 26 && z.ModeHead < 0)
                {
                    z.ModeHead = raw;
                    z.Kind = raw == 32 ? ZombieKind.Gargantuar : ZombieKind.Normal;
                    if (raw == 27)
                    {
                        z.ArmorHealth = 1100f;
                    }
                    else if (raw == 31)
                    {
                        z.ArmorHealth = 2200f;
                    }
                    else if (raw == 28)
                    {
                        z.Health = 500f;
                        z.StartingHealth = 500f;
                        z._modeHeadTimer = 2f * (275 + _random.Next(175)) / (z.Speed * 80f);
                    }
                    else if (raw == 32)
                    {
                        z.Health = 6000f;
                        z.StartingHealth = 6000f;
                        z.Speed = 0.12f;
                    }
                }

                if (s.Definition.Mechanic == ModeMechanic.Speed && z.ModeHead != -2)
                {
                    z.Speed *= 2f;
                    z.ModeHead = -2;
                }

                if (z.ModeHead < 26 || z.ModeHead > 31 || z.Headless || z.FrozenRemaining > 0)
                {
                    continue;
                }

                var headBefore = z._modeHeadTimer;
                z._modeHeadTimer -= dt * (z.ChillRemaining > 0 ? 0.5f : 1f);
                if (z.ModeHead == 26 && z._modeHeadTimer <= 0)
                {
                    s.HeadShots.Add(new ModeHeadShot { X = z.X, Row = z.Row, Hypnotized = z.Hypnotized });
                    z._modeHeadTimer = 1.5f;
                }
                else if (z.ModeHead == 29)
                {
                    foreach (float threshold in new[]
                    {
                        0.69f,
                        0.51f,
                        0.36f,
                        0.18f
                    }

                    )
                    {
                        if (headBefore > threshold && z._modeHeadTimer <= threshold)
                        {
                            s.HeadShots.Add(new ModeHeadShot { X = z.X, Row = z.Row, Hypnotized = z.Hypnotized });
                        }
                    }

                    if (z._modeHeadTimer <= 0)
                    {
                        z._modeHeadTimer = 1.5f;
                    }
                }
                else if (z.ModeHead == 28 && z._modeHeadTimer <= 0)
                {
                    foreach (var p in new PooledSnapshot<Plant>(Plants))
                    {
                        if (p.Row == z.Row)
                        {
                            RemoveAdventurePlantAndStack(p);
                        }
                    }

                    KillAdventureZombie(z);
                    EmitLaneFlame(z.Row);
                    Raise(GameEvent.JalapenoExplosion);
                }
                else if (z.ModeHead == 30 && Plants.Exists(p => p.Row == z.Row && Math.Abs(p.Column + 0.5f - z.X) < 0.8f))
                {
                    foreach (var p in new PooledSnapshot<Plant>(Plants))
                    {
                        if (p.Row == z.Row && Math.Abs(p.Column + 0.5f - z.X) < 1f)
                        {
                            RemoveAdventurePlantAndStack(p);
                        }
                    }

                    KillAdventureZombie(z);
                    Raise(GameEvent.Explosion);
                }
            }

            if (s == null)
            {
                throw new System.InvalidOperationException("The value 's' is not available.");
            }

            foreach (var shot in new PooledSnapshot<ModeHeadShot>(s.HeadShots))
            {
                if (shot.Hypnotized)
                {
                    shot.X += 4.5f * dt;
                    var enemy = Zombies.Find(z => !z.Hypnotized && z.Row == (int)shot.Row && Math.Abs(z.X - shot.X) < 0.35f);
                    if (enemy != null)
                    {
                        HitAdventureZombie(enemy, shot.Damage);
                        s.HeadShots.Remove(shot);
                    }
                    else if (shot.X > 12)
                    {
                        s.HeadShots.Remove(shot);
                    }

                    continue;
                }

                shot.X -= 4.5f * dt;
                var plant = Plants.FindLast(p => p.Row == (int)shot.Row && Math.Abs(p.Column + 0.5f - shot.X) < 0.35f);
                if (plant != null)
                {
                    plant.Health -= shot.Damage;
                    if (plant.Health <= 0)
                    {
                        RemoveAdventurePlantAndStack(plant);
                    }

                    s.HeadShots.Remove(shot);
                }
                else if (shot.X < -1)
                {
                    s.HeadShots.Remove(shot);
                }
            }
        }

        /// <summary>Gets or sets mode session.</summary>
        public ModeSession? ModeSession { get; private set; }

        /// <summary>Gets the mode session, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public ModeSession RequiredModeSession => ModeSession ?? throw new System.InvalidOperationException("The member 'ModeSession' is not available.");

        /// <summary>Stores mode accumulated.</summary>
        private double _modeAccumulated;
        /// <summary>Gets or sets is independent mode.</summary>
        public bool IsIndependentMode
        {
            get
            {
                return ModeSession != null;
            }
        }

        /// <summary>Gets or sets mode simulation remainder.</summary>
        public float ModeSimulationRemainder
        {
            get
            {
                return (float)Math.Max(0d, Math.Min(SimulationStep, _modeAccumulated));
            }
        }

        /// <summary>Gets or sets mode needs final ambush.</summary>
        private bool ModeNeedsFinalAmbush
        {
            get
            {
                var m = RequiredModeSession.Definition.Mechanic;
                return m != ModeMechanic.PeaHeads && m != ModeMechanic.Stars && m != ModeMechanic.Slots && m != ModeMechanic.Beghouled && m != ModeMechanic.BeghouledTwist && m != ModeMechanic.Whack && m != ModeMechanic.Vases && m != ModeMechanic.IZombie && m != ModeMechanic.Boss && m != ModeMechanic.Aquarium;
            }
        }

        /// <summary>Performs configure mode.</summary>
        /// <param name = "session">The session.</param>
        /// <param name = "access">The access.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void ConfigureMode(ModeSession session, AdventureSeedAccess? access)
        {
            if (!IsAdventure || session == null)
            {
                throw new InvalidOperationException("Modes require a prepared battle board.");
            }

            ModeSession = session;
            var a = Adventure;
            var d = session.Definition;
            Rules.AdventureDisableLoot = d.Mechanic == ModeMechanic.IZombie;
            Plants.Clear();
            Zombies.Clear();
            ClearPeas();
            ClearSuns();
            ClearFlames();
            _adventureSunReservation = 0;
            a.ConveyorTicks = 0;
            a._packets.Clear();
            a._vases.Clear();
            a._graves.Clear();
            a._craters.Clear();
            a._ice.Clear();
            a._nuts.Clear();
            a._coins.Clear();
            a._cobShells.Clear();
            a.Tutorial = AdventureTutorial.Complete;
            a.Phase = AdventurePhase.Playing;
            a.CurrentWave = 0;
            a._finalAmbush = false;
            a.VaseStage = 2;
            a._nextSkySun = 4.25f + (float)_random.NextDouble() * 2.75f;
            a._nextPacket = 4f;
            a._nextHammerSun = 8f;
            a.WaveCountdown = d.ChoosesSeeds ? 18f : 2f;
            var available = new List<PlantKind>();
            for (var id = 0; id < session.UnlockedBasicSeeds; id++)
            {
                available.Add((PlantKind)id);
            }

            if (access != null)
            {
                foreach (var kind in access.AdditionalSeeds)
                {
                    available.Add(kind);
                }

                a.ImitaterTarget = access.ImitaterTarget;
            }

            if (d.Mechanic == ModeMechanic.LastStand)
            {
                available.RemoveAll(k => k == PlantKind.Sunflower || k == PlantKind.SunShroom || k == PlantKind.TwinSunflower);
            }

            a.AvailableSeeds = available.AsReadOnly();
            var defaults = new List<PlantKind>
            {
                PlantKind.Sunflower,
                PlantKind.Peashooter,
                PlantKind.Wallnut,
                PlantKind.PotatoMine,
                PlantKind.CherryBomb,
                PlantKind.SnowPea
            };
            if (a.Level.IsNight)
            {
                defaults = new List<PlantKind>
                {
                    PlantKind.SunShroom,
                    PlantKind.PuffShroom,
                    PlantKind.FumeShroom,
                    PlantKind.GraveBuster,
                    PlantKind.HypnoShroom,
                    PlantKind.IceShroom
                };
            }

            if (a.Level.HasPool)
            {
                defaults[2] = PlantKind.LilyPad;
                defaults[3] = PlantKind.TangleKelp;
            }

            if (a.Level.HasRoof)
            {
                defaults = new List<PlantKind>
                {
                    PlantKind.Sunflower,
                    PlantKind.FlowerPot,
                    PlantKind.CabbagePult,
                    PlantKind.KernelPult,
                    PlantKind.Wallnut,
                    PlantKind.CherryBomb
                };
            }

            if (d.Mechanic == ModeMechanic.Stars)
            {
                defaults[1] = PlantKind.Starfruit;
            }

            if (d.Mechanic == ModeMechanic.LastStand)
            {
                defaults = new List<PlantKind>
                {
                    PlantKind.LilyPad,
                    PlantKind.Repeater,
                    PlantKind.Torchwood,
                    PlantKind.TallNut,
                    PlantKind.Pumpkin,
                    PlantKind.Cattail
                };
            }

            defaults.RemoveAll(k => !available.Contains(k));
            foreach (var k in available)
            {
                if (defaults.Count < session.PacketSlots && !defaults.Contains(k))
                {
                    defaults.Add(k);
                }
            }

            a.SelectedSeeds = defaults.AsReadOnly();
            if (d.Mechanic == ModeMechanic.Whack)
            {
                a.SelectedSeeds = Array.AsReadOnly(new[] { PlantKind.PotatoMine, PlantKind.GraveBuster, PlantKind.IceShroom });
            }

            SunAmount = d.Mechanic == ModeMechanic.LastStand ? 5000 : d.Mechanic == ModeMechanic.IZombie ? 150 : d.Puzzle || d.Mechanic == ModeMechanic.Whack || IsModeMatch ? 0 : 50;
            for (var r = 0; r < RowCount; r++)
            {
                Mowers[r].Available = !d.Puzzle && d.Mechanic != ModeMechanic.Aquarium && (!a.Level.HasRoof || Rules.AdventureRoofCleaners);
            }

            if (a.Level.HasRoof)
            {
                for (var r = 0; r < RowCount; r++)
                {
                    for (var c = 0; c < 3; c++)
                    {
                        PlaceAdventurePlant(PlantKind.FlowerPot, r, c);
                    }
                }
            }

            if (a.Level.Terrain == AdventureTerrain.Night && d.Mechanic != ModeMechanic.Vases && d.Mechanic != ModeMechanic.IZombie && d.Mechanic != ModeMechanic.Beghouled && d.Mechanic != ModeMechanic.BeghouledTwist && d.Mechanic != ModeMechanic.Portals)
            {
                InitializeAdventureGraves();
            }

            if (d.Mechanic == ModeMechanic.Vases)
            {
                InitializeModeVases();
            }
            else if (d.Mechanic == ModeMechanic.IZombie)
            {
                InitializeModeIZombie();
            }
            else if (IsModeMatch)
            {
                InitializeModeMatch();
            }
            else if (d.Mechanic == ModeMechanic.Aquarium)
            {
                SunAmount = 50;
                AddModeFish();
                AddModeFish();
            }
            else if (d.Mechanic == ModeMechanic.Portals)
            {
                InitializeModePortals();
            }
            else if (d.Mechanic == ModeMechanic.Bobsled)
            {
                for (var r = 0; r < RowCount; r++)
                {
                    if (!a.Level.IsWaterRow(r))
                    {
                        for (var c = 3; c < 9; c++)
                        {
                            a._ice.Add(new AdventureCellTimer { Row = r, Column = c, Remaining = float.MaxValue });
                        }
                    }
                }
            }

            InitializeModePackets();
            a.Waves = ModeWavePlan.Build(session, _random);
            ResetAdventureWaveSchedule((int)Math.Round(a.WaveCountdown * 100), ModeNeedsFinalAmbush);
            if (d.Mechanic == ModeMechanic.Whack)
            {
                a._finalAmbush = false;
            }

            session.Phase = d.ChoosesSeeds ? ModePhase.ChoosingSeeds : ModePhase.Playing;
            if (d.ChoosesSeeds)
            {
                a.Phase = AdventurePhase.ChoosingSeeds;
            }

            session.Advice = ModeInstruction();
        }

        /// <summary>Gets or sets is mode match.</summary>
        private bool IsModeMatch
        {
            get
            {
                return RequiredModeSession.Definition.Mechanic == ModeMechanic.Beghouled || RequiredModeSession.Definition.Mechanic == ModeMechanic.BeghouledTwist;
            }
        }

        /// <summary>Performs choose mode seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <returns>The resulting choose mode seeds.</returns>
        public bool ChooseModeSeeds(IEnumerable<PlantKind> seeds)
        {
            if (ModeSession == null || ModeSession.Phase != ModePhase.ChoosingSeeds || seeds == null)
            {
                return false;
            }

            var next = new List<PlantKind>();
            foreach (var kind in seeds)
            {
                if (next.Contains(kind) || !ContainsSeed(Adventure.AvailableSeeds, kind) || kind == PlantKind.Imitater && !Adventure.ImitaterTarget.HasValue)
                {
                    return false;
                }

                next.Add(kind);
            }

            if (next.Count != Math.Min(ModeSession.PacketSlots, Adventure.AvailableSeeds.Count))
            {
                return false;
            }

            Adventure.SelectedSeeds = next.AsReadOnly();
            Adventure.Phase = AdventurePhase.Playing;
            ModeSession.Phase = ModeSession.Definition.Mechanic == ModeMechanic.LastStand ? ModePhase.Preparing : ModePhase.Playing;
            return true;
        }

        /// <summary>Performs apply mode purchases.</summary>
        /// <param name = "firstAid">The first aid.</param>
        /// <param name = "placeRake">The place rake.</param>
        public void ApplyModePurchases(bool firstAid, Func<bool>? placeRake)
        {
            if (ModeSession == null)
            {
                return;
            }

            Rules.AdventureFirstAid = firstAid;
            Rules.AdventurePlaceRake = ModeSession.Definition.SupportsRake ? placeRake : null;
            if (Adventure.RakeRow < 0)
            {
                InitializeAdventurePurchases();
            }
        }

        /// <summary>Performs set mode imitater target.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting set mode imitater target.</returns>
        public bool SetModeImitaterTarget(PlantKind kind)
        {
            if (ModeSession == null || ModeSession.Phase != ModePhase.ChoosingSeeds || (int)kind < 0 || (int)kind > 39 || !ContainsSeed(Adventure.AvailableSeeds, PlantKind.Imitater))
            {
                return false;
            }

            Adventure.ImitaterTarget = kind;
            return true;
        }

        /// <summary>Performs plant mode vase cherry.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting plant mode vase cherry.</returns>
        public bool PlantModeVaseCherry(int row, int column)
        {
            if (ModeSession == null || ModeSession.Definition.Mechanic != ModeMechanic.Vases || ModeSession.Phase != ModePhase.Playing)
            {
                return false;
            }

            LastAdventurePlantingFailure = CheckAdventurePlanting(PlantKind.CherryBomb, row, column, false);
            if (LastAdventurePlantingFailure != AdventurePlantingFailure.None)
            {
                return false;
            }

            SpendAdventureSun(Cost(PlantKind.CherryBomb));
            _seedCooldownTicks[(int)PlantKind.CherryBomb] = DefinitionFor(PlantKind.CherryBomb).RechargeThresholdRaw + 1;
            PlaceAdventurePlant(PlantKind.CherryBomb, row, column);
            return true;
        }

        /// <summary>Performs begin mode defense.</summary>
        /// <returns>The resulting begin mode defense.</returns>
        public bool BeginModeDefense()
        {
            if (ModeSession == null || ModeSession.Phase != ModePhase.Preparing)
            {
                return false;
            }

            ModeSession.Phase = ModePhase.Playing;
            Adventure.WaveCountdown = 3f;
            ResetAdventureWaveSchedule(300, ModeNeedsFinalAmbush);
            return true;
        }

        /// <summary>Performs update mode.</summary>
        /// <param name = "dt">The dt.</param>
        public void UpdateMode(float dt)
        {
            if (ModeSession == null || dt < 0f || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                return;
            }

            if (State == MatchState.Won)
            {
                AdvanceAdventureRewardPresentation(dt);
                return;
            }

            if (State != MatchState.Playing || ModeSession.Phase != ModePhase.Playing)
            {
                return;
            }

            _modeAccumulated += dt;
            while (_modeAccumulated + 0.0000001 >= SimulationStep && State == MatchState.Playing && ModeSession.Phase == ModePhase.Playing)
            {
                TickMode((float)SimulationStep);
                _modeAccumulated -= SimulationStep;
                if (Math.Abs(_modeAccumulated) < 0.0000001)
                {
                    _modeAccumulated = 0d;
                }
            }
        }

        /// <summary>Performs tick mode.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickMode(float dt)
        {
            var s = RequiredModeSession;
            var d = s.Definition;
            Elapsed += dt;
            Adventure._ticks++;
            AdventurePresentationTick(dt);
            AdvanceAdventurePacketFeedback(dt);
            for (var i = 0; i < _seedCooldownTicks.Length; i++)
            {
                if (_seedCooldownTicks[i] > 0)
                {
                    _seedCooldownTicks[i]--;
                    if (_seedCooldownTicks[i] == 0)
                    {
                        AdventurePacketReady((PlantKind)i);
                    }
                }
            }

            Adventure.FogBlownRemaining = Math.Max(0f, Adventure.FogBlownRemaining - dt);
            TickCellTimers(Adventure._craters, dt);
            TickCellTimers(Adventure._ice, dt);
            if (d.Mechanic == ModeMechanic.Aquarium)
            {
                TickModeAquarium(dt);
                UpdateSuns(dt);
                return;
            }

            if (!d.Puzzle && d.Mechanic != ModeMechanic.LastStand && (!d.UsesPackets || d.Mechanic == ModeMechanic.Slots) && !IsModeMatch)
            {
                AdvanceAdventureEconomy(dt);
            }

            if (d.Mechanic == ModeMechanic.Slots)
            {
                TickModeSlot(dt);
            }

            if (d.UsesPackets)
            {
                TickModePackets(dt);
            }

            if (d.Mechanic == ModeMechanic.Boss)
            {
                AdvanceAdventureBoss(dt);
            }
            else if (!d.Puzzle)
            {
                if ((d.Mechanic == ModeMechanic.Stars || d.Mechanic == ModeMechanic.Slots || IsModeMatch) && Adventure.CurrentWave >= Adventure.Waves.Count && Adventure.Waves.Count > 0)
                {
                    if (s._continuousWaveTimer < 0)
                    {
                        s._continuousWaveTimer = 25f;
                    }

                    s._continuousWaveTimer -= dt;
                    float health = 0;
                    foreach (var z in Zombies)
                    {
                        if (z.Wave == Adventure.Waves.Count - 1 && !z.Hypnotized && !z.Headless)
                        {
                            health += z.Health + z.ArmorHealth + z.ShieldHealth * 0.2f;
                        }
                    }

                    if (s._continuousWaveTimer < 21f && WaveSchedule != null && health <= WaveSchedule.HealthToNextWave)
                    {
                        s._continuousWaveTimer = Math.Min(2f, s._continuousWaveTimer);
                    }

                    if (s._continuousWaveTimer <= 0)
                    {
                        var last = Adventure.Waves.Count - 1;
                        var repeats = new List<ZombieKind>();
                        foreach (var z in Adventure.Waves[last].Zombies)
                        {
                            repeats.Add(z == ZombieKind.Flag ? ZombieKind.Normal : z);
                        }

                        var waves = new List<AdventureWave>(Adventure.Waves);
                        waves[last] = new AdventureWave
                        {
                            Index = last,
                            IsFlag = false,
                            Zombies = repeats.AsReadOnly()
                        };
                        Adventure.Waves = waves.AsReadOnly();
                        Adventure.CurrentWave = last;
                        s._continuousWaveTimer = -1f;
                        ResetAdventureWaveSchedule(1, false);
                    }
                }

                if (d.Mechanic == ModeMechanic.Whack)
                {
                    TickModeWhack(dt);
                }
                else
                {
                    AdvanceScheduledAdventureWaves(dt);
                }
            }

            if (d.Mechanic == ModeMechanic.IZombie)
            {
                BeforeModeIZombie();
            }

            TickModeBackwardPlants(dt);
            UpdateSuns(dt);
            AdvanceAdventurePlants(dt);
            AdvanceAdventureProjectiles(dt);
            AdvanceAdventureZombies(dt);
            if (State == MatchState.Lost)
            {
                s.Phase = ModePhase.Lost;
                return;
            }

            AdvanceAdventureMowers(dt);
            AdvanceAdventurePurchases(dt);
            if (d.Mechanic == ModeMechanic.Bowling)
            {
                TickModeBowling(dt);
            }
            else if (d.Mechanic != ModeMechanic.Vases)
            {
                AdvanceAdventureSpecials(dt);
            }

            AdvanceAdventureUpgradeWorld(dt);
            TickModeSpecialZombies(dt);
            if (d.Mechanic == ModeMechanic.Portals)
            {
                TickModePortals(dt);
            }

            if (d.Mechanic == ModeMechanic.IZombie)
            {
                AfterModeIZombie(dt);
            }

            if (IsModeMatch)
            {
                TickModeMatch(dt);
            }

            for (var i = Flames.Count - 1; i >= 0; i--)
            {
                Flames[i].Age += dt;
                if (Flames[i].Age >= 1f)
                {
                    RemoveFlameAt(i);
                }
            }

            var cleared = false;
            if (d.Mechanic == ModeMechanic.Vases)
            {
                cleared = Adventure._vases.Count == 0 && Zombies.TrueForAll(z => z.Hypnotized);
            }
            else if (d.Mechanic == ModeMechanic.IZombie)
            {
                cleared = true;
                foreach (bool brain in s.Brains)
                {
                    if (!brain)
                    {
                        cleared = false;
                    }
                }
            }
            else if (d.Mechanic == ModeMechanic.Stars)
            {
                cleared = ModeStarCount() == 14;
            }
            else if (d.Mechanic == ModeMechanic.Slots)
            {
                cleared = SunAmount >= 2000;
            }
            else if (IsModeMatch)
            {
                cleared = s.Score >= 75;
            }
            else if (d.Mechanic == ModeMechanic.Boss)
            {
                cleared = Adventure.BossHealth <= 0f && Zombies.Count == 0 && Adventure._balls.Count == 0;
            }
            else
            {
                cleared = Adventure.CurrentWave >= Adventure.Waves.Count && Adventure._finalAmbush && Zombies.Count == 0;
            }

            if (cleared)
            {
                ClearModeStage();
            }
        }

        /// <summary>Performs clear mode stage.</summary>
        private void ClearModeStage()
        {
            var s = RequiredModeSession;
            if (s.Phase != ModePhase.Playing)
            {
                return;
            }

            s.ClearedStages = s.Stage;
            if (s.Definition.Survival)
            {
                s.Score = s.FlagsCompleted;
            }
            else if (s.Definition.Endless)
            {
                s.Score = s.ClearedStages;
            }
            else
            {
                s.Score = Math.Max(1, s.Score);
            }

            var next = s.Definition.Endless || s.Stage < s.Definition.Stages;
            s.Phase = next ? ModePhase.StageCleared : ModePhase.Won;
            Adventure.Phase = AdventurePhase.Reward;
            State = MatchState.Won;
            Raise(GameEvent.Won);
        }

        /// <summary>Performs continue mode stage.</summary>
        /// <returns>The resulting continue mode stage.</returns>
        public bool ContinueModeStage()
        {
            var s = ModeSession;
            if (s == null || s.Phase != ModePhase.StageCleared)
            {
                return false;
            }

            s.Stage++;
            s.SelectedCell = -1;
            s._failureDelay = 0;
            State = MatchState.Playing;
            Adventure.Phase = AdventurePhase.Playing;
            Zombies.Clear();
            ClearPeas();
            ClearFlames();
            Adventure._nuts.Clear();
            Adventure._balls.Clear();
            Adventure._packets.Clear();
            s._backwardPackets.Clear();
            s._giantPackets.Clear();
            s._giantNuts.Clear();
            s.RainPackets.Clear();
            s.HeadShots.Clear();
            _modeAccumulated = 0;
            _completedBoardAccumulated = 0;
            Adventure.CurrentWave = 0;
            Adventure.TotalWavesSpawned = 0;
            Adventure.FlagsSpawned = 0;
            Adventure._finalAmbush = false;
            Adventure._warned = false;
            Adventure._previousWaveHealth = 0;
            Adventure.WaveCountdown = 18f;
            if (s.Definition.Mechanic == ModeMechanic.Vases)
            {
                Plants.Clear();
                ClearSuns();
                InitializeModeVases();
                s.Phase = ModePhase.Playing;
            }
            else if (s.Definition.Mechanic == ModeMechanic.IZombie)
            {
                Plants.Clear();
                InitializeModeIZombie();
                s.Phase = ModePhase.Playing;
            }
            else if (s.Definition.Mechanic == ModeMechanic.LastStand)
            {
                SunAmount += 250;
                s.Phase = ModePhase.Preparing;
            }
            else
            {
                s.Phase = ModePhase.ChoosingSeeds;
                Adventure.Phase = AdventurePhase.ChoosingSeeds;
            }

            Adventure.Waves = ModeWavePlan.Build(s, _random);
            ResetAdventureWaveSchedule((int)Math.Round(Adventure.WaveCountdown * 100), ModeNeedsFinalAmbush);
            s._continuousWaveTimer = -1f;
            s.Advice = ModeInstruction();
            return true;
        }

        /// <summary>Performs try plant mode.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try plant mode.</returns>
        public bool TryPlantMode(PlantKind kind, int row, int column)
        {
            var s = ModeSession;
            if (s == null || State != MatchState.Playing || (s.Phase != ModePhase.Playing && s.Phase != ModePhase.Preparing) || s.Definition.Puzzle || IsModeMatch || s.Definition.Mechanic == ModeMechanic.Aquarium)
            {
                return false;
            }

            if (s.Definition.UsesPackets)
            {
                return false;
            }

            if (s.Definition.Mechanic == ModeMechanic.Stars && IsModeStarCell(row, column) && kind != PlantKind.Starfruit && kind != PlantKind.Pumpkin)
            {
                return false;
            }

            return TryPlant(kind, row, column);
        }

        /// <summary>Performs try use mode packet.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try use mode packet.</returns>
        public bool TryUseModePacket(int id, int row, int column)
        {
            var s = ModeSession;
            if (s == null || s.Phase != ModePhase.Playing || State != MatchState.Playing)
            {
                return false;
            }

            var p = Adventure._packets.Find(x => x.Id == id);
            if (p == null)
            {
                return false;
            }

            if (s.Definition.Mechanic == ModeMechanic.Columns)
            {
                var placed = false;
                for (var r = 0; r < RowCount; r++)
                {
                    if (CheckAdventurePlanting(p.Kind, r, column, true) == AdventurePlantingFailure.None)
                    {
                        PlaceAdventurePlant(p.Kind, r, column);
                        placed = true;
                    }
                }

                if (placed)
                {
                    RemoveAdventurePacket(p);
                }

                return placed;
            }

            var giant = s._giantPackets.Contains(id);
            if (!TryUseAdventurePacket(id, row, column))
            {
                return false;
            }

            if (giant)
            {
                s._giantPackets.Remove(id);
                if (Adventure._nuts.Count > 0)
                {
                    s._giantNuts.Add(Adventure._nuts[Adventure._nuts.Count - 1]);
                }
            }

            if (s._backwardPackets.Remove(id))
            {
                var plant = AdventurePlantAt(row, column, PlantLayer.Main);
                if (plant != null)
                {
                    plant.ModeFacesLeft = true;
                }
            }

            return true;
        }

        /// <summary>Performs tick mode backward plants.</summary>
        /// <param name = "dt">The dt.</param>
        private void TickModeBackwardPlants(float dt)
        {
            foreach (var p in Plants)
            {
                if (p.ModeFacesLeft && p.Kind != PlantKind.Repeater)
                {
                    p.ActionTimer = 100000f;
                    p._modeFireTimer -= dt;
                    if (p._modeFireTimer <= 0f && Zombies.Exists(z => z.Row == p.Row && z.X < p.Column + 0.5f && !z.Hypnotized))
                    {
                        FireAdventurePea(p, p.Row, true, 20f);
                        FireAdventurePea(p, p.Row, true, 20f).X += 0.3f;
                        p._modeFireTimer = 1.45f;
                    }
                }
            }
        }

        /// <summary>Selects the original introductory advice for the current independent mode.</summary>
        /// <returns>A bracketed original LawnStrings key, or an empty string when no original advice is displayed.</returns>
        public string ModeInstruction()
        {
            var d = RequiredModeSession.Definition;
            if (d.Survival)
            {
                // Original Challenge::InitLevel shows survival advice only before the first stage.
                return RequiredModeSession.Stage == 1 ? d.Endless ? "[ADVICE_SURVIVE_ENDLESS]" : "[ADVICE_SURVIVE_FLAGS]" : "";
            }

            switch (d.Mechanic)
            {
                case ModeMechanic.Vases:
                    // ScaryPotterStart's shovel tutorial belongs only to adventure, not standalone puzzles.
                    return "";
                case ModeMechanic.IZombie:
                    return "[ADVICE_I_ZOMBIE_EAT_ALL_BRAINS]";
                case ModeMechanic.Beghouled:
                    return "[ADVICE_BEGHOULED_DRAG_TO_MATCH_3]";
                case ModeMechanic.BeghouledTwist:
                    return "[ADVICE_BEGHOULED_TWIST_TO_MATCH_3]";
                case ModeMechanic.Stars:
                    return "[ADVICE_FILL_IN_STARFRUIT]";
                case ModeMechanic.Aquarium:
                    return "[ADVICE_ZOMBIQUARIUM_CLICK_TO_FEED]";
                case ModeMechanic.LastStand:
                    return RequiredModeSession.Stage == 1 ? "[ADVICE_SURVIVE_FLAGS]" : "";
                case ModeMechanic.Slots:
                    return "[ADVICE_SLOT_MACHINE_PULL]";
                default:
                    return "";
            }
        }

        /// <summary>Performs is mode star cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "col">The col.</param>
        /// <returns>The resulting is mode star cell.</returns>
        public static bool IsModeStarCell(int row, int col)
        {
            return row == 0 && col == 3 || row == 1 && (col == 3 || col == 4) || row == 2 && col >= 1 && col <= 6 || row == 3 && col >= 3 && col <= 5 || row == 4 && (col == 3 || col == 6);
        }

        /// <summary>Performs mode star count.</summary>
        /// <returns>The resulting mode star count.</returns>
        public int ModeStarCount()
        {
            var n = 0;
            for (var r = 0; r < 5; r++)
            {
                for (var c = 0; c < 9; c++)
                {
                    if (IsModeStarCell(r, c) && Plants.Exists(p => p.Row == r && p.Column == c && p.Kind == PlantKind.Starfruit))
                    {
                        n++;
                    }
                }
            }

            return n;
        }

        // PC 0x4338B0: count,type,zombie,seed. Seed 52 is the left-facing repeater, not an invented plant.
        /// <summary>Stores mode vase recipes.</summary>
        private static readonly int[][] s_modeVaseRecipes =
        {
            new[]
            {
                5,
                1,
                -1,
                0,
                5,
                1,
                -1,
                5,
                5,
                1,
                -1,
                17,
                6,
                2,
                0,
                -1,
                3,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1
            },
            new[]
            {
                7,
                1,
                -1,
                52,
                3,
                1,
                -1,
                5,
                3,
                1,
                -1,
                3,
                2,
                1,
                -1,
                4,
                6,
                2,
                0,
                -1,
                3,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1
            },
            new[]
            {
                6,
                1,
                -1,
                52,
                4,
                1,
                -1,
                5,
                2,
                1,
                -1,
                17,
                3,
                1,
                -1,
                12,
                3,
                1,
                -1,
                3,
                8,
                2,
                0,
                -1,
                2,
                2,
                4,
                -1,
                1,
                2,
                8,
                -1,
                1,
                2,
                15,
                -1
            },
            new[]
            {
                11,
                1,
                -1,
                8,
                4,
                1,
                -1,
                12,
                4,
                1,
                -1,
                52,
                8,
                2,
                15,
                -1,
                7,
                2,
                0,
                -1,
                1,
                2,
                7,
                -1
            },
            new[]
            {
                6,
                1,
                -1,
                52,
                3,
                1,
                -1,
                30,
                4,
                1,
                -1,
                17,
                2,
                1,
                -1,
                12,
                2,
                1,
                -1,
                5,
                3,
                1,
                -1,
                31,
                6,
                2,
                0,
                -1,
                5,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1,
                3,
                2,
                7,
                -1
            },
            new[]
            {
                7,
                1,
                -1,
                52,
                2,
                1,
                -1,
                17,
                5,
                1,
                -1,
                23,
                2,
                1,
                -1,
                18,
                4,
                1,
                -1,
                22,
                7,
                2,
                0,
                -1,
                5,
                2,
                3,
                -1,
                2,
                2,
                7,
                -1,
                1,
                2,
                15,
                -1
            },
            new[]
            {
                13,
                1,
                -1,
                21,
                3,
                1,
                -1,
                3,
                3,
                1,
                -1,
                17,
                10,
                2,
                0,
                -1,
                1,
                2,
                4,
                -1
            },
            new[]
            {
                7,
                1,
                -1,
                8,
                3,
                1,
                -1,
                23,
                5,
                1,
                -1,
                17,
                4,
                1,
                -1,
                52,
                8,
                2,
                15,
                -1,
                4,
                2,
                0,
                -1,
                4,
                2,
                18,
                -1
            },
            new[]
            {
                6,
                1,
                -1,
                52,
                2,
                1,
                -1,
                5,
                2,
                1,
                -1,
                0,
                2,
                1,
                -1,
                18,
                5,
                1,
                -1,
                17,
                1,
                1,
                -1,
                4,
                1,
                1,
                -1,
                3,
                1,
                1,
                -1,
                25,
                8,
                2,
                0,
                -1,
                5,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1,
                1,
                2,
                23,
                -1
            }
        };
        /// <summary>Performs initialize mode vases.</summary>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private void InitializeModeVases()
        {
            var s = RequiredModeSession;
            s._vaseSeeds.Clear();
            s._vaseSun.Clear();
            Adventure._vases.Clear();
            Adventure._packets.Clear();
            var id = s.Definition.Id;
            var min = id == 51 ? 4 : id == 52 || id == 53 || id == 57 ? 3 : 2;
            var cells = new List<int>();
            for (var r = 0; r < 5; r++)
            {
                for (var c = min; c < 9; c++)
                {
                    if (id != 52 || c != 8)
                    {
                        cells.Add(r * 9 + c);
                    }
                }
            }

            var recipe = id == 60 ? new[]
            {
                6,
                1,
                -1,
                52,
                2,
                1,
                -1,
                5,
                1,
                1,
                -1,
                0,
                2,
                1,
                -1,
                18,
                5,
                1,
                -1,
                17,
                1,
                1,
                -1,
                4,
                1,
                1,
                -1,
                3,
                1,
                1,
                -1,
                25,
                1,
                3,
                -1,
                -1,
                8 - Math.Min(8, (s.Stage - 1) / 10),
                2,
                0,
                -1,
                5,
                2,
                4,
                -1,
                1,
                2,
                15,
                -1,
                1 + Math.Min(8, (s.Stage - 1) / 10),
                2,
                23,
                -1
            }

            : s_modeVaseRecipes[id - 51];
            for (var i = 0; i < recipe.Length; i += 4)
            {
                for (var n = 0; n < recipe[i]; n++)
                {
                    if (cells.Count == 0)
                    {
                        throw new InvalidOperationException("PC vase recipe exceeded its legal columns.");
                    }

                    int pick = _random.Next(cells.Count), cell = cells[pick];
                    cells.RemoveAt(pick);
                    var vase = new AdventureVase
                    {
                        Row = cell / 9,
                        Column = cell % 9
                    };
                    if (recipe[i + 1] == 1)
                    {
                        var seed = recipe[i + 3];
                        vase._seed = seed == 52 ? PlantKind.Repeater : (PlantKind)seed;
                        s._vaseSeeds.Add(cell, seed);
                    }
                    else if (recipe[i + 1] == 2)
                    {
                        vase._zombie = (ZombieKind)recipe[i + 2];
                    }
                    else
                    {
                        s._vaseSun.Add(cell, (_random.Next(3) + 1) * 25);
                    }

                    Adventure._vases.Add(vase);
                }
            }

            var green = 0;
            foreach (var vase in Adventure._vases)
            {
                if (vase._seed.HasValue && green < 2)
                {
                    vase.ShowsPlant = true;
                    green++;
                }
            }

            if (id == 52)
            {
                for (var r = 0; r < 5; r++)
                {
                    var cell = r * 9 + 8;
                    Adventure._vases.Add(new AdventureVase {
                        Row = r,
                        Column = 8,
                        _seed = PlantKind.Repeater,
                        ShowsPlant = true
                    });
                    s._vaseSeeds[cell] = 52;
                }
            }

            Adventure.VaseStage = 2;
            Adventure.Tutorial = AdventureTutorial.Complete;
            // PC puzzles keep the cherry-bomb card; seed packets from vases remain free.
            Adventure.SelectedSeeds = Array.AsReadOnly(new[] { PlantKind.CherryBomb });
        }

        /// <summary>Performs smash mode vase.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting smash mode vase.</returns>
        public bool SmashModeVase(int row, int column)
        {
            var s = ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Vases || s.Phase != ModePhase.Playing || State != MatchState.Playing)
            {
                return false;
            }

            var vase = Adventure._vases.Find(v => v.Row == row && v.Column == column);
            if (vase == null)
            {
                return false;
            }

            int cell = row * 9 + column, seed, value;
            Adventure._vases.Remove(vase);
            if (s._vaseSeeds.TryGetValue(cell, out seed))
            {
                var packet = AddAdventurePacket(seed == 52 ? PlantKind.Repeater : (PlantKind)seed, row, column);
                if (seed == 52)
                {
                    s._backwardPackets.Add(packet.Id);
                }

                s._vaseSeeds.Remove(cell);
            }
            else if (s._vaseSun.TryGetValue(cell, out value))
            {
                for (var i = 0; i < value / 25; i++)
                {
                    Suns.Add(CreateAdventurePresentationSunFromPoint(40f + column * 80f + i * 15f, 80f + row * 100f, 25));
                }

                s._vaseSun.Remove(cell);
            }
            else if (vase._zombie.HasValue)
            {
                SpawnAdventureZombie(vase._zombie.Value, row, column + 0.5f, s.Stage - 1);
            }

            return true;
        }

        // PC 0x435B40 fixed and randomly placed plants. Coordinates differ from the mobile .NET port.
        /// <summary>Performs initialize mode izombie.</summary>
        private void InitializeModeIZombie()
        {
            var s = RequiredModeSession;
            Array.Clear(s.Brains, 0, s.Brains.Length);
            Array.Clear(s._brainBite, 0, s._brainBite.Length);
            s._sunflowerHealth.Clear();
            s._sunflowerDrops.Clear();
            switch (s.Definition.Id)
            {
                case 61:
                    ModeFixedPlant(1, 3, 1);
                    ModeFixedPlant(1, 3, 3);
                    ModeRandomPlants(1, 7);
                    ModeRandomPlants(17, 3);
                    ModeRandomPlants(0, 6);
                    ModeRandomPlants(5, 2);
                    break;
                case 62:
                    ModeFixedPlant(21, 3, 0);
                    ModeFixedPlant(1, 2, 0);
                    ModeFixedPlant(1, 3, 3);
                    ModeRandomPlants(21, 1, 0);
                    ModeRandomPlants(0, 1, 0);
                    ModeRandomPlants(5, 2, 3);
                    ModeRandomPlants(1, 1, 3);
                    ModeRandomPlants(1, 4);
                    ModeRandomPlants(21, 2);
                    ModeRandomPlants(5, 2);
                    ModeRandomPlants(0, 4);
                    break;
                case 63:
                    ModeFixedPlant(4, 3, 0);
                    ModeFixedPlant(1, 2, 0);
                    ModeFixedPlant(4, 2, 2);
                    ModeFixedPlant(1, 2, 4);
                    ModeFixedPlant(22, 3, 3);
                    ModeRandomPlants(22, 2);
                    ModeRandomPlants(1, 5);
                    ModeRandomPlants(0, 7);
                    ModeRandomPlants(28, 1);
                    break;
                case 64:
                    for (var r = 0; r < 5; r++)
                    {
                        ModeFixedPlant(3, 3, r);
                    }

                    ModeFixedPlant(1, 2, 0);
                    ModeFixedPlant(1, 2, 2);
                    ModeFixedPlant(1, 2, 4);
                    ModeRandomPlants(0, 1, 0);
                    ModeRandomPlants(5, 1, 1);
                    ModeRandomPlants(10, 2, 2);
                    ModeRandomPlants(5, 1, 3);
                    ModeRandomPlants(0, 1, 4);
                    ModeRandomPlants(0, 2);
                    ModeRandomPlants(1, 4);
                    break;
                case 65:
                    ModeFixedPlant(1, 3, 2);
                    ModeFixedPlant(1, 3, 3);
                    ModeRandomPlants(26, 1, 1);
                    ModeRandomPlants(26, 1, 4);
                    ModeRandomPlants(31, 1);
                    ModeRandomPlants(1, 5);
                    ModeRandomPlants(0, 8);
                    ModeRandomPlants(5, 2);
                    break;
                case 66:
                    ModeFixedPlant(36, 4, 1);
                    ModeFixedPlant(36, 4, 3);
                    ModeRandomPlants(1, 3, 1);
                    ModeRandomPlants(1, 3, 3);
                    ModeRandomPlants(22, 2);
                    ModeRandomPlants(1, 2);
                    ModeRandomPlants(21, 3);
                    ModeRandomPlants(5, 1);
                    ModeRandomPlants(0, 5);
                    ModeRandomPlants(17, 2);
                    ModeRandomPlants(34, 2);
                    break;
                case 67:
                    ModeFixedPlant(1, 4, 2);
                    ModeFixedPlant(1, 4, 4);
                    ModeRandomPlants(1, 6);
                    ModeRandomPlants(4, 9);
                    ModeRandomPlants(6, 8);
                    break;
                case 68:
                    ModeRandomPlants(3, 3);
                    ModeRandomPlants(31, 2);
                    ModeRandomPlants(0, 8);
                    ModeRandomPlants(17, 2);
                    ModeRandomPlants(4, 2);
                    ModeRandomPlants(1, 8);
                    break;
                case 69:
                    ModeFixedPlant(23, 5, 1);
                    ModeFixedPlant(22, 5, 3);
                    ModeRandomPlants(4, 4, 0);
                    ModeRandomPlants(1, 2, 0);
                    ModeRandomPlants(1, 2, 1);
                    ModeRandomPlants(18, 1, 1);
                    ModeRandomPlants(5, 1, 1);
                    ModeRandomPlants(28, 1, 1);
                    ModeRandomPlants(6, 3, 2);
                    ModeRandomPlants(1, 2, 2);
                    ModeRandomPlants(17, 1, 2);
                    ModeRandomPlants(0, 3, 3);
                    ModeRandomPlants(1, 2, 3);
                    ModeRandomPlants(1, 1, 4);
                    ModeRandomPlants(10, 1, 4);
                    ModeRandomPlants(13, 1, 4);
                    ModeRandomPlants(29, 1, 4);
                    ModeRandomPlants(28, 1, 4);
                    ModeRandomPlants(31, 1, 4);
                    break;
                case 70:
                    InitializeModeIZombieEndless();
                    break;
            }

            foreach (var p in Plants)
            {
                p.Asleep = false;
                p._modeFireTimer = 0;
                if (p.Kind == PlantKind.PotatoMine)
                {
                    p.ActionTimer = 0;
                    p.MinePhase = MinePhase.Armed;
                    p.PhaseAge = 20;
                }

                if (p.Kind == PlantKind.Sunflower)
                {
                    p.ActionTimer = 100000;
                    s._sunflowerHealth[p.Id] = p.Health;
                    s._sunflowerDrops[p.Id] = 0;
                }
            }

            int[][] cards =
            {
                new[]
                {
                    0,
                    4,
                    7
                },
                new[]
                {
                    0,
                    6,
                    4
                },
                new[]
                {
                    0,
                    4,
                    17
                },
                new[]
                {
                    0,
                    3,
                    20
                },
                new[]
                {
                    0,
                    8,
                    20,
                    16
                },
                new[]
                {
                    0,
                    3,
                    4,
                    23
                },
                new[]
                {
                    0,
                    3,
                    4,
                    8
                },
                new[]
                {
                    24,
                    2,
                    4,
                    20,
                    17,
                    21
                },
                new[]
                {
                    24,
                    2,
                    3,
                    4,
                    20,
                    17,
                    21,
                    7
                },
                new[]
                {
                    24,
                    2,
                    3,
                    4,
                    20,
                    17,
                    21,
                    7,
                    8
                }
            };
            var result = new List<ZombieKind>();
            foreach (int id in cards[s.Definition.Id - 61])
            {
                result.Add((ZombieKind)id);
            }

            s.ZombieCards = result.AsReadOnly();
        }

        /// <summary>Gets or sets mode izombie columns.</summary>
        internal int ModeIZombieColumns
        {
            get
            {
                var id = RequiredModeSession.Definition.Id;
                return id <= 65 ? 4 : id == 69 ? 6 : 5;
            }
        }

        /// <summary>Performs mode fixed plant.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "row">The row.</param>
        private void ModeFixedPlant(int kind, int column, int row)
        {
            if (AdventurePlantAt(row, column, PlantLayer.Main) == null)
            {
                PlaceAdventurePlant((PlantKind)kind, row, column);
            }
        }

        /// <summary>Performs mode random plants.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "count">The count.</param>
        /// <param name = "row">The row.</param>
        private void ModeRandomPlants(int kind, int count, int row = -1)
        {
            var cells = new List<int>();
            for (var r = 0; r < 5; r++)
            {
                if (row < 0 || r == row)
                {
                    for (var c = 0; c < ModeIZombieColumns; c++)
                    {
                        if (AdventurePlantAt(r, c, PlantLayer.Main) == null && ((kind != 3 && kind != 22) || ModeIZombieColumns - c <= 3))
                        {
                            cells.Add(r * 9 + c);
                        }
                    }
                }
            }

            for (var n = 0; n < count && cells.Count > 0; n++)
            {
                int p = _random.Next(cells.Count), cell = cells[p];
                cells.RemoveAt(p);
                PlaceAdventurePlant((PlantKind)kind, cell / 9, cell % 9);
            }
        }

        /// <summary>Performs initialize mode izombie endless.</summary>
        private void InitializeModeIZombieEndless()
        {
            var stage = RequiredModeSession.Stage - 1;
            var style = _random.Next(5);
            int lo = Math.Min(4, Math.Max(2, 2 + stage / 3)), hi = Math.Min(6, Math.Max(2, 3 + stage / 2));
            if (stage >= 10)
            {
                lo = Math.Min(5, Math.Max(2, 2 + stage / 3));
                hi = Math.Min(7, Math.Max(2, 3 + stage / 2));
            }

            var replace = stage == 0 ? 0 : stage == 1 ? 1 : _random.Next(lo, hi + 1);
            ModeRandomPlants(1, 8 - replace);
            ModeRandomPlants(8, replace);
            if (style == 0 && stage >= 1)
            {
                var branch = _random.Next(5);
                int[][] kinds =
                {
                    new[]
                    {
                        5,
                        9,
                        28,
                        4,
                        7,
                        4
                    },
                    new[]
                    {
                        4,
                        9,
                        6,
                        8
                    },
                    new[]
                    {
                        21,
                        9,
                        29,
                        8
                    },
                    new[]
                    {
                        10,
                        9,
                        31,
                        8
                    },
                    new[]
                    {
                        13,
                        12,
                        1,
                        5
                    }
                };
                var pattern = kinds[branch];
                for (var i = 0; i < pattern.Length; i += 2)
                {
                    ModeRandomPlants(pattern[i], pattern[i + 1]);
                }
            }
            else
            {
                var branch = _random.Next(6);
                var pattern = branch <= 2 ? new[]
                {
                    3,
                    1,
                    22,
                    1,
                    4,
                    1,
                    6,
                    2,
                    0,
                    1,
                    28,
                    1,
                    34,
                    1,
                    18,
                    1,
                    5,
                    1,
                    17,
                    1,
                    10,
                    1,
                    37,
                    1,
                    29,
                    1,
                    31,
                    1,
                    21,
                    2
                }

                : branch <= 4 ? new[]
                {
                    22,
                    1,
                    28,
                    3,
                    7,
                    1,
                    34,
                    3,
                    18,
                    1,
                    5,
                    3,
                    37,
                    1,
                    31,
                    1,
                    21,
                    3
                }

                : new[]
                {
                    4,
                    4,
                    6,
                    3,
                    17,
                    3,
                    10,
                    4,
                    21,
                    3
                };
                for (var i = 0; i < pattern.Length; i += 2)
                {
                    ModeRandomPlants(pattern[i], pattern[i + 1]);
                }
            }
        }

        /// <summary>Performs mode zombie cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting mode zombie cost.</returns>
        public static int ModeZombieCost(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Normal:
                case ZombieKind.Imp:
                    return 50;
                case ZombieKind.Conehead:
                case ZombieKind.PoleVaulter:
                    return 75;
                case ZombieKind.ScreenDoor:
                    return 100;
                case ZombieKind.Buckethead:
                case ZombieKind.Digger:
                case ZombieKind.Bungee:
                    return 125;
                case ZombieKind.Ladder:
                case ZombieKind.Balloon:
                    return 150;
                case ZombieKind.Football:
                case ZombieKind.Zomboni:
                    return 175;
                case ZombieKind.Pogo:
                    return 200;
                case ZombieKind.Gargantuar:
                    return 300;
                case ZombieKind.Dancing:
                    return 350;
                default:
                    return int.MaxValue;
            }
        }

        /// <summary>Performs place mode zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting place mode zombie.</returns>
        public bool PlaceModeZombie(ZombieKind kind, int row, int column)
        {
            var s = ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.IZombie || s.Phase != ModePhase.Playing || State != MatchState.Playing || row < 0 || row >= 5 || column < 0 || column >= 9)
            {
                return false;
            }

            var offered = false;
            foreach (var k in s.ZombieCards)
            {
                if (k == kind)
                {
                    offered = true;
                }
            }

            if (!offered)
            {
                return false;
            }

            if (kind != ZombieKind.Bungee && column < ModeIZombieColumns)
            {
                return false;
            }

            if (kind == ZombieKind.Bungee && (column >= ModeIZombieColumns || AdventurePlantAt(row, column, PlantLayer.Main) == null))
            {
                return false;
            }

            var cost = ModeZombieCost(kind);
            if (AdventureSunAvailable < cost)
            {
                return false;
            }

            if (!SpendAdventureSun(cost))
            {
                return false;
            }

            var z = SpawnAdventureZombie(kind, row, column + 0.5f, s.Stage - 1);
            if (kind == ZombieKind.Bungee)
            {
                z.Row = row;
                z.X = column + 0.5f;
                z.TargetColumn = column;
            }

            if (kind == ZombieKind.Gargantuar)
            {
                z.ThrownImp = true;
            }

            Raise(GameEvent.Planted);
            return true;
        }

        /// <summary>Performs before mode izombie.</summary>
        private void BeforeModeIZombie()
        {
            var s = ModeSession;
            foreach (var p in Plants)
            {
                if (p.Kind == PlantKind.Sunflower)
                {
                    p.ActionTimer = 100000;
                }
            }

            foreach (var z in new PooledSnapshot<Zombie>(Zombies))
            {
                if (!z.Hypnotized && !z.Headless && z.Kind != ZombieKind.Bungee && !(z.Kind == ZombieKind.Digger && z.AbilityPhase == 2) && z.X < 0.2f)
                {
                    if (s == null)
                    {
                        throw new System.InvalidOperationException("The value 's' is not available.");
                    }

                    if (s.Brains[z.Row])
                    {
                        Zombies.Remove(z);
                        continue;
                    }

                    z.X = 0.16f;
                    z.Eating = true;
                    s._brainBite[z.Row] += 0.01f;
                    if (s._brainBite[z.Row] >= 0.7f)
                    {
                        s.Brains[z.Row] = true;
                        Zombies.Remove(z);
                    }
                }
            }
        }

        /// <summary>Performs after mode izombie.</summary>
        /// <param name = "dt">The dt.</param>
        private void AfterModeIZombie(float dt)
        {
            var s = RequiredModeSession;
            foreach (var id in new List<int>(s._sunflowerHealth.Keys))
            {
                var p = Plants.Find(x => x.Id == id);
                var lost = s._sunflowerHealth[id] - (p == null ? 0 : p.Health);
                var drops = p == null ? 8 : Math.Min(8, (int)(lost / 40f));
                var old = s._sunflowerDrops[id];
                for (var i = old; i < drops; i++)
                {
                    Suns.Add(CreateSun(p == null ? 1f : p.Column + 0.5f, p == null ? 2f : p.Row + 0.5f, p == null ? 2f : p.Row + 0.7f));
                }

                s._sunflowerDrops[id] = drops;
            }

            var available = SunAmount - _adventureSunReservation;
            foreach (global::PlantsVsZombies.Simulation.Sun sun in Suns)
            {
                available += sun.Value;
            }

            if (Zombies.Count == 0 && available < 50)
            {
                s._failureDelay += dt;
                if (s._failureDelay >= 0.5f)
                {
                    State = MatchState.Lost;
                    Adventure.Phase = AdventurePhase.Lost;
                    s.Phase = ModePhase.Lost;
                    Raise(GameEvent.Lost);
                }
            }
            else
            {
                s._failureDelay = 0;
            }
        }

        /// <summary>Stores jalapeno damage.</summary>
        private const float JalapenoDamage = 1800f;
        /// <summary>Performs update extended plant.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting update extended plant.</returns>
        private bool UpdateExtendedPlant(int index, Plant plant)
        {
            if (plant.Kind == PlantKind.TallNut)
            {
                return true;
            }

            if (plant.Kind != PlantKind.Jalapeno)
            {
                return false;
            }

            // One hundred 0.01-second float steps can leave a tiny positive remainder.
            if (plant.ActionTimer > 0.000001f)
            {
                return true;
            }

            for (var z = Zombies.Count - 1; z >= 0; z--)
            {
                var zombie = Zombies[z];
                if (zombie.Row != plant.Row)
                {
                    continue;
                }

                switch (zombie.Kind)
                {
                    case ZombieKind.Normal:
                    case ZombieKind.Flag:
                    case ZombieKind.Conehead:
                    case ZombieKind.Buckethead:
                    case ZombieKind.ScreenDoor:
                        zombie.ChillRemaining = 0f;
                        DamageZombie(z, JalapenoDamage);
                        break;
                }
            }

            Plants.RemoveAt(index);
            EmitLaneFlame(plant.Row);
            Raise(GameEvent.JalapenoExplosion);
            return true;
        }

        /// <summary>Gets or sets battle random draw count.</summary>
        public ulong BattleRandomDrawCount
        {
            get
            {
                var original = _random as OriginalRandomAdapter;
                return original == null ? 0 : original.Generator.DrawCount;
            }
        }

        /// <summary>Performs capture battle random state.</summary>
        /// <returns>The resulting capture battle random state.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public OriginalRandomState CaptureBattleRandomState()
        {
            var original = _random as OriginalRandomAdapter;
            if (original == null)
            {
                throw new InvalidOperationException("GameSimulation must use OriginalRandomAdapter.");
            }

            return original.CaptureState();
        }

        /// <summary>Performs restore battle random state.</summary>
        /// <param name = "snapshot">The snapshot.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void RestoreBattleRandomState(OriginalRandomState snapshot)
        {
            var original = _random as OriginalRandomAdapter;
            if (original == null)
            {
                throw new InvalidOperationException("GameSimulation must use OriginalRandomAdapter.");
            }

            original.RestoreState(snapshot);
        }

        /// <summary>Gets or sets wave schedule.</summary>
        public global::PlantsVsZombies.Waves.AdventureWaveScheduleStatus? WaveSchedule { get; private set; }

        /// <summary>Gets the wave schedule, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public global::PlantsVsZombies.Waves.AdventureWaveScheduleStatus RequiredWaveSchedule => WaveSchedule ?? throw new System.InvalidOperationException("The member 'WaveSchedule' is not available.");

        /// <summary>Stores wave schedule event raised.</summary>
        public Action<global::PlantsVsZombies.Waves.WaveScheduleEvent>? WaveScheduleEventRaised;
        /// <summary>Stores scheduled wave remainder.</summary>
        private double _scheduledWaveRemainder;
        /// <summary>Stores scheduled ice trap ticks.</summary>
        private int _scheduledIceTrapTicks;
        /// <summary>Stores scheduled final ambush enabled.</summary>
        private bool _scheduledFinalAmbushEnabled = true;
        /// <summary>Stores scheduled honor tutorials.</summary>
        private bool _scheduledHonorTutorials = true;
        /// <summary>Stores first tutorial plant observed.</summary>
        private bool _firstTutorialPlantObserved;
        /// <summary>Performs notify adventure ice trap.</summary>
        /// <param name = "ticks">The ticks.</param>
        public void NotifyAdventureIceTrap(int ticks = 300)
        {
            _scheduledIceTrapTicks = Math.Max(_scheduledIceTrapTicks, Math.Max(0, ticks));
        }

        // Local EXE: 0x41FDD0 (100 Hz countdown), 0x41EF60 (health), 0x41F010 (200-tick final ambush).
        /// <summary>Performs initialize adventure wave scheduler.</summary>
        private void InitializeAdventureWaveScheduler()
        {
            if (!IsAdventure || _adventure == null)
            {
                return;
            }

            var firstCountdown = Adventure.Level.Mode == AdventureMode.WhackAZombie ? 200 : Adventure.Level.Number == 2 && !Adventure.Level.IsReplay ? 5000 : 1800;
            ResetAdventureWaveSchedule(firstCountdown, true, true);
        }

        /// <summary>After replacing Adventure.Waves/current wave, reset only the scheduler. It preserves CurrentWave and FlagsSpawned.</summary>
        public void ResetAdventureWaveSchedule(int firstCountdownTicks = -1, bool includeFinalAmbush = true, bool honorAdventureTutorials = false)
        {
            if (!IsAdventure || _adventure == null)
            {
                return;
            }

            var first = firstCountdownTicks < 0 ? Math.Max(1, (int)Math.Round(Adventure.WaveCountdown * 100.0)) : Math.Max(1, firstCountdownTicks);
            _scheduledFinalAmbushEnabled = includeFinalAmbush;
            _scheduledHonorTutorials = honorAdventureTutorials;
            WaveSchedule = new global::PlantsVsZombies.Waves.AdventureWaveScheduleStatus
            {
                CountdownTicks = first,
                InitialCountdownTicks = first,
                HealthToNextWave = -1,
                AmbushComplete = !includeFinalAmbush || Adventure.Level.Mode == AdventureMode.Boss || Adventure.Level.Mode == AdventureMode.Vasebreaker
            };
            _scheduledWaveRemainder = 0;
            _scheduledIceTrapTicks = 0;
            _firstTutorialPlantObserved = false;
            Adventure._finalAmbush = WaveSchedule.AmbushComplete;
            Adventure._warned = false;
            SynchronizeScheduledWaveFields();
        }

        /// <summary>Performs advance scheduled adventure waves.</summary>
        /// <param name = "dt">The dt.</param>
        public void AdvanceScheduledAdventureWaves(float dt)
        {
            if (!IsAdventure || _adventure == null)
            {
                return;
            }

            if (WaveSchedule == null)
            {
                InitializeAdventureWaveScheduler();
            }

            if (WaveSchedule == null || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                return;
            }

            var a = Adventure;
            // PC 0x41FDD0 blocks tutorial states 1/2/3, not the sunflower lesson (states 5/6/7).
            // The local tutorial enum is coarser; use the accepted live plant array to recognize the release.
            if (_scheduledHonorTutorials && !a.Level.IsReplay && a.CurrentWave == 0)
            {
                int peashooters = 0, sunflowers = 0;
                foreach (Plant plant in Plants)
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

                if (a.Level.Number == 1 && !WaveSchedule.TutorialReleased)
                {
                    if (!_firstTutorialPlantObserved && peashooters > 0)
                    {
                        _firstTutorialPlantObserved = true;
                        a._nextSkySun = 2f; // PC0x41CBD0: +0x5550=200 after first accepted placement.
                    }

                    if (peashooters < 2)
                    {
                        return;
                    }

                    SetAdventureTutorialWaveCountdown(100); // local EXE 0x420970 state4 (mobile port says99).
                }
                else if (a.Level.Number == 2 && !WaveSchedule.TutorialReleased && sunflowers >= 3)
                {
                    SetAdventureTutorialWaveCountdown(1000); // local EXE state8, while currentWave==0.
                }
                else if (a.Level.Number == 5 && a.Tutorial == AdventureTutorial.Shovel)
                {
                    return;
                }
            }

            _scheduledWaveRemainder += dt;
            while (_scheduledWaveRemainder + 0.00000001 >= 0.01)
            {
                _scheduledWaveRemainder -= 0.01;
                AdvanceScheduledAdventureWaveTick();
            }

            if (Math.Abs(_scheduledWaveRemainder) < 0.0000001)
            {
                _scheduledWaveRemainder = 0;
            }

            SynchronizeScheduledWaveFields();
        }

        /// <summary>Performs set adventure tutorial wave countdown.</summary>
        /// <param name = "ticks">The ticks.</param>
        private void SetAdventureTutorialWaveCountdown(int ticks)
        {
            RequiredWaveSchedule.CountdownTicks = RequiredWaveSchedule.InitialCountdownTicks = ticks;
            RequiredWaveSchedule.HealthToNextWave = -1;
            RequiredWaveSchedule.TutorialReleased = true;
            SynchronizeScheduledWaveFields();
        }

        /// <summary>Performs advance scheduled adventure wave tick.</summary>
        private void AdvanceScheduledAdventureWaveTick()
        {
            var s = RequiredWaveSchedule;
            var a = Adventure;
            s.ElapsedTicks++;
            if (s.FinalWaveSoundTicks > 0 && --s.FinalWaveSoundTicks == 0)
            {
                RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.FinalWaveSound, Math.Max(0, a.Waves.Count - 1));
            }

            if (_scheduledIceTrapTicks > 0)
            {
                _scheduledIceTrapTicks--;
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
            var flagNext = a.CurrentWave < a.Waves.Count && a.Waves[a.CurrentWave].IsFlag;
            if (flagNext && a.Level.Mode != AdventureMode.Bowling)
            {
                s.HealthToNextWave = 0;
                s.CountdownTicks = 4500;
            }
            else
            {
                s.HealthToNextWave = (int)((0.5 + _random.NextDouble() * 0.15) * s.HealthAtWaveStart);
                s.CountdownTicks = a.Level.Mode == AdventureMode.LittleTrouble ? 750 : 2500 + _random.Next(600);
            }

            s.InitialCountdownTicks = s.CountdownTicks;
            if (s.IsFinalWave)
            {
                s.AmbushTicks = _scheduledFinalAmbushEnabled ? 200 : 0;
                s.AmbushComplete = !_scheduledFinalAmbushEnabled;
                a._finalAmbush = !_scheduledFinalAmbushEnabled;
            }
        }

        /// <summary>Performs scheduled wave health.</summary>
        /// <param name = "wave">The wave.</param>
        /// <returns>The resulting scheduled wave health.</returns>
        private int ScheduledWaveHealth(int wave)
        {
            var total = 0;
            foreach (Zombie z in Zombies)
            {
                if (z.Wave == wave && z.Health > 0 && !z.Headless && !z.Hypnotized && z.Kind != ZombieKind.Bungee && !z.BungeeCarrier && z.LinkedZombieId == 0)
                {
                    total += Math.Max(0, (int)z.Health) + Math.Max(0, (int)z.ArmorHealth) + Math.Max(0, (int)(z.ShieldHealth * 0.2f));
                }
            }

            return total;
        }

        /// <summary>Performs announce scheduled wave.</summary>
        private void AnnounceScheduledWave()
        {
            var index = Adventure.CurrentWave;
            var nativeMode = ModeSession == null ? 0 : ModeSession.Definition.Id;
            var finalSurvival = ModeSession != null && ModeSession.Definition.Survival && !ModeSession.Definition.Endless && ModeSession.Stage >= ModeSession.Definition.Stages;
            if (index == Adventure.Waves.Count - 1 && global::PlantsVsZombies.Waves.AdventureWaveNoticeRules.AnnounceFinal(nativeMode, finalSurvival, nativeMode == 0 && Adventure.Level.Mode == AdventureMode.Boss))
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
                var whack = nativeMode == 30 || nativeMode == 0 && Adventure.Level.Mode == AdventureMode.WhackAZombie;
                if (whack ? index == Adventure.Waves.Count - 1 : Adventure.Waves[index].IsFlag)
                {
                    RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind.SirenSound, index);
                }
            }
        }

        /// <summary>Performs spawn scheduled adventure wave.</summary>
        /// <param name = "wave">The wave.</param>
        private void SpawnScheduledAdventureWave(AdventureWave wave)
        {
            var a = Adventure;
            var spawned = 0;
            foreach (ZombieKind entry in wave.Zombies)
            {
                var kind = entry == ZombieKind.Bobsled && !a._ice.Exists(c => c.Column < 8) ? ZombieKind.Normal : entry;
                var row = ChooseAdventureZombieRow(kind);
                var x = 9.5f + spawned++ * 0.12f;
                if (a.Level.Mode == AdventureMode.WhackAZombie && a._graves.Count > 0)
                {
                    var grave = a._graves[_random.Next(a._graves.Count)];
                    row = grave.Row;
                    x = grave.Column + 0.5f;
                }

                if (entry == ZombieKind.Bobsled)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var sled = SpawnAdventureZombie(kind, row, x + i * 0.2f, wave.Index);
                        if (i > 0 && sled.Kind == ZombieKind.Bobsled)
                        {
                            // Original ZombieInitialize gives the 300 helm health only to the leader (no parent zombie).
                            sled.ArmorHealth = 0f;
                        }
                    }
                }
                else if (a.Level.Mode == AdventureMode.Bungee && kind != ZombieKind.Bungee && kind != ZombieKind.Zomboni)
                {
                    DropAdventureZombie(kind, row, _random.Next(4, 9), wave.Index);
                }
                else
                {
                    SpawnAdventureZombie(kind, row, x, wave.Index);
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
        private void SpawnScheduledFinalAmbush()
        {
            var a = Adventure;
            var wave = Math.Max(0, a.Waves.Count - 1);
            // Ice-shroom freezes the native pool/roof ambush; no replacement timer or invented enemy is added.
            var iced = _scheduledIceTrapTicks > 0;
            if (a.Level.HasPool && !iced)
            {
                var n = a.Level.Number;
                var count = n == 21 || n == 22 || n == 31 || n == 32 ? 2 : 3;
                var cells = new List<int>();
                for (var c = 5; c < Columns; c++)
                {
                    for (var r = 2; r <= 3; r++)
                    {
                        cells.Add(c * 10 + r);
                    }
                }

                for (var i = 0; i < count && cells.Count > 0; i++)
                {
                    int pick = _random.Next(cells.Count), cell = cells[pick];
                    cells.RemoveAt(pick);
                    var z = SpawnAdventureZombie(PickScheduledAmbushZombie(false), cell % 10, cell / 10 + 0.5f, wave);
                    z.DropRemaining = 0.5f;
                    a.Notice(AdventureNoticeKind.PoolAmbush, (int)z.Kind, z.Row, cell / 10);
                }
            }
            else if (a.Level.HasRoof && !iced)
            {
                var count = a.Level.Number <= 42 ? 2 : 3;
                var cells = new List<int>();
                for (var c = 4; c < Columns; c++)
                {
                    for (var r = 0; r < RowCount; r++)
                    {
                        cells.Add(c * 10 + r);
                    }
                }

                for (var i = 0; i < count && cells.Count > 0; i++)
                {
                    int pick = _random.Next(cells.Count), cell = cells[pick];
                    cells.RemoveAt(pick);
                    DropAdventureZombie(PickScheduledAmbushZombie(false), cell % 10, cell / 10, wave);
                }
            }

            if (a.Level.Terrain == AdventureTerrain.Night && a.Level.Mode != AdventureMode.WhackAZombie)
            {
                foreach (var grave in a._graves)
                {
                    var z = SpawnAdventureZombie(PickScheduledAmbushZombie(true), grave.Row, grave.Column + 0.5f, wave);
                    InitializeAdventureGraveZombie(z, grave.Column);
                    a.Notice(AdventureNoticeKind.GraveRise, (int)z.Kind, grave.Row, grave.Column);
                }
            }
        }

        /// <summary>Starts the original land grave ambush without treating the zombie as a falling actor.</summary>
        /// <param name="zombie">The new zombie created by the final-wave grave ambush.</param>
        /// <param name="column">The grave's lawn column.</param>
        private void InitializeAdventureGraveZombie(Zombie zombie, int column)
        {
            var nativeX = 40f + column * 80f - 25f;
            var whack = Adventure.Level.Mode == AdventureMode.WhackAZombie || ModeSession != null && ModeSession.Definition.Mechanic == ModeMechanic.Whack;
            zombie.X = AdventureZombieNativeRules.GridX(nativeX, zombie.IsSmall ? .5f : 1f);
            zombie.PresentationPreviousX = zombie.X;
            zombie.GraveRiseTicks = whack ? 50 : AdventureZombieRiseRules.DurationTicks;
            zombie.PresentationWhackSpeed = whack;
            zombie.PresentationNativePhase = 14;
            zombie.DropRemaining = 0f;
            zombie.Eating = false;
            // PC 0x54DCCF uses mPosX+60,mPosY+110 for the original ZombieRise particle.
            Presentation.ZombieRiseEffects.Add(new AdventureZombieRiseEffect
            {
                Id = ++Presentation._effectId,
                Row = zombie.Row,
                X = nativeX + 60f,
                Y = AdventureBiteNativeY(zombie, (int)nativeX) + 110f,
                Whack = whack
            });
            if (whack)
            {
                Presentation.ZombieRiseSoundSerial++;
            }
            else
            {
                RequestAdventureZombieFoley(79);
            }
        }

        /// <summary>Performs pick scheduled ambush zombie.</summary>
        /// <param name = "graves">The graves.</param>
        /// <returns>The resulting pick scheduled ambush zombie.</returns>
        private ZombieKind PickScheduledAmbushZombie(bool graves)
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
            var table = VerifiedDataCatalog.AdventureLevels[Adventure.Level.Number - 1];
            foreach (int id in ids)
            {
                var d = VerifiedDataCatalog.Zombies[id];
                if (id != 0 && ((!Adventure.Level.IsReplay && Adventure.Level.Number < d.BaseLevelGateRaw) || table.ZombieFlagsRaw[id] == 0))
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

            var ticket = _random.Next(total);
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
        private void RaiseScheduledWaveEvent(global::PlantsVsZombies.Waves.WaveScheduleEventKind kind, int index)
        {
            if (WaveScheduleEventRaised != null)
            {
                WaveScheduleEventRaised(new global::PlantsVsZombies.Waves.WaveScheduleEvent { Kind = kind, WaveIndex = index, Tick = RequiredWaveSchedule.ElapsedTicks });
            }
        }

        /// <summary>Performs synchronize scheduled wave fields.</summary>
        private void SynchronizeScheduledWaveFields()
        {
            Adventure.WaveCountdown = RequiredWaveSchedule.RemainingSeconds;
            Adventure._waveInitialCountdown = RequiredWaveSchedule.InitialCountdownTicks * 0.01f;
            Adventure._waveTriggerHealth = RequiredWaveSchedule.HealthToNextWave;
            Adventure._previousWaveHealth = RequiredWaveSchedule.HealthAtWaveStart;
        }
    }
}
