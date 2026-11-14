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
            if (IsAdventure && (State == MatchState.Paused || State == MatchState.Lost))
            {
                AdvanceAdventureStormPresentation(deltaTime, State == MatchState.Paused);
                return;
            }

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
                if (plant.Kind == PlantKind.UmbrellaLeaf && plant.Health > 0f && plant.BungeeState != 2
                    && Math.Abs(plant.Row - row) <= 1 && Math.Abs(plant.Column - column) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Stores the shared original dance frame clock in native board updates.</summary>
        private int _dancerClockTicks;

        /// <summary>Advances the original dancer phase and fills only this leader's vacant backup slots.</summary>
        /// <param name="zombie">The live dancer being advanced.</param>
        private void AdvanceAdventureDancer(Zombie zombie)
        {
            if (zombie.DancerState == null)
            {
                AdventureDancerRules.Initialize(zombie, zombie.Kind == ZombieKind.Dancing ? _random.Next(12) : 0);
            }

            var previousPhase = zombie.PresentationNativePhase;
            var needsBackup = zombie.Kind == ZombieKind.Dancing
                && AdventureDancerRules.NeedsBackup(zombie, Zombies, Presentation.Corpses, Adventure.Level);
            var chilled = AdventureDancerRules.GroupAffected(zombie, Zombies, true);
            if (!AdventureDancerRules.Tick(zombie, _dancerClockTicks, needsBackup, chilled))
            {
                return;
            }

            if (previousPhase == 41)
            {
                var enemies = 0;
                foreach (var other in Zombies)
                {
                    if (other.Health > 0f && !other.Headless && !other.Hypnotized)
                    {
                        enemies++;
                    }
                }

                if (enemies <= 15)
                {
                    RequestAdventureZombieFoley(100);
                }
            }

            if (zombie.Headless)
            {
                return;
            }

            var state = zombie.DancerState!;
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            for (var slot = 0; slot < 4; slot++)
            {
                var row = zombie.Row + (slot == 0 ? -1 : slot == 1 ? 1 : 0);
                if (!AdventureDancerRules.CanSummonInRow(Adventure.Level, row)
                    || AdventureDancerRules.Occupied(Zombies, Presentation.Corpses, state.Followers[slot]))
                {
                    continue;
                }

                // PC 0x5443F0: four independently owned slots, exactly one hundred native pixels apart.
                var backupX = nativeX + (slot == 2 ? -100 : slot == 3 ? 100 : 0);
                var backup = SpawnAdventureZombie(ZombieKind.BackupDancer, row, 0f, zombie.Wave);
                backup.X = AdventureZombieNativeRules.GridX(backupX, AdventureDancerRules.Scale(backup));
                backup.PresentationPreviousX = backup.X;
                backup.Hypnotized = zombie.Hypnotized;
                backup.PresentationNativePhase = 50;
                backup.PresentationAnimationAge = 0f;
                backup.DancerState!.LeaderId = zombie.Id;
                backup.DancerState.PhaseTicks = AdventureDancerRules.RiseTicks;
                state.Followers[slot] = backup.Id;
                Presentation.ZombieRiseEffects.Add(new AdventureZombieRiseEffect
                {
                    Id = ++Presentation._effectId,
                    Row = row,
                    X = backupX + 60f,
                    Y = AdventureBiteNativeY(backup, backupX) + 110f,
                    Dancer = true
                });
                RequestAdventureZombieFoley(79);
            }
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

                var x = (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
                var backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z));
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
                        BlastAdventureRadius(nut.NativePixelX + 40, nut.NativePixelY + 40, nut.Row, 90, 1, 0x21);
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
            BlastAdventureAshPlant(plant, 115, 1);
            AdventurePresentationExplosion(PlantKind.CherryBomb, plant.Column + .5f, plant.Row);
            RemoveAdventurePlantAndStack(plant);
        }

        /// <summary>Starts the PC Jalapeno body on anim_explode with its original independent fuse.</summary>
        /// <param name="plant">The newly placed or transformed Jalapeno.</param>
        private void InitializeAdventureJalapeno(Plant plant)
        {
            if (plant.JalapenoPresentation != null)
            {
                return;
            }

            // PC 46C07A..46C0A4 selects 10 + RandFloat(5); case 20 shares 46C615's special=100/once-and-hold branch.
            var state = new AdventureJalapenoState { Rate = 10f + AdventureCherryRandomFloat(5f) };
            plant.JalapenoPresentation = state;
            plant._presentationKind = PlantKind.Jalapeno;
            plant.PresentationRate = state.Rate;
            plant.ActionTimer = AdventureJalapenoState.FuseTicks * .01f;
        }

        /// <summary>Advances the PC special countdown before animating the original Jalapeno pose and shake.</summary>
        /// <param name="plant">The Jalapeno whose fixed native update is due.</param>
        /// <returns>True because the native fuse owns this plant's action timer.</returns>
        private bool AdvanceAdventureJalapeno(Plant plant)
        {
            InitializeAdventureJalapeno(plant);
            var state = plant.JalapenoPresentation!;
            if (state.Detonated)
            {
                return true;
            }

            if (state.Countdown > 0)
            {
                state.Countdown--;
            }

            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 0)
            {
                DetonateAdventureJalapeno(plant);
            }

            // PC Animate 473200 explicitly includes seed 20; both RandFloat(2)-1 draws also occur on detonation.
            state.ShakeX = AdventureCherryRandomFloat(2f) - 1f;
            state.ShakeY = AdventureCherryRandomFloat(2f) - 1f;
            state.AnimationTicks++;
            state.NormalizedTime = Math.Min(.999999f, state.NormalizedTime + .01f * state.Rate / AdventureJalapenoState.ExplodeFrameCount);
            return true;
        }

        /// <summary>Runs the existing row burn exactly once when the independent native Jalapeno fuse reaches zero.</summary>
        /// <param name="plant">The Jalapeno consumed by its original row special.</param>
        private void DetonateAdventureJalapeno(Plant plant)
        {
            InitializeAdventureJalapeno(plant);
            var state = plant.JalapenoPresentation!;
            if (state.Detonated)
            {
                return;
            }

            state.Detonated = true;
            state.Countdown = 0;
            plant.ActionTimer = 0f;
            BurnAdventureRow(plant.Row);
            for (var i = Adventure._ice.Count - 1; i >= 0; i--)
            {
                if (Adventure._ice[i].Row == plant.Row)
                {
                    // PC DoSpecial 474B10 retains this row's ice for twenty final updates.
                    Adventure._ice[i].RemainingTicks = 20;
                }
            }

            for (var i = Adventure._balls.Count - 1; i >= 0; i--)
            {
                if (Adventure._balls[i].Row == plant.Row && Adventure._balls[i].IsIce)
                {
                    Adventure._balls.RemoveAt(i);
                }
            }
            if (Adventure.BossHeadVulnerable)
            {
                DamageAdventureBoss(1800f);
            }

            AdventurePresentationExplosion(PlantKind.Jalapeno, plant.Column + .5f, plant.Row);
            RequestAdventureZombieFoley(65);
            RemoveAdventurePlantAndStack(plant);
            Raise(GameEvent.JalapenoExplosion);
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
                case PlantKind.IceShroom:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.IceTrap;
                    break;
                case PlantKind.PotatoMine:
                    effect = global::PlantsVsZombies.Adventure.AdventureExplosionEffect.PotatoMine;
                    break;
                case PlantKind.Jalapeno:
                    Presentation.ScreenShake.Start(3, -4);
                    AdventurePresentationJalapenoFire(row);
                    return;
                default:
                    return; // Cob already creates BlastMark + PopcornSplash, not Powie.
            }

            if (kind != PlantKind.IceShroom)
            {
                // PC Plant::DoSpecial 0x474D16 and 0x474DE6 restart this same board shake.
                Presentation.ScreenShake.Start(3, -4);
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
            if (!Zombies.Contains(zombie))
            {
                return;
            }

            if (!burned)
            {
                HitAdventureZombie(zombie, damage, true, flags);
                return;
            }

            // PC 42954D uses BurnBody (54E920); Potato takes the separate damage(1800,0x12)
            // branch. BurnBody compares BODY health, so thick armor cannot absorb an ash kill.
            if (damage == 1800f && zombie.Kind != ZombieKind.Boss && zombie.Health < 1800f)
            {
                zombie.PresentationDeath = global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned;
                zombie.ArmorHealth = 0;
                zombie.ShieldHealth = 0;
                ApplyAdventureZombieDamage(zombie, damage, true, 0x12);
            }
            else
            {
                ApplyAdventureZombieDamage(zombie, damage, true, 0x12);
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
            Presentation._nativeShootSuppressions++;
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

            var wasCold = z.ChillRemaining > 0f || z.FrozenRemaining > 0f;
            var chilled = ApplyAdventureChill(z, true);
            if (!global::PlantsVsZombies.Adventure.AdventureIceRules.CanFreeze((int)z.Kind, AdventureZombieFeedbackPhase(z), chilled))
            {
                return false;
            }

            var pool = z.PresentationInPool ?? (Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            var offset = pool ? 0 : _random.Next(wasCold ? 101 : 201);
            z.FrozenRemaining = global::PlantsVsZombies.Adventure.AdventureIceRules.FreezeTicks(pool, wasCold, offset) * .01f;
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
                z.FrozenRemaining = 0f;
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
                x = shot.PresentationLobNativeX;
                y = shot.PresentationLaunchY + shot.PresentationLobHeight;
                return;
            }

            var slope = Adventure.Level.HasRoof ? Math.Max(0f, 5f - shot.X) * 20f - 10f : 0f;
            x -= 14f;
            y = AdventureTorchwoodRules.IsPea(shot.SourceKind) && shot.PresentationLaunchY != 0f
                ? shot.PresentationLaunchY + shot.PresentationLaneOffset - 14f
                : 80f + shot.FlightRow * pitch + slope + 16f;
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
                var px = shot.PresentationLobNativeX - shot.PresentationLobNativeVelocityX;
                var py = shot.PresentationLaunchY + shot.PresentationLobFloorOffset
                    + shot.PresentationLobContactHeight - shot.PresentationLobVelocity;
                x = px + 30f;
                y = py + 30f;
            }
            else
            {
                var zx = zombie == null ? 0 : (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
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
            x = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(grid, AdventureDancerRules.Scale(z));
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
            if (plant.Kind == PlantKind.Blover)
            {
                // PC PlantInit 46C23A starts anim_blow immediately at twenty fps and delays its special action fifty updates.
                plant.BloverPresentation = new AdventureBloverState();
                plant.ActionTimer = .5f;
                return;
            }

            if (plant.Kind == PlantKind.CherryBomb)
            {
                InitializeAdventureCherryBomb(plant);
                return;
            }

            if (plant.Kind == PlantKind.Jalapeno)
            {
                InitializeAdventureJalapeno(plant);
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
            if (plant.Kind == PlantKind.DoomShroom)
            {
                AdvanceAdventureDoomShroom(plant);
                return true;
            }

            if (plant.Kind == PlantKind.TangleKelp)
            {
                AdvanceAdventureTangleKelp(plant);
                return true;
            }

            if (plant.Kind == PlantKind.Chomper)
            {
                return AdvanceAdventureChomper(plant);
            }

            if (plant.Kind == PlantKind.CherryBomb)
            {
                return AdvanceAdventureCherryBomb(plant);
            }

            if (plant.Kind == PlantKind.Jalapeno)
            {
                return AdvanceAdventureJalapeno(plant);
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

            if (plant.Kind == PlantKind.Cactus && plant.ActionTimer < 100000f)
            {
                AdvanceAdventureCactus(plant);
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
            var kernel = plant.Kind == PlantKind.KernelPult;
            var scaredy = plant.Kind == PlantKind.ScaredyShroom;
            var state = plant.RequiredTiming;
            var pendingProjectiles = 0;
            if (scaredy)
            {
                // UpdateShooting precedes UpdateScaredyShroom, which in turn precedes UpdateShooter.
                var shooting = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                    new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, false, false, false,
                        global::PlantsVsZombies.Timing.ShootingTrack.Body, plant.PresentationShotAge * 35f >= 15f, 0));
                pendingProjectiles = shooting.ProjectilesCreated;
                AdvanceAdventureScaredyShroom(plant, state.ShootingCountdown > 0 && shooting.State.ShootingCountdown == 0,
                    shooting.State.ShootingCountdown > 0);
                state = shooting.State;
                if (plant.ScaredyShroomPresentation!.NativePhase != 1)
                {
                    state = global::PlantsVsZombies.Timing.PlantTiming.FromRaw(state.Kind, 150, state.ShootingCountdown);
                }
            }

            var lobber = plant.Kind == PlantKind.CabbagePult || kernel || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon;
            var targetZombie = fume || shortPuff || sunflower ? null : lobber ? FindAdventureLobberTarget(plant.Row, plant.Column)
                : FindAdventureTarget(plant.Row, plant.ModeFacesLeft ? -1f : plant.Column + .4f,
                    plant.ModeFacesLeft ? plant.Column + .5f : 20f, false);
            var target = fume ? FindAdventureFumeTarget(plant) != null : shortPuff ? FindAdventurePuffTarget(plant) != null
                : !sunflower && (targetZombie != null || !plant.ModeFacesLeft && CanShootAdventureBoss(plant))
                    && (!scaredy || plant.ScaredyShroomPresentation!.NativePhase == 1);
            // PC 0x46D240 starts the body animation and its species-specific shooting counter together.
            var track = shortPuff || fume || scaredy || plant.Kind == PlantKind.CabbagePult || kernel
                || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon
                ? global::PlantsVsZombies.Timing.ShootingTrack.Body : global::PlantsVsZombies.Timing.ShootingTrack.Head;
            var resetReady = !lobber || plant.PresentationShotAge < 0f || plant.PresentationLobberIdleAge >= 0f;
            var input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, !scaredy, true, true, target, track, resetReady, 0);
            var bound = global::PlantsVsZombies.Timing.PlantTiming.RequiredRandomBound(state, input);
            var draw = bound == 0 ? 0 : _random.Next(bound);
            var butter = kernel && state.Countdown <= 1 && target && _random.Next(4) == 0;
            var result = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                new global::PlantsVsZombies.Timing.PlantTimingInput(true, !scaredy, true, true, target, track, resetReady, draw, butter));
            plant.Timing = result.State;
            plant.ActionTimer = result.State.Countdown * .01f;
            if (result.AnimationStarted)
            {
                if (lobber)
                {
                    plant.PresentationLobberIdleAge = -1f;
                }

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
                RequestAdventureZombieFoley(4);
            }

            for (var i = 0; i < result.ProjectilesCreated + pendingProjectiles; i++)
            {
                if (fume)
                {
                    FireAdventureFume(plant);
                    continue;
                }

                if (scaredy || plant.Kind == PlantKind.CabbagePult || kernel || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon)
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

        /// <summary>Runs PC UpdateShooting, UpdateCactus and UpdateShooter in their original order.</summary>
        /// <param name="plant">The live cactus whose body and native weapon state are being advanced.</param>
        private void AdvanceAdventureCactus(Plant plant)
        {
            if (!plant.Timing.HasValue)
            {
                InitializeAdventureNativePlantTiming(plant);
            }

            var pose = plant.CactusPresentation;
            if (pose == null)
            {
                pose = new AdventureCactusState(plant.PresentationRate);
                plant.CactusPresentation = pose;
            }

            pose.AdvanceAnimation();
            var state = plant.RequiredTiming;
            var shooting = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                new global::PlantsVsZombies.Timing.PlantTimingInput(true, true, false, false, false,
                    global::PlantsVsZombies.Timing.ShootingTrack.Body, pose.AnimationFinished, 0));
            if (shooting.ProjectilesCreated > 0)
            {
                FireAdventureNativeTimedProjectile(plant);
                plant.PresentationShotAge = pose.SampleSeconds(0f);
            }

            if (state.ShootingCountdown > 0 && shooting.State.ShootingCountdown == 0)
            {
                pose.FinishShooting();
            }

            var airTarget = false;
            var groundTarget = false;
            foreach (var zombie in Zombies)
            {
                if (AdventureCactusState.CanTarget(plant, zombie, true))
                {
                    airTarget = true;
                }
                else if (AdventureCactusState.CanTarget(plant, zombie, false))
                {
                    groundTarget = true;
                }
            }

            var previousPhase = pose.NativePhase;
            pose.Update(airTarget, shooting.State.ShootingCountdown > 0);
            state = shooting.State;
            if (previousPhase != 31 && pose.NativePhase == 31)
            {
                RequestAdventureZombieFoley(63);
            }
            else if (previousPhase == 31 && pose.NativePhase == 32)
            {
                // PC 0x46F625 resets the launch counter before this tick's UpdateShooter.
                state = global::PlantsVsZombies.Timing.PlantTiming.FromRaw(state.Kind, 1, state.ShootingCountdown);
            }

            var high = pose.NativePhase == 32;
            var target = high ? airTarget : pose.NativePhase == 30 && (groundTarget || CanShootAdventureBoss(plant));
            var input = new global::PlantsVsZombies.Timing.PlantTimingInput(true, false, true, false, target,
                global::PlantsVsZombies.Timing.ShootingTrack.Body, true, 0, cactusHigh: high);
            var bound = global::PlantsVsZombies.Timing.PlantTiming.RequiredRandomBound(state, input);
            var result = global::PlantsVsZombies.Timing.PlantTiming.Tick(state,
                new global::PlantsVsZombies.Timing.PlantTimingInput(true, false, true, false, target,
                    global::PlantsVsZombies.Timing.ShootingTrack.Body, true, bound == 0 ? 0 : _random.Next(bound), cactusHigh: high));
            plant.Timing = result.State;
            plant.ActionTimer = result.State.Countdown * .01f;
            plant.PresentationBiteNativeState = pose.NativePhase;
            if (result.AnimationStarted)
            {
                pose.BeginShooting();
                plant.PresentationShotAge = 0f;
                plant._presentationPrefiring = true;
                plant.ShootAge = 0f;
            }
        }

        /// <summary>Updates the original Scaredy-shroom state using the native circle and actual zombie rectangles.</summary>
        /// <param name="plant">The awake mushroom whose body is being advanced.</param>
        /// <param name="finishedShooting">Whether UpdateShooting just restored the original idle animation.</param>
        /// <param name="shooting">Whether the current original shooting animation still owns the body.</param>
        private void AdvanceAdventureScaredyShroom(Plant plant, bool finishedShooting, bool shooting)
        {
            var state = plant.ScaredyShroomPresentation;
            if (state == null)
            {
                state = new AdventureScaredyShroomState();
                plant.ScaredyShroomPresentation = state;
            }

            var nearby = false;
            if (!shooting)
            {
                var plantY = 80 + plant.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85 : 100)
                    + (Adventure.Level.HasRoof ? (int)(Math.Max(0f, 5f - plant.Column) * 20f - 10f) : 0);
                foreach (var zombie in Zombies)
                {
                    var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                    var body = AdventureZombieTargetingRules.BodyRectangle(zombie, (int)AdventureBiteNativeY(zombie, nativeX));
                    var bodyY = body.Y + zombie.PresentationBiteBodyY;
                    var clip = zombie.PresentationBiteClipHeight > -100 ? zombie.PresentationBiteClipHeight : 0;
                    if (zombie.GraveRiseTicks > 0)
                    {
                        var poolRise = zombie.PoolState != null && zombie.PresentationInPool == true;
                        var altitude = (int)(poolRise ? AdventureZombiePoolRules.BodyAltitude(zombie) : AdventureZombieRiseRules.Altitude(zombie.GraveRiseTicks));
                        bodyY -= altitude;
                        clip = -altitude + (poolRise ? 0 : Math.Min(zombie.GraveRiseTicks, 40));
                    }
                    else if (zombie.PresentationNativePhase == 50 && zombie.DancerState != null)
                    {
                        var altitude = AdventureDancerRules.RiseAltitude(zombie.DancerState.PhaseTicks);
                        bodyY -= altitude;
                        clip = -altitude;
                    }

                    if (AdventureScaredyShroomRules.CanScare(plant, zombie, body, bodyY, body.Height - clip, plantY))
                    {
                        nearby = true;
                        break;
                    }
                }
            }

            var randomUnit = state.NeedsRandom(nearby, shooting) ? (float)_random.NextDouble() : 0f;
            state.Tick(nearby, shooting, randomUnit, finishedShooting);
            plant.PresentationBiteNativeState = state.NativePhase;
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
            else if (z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)))
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
                NativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z)),
                NativeY = AdventureBiteNativeY(z, global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z))),
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
                if (plant.Row == row && plant.Column <= column && column < plant.Column + plant.ColumnSpan && plant.Layer == layer && plant.BungeeState != 2
                    && !(plant.Kind == PlantKind.Squash && plant.PresentationBiteNotOnGround))
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
            PlaceAdventurePlantWithEffects(kind, row, column);
            planted = true;
        }

        /// <summary>Places a player-selected plant and records the native planting effects at the successful action boundary.</summary>
        /// <param name="kind">The planted seed before any later Imitater transformation.</param>
        /// <param name="row">The selected lawn row.</param>
        /// <param name="column">The selected lawn column.</param>
        private void PlaceAdventurePlantWithEffects(PlantKind kind, int row, int column)
        {
            var previous = Presentation._nativePlantingEffects;
            Presentation._nativePlantingEffects = true;
            try
            {
                PlaceAdventurePlant(kind, row, column);
            }
            finally
            {
                Presentation._nativePlantingEffects = previous;
            }

            RecordAdventurePlantingEffect(kind, row, column);
        }

        /// <summary>Creates the original PC planting burst only after a successful player placement.</summary>
        /// <param name="kind">The planted seed type, before any later Imitater transformation.</param>
        /// <param name="row">The planted board row.</param>
        /// <param name="column">The planted board column.</param>
        private void RecordAdventurePlantingEffect(PlantKind kind, int row, int column)
        {
            // PC Board::DoPlantingEffects 0x418AB0 chooses water by the square, even on an existing lily pad.
            RequestAdventureZombieFoley(kind != PlantKind.CoffeeBean && Adventure.Level.IsWaterRow(row) ? 52 : 7);
            // PC Board::DoPlantingEffects 0x418AB0: coffee is flying and produces no soil or water burst.
            if (kind == PlantKind.CoffeeBean)
            {
                return;
            }

            var level = Adventure.Level;
            var y = 80f + row * (level.HasPool || level.HasRoof ? 85f : 100f) + 74f;
            if (level.HasRoof)
            {
                y += Math.Max(0f, 5f - column) * 20f - 10f;
            }

            y += kind == PlantKind.LilyPad ? 15f : kind == PlantKind.FlowerPot ? 30f : 0f;
            var id = ++Presentation._effectId;
            Presentation.PlantingEffects.Add(new global::PlantsVsZombies.Adventure.AdventurePlantingEffect
            {
                Id = id,
                Row = row,
                Seed = id * 397 ^ row * 9 + column,
                X = 40f + column * 80f + 41f,
                Y = y,
                Pool = level.IsWaterRow(row)
            });
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
                PlaceAdventurePlantWithEffects(p.Kind, row, column);
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

            if (a._ice.Exists(v => v.Row == row && v.CoversColumn(column)))
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
                return main != null && main.BungeeState == 0 && main.Asleep && main.WakeTimer <= 0 && AdventurePlantAt(row, column, PlantLayer.Effect) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.NeedsSleepingMushroom;
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

            var support = AdventurePlantAt(row, column, PlantLayer.Support);
            if (l.IsWaterRow(row) && kind == PlantKind.PotatoMine)
            {
                return support != null && support.Kind == PlantKind.LilyPad
                    ? AdventurePlantingFailure.PotatoMineOnLilyPad : AdventurePlantingFailure.NeedsLilyPad;
            }

            if (l.IsWaterRow(row) && !aquatic && !(kind == PlantKind.Pumpkin && main != null && main.Kind == PlantKind.Cattail) && (support == null || support.BungeeState != 0 || support.Kind != PlantKind.LilyPad))
            {
                return AdventurePlantingFailure.NeedsLilyPad;
            }

            if (kind == PlantKind.LilyPad && main != null)
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (kind == PlantKind.FlowerPot && (l.IsWaterRow(row) || main != null || AdventurePlantAt(row, column, PlantLayer.Armor) != null))
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (l.HasRoof && kind != PlantKind.FlowerPot && (support == null || support.BungeeState != 0 || support.Kind != PlantKind.FlowerPot))
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
            if (kind != PlantKind.FlowerPot && AdventurePlantAt(row, column, PlantLayer.Support) is Plant pot
                && pot.Kind == PlantKind.FlowerPot)
            {
                // PC 46CA8B..46CAD6 stops the existing pot at its current animation frame.
                pot.PresentationFlowerPotPaused = true;
            }

            if (kind == PlantKind.GraveBuster)
            {
                // Plant::PlantInit plays FOLEY_GRAVEBUSTERCHOMP when the buster lands in play.
                Presentation.GraveBusterChompSoundSerial++;
            }

            if (kind == PlantKind.Jalapeno)
            {
                RequestAdventureZombieFoley(57);
            }
            else if (kind == PlantKind.Plantern)
            {
                RequestAdventureZombieFoley(-3);
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
                || plant == null || plant.Kind == PlantKind.GraveBuster || !Plants.Contains(plant)
                || plant.Kind == PlantKind.Squash && plant.PresentationBiteNotOnGround)
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
            if (p.Kind != PlantKind.CoffeeBean)
            {
                RemoveAdventureLadder(p.Row, p.Column);
            }

            Plants.Remove(p);
            if (p.Kind == PlantKind.TangleKelp && p.TangleKelpPresentation != null)
            {
                var target = FindAdventureKelpZombie(p.TangleKelpPresentation.TargetZombieId);
                if (target != null)
                {
                    // PC Plant::Die also finishes a grabbed target when the plant is shoveled.
                    target.TangleKelpGrab = null;
                    target.PresentationDeath = AdventureZombieDeathPresentation.Eaten;
                    KillAdventureZombie(target);
                }
            }

            if (p.Layer == PlantLayer.Support)
            {
                Plants.RemoveAll(x => x.Row == p.Row && x.Column <= p.Column && p.Column < x.Column + x.ColumnSpan);
            }

            if (AdventureAirAttackPlantAt(p.Row, p.Column) is Plant pot && pot.Kind == PlantKind.FlowerPot
                && pot.PresentationFlowerPotPaused)
            {
                // PC Plant::Die restarts only an uncovered pot, preserving its current animation phase.
                pot.PresentationFlowerPotPaused = false;
                pot.PresentationRate = 10f + (float)_random.NextDouble() * 5f;
                pot._presentationKind = PlantKind.FlowerPot;
            }

            if (Adventure.SelectedCobCannonId.HasValue && FindPlantById(Adventure.SelectedCobCannonId.Value) == null)
            {
                Adventure.SelectedCobCannonId = null;
            }
        }

        /// <summary>Applies the original storm state before the level introduction.</summary>
        internal void BeginAdventureStormIntro()
        {
            if (IsAdventure && Adventure.Level.Mode == AdventureMode.Storm)
            {
                Adventure.Storm.BeginIntro();
            }
        }

        /// <summary>Applies the original storm countdown when the introduction finishes.</summary>
        internal void BeginAdventureStormPlaying()
        {
            if (IsAdventure && Adventure.Level.Mode == AdventureMode.Storm)
            {
                Adventure.Storm.BeginPlaying();
            }
        }

        /// <summary>Advances the original storm challenge after the board objects for one native update.</summary>
        /// <param name="intro">Whether the native scene is still LevelIntro.</param>
        /// <param name="paused">Whether to freeze rain and board counters while allowing the flash to settle.</param>
        internal void AdvanceAdventureStormTick(bool intro = false, bool paused = false)
        {
            if (!IsAdventure || Adventure.Level.Mode != AdventureMode.Storm)
            {
                return;
            }

            var yeti = false;
            foreach (var zombie in Zombies)
            {
                if (zombie.Kind == ZombieKind.Yeti)
                {
                    yeti = true;
                    break;
                }
            }

            if (State == MatchState.Lost && !Adventure.Storm.Lost)
            {
                Adventure.Storm.BeginLoss();
            }

            if (Adventure.Storm.Advance(_random, !intro && State != MatchState.Lost, paused, State == MatchState.Lost, yeti))
            {
                RequestAdventureZombieFoley(17);
            }
        }

        /// <summary>Continues native storm updates while the regular simulation is suspended.</summary>
        /// <param name="delta">The elapsed presentation time in seconds.</param>
        /// <param name="paused">Whether the board is paused rather than showing the loss sequence.</param>
        /// <param name="intro">Whether the suspended board is in its introduction.</param>
        internal void AdvanceAdventureStormPresentation(float delta, bool paused, bool intro = false)
        {
            if (!IsAdventure || Adventure.Level.Mode != AdventureMode.Storm || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            var storm = Adventure.Storm;
            if (State == MatchState.Lost && !storm.Lost)
            {
                storm.BeginLoss();
            }

            storm.PresentationRemainder += delta;
            while (storm.PresentationRemainder + .0000001 >= SimulationStep)
            {
                if (State == MatchState.Lost && !paused)
                {
                    var foley = storm.AdvanceLossClock();
                    if (foley >= 0)
                    {
                        RequestAdventureZombieFoley(foley, presentationTick: (int)Math.Round(Elapsed * 100f) + storm.LostTicks);
                    }
                }

                AdvanceAdventureStormTick(intro, paused || storm.LossDialogReady);
                storm.PresentationRemainder -= SimulationStep;
                if (Math.Abs(storm.PresentationRemainder) < .0000001)
                {
                    storm.PresentationRemainder = 0d;
                }
            }
        }

        /// <summary>Gets whether the original stage uses fog, excluding Stormy Night and Invisighoul.</summary>
        internal bool AdventureHasFog => IsAdventure && Adventure.Level.Terrain == AdventureTerrain.Fog
            && Adventure.Level.Mode != AdventureMode.Storm && (ModeSession == null || ModeSession.Definition.Id != 21);

        /// <summary>Gets the original stage-dependent first fog column.</summary>
        internal int AdventureFogLeftColumn => AdventureFogState.LeftColumn(Adventure.Level.Number,
            ModeSession != null, ModeSession != null && ModeSession.Definition.Id == 41);

        /// <summary>Starts the original off-lawn fog state before the street preview and seed chooser.</summary>
        internal void BeginAdventureFogIntro()
        {
            if (AdventureHasFog)
            {
                Adventure.FogBlownRemaining = 2f;
                Adventure.Fog.BeginIntro(AdventureFogLeftColumn);
            }
        }

        /// <summary>Advances one original Board fog update and its playing or opening countdown.</summary>
        /// <param name="intro">Whether the level opening owns the countdown instead of gameplay.</param>
        /// <param name="rollIn">Whether the opening has reached TimeFogRollIn.</param>
        internal void AdvanceAdventureFog(bool intro = false, bool rollIn = false)
        {
            if (!AdventureHasFog)
            {
                return;
            }

            Adventure.Fog.Advance(AdventureFogLeftColumn, Adventure.FogBlownRemaining, intro, Plants);
            if (!intro || rollIn)
            {
                if (Adventure.FogBlownRemaining < 100f)
                {
                    var ticks = (int)Math.Round(Adventure.FogBlownRemaining * 100f);
                    Adventure.FogBlownRemaining = Math.Max(0, ticks - 1) * .01f;
                }
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

            if (!AdventureHasFog || Adventure.FogBlownRemaining > 0f || column < AdventureFogLeftColumn)
            {
                return true;
            }

            foreach (var plant in Plants)
            {
                var dx = Math.Abs(plant.Column - column);
                var dy = Math.Abs(plant.Row - row);
                if (plant.Health > 0f && (plant.Kind == PlantKind.Plantern && dx <= 3 && dy <= 2 && dx + dy != 5
                    || plant.Kind == PlantKind.Torchwood && dx + dy <= 1))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Advances PC UpdateTangleKelp (0x46E430), keeping the authored grab attached until death.</summary>
        /// <param name="plant">The water plant whose native grab owns its attack timing.</param>
        private void AdvanceAdventureTangleKelp(Plant plant)
        {
            var state = plant.TangleKelpPresentation;
            if (state == null)
            {
                var x = plant.Column + .5f;
                var target = FindAdventureTarget(plant.Row, x - .5f, x + .7f, false, true, PlantKind.TangleKelp);
                if (target == null)
                {
                    return;
                }

                RequestAdventureZombieFoley(68);
                SplashAdventureTangleKelpTarget(target);
                state = new AdventureTangleKelpState
                {
                    TargetZombieId = target.Id,
                    OffsetX = target.Kind == ZombieKind.Snorkel ? -43 : AdventureZombieFeedbackPhase(target) == 53 ? -20 : -13,
                    OffsetY = target.Kind == ZombieKind.Snorkel ? 55 : AdventureZombieFeedbackPhase(target) == 53 ? 37 : 15
                };
                plant.TangleKelpPresentation = state;
                target.TangleKelpGrab = state;
                plant.PresentationBiteNativeState = 34;
                return;
            }

            state.Countdown--;
            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 50)
            {
                var target = FindAdventureKelpZombie(state.TargetZombieId);
                if (target != null)
                {
                    target.PresentationNativeHeight = 3;
                    StopAdventureNativeEating(target);
                    SplashAdventureTangleKelpTarget(target);
                }
            }

            if (state.Countdown == 20)
            {
                var x = 40f + plant.Column * 80f;
                var y = 80f + plant.Row * 85f;
                Presentation.ZombieSplashes.Add(new AdventureZombieSplash
                {
                    Id = ++Presentation._effectId, Row = plant.Row,
                    X = x - 23f, Y = y + 7f, ParticleX = x + 31f, ParticleY = y + 64f,
                    ScaleX = 1.3f, ScaleY = 1.3f
                });
                RequestAdventureZombieFoley(53);
            }

            if (state.Countdown <= 0)
            {
                RemoveAdventurePlantAndStack(plant);
            }
        }

        /// <summary>Finds the surviving actor referenced by the original kelp grab.</summary>
        /// <param name="id">The stable zombie identifier retained by the grabbing plant.</param>
        /// <returns>The living board actor, or null after an earlier death or removal.</returns>
        private Zombie? FindAdventureKelpZombie(int id)
        {
            foreach (var zombie in Zombies)
            {
                if (zombie.Id == id)
                {
                    return zombie;
                }
            }

            return null;
        }

        /// <summary>Emits the original target's PoolSplash(false), including its swimming-snorkel offsets.</summary>
        /// <param name="target">The grabbed zombie whose native position anchors the splash.</param>
        private void SplashAdventureTangleKelpTarget(Zombie target)
        {
            var scale = AdventureDancerRules.Scale(target);
            var swimming = AdventureZombieFeedbackPhase(target) == 59;
            var altitude = target.PoolState?.Altitude ?? target.PresentationAltitude;
            AddAdventureZombieSplash(target, (swimming ? -14f : 23f) * scale,
                (swimming ? 70f : 78f) * (altitude == 0f ? scale : 1f), .8f, .8f, 52,
                target.Kind == ZombieKind.DolphinRider ? 65f : (float?)null);
        }

        /// <summary>Runs the original Blover body clock, single wind effect, and two-hundred-update special lifetime.</summary>
        /// <param name="plant">The planted Blover whose original state is advanced.</param>
        private void AdvanceAdventureBlover(Plant plant)
        {
            var state = plant.BloverPresentation ??= new AdventureBloverState();
            if (state.Advance())
            {
                RequestAdventureZombieFoley(-2);
                BlowAdventureFliersAway();
                // PC BlowAwayFliers 474AAE includes the original twenty-second fog return interval.
                Adventure.FogBlownRemaining = 40f;
            }

            plant.ActionTimer = state.SpecialTicks * .01f;
            if (state.Expired)
            {
                RemoveAdventurePlantAndStack(plant);
            }
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
                    if (!Plants.Contains(p) || p.BungeeState != 0)
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
                        var wakeTicks = Math.Max(0, (int)Math.Round(p.WakeTimer * 100f) - 1);
                        p.WakeTimer = wakeTicks * .01f;
                        if (wakeTicks == 100)
                        {
                            // This combined timer retains Coffee's 100-tick crumble followed by the mushroom's 100-tick wake.
                            RequestAdventureZombieFoley(69);
                        }
                        if (wakeTicks == 60)
                        {
                            // PC Plant::Update plays Wakeup at the sixty-tick milestone.
                            RequestAdventureZombieFoley(83);
                        }
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

                    if (p.Kind == PlantKind.MagnetShroom)
                    {
                        AdvanceAdventureMagnetShroom(p);
                        continue;
                    }

                    if (p.Kind == PlantKind.Torchwood)
                    {
                        UpdateAdventureTorchwood(p);
                        continue;
                    }

                    if (p.Kind == PlantKind.Squash)
                    {
                        AdvanceAdventureSquash(p);
                        continue;
                    }

                    if (p.Kind == PlantKind.Blover)
                    {
                        AdvanceAdventureBlover(p);
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
                            RequestAdventureZombieFoley(4);
                            Suns.Add(CreateAdventurePresentationSun(p, p.Kind == PlantKind.SunShroom && p.PresentationAwakeAge < 120f ? 15 : 25));
                            p.ActionTimer = 24f;
                            break;
                        case PlantKind.Marigold:
                            RequestAdventureZombieFoley(4);
                            p.ActionCount++;
                            SpawnAdventureCoin(_random.Next(100) < 10 ? AdventureCoinKind.Gold : AdventureCoinKind.Silver, p.Column + 0.5f, p.Row);
                            p.ActionTimer = 24f;
                            break;
                        case PlantKind.CherryBomb:
                            DetonateAdventureCherryBomb(p);
                            break;
                        case PlantKind.Jalapeno:
                            DetonateAdventureJalapeno(p);
                            break;
                        case PlantKind.IceShroom:
                            RequestAdventureZombieFoley(18);
                            NotifyAdventureIceTrap();
                            AdventurePresentationExplosion(PlantKind.IceShroom, x, p.Row);
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
                        case PlantKind.PotatoMine:
                            AdvanceAdventurePotatoMine(p);
                            break;
                        case PlantKind.Spikeweed:
                            foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
                            {
                                if (!z.Airborne && !z.Hypnotized && z.Row == p.Row && Math.Abs(z.X - x) < 0.6f)
                                {
                                    if (z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult)
                                    {
                                        if (z.Kind == ZombieKind.Zomboni)
                                        {
                                            HitAdventureZombie(z, 20f, true, 32U);
                                        }
                                        else
                                        {
                                            KillAdventureZombie(z);
                                        }

                                        RemoveAdventurePlantAndStack(p);
                                        RequestAdventureZombieFoley(1);
                                        break;
                                    }

                                    HitAdventureZombie(z, 20f, true);
                                    RequestAdventureZombieFoley(1);
                                }
                            }

                            p.ActionTimer = 1f;
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
                                        p.PresentationButter = false;
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

        /// <summary>Gets the original body rectangle used by the magnet's circular reach and nearest-target priority.</summary>
        /// <param name="zombie">The potential magnetic target.</param>
        /// <returns>The body rectangle after native altitude and clipping.</returns>
        private NativeRect AdventureMagnetBodyRectangle(Zombie zombie)
        {
            var scale = AdventureDancerRules.Scale(zombie);
            var x = AdventureZombieNativeRules.NativePositionX(zombie.X, scale);
            var altitude = zombie.PoolState != null ? AdventureZombiePoolRules.BodyAltitude(zombie) : zombie.PresentationAltitude;
            var y = (int)AdventureBiteNativeY(zombie, x) + (int)-altitude;
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie, y);
            var clip = zombie.PoolState != null ? zombie.PresentationBiteClipHeight : (int)AdventureZombieMotionRules.ClipHeight(zombie, scale);
            return new NativeRect(body.X, body.Y, body.Width, Math.Max(0, body.Height - (clip > -100 ? clip : 0)));
        }

        /// <summary>Runs PC UpdateMagnetShroom 470170, including the original independent body and cooldown clocks.</summary>
        /// <param name="plant">The awake Magnet-shroom updated at one hundred hertz.</param>
        private void AdvanceAdventureMagnetShroom(Plant plant)
        {
            var state = plant.MagnetPresentation ??= new AdventureMagnetState
            {
                AnimationRate = plant.PresentationRate > 0f ? plant.PresentationRate : 12f
            };
            state.AnimationSeconds += .01f;
            if (state.Item != null)
            {
                state.Item.AgeTicks++;
            }

            if (state.CooldownTicks > 0)
            {
                state.CooldownTicks--;
            }

            plant.ActionTimer = state.CooldownTicks * .01f;
            plant.PresentationBiteNativeState = state.NativePhase;
            if (state.NativePhase == 27)
            {
                if (state.CooldownTicks == 0)
                {
                    state.NativePhase = 1;
                    plant.PresentationBiteNativeState = 1;
                    state.AnimationSeconds = 0f;
                    state.AnimationRate = AdventureZombieIsIZombie ? 0f : 10f + (float)_random.NextDouble() * 5f;
                    state.BlendTicks = 30;
                    state.Item = null;
                }

                return;
            }

            if (state.NativePhase == 26)
            {
                if (state.AnimationSeconds * 12f / AdventureMagnetRules.SuckingFrames >= 1f)
                {
                    state.NativePhase = 27;
                    plant.PresentationBiteNativeState = 27;
                    state.AnimationSeconds = 0f;
                    state.AnimationRate = AdventureZombieIsIZombie ? 0f : 2f;
                    state.BlendTicks = 20;
                }

                return;
            }

            var plantX = 40 + plant.Column * 80;
            var plantY = AdventurePlantEatingRectangle(plant).Y;
            Zombie? target = null;
            var nearest = float.MaxValue;
            foreach (var zombie in Zombies)
            {
                var body = AdventureMagnetBodyRectangle(zombie);
                if (!AdventureMagnetRules.CanTarget(zombie, body, plant.Row)
                    || !AdventureMagnetRules.InRange(plantX, plantY, body, zombie.Eating))
                {
                    continue;
                }

                var distance = AdventureMagnetRules.TargetDistance(plantX, plantY, body, Math.Abs(zombie.Row - plant.Row));
                if (distance < nearest)
                {
                    nearest = distance;
                    target = zombie;
                }
            }

            if (target != null)
            {
                AttractAdventureMagnetMetal(plant, target);
                return;
            }

            Plant? ladder = null;
            var ladderScore = float.MaxValue;
            foreach (var other in Plants)
            {
                if (!other.HasLadder || other.Health <= 0f)
                {
                    continue;
                }

                var dx = Math.Abs(other.Column - plant.Column);
                var dy = Math.Abs(other.Row - plant.Row);
                var distance = Math.Max(dx, dy);
                var score = distance + dy * .05f;
                if (distance <= 2 && score < ladderScore)
                {
                    ladder = other;
                    ladderScore = score;
                }
            }

            if (ladder != null)
            {
                foreach (var other in Plants)
                {
                    if (other.Row == ladder.Row && other.Column == ladder.Column)
                    {
                        other.HasLadder = false;
                    }
                }

                BeginAdventureMagnetAttraction(plant, new AdventureMagnetItem
                {
                    NativeKind = 20,
                    X = 40 + ladder.Column * 80 + 40,
                    Y = AdventurePlantEatingRectangle(ladder).Y,
                    PositionResolved = true,
                    DestinationX = 10f + RandomAdventureMagnetOffset(),
                    DestinationY = RandomAdventureMagnetOffset()
                });
            }
        }

        /// <summary>Gets the original uniform destination jitter for a magnetic item.</summary>
        /// <returns>A native pixel offset in minus ten through plus ten.</returns>
        private float RandomAdventureMagnetOffset()
        {
            return -10f + (float)_random.NextDouble() * 20f;
        }

        /// <summary>Starts PC 46F9C0's original shooting clip, metal item and single attraction Foley.</summary>
        /// <param name="plant">The ready Magnet-shroom.</param>
        /// <param name="item">The original removed metal item.</param>
        private void BeginAdventureMagnetAttraction(Plant plant, AdventureMagnetItem item)
        {
            var state = plant.MagnetPresentation!;
            state.NativePhase = 26;
            state.CooldownTicks = AdventureMagnetRules.CooldownTicks;
            state.AnimationSeconds = 0f;
            state.AnimationRate = 12f;
            state.BlendTicks = 20;
            state.Item = item;
            plant.PresentationBiteNativeState = 26;
            plant.ActionTimer = 15f;
            RequestAdventureZombieFoley(42);
        }

        /// <summary>Captures the original GetTrackPosition input after its species-specific magnetic disarming operation.</summary>
        /// <param name="zombie">The disarmed actor whose original artwork supplies the source point.</param>
        /// <param name="track">The original retained metal track.</param>
        /// <returns>A source-pose snapshot which cannot follow later target movement or death.</returns>
        private AdventureZombiePartDrop CaptureAdventureMagnetSource(Zombie zombie, string track)
        {
            var animation = AdventurePresentationZombieAnimation(zombie);
            var rate = AdventurePresentationZombieRate(zombie);
            var age = zombie.PresentationAnimationAge;
            if (zombie.Kind == ZombieKind.Digger && zombie.PresentationNativePhase == 34 && zombie.DiggerState != null)
            {
                rate = 12f;
                age = zombie.DiggerState.PausedDigSeconds;
            }

            return new AdventureZombiePartDrop
            {
                NativeKind = (int)zombie.Kind,
                NativePhase = zombie.PresentationNativePhase,
                NativeAltitude = zombie.PoolState != null ? AdventureZombiePoolRules.BodyAltitude(zombie) : zombie.PresentationAltitude,
                Row = zombie.Row,
                X = zombie.X,
                Scale = AdventureDancerRules.Scale(zombie),
                Asset = AdventureZombiePresentationAsset(zombie),
                Animation = animation,
                AnimationAge = age,
                Rate = rate,
                Anchor = track,
                Backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized
                    || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie))
            };
        }

        /// <summary>Applies PC 46F9C0's per-item removal without damaging unrelated equipment or creating duplicate debris.</summary>
        /// <param name="plant">The Magnet-shroom taking the selected nearest metal.</param>
        /// <param name="zombie">The original eligible source zombie.</param>
        private void AttractAdventureMagnetMetal(Plant plant, Zombie zombie)
        {
            EnsureAdventureZombieEquipment(zombie);
            var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var nativeY = AdventureBiteNativeY(zombie, nativeX);
            var item = new AdventureMagnetItem { X = nativeX, Y = nativeY };
            var helm = AdventureZombieEquipmentRules.HelmType(zombie);
            if (zombie.ArmorHealth > 0f && (helm == 2 || helm == 3))
            {
                var stage = AdventureZombieEquipmentRules.DamageIndex(zombie.ArmorHealth, (int)zombie.PresentationMaxArmor);
                zombie.ArmorHealth = 0f;
                item.NativeKind = (helm == 2 ? 1 : 4) + stage;
                if (helm == 2)
                {
                    item.Source = CaptureAdventureMagnetSource(zombie, "anim_bucket");
                    item.AnchorImage = "IMAGE_REANIM_ZOMBIE_BUCKET1";
                    item.DestinationX = 25f;
                }
                else
                {
                    item.X = nativeX + 37f;
                    item.Y = nativeY - 60f;
                    item.PositionResolved = true;
                    item.DestinationX = 20f;
                }

                item.DestinationY = 20f;
            }
            else if (zombie.ShieldHealth > 0f && (zombie.Kind == ZombieKind.ScreenDoor || zombie.Kind == ZombieKind.Ladder))
            {
                var stage = AdventureZombieEquipmentRules.DamageIndex(zombie.ShieldHealth, (int)zombie.PresentationMaxShield);
                zombie.ShieldHealth = 0f;
                zombie.PresentationNativePhase = 0;
                if (!zombie.Eating)
                {
                    // PC 54EEA0/46F9C0 uses StartWalkAnim(0) after either held shield is removed.
                    zombie.PresentationAnimation = string.Empty;
                    zombie.PresentationBodyBlendTicks = 0;
                    PickAdventureZombieNativeSpeed(zombie, 0);
                }
                else
                {
                    // A latched meal keeps its native velocity until the later StopEating walk reset.
                    zombie._presentationSpeedPhase = 0;
                    zombie._presentationSpeedKind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
                }

                if (zombie.Kind == ZombieKind.ScreenDoor)
                {
                    item.NativeKind = 7 + stage;
                    item.Source = CaptureAdventureMagnetSource(zombie, "anim_screendoor");
                    item.AnchorImage = "IMAGE_REANIM_ZOMBIE_SCREENDOOR1";
                }
                else
                {
                    zombie.UsedAbility = true;
                    item.NativeKind = 14 + stage;
                    item.X = nativeX + 31f;
                    item.Y = nativeY + 20f;
                    item.PositionResolved = true;
                    item.AnchorImage = "IMAGE_REANIM_ZOMBIE_LADDER_5";
                }

                item.DestinationX = 30f;
            }
            else if (zombie.Kind == ZombieKind.Pogo)
            {
                BreakAdventurePogo(zombie, 16U);
                item.NativeKind = zombie.PresentationArmLost ? 12 : 10;
                item.Source = CaptureAdventureMagnetSource(zombie, "Zombie_pogo_stick");
                item.AnchorImage = "IMAGE_REANIM_ZOMBIE_POGO_STICKDAMAGE1";
                item.SourceOffsetX = 40f;
                item.SourceOffsetY = 84f;
                item.DestinationX = 44f;
                item.DestinationY = 10f;
            }
            else if (zombie.Kind == ZombieKind.JackInTheBox && zombie.PresentationNativePhase == 15)
            {
                zombie.UsedAbility = true;
                zombie.AbilityTimer = float.MaxValue;
                PickAdventureZombieNativeSpeed(zombie, 15);
                zombie.PresentationNativePhase = 0;
                zombie.JackInTheBoxTicks = 0;
                item.NativeKind = 13;
                item.Source = CaptureAdventureMagnetSource(zombie, "Zombie_jackbox_box");
                item.AnchorImage = "IMAGE_REANIM_ZOMBIE_JACKBOX_BOX";
                item.DestinationX = 20f;
                item.DestinationY = 15f;
            }
            else if (zombie.Kind == ZombieKind.Digger)
            {
                LoseAdventureDiggerAxe(zombie);
                item.NativeKind = 21;
                item.Source = CaptureAdventureMagnetSource(zombie, "Zombie_digger_pickaxe");
                item.AnchorImage = "IMAGE_REANIM_ZOMBIE_DIGGER_PICKAXE";
                item.DestinationX = 45f;
                item.DestinationY = 15f;
            }

            item.DestinationX += RandomAdventureMagnetOffset();
            item.DestinationY += RandomAdventureMagnetOffset();
            BeginAdventureMagnetAttraction(plant, item);
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
            // PC UpdateMotion 47CCB0 advances normal and backward peas by 3.33 pixels per native update.
            var peaVelocity = AdventureTorchwoodRules.IsPea(p.Kind) ? 3.33f * 100f / 80f : 4.5f;
            shot.VelocityX = back ? -peaVelocity : peaVelocity;
            shot.Damage = damage;
            shot.Backward = back;
            // Preserve the existing muzzle while retaining its fixed original trajectory after launch.
            shot.PresentationLaunchY = 80f + row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f)
                + (Adventure.Level.HasRoof ? Math.Max(0f, 5f - shot.X) * 20f - 10f : 0f) + 30f;
            if (p.Kind == PlantKind.Threepeater)
            {
                // PC Plant::Fire 0x4752E0 and Projectile::UpdateMotion 0x47CCB0.
                // The logical collision row is fixed at launch; only the artwork cuts into that lane.
                shot.X = p.Column + 59f / 80f;
                shot.VelocityX = 3.33f * 100f / 80f;
                shot.PresentationLaunchY = 80f + p.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f)
                    + (Adventure.Level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f) + 24f;
                shot.PresentationLaneVelocity = Math.Sign(row - p.Row) * 3f;
            }
            else if (p.Kind == PlantKind.SplitPea)
            {
                // PC Plant::Fire 0x4752E0 uses the same moving body anchor for both heads.
                AdventureSplitPeaRules.HeadOffset(p.Age, p.PresentationRate > 0f ? p.PresentationRate : 12f, out var headX, out var headY);
                shot.X = p.Column + (headX + (back ? -64f : 24f) + 14f) / 80f;
                shot.StartX = shot.X;
                shot.PresentationLaunchY = 80f + p.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f)
                    + (Adventure.Level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f)
                    + headY - 33f + 14f - (HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column) ? 5f : 0f);
            }

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
            if (p.Kind != PlantKind.SnowPea && p.Kind != PlantKind.WinterMelon)
            {
                if (p.Kind != PlantKind.Starfruit)
                {
                    RequestAdventureZombieFoley(3);
                }

                if (AdventurePuffShroomRules.IsShortRange(p.Kind) || p.Kind == PlantKind.ScaredyShroom)
                {
                    RequestAdventureZombieFoley(29);
                }

                Presentation._nativeShootSuppressions++;
            }

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
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, plant.Kind)
                    || !CanTakeAdventureAshDamage(zombie, 1U))
                {
                    continue;
                }

                var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                var bodyY = (int)AdventureBiteNativeY(zombie, nativeX);
                var body = AdventureZombieTargetingRules.BodyRectangle(zombie, bodyY);
                var stationary = AdventureStarfruitRules.TargetNotWalking(zombie)
                    || AdventureDancerRules.IsDancer(zombie) && AdventureDancerRules.GroupAffected(zombie, Zombies, false);
                var velocity = stationary ? 0f : zombie.PresentationNativeVelocity * (zombie.ChillRemaining > 0f ? .5f : 1f);
                if (zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie)))
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
                    RequestAdventureZombieFoley(1);
                }
            }

            plant._presentationPrefiring = false;
            RequestAdventureZombieFoley(30);
            Presentation._nativeShootSuppressions++;
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
            shot.HitsAir = plant.Kind == PlantKind.Cactus && plant.CactusPresentation?.NativePhase == 32;
            if (plant.Kind == PlantKind.Cactus)
            {
                // PC Plant::Fire 0x4752E0 creates the 30x16 spike at +(93,-50) or +(70,23).
                // The renderer stores its centre, whereas native projectile coordinates name its top-left corner.
                shot.X = plant.Column + (shot.HitsAir ? 108f : 85f) / 80f;
                shot.StartX = shot.X;
                shot.PresentationPreviousX = shot.X;
                shot.VelocityX = 3.33f * 100f / 80f;
                var plantY = 80f + plant.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f)
                    + (Adventure.Level.HasRoof ? Math.Max(0f, 5f - plant.Column) * 20f - 10f : 0f);
                shot.PresentationLaunchY = plantY + (shot.HitsAir ? -42f : 31f)
                    - (HasPlantKindAt(PlantKind.FlowerPot, plant.Row, plant.Column) ? 5f : 0f);
            }
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
                // PC 47301D switches the basket back to kernels before creating the butter projectile.
                plant.PresentationButter = false;
                if (shot.Butter)
                {
                    shot.Damage = 40f;
                }

                plant.PresentationShotAge = .29f;
            }

            if (lob)
            {
                // PC Plant::Fire resolves its target at creation, before any other plant can kill it.
                AdvanceAdventureProjectilePresentation(shot, 0f);
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
            RequestAdventureZombieFoley(3);
            var rowPitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            var plantY = 80f + plant.Row * rowPitch
                + (Adventure.Level.HasRoof ? Math.Max(0, 5 - plant.Column) * 20f - 10f : 0f);
            for (var index = 0; index < 5; index++)
            {
                var star = FireAdventurePea(plant, plant.Row, false, 20f);
                star.X = plant.Column + 25f / 80f;
                star.StartX = star.X;
                star.PresentationPreviousX = star.X;
                star.VelocityX = AdventureStarfruitRules.VelocityX(index) * 100f / 80f;
                star.VelocityY = AdventureStarfruitRules.VelocityY(index) * 100f / rowPitch;
                star.Backward = star.VelocityX < 0f;
                star.PiercesShield = star.Backward;
                star.PresentationLaunchY = AdventureStarfruitRules.LaunchY((int)plantY);
                star.PresentationStarShadowY = plantY + 82f
                    - (Adventure.Level.HasRoof && AdventureStarfruitRules.LaunchX(plant.Column) < 480 ? 12f : 0f);
                // PC ProjectileInitialize 47BB30: independent original signed angular velocity per star.
                star.PresentationRotation = 0f;
                star.PresentationSpin = .05f + (float)_random.NextDouble() * .05f;
                if (_random.Next(2) == 0)
                {
                    star.PresentationSpin = -star.PresentationSpin;
                }

                star.PresentationMotionInitialized = true;
                star.Range = float.MaxValue;
            }

            plant._presentationPrefiring = false;
        }

        /// <summary>Finds the original leftmost lobber target using Plant::GetPlantAttackRect and range mask thirteen.</summary>
        /// <param name="row">The lobber's planted row.</param>
        /// <param name="column">The lobber's planted column, independent of its weapon muzzle.</param>
        /// <returns>The nearest admitted body overlapping the original eight-hundred-pixel attack interval.</returns>
        private Zombie? FindAdventureLobberTarget(int row, int column)
        {
            var left = 40 + column * 80 + 60;
            Zombie? best = null;
            var bestLeft = int.MaxValue;
            foreach (var zombie in Zombies)
            {
                if (zombie.Kind != ZombieKind.Boss && zombie.Row != row || !CanTakeAdventureAshDamage(zombie, 13U))
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                if (Math.Min(left + 800, body.X + body.Width) >= Math.Max(left, body.X) && body.X < bestLeft)
                {
                    best = zombie;
                    bestLeft = body.X;
                }
            }

            return best;
        }

        /// <summary>Checks PC 47DFF0's lobber rectangle at the preceding integer projectile position.</summary>
        /// <param name="shot">The cabbage, kernel, butter or melon being integrated.</param>
        /// <param name="previousX">The original integer projectile position before this native motion update.</param>
        /// <param name="zombie">The admitted collision candidate.</param>
        /// <returns>Whether the original horizontal body intervals touch or overlap.</returns>
        private static bool AdventureLobberCollides(Pea shot, int previousX, Zombie zombie)
        {
            var left = previousX + (shot.Splash ? 20 : 0);
            var width = shot.Splash ? 60 : 40;
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            return Math.Min(left + width, body.X + body.Width) >= Math.Max(left, body.X);
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
                if (!z.Hypnotized && (sourceKind != PlantKind.TangleKelp || z.PresentationInPool == true)
                    && AdventureZombieTargetingRules.CanBeAutoTargeted(z, sourceKind) && z.DropRemaining <= 0f && z.Row == row && z.X >= min && z.X <= max && (!z.Airborne || air) && (!z.Submerged || submerged))
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

        /// <summary>Gets the native plant or aimed blast center's vertical board coordinate.</summary>
        /// <param name="x">The horizontal grid coordinate of the blast center.</param>
        /// <param name="row">The vertical grid coordinate of the blast center.</param>
        /// <returns>The original board pixel coordinate, including roof slope.</returns>
        private int AdventureBlastCenterY(float x, float row)
        {
            return (int)(120f + row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f)
                + (Adventure.Level.HasRoof ? Math.Max(0f, 5.5f - x) * 20f - 10f : 0f));
        }

        /// <summary>Applies a Cherry Bomb or Doom-shroom's original radius and row restriction.</summary>
        /// <param name="plant">The plant whose native eighty-pixel bounds define the center.</param>
        /// <param name="radius">The original circular radius in board pixels.</param>
        /// <param name="rows">The maximum original row distance.</param>
        private void BlastAdventureAshPlant(Plant plant, int radius, int rows)
        {
            // PC Plant::DoSpecial 0x474B10: Cherry 115/1, Doom 250/3; target mask 0x7F.
            var x = plant.Column + .5f;
            BlastAdventureRadius(80 + plant.Column * 80, AdventureBlastCenterY(x, plant.Row), plant.Row, radius, rows, 0x7F);
        }

        /// <summary>Checks the native EffectedByDamage gate used by plant and projectile target masks.</summary>
        /// <param name="zombie">The candidate zombie.</param>
        /// <param name="targetFlags">The original damage range mask, separate from damage behavior flags.</param>
        /// <returns>Whether the original mask admits this live zombie's current movement phase.</returns>
        private bool CanTakeAdventureAshDamage(Zombie zombie, uint targetFlags)
        {
            var phase = AdventureZombieFeedbackPhase(zombie);
            if (zombie.Hypnotized != ((targetFlags & 0x80) != 0) || zombie.Health <= 0f || (targetFlags & 0x20) == 0 && phase >= 1 && phase <= 3
                || zombie.PresentationNativeHeight == 9
                || zombie.Kind == ZombieKind.Bungee && phase != 6 && phase != 7
                || zombie.Kind == ZombieKind.Boss && !Adventure.BossHeadVulnerable
                || zombie.BobsledState?.Position > 0)
            {
                return false;
            }

            // DropRemaining also pauses pool ambushes. Only a linked bungee passenger maps to native height nine.
            if (zombie.DropRemaining > 0f)
            {
                foreach (var carrier in Zombies)
                {
                    if (carrier.Kind == ZombieKind.Bungee && carrier.BungeeCarrier && carrier.LinkedZombieId == zombie.Id)
                    {
                        return false;
                    }
                }
            }

            // PC 0x54D810 returns directly for these transient phases before the screen-edge gate.
            if (phase == 12 || phase == 71 || phase == 33 || phase == 34 || phase == 35 || phase == 52
                || phase == 54 || phase == 58 || phase == 74 || phase == 14 || phase == 19 || phase == 50)
            {
                return (targetFlags & 0x10) != 0;
            }

            if (zombie.Kind != ZombieKind.Bobsled && AdventureZombieTargetingRules.BodyRectangle(zombie).X > 800)
            {
                return false;
            }

            var submerged = zombie.Kind == ZombieKind.Snorkel && zombie.Submerged && !zombie.Eating;
            var underground = zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0;
            return (targetFlags & 4) != 0 && submerged || (targetFlags & 0x40) != 0 && underground
                || (targetFlags & 2) != 0 && zombie.Airborne
                || (targetFlags & 1) != 0 && !zombie.Airborne && !submerged && !underground;
        }

        /// <summary>Applies the original circle-to-body-rectangle intersection used by radial ash attacks.</summary>
        /// <param name="x">The blast center's native horizontal pixel coordinate.</param>
        /// <param name="y">The blast center's native vertical pixel coordinate.</param>
        /// <param name="row">The blast's original board row.</param>
        /// <param name="radius">The circular radius in board pixels.</param>
        /// <param name="rows">The maximum original row distance.</param>
        /// <param name="targetFlags">The original damage range mask.</param>
        private void BlastAdventureRadius(int x, int y, int row, int radius, int rows, uint targetFlags)
        {
            // PC Board::KillAllZombiesInRadius 0x4294C0 and GetCircleRectOverlap 0x428460.
            foreach (var zombie in new PooledSnapshot<Zombie>(Zombies))
            {
                if (!CanTakeAdventureAshDamage(zombie, targetFlags)
                    || zombie.Kind != ZombieKind.Boss && Math.Abs(zombie.Row - row) > rows)
                {
                    continue;
                }

                var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                var body = AdventureZombieTargetingRules.BodyRectangle(zombie, (int)AdventureBiteNativeY(zombie, nativeX));
                var top = body.Y + zombie.PresentationBiteBodyY;
                var clip = zombie.PresentationBiteClipHeight > -100 ? zombie.PresentationBiteClipHeight : 0;
                if (zombie.GraveRiseTicks > 0)
                {
                    var poolRise = zombie.PoolState != null && zombie.PresentationInPool == true;
                    var altitude = (int)(poolRise ? AdventureZombiePoolRules.BodyAltitude(zombie) : AdventureZombieRiseRules.Altitude(zombie.GraveRiseTicks));
                    top -= altitude;
                    clip = -altitude + (poolRise ? 0 : Math.Min(zombie.GraveRiseTicks, 40));
                }
                else if (zombie.PresentationNativePhase == 50 && zombie.DancerState != null)
                {
                    var altitude = AdventureDancerRules.RiseAltitude(zombie.DancerState.PhaseTicks);
                    top -= altitude;
                    clip = -altitude;
                }

                var height = body.Height - clip;
                var dx = Math.Max(0, Math.Max(body.X - x, x - body.X - body.Width));
                var dy = y < top ? top - y : Math.Max(0, y - top - height);
                if (dx * dx + dy * dy <= radius * radius)
                {
                    HitAdventureExplosionZombie(zombie, 1800f, true, 0x12);
                }
            }
        }

        /// <summary>Burns eligible zombies throughout the original Jalapeno row without a horizontal cutoff.</summary>
        /// <param name="row">The original board row ignited by the plant.</param>
        private void BurnAdventureRow(int row)
        {
            // PC Plant::BurnRow 0x474920 removes freeze/chill before BurnBody, preserving equipment.
            foreach (var zombie in new PooledSnapshot<Zombie>(Zombies))
            {
                if ((zombie.Row == row || zombie.Kind == ZombieKind.Boss) && CanTakeAdventureAshDamage(zombie, 0x7F))
                {
                    zombie.FrozenRemaining = 0f;
                    zombie.ChillRemaining = 0f;
                    HitAdventureExplosionZombie(zombie, 1800f, true, 0x12);
                }
            }
        }

        /// <summary>Advances PC 47CCB0's star motion and 47C130's native row and horizontal-rectangle collision.</summary>
        /// <param name="shot">The original five-direction star projectile.</param>
        /// <param name="dt">The fixed simulation interval in seconds.</param>
        /// <param name="allowDamage">Whether the current board permits combat damage.</param>
        /// <returns>Whether the star has hit a target, struck the roof or left the original board bounds.</returns>
        private bool AdvanceAdventureStarProjectile(Pea shot, float dt, bool allowDamage)
        {
            var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            var previousX = 40f + shot.X * 80f;
            shot.X += shot.VelocityX * dt;
            shot.FlightRow += shot.VelocityY * dt;
            shot.PresentationNativeAge += dt;
            var x = 40f + shot.X * 80f;
            var y = AdventureStarfruitRules.PositionY(shot, pitch);
            shot.PresentationStarShadowY += shot.VelocityY * dt * pitch;
            if (shot.VelocityY != 0f)
            {
                shot.Row = AdventureStarfruitRules.CollisionRow(x, y, Adventure.Level.HasPool, Adventure.Level.HasRoof);
            }

            if (AdventureStarfruitRules.OffBoard(x, y))
            {
                return true;
            }

            Zombie? hit = null;
            var hitX = int.MaxValue;
            if (allowDamage && shot.PresentationStarShadowY - y <= 90f)
            {
                foreach (var zombie in Zombies)
                {
                    if ((zombie.Row != shot.Row && zombie.Kind != ZombieKind.Boss)
                        || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, PlantKind.Starfruit)
                        || !CanTakeAdventureAshDamage(zombie, 1U)
                        || zombie.Kind == ZombieKind.Snorkel && zombie.PresentationNativePhase == 59
                        || zombie.Kind == ZombieKind.Digger && shot.VelocityX >= 0f
                            && (int)Math.Round(shot.PresentationNativeAge * 100f) < 25)
                    {
                        continue;
                    }

                    // UpdateMotion stores integer mX/mY after collision, so GetProjectileRect uses the previous x.
                    var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                    var overlap = Math.Min((int)previousX + 40, body.X + body.Width) - Math.Max((int)previousX, body.X);
                    var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                    if (overlap >= 0 && nativeX < hitX)
                    {
                        hit = zombie;
                        hitX = nativeX;
                    }
                }
            }

            if (hit != null)
            {
                AdventurePresentationImpact(shot, hit);
                HitAdventureZombie(hit, shot.Damage, shot.PiercesShield);
                return true;
            }

            if (shot.PresentationStarShadowY - y < 23f)
            {
                AdventurePresentationImpact(shot, null);
                return true;
            }

            // PC UpdateMotion adjusts only the shadow after this tick's collisions; stars stay on a straight line.
            if (Adventure.Level.HasRoof)
            {
                shot.PresentationStarShadowY += (Math.Max(0f, 440f - x) - Math.Max(0f, 440f - previousX)) * .25f;
            }

            return false;
        }

        /// <summary>Runs original Plant::UpdateTorchwood before projectiles advance their positions.</summary>
        /// <param name="torch">The live Torchwood checking normal and snow peas in its logical row.</param>
        private void UpdateAdventureTorchwood(Plant torch)
        {
            foreach (var shot in Peas)
            {
                if (!AdventureTorchwoodRules.Overlaps(torch, shot))
                {
                    continue;
                }

                shot.LastTorchColumn = torch.Column;
                if (shot.IsSnow)
                {
                    // PC ConvertToPea destroys the trail and leaves this same tree unable to ignite it again.
                    shot.IsSnow = false;
                    UpdateAdventureSnowTrail(shot);
                    RequestAdventureZombieFoley(3);
                }
                else
                {
                    // PC ConvertToFireball 47E0E0 retains mPos/mVel/mMotion and creates a new attachment clock.
                    shot.Fire = true;
                    shot.Damage = 40f;
                    shot.Splash = true;
                    shot.PresentationFireAge = 0f;
                    shot.PresentationFireRate = 50f + (float)_random.NextDouble() * 30f;
                    RequestAdventureZombieFoley(39);
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
                if (p.Fire)
                {
                    p.PresentationFireAge += dt;
                }

                if (p.SourceKind == PlantKind.Starfruit)
                {
                    if (AdvanceAdventureStarProjectile(p, dt, allowDamage))
                    {
                        RemovePeaAt(i);
                    }

                    continue;
                }

                if (p.Lobbed && !p.PresentationMotionInitialized)
                {
                    AdvanceAdventureProjectilePresentation(p, 0f);
                }

                UpdateAdventureHomingVelocity(p, dt);
                var before = p.X;
                var beforeNativeX = (int)p.PresentationLobNativeX;
                p.X += p.VelocityX * dt;
                p.FlightRow += p.VelocityY * dt;
                if (p.SourceKind == PlantKind.Threepeater)
                {
                    p.PresentationPreviousLaneOffset = p.PresentationLaneOffset;
                    p.PresentationLaneOffset += p.PresentationLaneVelocity * (dt * 100f);
                    p.PresentationLaneVelocity *= .97f;
                }
                AdvanceAdventureProjectilePresentation(p, dt);
                if (AdventurePuffShroomRules.IsShortRange(p.SourceKind) && AdventurePuffShroomRules.Expired(p.PresentationNativeAge))
                {
                    RemovePeaAt(i);
                    continue;
                }

                p.Row = (int)Math.Round(p.FlightRow);
                UpdateAdventureSnowTrail(p);
                Zombie? hit = null;
                foreach (Zombie z in Zombies)
                {
                    if (allowDamage && p.PresentationCanImpact && (p.Lobbed ? CanTakeAdventureAshDamage(z, 13U) : AdventureZombieTargetingRules.CanBeAutoTargeted(z, p.SourceKind)) && z.GraveRiseTicks <= 0 && z.PresentationNativePhase != 50 && z.PresentationNativePhase != 74
                        && !(z.Kind == ZombieKind.DolphinRider && (z.PresentationNativePhase == 52 || z.PresentationNativePhase == 54))
                        && !(z.Kind == ZombieKind.Digger && z.PresentationNativePhase >= 33 && z.PresentationNativePhase <= 35)
                        && (!p.Homing || z.Id == p.TargetZombieId) && !z.Hypnotized && Math.Abs(z.Row - p.FlightRow) < 0.45f && !p.HitIds.Contains(z.Id) && (!z.Airborne || p.HitsAir)
                        && (p.SourceKind != PlantKind.Cactus || z.Airborne == p.HitsAir && AdventureZombieTargetingRules.CanBeAutoTargeted(z, PlantKind.Cactus))
                        && (!z.Submerged || p.Lobbed) && (!p.Lobbed || z.PresentationNativePhase != 59
                            || p.PresentationLobContactHeight > 45f)
                        && (p.Lobbed ? AdventureLobberCollides(p, beforeNativeX, z) : z.Kind == ZombieKind.Zomboni
                        ? AdventureZomboniRules.ProjectileCollides(p, z)
                        : AdventurePuffShroomRules.IsShortRange(p.SourceKind) ? AdventurePuffShroomRules.Collides(p, z)
                        : z.X >= Math.Min(before, p.X) - 0.25f && z.X <= Math.Max(before, p.X) + 0.25f))
                    {
                        if (hit == null || (p.Lobbed ? (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z))
                            < (int)AdventureZombieNativeRules.NativePositionX(hit.X, AdventureDancerRules.Scale(hit))
                            : Math.Abs(z.X - before) < Math.Abs(hit.X - before)))
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
                    ApplyAdventureProjectileCold(p, hit);
                    HitAdventureZombie(hit, p.Damage, p.PiercesShield);
                    if (p.Splash)
                    {
                        foreach (Zombie other in new PooledSnapshot<Zombie>(Zombies))
                        {
                            if (other != hit && !other.Hypnotized && !other.Airborne
                                && !(other.Kind == ZombieKind.DolphinRider && (other.PresentationNativePhase == 52 || other.PresentationNativePhase == 54))
                                && Math.Abs(other.Row - hit.Row) <= (p.Fire ? 0 : 1) && Math.Abs(other.X - hit.X) <= (p.Fire ? 0.5f : 1f))
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

                if (!remove && p.Lobbed && p.PresentationCanImpact
                    && p.PresentationLobContactHeight > 80f)
                {
                    // PC UpdateLobMotion 47C880 impacts the floor even when no zombie survives beneath the shot.
                    AdventurePresentationImpact(p, null);
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
            if (!Zombies.Contains(z) || z.GraveRiseTicks > 0
                || z.PresentationNativePhase == 50 && (damageFlags & 0x10) == 0
                || z.ZomboniState?.FlatTires == true || z.BobsledState?.Position > 0)
            {
                return;
            }

            ApplyAdventureZombieDamage(z, damage, bypassShield, damageFlags);
        }

        /// <summary>Applies damage after the caller has admitted the zombie through its native target gate.</summary>
        /// <param name="z">The admitted live zombie.</param>
        /// <param name="damage">The damage before equipment absorption.</param>
        /// <param name="bypassShield">Whether ordinary held-shield absorption is bypassed.</param>
        /// <param name="damageFlags">The original TakeDamage behavior mask.</param>
        private void ApplyAdventureZombieDamage(Zombie z, float damage, bool bypassShield, uint damageFlags)
        {
            AdventurePresentationZombieHit(z, damage, bypassShield, damageFlags);
            if (z.Airborne)
            {
                var flyingDamage = Math.Min(z.FlyingHealth, (int)damage);
                z.FlyingHealth -= flyingDamage;
                damage -= flyingDamage;
                if (z.FlyingHealth == 0)
                {
                    LandAdventureBalloon(z, damageFlags);
                    if (!Zombies.Contains(z))
                    {
                        return;
                    }
                }
            }

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
            if (z.Kind == ZombieKind.Zomboni && (damageFlags & 32U) != 0)
            {
                BeginAdventureZomboniFlatDeath(z);
            }
            else if (z.Health <= 0)
            {
                z.Health = 0f;
                KillAdventureZombie(z, damageFlags);
            }
        }

        /// <summary>Starts PC ZamboniDeath's authored tire puncture and 280-update wheelie.</summary>
        /// <param name="zombie">The vehicle hit by the original spike damage flag.</param>
        private void BeginAdventureZomboniFlatDeath(Zombie zombie)
        {
            var state = zombie.ZomboniState ??= new AdventureZomboniState();
            state.FlatTires = true;
            state.DeathTicks = 280;
            state.Wheelie2 = _random.Next(4) == 0
                && AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie)) < 600f;
            zombie.PresentationNativePhase = 1;
            zombie.PresentationAnimation = state.Animation;
            zombie.PresentationAnimationAge = 0f;
            zombie.PresentationNativeVelocity = 0f;
            zombie.Speed = 0f;
            zombie.Eating = false;
            zombie.FrozenRemaining = 0f;
            zombie.PresentationButter = 0f;
            RequestAdventureZombieFoley(22);
            AddAdventureZomboniParticle(zombie, "ZamboniTire", 29f, 114f);
        }

        /// <summary>Creates an unchanged original vehicle particle at the PC unscaled native origin.</summary>
        /// <param name="zombie">The source vehicle whose position and row determine the particle layer.</param>
        /// <param name="particle">The original particle definition name.</param>
        /// <param name="offsetX">The unscaled horizontal offset from mPosX.</param>
        /// <param name="offsetY">The unscaled vertical offset from mPosY.</param>
        private void AddAdventureZomboniParticle(Zombie zombie, string particle, float offsetX, float offsetY)
        {
            var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            Presentation.ZomboniParticles.Add(new AdventureZomboniParticle
            {
                Id = ++Presentation._effectId, Seed = _random.Next(), Row = zombie.Row, Particle = particle,
                X = nativeX + offsetX, Y = AdventureBiteNativeY(zombie, nativeX) + offsetY
            });
        }

        /// <summary>Emits PC ZamboniDeath's original explosion once, without a generic corpse animation.</summary>
        /// <param name="zombie">The ice vehicle whose body is being removed.</param>
        private void ExplodeAdventureZomboni(Zombie zombie)
        {
            var state = zombie.ZomboniState ??= new AdventureZomboniState();
            if (state.Exploded)
            {
                return;
            }

            state.Exploded = true;
            AddAdventureZomboniParticle(zombie, state.Wheelie2 ? "ZamboniExplosion2" : "ZamboniExplosion", 80f, 60f);
            RequestAdventureZombieFoley(23);
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

            if (z.Kind == ZombieKind.Bungee)
            {
                ReleaseAdventureBungeeCargo(z);
            }

            if (z.BobsledState != null)
            {
                var leaderId = z.BobsledState.LeaderId;
                z.BobsledState = null;
                foreach (var member in new PooledSnapshot<Zombie>(Zombies))
                {
                    if (member.BobsledState?.LeaderId != leaderId)
                    {
                        continue;
                    }

                    member.BobsledState = null;
                    if (z.PresentationDeath == AdventureZombieDeathPresentation.Burned)
                    {
                        // PC BobsledBurn preserves every rider's burned pose without another loot roll.
                        member.PresentationDeath = AdventureZombieDeathPresentation.Burned;
                        member.PresentationHealthBeforeHit = member.Health;
                        member.Health = 0f;
                        Zombies.Remove(member);
                        AdventurePresentationZombieDeath(member, damageFlags);
                        Raise(GameEvent.ZombieDied);
                    }
                    else
                    {
                        member.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                        KillAdventureZombie(member, damageFlags);
                    }
                }
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

        /// <summary>Gets the original planted cell's native vertical coordinate, including the roof slope.</summary>
        /// <param name="row">The original board row.</param>
        /// <param name="column">The clamped original board column.</param>
        /// <returns>The native GridToPixelY coordinate before plant height offsets.</returns>
        private int AdventureSquashCellY(int row, int column)
        {
            return 80 + row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85 : 100)
                + (Adventure.Level.HasRoof ? (int)(Math.Max(0, 5 - column) * 20 - 10) : 0);
        }

        /// <summary>Finds the closest original contact target while preferring the initially selected live identifier.</summary>
        /// <param name="plant">The Squash whose original contact interval is tested.</param>
        /// <param name="state">The retained original target and native body origin.</param>
        /// <returns>The admitted native zombie, or null when no original target remains.</returns>
        private Zombie? FindAdventureSquashTarget(Plant plant, AdventureSquashState state)
        {
            Zombie? best = null;
            var bestGap = int.MaxValue;
            var attack = AdventureSquashRules.AttackRectangle(state.NativeX);
            foreach (var zombie in Zombies)
            {
                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                if (!CanTakeAdventureAshDamage(zombie, 13U)
                    || !AdventureSquashRules.CanTarget(plant, zombie, body, state.NativeX, AdventureZombieFeedbackPhase(zombie), Adventure.Level.Mode == AdventureMode.Vasebreaker))
                {
                    continue;
                }

                if (zombie.Id == state.TargetZombieId)
                {
                    return zombie;
                }

                var gap = -AdventureSquashRules.Overlap(attack, body);
                if (best == null || gap < bestGap)
                {
                    best = zombie;
                    bestGap = gap;
                }
            }

            return best;
        }

        /// <summary>Predicts the PC body center at the requested native lead time before subtracting half the plant width.</summary>
        /// <param name="zombie">The original target whose current body and movement state are sampled.</param>
        /// <param name="ticks">The native prediction horizon used by ZombieTargetLeadX.</param>
        /// <returns>The integer native landing coordinate of the Squash's left edge.</returns>
        private int AdventureSquashTargetX(Zombie zombie, int ticks)
        {
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            var velocity = zombie.PresentationNativeVelocity;
            if (zombie.ChillRemaining > 0f)
            {
                velocity *= .5f;
            }

            if (zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie)))
            {
                velocity = -velocity;
            }

            if (zombie.Eating || zombie.FrozenRemaining > 0f || zombie.PresentationButter > 0f
                || AdventureDancerRules.IsDancer(zombie) && !AdventureDancerRules.CanWalk(zombie, Zombies))
            {
                velocity = 0f;
            }

            return (int)(body.X + body.Width / 2 - velocity * ticks) - 40;
        }

        /// <summary>Applies PC DoSquashDamage's narrow horizontal overlap and no-body-parts damage flags.</summary>
        /// <param name="plant">The falling Squash reaching its original damage update.</param>
        /// <param name="state">The moving native body position at impact.</param>
        private void DamageAdventureSquashTargets(Plant plant, AdventureSquashState state)
        {
            var attack = AdventureSquashRules.AttackRectangle(state.NativeX);
            foreach (var zombie in new PooledSnapshot<Zombie>(Zombies))
            {
                if (zombie.Kind != ZombieKind.Boss && zombie.Row != plant.Row || !CanTakeAdventureAshDamage(zombie, 13U))
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                if (AdventureSquashRules.Overlap(attack, body) > (zombie.Kind == ZombieKind.Football ? -20 : 0))
                {
                    // PC 0x46EB68 calls TakeDamage(1800,18): shield piercing and no detached parts or ordinary corpse.
                    ApplyAdventureZombieDamage(zombie, 1800f, true, 0x12U);
                }
            }
        }

        /// <summary>Advances PC UpdateSquash's exact look, prelaunch, flight, damage, landing, and flattening sequence.</summary>
        /// <param name="plant">The live awake Squash being advanced by one native update.</param>
        private void AdvanceAdventureSquash(Plant plant)
        {
            var state = plant.SquashPresentation;
            if (state == null)
            {
                state = new AdventureSquashState
                {
                    NativeX = 40 + plant.Column * 80,
                    NativeY = AdventureSquashCellY(plant.Row, plant.Column)
                };
                plant.SquashPresentation = state;
            }

            state.AdvanceClock();
            switch (state.NativePhase)
            {
                case 0:
                    var target = FindAdventureSquashTarget(plant, state);
                    if (target != null)
                    {
                        state.TargetZombieId = target.Id;
                        state.TargetX = AdventureSquashTargetX(target, 0);
                        state.Enter(3, 80, state.TargetX < state.NativeX ? "anim_lookleft" : "anim_lookright", 24f, 10);
                        RequestAdventureZombieFoley(41);
                    }

                    break;
                case 3:
                    if (state.Countdown == 0)
                    {
                        // Local PC 0x46EEAE uses forty-five updates; the mobile reference uses thirty.
                        state.Enter(4, 45, "anim_jumpup", 24f, 20);
                    }

                    break;
                case 4:
                    if (state.Countdown == 0)
                    {
                        var redirected = FindAdventureSquashTarget(plant, state);
                        if (redirected != null)
                        {
                            state.TargetX = AdventureSquashTargetX(redirected, 30);
                        }

                        state.Enter(5, 50);
                    }

                    break;
                case 5:
                case 6:
                    var column = Math.Max(0, Math.Min(8, (int)Math.Floor((state.TargetX - 40f) / 80f)));
                    var destinationY = AdventureSquashCellY(plant.Row, column) + 8;
                    if (state.NativePhase == 5)
                    {
                        state.NativeX = AdventureSquashRules.RisingPosition(state.Countdown, 40 + plant.Column * 80, state.TargetX);
                        state.NativeY = AdventureSquashRules.RisingPosition(state.Countdown, AdventureSquashCellY(plant.Row, plant.Column), destinationY - 120);
                        if (state.Countdown == 0)
                        {
                            state.Enter(6, 10, "anim_jumpdown", 60f, 0);
                        }
                    }
                    else
                    {
                        state.NativeY = destinationY - 120 + (10 - state.Countdown) * 12;
                        if (state.Countdown == 5)
                        {
                            DamageAdventureSquashTargets(plant, state);
                        }

                        if (state.Countdown == 0)
                        {
                            var water = Adventure.Level.IsWaterRow(plant.Row);
                            Presentation._squashLandings.Add(new AdventureSquashLanding
                            {
                                Id = ++Presentation._effectId, Row = plant.Row, Water = water,
                                X = state.NativeX + (water ? -11f : 40f),
                                Y = state.NativeY + (water ? 20f : Adventure.Level.HasRoof ? 69f : 80f)
                            });
                            if (water)
                            {
                                RequestAdventureZombieFoley(1);
                                RequestAdventureZombieFoley(-4);
                                RemoveAdventurePlantAndStack(plant);
                            }
                            else
                            {
                                state.Enter(7, 100);
                                Presentation.ScreenShake.Start(1, 4);
                                RequestAdventureZombieFoley(40);
                            }
                        }
                    }

                    break;
                case 7:
                    if (state.Countdown == 0)
                    {
                        RemoveAdventurePlantAndStack(plant);
                    }

                    break;
            }

            plant.PresentationBiteNativeState = state.NativePhase;
            plant.PresentationBiteNotOnGround = state.NativePhase >= 5;
        }

        /// <summary>Starts and advances the original awake Doom-shroom special action once per native tick.</summary>
        /// <param name="plant">The awake Doom-shroom whose fuse and original exploding body are updated.</param>
        private void AdvanceAdventureDoomShroom(Plant plant)
        {
            var state = plant.DoomShroomPresentation;
            if (state == null)
            {
                state = new AdventureDoomShroomState();
                plant.DoomShroomPresentation = state;
                plant.PresentationRate = AdventureDoomShroomState.Rate;
                RequestAdventureZombieFoley(57);
            }

            if (state.Countdown > 0)
            {
                state.Countdown--;
            }

            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 0)
            {
                BlastAdventureAshPlant(plant, 250, 3);
                AdventurePresentationExplosion(PlantKind.DoomShroom, plant.Column + .5f, plant.Row);
                RemoveAdventurePlantAndStack(plant);
                Adventure._craters.Add(new AdventureCellTimer { Row = plant.Row, Column = plant.Column, Remaining = 180f });
                Raise(GameEvent.DoomExplosion);
            }

            // PC Reanimation::Update samples each configured track independently on both axes.
            for (var head = 0; head < state.HeadShakeX.Length; head++)
            {
                var amount = head == 0 ? 1f : 2f;
                state.HeadShakeX[head] = AdventureExplosionRandomFloat(amount * 2f) - amount;
                state.HeadShakeY[head] = AdventureExplosionRandomFloat(amount * 2f) - amount;
            }

            state.AnimationTicks++;
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
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, PlantKind.Chomper) || zombie.Hypnotized || zombie.Row != plant.Row || zombie.Airborne || zombie.Submerged || zombie.DropRemaining > 0 || zombie.Kind == ZombieKind.Bungee && zombie.TargetColumn == plant.Column || phase == 20 || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0)
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
                    || zombie.Kind == ZombieKind.Boss || zombie.PresentationNativeHeight == 9
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
                var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
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
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(z) || z.Hypnotized || z.Airborne || z.Kind == ZombieKind.Boss || z.Row != p.Row || z.DropRemaining > 0 || z.PresentationNativeHeight == 9 || z.Kind == ZombieKind.Digger && z.AbilityPhase == 0)
                {
                    continue;
                }

                var body = AdventureZombieTargetingRules.BodyRectangle(z);
                var overlap = Math.Min(right, body.X + body.Width) - Math.Max(left, body.X);
                var phase = AdventureZombieFeedbackPhase(z);
                var triggerLeft = left + (z.Kind == ZombieKind.PoleVaulter ? 40 : 0);
                var triggerOverlap = Math.Min(right, body.X + body.Width) - Math.Max(triggerLeft, body.X);
                if (AdventureZombieTargetingRules.CanBeAutoTargeted(z, PlantKind.PotatoMine)
                    && (z.Kind != ZombieKind.Bungee || z.TargetColumn == p.Column)
                    && phase != 11 && phase != 12 && !(z.Kind == ZombieKind.Pogo && !z.UsedAbility) && triggerOverlap >= (z.Eating ? -30 : 0))
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
            if (Presentation.VaseScreenFlashTicks >= 0 && Presentation.VaseScreenFlashTicks < 50)
            {
                Presentation.VaseScreenFlashTicks++;
            }

            // PC Board::Update consumes one Rand(2) only while the decremented counter is nonzero.
            var shake = Presentation.ScreenShake;
            shake.Tick(shake.TicksRemaining > 1 && _random.Next(2) == 0);
            for (var i = Presentation._plantSoundCues.Count - 1; i >= 0; i--)
            {
                var cue = Presentation._plantSoundCues[i];
                cue.Ticks--;
                if (cue.Ticks == 0)
                {
                    Presentation._plantSoundCues.RemoveAt(i);
                    RequestAdventureZombieFoley(cue.Foley);
                }
                else
                {
                    Presentation._plantSoundCues[i] = cue;
                }
            }

            for (var i = Presentation._squashLandings.Count - 1; i >= 0; i--)
            {
                var landing = Presentation._squashLandings[i];
                landing.Ticks++;
                if (landing.Ticks >= (landing.Water ? 78 : 30))
                {
                    Presentation._squashLandings.RemoveAt(i);
                }
                else
                {
                    Presentation._squashLandings[i] = landing;
                }
            }

            for (var i = Presentation.PlantingEffects.Count - 1; i >= 0; i--)
            {
                var effect = Presentation.PlantingEffects[i];
                effect.Age += dt;
                if (effect.Age + .000001f >= (effect.Pool ? .4f : .3f))
                {
                    Presentation.PlantingEffects.RemoveAt(i);
                }
                else
                {
                    Presentation.PlantingEffects[i] = effect;
                }
            }

            for (var i = Presentation.FootstepEffects.Count - 1; i >= 0; i--)
            {
                var effect = Presentation.FootstepEffects[i];
                effect.Age += dt;
                if (effect.Age >= .2f)
                {
                    Presentation.FootstepEffects.RemoveAt(i);
                }
                else
                {
                    Presentation.FootstepEffects[i] = effect;
                }
            }

            for (var i = Presentation.ZombieSplashes.Count - 1; i >= 0; i--)
            {
                var effect = Presentation.ZombieSplashes[i];
                effect.Age += dt;
                if (effect.Age >= 2f)
                {
                    Presentation.ZombieSplashes.RemoveAt(i);
                }
            }

            for (var i = Presentation.ZombieRiseEffects.Count - 1; i >= 0; i--)
            {
                var effect = Presentation.ZombieRiseEffects[i];
                effect.Age += dt;
                if (effect.Age >= (effect.Digger ? 5f : 2f))
                {
                    Presentation.ZombieRiseEffects.RemoveAt(i);
                }
            }

            for (var i = Presentation.ZomboniParticles.Count - 1; i >= 0; i--)
            {
                var effect = Presentation.ZomboniParticles[i];
                effect.Age += dt;
                if (effect.Age >= effect.Duration)
                {
                    Presentation.ZomboniParticles.RemoveAt(i);
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
                if (corpse.RequiredZombie.UseLadderColumn >= 0 && corpse.RequiredZombie.PresentationNativeHeight == 7)
                {
                    AdvanceAdventureLadderFall(corpse.RequiredZombie);
                }
                else if (corpse.RequiredZombie.Kind == ZombieKind.DolphinRider && corpse.RequiredZombie.PresentationNativeHeight == 7)
                {
                    // PC UpdateDeath 54F4A0 continues the original falling update after a Tall-nut collision.
                    corpse.RequiredZombie.PresentationAltitude = Math.Max(0f, corpse.RequiredZombie.PresentationAltitude - 1f);
                    if (corpse.RequiredZombie.PresentationAltitude == 0f)
                    {
                        corpse.RequiredZombie.PresentationNativeHeight = 0;
                    }
                }

                var pool = corpse.RequiredZombie.PoolState;
                if (pool != null && !Zombies.Contains(corpse.RequiredZombie))
                {
                    if (pool.SeaweedAge >= 0f)
                    {
                        pool.SeaweedAge += dt;
                    }

                    if (pool.SplashAge >= 0f)
                    {
                        pool.SplashAge += dt;
                    }
                }

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

            var nuts = Adventure.BowlingNuts;
            for (var nutIndex = 0; nutIndex < nuts.Count; nutIndex++)
            {
                var nut = nuts[nutIndex];
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

                if (zombie.DiggerState != null)
                {
                    if (zombie.DiggerState.SurpriseAge >= 0f)
                    {
                        zombie.DiggerState.SurpriseAge += dt;
                    }
                }

                AdventurePresentationZombieAnimation(zombie);
                zombie.PresentationFlash = Math.Max(0, zombie.PresentationFlash - dt);
                zombie.PresentationArmorFlash = Math.Max(0, zombie.PresentationArmorFlash - dt);
                var butterTicks = Math.Max(0, (int)Math.Round(zombie.PresentationButter * 100f) - 1);
                zombie.PresentationButter = butterTicks * .01f;
                AdvanceAdventureZombieClock(zombie, dt);
            }

            foreach (Plant plant in Plants)
            {
                if (plant.BungeeState != 0)
                {
                    plant.Age += dt * (plant.BungeeState == 2 ? .1f / Math.Max(1f, plant.PresentationRate) : 1f);
                }

                if (plant.PresentationUmbrellaAge >= 0f)
                {
                    var umbrellaTicks = (int)Math.Round(plant.PresentationUmbrellaAge * 100f) + 1;
                    plant.PresentationUmbrellaAge = umbrellaTicks * .01f;
                    if (umbrellaTicks >= 60)
                    {
                        plant.PresentationUmbrellaAge = -1f;
                    }
                }

                if (plant._presentationKind != plant.Kind)
                {
                    plant._presentationKind = plant.Kind;
                    var minimum = global::PlantsVsZombies.Adventure.AdventurePresentationRules.AttachedHead(plant.Kind) ? 15f : plant.Kind == PlantKind.CherryBomb || plant.Kind == PlantKind.Imitater ? 25f : 10f;
                    plant.PresentationRate = minimum + (float)_random.NextDouble() * 5f;
                }

                if (plant.Kind == PlantKind.FlowerPot)
                {
                    var potRate = plant.BungeeState == 2 ? .1f : plant.PresentationFlowerPotPaused ? 0f : plant.PresentationRate;
                    plant.PresentationBodyFrames += dt * potRate;
                }

                if (plant.Asleep)
                {
                    continue;
                }

                AdvanceAdventureDefensivePlantPresentation(plant, dt);
                var previousAwakeAge = plant.PresentationAwakeAge;
                plant.PresentationAwakeAge += dt;
                if (plant.Kind == PlantKind.SunShroom && previousAwakeAge < 120f && plant.PresentationAwakeAge >= 120f)
                {
                    RequestAdventureZombieFoley(63);
                }
                if (plant.PresentationShotAge >= 0)
                {
                    plant.PresentationShotAge += dt;
                    var shootingFrames = AdventurePresentationRules.LobberShootingFrames(plant.Kind);
                    if (shootingFrames > 0)
                    {
                        if (plant.PresentationLobberIdleAge >= 0f)
                        {
                            if (!AdventureZombieIsIZombie)
                            {
                                plant.PresentationLobberIdleAge += dt;
                            }
                        }
                        else if (plant.PresentationShotAge * 35f + .0001f >= shootingFrames)
                        {
                            // PC 472C70 -> 4768A0 starts idle at time zero and the source definition's twelve FPS.
                            plant.PresentationLobberIdleAge = 0f;
                            plant.PresentationRate = AdventureZombieIsIZombie ? 0f : 12f;
                        }
                    }
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

            if (plant.Kind != PlantKind.Repeater || !plant.Timing.HasValue)
            {
                // PC UpdateShooting can fire the first pea before UpdateShooter restarts the
                // Repeater head on the same tick. Its native timing already owns that new clock.
                plant.PresentationShotAge = age;
                var head = plant.Kind == PlantKind.Threepeater ? Math.Max(0, Math.Min(2, 1 - (shot.Row - plant.Row))) : plant.Kind == PlantKind.SplitPea && shot.Backward ? 1 : 0;
                plant.PresentationHeadAge[head] = age;
            }
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

        /// <summary>Applies PC 54E470's independent four-hundred-update butter immobilization without ice or chill.</summary>
        /// <param name="zombie">The actual projectile victim whose original butter admission is checked.</param>
        /// <returns>Whether the original butter state was applied.</returns>
        internal bool ApplyAdventureButter(Zombie zombie)
        {
            var phase = AdventureZombieFeedbackPhase(zombie);
            var sled = zombie.PresentationHasSled ?? (zombie.Kind == ZombieKind.Bobsled && !zombie.UsedAbility);
            var canChill = AdventureIceRules.CanChill((int)zombie.Kind, phase, zombie.Health <= 0f, zombie.Hypnotized, sled);
            if (zombie.Headless || !AdventureIceRules.CanFreeze((int)zombie.Kind, phase, canChill)
                || zombie.Kind == ZombieKind.Zomboni || zombie.Kind == ZombieKind.Boss
                || zombie.TangleKelpGrab != null || zombie.PresentationNativeHeight == 3 || sled || zombie.Airborne)
            {
                return false;
            }

            zombie.PresentationButter = 4f;
            if (zombie.Kind == ZombieKind.Pogo)
            {
                zombie.PresentationAltitude = zombie.PogoState?.OnHighGround == true ? 30f : 0f;
                zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
            }

            return true;
        }

        /// <summary>Performs adventure presentation impact.</summary>
        /// <param name = "shot">The shot.</param>
        /// <param name = "zombie">The zombie.</param>
        private void AdventurePresentationImpact(Pea shot, Zombie? zombie)
        {
            RequestAdventureProjectileImpactSound(shot, zombie);
            if (shot.Butter && zombie != null)
            {
                ApplyAdventureButter(zombie);
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
            if (shot.Lobbed && zombie == null && !shot.IsSnow)
            {
                // PC 47D410: melon bursts use the preceding flight point; cabbage/butter use mPosY + 12.
                effect.NativeIcePoint = true;
                effect.NativeParticleX = shot.PresentationLobNativeX + 12f;
                effect.NativeParticleY = shot.PresentationLaunchY + shot.PresentationLobFloorOffset + 12f;
                if (shot.Splash)
                {
                    effect.NativeParticleX = shot.PresentationLobNativeX - shot.PresentationLobNativeVelocityX + 30f;
                    effect.NativeParticleY = shot.PresentationLaunchY + shot.PresentationLobFloorOffset
                        + shot.PresentationLobContactHeight - shot.PresentationLobVelocity + 30f;
                }
            }

            if (shot.SourceKind == PlantKind.Starfruit)
            {
                // PC DoImpact 47D410 puts StarSplat at projectile top-left +(12,12).
                effect.NativeIcePoint = true;
                effect.NativeParticleX = 40f + shot.X * 80f + 12f;
                effect.NativeParticleY = AdventureStarfruitRules.PositionY(shot, Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f) + 12f;
            }

            Presentation.Impacts.Add(effect);
        }

        /// <summary>Performs adventure presentation zombie death.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name="damageFlags">The original lethal hit flags passed to Zombie::DieWithLoot.</param>
        private void AdventurePresentationZombieDeath(Zombie zombie, uint damageFlags = 0)
        {
            if (zombie.Kind == ZombieKind.Zomboni
                && (zombie.PresentationDeath == AdventureZombieDeathPresentation.Normal
                    || zombie.PresentationDeath == AdventureZombieDeathPresentation.Mowed))
            {
                ExplodeAdventureZomboni(zombie);
                return;
            }

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

            // PC DieWithLoot 0x54F030 calls DropShield(1) before selecting the death clip.
            DropAdventureZombieHeldShield(zombie, 1U);

            if (zombie.Kind == ZombieKind.Snorkel && zombie.PresentationNativePhase == 59)
            {
                // PC UpdateDamageStates removes a headless swimming snorkel before any land death clip.
                return;
            }

            if (zombie.Kind == ZombieKind.DolphinRider && zombie.PresentationNativePhase != 55)
            {
                // PC PlayDeathAnim 54F084 keeps a body reanimation only after the dolphin has been lost in water.
                return;
            }

            var rate = zombie.Kind == ZombieKind.Football ? 24f : zombie.Kind == ZombieKind.Gargantuar || zombie.Kind == ZombieKind.Snorkel || zombie.Kind == ZombieKind.Yeti ? 14f : zombie.Kind == ZombieKind.Digger || zombie.Kind == ZombieKind.Boss ? 18f : 24f + (float)_random.NextDouble() * 6f;
            if (zombie.Kind == ZombieKind.DolphinRider && zombie.ChillRemaining > 0f)
            {
                // PC phase-one UpdateAnimSpeed applies the retained chill once; the dying chill counter no longer advances.
                rate *= .5f;
            }
            var roll = _random.Next(100);
            var clip = zombie.Kind == ZombieKind.DolphinRider ? "anim_death" : zombie.PresentationInPool == true ? "anim_waterdeath" : roll == 99 && Adventure.Level.Number > 5 && zombie.ChillRemaining <= 0 && Zombies.Count <= 5 ? "anim_superlongdeath" : roll > 50 ? "anim_death2" : "anim_death";
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
            var backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie));
            var hasSled = zombie.PresentationHasSled ?? (zombie.Kind == ZombieKind.Bobsled && !zombie.UsedAbility);
            var scale = AdventureDancerRules.Scale(zombie);
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
                    var muzzleX = shot.SourceKind == PlantKind.CabbagePult ? 5f
                        : shot.SourceKind == PlantKind.KernelPult ? shot.Butter ? 12f : 19f : 25f;
                    var nativeLaunchX = 40f + shot.PresentationLaunchColumn * 80f + muzzleX;
                    var launch = (nativeLaunchX - 40f) / 80f;
                    var target = FindAdventureLobberTarget(shot.PresentationLaunchRow, shot.PresentationLaunchColumn);
                    var distance = 700f - nativeLaunchX;
                    if (target != null)
                    {
                        // PC ZombieTargetLeadX(50): eating, freezing and butter set prediction velocity to zero.
                        var stationary = AdventureStarfruitRules.TargetNotWalking(target)
                            || AdventureDancerRules.IsDancer(target) && AdventureDancerRules.GroupAffected(target, Zombies, false);
                        var velocity = stationary ? 0f : target.PresentationNativeVelocity * (target.ChillRemaining > 0f ? .5f : 1f);
                        if (target.PresentationWalkingBackwards ?? (target.Hypnotized
                            || target.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(target)))
                        {
                            velocity = -velocity;
                        }

                        var body = AdventureZombieTargetingRules.BodyRectangle(target);
                        distance = body.X + body.Width / 2 - velocity * 50f - nativeLaunchX - 30f;
                        if (target.PresentationNativePhase == 53 || target.Kind == ZombieKind.Pogo && !target.UsedAbility)
                        {
                            distance -= 60f;
                        }
                        else if (target.PresentationNativePhase == 59)
                        {
                            distance -= 40f;
                        }
                    }

                    shot.PresentationLobX = launch;
                    shot.PresentationPreviousX = launch;
                    shot.PresentationLobNativeVelocityX = Math.Max(40f, distance) / 120f;
                    shot.VelocityX = shot.PresentationLobNativeVelocityX * 100f / 80f;
                    var pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
                    var launchOffset = shot.SourceKind == PlantKind.KernelPult ? shot.Butter ? -56f : -37f : shot.Splash ? -46f : -12f;
                    var launchY = 80 + shot.PresentationLaunchRow * pitch + (Adventure.Level.HasRoof ? Math.Max(0, 5 - shot.PresentationLaunchColumn) * 20 - 10 : 0) + launchOffset;
                    if (HasPlantKindAt(PlantKind.FlowerPot, shot.PresentationLaunchRow, shot.PresentationLaunchColumn))
                    {
                        launchY -= 5;
                    }

                    shot.PresentationLaunchY = launchY;
                    shot.PresentationLobNativeX = nativeLaunchX;
                    shot.PresentationLobNativeY = launchY;
                    float heightDifference = 0;
                    if (target != null)
                    {
                        var nativeX = AdventureZombieNativeRules.NativePositionX(target.X, AdventureDancerRules.Scale(target));
                        var body = AdventureZombieTargetingRules.BodyRectangle(target);
                        var targetY = (int)AdventureBiteNativeY(target, nativeX) + body.Y + target.PresentationBiteBodyY;
                        if (target.Kind == ZombieKind.Boss)
                        {
                            targetY = (int)(80f + shot.PresentationLaunchRow * pitch - (Adventure.Level.HasRoof ? 10f : 0f));
                        }

                        heightDifference = targetY - launchY;
                    }

                    shot.PresentationLobInitialVelocity = -7f + heightDifference / 120f;
                    shot.PresentationLobVelocity = shot.PresentationLobInitialVelocity;
                    shot.PresentationLobImpactHeight = (shot.Butter ? -32f : shot.Splash ? -35f : -30f)
                        + (Adventure.Level.IsWaterRow(shot.Row) ? 40f : 0f);
                }
            }

            shot.PresentationNativeAge += dt;
            if (!shot.Lobbed)
            {
                return;
            }

            // PC 47C880 integrates float32 velocity before position, once per native update.
            // Keeping native X avoids a grid conversion rounding an integer contact edge one update early.
            var updates = (int)Math.Round(dt * 100f);
            for (var update = 0; update < updates; update++)
            {
                var previousX = shot.PresentationLobNativeX;
                shot.PresentationLobVelocity += .115f;
                shot.PresentationLobNativeX += shot.PresentationLobNativeVelocityX;
                shot.PresentationLobNativeZ += shot.PresentationLobVelocity;
                shot.PresentationLobContactHeight = shot.PresentationLobNativeZ;
                shot.PresentationLobFloorOffset = shot.PresentationLobNativeY - shot.PresentationLaunchY;
                if (Adventure.Level.HasRoof)
                {
                    // UpdateLobMotion checks contact before UpdateMotion moves mPosY and rebases mPosZ.
                    var rowY = 70f + shot.Row * 85f;
                    var previousFloor = rowY + Math.Max(0f, 440f - previousX) * .25f;
                    var floor = rowY + Math.Max(0f, 440f - shot.PresentationLobNativeX) * .25f;
                    var floorDelta = floor - previousFloor;
                    shot.PresentationLobNativeY += floorDelta;
                    shot.PresentationLobNativeZ -= floorDelta;
                }
            }

            shot.X = (shot.PresentationLobNativeX - 40f) / 80f;
            shot.PresentationLobHeight = shot.PresentationLobNativeY + shot.PresentationLobNativeZ - shot.PresentationLaunchY;
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
            // PC Projectile::DoImpact 0x47D410 uses the ash plants' twelve-tick shake.
            Presentation.ScreenShake.Start(3, -4);
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
            var maximum = kind == PlantKind.TallNut ? 8000 : 4000;
            return old != null && old.Kind == kind && old.BungeeState == 0 && old.Health < maximum * 2 / 3;
        }

        /// <summary>Performs remember adventure reward position.</summary>
        /// <param name = "zombie">The zombie.</param>
        private void RememberAdventureRewardPosition(Zombie zombie)
        {
            // PC 0x54DE40: GetZombieRect centre. The basic body rect is (36,0,42,115).
            var scale = AdventureDancerRules.Scale(zombie);
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

            // PC GetZombieRect (0x54DE40) mirrors the local body rectangle after mind control.
            if (zombie.Hypnotized)
            {
                var bodyWidth = zombie.Kind == ZombieKind.Gargantuar ? 180 : 120;
                rectX = bodyWidth - rectW - rectX;
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
            AdvanceAdventurePoolEffect();
            Elapsed += .01f;
            AdventurePresentationTick(.01f);
            // Original Board::UpdateGameObjects keeps existing projectiles moving through the award sequence.
            AdvanceAdventureProjectiles(.01f, allowDamage: false);
            AdvanceAdventurePresentationSuns(.01f);
            AdvanceVasePackets();
            AdvanceAdventureCoins(.01f);
            AdvanceAdventureStormTick();
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
            var enabledZombieTypes = new bool[34];
            _adventure = new AdventureBoard(level)
            {
                Phase = level.ChoosesSeeds ? AdventurePhase.ChoosingSeeds : AdventurePhase.Playing,
                SelectedSeeds = level.DefaultSeeds,
                Waves = Rules.DeferAdventureWaveInitialization ? Array.Empty<AdventureWave>()
                    : AdventureWavePlan.Build(level, _random, context: Rules.AdventureWaveContext, enabledOutput: enabledZombieTypes),
                Tutorial = level.IsReplay ? AdventureTutorial.Complete : level.Number == 1 ? AdventureTutorial.ChooseSeed : level.Number == 2 ? AdventureTutorial.PlantSunflower : level.Number == 5 ? AdventureTutorial.Shovel : level.Number == 11 ? AdventureTutorial.NightMushrooms : level.Number == 21 ? AdventureTutorial.PoolSupport : level.Number == 31 ? AdventureTutorial.FogVisibility : level.Number == 41 ? AdventureTutorial.RoofPots : level.Mode == AdventureMode.Bowling ? AdventureTutorial.Bowling : level.Mode == AdventureMode.WhackAZombie ? AdventureTutorial.Hammer : level.Mode == AdventureMode.Vasebreaker ? AdventureTutorial.Vases : level.Mode == AdventureMode.Boss ? AdventureTutorial.Boss : level.HasConveyor ? AdventureTutorial.Conveyor : AdventureTutorial.Complete,
                WaveCountdown = !level.IsReplay && level.Number == 2 ? 50f : level.Mode == AdventureMode.Bowling || level.Mode == AdventureMode.WhackAZombie || level.Mode == AdventureMode.LittleTrouble || level.Mode == AdventureMode.Storm || level.Mode == AdventureMode.Bungee ? 2f : 18f,
                _nextPacket = 4f,
                _nextSkySun = Rules.DeferAdventureWaveInitialization ? 0f
                    : !level.IsReplay && level.Number == 1 ? 7f : 4.25f + (float)_random.NextDouble() * 2.75f,
                _nextHammerSun = 8f,
                BossHealth = level.Mode == AdventureMode.Boss ? 40000f : 0f,
                _bossTimer = 12f
            };
            Array.Copy(enabledZombieTypes, Adventure.EnabledZombieTypes, enabledZombieTypes.Length);
            if (level.Terrain == AdventureTerrain.Fog)
            {
                Adventure.Fog.InitializeLooks(_gravePresentationRandom);
            }
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

            if (Rules.DeferAdventureWaveInitialization)
            {
                initialized = true;
                return;
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

        /// <summary>Accumulates the native water updates while the board waits for seed selection or preparation.</summary>
        private double _idlePoolRemainder;

        /// <summary>Advances the PC outer-board PoolEffect counter before gameplay objects update.</summary>
        /// <param name="survivalRepick">Whether PC CutScene::IsSurvivalRepick (0x446350) suppresses the water update.</param>
        internal void AdvanceAdventurePoolEffect(bool survivalRepick = false)
        {
            // PC Board::Update 0x421411..0x42143E: ice and survival repick stop the pool, not plant bobbing.
            if (IsAdventure && Adventure.Level.HasPool && Adventure.IceTrapTicks == 0
                && State != MatchState.Paused && State != MatchState.Lost && !survivalRepick)
            {
                Adventure.PoolEffectTicks++;
            }
        }

        /// <summary>Advances only the original water clock while gameplay is held by a chooser or preparation UI.</summary>
        /// <param name="seconds">The elapsed unpaused board presentation time.</param>
        /// <param name="survivalRepick">Whether a survival stage is returning to seed selection.</param>
        internal void AdvanceAdventurePoolPresentation(float seconds, bool survivalRepick = false)
        {
            if (!IsAdventure || State == MatchState.Paused || State == MatchState.Lost || seconds <= 0f
                || float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                return;
            }

            _idlePoolRemainder += seconds;
            while (_idlePoolRemainder + .0000001 >= SimulationStep)
            {
                _idlePoolRemainder -= SimulationStep;
                AdvanceAdventurePoolEffect(survivalRepick);
            }
        }

        /// <summary>Decrements PC Board::mIceTrapCounter after this tick's plants can reset it to 300.</summary>
        private void FinishAdventureIceTrapTick()
        {
            // PC 0x4748CC sets 300 during UpdateGameObjects; 0x421042 then decrements it in that same tick.
            if (Adventure.IceTrapTicks > 0)
            {
                Adventure.IceTrapTicks--;
            }
        }

        /// <summary>Performs advance adventure.</summary>
        /// <param name = "dt">The dt.</param>
        /// <param name = "handled">The handled.</param>
        private void AdvanceAdventure(float dt, ref bool handled)
        {
            handled = true;
            AdvanceAdventurePoolEffect();
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

            AdvanceAdventureFog();
            TickCellTimers(Adventure._craters, dt);
            AdvanceAdventureIceTrails();
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
            AdvanceVasePackets();
            AdvanceAdventurePlants(dt);
            AdvanceAdventureProjectiles(dt);
            AdvanceAdventureZombies(dt);
            FinishAdventureIceTrapTick();
            AdvanceAdventurePurchases(dt);
            if (State != MatchState.Playing)
            {
                Adventure.Phase = AdventurePhase.Lost;
                AdvanceAdventureStormTick();
                return;
            }

            AdvanceAdventureMowers(dt);
            AdvanceVaseMallet();
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

            // PC Board::HasZombies (0x4148E0) excludes mind-controlled zombies.
            var cleared = Zombies.TrueForAll(zombie => zombie.Hypnotized);
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
                // PC TrySpawnLevelAward (0x54BBC0) removes remaining allies without loot.
                Zombies.Clear();
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

            AdvanceAdventureStormTick();
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
                if (!a._packets[i].IsVaseDrop)
                {
                    a._packets[i].Age += dt;
                }
            }
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
            var picker = Adventure.RowPicker;
            if (Adventure.RakeAttracting && CanAdventureZombieSpawnInRow(kind, Adventure.RakeRow))
            {
                Adventure.RakeAttracting = false;
                picker.RecordPick(Adventure.RakeRow);
                return Adventure.RakeRow;
            }

            for (var row = 0; row < AdventureRowPicker.RowCount; row++)
            {
                var weight = 0f;
                if (CanAdventureZombieSpawnInRow(kind, row))
                {
                    if (ModeSession != null && ModeSession.Definition.Id == 26)
                    {
                        weight = AdventurePortalRowSpawnWeight(row);
                    }
                    else if (ModeSession == null || ModeSession.Definition.Id != 21 || Adventure.CurrentWave > 3 || row != 5)
                    {
                        var sinceMowed = Adventure.CurrentWave - picker.LastMowedWave[row];
                        if (IsNativeContinuousChallenge && Adventure.CurrentWave == Adventure.Waves.Count - 1)
                        {
                            sinceMowed = 100;
                        }

                        weight = sinceMowed <= 1 ? 0.01f : sinceMowed == 2 ? 0.5f : 1f;
                    }
                }

                picker.Weights[row] = weight;
            }

            return picker.Pick(_random);
        }

        /// <summary>Checks PC 0x4197D0's lane gates before smoothing or a forced rake pick.</summary>
        /// <param name="kind">The native zombie identifier being created.</param>
        /// <param name="row">The candidate zero-based lane.</param>
        /// <returns>Whether the board and native mode permit this species in the lane.</returns>
        private bool CanAdventureZombieSpawnInRow(ZombieKind kind, int row)
        {
            var level = Adventure.Level;
            if (row < 0 || row >= RowCount || !level.IsLawnRow(row))
            {
                return false;
            }

            var mode = ModeSession == null ? 0 : ModeSession.Definition.Id;
            var wave = Adventure.CurrentWave;
            if (mode == 31)
            {
                wave += (RequiredModeSession.Stage - 1) * RequiredModeSession.Definition.WaveCount;
            }

            if (!AdventureZombiePoolRules.CanSpawnInRow(kind, level.IsWaterRow(row), wave)
                || level.HasRoof && (kind == ZombieKind.Zomboni || kind == ZombieKind.Bobsled)
                || kind == ZombieKind.Bobsled && !Adventure._ice.Exists(ice => ice.Row == row && ice.RemainingTicks > 0))
            {
                return false;
            }

            if (row == 0 && (kind == ZombieKind.Gargantuar || (int)kind == 32) && (mode < 11 || mode > 15))
            {
                return false;
            }

            return kind != ZombieKind.Dancing || level.HasPool || level.IsLawnRow(row - 1) && level.IsLawnRow(row + 1);
        }

        /// <summary>Checks PC 0x460B30's modes whose last authored wave is reused continuously.</summary>
        private bool IsNativeContinuousChallenge
        {
            get
            {
                if (ModeSession == null)
                {
                    return Adventure.Level.Mode == AdventureMode.Boss;
                }

                var mode = ModeSession.Definition.Id;
                return mode == 18 || mode == 20 || mode == 22 || mode == 24 || mode == 35
                    || mode == 36 || mode == 40 || mode == 71 || mode == 72;
            }
        }

        /// <summary>Preserves PC 0x432A40's portal-path distance and portal-presence lane weights.</summary>
        /// <param name="row">The zero-based lane from which the zombie would enter.</param>
        /// <returns>One hundredth near a mower, one on an open portal lane, or one fifth on another lane.</returns>
        /// <exception cref="InvalidOperationException">An open portal has no matching exit.</exception>
        private float AdventurePortalRowSpawnWeight(int row)
        {
            var portals = RequiredModeSession.Portals;
            var pathRow = row;
            var column = 10;
            var distance = 0;
            while (distance < 40)
            {
                ModePortal? entrance = null;
                foreach (var portal in portals)
                {
                    if (portal.Row == pathRow && portal.Column < column && (entrance == null || portal.Column >= entrance.Column))
                    {
                        entrance = portal;
                    }
                }

                if (entrance == null)
                {
                    distance += column;
                    break;
                }

                ModePortal? exit = null;
                foreach (var portal in portals)
                {
                    if (portal != entrance && portal.Pair == entrance.Pair)
                    {
                        exit = portal;
                        break;
                    }
                }

                if (exit == null)
                {
                    throw new InvalidOperationException("An open spawn portal has no matching exit.");
                }

                distance += column - entrance.Column;
                column = exit.Column;
                pathRow = exit.Row;
            }

            if (distance < 5)
            {
                return 0.01f;
            }

            foreach (var portal in portals)
            {
                if (portal.Row == row)
                {
                    return 1f;
                }
            }

            return 0.2f;
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
            if (p.IsVaseDrop)
            {
                p.InitializeVaseDrop(_random);
            }

            Adventure._packets.Add(p);
            InitializeAdventureConveyorPacket(p, Adventure._packets.Count - 1);
            Adventure.Notice(AdventureNoticeKind.ConveyorPacket, p.Id);
            return p;
        }

        /// <summary>Collects a live vase seed coin for the normal cursor.</summary>
        /// <param name="packetId">The identifier of the usable seed coin beneath the pointer.</param>
        /// <returns>Whether a live, unheld vase seed was collected while the board was playing.</returns>
        internal bool CollectVasePacket(int packetId)
        {
            if (!IsAdventure || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing
                || ModeSession != null && ModeSession.Phase != ModePhase.Playing)
            {
                return false;
            }

            foreach (var packet in Adventure._packets)
            {
                if (packet.Id == packetId && packet.IsVaseDrop && !packet.IsHeld)
                {
                    packet.CollectVaseDrop();
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns a selected usable seed to its original drop position after cursor cancellation.</summary>
        /// <param name="packetId">The identifier of the vase seed coin previously held by the cursor.</param>
        internal void DropVasePacket(int packetId)
        {
            if (!IsAdventure)
            {
                return;
            }

            foreach (var packet in Adventure._packets)
            {
                if (packet.Id == packetId && packet.IsVaseDrop && packet.IsHeld)
                {
                    packet.DropVaseDrop();
                    return;
                }
            }
        }

        /// <summary>Advances all original vase seed coins once, including expiration and independent recipe cleanup.</summary>
        private void AdvanceVasePackets()
        {
            for (var index = Adventure._packets.Count - 1; index >= 0; index--)
            {
                var packet = Adventure._packets[index];
                if (!packet.IsVaseDrop)
                {
                    continue;
                }

                packet.Age += .01f;
                if (packet.TickVaseDrop(_random))
                {
                    RemoveAdventurePacket(packet);
                    ModeSession?._backwardPackets.Remove(packet.Id);
                }
            }
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
            Presentation.VaseMalletTicks = 0;
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
            for (var c = stage == 0 ? 6 : stage == 1 ? 5 : 4; c < 9; c++)
            {
                for (var r = 0; r < 5; r++)
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

            // PC 0x4338B0 only requests green plant vases for the second and third stages.
            RevealRandomVasePlants(stage == 0 ? 0 : stage + 1);

            Adventure.Notice(AdventureNoticeKind.VaseStage, stage);
        }

        /// <summary>Selects the original green plant vases uniformly without replacement in creation order.</summary>
        /// <param name="count">The number of hidden plant vases to reveal.</param>
        private void RevealRandomVasePlants(int count)
        {
            var plants = new List<AdventureVase>();
            foreach (var vase in Adventure._vases)
            {
                if (vase._seed.HasValue && !vase.ShowsPlant)
                {
                    plants.Add(vase);
                }
            }

            for (var index = 0; index < count && plants.Count > 0; index++)
            {
                var pick = _random.Next(plants.Count);
                plants[pick].ShowsPlant = true;
                plants.RemoveAt(pick);
            }
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

        /// <summary>Starts the original delayed hammer swing over an adventure vase.</summary>
        /// <param name="row">The target vase's lawn row.</param>
        /// <param name="column">The target vase's lawn column.</param>
        /// <returns>Whether an available vase accepted a new hammer swing.</returns>
        public bool SmashAdventureVase(int row, int column)
        {
            if (!IsAdventure || ModeSession != null || State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing || Adventure.Level.Mode != AdventureMode.Vasebreaker
                || Adventure.VaseInterludeActive && Adventure._vaseInterludePopulated)
            {
                return false;
            }

            return BeginVaseMallet(row, column);
        }

        /// <summary>Starts the single native pot-opening hammer without releasing its contents early.</summary>
        /// <param name="row">The target vase's lawn row.</param>
        /// <param name="column">The target vase's lawn column.</param>
        /// <returns>Whether the target exists and no earlier hammer is still active.</returns>
        private bool BeginVaseMallet(int row, int column)
        {
            if (Presentation.VaseMalletTicks > 0)
            {
                return false;
            }

            foreach (var vase in Adventure._vases)
            {
                if (vase.Row == row && vase.Column == column)
                {
                    // PC 0x434A50 creates one Hammer, anim_open_pot, PlayOnceAndHold at 40 FPS.
                    Presentation.VaseMalletRow = row;
                    Presentation.VaseMalletColumn = column;
                    Presentation.VaseMalletTicks = AdventurePresentationState.VaseMalletDurationTicks;
                    Presentation.VaseSwingSoundSerial++;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Releases the targeted pot only after the original hammer has completed one loop.</summary>
        private void AdvanceVaseMallet()
        {
            if (Presentation.VaseMalletTicks == 0)
            {
                return;
            }

            Presentation.VaseMalletTicks--;
            if (Presentation.VaseMalletTicks > 0)
            {
                return;
            }

            for (var index = 0; index < Adventure._vases.Count; index++)
            {
                var vase = Adventure._vases[index];
                if (vase.Row != Presentation.VaseMalletRow || vase.Column != Presentation.VaseMalletColumn)
                {
                    continue;
                }

                Adventure._vases.RemoveAt(index);
                if (ModeSession == null)
                {
                    OpenAdventureVase(vase);
                }
                else
                {
                    OpenModeVase(vase);
                }

                Presentation.VaseBreakSoundSerial++;
                break;
            }
        }

        /// <summary>Releases the plant or zombie hidden in an already removed adventure vase.</summary>
        /// <param name="vase">The vase reached by the completed native hammer swing.</param>
        private void OpenAdventureVase(AdventureVase vase)
        {
            if (vase._seed.HasValue)
            {
                AddAdventurePacket(vase._seed.Value, vase.Row, vase.Column);
            }

            if (vase._zombie.HasValue)
            {
                SpawnAdventureBoardZombie(vase._zombie.Value, vase.Row, vase.Column + 0.5f, Adventure.VaseStage);
            }
        }

        /// <summary>Starts the original half-second flash at the native puzzle-stage clearing point.</summary>
        private void StartVaseScreenFlash()
        {
            Presentation.VaseScreenFlashSerial++;
            Presentation.VaseScreenFlashTicks = 0;
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
                    StartVaseScreenFlash();
                    Adventure.Notice(AdventureNoticeKind.VaseInterludeStarted, message);
                    if (!Adventure.VaseInterludeDialogueEnabled)
                    {
                        PopulateAdventureVaseInterlude(message == 2700 ? 2702 : 2801);
                        FinishAdventureVaseInterlude();
                    }
                }
            }
            else if (ModeSession == null && Adventure.Level.Mode == AdventureMode.Vasebreaker && Adventure._vases.Count == 0 && Zombies.TrueForAll(zombie => zombie.Hypnotized) && Adventure.VaseStage < 2)
            {
                Zombies.Clear();
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
                        SpawnAdventureBoardZombie(kind, _random.Next(5), 9.5f + i * 0.2f, a._bossCycle);
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
                return l.IsWaterRow(row) && lily != null && lily.BungeeState == 0 && lily.Kind == PlantKind.LilyPad && AdventurePlantAt(row, column, PlantLayer.Main) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.NeedsUpgrade;
            }

            var source = AdventurePlantAt(row, column, PlantLayer.Main);
            if (source == null || source.BungeeState != 0 || source.Column != column || source.Kind != prerequisite.Value)
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
                if (right == null || right.BungeeState != 0 || right.Kind != PlantKind.KernelPult || right.Column != column + 1 || AdventurePlantAt(row, column, PlantLayer.Armor) != null || AdventurePlantAt(row, column + 1, PlantLayer.Armor) != null)
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
                                RequestAdventureZombieFoley(1);
                            }
                        }

                        p.ActionCount++;
                        // PC Gloom-shroom's four area pulses have no launch Foley.
                        Presentation._nativeShootSuppressions++;
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
                    RequestAdventureZombieFoley(4);
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
                                HitAdventureZombie(z, 1800f, true, z.Kind == ZombieKind.Zomboni ? 32U : 0U);
                                RequestAdventureZombieFoley(1);
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
                                RequestAdventureZombieFoley(1);
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
                        RequestAdventureZombieFoley(42);
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

            var previousRemaining = p.CannonReloadRemaining;
            p.CannonReloadRemaining = Math.Max(0, p.CannonReloadRemaining - dt);
            // PC reanim markers: charge has 15 frames at 12 fps, shooting has 42 at 12 fps.
            var cueRemaining = p.CannonPhase == CobCannonPhase.Loading
                ? AdventureCobLoadingSeconds - .5f * 15f / 12f
                : AdventureCobFiringSeconds - .48f * 42f / 12f;
            if ((p.CannonPhase == CobCannonPhase.Loading || p.CannonPhase == CobCannonPhase.Firing)
                && previousRemaining > cueRemaining + .000001f && p.CannonReloadRemaining <= cueRemaining + .000001f)
            {
                RequestAdventureZombieFoley(p.CannonPhase == CobCannonPhase.Loading ? 64 : 76);
            }
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
                RequestAdventureZombieFoley(3);
                Presentation._nativeShootSuppressions++;
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
            if (coin.ValueUnits > 0)
            {
                if (kind == AdventureCoinKind.Silver || kind == AdventureCoinKind.Gold)
                {
                    coin.PresentationX -= 10f;
                    coin.PresentationY -= 8f;
                    // Original silver/gold reanimations have 21 frames at 30 fps.
                    coin.AnimationOffset = (float)_random.NextDouble() * .99f * 21f / 30f;
                    coin.AnimationRate = .6f + (float)_random.NextDouble() * .4f;
                }

                coin.FallVelocityY = -3f - (float)_random.NextDouble() * 2f;
                coin.FallVelocityX = -.5f + (float)_random.NextDouble();
                coin.GroundY = Math.Max(80, Math.Min(521, (int)coin.PresentationY + 45 + _random.Next(20)));
                coin.PresentationPreviousX = coin.PresentationX;
                coin.PresentationPreviousY = coin.PresentationY;
            }

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
                        if (FinishAdventureCoin(coin))
                        {
                            RequestAdventureZombieFoley(31);
                        }
                    }
                }
                else
                {
                    coin.TickFall();
                    if (coin.PresentationX > 740f)
                    {
                        coin.PresentationX = 740f;
                        coin.FallVelocityX = -.4f - (float)_random.NextDouble() * .4f;
                    }
                    else if (coin.PresentationX < 0f)
                    {
                        coin.PresentationX = 0f;
                        coin.FallVelocityX = .4f + (float)_random.NextDouble() * .4f;
                    }

                    if (coin.Age >= 30f && !coin.PersistencePending)
                    {
                        Adventure._coins.RemoveAt(i);
                    }
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
                BlastAdventureRadius((int)(40f + shell.TargetX * 80f), AdventureBlastCenterY(shell.TargetX, shell.TargetRow), row, 115, 1, 0x7F);
                AdventurePresentationCobImpact(shell.TargetX, shell.TargetRow);
                if (shell.TargetX >= 7.5f && Math.Abs(shell.TargetRow - 2f) <= 1f)
                {
                    DamageAdventureBoss(1800f);
                }

                Adventure._cobShells.RemoveAt(i);
                // PC 0x47D410 uses the same direct Doom-shroom sample for the cob blast.
                Raise(GameEvent.DoomExplosion);
                Adventure.Notice(AdventureNoticeKind.CobImpact, shell.SourcePlantId, row, (int)shell.TargetX);
            }
        }

        /// <summary>Performs adventure zombie attack rectangle.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie attack rectangle.</returns>
        public NativeRect AdventureZombieAttackRectangle(Zombie z)
        {
            int kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind, phase = AdventureZombieFeedbackPhase(z);
            var x = (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var y = (int)AdventureBiteNativeY(z, x);
            var backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z));
            return global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.AttackRect(kind, phase, Adventure.Level.Mode == global::PlantsVsZombies.Adventure.AdventureMode.Bowling, backwards, x, y, z.PresentationBiteBodyY, z.PresentationBiteClipHeight, z.Kind == ZombieKind.Gargantuar ? 180 : 120);
        }

        /// <summary>Performs adventure bite native y.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "x">The x.</param>
        /// <returns>The resulting adventure bite native y.</returns>
        private float AdventureBiteNativeY(Zombie z, float x)
        {
            return 80f + z.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f) + (Adventure.Level.HasRoof ? Math.Max(0f, 5f - x / 80f) * 20f - 10f : 0f) - 30f - (z.Kind == ZombieKind.Balloon ? 30f : 0f) - (z.Kind == ZombieKind.Pogo ? 16f : 0f) + z.PresentationVaultPositionY;
        }

        /// <summary>Performs adventure plant eating rectangle.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting adventure plant eating rectangle.</returns>
        public NativeRect AdventurePlantEatingRectangle(Plant p)
        {
            return global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.PlantRect((int)p.Kind, 40 + p.Column * 80, 80 + p.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85 : 100) + (Adventure.Level.HasRoof ? (int)(Math.Max(0f, 5f - p.Column) * 20f - 10f) : 0), p.Kind == PlantKind.Pumpkin ? 120 : 80);
        }

        /// <summary>Uses the original BungeeOrder/CatapultOrder instead of plant creation order for a layered cell.</summary>
        /// <param name="row">The native lawn row.</param>
        /// <param name="column">The native lawn column.</param>
        /// <returns>The flying, normal, pumpkin or support plant in original priority order.</returns>
        private Plant? AdventureAirAttackPlantAt(int row, int column)
        {
            return AdventurePlantAt(row, column, PlantLayer.Effect) ?? AdventurePlantAt(row, column, PlantLayer.Main)
                ?? AdventurePlantAt(row, column, PlantLayer.Armor) ?? AdventurePlantAt(row, column, PlantLayer.Support);
        }

        /// <summary>Applies the original SquishAllInSquare stack removal rather than letting a pumpkin absorb a whole-cell crush.</summary>
        /// <param name="row">The crushed lawn row.</param>
        /// <param name="column">The crushed lawn column.</param>
        /// <param name="driveOver">Whether to preserve spiky plants for the separate vehicle/spike collision.</param>
        private void CrushAdventurePlantCell(int row, int column, bool driveOver)
        {
            foreach (var plant in new PooledSnapshot<Plant>(Plants))
            {
                if (plant.Row == row && plant.Column == column && plant.Kind != PlantKind.Spikerock
                    && (!driveOver || plant.Kind != PlantKind.Spikeweed))
                {
                    RemoveAdventurePlantAndStack(plant);
                }
            }
        }

        /// <summary>Performs adventure bite can target.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "p">The p.</param>
        /// <param name = "attack">The attack.</param>
        /// <param name = "priority">The priority.</param>
        /// <returns>The resulting adventure bite can target.</returns>
        private bool AdventureBiteCanTarget(Zombie z, Plant p, global::PlantsVsZombies.Adventure.AdventureBiteAttack attack, bool priority = true)
        {
            if (p.Health <= 0 || p.Row != z.Row || p.Layer == global::PlantsVsZombies.Adventure.PlantLayer.Effect || p.BungeeState == 2 || p.PresentationBiteNotOnGround || p.Kind == PlantKind.TangleKelp || p.Kind == PlantKind.Squash && p.PresentationBiteNativeState >= 5 && p.PresentationBiteNativeState <= 7)
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
                var barrier = (p.Kind == PlantKind.Wallnut || p.Kind == PlantKind.TallNut || p.Kind == PlantKind.Pumpkin) && !AdventureHasLadderAt(p.Row, p.Column);
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
            if (z.TangleKelpGrab != null)
            {
                // PC FindPlantTarget and UpdateZombieDolphinRider reject a target already held by kelp.
                return null;
            }

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
            if (z.TangleKelpGrab != null)
            {
                // PC FindPlantTarget 0x54B392 rejects kelp targets without clearing the existing eating latch.
                return z.Eating;
            }

            if (z.Headless || !AdventureNativeChewer(z) || z.PresentationGarlicCounter >= 0)
            {
                return false;
            }

            var checks = global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.EatingChecks(z.Age, dt, z.ChillRemaining > 0);
            if (checks > 0)
            {
                if (target != null || zombieTarget)
                {
                    z.Eating = true;
                    if (z.Kind == ZombieKind.Snorkel)
                    {
                        z.Submerged = false;
                    }
                }
                else
                {
                    StopAdventureNativeEating(z);
                }
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

        /// <summary>Restarts native walking and selects its species speed when a latched meal ends.</summary>
        /// <param name="z">The zombie whose plant or opposing zombie is no longer in eating range.</param>
        private void StopAdventureNativeEating(Zombie z)
        {
            if (!z.Eating)
            {
                return;
            }

            z.Eating = false;
            if (z.Kind == ZombieKind.Snorkel)
            {
                z.Submerged = z.PresentationInPool == true;
                // The native UpdateZombieSnorkel sees StopEating before the next position update.
                // Start the reverse pose now so this tick cannot move while still displaying anim_eat.
                if (z.PresentationNativePhase == 60 || z.PresentationNativePhase == 61)
                {
                    SetAdventureZombieMotionPhase(z, 62);
                }
            }

            var phase = AdventureZombieFeedbackPhase(z);
            // PC StopEating 0x54B1C0 skips StartWalkAnim for tunnelling diggers and snorkel zombies.
            // StartWalkAnim 0x54B050 calls PickRandomSpeed 0x540500 before selecting the walking clip.
            if (phase != 32 && z.Kind != ZombieKind.Snorkel)
            {
                PickAdventureZombieNativeSpeed(z, phase);
                AdventurePresentationZombieAnimation(z);
            }
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
                RequestAdventureZombieFoley(-5);
                AdventureDancerRules.DetachForMindControl(z, Zombies);
                // PC 0x547610 calls TrySpawnLevelAward using this zombie after mind control.
                RememberAdventureRewardPosition(z);
                z.Submerged = false;
                // PC 0x547610 retains the eating latch: 0.17 is replaced when StopEating restarts walking.
                z.PresentationNativeVelocity = .17f;
                z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(.17f);
                Presentation.PlantBiteEffects.Add(new global::PlantsVsZombies.Adventure.AdventurePlantBiteEffect {
                    Id = ++Presentation._effectId,
                    ParticleId = 88,
                    Row = z.Row,
                    Kind = z.Kind,
                    NativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z)),
                    NativeY = AdventureBiteNativeY(z, global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z))),
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
            // PC PlayDeathAnim replaces every live phase, stops eating, and retains the current pool altitude.
            corpse.RequiredZombie.PresentationNativePhase = 1;
            corpse.RequiredZombie.Eating = false;
            if (corpse.RequiredZombie.PresentationNativeHeight == 6)
            {
                corpse.RequiredZombie.PresentationNativeHeight = 7;
            }

            if (corpse.RequiredZombie.PresentationBodyBlendTicks >= 0)
            {
                corpse.RequiredZombie.PresentationBodyBlendTicks = 20;
            }
            if (corpse.RequiredZombie.Kind == ZombieKind.Pogo)
            {
                corpse.RequiredZombie.PresentationAltitude = 0f;
                corpse.RequiredZombie.PresentationBiteBodyY = 0;
                (corpse.RequiredZombie.PogoState ??= new AdventurePogoState()).AnimationBlendTicks = 20;
            }

            if (corpse.RequiredZombie.Kind == ZombieKind.DolphinRider)
            {
                corpse.RequiredZombie.PresentationNativeVelocity = 0f;
                corpse.RequiredZombie.Speed = 0f;
                (corpse.RequiredZombie.DolphinState ??= new AdventureDolphinState()).AnimationBlendTicks = 20;
            }

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

            if (z.Kind == ZombieKind.Bobsled && z.BobsledState?.Position == 0
                && z.PresentationNativePhase != 19 && z._presentationArmorBeforeHit > 0f
                && z.ArmorHealth <= 0f && (flags & 16U) == 0)
            {
                CrashAdventureBobsled(z);
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
                var id = AdventureZombieEquipmentRules.ShieldParticle(z);
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

                z.X = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridX(global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.SpawnSourceX((int)z.Kind, r40, r10), AdventureDancerRules.Scale(z));
            }

            if (z.Kind == ZombieKind.PoleVaulter)
            {
                z.PresentationNativePhase = 11;
            }
            else if (z.Kind == ZombieKind.Ladder)
            {
                z.PresentationNativePhase = z.ShieldHealth > 0f ? 76 : 0;
            }
            else if (z.Kind == ZombieKind.Pogo)
            {
                z.PogoState = new AdventurePogoState { PhaseTicks = 1 + _random.Next(AdventurePogoRules.BounceTicks) };
                z.PresentationNativePhase = 20;
                z.PresentationNativeHeight = 0;
            }
            else if (z.Kind == ZombieKind.Digger)
            {
                z.DiggerState = new AdventureDiggerState();
                z.PresentationNativePhase = 32;
                z.PresentationNativeHeight = 0;
            }
            else if (z.Kind == ZombieKind.Snorkel)
            {
                z.PresentationNativePhase = 57;
                z.Submerged = false;
            }
            else if (z.Kind == ZombieKind.DolphinRider)
            {
                z.DolphinState = new AdventureDolphinState();
                z.PresentationNativePhase = 51;
            }
            else if (z.Kind == ZombieKind.Balloon)
            {
                z.PresentationNativePhase = 73;
                z.PresentationAltitude = 25f;
                z.PresentationNativeHeight = 0;
            }

            // Explicit in-lawn spawns retain their pool placement; edge spawns enter through CheckForPool.
            z.PresentationPoolRow = Adventure.Level.IsWaterRow(z.Row);
            z.PresentationInPool = !rightEdge && z.PresentationPoolRow
                && AdventureZombieMotionRules.IsPoolPosition(AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z)));
            if (z.PresentationInPool == true)
            {
                z.PresentationAltitude = z.Kind == ZombieKind.Snorkel ? 10f : -40f * AdventureDancerRules.Scale(z);
                if (z.Kind == ZombieKind.Snorkel)
                {
                    z.PresentationNativePhase = 59;
                    z.Submerged = true;
                }
                else if (z.Kind == ZombieKind.DolphinRider)
                {
                    z.PresentationAltitude = 0f;
                    z.PresentationNativePhase = 53;
                }
            }

            if (z.Kind == ZombieKind.Balloon)
            {
                z.PresentationInPool = false;
                z.PresentationAltitude = 25f;
            }

            if (AdventureDancerRules.IsDancer(z))
            {
                AdventureDancerRules.Initialize(z, z.Kind == ZombieKind.Dancing ? _random.Next(12) : 0);
            }

            // PC riding retains the speed selected for the original walk carrying the dolphin.
            PickAdventureZombieNativeSpeed(z, z.Kind == ZombieKind.DolphinRider ? 51 : AdventureZombieFeedbackPhase(z));
            if (z.Kind == ZombieKind.DolphinRider)
            {
                z._presentationSpeedPhase = z.PresentationNativePhase;
            }
            // These assignments occur after the initial PickRandomSpeed in PC Init.
            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility)
            {
                z.BobsledState = new AdventureBobsledState { LeaderId = z.Id };
                z.PresentationNativePhase = 17;
                z.PresentationHasSled = true;
                z.PresentationAltitude = AdventureBobsledRules.InitialAltitude(0);
                z.PresentationNativeVelocity = .6f;
                z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(.6f);
            }
        }

        /// <summary>Performs refresh adventure zombie native speed.</summary>
        /// <param name = "z">The z.</param>
        private void RefreshAdventureZombieNativeSpeed(Zombie z)
        {
            if (z.Kind == ZombieKind.Bungee && z.BungeeState != null)
            {
                z.PresentationNativeVelocity = 0f;
                z.Speed = 0f;
                return;
            }

            if (z.ZomboniState?.FlatTires == true)
            {
                z.PresentationNativeVelocity = 0f;
                z.Speed = 0f;
                return;
            }

            if (z.PresentationWhackSpeed)
            {
                return;
            }

            var phase = AdventureZombieFeedbackPhase(z);
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            if (z.Kind == ZombieKind.Bobsled && z.BobsledState != null && !z.UsedAbility)
            {
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
                return;
            }

            if (AdventurePogoRules.IsBouncing(z) && z.PogoState != null)
            {
                // PC 540F10 retains the explicitly selected zero or forward-vault velocity until the next landing.
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
                return;
            }

            if (z.Kind == ZombieKind.Digger && phase >= 33 && phase <= 36)
            {
                // PC emergence and axe-loss pause retain the tunnelling velocity until StartWalkAnim.
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                return;
            }

            if (z.Kind == ZombieKind.Balloon && phase == 74)
            {
                // PC LandFlyer changes only the clip: PickRandomSpeed happens after the pop finishes.
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
                return;
            }

            if (z.Kind == ZombieKind.DolphinRider && (phase == 52 || phase == 53 || phase == 54))
            {
                // PC 541C70 changes these clips without PickRandomSpeed; takeoff and landing set velocity explicitly.
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
                return;
            }

            if (z._presentationSpeedPhase != phase || z._presentationSpeedKind != kind)
            {
                PickAdventureZombieNativeSpeed(z, phase);
            }

            if (z.Kind == ZombieKind.Zomboni)
            {
                var nativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
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
                NativePhase = z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo ? z.PresentationNativePhase : -1,
                NativeAltitude = z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo ? z.PresentationAltitude : 0f,
                ParticleId = particleId,
                X = z.X,
                Row = z.Row,
                Scale = AdventureDancerRules.Scale(z),
                Asset = AdventureZombiePresentationAsset(z),
                Animation = clip,
                AnimationAge = z.PresentationAnimationAge,
                Rate = AdventurePresentationZombieRate(z),
                Anchor = anchor,
                Image = image,
                Backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)),
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
                BreakAdventurePogo(z, flags);
            }
        }

        /// <summary>Detaches a remaining shield from the held pose before a native death replaces it.</summary>
        /// <param name="zombie">The dying shield carrier whose intact pose is captured.</param>
        /// <param name="flags">The original DropShield mask; bit 1 suppresses the newspaper rip and bit 16 suppresses particles.</param>
        private void DropAdventureZombieHeldShield(Zombie zombie, uint flags)
        {
            if (zombie.ShieldHealth <= 0f)
            {
                return;
            }

            var particle = AdventureZombieEquipmentRules.ShieldParticle(zombie);
            if (particle < 0)
            {
                return;
            }

            if ((flags & 16U) == 0)
            {
                var kind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
                AddAdventureZombiePart(zombie, kind, particle, AdventureZombieEquipmentRules.ShieldTrack(zombie), null);
                if (zombie.Kind == ZombieKind.Newspaper && (flags & 1U) == 0)
                {
                    RequestAdventureZombieFoley(67);
                }
            }

            zombie.ShieldHealth = 0f;
        }

        /// <summary>Performs adventure presentation zombie mowed parts.</summary>
        /// <param name = "z">The z.</param>
        private void AdventurePresentationZombieMowedParts(Zombie z)
        {
            var inPool = z.PresentationInPool ?? (Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            if (!inPool || !Adventure.Level.IsWaterRow(z.Row))
            {
                // PC MowDown 0x54E776/0x54E8CA drops shields, including a newspaper rip.
                DropAdventureZombieHeldShield(z, 0U);
            }

            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var height = z.PresentationNativeHeight;
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            if (!global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.CanLoseParts(kind, height, z.Airborne, sled))
            {
                return;
            }

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
            if (z.BungeeCarrierId != 0)
            {
                return;
            }

            if (z.Kind == ZombieKind.Bungee && z.BungeeState != null)
            {
                if (z.FrozenRemaining <= 0f && z.PresentationButter <= 0f)
                {
                    z.BungeeState.AnimationSeconds += dt;
                    z.PresentationAnimationAge = z.BungeeState.AnimationSeconds;
                }

                return;
            }

            RefreshAdventureZombieNativeSpeed(z);
            var clip = AdventurePresentationZombieAnimation(z);
            z._presentationPreviousWalkCycle = z._presentationWalkLoops + (double)z._presentationWalkPhase;
            z._presentationTickSlow = (AdventureDancerRules.IsDancer(z)
                ? AdventureDancerRules.GroupAffected(z, Zombies, true) : z.ChillRemaining > 0f) ? .5f : 1f;
            if (z.Kind == ZombieKind.Balloon && z.Airborne
                || z.Kind == ZombieKind.DolphinRider && (z.PresentationNativePhase == 52 || z.PresentationNativePhase == 54))
            {
                // PC UpdateAnimSpeed leaves the original clocks on flat ground tracks unaffected by chill.
                z._presentationTickSlow = 1f;
            }
            if (z.FrozenRemaining > 0f || z.PresentationButter > 0f)
            {
                return;
            }

            if (z.Kind == ZombieKind.Digger && z.PresentationNativePhase == 34)
            {
                // SetAnimRate(0) preserves the current digging frame during the axe-loss pause.
                return;
            }

            var step = dt * z._presentationTickSlow;
            if (AdventurePogoRules.IsBouncing(z))
            {
                var state = z.PogoState ??= new AdventurePogoState();
                state.AnimationSeconds = Math.Min(AdventurePogoRules.HeldAnimationSeconds, state.AnimationSeconds + step);
                z.PresentationAnimationAge = state.AnimationSeconds;
                return;
            }

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

            if (z.Kind == ZombieKind.PoleVaulter && z.PresentationNativePhase == 12)
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
            if (z.BungeeCarrierId != 0)
            {
                return "anim_idle";
            }

            if (z.Kind == ZombieKind.Zomboni && z.ZomboniState?.FlatTires == true)
            {
                return z.ZomboniState.Animation;
            }

            string asset = AdventureZombiePresentationAsset(z), clip;
            var motionClip = AdventureZombieMotionRules.Animation(z);
            if (z.Kind == ZombieKind.JackInTheBox && z.PresentationNativePhase == 16)
            {
                clip = "anim_pop";
            }
            else if (motionClip != null)
            {
                clip = motionClip;
            }
            else if (AdventureDancerRules.IsDancer(z))
            {
                clip = AdventureDancerRules.Animation(z);
            }
            else if (z.PresentationNewspaperGaspRemaining > 0f)
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
            else if (z.Kind == ZombieKind.PoleVaulter && z.PresentationNativePhase == 12)
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
            else if (z.PresentationInPool == true && z.PoolState?.HasDuckyTube == true && z.PresentationNativeHeight == 0)
            {
                clip = "anim_swim";
            }
            else if (z.Kind == ZombieKind.Balloon && z.Airborne)
            {
                clip = "anim_idle";
            }
            else if (z.Kind == ZombieKind.Bobsled && z.PresentationNativePhase == 18)
            {
                clip = "anim_jump";
            }
            else if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility && z.PresentationNativePhase != 19)
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
            else if (z.Kind == ZombieKind.Bungee && z.BungeeState != null)
            {
                clip = z.BungeeState.Animation;
            }
            else if (z.BungeeCarrierId != 0)
            {
                clip = "anim_idle";
            }
            else
            {
                clip = z.PresentationWalk2 ? "anim_walk2" : "anim_walk";
            }

            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (z.Kind != ZombieKind.Bungee && !(z.Kind == ZombieKind.Ladder && z.PresentationNativePhase == 77)
                && !global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(asset, clip, out data))
            {
                clip = global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(asset, "anim_walk", out data) ? "anim_walk" : "anim_idle";
            }

            if (!string.Equals(z.PresentationAnimation, clip, StringComparison.Ordinal))
            {
                if (z.Kind == ZombieKind.PoleVaulter && z.PresentationAnimation.Length > 0)
                {
                    z.PresentationPoleVaultBlendTicks = 20;
                }

                if (z.Kind == ZombieKind.Balloon && z.PresentationAnimation.Length > 0)
                {
                    z.PresentationBalloonBlendTicks = 20;
                }

                if (z.Kind == ZombieKind.DolphinRider
                    && (z.Eating || string.Equals(z.PresentationAnimation, "anim_eat", StringComparison.Ordinal)))
                {
                    (z.DolphinState ??= new AdventureDolphinState()).AnimationBlendTicks = 20;
                }

                if (AdventureDancerRules.IsDancer(z) && z.DancerState != null
                    && (z.Eating || string.Equals(z.PresentationAnimation, "anim_eat", StringComparison.Ordinal)))
                {
                    // PC StartEating and StopEating use twenty-tick blends independently of the current dance phase.
                    z.DancerState.AnimationBlendTicks = 20;
                }

                if (z.Kind == ZombieKind.Pogo && z.PogoState != null
                    && (z.Eating || string.Equals(z.PresentationAnimation, "anim_eat", StringComparison.Ordinal)))
                {
                    z.PogoState.AnimationBlendTicks = 20;
                }

                if (z.Kind == ZombieKind.Digger && z.DiggerState != null
                    && (z.Eating || string.Equals(z.PresentationAnimation, "anim_eat", StringComparison.Ordinal)))
                {
                    z.DiggerState.AnimationBlendTicks = 20;
                }

                if (z.PresentationBodyBlendTicks >= 0 && (z.Eating || z.PresentationAnimation == "anim_eat"))
                {
                    z.PresentationBodyBlendTicks = 20;
                }

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
            if (z.BungeeCarrierId != 0)
            {
                return 0f;
            }

            if (z.Kind == ZombieKind.Bungee && z.BungeeState != null)
            {
                return z.BungeeState.AnimationRate;
            }

            if (z.Kind == ZombieKind.Zomboni && z.ZomboniState?.FlatTires == true)
            {
                return z.ZomboniState.Rate;
            }

            var clip = AdventurePresentationZombieAnimation(z);
            if (AdventurePogoRules.IsBouncing(z))
            {
                return 40f;
            }

            if (z.Kind == ZombieKind.Digger)
            {
                var phase = AdventureZombieFeedbackPhase(z);
                if (phase == 32)
                {
                    return 12f;
                }

                if (phase == 34 || phase == 35 && z.DiggerState?.PhaseTicks > 30)
                {
                    return 0f;
                }

                if (phase == 33)
                {
                    return z.DiggerState?.PhaseTicks > 30 ? 20f : 12f;
                }

                if (phase == 35 || phase == 36)
                {
                    return 12f;
                }
            }

            if (z.Kind == ZombieKind.Ladder && z.PresentationNativePhase == 77)
            {
                return 24f;
            }

            if (z.Kind == ZombieKind.JackInTheBox && z.PresentationNativePhase == 16)
            {
                return 28f;
            }

            if (z.Kind == ZombieKind.Balloon && z.PresentationNativePhase == 74)
            {
                return 24f;
            }

            if (AdventureDancerRules.IsDancer(z))
            {
                var fixedRate = AdventureDancerRules.FixedRate(z);
                if (fixedRate >= 0f)
                {
                    return fixedRate;
                }

                if (AdventureDancerRules.GroupAffected(z, Zombies, false))
                {
                    return 0f;
                }
            }

            if (z.PresentationNewspaperGaspRemaining > 0f)
            {
                return 8f;
            }

            if (z.Kind == ZombieKind.PoleVaulter && z.PresentationNativePhase == 12)
            {
                return 24f;
            }

            if (z.Kind == ZombieKind.Snorkel)
            {
                if (z.PresentationNativePhase == 58)
                {
                    return 16f;
                }

                if (z.PresentationNativePhase == 60 || z.PresentationNativePhase == 62)
                {
                    return 24f;
                }

                if (z.PresentationNativePhase == 59)
                {
                    return 12f;
                }
            }

            if (z.Kind == ZombieKind.DolphinRider)
            {
                if (z.PresentationNativePhase == 52)
                {
                    return 16f;
                }

                if (z.PresentationNativePhase == 53)
                {
                    return 12f;
                }

                if (z.PresentationNativePhase == 54)
                {
                    return 10f;
                }
            }

            if (z.Eating)
            {
                return z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.Balloon || z.Kind == ZombieKind.Imp || z.Kind == ZombieKind.Digger || z.Kind == ZombieKind.JackInTheBox || z.Kind == ZombieKind.Snorkel || z.Kind == ZombieKind.Yeti ? 20f : 36f;
            }

            if (z.Kind == ZombieKind.Catapult)
            {
                return 5.5f;
            }

            if (z.Kind == ZombieKind.Bobsled && z.PresentationNativePhase == 18)
            {
                return 20f;
            }

            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility && z.PresentationNativePhase != 19)
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

            return data.GroundDistance > .000001f ? speed * 80f * .01f * 47f * data.Count / data.GroundDistance / (AdventureDancerRules.Scale(z)) : data.SourceRate;
        }

        /// <summary>Performs adventure presentation walking distance.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "dt">The dt.</param>
        /// <param name = "fallbackDistance">The fallback distance.</param>
        /// <returns>The resulting adventure presentation walking distance.</returns>
        private float AdventurePresentationWalkingDistance(Zombie z, float dt, float fallbackDistance)
        {
            if (dt <= 0f || z.PresentationNewspaperGaspRemaining > 0f || z.FrozenRemaining > 0f || z.PresentationButter > 0f || z.DropRemaining > 0f || z.Eating
                || AdventureDancerRules.IsDancer(z) && !AdventureDancerRules.CanWalk(z, Zombies))
            {
                return 0f;
            }

            if (AdventurePogoRules.IsBouncing(z))
            {
                // PC 546730 moves bouncing pogo zombies by mVelX, independently of their held body clip.
                return fallbackDistance;
            }

            if (z.Kind == ZombieKind.Balloon && z.Airborne)
            {
                // PC 546730 uses native velocity in flight and the pop's constant ground track while landing.
                return z.PresentationNativePhase == 74 ? 0f : z.Speed * dt * (z.ChillRemaining > 0f ? .4f : 1f);
            }

            if (z.Kind == ZombieKind.DolphinRider && z.PresentationNativePhase == 52)
            {
                // The authored entry ground is constant; do not replace its sixteen-fps animation clock with the source rate.
                return 0f;
            }

            if (z.Kind == ZombieKind.DolphinRider && z.PresentationNativePhase == 54)
            {
                // PC 546730 takes the native velocity directly during a dolphin jump, even while chilled.
                return z.Speed * dt;
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

            return data.VelocityAtPhase(z._presentationWalkPhase, rate * slow) * (AdventureDancerRules.Scale(z)) * (dt * 100f) / 80f;
        }

        /// <summary>Emits the original football running dust at each authored foot contact.</summary>
        /// <param name="z">The walking zombie whose animation clock was advanced this tick.</param>
        /// <param name="gridX">The zombie's horizontal grid position before this tick's movement.</param>
        private void RecordAdventureZombieFootsteps(Zombie z, float gridX)
        {
            if (z.Kind != ZombieKind.Football || z.Wave == -4 || z.PresentationButter > 0f)
            {
                return;
            }

            // PC UpdateZombieWalking 0x546730: these independent events use mX/mY, without mirroring or scaling.
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(gridX, AdventureDancerRules.Scale(z));
            var nativeY = (int)AdventureBiteNativeY(z, nativeX);
            var before = z._presentationPreviousWalkCycle;
            var after = z._presentationWalkLoops + (double)z._presentationWalkPhase;
            if (AdventureZombieBiteRules.Crossed(before, after, .03f))
            {
                AddAdventureZombieFootstep(z, nativeX + 81, nativeY + 106);
            }

            if (AdventureZombieBiteRules.Crossed(before, after, .61f))
            {
                AddAdventureZombieFootstep(z, nativeX + 87, nativeY + 110);
            }
        }

        /// <summary>Retains one independent twenty-tick Dust_Foot particle instance.</summary>
        /// <param name="z">The zombie whose row supplies the original render layer.</param>
        /// <param name="x">The unscaled original horizontal particle origin.</param>
        /// <param name="y">The unscaled original vertical particle origin.</param>
        private void AddAdventureZombieFootstep(Zombie z, int x, int y)
        {
            var id = ++Presentation._effectId;
            Presentation.FootstepEffects.Add(new AdventureZombieFootstepEffect
            {
                Id = id,
                Row = z.Row,
                Seed = unchecked(z.Id * 397 ^ id),
                X = x,
                Y = y
            });
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

            if (z.Kind == ZombieKind.Zomboni)
            {
                // PC TakeBodyDamage 0x54D080 requests ShieldHit on every unsuppressed body hit.
                RequestAdventureZombieFoley(46);
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
            if (z.Kind == ZombieKind.Zomboni)
            {
                var state = z.ZomboniState ??= new AdventureZomboniState();
                if (z.Health > 0f && state.SmokeStartAge < 0f
                    && AdventureZombieEquipmentRules.DamageIndex(z.Health, (int)z.StartingHealth) == 2)
                {
                    state.SmokeStartAge = z.Age;
                }

                return;
            }

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
                NativePhase = z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo ? z.PresentationNativePhase : -1,
                NativeAltitude = z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo ? z.PresentationAltitude : 0f,
                X = z.X,
                Row = z.Row,
                Scale = AdventureDancerRules.Scale(z),
                Asset = AdventureZombiePresentationAsset(z),
                Animation = clip,
                AnimationAge = z.PresentationAnimationAge,
                Rate = AdventurePresentationZombieRate(z),
                Anchor = global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ArmAnchor(kind),
                Image = global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ArmImage(kind),
                Mowed = phase == 3,
                Backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)),
                Chilled = z.ChillRemaining > 0f || z.FrozenRemaining > 0f,
                Hypnotized = z.Hypnotized
            });
        }

        /// <summary>Performs request adventure zombie foley.</summary>
        /// <param name = "id">The id.</param>
        /// <param name="pitch">The explicit original pitch adjustment in semitones.</param>
        /// <param name="presentationTick">The advancing cutscene tick when the battle elapsed clock is frozen.</param>
        private void RequestAdventureZombieFoley(int id, float pitch = 0f, int? presentationTick = null)
        {
            Presentation.ZombieSounds.Add(new global::PlantsVsZombies.Adventure.AdventureZombieSoundEvent
            {
                Id = ++Presentation._effectId, Foley = id, Tick = presentationTick ?? (int)System.Math.Round(Elapsed * 100f), Pitch = pitch
            });
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
                // PC 53DE56/53DED5 initializes every actor's vocal clock before native placement and speed draws.
                GroanTicks = 400 + _random.Next(101),
                Kind = kind,
                Row = row,
                X = x,
                Wave = wave,
                Health = health,
                StartingHealth = health,
                ArmorHealth = armor,
                ShieldHealth = shield,
                Airborne = kind == ZombieKind.Balloon,
                FlyingHealth = kind == ZombieKind.Balloon ? 20 : 0,
                PresentationNativeHeight = 0,
                Submerged = kind == ZombieKind.Digger,
                AbilityTimer = kind == ZombieKind.Dancing ? 4f : kind == ZombieKind.Bungee ? 4f : 5f
            };
            if (Adventure.Level.Mode == AdventureMode.LittleTrouble)
            {
                z.IsSmall = true;
                z.Health = (int)z.Health / 4;
                z.ArmorHealth = (int)z.ArmorHealth / 4;
                z.ShieldHealth = (int)z.ShieldHealth / 4;
                z.FlyingHealth /= 4;
                z.StartingHealth = z.Health;
            }

            z.PresentationMaxArmor = z.ArmorHealth;
            z.PresentationMaxShield = z.ShieldHealth;
            InitializeAdventureZombieNative(z, x >= 9.5f && kind != ZombieKind.Bungee);
            if (kind == ZombieKind.Bungee)
            {
                z.BungeeState = new AdventureBungeeState();
                z.PresentationAltitude = 3000f + _random.Next(151);
                z.PresentationNativeVelocity = 0f;
                z.Speed = 0f;
                z.PresentationNativePhase = 4;
                PickAdventureBungeeTarget(z);
            }

            if (kind == ZombieKind.JackInTheBox)
            {
                // PC 53E808..53E895: distance tickets, rare early pop, then integer division by velocity and doubling.
                var distance = 450 + _random.Next(300);
                if (_random.Next(20) == 0)
                {
                    distance /= 3;
                }

                z.JackInTheBoxTicks = Adventure.Level.Mode == AdventureMode.Vasebreaker
                    ? 10 : (int)(distance / (double)z.PresentationNativeVelocity) * 2;
                z.PresentationNativePhase = 15;
                z._presentationSpeedPhase = 15;
            }
            if (Adventure.Level.IsWaterRow(row) && (AdventureZombiePoolRules.UsesPlainBody(kind) || kind == ZombieKind.Balloon)
                || kind == ZombieKind.DuckyTube)
            {
                // PC LoadPlainZombieReanim 0x53FE40 enables tube artwork by the row, retaining the original kind and armor.
                z.PoolState = new AdventureZombiePoolState { HasDuckyTube = AdventureZombiePoolRules.UsesPlainBody(kind) };
                z.PresentationAltitude = kind == ZombieKind.Balloon ? 25f : 0f;
                z.PoolState.Altitude = z.PresentationAltitude;
                z.PresentationInPool = false;
                z.PresentationNativeHeight = 0;
            }

            Zombies.Add(z);
            if (kind == ZombieKind.DolphinRider)
            {
                RequestAdventureZombieFoley(51);
            }
            else if (kind == ZombieKind.Balloon)
            {
                RequestAdventureZombieFoley(60);
            }

            if (kind == ZombieKind.Zomboni)
            {
                z.ZomboniState = new AdventureZomboniState();
                RequestAdventureZombieFoley(16);
            }

            SpawnedZombies++;
            return z;
        }

        /// <summary>Preserves PC 0x419A60's one-in-five prefix, also inlined for the bungee carrier at 0x41ECCF.</summary>
        /// <param name="kind">The native species identifier.</param>
        /// <param name="row">The already selected zero-based row.</param>
        /// <param name="x">The requested grid position, replaced by native edge initialization when appropriate.</param>
        /// <param name="wave">The originating zero-based wave.</param>
        /// <returns>The initialized independent zombie.</returns>
        private Zombie SpawnAdventureBoardZombie(ZombieKind kind, int row, float x, int wave)
        {
            var variant = _random.Next(5) == 0;
            var zombie = SpawnAdventureZombie(kind, row, x, wave);
            zombie.NativeVariant = variant;
            return zombie;
        }

        /// <summary>Creates PC 0x41EBB0's temporary target grid for columns four through eight and five roof rows.</summary>
        /// <returns>The twenty-five target weights, initially ten thousand each.</returns>
        private static int[] CreateAdventureBungeeDropWeights()
        {
            var weights = new int[25];
            for (var index = 0; index < weights.Length; index++)
            {
                weights[index] = 10000;
            }

            return weights;
        }

        /// <summary>Selects PC 0x41EC80's weighted bungee target and leaves its small residual weight.</summary>
        /// <param name="weights">The original column-major twenty-five-cell target grid.</param>
        /// <returns>The target encoded as column times ten plus row.</returns>
        /// <exception cref="InvalidOperationException">The target grid has no selectable weight.</exception>
        private int PickAdventureBungeeDropCell(int[] weights)
        {
            var total = 0;
            foreach (var weight in weights)
            {
                total += weight;
            }

            var ticket = _random.Next(total);
            for (var index = 0; index < weights.Length; index++)
            {
                ticket -= weights[index];
                if (ticket < 0)
                {
                    weights[index] = 1;
                    return (index / 5 + 4) * 10 + index % 5;
                }
            }

            throw new InvalidOperationException("The native bungee target grid has no selectable cell.");
        }

        /// <summary>Performs drop adventure zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "wave">The wave.</param>
        private void DropAdventureZombie(ZombieKind kind, int row, int column, int wave)
        {
            // PC 0x41EC80 creates both actors before the bungee update checks Umbrella Leaf.
            var carrierRow = ChooseAdventureZombieRow(ZombieKind.Bungee);
            var carrier = SpawnAdventureBoardZombie(ZombieKind.Bungee, carrierRow, column + 0.5f, wave);
            var passengerRow = ChooseAdventureZombieRow(kind);
            var passenger = SpawnAdventureBoardZombie(kind, passengerRow, column + 0.5f, wave);
            passenger.Row = row;
            passenger.DropRemaining = float.MaxValue;
            passenger.BungeeCarrierId = carrier.Id;
            passenger.PresentationNativeHeight = 9;
            passenger.X = AdventureZombieNativeRules.GridX(25f + column * 80f, AdventureDancerRules.Scale(passenger));
            passenger.PresentationPreviousX = passenger.X;
            carrier.BungeeCarrier = true;
            carrier.Row = row;
            carrier.TargetColumn = column;
            carrier.X = AdventureZombieNativeRules.GridX(40f + column * 80f, AdventureDancerRules.Scale(carrier));
            carrier.PresentationPreviousX = carrier.X;
            carrier.BungeeState!.CarriesZombie = true;
            carrier.BungeeState.AnimationSeconds = 0f;
            carrier.LinkedZombieId = passenger.Id;
            Adventure.Notice(AdventureNoticeKind.RoofDrop, (int)kind, row, column);
        }

        /// <summary>Reproduces PC 540180's weighted cell selection and reservation, including protection of the last sun producer.</summary>
        /// <param name="zombie">The bungee selecting its initial target.</param>
        /// <param name="column">A boss-constrained column, or minus one for the complete lawn.</param>
        private void PickAdventureBungeeTarget(Zombie zombie, int column = -1)
        {
            var producers = 0;
            foreach (var plant in Plants)
            {
                if (plant.Health > 0f && plant.BungeeState != 2 && BungeeSunProducer(plant.Kind))
                {
                    producers++;
                }
            }

            var reservedProducers = 0;
            foreach (var other in Zombies)
            {
                if (other.Kind == ZombieKind.Bungee && other.Health > 0f && other.TargetColumn >= 0)
                {
                    var target = AdventureAirAttackPlantAt(other.Row, other.TargetColumn);
                    if (target != null && BungeeSunProducer(target.Kind))
                    {
                        reservedProducers++;
                    }
                }
            }

            var protectProducer = reservedProducers == producers - 1;
            var total = 0;
            for (var x = 0; x < 9; x++)
            {
                for (var row = 0; row < 6; row++)
                {
                    var index = x * 6 + row;
                    _bungeeTargetWeights[index] = 0;
                    if (!Adventure.Level.IsLawnRow(row) || column >= 0 && x != column)
                    {
                        continue;
                    }

                    var graveAtCell = false;
                    foreach (var grave in Adventure._graves)
                    {
                        if (grave.Row == row && grave.Column == x)
                        {
                            graveAtCell = true;
                            break;
                        }
                    }

                    if (graveAtCell)
                    {
                        continue;
                    }

                    var plant = AdventureAirAttackPlantAt(row, x);
                    if (plant != null && (plant.Kind == PlantKind.GraveBuster || plant.Kind == PlantKind.CobCannon
                        || protectProducer && BungeeSunProducer(plant.Kind)))
                    {
                        continue;
                    }

                    var reserved = false;
                    foreach (var other in Zombies)
                    {
                        if (other.Kind == ZombieKind.Bungee && other.Health > 0f
                            && other.Row == row && other.TargetColumn == x)
                        {
                            reserved = true;
                            break;
                        }
                    }

                    if (!reserved)
                    {
                        _bungeeTargetWeights[index] = plant == null ? 1 : 10000;
                        total += _bungeeTargetWeights[index];
                    }
                }
            }

            if (total == 0)
            {
                zombie.Health = 0f;
                zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                return;
            }

            var draw = _random.Next(total);
            for (var index = 0; index < _bungeeTargetWeights.Length; index++)
            {
                draw -= _bungeeTargetWeights[index];
                if (draw < 0)
                {
                    zombie.Row = index % 6;
                    zombie.TargetColumn = index / 6;
                    zombie.X = AdventureZombieNativeRules.GridX(40f + zombie.TargetColumn * 80f, AdventureDancerRules.Scale(zombie));
                    zombie.PresentationPreviousX = zombie.X;
                    return;
                }
            }
        }

        /// <summary>Reuses the native column-major nine-by-six target weights without per-spawn allocation.</summary>
        private readonly int[] _bungeeTargetWeights = new int[54];

        /// <summary>Checks the original MakesSun plant types used by bungee target selection.</summary>
        /// <param name="kind">The candidate original plant type.</param>
        /// <returns>Whether the plant produces sun.</returns>
        private static bool BungeeSunProducer(PlantKind kind)
        {
            return kind == PlantKind.Sunflower || kind == PlantKind.TwinSunflower || kind == PlantKind.SunShroom;
        }

        /// <summary>Advances PC 540C20/540980's original bungee altitude, phase counters and capture lifecycle.</summary>
        /// <param name="zombie">The live bungee after immobilization checks.</param>
        private void AdvanceAdventureBungee(Zombie zombie)
        {
            if (zombie.Health <= 0f)
            {
                zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                KillAdventureZombie(zombie);
                return;
            }

            var state = zombie.BungeeState ??= new AdventureBungeeState();
            if (state.TargetPlant != null && !Plants.Contains(state.TargetPlant))
            {
                state.TargetPlant = null;
            }

            if (state.PhaseTicks > 0)
            {
                state.PhaseTicks--;
            }

            if (state.Phase == 4 || state.Phase == 5)
            {
                var previous = zombie.PresentationAltitude;
                zombie.PresentationAltitude -= 8f;
                if (previous > 2596f && zombie.PresentationAltitude <= 2596f && zombie.LinkedZombieId == 0)
                {
                    RequestAdventureZombieFoley(74);
                }

                if (state.Phase == 4 && zombie.PresentationAltitude < 1500f && Adventure.Level.Mode != AdventureMode.Boss)
                {
                    state.Phase = 5;
                    RequestAdventureZombieFoley(44);
                }

                if (zombie.PresentationAltitude <= 40f)
                {
                    Plant? umbrella = null;
                    foreach (var plant in Plants)
                    {
                        if (plant.Kind == PlantKind.UmbrellaLeaf && plant.Health > 0f && plant.BungeeState != 2
                            && Math.Abs(plant.Row - zombie.Row) <= 1 && Math.Abs(plant.Column - zombie.TargetColumn) <= 1)
                        {
                            umbrella = plant;
                            break;
                        }
                    }

                    if (umbrella != null)
                    {
                        RequestAdventureZombieFoley(-6);
                        RequestAdventureZombieFoley(73);
                        if (umbrella.PresentationUmbrellaAge < 0f)
                        {
                            umbrella.PresentationUmbrellaAge = 0f;
                        }

                        state.HitUmbrella = true;
                        state.Phase = 8;
                    }
                    else if (zombie.PresentationAltitude <= 0f)
                    {
                        zombie.PresentationAltitude = 0f;
                        Zombie? passenger = null;
                        foreach (var other in Zombies)
                        {
                            if (other.Id == zombie.LinkedZombieId)
                            {
                                passenger = other;
                                break;
                            }
                        }

                        if (passenger != null)
                        {
                            passenger.BungeeCarrierId = 0;
                            passenger.DropRemaining = 0f;
                            passenger.PresentationNativeHeight = 0;
                            passenger.PresentationAnimation = string.Empty;
                            AdventurePresentationZombieAnimation(passenger);
                            zombie.LinkedZombieId = 0;
                            state.CarriesZombie = false;
                            state.Enter(8);
                        }
                        else
                        {
                            state.Enter(6, 300, 5);
                        }
                    }
                }
            }
            else if (state.Phase == 6 && state.PhaseTicks == 0)
            {
                state.Enter(7, 0, 20);
                var target = AdventureAirAttackPlantAt(zombie.Row, zombie.TargetColumn);
                if (target != null && target.Kind != PlantKind.CobCannon && target.Kind != PlantKind.GraveBuster
                    && !target.PresentationBiteNotOnGround)
                {
                    state.TargetPlant = target;
                    target.BungeeState = 1;
                }
            }
            else if (state.Phase == 7 && state.AnimationSeconds * 24f >= 15f)
            {
                var target = state.TargetPlant;
                if (target != null)
                {
                    foreach (var other in Zombies)
                    {
                        if (other != zombie && other.BungeeState?.TargetPlant == target)
                        {
                            other.BungeeState.TargetPlant = null;
                        }
                    }

                    target.BungeeState = 2;
                    RequestAdventureZombieFoley(68);
                    if (target.Kind == PlantKind.Cattail && AdventurePlantAt(target.Row, target.Column, PlantLayer.Armor) != null)
                    {
                        var previous = Presentation._nativePlantingEffects;
                        Presentation._nativePlantingEffects = true;
                        try
                        {
                            PlaceAdventurePlant(PlantKind.LilyPad, target.Row, target.Column);
                        }
                        finally
                        {
                            Presentation._nativePlantingEffects = previous;
                        }
                    }
                }

                state.Enter(8);
            }
            else if (state.Phase == 8)
            {
                zombie.PresentationAltitude += 8f;
                if (zombie.PresentationAltitude >= 600f)
                {
                    zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                    KillAdventureZombie(zombie);
                    return;
                }
            }

            zombie.PresentationNativePhase = state.Phase;
            zombie.PresentationAnimation = state.Animation;
            zombie.PresentationAnimationAge = state.AnimationSeconds;
        }

        /// <summary>Restores a grabbed plant on interruption, removes lifted cargo, and releases no suspended zombie on carrier death.</summary>
        /// <param name="zombie">The departing or dying bungee whose ownership must be cleared.</param>
        private void ReleaseAdventureBungeeCargo(Zombie zombie)
        {
            var state = zombie.BungeeState;
            var plant = state?.TargetPlant;
            if (plant != null && Plants.Contains(plant))
            {
                if (plant.BungeeState == 2)
                {
                    RemoveAdventurePlantAndStack(plant);
                }
                else
                {
                    plant.BungeeState = 0;
                }
            }

            if (state != null)
            {
                state.TargetPlant = null;
            }

            foreach (var passenger in new PooledSnapshot<Zombie>(Zombies))
            {
                if (zombie.LinkedZombieId != 0 && passenger.Id == zombie.LinkedZombieId
                    || zombie.Id != 0 && passenger.BungeeCarrierId == zombie.Id)
                {
                    passenger.BungeeCarrierId = 0;
                    passenger.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                    KillAdventureZombie(passenger);
                }
            }

            zombie.LinkedZombieId = 0;
        }

        /// <summary>Restarts a native one-shot phase, including transitions that reuse the same clip.</summary>
        /// <param name="z">The zombie changing phase.</param>
        /// <param name="phase">The original PC phase identifier.</param>
        private void SetAdventureZombieMotionPhase(Zombie z, int phase)
        {
            z.PresentationNativePhase = phase;
            if (z.Kind == ZombieKind.PoleVaulter)
            {
                z.PresentationPoleVaultBlendTicks = phase == 13 ? 0 : 20;
            }

            z.PresentationAnimation = string.Empty;
            AdventurePresentationZombieAnimation(z);
            RefreshAdventureZombieNativeSpeed(z);
        }

        /// <summary>Starts the authored vault without teleporting the zombie across its target.</summary>
        /// <param name="z">The pole vaulter beginning its jump.</param>
        /// <param name="target">The original vault target, including a possible Tall-nut.</param>
        private void BeginAdventurePoleVault(Zombie z, Plant target)
        {
            SetAdventureZombieMotionPhase(z, 12);
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var distance = Adventure.Level.Mode == AdventureMode.Bowling ? 0 : nativeX - (40 + target.Column * 80) - 80;
            // Original anim_jump contains 43 frames and runs at 24 fps.
            z._poleVaultVelocity = distance / (43f / 24f * 100f);
            z.UsedAbility = true;
            z.Eating = false;
        }

        /// <summary>Moves a pole vault and dispatches its original normalized animation events.</summary>
        /// <param name="z">The zombie whose vault may be active.</param>
        /// <param name="dt">The fixed simulation interval in seconds.</param>
        /// <returns>Whether this tick is consumed by the vault.</returns>
        private bool AdvanceAdventurePoleVault(Zombie z, float dt)
        {
            if (z.Kind != ZombieKind.PoleVaulter || z.PresentationNativePhase != 12)
            {
                return false;
            }

            var before = z._presentationPreviousWalkCycle;
            var progress = z._presentationWalkLoops + (double)z._presentationWalkPhase;
            if (AdventureZombieBiteRules.Crossed(before, progress, .2f))
            {
                RequestAdventureZombieFoley(74);
            }

            if (AdventureZombieBiteRules.Crossed(before, progress, .4f))
            {
                RequestAdventureZombieFoley(78);
            }

            if (progress > .6f && progress <= .7f)
            {
                var barrier = FindAdventureNativePlantTarget(z, AdventureBiteAttack.Vault);
                if (barrier != null && barrier.Kind == PlantKind.TallNut)
                {
                    RequestAdventureZombieFoley(48);
                    Presentation.PlantBiteEffects.Add(new AdventurePlantBiteEffect
                    {
                        Id = ++Presentation._effectId, Row = z.Row, ParticleId = 29,
                        NativeX = 40 + barrier.Column * 80 + 60,
                        NativeY = 80 + barrier.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85 : 100) - 20,
                        Seed = z.Id
                    });
                    z.X = AdventureZombieNativeRules.GridX(40 + barrier.Column * 80, AdventureDancerRules.Scale(z));
                    // PC 0x541A98 changes mPosY, not mAltitude; the next position update restores the row floor.
                    z.PresentationVaultPositionY = -30f;
                    z.PresentationNativeHeight = 7;
                    SetAdventureZombieMotionPhase(z, 13);
                    return true;
                }
            }

            if (progress >= 1d)
            {
                // The native jump artwork has moved 150 pixels relative to the walking origin.
                z.X -= 150f / 80f;
                // The authored jump displacement becomes the walking origin, including its interpolation history.
                z.PresentationPreviousX -= 150f / 80f;
                z.PresentationVaultPositionY = 0f;
                SetAdventureZombieMotionPhase(z, 13);
                return true;
            }

            // PC vault velocity ignores chill; the authored reanimation clock still slows down.
            z.X -= z._poleVaultVelocity * (dt * 100f) / 80f;
            if (Adventure.Level.HasRoof)
            {
                var nativeX = AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
                z.PresentationVaultPositionY = AdventureZombieMotionRules.PoleVaultRoofOffset(nativeX, (float)progress);
            }

            return true;
        }

        /// <summary>Changes a dolphin clip while preserving the native velocity unless the original branch picks a new speed.</summary>
        /// <param name="z">The dolphin rider changing its original phase.</param>
        /// <param name="phase">The original phase identifier from fifty one through fifty six.</param>
        private void SetAdventureDolphinPhase(Zombie z, int phase)
        {
            z.PresentationNativePhase = phase;
            var state = z.DolphinState ??= new AdventureDolphinState();
            state.AnimationBlendTicks = phase == 52 ? 20 : 0;
            if (phase == 55 || phase == 56)
            {
                // PC StartWalkAnim(0) replaces the jump/swim pose before any same-tick meal blend.
                state.WalkingPoseVersion++;
                z.PresentationPreviousX = z.X;
            }

            z.PresentationAnimation = string.Empty;
            AdventurePresentationZombieAnimation(z);
            if (phase == 51 || phase == 55 || phase == 56)
            {
                PickAdventureZombieNativeSpeed(z, phase);
            }
            else
            {
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = (int)z.Kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
            }
        }

        /// <summary>Checks PC ShouldTriggerTimedEvent's inclusive previous and exclusive current one-shot interval.</summary>
        /// <param name="z">The actor whose native animation was advanced this update.</param>
        /// <param name="progress">The updated normalized one-shot animation time.</param>
        /// <param name="marker">The original normalized event marker.</param>
        /// <returns>Whether the original event falls in this update's animation interval.</returns>
        private static bool AdventureDolphinTimedEvent(Zombie z, double progress, float marker)
        {
            return z._presentationPreviousWalkCycle <= marker && progress > marker;
        }

        /// <summary>Runs PC UpdateZombieDolphinRider's bank, entry, ride, jump, and dismounted swimming branches.</summary>
        /// <param name="z">The actor whose original dolphin phase may be active.</param>
        /// <param name="dt">The active fixed update interval in seconds.</param>
        /// <returns>False because the common walking update still runs after the original dolphin action.</returns>
        private bool AdvanceAdventureDolphin(Zombie z, float dt)
        {
            if (z.Kind != ZombieKind.DolphinRider)
            {
                return false;
            }

            var state = z.DolphinState ??= new AdventureDolphinState();
            var phase = z.PresentationNativePhase;
            if (phase == 54 && state.JumpTicks > 0)
            {
                state.JumpTicks--;
            }

            if (z.TangleKelpGrab != null || z.PresentationNativeHeight == 3)
            {
                // PC 541C70 returns before changing a kelp target's existing dolphin phase.
                return false;
            }

            var backwards = z.PresentationWalkingBackwards ?? z.Hypnotized;
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var progress = z._presentationWalkLoops + (double)z._presentationWalkPhase;
            if (phase == 51 && !backwards && nativeX > 700 && nativeX <= 720)
            {
                SetAdventureDolphinPhase(z, 52);
            }
            else if (phase == 52)
            {
                if (AdventureDolphinTimedEvent(z, progress, .56f))
                {
                    AddAdventureZombieSplash(z, -83f, 73f, 1.2f, .8f, 53);
                }

                if (z._presentationWalkLoops > 0)
                {
                    z.X -= 70f / 80f;
                    z.PresentationPreviousX -= 70f / 80f;
                    z.PresentationInPool = true;
                    z.PresentationAltitude = 0f;
                    SetAdventureDolphinPhase(z, 53);
                }
            }
            else if (phase == 53)
            {
                if (nativeX <= 10)
                {
                    z.PresentationNativeHeight = 2;
                    z.PresentationAltitude = -40f;
                    SetAdventureDolphinPhase(z, 51);
                    AddAdventureZombieSplash(z, 23f, 78f, .8f, .8f, 52, 65f);
                }
                else if (!z.Headless)
                {
                    var target = FindAdventureNativePlantTarget(z, AdventureBiteAttack.Vault);
                    if (target != null)
                    {
                        RequestAdventureZombieFoley(50);
                        RequestAdventureZombieFoley(52);
                        state.JumpTicks = 120;
                        z.Eating = false;
                        z.PresentationNativeVelocity = .5f;
                        SetAdventureDolphinPhase(z, 54);
                    }
                }
            }
            else if (phase == 54)
            {
                z.PresentationAltitude = Math.Max(0, Math.Min(120, 120 - state.JumpTicks)) / 12f;
                var complete = false;
                if (AdventureDolphinTimedEvent(z, progress, .3f))
                {
                    var barrier = FindAdventureNativePlantTarget(z, AdventureBiteAttack.Vault);
                    if (barrier != null && barrier.Kind == PlantKind.TallNut)
                    {
                        RequestAdventureZombieFoley(48);
                        Presentation.PlantBiteEffects.Add(new AdventurePlantBiteEffect
                        {
                            Id = ++Presentation._effectId, Row = z.Row, ParticleId = 29,
                            NativeX = 40 + barrier.Column * 80 + 60,
                            NativeY = 80 + barrier.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85 : 100) - 20,
                            Seed = z.Id
                        });
                        z.X = AdventureZombieNativeRules.GridX(40 + barrier.Column * 80 + 25, AdventureDancerRules.Scale(z));
                        z.PresentationAltitude = 30f;
                        z.PresentationNativeHeight = 7;
                        complete = true;
                    }
                }
                else if (AdventureDolphinTimedEvent(z, progress, .49f))
                {
                    AddAdventureZombieSplash(z, -63f, 73f, 1.2f, .8f, 53);
                    z.PresentationNativeVelocity = 0f;
                    z.Speed = 0f;
                }
                else if (z._presentationWalkLoops > 0)
                {
                    z.X -= 94f / 80f;
                    z.PresentationAltitude = 0f;
                    complete = true;
                }

                if (complete)
                {
                    z.UsedAbility = true;
                    SetAdventureDolphinPhase(z, 55);
                }
            }
            else if (phase == 55 && (nativeX <= 10 && !backwards || nativeX > 680 && backwards))
            {
                z.PresentationNativeHeight = 2;
                z.PresentationAltitude = -40f;
                SetAdventureDolphinPhase(z, 56);
                AddAdventureZombieSplash(z, 23f, 78f, .8f, .8f, 52, 65f);
            }

            return false;
        }

        /// <summary>Runs the original snorkel walk, jump, swim, rise, eat, and reverse-dive phases.</summary>
        /// <param name="z">The zombie whose snorkel phase may be active.</param>
        /// <param name="dt">The fixed simulation interval in seconds.</param>
        /// <returns>Whether the jump phase consumes movement and eating this tick.</returns>
        private bool AdvanceAdventureSnorkel(Zombie z, float dt)
        {
            if (z.Kind != ZombieKind.Snorkel)
            {
                return false;
            }

            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var phase = z.PresentationNativePhase;
            if (phase == 57 && !z.Hypnotized && nativeX > 700 && nativeX <= 720)
            {
                SetAdventureZombieMotionPhase(z, 58);
                return true;
            }

            if (phase == 58)
            {
                var progress = z._presentationWalkLoops + (double)z._presentationWalkPhase;
                z.PresentationAltitude = Math.Min(1000, (int)(progress * 1000d)) * .01f;
                if (AdventureZombieBiteRules.Crossed(z._presentationPreviousWalkCycle, progress, .83f))
                {
                    AddAdventureZombieSplash(z, -47f, 73f, 1.2f, .8f, 53);
                }

                if (progress >= 1d)
                {
                    z.PresentationInPool = true;
                    SetAdventureZombieMotionPhase(z, 59);
                    // PC jump completion retains the 0.2 entry velocity until a later PickRandomSpeed.
                    z.PresentationNativeVelocity = .2f;
                    z.Speed = AdventureZombieNativeRules.GridSpeed(.2f);
                }
                else
                {
                    z.X -= .2f * (dt * 100f) / 80f;
                }

                z.Submerged = z.PresentationInPool == true && !z.Eating;
                return true;
            }

            if (phase == 59)
            {
                if (z.Headless)
                {
                    // PC UpdateDamageStates removes a headless submerged snorkel without a death reanimation.
                    z.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                    KillAdventureZombie(z);
                    return true;
                }

                if (!z.Headless && (nativeX <= 25 && !z.Hypnotized || nativeX > 640 && z.Hypnotized))
                {
                    z.PresentationNativeHeight = 2;
                    z.PresentationAltitude = -90f;
                    z.X += (z.Hypnotized ? 15f : -15f) / 80f;
                    SetAdventureZombieMotionPhase(z, 57);
                    AddAdventureZombieSplash(z, 23f, 78f, .8f, .8f, 52);
                }
                else if (z.Eating)
                {
                    SetAdventureZombieMotionPhase(z, 60);
                }
            }
            else if (phase == 60 || phase == 61)
            {
                if (!z.Eating)
                {
                    SetAdventureZombieMotionPhase(z, 62);
                }
                else if (phase == 60 && z._presentationWalkLoops > 0)
                {
                    SetAdventureZombieMotionPhase(z, 61);
                }
            }
            else if (phase == 62 && z._presentationWalkLoops > 0)
            {
                SetAdventureZombieMotionPhase(z, 59);
            }

            z.Submerged = z.PresentationInPool == true && !z.Eating;
            return false;
        }

        /// <summary>Updates native pool entry, exit, altitude, and attack-rectangle clipping.</summary>
        /// <param name="z">The zombie whose water contact is being checked.</param>
        /// <param name="dt">The fixed simulation interval in seconds.</param>
        private void AdvanceAdventureZombiePool(Zombie z, float dt)
        {
            if (z.PoolState != null)
            {
                AdvanceAdventurePlainZombiePool(z);
                return;
            }

            var scale = AdventureDancerRules.Scale(z);
            if (z.PresentationNativeHeight == 3)
            {
                // PC UpdateZombiePool 0x5451F9 lowers a dragged body by one pixel per update.
                z.PresentationAltitude -= z.Kind == ZombieKind.DolphinRider ? 1f : dt * 100f;
            }
            else if (z.PresentationNativeHeight == 7 && z.Kind != ZombieKind.Pogo)
            {
                z.PresentationAltitude = Math.Max(0f, z.PresentationAltitude - (z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Balloon ? 1f : dt * 100f));
                if (z.PresentationAltitude == 0f)
                {
                    z.PresentationNativeHeight = 0;
                    z.PresentationVaultPositionY = 0f;
                }
            }
            else if (z.PresentationNativeHeight == 1 || z.PresentationNativeHeight == 2)
            {
                var entering = z.PresentationNativeHeight == 1;
                z.PresentationAltitude += z.Kind == ZombieKind.DolphinRider ? entering ? -1f : 1f
                    : (entering ? -1f : z.Kind == ZombieKind.Snorkel ? 2f : 1f) * dt * 100f;
                var destination = entering ? -40f * scale : 0f;
                if (entering ? z.PresentationAltitude <= destination : z.PresentationAltitude >= destination)
                {
                    z.PresentationAltitude = destination;
                    z.PresentationNativeHeight = 0;
                    z.PresentationInPool = entering;
                }
            }
            else if (z.Kind != ZombieKind.Snorkel && z.Kind != ZombieKind.DolphinRider && !z.Airborne
                && (z.Kind == ZombieKind.Normal || z.Kind == ZombieKind.Conehead || z.Kind == ZombieKind.Buckethead
                    || z.Kind == ZombieKind.Flag || z.Kind == ZombieKind.DuckyTube || z.Kind == ZombieKind.Balloon))
            {
                var nativeX = AdventureZombieNativeRules.NativePositionX(z.X, scale);
                var pool = Adventure.Level.IsWaterRow(z.Row) && AdventureZombieMotionRules.IsPoolPosition(nativeX);
                if (pool != (z.PresentationInPool == true))
                {
                    z.PresentationNativeHeight = pool ? 1 : 2;
                    if (pool)
                    {
                        z.PresentationInPool = true;
                    }

                    AddAdventureZombieSplash(z, 23f, 78f, .8f, .8f, pool ? 19 : 52);
                }
            }

            z.PresentationBiteBodyY = (int)-z.PresentationAltitude;
            z.PresentationBiteClipHeight = (int)AdventureZombieMotionRules.ClipHeight(z, scale);
        }

        /// <summary>Emits the original Splash reanimation, PlantingPool particles, and water Foley.</summary>
        /// <param name="z">The zombie entering or leaving the pool.</param>
        /// <param name="offsetX">The native horizontal splash offset.</param>
        /// <param name="offsetY">The native vertical splash offset.</param>
        /// <param name="scaleX">The authored horizontal splash scale.</param>
        /// <param name="scaleY">The authored vertical splash scale.</param>
        /// <param name="foley">The original water Foley identifier.</param>
        /// <param name="particleOffsetY">The optional original particle offset when PoolSplash uses a distinct vertical origin.</param>
        private void AddAdventureZombieSplash(Zombie z, float offsetX, float offsetY, float scaleX, float scaleY, int foley, float? particleOffsetY = null)
        {
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var nativeY = (int)AdventureBiteNativeY(z, nativeX);
            Presentation.ZombieSplashes.Add(new AdventureZombieSplash
            {
                Id = ++Presentation._effectId, Row = z.Row,
                X = nativeX + offsetX,
                Y = nativeY + offsetY,
                ParticleX = nativeX + offsetX + 37f, ParticleY = nativeY + (particleOffsetY ?? offsetY + 42f),
                ScaleX = scaleX, ScaleY = scaleY
            });
            RequestAdventureZombieFoley(foley);
        }

        /// <summary>Updates the original row countdowns before vehicles extend the ice during their action update.</summary>
        private void AdvanceAdventureIceTrails()
        {
            for (var index = Adventure._ice.Count - 1; index >= 0; index--)
            {
                var trail = Adventure._ice[index];
                trail.RemainingTicks--;
                trail.AgeTicks++;
                if (trail.RemainingTicks <= 0)
                {
                    Adventure._ice.RemoveAt(index);
                }
            }
        }

        /// <summary>Extends the shared native ice row rather than generating independently expiring cell stamps.</summary>
        /// <param name="zombie">The vehicle advancing its original ice front.</param>
        private void ExtendAdventureIceTrail(Zombie zombie)
        {
            var x = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var permanent = ModeSession != null && ModeSession.Definition.Mechanic == ModeMechanic.Bobsled;
            foreach (var trail in Adventure._ice)
            {
                if (trail.Row == zombie.Row)
                {
                    trail.Extend(x, Adventure.Level.HasRoof, permanent);
                    return;
                }
            }

            if ((int)(x + 118f) >= 800)
            {
                return;
            }

            var created = new AdventureIceTrail { Row = zombie.Row };
            created.Extend(x, Adventure.Level.HasRoof, permanent);
            if (created.RemainingTicks > 0)
            {
                created.Id = ++Presentation._effectId;
                Adventure._ice.Add(created);
            }
        }

        /// <summary>Advances the original running and stopped box-opening phases without chilling the phase clock.</summary>
        /// <param name="zombie">The live zombie whose native action is being updated.</param>
        /// <returns>Whether box opening or detonation consumes this zombie's movement update.</returns>
        private bool AdvanceAdventureJackInTheBox(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.JackInTheBox || zombie.PresentationNativePhase != 15 && zombie.PresentationNativePhase != 16)
            {
                return false;
            }

            if (zombie.JackInTheBoxTicks > 0)
            {
                zombie.JackInTheBoxTicks--;
            }

            // PC UpdateZombieJackInTheBox 542560: only a headed running zombie can start opening its box.
            if (zombie.PresentationNativePhase == 15)
            {
                if (zombie.JackInTheBoxTicks <= 0 && !zombie.Headless)
                {
                    zombie.PresentationNativePhase = 16;
                    zombie._presentationSpeedPhase = 16;
                    zombie.JackInTheBoxTicks = 110;
                    zombie.Eating = false;
                    RequestAdventureZombieFoley(-6);
                    AdventurePresentationZombieAnimation(zombie);
                    return true;
                }

                return false;
            }

            if (zombie.JackInTheBoxTicks == 80)
            {
                RequestAdventureZombieFoley(34);
            }

            if (zombie.JackInTheBoxTicks <= 0)
            {
                var x = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie)) + 60;
                var y = (int)AdventureBiteNativeY(zombie, x - 60) + 60;
                BlastAdventureRadius(x, y, zombie.Row, 115, 1, zombie.Hypnotized ? 0x7FU : 0xFFU);
                if (!zombie.Hypnotized)
                {
                    foreach (var plant in new PooledSnapshot<Plant>(Plants))
                    {
                        var rect = AdventureZombieBiteRules.PlantRect((int)plant.Kind, 40 + plant.Column * 80,
                            AdventureSquashCellY(plant.Row, plant.Column), plant.Kind == PlantKind.Pumpkin ? 120 : 80);
                        var dx = Math.Max(0, Math.Max(rect.X - x, x - rect.X - rect.Width));
                        var dy = Math.Max(0, Math.Max(rect.Y - y, y - rect.Y - rect.Height));
                        if (dx * dx + dy * dy <= 90 * 90)
                        {
                            RemoveAdventurePlantAndStack(plant);
                        }
                    }
                }

                Presentation.Explosions.Add(new AdventureExplosion
                {
                    Id = ++Presentation._effectId, Seed = _random.Next(), Row = zombie.Row,
                    NativeX = x, NativeY = y, Effect = AdventureExplosionEffect.JackExplode
                });
                Presentation.ScreenShake.Start(4, -6);
                RequestAdventureZombieFoley(23);
                zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                KillAdventureZombie(zombie);
                OpenVasesInJackExplosion(x, y);
            }

            return true;
        }

        /// <summary>Opens the original three-by-three vase grid after a Jack-in-the-Box explosion.</summary>
        /// <param name="x">The same integer board-pixel center used by the zombie's native explosion.</param>
        /// <param name="y">The same integer vertical center used by the native explosion.</param>
        private void OpenVasesInJackExplosion(int x, int y)
        {
            if (Adventure.Level.Mode != AdventureMode.Vasebreaker)
            {
                return;
            }

            // PC ScaryPotterJackExplode 0x435050 uses clamped grid coordinates, not the plant blast radius.
            var column = x < 40 ? -1 : Math.Min(8, (x - 40) / 80);
            var row = column < 0 || y < 80 ? -1 : Math.Min(4, (y - 80) / 100);
            for (var index = 0; index < Adventure._vases.Count;)
            {
                var vase = Adventure._vases[index];
                if (Math.Abs(vase.Column - column) > 1 || Math.Abs(vase.Row - row) > 1)
                {
                    index++;
                    continue;
                }

                Adventure._vases.RemoveAt(index);
                if (ModeSession == null)
                {
                    OpenAdventureVase(vase);
                }
                else
                {
                    OpenModeVase(vase);
                }

                Presentation.VaseBreakSoundSerial++;
            }
        }

        /// <summary>Marks the original live flying targets of Plant::BlowAwayFliers without removing their visible bodies.</summary>
        private void BlowAdventureFliersAway()
        {
            foreach (var zombie in Zombies)
            {
                if (zombie.Health > 0f && zombie.Airborne && zombie.PresentationNativePhase != 74
                    && !(zombie.PresentationNativePhase >= 1 && zombie.PresentationNativePhase <= 3))
                {
                    zombie.BlowingAway = true;
                }
            }
        }

        /// <summary>Applies PC 546A20's original ten-pixel wind displacement and integer-position exit check.</summary>
        /// <param name="zombie">The living actor whose original BlowingAway flag may be set.</param>
        /// <returns>Whether the native right-edge exit removed the actor this update.</returns>
        private bool AdvanceAdventureBalloonWind(Zombie zombie)
        {
            if (!zombie.BlowingAway)
            {
                return false;
            }

            // PC checks the previous integer mX, then adds ten to float mPosX independently of frozen walking.
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            zombie.X += 10f / 80f;
            if (nativeX <= 850)
            {
                return false;
            }

            // The original endpoint is DieWithLoot (54C060), with immediate body removal and one ordinary loot roll.
            DropAdventureLoot(zombie);
            zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
            KillAdventureZombie(zombie);
            return true;
        }

        /// <summary>Runs PC LandFlyer's original pop transition and immediate pool-row removal.</summary>
        /// <param name="zombie">The airborne balloon whose attachment health has reached zero.</param>
        /// <param name="damageFlags">The original damage flags, including suppression of pop artwork and sound.</param>
        private void LandAdventureBalloon(Zombie zombie, uint damageFlags)
        {
            if (zombie.Kind != ZombieKind.Balloon)
            {
                return;
            }

            if ((damageFlags & 16U) == 0 && zombie.PresentationNativePhase == 73)
            {
                // PC 5415F0 selects the original twenty-eight-frame pop at 24 fps, with a twenty-update blend.
                RequestAdventureZombieFoley(22);
                SetAdventureZombieMotionPhase(zombie, 74);
                zombie.PresentationBalloonBlendTicks = 20;
            }

            if (Adventure.Level.IsWaterRow(zombie.Row))
            {
                // PC removes a popped balloon immediately on any pool row, retaining its one original loot roll.
                DropAdventureLoot(zombie);
                zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                KillAdventureZombie(zombie);
                return;
            }

            zombie.PresentationNativeHeight = 7;
        }

        /// <summary>Completes PC UpdateZombieFlyer's pop before releasing the original walking body.</summary>
        /// <param name="zombie">The balloon whose once-and-hold pop clock may have completed.</param>
        private void AdvanceAdventureBalloon(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.Balloon || zombie.PresentationNativePhase != 74 || zombie._presentationWalkLoops <= 0)
            {
                return;
            }

            zombie.Airborne = false;
            zombie.UsedAbility = true;
            SetAdventureZombieMotionPhase(zombie, 75);
            zombie.PresentationBalloonBlendTicks = 0;
        }

        /// <summary>Updates PC 543AD0's sled push, authored boarding, ice damage, and crash detachment.</summary>
        /// <param name="zombie">The member whose independent native phase counter advances this update.</param>
        private void AdvanceAdventureBobsled(Zombie zombie)
        {
            var state = zombie.BobsledState;
            if (state == null || zombie.UsedAbility)
            {
                return;
            }

            if (zombie.PresentationNativePhase == 19)
            {
                if (--state.PhaseTicks <= 0)
                {
                    zombie.UsedAbility = true;
                    zombie.PresentationHasSled = false;
                    zombie.PresentationNativePhase = 0;
                    zombie.LinkedZombieId = 0;
                    zombie.BobsledState = null;
                    RefreshAdventureZombieNativeSpeed(zombie);
                }

                return;
            }

            if (zombie.PresentationNativePhase == 17 && --state.PhaseTicks <= 0)
            {
                SetAdventureZombieMotionPhase(zombie, 18);
            }
            else if (zombie.PresentationNativePhase == 18)
            {
                var age = (int)(AdventureBobsledRules.BoardingProgress(zombie.PresentationAnimationAge) * 50f);
                var start = state.Position == 1 || state.Position == 3 ? 8f : -9f;
                zombie.PresentationAltitude = start + (18f - start) * age / 50f;
            }

            // PC retains an eight-hundred-pixel edge when a Jalapeno has completely removed the row's ice.
            var iceEdge = 800;
            foreach (var ice in Adventure._ice)
            {
                if (ice.Row != zombie.Row)
                {
                    continue;
                }

                ice.RemainingTicks = Math.Max(500, ice.RemainingTicks);
                iceEdge = ice.MinX;
                break;
            }

            if (state.Position == 0 && AdventureZombieNativeRules.NativePositionX(zombie.X,
                AdventureDancerRules.Scale(zombie)) + 10f < iceEdge)
            {
                HitAdventureZombie(zombie, 6f, false, 8U);
            }
        }

        /// <summary>Starts PC 5439A0's shared sled break without removing its four living riders.</summary>
        /// <param name="leader">The front zombie whose three-hundred-point sled helmet was destroyed.</param>
        private void CrashAdventureBobsled(Zombie leader)
        {
            foreach (var member in Zombies)
            {
                if (member.BobsledState?.LeaderId != leader.Id)
                {
                    continue;
                }

                member.BobsledState.PhaseTicks = 150;
                member.PresentationNativePhase = 19;
                member.PresentationAltitude = 0f;
                member.Eating = false;
                member.PresentationAnimation = string.Empty;
                var animation = AdventurePresentationZombieAnimation(member);
                member._presentationWalkLoops = 0;
                member._presentationWalkPhase = member == leader ? 0f : (float)_random.NextDouble();
                member.PresentationNativeVelocity = leader.PresentationNativeVelocity;
                member.Speed = leader.Speed;
                if (AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(member), animation, out var clip))
                {
                    member.PresentationAnimationAge = member._presentationWalkPhase * clip.Count / AdventurePresentationZombieRate(member);
                }
            }
        }

        /// <summary>Runs the PC playing update's ambient vocal attempt before immobilization and action checks.</summary>
        /// <param name="zombie">The current original actor whose vocal counter is decremented.</param>
        private void AdvanceAdventureZombieGroan(Zombie zombie)
        {
            if (State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing
                || zombie.Wave == -2 || zombie.Wave == -3 || zombie.Wave == -4
                || zombie.Health <= 0f || zombie.PresentationNativePhase >= 1 && zombie.PresentationNativePhase <= 3)
            {
                return;
            }

            // PC 547016..547032 tests equality after decrement, unlike the mobile port's repeated <= 0 attempts.
            zombie.GroanTicks--;
            if (zombie.GroanTicks != 0 || _random.Next(Zombies.Count) != 0 || zombie.Headless
                || zombie.Kind == ZombieKind.Boss || Adventure.RewardPresentation != null)
            {
                return;
            }

            var pitch = Adventure.Level.Mode == AdventureMode.LittleTrouble ? 40f + (float)_random.NextDouble() * 10f : 0f;
            // PC 547091 selects the low giant vocal before considering the original one-in-five actor variant.
            RequestAdventureZombieFoley(zombie.Kind == ZombieKind.Gargantuar ? 70 : zombie.NativeVariant ? 12 : 11,
                zombie.Kind == ZombieKind.Gargantuar ? 0f : pitch);
            zombie.GroanTicks = 500 + _random.Next(1000);
        }

        /// <summary>Removes the original pogo stick, preserving its current altitude until native falling reaches the lawn.</summary>
        /// <param name="zombie">The pogo zombie whose retained stick is being removed.</param>
        /// <param name="damageFlags">The original damage mask; bit sixteen suppresses the detached stick particle.</param>
        private void BreakAdventurePogo(Zombie zombie, uint damageFlags = 0)
        {
            if (zombie.Kind != ZombieKind.Pogo || zombie.UsedAbility)
            {
                return;
            }

            // PC PogoBreak 540DF0 emits particle sixteen except when the magnet supplies flag sixteen.
            if ((damageFlags & 16U) == 0)
            {
                AddAdventureZombiePart(zombie, (int)ZombieKind.Pogo, 16, "Zombie_pogo_stick", null);
            }

            var state = zombie.PogoState ??= new AdventurePogoState();
            state.AnimationBlendTicks = 0;
            zombie.PresentationNativeHeight = 7;
            zombie.PresentationNativePhase = 0;
            zombie.ShieldHealth = 0f;
            zombie.UsedAbility = true;
            zombie.Eating = false;
            PickAdventureZombieNativeSpeed(zombie, 0);
            AdventurePresentationZombieAnimation(zombie);
        }

        /// <summary>Applies PC UpdateZombieFalling's one-pixel descent after the pogo stick has been lost.</summary>
        /// <param name="zombie">The live actor, including a pogo falling after a magnetic removal or Tall-nut block.</param>
        private static void AdvanceAdventurePogoFalling(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.Pogo || zombie.PresentationNativeHeight != 7)
            {
                return;
            }

            var floor = zombie.PogoState?.OnHighGround == true ? 30f : 0f;
            zombie.PresentationAltitude = Math.Max(floor, zombie.PresentationAltitude - 1f);
            zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
            if (zombie.PresentationAltitude <= floor)
            {
                zombie.PresentationNativeHeight = 0;
            }
        }

        /// <summary>Finds the original pogo vault target using the previous integer body position, before mX is stored.</summary>
        /// <param name="zombie">The bouncing pogo whose previous native attack rectangle is sampled.</param>
        /// <param name="nativeX">The retained integer mX from before this update's movement.</param>
        /// <returns>The first eligible original vault target, including plants carrying a ladder, or null.</returns>
        private Plant? FindAdventurePogoVaultTarget(Zombie zombie, int nativeX)
        {
            var backwards = zombie.PresentationWalkingBackwards ?? zombie.Hypnotized;
            var attack = AdventureZombieBiteRules.AttackRect((int)ZombieKind.Pogo, AdventureZombieFeedbackPhase(zombie),
                Adventure.Level.Mode == AdventureMode.Bowling, backwards, nativeX, 0);
            foreach (var plant in Plants)
            {
                if (AdventureBiteCanTarget(zombie, plant, AdventureBiteAttack.Vault)
                    && AdventureZombieBiteRules.Reaches(attack, AdventurePlantEatingRectangle(plant)))
                {
                    return plant;
                }
            }

            return null;
        }

        /// <summary>Runs PC 540F10 after ordinary position movement and before storing the next integer mX.</summary>
        /// <param name="zombie">The live pogo zombie whose eighty-update bounce may finish.</param>
        /// <param name="previousGridX">The previous simulation position supplying the original retained integer mX.</param>
        private void AdvanceAdventurePogo(Zombie zombie, float previousGridX)
        {
            if (!AdventurePogoRules.IsBouncing(zombie) || zombie.Health <= 0f
                || zombie.FrozenRemaining > 0f || zombie.PresentationButter > 0f || zombie.PresentationNativeHeight == 8)
            {
                return;
            }

            var state = zombie.PogoState ??= new AdventurePogoState { PhaseTicks = AdventurePogoRules.BounceTicks };
            var phase = zombie.PresentationNativePhase < 0 ? 20 : zombie.PresentationNativePhase;
            zombie.PresentationNativePhase = phase;
            state.PhaseTicks = Math.Max(0, state.PhaseTicks - 1);
            zombie.PresentationAltitude = AdventurePogoRules.Altitude(phase, state.PhaseTicks);
            if (state.PhaseTicks == 7)
            {
                // The same eleven-frame clip restarts in place; PlayZombieReanim is not called again.
                state.AnimationSeconds = 0f;
                zombie.PresentationAnimationAge = 0f;
                zombie._presentationWalkPhase = 0f;
                zombie._presentationWalkLoops = 0;
            }

            var onBoard = zombie.Wave != -2 && zombie.Wave != -3;
            if (onBoard && state.PhaseTicks == 5)
            {
                RequestAdventureZombieFoley(26);
            }

            if (zombie.PresentationNativeHeight == 4)
            {
                state.OnHighGround = true;
                zombie.PresentationNativeHeight = 0;
            }
            else if (zombie.PresentationNativeHeight == 5)
            {
                state.OnHighGround = false;
                zombie.PresentationNativeHeight = 0;
            }

            if (state.OnHighGround)
            {
                zombie.PresentationAltitude += 30f;
            }

            zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(previousGridX, AdventureDancerRules.Scale(zombie));
            if (phase == 27 && state.PhaseTicks == 70)
            {
                var barrier = FindAdventurePogoVaultTarget(zombie, nativeX);
                if (barrier?.Kind == PlantKind.TallNut)
                {
                    RequestAdventureZombieFoley(48);
                    Presentation.PlantBiteEffects.Add(new AdventurePlantBiteEffect
                    {
                        Id = ++Presentation._effectId,
                        ParticleId = 29,
                        Row = barrier.Row,
                        NativeX = 40f + barrier.Column * 80f + 60f,
                        NativeY = AdventurePlantEatingRectangle(barrier).Y - 20f,
                        Seed = zombie.Id * 397 ^ Presentation._effectId
                    });
                    BreakAdventurePogo(zombie);
                    return;
                }
            }

            if (state.PhaseTicks > 0)
            {
                return;
            }

            var target = onBoard ? FindAdventurePogoVaultTarget(zombie, nativeX) : null;
            state.PhaseTicks = AdventurePogoRules.BounceTicks;
            if (target == null)
            {
                zombie.PresentationNativePhase = 20;
                PickAdventureZombieNativeSpeed(zombie, 20);
            }
            else
            {
                zombie.PresentationNativePhase = phase == 21 ? 27 : 21;
                zombie.PresentationNativeVelocity = phase == 21 ? (nativeX - (40 + target.Column * 80) + 60) / 80f : 0f;
                zombie.Speed = AdventureZombieNativeRules.GridSpeed(zombie.PresentationNativeVelocity);
                zombie._presentationSpeedPhase = zombie.PresentationNativePhase;
                zombie._presentationSpeedKind = (int)ZombieKind.Pogo;
            }
        }

        /// <summary>Applies PC DiggerLoseAxe 0x543CC0 without removing its nonmagnetic hardhat.</summary>
        /// <param name="zombie">The digger whose retained pickaxe has been removed.</param>
        private void LoseAdventureDiggerAxe(Zombie zombie)
        {
            var state = zombie.DiggerState ??= new AdventureDiggerState();
            if (AdventureZombieFeedbackPhase(zombie) == 32)
            {
                state.PausedDigSeconds = zombie.PresentationAnimationAge;
                zombie.PresentationNativePhase = 34;
                state.PhaseTicks = 200;
                state.TunnelStopAge = zombie.Age;
            }

            zombie.UsedAbility = true;
        }

        /// <summary>Starts PC 0x543D90's emergence and its independent original dirt effects.</summary>
        /// <param name="zombie">The stationary digger beginning its ascent.</param>
        /// <param name="withoutAxe">Whether this follows the original two-hundred-update axe-loss pause.</param>
        private void BeginAdventureDiggerRise(Zombie zombie, bool withoutAxe)
        {
            var state = zombie.DiggerState ??= new AdventureDiggerState();
            zombie.PresentationNativePhase = withoutAxe ? 35 : 33;
            zombie.AbilityPhase = 1;
            zombie.Submerged = false;
            zombie.PresentationAltitude = -120f;
            state.PhaseTicks = 130;
            state.AnimationBlendTicks = 0;
            if (state.TunnelStopAge < 0f)
            {
                state.TunnelStopAge = zombie.Age;
            }

            var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            Presentation.ZombieRiseEffects.Add(new AdventureZombieRiseEffect
            {
                Id = ++Presentation._effectId,
                Row = zombie.Row,
                X = nativeX + 60f,
                Y = AdventureBiteNativeY(zombie, nativeX) + 118f,
                Digger = true
            });
            AdventurePresentationZombieAnimation(zombie);
            RequestAdventureZombieFoley(80);
            if (!withoutAxe)
            {
                RequestAdventureZombieFoley(83);
            }
        }

        /// <summary>Runs the original PC digger's tunnelling, emergence, axe-loss pause, and dizzy phases.</summary>
        /// <param name="zombie">The actor updated at the original one-hundred-hertz simulation rate.</param>
        /// <param name="dt">The current fixed simulation interval in seconds.</param>
        /// <returns>Whether the original phase blocks ordinary walking and eating this update.</returns>
        private bool AdvanceAdventureDigger(Zombie zombie, float dt)
        {
            if (zombie.Kind != ZombieKind.Digger)
            {
                return false;
            }

            var state = zombie.DiggerState ??= new AdventureDiggerState();
            var phase = AdventureZombieFeedbackPhase(zombie);
            zombie.PresentationNativePhase = phase;
            if (phase == 32)
            {
                // PC checks the strict native edge before this update's unchilled tunnelling movement.
                if (zombie.X < AdventureZombieNativeRules.GridX(10f, AdventureDancerRules.Scale(zombie)))
                {
                    BeginAdventureDiggerRise(zombie, false);
                }
                else
                {
                    var mine = FindAdventureNativePlantTarget(zombie, AdventureBiteAttack.Chew);
                    if (!AdvanceAdventureNativeEating(zombie, mine, dt))
                    {
                        zombie.X -= zombie.Speed * dt;
                    }
                }

                return true;
            }

            if (phase == 34)
            {
                state.PhaseTicks = Math.Max(0, state.PhaseTicks - 1);
                if (state.PhaseTicks == 150)
                {
                    state.SurpriseAge = 0f;
                }

                if (state.PhaseTicks == 0)
                {
                    BeginAdventureDiggerRise(zombie, true);
                }

                return true;
            }

            if (phase == 33 || phase == 35)
            {
                state.PhaseTicks = Math.Max(0, state.PhaseTicks - 1);
                zombie.PresentationAltitude = AdventureDiggerState.RiseAltitude(state.PhaseTicks);
                if (state.PhaseTicks == 30)
                {
                    state.AnimationBlendTicks = phase == 35 ? 20 : 0;
                    // The no-axe branch restarts the same previously held landing clip at twelve FPS.
                    zombie.PresentationAnimation = string.Empty;
                    AdventurePresentationZombieAnimation(zombie);
                }

                if (state.PhaseTicks == 0)
                {
                    zombie.PresentationAltitude = 0f;
                    zombie.PresentationNativePhase = phase == 35 ? 38 : 36;
                    state.AnimationBlendTicks = phase == 35 ? 20 : 10;
                    if (phase == 35)
                    {
                        zombie.AbilityPhase = 3;
                        PickAdventureZombieNativeSpeed(zombie, 38);
                    }

                    AdventurePresentationZombieAnimation(zombie);
                }

                return true;
            }

            if (phase == 36)
            {
                if (zombie._presentationWalkLoops <= 1)
                {
                    return true;
                }

                zombie.PresentationNativePhase = 37;
                zombie.AbilityPhase = 2;
                state.AnimationBlendTicks = 20;
                PickAdventureZombieNativeSpeed(zombie, 37);
                AdventurePresentationZombieAnimation(zombie);
            }

            return false;
        }

        /// <summary>Checks the original cell-based ladder independently of the targeted plant layer.</summary>
        /// <param name="row">The native board row.</param>
        /// <param name="column">The native board column.</param>
        /// <returns>Whether any surviving plant in this cell retains the placed ladder.</returns>
        internal bool AdventureHasLadderAt(int row, int column)
        {
            foreach (var plant in Plants)
            {
                if (plant.Row == row && plant.Column == column && plant.HasLadder && plant.Health > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Removes PC Plant::Die's ladder from every plant layer in the cell.</summary>
        /// <param name="row">The native board row.</param>
        /// <param name="column">The native board column.</param>
        private void RemoveAdventureLadder(int row, int column)
        {
            foreach (var plant in Plants)
            {
                if (plant.Row == row && plant.Column == column)
                {
                    plant.HasLadder = false;
                }
            }
        }

        /// <summary>Runs PC 546620's retained twelve-frame placement before detaching the carried shield.</summary>
        /// <param name="zombie">The live ladder carrier whose original action is being advanced.</param>
        /// <returns>Whether the placing body must hold its position for this update.</returns>
        private bool AdvanceAdventureLadderPlacing(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.Ladder)
            {
                return false;
            }

            var phase = AdventureZombieFeedbackPhase(zombie);
            if (phase == 77 && zombie.ShieldHealth <= 0f)
            {
                // Native DetachShield interrupts the placing phase independently of the remaining head state.
                SetAdventureZombieMotionPhase(zombie, 0);
                zombie.PresentationBodyBlendTicks = 0;
                return false;
            }

            if (zombie.Hypnotized || zombie.Headless || zombie.Health <= 0f)
            {
                return phase == 77;
            }

            if (phase == 77)
            {
                if (zombie.PresentationAnimationAge * 24f + .0001f < 12f)
                {
                    return true;
                }

                var target = FindAdventureNativePlantTarget(zombie, AdventureBiteAttack.Ladder);
                if (target != null)
                {
                    target.HasLadder = true;
                    zombie.UseLadderColumn = target.Column;
                    zombie.PresentationNativeHeight = 6;
                    zombie.ShieldHealth = 0f;
                    zombie.UsedAbility = true;
                    SetAdventureZombieMotionPhase(zombie, 0);
                    zombie.PresentationBodyBlendTicks = 0;
                    RequestAdventureZombieFoley(-7);
                }
                else
                {
                    SetAdventureZombieMotionPhase(zombie, 76);
                    zombie.PresentationBodyBlendTicks = 0;
                }

                return false;
            }

            if (phase == 76 && zombie.ShieldHealth > 0f && zombie.PresentationNativeHeight == 0
                && FindAdventureNativePlantTarget(zombie, AdventureBiteAttack.Ladder) != null)
            {
                StopAdventureNativeEating(zombie);
                zombie.PresentationNativePhase = 77;
                zombie.PresentationAnimation = string.Empty;
                AdventurePresentationZombieAnimation(zombie);
                // Playing the placement clip retains the carrier's velocity; StartWalkAnim resamples only on completion.
                zombie._presentationSpeedPhase = 77;
                zombie._presentationSpeedKind = (int)zombie.Kind;
                zombie.PresentationBodyBlendTicks = 10;
                return true;
            }

            return false;
        }

        /// <summary>Updates PC 5452B0's falling height, including the armed pole vaulter's extra pixel.</summary>
        /// <param name="zombie">The live or retained dead actor falling from a ladder.</param>
        private static void AdvanceAdventureLadderFall(Zombie zombie)
        {
            var ground = zombie.PogoState?.OnHighGround == true ? 30f : 0f;
            zombie.PresentationAltitude = Math.Max(ground, zombie.PresentationAltitude
                - (zombie.PresentationNativePhase == 11 ? 2f : 1f));
            if (zombie.PresentationAltitude <= ground)
            {
                zombie.PresentationNativeHeight = 0;
            }

            if (zombie.PoolState != null)
            {
                zombie.PoolState.Altitude = zombie.PresentationAltitude;
            }

            zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
        }

        /// <summary>Runs PC 546CE0's ladder probe, .8-pixel rise, and same-update transition into falling.</summary>
        /// <param name="zombie">The actor with an active original ladder height state.</param>
        /// <param name="dt">The fixed one-hundred-hertz update interval.</param>
        /// <returns>Whether this update belongs to ladder traversal rather than eating or another ability.</returns>
        private bool AdvanceAdventureLadderTraversal(Zombie zombie, float dt)
        {
            if (zombie.UseLadderColumn < 0 || zombie.PresentationNativeHeight != 6 && zombie.PresentationNativeHeight != 7)
            {
                return false;
            }

            if (zombie.PresentationNativeHeight == 6)
            {
                var scale = AdventureDancerRules.Scale(zombie);
                var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, scale);
                var ground = zombie.PogoState?.OnHighGround == true ? 30f : 0f;
                var probe = (int)(nativeX + 5 + (zombie.PresentationAltitude - ground) * .5f);
                var column = Math.Max(0, Math.Min(8, (Math.Max(40, probe) - 40) / 80));
                if (!AdventureHasLadderAt(zombie.Row, column))
                {
                    zombie.PresentationNativeHeight = 7;
                }
                else
                {
                    zombie.PresentationAltitude += .8f;
                    if (zombie.PresentationNativeVelocity < .5f)
                    {
                        zombie.X -= .5f / 80f;
                    }

                    if (zombie.PresentationAltitude >= ground + 90f)
                    {
                        zombie.PresentationNativeHeight = 7;
                    }
                }
            }

            if (zombie.PresentationNativeHeight == 7)
            {
                AdvanceAdventureLadderFall(zombie);
            }

            if (zombie.PoolState != null)
            {
                zombie.PoolState.Altitude = zombie.PresentationAltitude;
            }

            zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
            zombie.PresentationBiteClipHeight = -10000;
            zombie.Eating = false;
            var slow = zombie.ChillRemaining > 0f ? .5f : 1f;
            zombie.ChillRemaining = Math.Max(0f, zombie.ChillRemaining - dt);
            var direction = zombie.PresentationWalkingBackwards ?? zombie.Hypnotized;
            var previousWalkingX = zombie.X;
            zombie.X += (direction ? 1f : -1f) * AdventurePresentationWalkingDistance(zombie, dt, zombie.Speed * dt * slow);
            RecordAdventureZombieFootsteps(zombie, previousWalkingX);
            return true;
        }

        /// <summary>Applies the existing escape, mower and loss boundaries to ordinary and ladder movement.</summary>
        /// <param name="z">The live actor whose horizontal movement has completed.</param>
        /// <returns>Whether the actor caused the current match to be lost.</returns>
        private bool CheckAdventureZombieBoardEdge(Zombie z)
        {
            if (z.X > 12f && (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)))
            {
                z.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                KillAdventureZombie(z);
                return false;
            }

            if (!z.Hypnotized && z.X < 0.15f && Mowers[z.Row].Available)
            {
                Mowers[z.Row].Available = false;
                Mowers[z.Row].Active = true;
                Adventure.RowPicker.LastMowedWave[z.Row] = Adventure.CurrentWave;
                Raise(GameEvent.Mower);
            }
            else if (!z.Hypnotized && z.X < -0.65f && !Mowers[z.Row].Active)
            {
                State = MatchState.Lost;
                Adventure.Phase = AdventurePhase.Lost;
                Raise(GameEvent.Lost);
                return true;
            }

            return false;
        }

        /// <summary>Performs advance adventure zombies.</summary>
        /// <param name = "dt">The dt.</param>
        private void AdvanceAdventureZombies(float dt)
        {
            AdvanceAdventurePlantHitCounters(dt);
            _dancerClockTicks++;
            foreach (Zombie z in new PooledSnapshot<Zombie>(Zombies))
            {
                if (!Zombies.Contains(z))
                {
                    continue;
                }

                z.Age += dt;
                if (AdvanceAdventureBalloonWind(z))
                {
                    continue;
                }

                AdvanceAdventureZombieGroan(z);
                if (z.Kind == ZombieKind.Zomboni && z.ZomboniState?.FlatTires == true)
                {
                    if (--z.ZomboniState.DeathTicks <= 0)
                    {
                        ExplodeAdventureZomboni(z);
                        KillAdventureZombie(z);
                    }

                    continue;
                }

                if (z.PoolState != null)
                {
                    if (z.PoolState.SplashAge >= 0f)
                    {
                        z.PoolState.SplashAge += dt;
                    }

                    if (z.PoolState.SeaweedAge >= 0f)
                    {
                        z.PoolState.SeaweedAge += dt;
                    }
                }

                if (z.Wave != -4 && AdventureZomboniRules.HasCriticalDamage(z) && _random.Next(5) == 0)
                {
                    // PC 5471DD..547221 applies silent body damage with a one-in-five chance per native tick.
                    HitAdventureZombie(z, z.StartingHealth >= 500f ? 3f : 1f, true, 9U);
                    if (!Zombies.Contains(z))
                    {
                        continue;
                    }
                }

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
                        if (z.PoolState != null && z.PresentationInPool == true)
                        {
                            z.PoolState.Altitude = AdventureZombiePoolRules.RiseAltitude(z.GraveRiseTicks, AdventureDancerRules.Scale(z));
                        }

                        if (z.GraveRiseTicks == 0)
                        {
                            z.PresentationNativePhase = -1;
                            UpdateAdventurePoolBodyRectangle(z);
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

                if (z.FrozenRemaining > 0f || z.PresentationButter > 0f)
                {
                    z.FrozenRemaining = Math.Max(0, z.FrozenRemaining - dt);
                    continue;
                }

                if (AdvanceAdventureLadderTraversal(z, dt))
                {
                    if (CheckAdventureZombieBoardEdge(z))
                    {
                        return;
                    }

                    continue;
                }

                AdvanceAdventurePogoFalling(z);
                var slow = (AdventureDancerRules.IsDancer(z)
                    ? AdventureDancerRules.GroupAffected(z, Zombies, true) : z.ChillRemaining > 0f) ? .5f : 1f;
                AdvanceAdventureZombiePool(z, dt);
                if (z.FrozenRemaining > 0f)
                {
                    continue;
                }

                z.ChillRemaining = Math.Max(0, z.ChillRemaining - dt);
                if (AdvanceAdventureJackInTheBox(z))
                {
                    continue;
                }

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

                if (AdvanceAdventurePoleVault(z, dt) || AdvanceAdventureSnorkel(z, dt) || AdvanceAdventureDolphin(z, dt))
                {
                    continue;
                }

                AdvanceAdventureBalloon(z);
                AdvanceAdventureBobsled(z);
                if (AdvanceAdventureLadderPlacing(z))
                {
                    continue;
                }

                z.AbilityTimer -= dt * slow;
                if (z.Kind == ZombieKind.Bungee)
                {
                    AdvanceAdventureBungee(z);
                    continue;
                }

                if (AdventureDancerRules.IsDancer(z))
                {
                    AdvanceAdventureDancer(z);
                    if (z.PresentationNativePhase == 50)
                    {
                        continue;
                    }
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

                if (AdvanceAdventureDigger(z, dt))
                {
                    continue;
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
                                victim = AdventureAirAttackPlantAt(p.Row, p.Column);
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
                        else if (victim != null)
                        {
                            RequestAdventureZombieFoley(73);
                        }

                        z.AbilityPhase++;
                        z.AbilityTimer = 5f;
                    }

                    continue;
                }

                if (z.Kind == ZombieKind.Zomboni)
                {
                    ExtendAdventureIceTrail(z);
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
                if (opponent != null && (z.Hypnotized || opponent.Hypnotized) && !z.Headless && z.TangleKelpGrab == null
                    && !AdventurePogoRules.IsBouncing(z))
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
                var vault = z.Kind == ZombieKind.PoleVaulter;
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

                if (target != null && AdventureHasLadderAt(target.Row, target.Column) && target.Layer != PlantLayer.Support && z.Kind != ZombieKind.Digger
                    && (AdventureNativeChewer(z) || z.Kind == ZombieKind.PoleVaulter && z.PresentationNativePhase == 11))
                {
                    StopAdventureNativeEating(z);
                    if (z.PresentationNativeHeight == 0 && z.UseLadderColumn != target.Column)
                    {
                        z.PresentationNativeHeight = 6;
                        z.UseLadderColumn = target.Column;
                    }

                    target = null;
                }

                if (target != null && z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility && !target.HasLadder)
                {
                    BeginAdventurePoleVault(z, target);
                    continue;
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

                if (target != null && z.Kind == ZombieKind.Dancing && z.PresentationNativePhase == 40)
                {
                    // PC EatPlant shortens the moonwalk instead of biting before the first point animation.
                    z.DancerState!.PhaseTicks = 1;
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
                        CrushAdventurePlantCell(target.Row, target.Column, true);
                        if (z.Kind == ZombieKind.Zomboni && target.Kind == PlantKind.Spikeweed)
                        {
                            HitAdventureZombie(z, 20f, true, 32U);
                            RemoveAdventurePlantAndStack(target);
                        }
                        else if (target.Kind == PlantKind.Spikerock)
                        {
                            HitAdventureZombie(z, 1800f, true, z.Kind == ZombieKind.Zomboni ? 32U : 0U);
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
                            RequestAdventureZombieFoley(-5);
                            AdventureDancerRules.DetachForMindControl(z, Zombies);
                            RememberAdventureRewardPosition(z);
                            z.Submerged = false;
                            RemoveAdventurePlantAndStack(target);
                            continue;
                        }

                        if (z.Kind == ZombieKind.Gargantuar && target.Kind != PlantKind.Spikerock)
                        {
                            CrushAdventurePlantCell(target.Row, target.Column, false);
                            z.BiteTimer = 2f;
                            continue;
                        }

                        target.Health -= z.Kind == ZombieKind.Gargantuar ? 50f : 4f;
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
                    var direction = z.Hypnotized || z.PresentationNativePhase == 40
                        || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z) ? 1f : -1f;
                    var previousX = z.X;
                    z.X += direction * AdventurePresentationWalkingDistance(z, dt, z.Speed * dt * slow);
                    RecordAdventureZombieFootsteps(z, previousX);
                    AdvanceAdventurePogo(z, previousX);
                }

                if (CheckAdventureZombieBoardEdge(z))
                {
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
                var row = ChooseAdventureZombieRow(zombieKind);
                var zombie = SpawnAdventureBoardZombie(zombieKind, row, grave.Column + 0.5f, a.CurrentWave - 1);
                zombie.Row = grave.Row;
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
                var zombieKind = final ? (_random.Next(2) == 0 ? ZombieKind.Conehead : ZombieKind.Buckethead) : kind;
                var row = ChooseAdventureZombieRow(zombieKind);
                var z = SpawnAdventureBoardZombie(zombieKind, row, g.Column + 0.5f, a.CurrentWave - 1);
                z.Row = g.Row;
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

            if (m == ModeMechanic.Vases)
            {
                return;
            }

            if (m == ModeMechanic.Slots)
            {
                foreach (var expiring in new PooledSnapshot<AdventurePacket>(Adventure._packets))
                {
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
                            z.X = exit.Column + (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z) ? 0.65f : 0.35f);
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
                        a._ice.Add(new AdventureIceTrail
                        {
                            Id = ++Presentation._effectId, Row = r, MinX = 400, RemainingTicks = int.MaxValue
                        });
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
            PlaceAdventurePlantWithEffects(PlantKind.CherryBomb, row, column);
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
                var repick = ModeSession.Definition.Survival && ModeSession.Stage > 1 && ModeSession.Phase == ModePhase.ChoosingSeeds;
                AdvanceAdventurePoolPresentation(dt, repick);
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
            AdvanceAdventurePoolEffect();
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

            AdvanceAdventureFog();
            TickCellTimers(Adventure._craters, dt);
            AdvanceAdventureIceTrails();
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
            AdvanceVasePackets();
            AdvanceAdventurePlants(dt);
            AdvanceAdventureProjectiles(dt);
            AdvanceAdventureZombies(dt);
            FinishAdventureIceTrapTick();
            if (State == MatchState.Lost)
            {
                s.Phase = ModePhase.Lost;
                return;
            }

            AdvanceAdventureMowers(dt);
            AdvanceAdventurePurchases(dt);
            AdvanceVaseMallet();
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
                cleared = Adventure.BossHealth <= 0f && Zombies.TrueForAll(zombie => zombie.Hypnotized) && Adventure._balls.Count == 0;
            }
            else
            {
                cleared = Adventure.CurrentWave >= Adventure.Waves.Count && Adventure._finalAmbush && Zombies.TrueForAll(zombie => zombie.Hypnotized);
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
            // PC 0x41FAB0 preserves allies only while survival is waiting for another seed selection.
            if (!s.Definition.Survival || !next)
            {
                Zombies.Clear();
            }

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
            Adventure.WaveCountdown = s.Definition.Survival ? 6f : 18f;
            Adventure.RowPicker.ResetMowedWaves();
            if (s.Definition.Mechanic == ModeMechanic.Vases)
            {
                Plants.Clear();
                ClearSuns();
                StartVaseScreenFlash();
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
                        PlaceAdventurePlantWithEffects(p.Kind, r, column);
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
            Presentation.VaseMalletTicks = 0;
            var s = RequiredModeSession;
            s._vaseSeeds.Clear();
            s._vaseSun.Clear();
            Adventure._vases.Clear();
            Adventure._packets.Clear();
            var id = s.Definition.Id;
            var min = id == 51 ? 4 : id == 52 || id == 53 || id == 57 ? 3 : 2;
            var cells = new List<int>();
            for (var c = min; c < 9; c++)
            {
                for (var r = 0; r < 5; r++)
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

            RevealRandomVasePlants(2);

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

        /// <summary>Starts the original delayed hammer swing over a standalone puzzle vase.</summary>
        /// <param name="row">The target vase's lawn row.</param>
        /// <param name="column">The target vase's lawn column.</param>
        /// <returns>Whether an available vase accepted a new hammer swing.</returns>
        public bool SmashModeVase(int row, int column)
        {
            var s = ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Vases || s.Phase != ModePhase.Playing || State != MatchState.Playing)
            {
                return false;
            }

            return BeginVaseMallet(row, column);
        }

        /// <summary>Releases the original standalone recipe contents after the target vase has been removed.</summary>
        /// <param name="vase">The vase reached by the completed native hammer swing.</param>
        private void OpenModeVase(AdventureVase vase)
        {
            var s = RequiredModeSession;
            var cell = vase.Row * 9 + vase.Column;
            if (s._vaseSeeds.TryGetValue(cell, out var seed))
            {
                var packet = AddAdventurePacket(seed == 52 ? PlantKind.Repeater : (PlantKind)seed, vase.Row, vase.Column);
                if (seed == 52)
                {
                    s._backwardPackets.Add(packet.Id);
                }

                s._vaseSeeds.Remove(cell);
            }
            else if (s._vaseSun.TryGetValue(cell, out var value))
            {
                for (var i = 0; i < value / 25; i++)
                {
                    Suns.Add(CreateAdventurePresentationSunFromPoint(40f + vase.Column * 80f + i * 15f, 80f + vase.Row * 100f, 25));
                }

                s._vaseSun.Remove(cell);
            }
            else if (vase._zombie.HasValue)
            {
                SpawnAdventureBoardZombie(vase._zombie.Value, vase.Row, vase.Column + 0.5f, s.Stage - 1);
            }
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
                    ModeFixedPlant(1, 3, 2);
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

            var z = SpawnAdventureBoardZombie(kind, row, column + 0.5f, s.Stage - 1);
            if (kind == ZombieKind.Bungee)
            {
                z.Row = row;
                z.X = AdventureZombieNativeRules.GridX(40f + column * 80f, AdventureDancerRules.Scale(z));
                z.PresentationPreviousX = z.X;
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
                if (!z.Hypnotized && !z.Headless && z.Kind != ZombieKind.Bungee && !(z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)) && z.X < 0.2f)
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
                var lost = s._sunflowerHealth[id] - (p == null || p.BungeeState == 2 ? 0 : p.Health);
                var drops = p == null || p.BungeeState == 2 ? 8 : Math.Min(8, (int)(lost / 40f));
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
            if (!IsAdventure)
            {
                return;
            }

            Adventure.IceTrapTicks = Math.Max(Adventure.IceTrapTicks, Math.Max(0, ticks));
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
            var mode = ModeSession == null ? 0 : ModeSession.Definition.Id;
            var flagNext = a.CurrentWave < a.Waves.Count && a.Waves[a.CurrentWave].IsFlag;
            if (flagNext && a.Level.Mode != AdventureMode.Bowling && mode != 31)
            {
                s.HealthToNextWave = 0;
                s.CountdownTicks = 4500;
            }
            else
            {
                var threshold = AdventureSpawnRandom.NextRange(_random, 0.5f, 0.65f);
                s.HealthToNextWave = (int)((double)threshold * s.HealthAtWaveStart);
                s.CountdownTicks = a.Level.Mode == AdventureMode.LittleTrouble || mode == 27 || mode == 31
                    ? 750 : 2500 + _random.Next(600);
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
                    total += Math.Max(0, (int)z.Health) + Math.Max(0, (int)z.ArmorHealth)
                        + Math.Max(0, (int)(z.ShieldHealth * 0.2f)) + Math.Max(0, z.FlyingHealth);
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
            var dropWeights = a.Level.Mode == AdventureMode.Bungee ? CreateAdventureBungeeDropWeights() : null;
            var spawned = 0;
            foreach (var kind in wave.Zombies)
            {
                var x = 9.5f + spawned++ * 0.12f;
                if (kind == ZombieKind.Bobsled && !a._ice.Exists(ice => ice.RemainingTicks > 0 && ice.MinX < 700))
                {
                    for (var follower = 0; follower < 4; follower++)
                    {
                        var fallbackRow = ChooseAdventureZombieRow(ZombieKind.Normal);
                        SpawnAdventureBoardZombie(ZombieKind.Normal, fallbackRow, x, wave.Index);
                    }

                    continue;
                }

                if (a.Level.Mode == AdventureMode.Bungee && kind != ZombieKind.Bungee && kind != ZombieKind.Zomboni)
                {
                    var cell = PickAdventureBungeeDropCell(dropWeights!);
                    DropAdventureZombie(kind, cell % 10, cell / 10, wave.Index);
                    continue;
                }

                var row = kind == ZombieKind.Boss ? 0 : ChooseAdventureZombieRow(kind);
                if (a.Level.Mode == AdventureMode.WhackAZombie && a._graves.Count > 0)
                {
                    var grave = a._graves[_random.Next(a._graves.Count)];
                    row = grave.Row;
                    x = grave.Column + 0.5f;
                }

                var leader = SpawnAdventureBoardZombie(kind, row, x, wave.Index);
                if (kind == ZombieKind.Bobsled)
                {
                    for (var follower = 1; follower < 4; follower++)
                    {
                        var sled = SpawnAdventureZombie(kind, row, x, wave.Index);
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
        private void SpawnScheduledFinalAmbush()
        {
            var a = Adventure;
            var wave = Math.Max(0, a.Waves.Count - 1);
            // Ice-shroom freezes the native pool/roof ambush; no replacement timer or invented enemy is added.
            var iced = Adventure.IceTrapTicks > 0;
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
                    var kind = PickScheduledAmbushZombie(false);
                    var z = SpawnAdventureBoardZombie(kind, cell % 10, cell / 10 + 0.5f, wave);
                    InitializeAdventurePoolZombie(z, cell / 10);
                    a.Notice(AdventureNoticeKind.PoolAmbush, (int)z.Kind, z.Row, cell / 10);
                }
            }
            else if (a.Level.HasRoof && !iced)
            {
                var count = a.Level.Number <= 42 ? 2 : 3;
                var dropWeights = CreateAdventureBungeeDropWeights();
                for (var index = 0; index < count; index++)
                {
                    var kind = PickScheduledAmbushZombie(false);
                    var cell = PickAdventureBungeeDropCell(dropWeights);
                    DropAdventureZombie(kind, cell % 10, cell / 10, wave);
                }
            }

            if (a.Level.Terrain == AdventureTerrain.Night && a.Level.Mode != AdventureMode.WhackAZombie)
            {
                foreach (var grave in a._graves)
                {
                    var kind = PickScheduledAmbushZombie(true);
                    var row = ChooseAdventureZombieRow(kind);
                    var z = SpawnAdventureBoardZombie(kind, row, grave.Column + 0.5f, wave);
                    z.Row = grave.Row;
                    InitializeAdventureGraveZombie(z, grave.Column);
                    a.Notice(AdventureNoticeKind.GraveRise, (int)z.Kind, grave.Row, grave.Column);
                }
            }
        }

        /// <summary>Starts the original fifty-update pool ambush with its native swimming body and attached seaweed.</summary>
        /// <param name="zombie">The selected normal, conehead, or buckethead zombie retaining its original armor.</param>
        /// <param name="column">The selected pool cell.</param>
        private void InitializeAdventurePoolZombie(Zombie zombie, int column)
        {
            var nativeX = 40f + column * 80f - 25f;
            zombie.X = AdventureZombieNativeRules.GridX(nativeX, AdventureDancerRules.Scale(zombie));
            zombie.PresentationPreviousX = zombie.X;
            zombie.PoolState ??= new AdventureZombiePoolState { HasDuckyTube = AdventureZombiePoolRules.UsesPlainBody(zombie.Kind) };
            zombie.PoolState.Altitude = AdventureZombiePoolRules.RiseAltitude(50, AdventureDancerRules.Scale(zombie));
            zombie.PoolState.SeaweedAge = 0f;
            zombie.PresentationInPool = true;
            zombie.PresentationNativeHeight = 0;
            zombie.GraveRiseTicks = 50;
            zombie.PresentationNativePhase = 14;
            zombie.DropRemaining = 0f;
            zombie.Eating = false;
            zombie.PresentationBiteBodyY = 0;
            zombie.PresentationBiteClipHeight = -10000;
            AdventurePresentationZombieAnimation(zombie);
            RecordAdventurePoolSplash(zombie, false);
        }

        /// <summary>Advances the original plain water body's entrance or exit without changing its species.</summary>
        /// <param name="zombie">The live zombie with an initialized pool body.</param>
        private void AdvanceAdventurePlainZombiePool(Zombie zombie)
        {
            var state = zombie.PoolState;
            if (state == null || zombie.Airborne)
            {
                return;
            }

            var scale = AdventureDancerRules.Scale(zombie);
            if (zombie.PresentationNativeHeight == 3)
            {
                state.Altitude -= 1f;
            }
            else if (zombie.PresentationNativeHeight == 1)
            {
                state.Altitude = Math.Max(-40f * scale, state.Altitude - 1f);
                if (state.Altitude <= -40f * scale)
                {
                    zombie.PresentationNativeHeight = 0;
                    AdventurePresentationZombieAnimation(zombie);
                }
            }
            else if (zombie.PresentationNativeHeight == 2)
            {
                state.Altitude = Math.Min(0f, state.Altitude + 1f);
                if (state.Altitude >= 0f)
                {
                    zombie.PresentationNativeHeight = 0;
                    zombie.PresentationInPool = false;
                }
            }
            else
            {
                // PC PixelToGridX returns -1 left of x40 and clamps the right edge to column eight.
                var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, scale);
                var waterSquare = Adventure.Level.IsWaterRow(zombie.Row) && AdventureZombieMotionRules.IsPoolPosition(nativeX);
                if (waterSquare && zombie.PresentationInPool != true)
                {
                    if (Adventure.IceTrapTicks > 0)
                    {
                        zombie.FrozenRemaining = Math.Max(zombie.FrozenRemaining, Adventure.IceTrapTicks * .01f);
                        ApplyAdventureChill(zombie, true);
                        return;
                    }

                    zombie.PresentationNativeHeight = 1;
                    zombie.PresentationInPool = true;
                    RecordAdventurePoolSplash(zombie, true);
                }
                else if (!waterSquare && zombie.PresentationInPool == true)
                {
                    zombie.PresentationNativeHeight = 2;
                    AdventurePresentationZombieAnimation(zombie);
                    RecordAdventurePoolSplash(zombie, false);
                }
            }

            UpdateAdventurePoolBodyRectangle(zombie);
        }

        /// <summary>Updates the native submerged collision offsets after a pool altitude transition.</summary>
        /// <param name="zombie">The actor whose original body rectangle follows the water altitude.</param>
        private static void UpdateAdventurePoolBodyRectangle(Zombie zombie)
        {
            if (zombie.PoolState == null)
            {
                return;
            }

            if (zombie.UseLadderColumn >= 0 && (zombie.PresentationNativeHeight == 6 || zombie.PresentationNativeHeight == 7))
            {
                zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
                zombie.PresentationBiteClipHeight = -10000;
            }
            else if (zombie.PresentationInPool == true)
            {
                zombie.PresentationBiteBodyY = (int)-zombie.PoolState.Altitude;
                zombie.PresentationBiteClipHeight = (int)(-zombie.PoolState.Altitude - 7f
                    + 10f * (1f - AdventureDancerRules.Scale(zombie)) + (zombie.Eating ? 7f : 0f));
            }
            else
            {
                zombie.PresentationBiteBodyY = 0;
                zombie.PresentationBiteClipHeight = -10000;
            }
        }

        /// <summary>Records the original fixed splash origin and entry or emergence Foley for the actor's own render layer.</summary>
        /// <param name="zombie">The actor crossing the water surface.</param>
        /// <param name="entering">Whether the normal entrance uses pitched Foley 19 instead of Foley 52.</param>
        private void RecordAdventurePoolSplash(Zombie zombie, bool entering)
        {
            var state = zombie.PoolState!;
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var nativeY = (int)AdventureBiteNativeY(zombie, nativeX);
            state.SplashAge = 0f;
            state.SplashNativeX = nativeX + 23f;
            state.SplashNativeY = nativeY + 78f;
            RequestAdventureZombieFoley(entering ? 19 : 52);
        }

        /// <summary>Starts the original land grave ambush without treating the zombie as a falling actor.</summary>
        /// <param name="zombie">The new zombie created by the final-wave grave ambush.</param>
        /// <param name="column">The grave's lawn column.</param>
        private void InitializeAdventureGraveZombie(Zombie zombie, int column)
        {
            var nativeX = 40f + column * 80f - 25f;
            var whack = Adventure.Level.Mode == AdventureMode.WhackAZombie || ModeSession != null && ModeSession.Definition.Mechanic == ModeMechanic.Whack;
            zombie.X = AdventureZombieNativeRules.GridX(nativeX, AdventureDancerRules.Scale(zombie));
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
            foreach (var id in ids)
            {
                var d = VerifiedDataCatalog.Zombies[id];
                if (ModeSession == null && !Adventure.Level.IsReplay && Adventure.Level.Number < d.BaseLevelGateRaw
                    || id != 0 && !Adventure.EnabledZombieTypes[id])
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
