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
    /// <summary>Coordinates deterministic sessions and delegates board operations to their owning collaborators.</summary>
    public sealed class GameSimulation
    {
        /// <summary>Stores the state owned by this match.</summary>
        private readonly SimulationWorld _world = new SimulationWorld();
        /// <summary>Coordinates AdventureModeRunner operations.</summary>
        private readonly AdventureModeRunner _adventureModeRunner;
        /// <summary>Coordinates AdventureSpecials operations.</summary>
        private readonly AdventureSpecials _adventureSpecials;
        /// <summary>Coordinates AquariumMode operations.</summary>
        private readonly AquariumMode _aquariumMode;
        /// <summary>Coordinates BalloonZombieActions operations.</summary>
        private readonly BalloonZombieActions _balloonZombieActions;
        /// <summary>Coordinates BattlePresentationSystem operations.</summary>
        private readonly BattlePresentationSystem _battlePresentationSystem;
        /// <summary>Coordinates BiteSystem operations.</summary>
        private readonly BiteSystem _biteSystem;
        /// <summary>Coordinates BoardEnvironment operations.</summary>
        private readonly BoardEnvironment _boardEnvironment;
        /// <summary>Coordinates BoardQueries operations.</summary>
        private readonly BoardQueries _boardQueries;
        /// <summary>Coordinates BobsledZombieActions operations.</summary>
        private readonly BobsledZombieActions _bobsledZombieActions;
        /// <summary>Coordinates BossSystem operations.</summary>
        private readonly BossSystem _bossSystem;
        /// <summary>Coordinates BowlingSystem operations.</summary>
        private readonly BowlingSystem _bowlingSystem;
        /// <summary>Coordinates BungeeZombieActions operations.</summary>
        private readonly BungeeZombieActions _bungeeZombieActions;
        /// <summary>Coordinates CoinSystem operations.</summary>
        private readonly CoinSystem _coinSystem;
        /// <summary>Coordinates ColdSystem operations.</summary>
        private readonly ColdSystem _coldSystem;
        /// <summary>Coordinates CombatSystem operations.</summary>
        private readonly CombatSystem _combatSystem;
        /// <summary>Coordinates ConveyorSystem operations.</summary>
        private readonly ConveyorSystem _conveyorSystem;
        /// <summary>Coordinates DancerZombieActions operations.</summary>
        private readonly DancerZombieActions _dancerZombieActions;
        /// <summary>Coordinates DiggerZombieActions operations.</summary>
        private readonly DiggerZombieActions _diggerZombieActions;
        /// <summary>Coordinates EconomySystem operations.</summary>
        private readonly EconomySystem _economySystem;
        /// <summary>Coordinates ExplosionEffects operations.</summary>
        private readonly ExplosionEffects _explosionEffects;
        /// <summary>Coordinates ExplosivePlantActions operations.</summary>
        private readonly ExplosivePlantActions _explosivePlantActions;
        /// <summary>Coordinates GargantuarActions operations.</summary>
        private readonly GargantuarActions _gargantuarActions;
        /// <summary>Coordinates GraveSystem operations.</summary>
        private readonly GraveSystem _graveSystem;
        /// <summary>Coordinates IZombieMode operations.</summary>
        private readonly IZombieMode _iZombieMode;
        /// <summary>Coordinates IndependentModeRunner operations.</summary>
        private readonly IndependentModeRunner _independentModeRunner;
        /// <summary>Coordinates JackZombieActions operations.</summary>
        private readonly JackZombieActions _jackZombieActions;
        /// <summary>Coordinates LadderSystem operations.</summary>
        private readonly LadderSystem _ladderSystem;
        /// <summary>Coordinates LegacyCombatSystem operations.</summary>
        private readonly LegacyCombatSystem _legacyCombatSystem;
        /// <summary>Coordinates LegacyPlantActions operations.</summary>
        private readonly LegacyPlantActions _legacyPlantActions;
        /// <summary>Coordinates LegacySimulation operations.</summary>
        private readonly LegacySimulation _legacySimulation;
        /// <summary>Coordinates LevelOneModeRunner operations.</summary>
        private readonly LevelOneModeRunner _levelOneModeRunner;
        /// <summary>Coordinates LootSystem operations.</summary>
        private readonly LootSystem _lootSystem;
        /// <summary>Coordinates MagnetPlantActions operations.</summary>
        private readonly MagnetPlantActions _magnetPlantActions;
        /// <summary>Coordinates MatchMode operations.</summary>
        private readonly MatchMode _matchMode;
        /// <summary>Coordinates ModePlantActions operations.</summary>
        private readonly ModePlantActions _modePlantActions;
        /// <summary>Coordinates ModeZombieActions operations.</summary>
        private readonly ModeZombieActions _modeZombieActions;
        /// <summary>Coordinates MowerSystem operations.</summary>
        private readonly MowerSystem _mowerSystem;
        /// <summary>Coordinates PacketMode operations.</summary>
        private readonly PacketMode _packetMode;
        /// <summary>Coordinates PlantSystem operations.</summary>
        private readonly PlantSystem _plantSystem;
        /// <summary>Coordinates PlantTimingSystem operations.</summary>
        private readonly PlantTimingSystem _plantTimingSystem;
        /// <summary>Coordinates PlantingSystem operations.</summary>
        private readonly PlantingSystem _plantingSystem;
        /// <summary>Coordinates PogoZombieActions operations.</summary>
        private readonly PogoZombieActions _pogoZombieActions;
        /// <summary>Coordinates PortalMode operations.</summary>
        private readonly PortalMode _portalMode;
        /// <summary>Coordinates ProjectileSystem operations.</summary>
        private readonly ProjectileSystem _projectileSystem;
        /// <summary>Coordinates PurchaseSystem operations.</summary>
        private readonly PurchaseSystem _purchaseSystem;
        /// <summary>Coordinates RewardSystem operations.</summary>
        private readonly RewardSystem _rewardSystem;
        /// <summary>Coordinates SeedBankSystem operations.</summary>
        private readonly SeedBankSystem _seedBankSystem;
        /// <summary>Coordinates SimulationRandom operations.</summary>
        private readonly SimulationRandom _simulationRandom;
        /// <summary>Coordinates SimulationSession operations.</summary>
        private readonly SimulationSession _simulationSession;
        /// <summary>Coordinates SlotMode operations.</summary>
        private readonly SlotMode _slotMode;
        /// <summary>Coordinates SpecialPlantActions operations.</summary>
        private readonly SpecialPlantActions _specialPlantActions;
        /// <summary>Coordinates StarsMode operations.</summary>
        private readonly StarsMode _starsMode;
        /// <summary>Coordinates SunSystem operations.</summary>
        private readonly SunSystem _sunSystem;
        /// <summary>Coordinates UpgradePlantActions operations.</summary>
        private readonly UpgradePlantActions _upgradePlantActions;
        /// <summary>Coordinates VaseMode operations.</summary>
        private readonly VaseMode _vaseMode;
        /// <summary>Coordinates VaseSystem operations.</summary>
        private readonly VaseSystem _vaseSystem;
        /// <summary>Coordinates VaultZombieActions operations.</summary>
        private readonly VaultZombieActions _vaultZombieActions;
        /// <summary>Coordinates VehicleZombieActions operations.</summary>
        private readonly VehicleZombieActions _vehicleZombieActions;
        /// <summary>Coordinates WaterZombieActions operations.</summary>
        private readonly WaterZombieActions _waterZombieActions;
        /// <summary>Coordinates WaveSystem operations.</summary>
        private readonly WaveSystem _waveSystem;
        /// <summary>Coordinates WhackSystem operations.</summary>
        private readonly WhackSystem _whackSystem;
        /// <summary>Coordinates ZombieFeedbackSystem operations.</summary>
        private readonly ZombieFeedbackSystem _zombieFeedbackSystem;
        /// <summary>Coordinates ZombieLifecycle operations.</summary>
        private readonly ZombieLifecycle _zombieLifecycle;
        /// <summary>Coordinates ZombieSpawnSystem operations.</summary>
        private readonly ZombieSpawnSystem _zombieSpawnSystem;
        /// <summary>Coordinates ZombieSystem operations.</summary>
        private readonly ZombieSystem _zombieSystem;
        /// <summary>Creates and initializes the deterministic match.</summary>
        /// <param name="rules">The selected rules, or defaults.</param>
        /// <param name="randomSeed">The initial combat random seed.</param>
        /// <exception cref="ArgumentException">The mode options conflict.</exception>
        public GameSimulation(GameRules? rules = null, int randomSeed = 12345)
            : this(rules, randomSeed, null)
        {
        }

        /// <summary>Connects collaborators to a new or fully restored world without replaying initialization.</summary>
        /// <param name="rules">Rules for a new board.</param>
        /// <param name="randomSeed">The new board's seed.</param>
        /// <param name="restoredWorld">A detached verified checkpoint, or null for normal initialization.</param>
        private GameSimulation(GameRules? rules, int randomSeed, SimulationWorld? restoredWorld)
        {
            if (restoredWorld != null)
            {
                _world = restoredWorld;
                _world.RebuildViews();
            }
            _adventureModeRunner = new AdventureModeRunner(_world);
            _adventureSpecials = new AdventureSpecials(_world);
            _aquariumMode = new AquariumMode(_world);
            _balloonZombieActions = new BalloonZombieActions(_world);
            _battlePresentationSystem = new BattlePresentationSystem(_world);
            _biteSystem = new BiteSystem(_world);
            _boardEnvironment = new BoardEnvironment(_world);
            _boardQueries = new BoardQueries(_world);
            _bobsledZombieActions = new BobsledZombieActions(_world);
            _bossSystem = new BossSystem(_world);
            _bowlingSystem = new BowlingSystem(_world);
            _bungeeZombieActions = new BungeeZombieActions(_world);
            _coinSystem = new CoinSystem(_world);
            _coldSystem = new ColdSystem(_world);
            _combatSystem = new CombatSystem(_world);
            _conveyorSystem = new ConveyorSystem(_world);
            _dancerZombieActions = new DancerZombieActions(_world);
            _diggerZombieActions = new DiggerZombieActions(_world);
            _economySystem = new EconomySystem(_world);
            _explosionEffects = new ExplosionEffects(_world);
            _explosivePlantActions = new ExplosivePlantActions(_world);
            _gargantuarActions = new GargantuarActions(_world);
            _graveSystem = new GraveSystem(_world);
            _iZombieMode = new IZombieMode(_world);
            _independentModeRunner = new IndependentModeRunner(_world);
            _jackZombieActions = new JackZombieActions(_world);
            _ladderSystem = new LadderSystem(_world);
            _legacyCombatSystem = new LegacyCombatSystem(_world);
            _legacyPlantActions = new LegacyPlantActions(_world);
            _legacySimulation = new LegacySimulation(_world);
            _levelOneModeRunner = new LevelOneModeRunner(_world);
            _lootSystem = new LootSystem(_world);
            _magnetPlantActions = new MagnetPlantActions(_world);
            _matchMode = new MatchMode(_world);
            _modePlantActions = new ModePlantActions(_world);
            _modeZombieActions = new ModeZombieActions(_world);
            _mowerSystem = new MowerSystem(_world);
            _packetMode = new PacketMode(_world);
            _plantSystem = new PlantSystem(_world);
            _plantTimingSystem = new PlantTimingSystem(_world);
            _plantingSystem = new PlantingSystem(_world);
            _pogoZombieActions = new PogoZombieActions(_world);
            _portalMode = new PortalMode(_world);
            _projectileSystem = new ProjectileSystem(_world);
            _purchaseSystem = new PurchaseSystem(_world);
            _rewardSystem = new RewardSystem(_world);
            _seedBankSystem = new SeedBankSystem(_world);
            _simulationRandom = new SimulationRandom(_world);
            _simulationSession = new SimulationSession(_world);
            _slotMode = new SlotMode(_world);
            _specialPlantActions = new SpecialPlantActions(_world);
            _starsMode = new StarsMode(_world);
            _sunSystem = new SunSystem(_world);
            _upgradePlantActions = new UpgradePlantActions(_world);
            _vaseMode = new VaseMode(_world);
            _vaseSystem = new VaseSystem(_world);
            _vaultZombieActions = new VaultZombieActions(_world);
            _vehicleZombieActions = new VehicleZombieActions(_world);
            _waterZombieActions = new WaterZombieActions(_world);
            _waveSystem = new WaveSystem(_world);
            _whackSystem = new WhackSystem(_world);
            _zombieFeedbackSystem = new ZombieFeedbackSystem(_world);
            _zombieLifecycle = new ZombieLifecycle(_world);
            _zombieSpawnSystem = new ZombieSpawnSystem(_world);
            _zombieSystem = new ZombieSystem(_world);
            _adventureModeRunner.Connect(_adventureSpecials, _battlePresentationSystem, _boardEnvironment, _bossSystem, _coinSystem, _conveyorSystem, _economySystem, _graveSystem, _legacySimulation, _mowerSystem, _plantSystem, _plantingSystem, _projectileSystem, _purchaseSystem, _rewardSystem, _seedBankSystem, _sunSystem, _vaseSystem, _waveSystem, _whackSystem, _zombieSystem);
            _adventureSpecials.Connect(_adventureModeRunner, _bowlingSystem, _projectileSystem, _purchaseSystem, _sunSystem, _vaseSystem);
            _aquariumMode.Connect(_adventureModeRunner, _independentModeRunner, _legacySimulation, _sunSystem);
            _balloonZombieActions.Connect(_adventureModeRunner, _lootSystem, _vaultZombieActions, _zombieFeedbackSystem, _zombieLifecycle);
            _battlePresentationSystem.Connect(_adventureModeRunner, _biteSystem, _boardQueries, _coldSystem, _explosionEffects, _graveSystem, _plantingSystem, _projectileSystem, _specialPlantActions, _zombieFeedbackSystem);
            _biteSystem.Connect(_adventureModeRunner, _boardQueries, _explosionEffects, _explosivePlantActions, _ladderSystem, _legacySimulation, _plantTimingSystem, _plantingSystem, _rewardSystem, _specialPlantActions, _vaultZombieActions, _zombieFeedbackSystem);
            _boardEnvironment.Connect(_adventureModeRunner, _zombieFeedbackSystem);
            _boardQueries.Connect(_biteSystem, _combatSystem);
            _bobsledZombieActions.Connect(_adventureModeRunner, _combatSystem, _vaultZombieActions, _zombieFeedbackSystem);
            _bossSystem.Connect(_adventureModeRunner, _legacySimulation, _zombieSpawnSystem, _biteSystem, _zombieFeedbackSystem, _zombieLifecycle);
            _bowlingSystem.Connect(_adventureModeRunner, _biteSystem, _combatSystem, _explosionEffects, _independentModeRunner, _legacySimulation, _zombieFeedbackSystem);
            _bungeeZombieActions.Connect(_adventureModeRunner, _biteSystem, _boardQueries, _plantingSystem, _zombieFeedbackSystem, _zombieLifecycle, _zombieSpawnSystem);
            _coinSystem.Connect(_adventureModeRunner, _battlePresentationSystem, _boardQueries, _bossSystem, _combatSystem, _legacySimulation, _zombieFeedbackSystem);
            _coldSystem.Connect(_adventureModeRunner, _biteSystem, _combatSystem, _zombieFeedbackSystem);
            _combatSystem.Connect(_adventureModeRunner, _balloonZombieActions, _biteSystem, _explosionEffects, _ladderSystem, _legacySimulation, _vehicleZombieActions, _zombieFeedbackSystem, _zombieLifecycle);
            _conveyorSystem.Connect(_adventureModeRunner, _boardQueries);
            _dancerZombieActions.Connect(_adventureModeRunner, _biteSystem, _zombieFeedbackSystem, _zombieSpawnSystem);
            _diggerZombieActions.Connect(_biteSystem, _boardQueries, _zombieFeedbackSystem);
            _economySystem.Connect(_adventureModeRunner, _conveyorSystem, _sunSystem, _vaseSystem);
            _explosionEffects.Connect(_combatSystem);
            _explosivePlantActions.Connect(_adventureModeRunner, _bossSystem, _combatSystem, _explosionEffects, _legacySimulation, _plantingSystem, _zombieFeedbackSystem);
            _gargantuarActions.Connect(_adventureModeRunner, _biteSystem, _boardQueries, _combatSystem, _plantingSystem, _vaseMode, _vaseSystem, _vaultZombieActions, _zombieFeedbackSystem, _zombieSpawnSystem);
            _graveSystem.Connect(_adventureModeRunner, _plantingSystem);
            _iZombieMode.Connect(_adventureModeRunner, _boardQueries, _independentModeRunner, _legacySimulation, _plantingSystem, _seedBankSystem, _sunSystem, _zombieSpawnSystem);
            _independentModeRunner.Connect(_adventureModeRunner, _adventureSpecials, _aquariumMode, _battlePresentationSystem, _boardEnvironment, _boardQueries, _bossSystem, _bowlingSystem, _coinSystem, _conveyorSystem, _economySystem, _graveSystem, _iZombieMode, _legacySimulation, _matchMode, _modePlantActions, _modeZombieActions, _mowerSystem, _packetMode, _plantSystem, _plantingSystem, _portalMode, _projectileSystem, _purchaseSystem, _rewardSystem, _seedBankSystem, _slotMode, _starsMode, _sunSystem, _vaseMode, _vaseSystem, _waveSystem, _whackSystem, _zombieSystem);
            _jackZombieActions.Connect(_adventureModeRunner, _biteSystem, _combatSystem, _plantingSystem, _specialPlantActions, _vaseMode, _vaseSystem, _zombieFeedbackSystem, _zombieLifecycle);
            _ladderSystem.Connect(_adventureModeRunner, _biteSystem, _boardQueries, _vaultZombieActions, _zombieFeedbackSystem);
            _legacyCombatSystem.Connect(_legacySimulation, _projectileSystem);
            _legacyPlantActions.Connect(_boardQueries, _legacyCombatSystem, _legacySimulation, _projectileSystem, _sunSystem);
            _legacySimulation.Connect(_seedBankSystem, _adventureModeRunner, _boardEnvironment, _legacyCombatSystem, _legacyPlantActions, _levelOneModeRunner, _plantingSystem, _projectileSystem, _rewardSystem, _sunSystem);
            _levelOneModeRunner.Connect(_legacySimulation, _sunSystem);
            _lootSystem.Connect(_adventureModeRunner, _coinSystem, _sunSystem);
            _magnetPlantActions.Connect(_biteSystem, _diggerZombieActions, _pogoZombieActions, _zombieFeedbackSystem);
            _matchMode.Connect(_adventureModeRunner, _boardQueries, _independentModeRunner, _plantingSystem, _sunSystem);
            _modePlantActions.Connect(_projectileSystem);
            _modeZombieActions.Connect(_combatSystem, _independentModeRunner, _legacySimulation, _plantingSystem, _projectileSystem, _zombieLifecycle, _biteSystem, _boardQueries, _explosionEffects, _zombieFeedbackSystem);
            _mowerSystem.Connect(_adventureModeRunner, _zombieLifecycle);
            _packetMode.Connect(_adventureModeRunner, _boardQueries, _independentModeRunner, _vaseSystem);
            _plantSystem.Connect(_adventureModeRunner, _boardQueries, _coinSystem, _combatSystem, _explosionEffects, _explosivePlantActions, _lootSystem, _magnetPlantActions, _plantTimingSystem, _plantingSystem, _projectileSystem, _specialPlantActions, _sunSystem, _upgradePlantActions, _zombieFeedbackSystem);
            _plantTimingSystem.Connect(_adventureModeRunner, _biteSystem, _boardQueries, _explosivePlantActions, _projectileSystem, _specialPlantActions, _sunSystem, _zombieFeedbackSystem);
            _plantingSystem.Connect(_adventureModeRunner, _biteSystem, _boardEnvironment, _boardQueries, _conveyorSystem, _ladderSystem, _legacySimulation, _plantTimingSystem, _purchaseSystem, _seedBankSystem, _specialPlantActions, _upgradePlantActions, _zombieFeedbackSystem, _zombieLifecycle);
            _pogoZombieActions.Connect(_adventureModeRunner, _biteSystem, _zombieFeedbackSystem);
            _portalMode.Connect(_independentModeRunner);
            _projectileSystem.Connect(_adventureModeRunner, _battlePresentationSystem, _biteSystem, _boardQueries, _bossSystem, _coldSystem, _combatSystem, _legacySimulation, _upgradePlantActions, _vehicleZombieActions, _zombieFeedbackSystem);
            _purchaseSystem.Connect(_adventureModeRunner, _boardQueries, _combatSystem);
            _rewardSystem.Connect(_adventureModeRunner, _battlePresentationSystem, _boardEnvironment, _coinSystem, _projectileSystem, _sunSystem, _vaseSystem);
            _seedBankSystem.Connect(_adventureModeRunner, _boardQueries, _coinSystem, _conveyorSystem);
            _simulationSession.Connect(_adventureModeRunner, _levelOneModeRunner, _rewardSystem);
            _slotMode.Connect(_coinSystem, _independentModeRunner, _sunSystem, _vaseSystem);
            _specialPlantActions.Connect(_adventureModeRunner, _balloonZombieActions, _biteSystem, _boardQueries, _bossSystem, _coldSystem, _combatSystem, _explosionEffects, _ladderSystem, _legacySimulation, _plantingSystem, _waterZombieActions, _waveSystem, _zombieFeedbackSystem, _zombieLifecycle);
            _sunSystem.Connect(_adventureModeRunner, _legacySimulation, _levelOneModeRunner, _seedBankSystem);
            _upgradePlantActions.Connect(_adventureModeRunner, _boardQueries, _combatSystem, _legacySimulation, _plantTimingSystem, _plantingSystem, _projectileSystem, _seedBankSystem, _sunSystem, _zombieFeedbackSystem);
            _vaseMode.Connect(_adventureModeRunner, _independentModeRunner, _sunSystem, _vaseSystem, _zombieSpawnSystem);
            _vaseSystem.Connect(_adventureModeRunner, _conveyorSystem, _projectileSystem, _sunSystem, _vaseMode, _zombieSpawnSystem);
            _vaultZombieActions.Connect(_adventureModeRunner, _boardQueries, _zombieFeedbackSystem);
            _vehicleZombieActions.Connect(_adventureModeRunner, _biteSystem, _plantTimingSystem, _plantingSystem, _projectileSystem, _specialPlantActions, _zombieFeedbackSystem);
            _waterZombieActions.Connect(_adventureModeRunner, _biteSystem, _boardQueries, _coldSystem, _vaultZombieActions, _zombieFeedbackSystem, _zombieLifecycle);
            _waveSystem.Connect(_adventureModeRunner, _bungeeZombieActions, _waterZombieActions, _zombieSpawnSystem);
            _whackSystem.Connect(_adventureModeRunner, _graveSystem, _legacySimulation, _waterZombieActions, _waveSystem, _zombieFeedbackSystem, _zombieLifecycle, _zombieSpawnSystem);
            _zombieFeedbackSystem.Connect(_adventureModeRunner, _battlePresentationSystem, _biteSystem, _bobsledZombieActions, _pogoZombieActions, _vehicleZombieActions);
            _zombieLifecycle.Connect(_bungeeZombieActions, _legacySimulation, _lootSystem, _rewardSystem, _zombieFeedbackSystem);
            _zombieSpawnSystem.Connect(_adventureModeRunner, _bungeeZombieActions, _independentModeRunner, _zombieFeedbackSystem);
            _zombieSystem.Connect(_adventureModeRunner, _balloonZombieActions, _biteSystem, _boardEnvironment, _boardQueries, _bobsledZombieActions, _bungeeZombieActions, _combatSystem, _dancerZombieActions, _diggerZombieActions, _gargantuarActions, _jackZombieActions, _ladderSystem, _legacySimulation, _plantTimingSystem, _plantingSystem, _pogoZombieActions, _rewardSystem, _vaultZombieActions, _vehicleZombieActions, _waterZombieActions, _zombieFeedbackSystem, _zombieLifecycle);
            if (restoredWorld == null)
            {
                _simulationSession.Initialize(rules, randomSeed);
            }
            else
            {
                _world._rewardPresentationTick = _rewardSystem.AdvanceAdventureRewardTick;
                _world.ModeSession?.AttachRestoredGame(this);
            }
        }

        /// <summary>Captures all authoritative board state without invoking persistence callbacks.</summary>
        /// <returns>The complete replica checkpoint payload.</returns>
        internal byte[] CaptureCheckpoint()
        {
            return MatchCheckpointCodec.Capture(_world);
        }

        /// <summary>Gets the authoritative model inspected by the offline checkpoint editor.</summary>
        internal SimulationWorld CheckpointState => _world;

        /// <summary>Reads preparation targets without changing model values or random streams.</summary>
        /// <returns>The bounded live-population targets and short-lived event allowance.</returns>
        internal SimulationCapacityRequirements GetCapacityRequirements()
        {
            return new SimulationCapacityRequirements(Math.Max(128, Plants.Count + 32),
                Math.Max(256, Zombies.Count + Presentation.Corpses.Count + 64),
                Math.Max(128, Peas.Count + 64), 128, 16, soundEvents: SimulationCapacityRequirements.MaximumSoundEvents);
        }

        /// <summary>Gets current runtime record pool counters without allocating a report object.</summary>
        internal SimulationPoolStatistics RuntimePoolStatistics => new SimulationPoolStatistics(
            _world.LifetimePool.CreatedCount + _world._peaPool.CreatedCount + _world._sunPool.CreatedCount + _world._flamePool.CreatedCount,
            _world.LifetimePool.ActiveCount, _world.LifetimePool.RecycledCount);

        /// <summary>Prepares only the requested number of reusable objects during a loading frame.</summary>
        /// <param name="requirements">The high-water preparation targets.</param>
        /// <param name="maxObjects">The maximum number of objects constructed by this call.</param>
        /// <returns>Whether every requested reusable capacity is available.</returns>
        internal bool PrepareCapacityStep(in SimulationCapacityRequirements requirements, int maxObjects = 16)
        {
            if (maxObjects <= 0 || !_world.LifetimePool.PrepareCollectionsStep(_world, requirements))
            {
                return false;
            }
            var remaining = Math.Max(0, maxObjects);
            remaining -= _world._peaPool.PrepareCapacityStep(Math.Max(0, requirements.Projectiles - _world.Peas.Count), remaining);
            remaining -= _world._sunPool.PrepareCapacityStep(32, remaining);
            remaining -= _world._flamePool.PrepareCapacityStep(6, remaining);
            var modelComplete = _world.LifetimePool.PrepareCapacityStep(requirements, remaining);
            return modelComplete && _world._peaPool.AvailableCount + _world.Peas.Count >= requirements.Projectiles
                && _world._sunPool.AvailableCount >= 32 && _world._flamePool.AvailableCount >= 6;
        }

        /// <summary>Completes the original end-level coin cleanup when leaving during the award sequence.</summary>
        /// <returns>Whether pending coin and unused-mower account writes succeeded.</returns>
        internal bool FinishAdventureForSaving()
        {
            return _seedBankSystem.FinishAdventureForSaving(_world.ModeSession != null);
        }

        /// <summary>Creates a match from a complete detached checkpoint.</summary>
        /// <param name="payload">The validated replica checkpoint payload.</param>
        /// <returns>The restored match with freshly connected collaborators.</returns>
        internal static GameSimulation RestoreCheckpoint(byte[] payload)
        {
            return new GameSimulation(null, 0, MatchCheckpointCodec.Restore(payload));
        }
        /// <summary>Stores rows.</summary>
        public const int Rows = 5;
        /// <summary>Stores columns.</summary>
        public const int Columns = 9;
        /// <summary>Stores plants.</summary>
        public EntityView<Plant> Plants
        {
            get
            {
                return _world.Plants.View;
            }
        }
        /// <summary>Stores zombies.</summary>
        public EntityView<Zombie> Zombies
        {
            get
            {
                return _world.Zombies.View;
            }
        }
        /// <summary>Stores peas.</summary>
        public EntityView<Pea> Peas
        {
            get
            {
                return _world.PeasView;
            }
        }
        /// <summary>Stores suns.</summary>
        public EntityView<global::PlantsVsZombies.Simulation.Sun> Suns
        {
            get
            {
                return _world.SunsView;
            }
        }
        /// <summary>Stores mowers.</summary>
        public EntityView<Mower> Mowers
        {
            get
            {
                return _world.MowersView;
            }
        }
        /// <summary>Stores flames.</summary>
        public EntityView<LaneFlame> Flames
        {
            get
            {
                return _world.FlamesView;
            }
        }
        /// <summary>Stores rules.</summary>
        public GameRules Rules
        {
            get
            {
                return _world.Rules;
            }
        }
        /// <summary>Gets or sets state.</summary>
        public MatchState State
        {
            get
            {
                return _world.State;
            }
            private set
            {
                _world.State = value;
            }
        }
        /// <summary>Gets or sets sun amount.</summary>
        public int SunAmount
        {
            get
            {
                return _world.SunAmount;
            }
            private set
            {
                _world.SunAmount = value;
            }
        }
        /// <summary>Gets or sets spawned zombies.</summary>
        public int SpawnedZombies
        {
            get
            {
                return _world.SpawnedZombies;
            }
            private set
            {
                _world.SpawnedZombies = value;
            }
        }
        /// <summary>Gets or sets elapsed.</summary>
        public float Elapsed
        {
            get
            {
                return _world.Elapsed;
            }
            private set
            {
                _world.Elapsed = value;
            }
        }
        /// <summary>The mode selected at construction; changing the rules later does not switch modes.</summary>
        public bool IsExperimentalLevelOne
        {
            get
            {
                return _world.IsExperimentalLevelOne;
            }
            private set
            {
                _world.IsExperimentalLevelOne = value;
            }
        }
        /// <summary>Gets or sets is adventure.</summary>
        public bool IsAdventure
        {
            get
            {
                return _world.IsAdventure;
            }
            private set
            {
                _world.IsAdventure = value;
            }
        }
        /// <summary>Gets or sets row count.</summary>
        public int RowCount
        {
            get
            {
                return _world.RowCount;
            }
            private set
            {
                _world.RowCount = value;
            }
        }

        /// <summary>Stores event raised.</summary>
        public Action<GameEvent>? EventRaised
        {
            get
            {
                return _world.EventRaised;
            }
            set
            {
                _world.EventRaised = value;
            }
        }

        /// <summary>Performs cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting cost.</returns>
        public static int Cost(PlantKind kind)
        {
            return LegacySimulation.Cost(kind);
        }

        /// <summary>Performs recharge time.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting recharge time.</returns>
        public static float RechargeTime(PlantKind kind)
        {
            return LegacySimulation.RechargeTime(kind);
        }

        /// <summary>Performs cooldown remaining.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting cooldown remaining.</returns>
        public float CooldownRemaining(PlantKind kind)
        {
            return _legacySimulation.CooldownRemaining(kind);
        }

        /// <summary>Performs plant at.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting plant at.</returns>
        public Plant PlantAt(int row, int column)
        {
            return _legacySimulation.PlantAt(row, column);
        }

        /// <summary>Performs try plant.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try plant.</returns>
        public bool TryPlant(PlantKind kind, int row, int column)
        {
            return _legacySimulation.TryPlant(kind, row, column);
        }

        /// <summary>Performs remove plant.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting remove plant.</returns>
        public bool RemovePlant(int row, int column)
        {
            return _legacySimulation.RemovePlant(row, column);
        }

        /// <summary>Performs collect sun.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting collect sun.</returns>
        public bool CollectSun(global::PlantsVsZombies.Simulation.Sun sun)
        {
            return _sunSystem.CollectSun(sun);
        }

        /// <summary>Performs toggle pause.</summary>
        public void TogglePause()
        {
            _legacySimulation.TogglePause();
        }

        /// <summary>Performs update.</summary>
        /// <param name = "deltaTime">The delta time.</param>
        public void Update(float deltaTime)
        {
            _legacySimulation.Update(deltaTime);
            _world.LifetimePool.CompleteFrame(_world);
        }
        // Explicit notifications survive a headless tick or a host attaching before its first
        // Update. The host drains them at the corresponding existing Planted/Explosion event.
        /// <summary>Performs try take adventure cherry bomb sound.</summary>
        /// <param name = "trigger">The trigger.</param>
        /// <param name = "sound">The sound.</param>
        /// <returns>The resulting try take adventure cherry bomb sound.</returns>
        public bool TryTakeAdventureCherryBombSound(GameEvent trigger, out global::PlantsVsZombies.Adventure.AdventureCherryBombSound sound)
        {
            return _explosivePlantActions.TryTakeAdventureCherryBombSound(trigger, out sound);
        }

        /// <summary>Gets or sets uses native adventure conveyor.</summary>
        public bool UsesNativeAdventureConveyor
        {
            get
            {
                return _conveyorSystem.UsesNativeAdventureConveyor;
            }
        }

        /// <summary>Performs adventure conveyor card x.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting adventure conveyor card x.</returns>
        public float AdventureConveyorCardX(int index)
        {
            return _conveyorSystem.AdventureConveyorCardX(index);
        }

        /// <summary>Performs apply adventure chill.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "iceTrap">The ice trap.</param>
        /// <returns>The resulting apply adventure chill.</returns>
        public bool ApplyAdventureChill(Zombie z, bool iceTrap = false)
        {
            return _coldSystem.ApplyAdventureChill(z, iceTrap);
        }

        /// <summary>Performs apply adventure ice trap.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting apply adventure ice trap.</returns>
        public bool ApplyAdventureIceTrap(Zombie z)
        {
            return _coldSystem.ApplyAdventureIceTrap(z);
        }

        /// <summary>Performs adventure ice impact point.</summary>
        /// <param name = "impact">The impact.</param>
        /// <param name = "fraction">The fraction.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        public void AdventureIceImpactPoint(global::PlantsVsZombies.Adventure.AdventureImpact impact, float fraction, out float x, out float y)
        {
            _coldSystem.AdventureIceImpactPoint(impact, fraction, out x, out y);
        }
        /// <summary>Performs record adventure plant hit.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "nativeTicks">The native ticks.</param>
        public void RecordAdventurePlantHit(Plant plant, int nativeTicks = 25)
        {
            _plantTimingSystem.RecordAdventurePlantHit(plant, nativeTicks);
        }

        /// <summary>Gets or sets last adventure planting failure.</summary>
        public AdventurePlantingFailure LastAdventurePlantingFailure
        {
            get
            {
                return _world.LastAdventurePlantingFailure;
            }
            private set
            {
                _world.LastAdventurePlantingFailure = value;
            }
        }

        /// <summary>Performs adventure plant at.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "layer">The layer.</param>
        /// <returns>The resulting adventure plant at.</returns>
        public Plant AdventurePlantAt(int row, int column, PlantLayer layer)
        {
            return _boardQueries.AdventurePlantAt(row, column, layer);
        }

        /// <summary>Performs can plant adventure.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting can plant adventure.</returns>
        public AdventurePlantingFailure CanPlantAdventure(PlantKind kind, int row, int column)
        {
            return _plantingSystem.CanPlantAdventure(kind, row, column);
        }

        /// <summary>Performs try use adventure packet.</summary>
        /// <param name = "packetId">The packet id.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try use adventure packet.</returns>
        public bool TryUseAdventurePacket(int packetId, int row, int column)
        {
            return _plantingSystem.TryUseAdventurePacket(packetId, row, column);
        }

        /// <summary>Resolves PC 498E30's chooser warning using original recommendation and Last Stand admission rules.</summary>
        /// <param name="kind">The seed actually hovered, including the original Imitater packet identifier.</param>
        /// <param name="coffeeChosen">Whether coffee is already selected in this chooser.</param>
        /// <returns>The original LawnStrings warning key, or an empty string for a recommended seed.</returns>
        internal string AdventureChooserWarningKey(PlantKind kind, bool coffeeChosen)
        {
            return _plantingSystem.AdventureChooserWarningKey(kind, coffeeChosen);
        }

        /// <summary>Removes the plant selected by the native shovel hit test.</summary>
        /// <param name="plant">The highlighted tool target, or null when the pointer has no target.</param>
        /// <returns>Whether the target was present and could be removed from the playing board.</returns>
        public bool TryShovelAdventurePlant(Plant? plant)
        {
            return _plantingSystem.TryShovelAdventurePlant(plant);
        }

        /// <summary>Allocates Zomboss at PC CutScene::AnimateBoard's original entrance boundary.</summary>
        internal void BeginAdventureBossEntrance()
        {
            _bossSystem.InitializeAdventureBoss();
        }

        /// <summary>Advances only the original Zomboss entrance and its timed contacts while the opening owns the board.</summary>
        internal void AdvanceAdventureBossIntro()
        {
            _bossSystem.AdvanceAdventureBossIntro();
        }

        /// <summary>Applies the original storm state before the level introduction.</summary>
        internal void BeginAdventureStormIntro()
        {
            _boardEnvironment.BeginAdventureStormIntro();
        }

        /// <summary>Applies the original storm countdown when the introduction finishes.</summary>
        internal void BeginAdventureStormPlaying()
        {
            _boardEnvironment.BeginAdventureStormPlaying();
        }

        /// <summary>Advances the original storm challenge after the board objects for one native update.</summary>
        /// <param name="intro">Whether the native scene is still LevelIntro.</param>
        /// <param name="paused">Whether to freeze rain and board counters while allowing the flash to settle.</param>
        internal void AdvanceAdventureStormTick(bool intro = false, bool paused = false)
        {
            _boardEnvironment.AdvanceAdventureStormTick(intro, paused);
        }

        /// <summary>Continues native storm updates while the regular simulation is suspended.</summary>
        /// <param name="delta">The elapsed presentation time in seconds.</param>
        /// <param name="paused">Whether the board is paused rather than showing the loss sequence.</param>
        /// <param name="intro">Whether the suspended board is in its introduction.</param>
        internal void AdvanceAdventureStormPresentation(float delta, bool paused, bool intro = false)
        {
            _boardEnvironment.AdvanceAdventureStormPresentation(delta, paused, intro);
        }

        /// <summary>Gets whether the original stage uses fog, excluding Stormy Night and Invisighoul.</summary>
        internal bool AdventureHasFog
        {
            get
            {
                return _boardEnvironment.AdventureHasFog;
            }
        }

        /// <summary>Gets the original stage-dependent first fog column.</summary>
        internal int AdventureFogLeftColumn
        {
            get
            {
                return _boardEnvironment.AdventureFogLeftColumn;
            }
        }

        /// <summary>Starts the original off-lawn fog state before the street preview and seed chooser.</summary>
        internal void BeginAdventureFogIntro()
        {
            _boardEnvironment.BeginAdventureFogIntro();
        }

        /// <summary>Advances one original Board fog update and its playing or opening countdown.</summary>
        /// <param name="intro">Whether the level opening owns the countdown instead of gameplay.</param>
        /// <param name="rollIn">Whether the opening has reached TimeFogRollIn.</param>
        internal void AdvanceAdventureFog(bool intro = false, bool rollIn = false)
        {
            _boardEnvironment.AdvanceAdventureFog(intro, rollIn);
        }

        /// <summary>Performs is adventure cell visible.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting is adventure cell visible.</returns>
        public bool IsAdventureCellVisible(int row, int column)
        {
            return _boardEnvironment.IsAdventureCellVisible(row, column);
        }

        /// <summary>Gets or sets adventure presentation remainder.</summary>
        public float AdventurePresentationRemainder
        {
            get
            {
                return _battlePresentationSystem.AdventurePresentationRemainder;
            }
        }

        /// <summary>Gets or sets presentation.</summary>
        public global::PlantsVsZombies.Adventure.AdventurePresentationState Presentation
        {
            get
            {
                return _world.Presentation;
            }
            private set
            {
                _world.Presentation = value;
            }
        }

        /// <summary>Applies PC 54E470's independent four-hundred-update butter immobilization without ice or chill.</summary>
        /// <param name="zombie">The actual projectile victim whose original butter admission is checked.</param>
        /// <returns>Whether the original butter state was applied.</returns>
        internal bool ApplyAdventureButter(Zombie zombie)
        {
            return _battlePresentationSystem.ApplyAdventureButter(zombie);
        }

        // 88 ascending updates to -704, then 84 descending updates until Z > -40.
        /// <summary>Stores adventure presentation cob flight seconds.</summary>
        public const float AdventurePresentationCobFlightSeconds = 1.72f;

        /// <summary>Performs bank adventure coins.</summary>
        /// <returns>The resulting bank adventure coins.</returns>
        public bool BankAdventureCoins()
        {
            return _seedBankSystem.BankAdventureCoins();
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
            return _seedBankSystem.AdventurePacketContext(kind, playing, darken, anticipatedSun, destination);
        }
        /// <summary>Gets the adventure board, which exists exactly when <see cref="IsAdventure"/> is true.</summary>
        /// <exception cref="InvalidOperationException">This simulation does not run an adventure level.</exception>
        public AdventureBoard Adventure
        {
            get
            {
                return _adventureModeRunner.Adventure;
            }
        }

        /// <summary>Performs choose adventure seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <returns>The resulting choose adventure seeds.</returns>
        public bool ChooseAdventureSeeds(IEnumerable<PlantKind>? seeds)
        {
            return _adventureModeRunner.ChooseAdventureSeeds(seeds);
        }

        /// <summary>Pure validation for hosts that must persist a selection before publishing gameplay.</summary>
        public bool CanChooseAdventureSeeds(IEnumerable<PlantKind> seeds)
        {
            return _adventureModeRunner.CanChooseAdventureSeeds(seeds);
        }

        /// <summary>Performs choose default adventure seeds.</summary>
        /// <returns>The resulting choose default adventure seeds.</returns>
        public bool ChooseDefaultAdventureSeeds()
        {
            return _adventureModeRunner.ChooseDefaultAdventureSeeds();
        }

        /// <summary>Performs select adventure seed.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting select adventure seed.</returns>
        public bool SelectAdventureSeed(PlantKind kind)
        {
            return _adventureModeRunner.SelectAdventureSeed(kind);
        }

        /// <summary>Performs acknowledge adventure tutorial.</summary>
        /// <returns>The resulting acknowledge adventure tutorial.</returns>
        public bool AcknowledgeAdventureTutorial()
        {
            return _adventureModeRunner.AcknowledgeAdventureTutorial();
        }

        /// <summary>Performs collect adventure reward.</summary>
        /// <returns>The resulting collect adventure reward.</returns>
        public bool CollectAdventureReward()
        {
            return _adventureModeRunner.CollectAdventureReward();
        }

        /// <summary>Advances the PC outer-board PoolEffect counter before gameplay objects update.</summary>
        /// <param name="survivalRepick">Whether PC CutScene::IsSurvivalRepick (0x446350) suppresses the water update.</param>
        internal void AdvanceAdventurePoolEffect(bool survivalRepick = false)
        {
            _boardEnvironment.AdvanceAdventurePoolEffect(survivalRepick);
        }

        /// <summary>Advances only the original water clock while gameplay is held by a chooser or preparation UI.</summary>
        /// <param name="seconds">The elapsed unpaused board presentation time.</param>
        /// <param name="survivalRepick">Whether a survival stage is returning to seed selection.</param>
        internal void AdvanceAdventurePoolPresentation(float seconds, bool survivalRepick = false)
        {
            _boardEnvironment.AdvanceAdventurePoolPresentation(seconds, survivalRepick);
        }

        /// <summary>Advances the original grid-item emergence counter during introduction or battle.</summary>
        internal void AdvanceAdventureGraves()
        {
            _graveSystem.AdvanceAdventureGraves();
        }

        /// <summary>Collects a live vase seed coin for the normal cursor.</summary>
        /// <param name="packetId">The identifier of the usable seed coin beneath the pointer.</param>
        /// <returns>Whether a live, unheld vase seed was collected while the board was playing.</returns>
        internal bool CollectVasePacket(int packetId)
        {
            return _vaseSystem.CollectVasePacket(packetId);
        }

        /// <summary>Returns a selected usable seed to its original drop position after cursor cancellation.</summary>
        /// <param name="packetId">The identifier of the vase seed coin previously held by the cursor.</param>
        internal void DropVasePacket(int packetId)
        {
            _vaseSystem.DropVasePacket(packetId);
        }

        /// <summary>Strikes the center of a zombie's hammer hit rectangle for simulation-only callers.</summary>
        /// <param name="zombieId">The live zombie to strike.</param>
        /// <returns>Whether the hammer strike was accepted.</returns>
        public bool HammerAdventureZombie(int zombieId)
        {
            return _whackSystem.HammerAdventureZombie(zombieId);
        }

        /// <summary>Applies the original helmet damage or immediate lethal hammer strike at a board click.</summary>
        /// <param name="zombieId">The live zombie selected by the hammer hit test.</param>
        /// <param name="hitX">The original board-pixel click x coordinate.</param>
        /// <param name="hitY">The original board-pixel click y coordinate.</param>
        /// <returns>Whether the hammer strike was accepted.</returns>
        public bool HammerAdventureZombie(int zombieId, float hitX, float hitY)
        {
            return _whackSystem.HammerAdventureZombie(zombieId, hitX, hitY);
        }

        /// <summary>Starts the original delayed hammer swing over an adventure vase.</summary>
        /// <param name="row">The target vase's lawn row.</param>
        /// <param name="column">The target vase's lawn column.</param>
        /// <returns>Whether an available vase accepted a new hammer swing.</returns>
        public bool SmashAdventureVase(int row, int column)
        {
            return _vaseSystem.SmashAdventureVase(row, column);
        }

        /// <summary>Populates the next adventure vase stage at the original dialogue entry.</summary>
        /// <param name="messageIndex">The original Crazy Dave message being presented.</param>
        /// <returns>Whether this entry populated the next stage.</returns>
        public bool PopulateAdventureVaseInterlude(int messageIndex)
        {
            return _adventureSpecials.PopulateAdventureVaseInterlude(messageIndex);
        }

        /// <summary>Finishes the vase transition after Dave has departed.</summary>
        /// <returns>Whether an already populated transition was finished.</returns>
        public bool FinishAdventureVaseInterlude()
        {
            return _adventureSpecials.FinishAdventureVaseInterlude();
        }
        /// <summary>Gets or sets adventure sun available.</summary>
        public int AdventureSunAvailable
        {
            get
            {
                return _seedBankSystem.AdventureSunAvailable;
            }
        }

        /// <summary>Performs set adventure imitater target.</summary>
        /// <param name = "target">The target.</param>
        /// <returns>The resulting set adventure imitater target.</returns>
        public bool SetAdventureImitaterTarget(PlantKind target)
        {
            return _seedBankSystem.SetAdventureImitaterTarget(target);
        }

        /// <summary>Performs adventure seed cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure seed cost.</returns>
        public int AdventureSeedCost(PlantKind kind)
        {
            return _seedBankSystem.AdventureSeedCost(kind);
        }

        /// <summary>Performs adventure seed recharge time.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure seed recharge time.</returns>
        public float AdventureSeedRechargeTime(PlantKind kind)
        {
            return _seedBankSystem.AdventureSeedRechargeTime(kind);
        }

        // Native counters are interpreted at the replica's 100 Hz. Animation/loading/flight durations are approximations.
        /// <summary>Stores adventure cob firing seconds.</summary>
        public const float AdventureCobFiringSeconds = AdventureCobCannonRules.FiringSeconds;
        /// <summary>Stores adventure cob loading seconds.</summary>
        public const float AdventureCobLoadingSeconds = AdventureCobCannonRules.LoadingSeconds;
        /// <summary>Stores adventure cob flight seconds.</summary>
        public const float AdventureCobFlightSeconds = AdventurePresentationCobFlightSeconds;

        /// <summary>Performs try aim adventure cob cannon.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try aim adventure cob cannon.</returns>
        public bool TryAimAdventureCobCannon(int row, int column)
        {
            return _upgradePlantActions.TryAimAdventureCobCannon(row, column);
        }

        /// <summary>Performs try aim adventure cob cannon.</summary>
        /// <param name = "plantId">The plant id.</param>
        /// <returns>The resulting try aim adventure cob cannon.</returns>
        public bool TryAimAdventureCobCannon(int plantId)
        {
            return _upgradePlantActions.TryAimAdventureCobCannon(plantId);
        }

        /// <summary>Performs cancel adventure cob cannon aim.</summary>
        /// <returns>The resulting cancel adventure cob cannon aim.</returns>
        public bool CancelAdventureCobCannonAim()
        {
            return _upgradePlantActions.CancelAdventureCobCannonAim();
        }

        /// <summary>Performs try fire adventure cob cannon.</summary>
        /// <param name = "targetX">The target x.</param>
        /// <param name = "targetRow">The target row.</param>
        /// <returns>The resulting try fire adventure cob cannon.</returns>
        public bool TryFireAdventureCobCannon(float targetX, float targetRow)
        {
            return _upgradePlantActions.TryFireAdventureCobCannon(targetX, targetRow);
        }

        /// <summary>Performs try fire adventure cob cannon at cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try fire adventure cob cannon at cell.</returns>
        public bool TryFireAdventureCobCannonAtCell(int row, int column)
        {
            return _upgradePlantActions.TryFireAdventureCobCannonAtCell(row, column);
        }

        /// <summary>Creates a board pickup using the original currency units and logical pixel position.</summary>
        /// <param name="kind">The currency or loot type to create.</param>
        /// <param name="x">The pickup's horizontal lawn coordinate.</param>
        /// <param name="row">The pickup's lawn row.</param>
        /// <returns>The new pickup, or null when the board or coordinates cannot accept it.</returns>
        public AdventureCoin? SpawnAdventureCoin(AdventureCoinKind kind, float x, float row)
        {
            return _coinSystem.SpawnAdventureCoin(kind, x, row);
        }

        /// <summary>Calls <see cref="SpawnAdventureCoin"/> where a result must exist, failing with a descriptive exception instead of returning null.</summary>
        /// <param name="kind">The <c>kind</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <param name="x">The <c>x</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <param name="row">The <c>row</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <returns>The non-null spawn adventure coin result.</returns>
        /// <exception cref="System.InvalidOperationException">The method returned no result.</exception>
        public AdventureCoin RequiredSpawnAdventureCoin(AdventureCoinKind kind, float x, float row)
        {
            return _coinSystem.RequiredSpawnAdventureCoin(kind, x, row);
        }

        /// <summary>Starts the original manual money collection flight or commits noncurrency loot.</summary>
        /// <param name="coin">The live pickup clicked by the player.</param>
        /// <returns>Whether collection began; currency is persisted and credited when its flight arrives.</returns>
        public bool CollectAdventureCoin(AdventureCoin? coin)
        {
            return _coinSystem.CollectAdventureCoin(coin);
        }

        /// <summary>Performs adventure zombie attack rectangle.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie attack rectangle.</returns>
        public NativeRect AdventureZombieAttackRectangle(Zombie z)
        {
            return _biteSystem.AdventureZombieAttackRectangle(z);
        }

        /// <summary>Performs adventure plant eating rectangle.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting adventure plant eating rectangle.</returns>
        public NativeRect AdventurePlantEatingRectangle(Plant p)
        {
            return _biteSystem.AdventurePlantEatingRectangle(p);
        }

        /// <summary>Performs adventure zombie presentation asset.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie presentation asset.</returns>
        public static string AdventureZombiePresentationAsset(Zombie z)
        {
            return ZombieFeedbackSystem.AdventureZombiePresentationAsset(z);
        }

        /// <summary>Performs adventure presentation zombie animation.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure presentation zombie animation.</returns>
        public string AdventurePresentationZombieAnimation(Zombie z)
        {
            return _zombieFeedbackSystem.AdventurePresentationZombieAnimation(z);
        }

        /// <summary>Performs adventure presentation zombie rate.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure presentation zombie rate.</returns>
        public float AdventurePresentationZombieRate(Zombie z)
        {
            return _zombieFeedbackSystem.AdventurePresentationZombieRate(z);
        }

        /// <summary>Checks the original cell-based ladder independently of the targeted plant layer.</summary>
        /// <param name="row">The native board row.</param>
        /// <param name="column">The native board column.</param>
        /// <returns>Whether any surviving plant in this cell retains the placed ladder.</returns>
        internal bool AdventureHasLadderAt(int row, int column)
        {
            return _ladderSystem.AdventureHasLadderAt(row, column);
        }
        /// <summary>Captured at construction; always false outside experimental 1-1.</summary>
        public bool LevelOneUsesNativeSkySunRules
        {
            get
            {
                return _levelOneModeRunner.LevelOneUsesNativeSkySunRules;
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
                return _levelOneModeRunner.LevelOneNativeSkySuns;
            }
        }

        /// <summary>Gets or sets level one native sky sun countdown ticks.</summary>
        public int? LevelOneNativeSkySunCountdownTicks
        {
            get
            {
                return _levelOneModeRunner.LevelOneNativeSkySunCountdownTicks;
            }
        }

        /// <summary>Gets or sets level one native sky sun drop count.</summary>
        public int? LevelOneNativeSkySunDropCount
        {
            get
            {
                return _levelOneModeRunner.LevelOneNativeSkySunDropCount;
            }
        }

        /// <summary>Tests one live object in native pixels; it does not select between overlapping objects.</summary>
        public bool HitTestLevelOneNativeSkySun(LevelOneNativeSkySun sun, int pixelX, int pixelY)
        {
            return _levelOneModeRunner.HitTestLevelOneNativeSkySun(sun, pixelX, pixelY);
        }

        /// <summary>
        /// Begins flight for the selected object. Credit and SunCollected occur
        /// only when a later effective native update reaches the target.
        /// </summary>
        public bool TryClickLevelOneNativeSkySun(LevelOneNativeSkySun sun, int pixelX, int pixelY, int button = 0)
        {
            return _levelOneModeRunner.TryClickLevelOneNativeSkySun(sun, pixelX, pixelY, button);
        }

        /// <summary>Mode detection for the unsodded background and single-row grass.</summary>
        public bool LevelOneUsesSingleRowLawn
        {
            get
            {
                return _levelOneModeRunner.LevelOneUsesSingleRowLawn;
            }
        }

        /// <summary>Null outside this experiment; only native tutorial states 0-4 are modeled.</summary>
        public LevelOneTutorialStage? CurrentLevelOneTutorialStage
        {
            get
            {
                return _levelOneModeRunner.CurrentLevelOneTutorialStage;
            }
        }

        /// <summary>Gets or sets level one selected seed.</summary>
        public PlantKind? LevelOneSelectedSeed
        {
            get
            {
                return _levelOneModeRunner.LevelOneSelectedSeed;
            }
        }

        /// <summary>Only the verified tutorial gate, not every reason a wave can be delayed.</summary>
        public bool LevelOneTutorialBlocksWaveCountdown
        {
            get
            {
                return _levelOneModeRunner.LevelOneTutorialBlocksWaveCountdown;
            }
        }

        /// <summary>Gets or sets level one waves dispatched.</summary>
        public int LevelOneWavesDispatched
        {
            get
            {
                return _world.LevelOneWavesDispatched;
            }
            private set
            {
                _world.LevelOneWavesDispatched = value;
            }
        }

        /// <summary>Gets or sets level one total waves.</summary>
        public int LevelOneTotalWaves
        {
            get
            {
                return _levelOneModeRunner.LevelOneTotalWaves;
            }
        }

        /// <summary>Gets or sets level one total zombies.</summary>
        public int LevelOneTotalZombies
        {
            get
            {
                return _levelOneModeRunner.LevelOneTotalZombies;
            }
        }

        /// <summary>Coarse dispatched-batch fraction, not the original health-based HUD meter.</summary>
        public float LevelOneWaveProgress
        {
            get
            {
                return _levelOneModeRunner.LevelOneWaveProgress;
            }
        }

        /// <summary>Approximate scheduler time; null when inactive or all batches are dispatched.</summary>
        public double? LevelOneWaveCountdownSeconds
        {
            get
            {
                return _levelOneModeRunner.LevelOneWaveCountdownSeconds;
            }
        }

        /// <summary>Uses the same zero-based row, column order as TryPlant.</summary>
        public bool IsLevelOnePlantingCell(int row, int column)
        {
            return _levelOneModeRunner.IsLevelOnePlantingCell(row, column);
        }

        /// <summary>Checks experimental card selection without changing state.</summary>
        public bool CanSelectLevelOneSeed(PlantKind kind)
        {
            return _levelOneModeRunner.CanSelectLevelOneSeed(kind);
        }

        /// <summary>Checks current experimental placement conditions without planting.</summary>
        public bool CanPlantLevelOneSeedAt(int row, int column)
        {
            return _levelOneModeRunner.CanPlantLevelOneSeedAt(row, column);
        }

        /// <summary>
        /// Selects the only available seed when the card is usable. The host
        /// must call this before TryPlant in the experimental mode.
        /// </summary>
        public bool TrySelectLevelOneSeed(PlantKind kind)
        {
            return _levelOneModeRunner.TrySelectLevelOneSeed(kind);
        }

        /// <summary>Cancel a held experimental seed; returns false if none is held or play is paused.</summary>
        public bool CancelLevelOneSeedSelection()
        {
            return _levelOneModeRunner.CancelLevelOneSeedSelection();
        }

        /// <summary>Performs click mode match cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting click mode match cell.</returns>
        public bool ClickModeMatchCell(int row, int column)
        {
            return _matchMode.ClickModeMatchCell(row, column);
        }

        /// <param name = "action">0 repeater, 1 fume-shroom, 2 tall-nut, 3 shuffle, 4 fill a crater.</param>
        public bool BuyModeMatchAction(int action)
        {
            return _matchMode.BuyModeMatchAction(action);
        }

        /// <summary>Performs spin mode slots.</summary>
        /// <returns>The resulting spin mode slots.</returns>
        public bool SpinModeSlots()
        {
            return _slotMode.SpinModeSlots();
        }

        /// <summary>Performs buy mode fish.</summary>
        /// <returns>The resulting buy mode fish.</returns>
        public bool BuyModeFish()
        {
            return _aquariumMode.BuyModeFish();
        }

        /// <summary>Performs feed mode fish.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <returns>The resulting feed mode fish.</returns>
        public bool FeedModeFish(float x, float y)
        {
            return _aquariumMode.FeedModeFish(x, y);
        }

        /// <summary>Performs buy mode aquarium trophy.</summary>
        /// <returns>The resulting buy mode aquarium trophy.</returns>
        public bool BuyModeAquariumTrophy()
        {
            return _aquariumMode.BuyModeAquariumTrophy();
        }

        /// <summary>Performs is mode zombie visible.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <returns>The resulting is mode zombie visible.</returns>
        public bool IsModeZombieVisible(Zombie zombie)
        {
            return _modeZombieActions.IsModeZombieVisible(zombie);
        }

        /// <summary>Gets or sets mode session.</summary>
        public ModeSession? ModeSession
        {
            get
            {
                return _world.ModeSession;
            }
            private set
            {
                _world.ModeSession = value;
            }
        }

        /// <summary>Gets the mode session, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public ModeSession RequiredModeSession
        {
            get
            {
                return _independentModeRunner.RequiredModeSession;
            }
        }
        /// <summary>Gets or sets is independent mode.</summary>
        public bool IsIndependentMode
        {
            get
            {
                return _independentModeRunner.IsIndependentMode;
            }
        }

        /// <summary>Gets or sets mode simulation remainder.</summary>
        public float ModeSimulationRemainder
        {
            get
            {
                return _independentModeRunner.ModeSimulationRemainder;
            }
        }

        /// <summary>Performs configure mode.</summary>
        /// <param name = "session">The session.</param>
        /// <param name = "access">The access.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void ConfigureMode(ModeSession session, AdventureSeedAccess? access)
        {
            _independentModeRunner.ConfigureMode(session, access);
        }

        /// <summary>Performs choose mode seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <returns>The resulting choose mode seeds.</returns>
        public bool ChooseModeSeeds(IEnumerable<PlantKind> seeds)
        {
            return _independentModeRunner.ChooseModeSeeds(seeds);
        }

        /// <summary>Performs apply mode purchases.</summary>
        /// <param name = "firstAid">The first aid.</param>
        /// <param name = "placeRake">The place rake.</param>
        public void ApplyModePurchases(bool firstAid, Func<bool>? placeRake)
        {
            _independentModeRunner.ApplyModePurchases(firstAid, placeRake);
        }

        /// <summary>Performs set mode imitater target.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting set mode imitater target.</returns>
        public bool SetModeImitaterTarget(PlantKind kind)
        {
            return _independentModeRunner.SetModeImitaterTarget(kind);
        }

        /// <summary>Performs plant mode vase cherry.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting plant mode vase cherry.</returns>
        public bool PlantModeVaseCherry(int row, int column)
        {
            return _independentModeRunner.PlantModeVaseCherry(row, column);
        }

        /// <summary>Performs begin mode defense.</summary>
        /// <returns>The resulting begin mode defense.</returns>
        public bool BeginModeDefense()
        {
            return _independentModeRunner.BeginModeDefense();
        }

        /// <summary>Performs update mode.</summary>
        /// <param name = "dt">The dt.</param>
        public void UpdateMode(float dt)
        {
            _independentModeRunner.UpdateMode(dt);
            _world.LifetimePool.CompleteFrame(_world);
        }

        /// <summary>Performs continue mode stage.</summary>
        /// <returns>The resulting continue mode stage.</returns>
        public bool ContinueModeStage()
        {
            return _independentModeRunner.ContinueModeStage();
        }

        /// <summary>Performs try plant mode.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try plant mode.</returns>
        public bool TryPlantMode(PlantKind kind, int row, int column)
        {
            return _independentModeRunner.TryPlantMode(kind, row, column);
        }

        /// <summary>Performs try use mode packet.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try use mode packet.</returns>
        public bool TryUseModePacket(int id, int row, int column)
        {
            return _independentModeRunner.TryUseModePacket(id, row, column);
        }

        /// <summary>Selects the original introductory advice for the current independent mode.</summary>
        /// <returns>A bracketed original LawnStrings key, or an empty string when no original advice is displayed.</returns>
        public string ModeInstruction()
        {
            return _independentModeRunner.ModeInstruction();
        }

        /// <summary>Performs is mode star cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "col">The col.</param>
        /// <returns>The resulting is mode star cell.</returns>
        public static bool IsModeStarCell(int row, int col)
        {
            return StarsMode.IsModeStarCell(row, col);
        }

        /// <summary>Performs mode star count.</summary>
        /// <returns>The resulting mode star count.</returns>
        public int ModeStarCount()
        {
            return _starsMode.ModeStarCount();
        }

        /// <summary>Starts the original delayed hammer swing over a standalone puzzle vase.</summary>
        /// <param name="row">The target vase's lawn row.</param>
        /// <param name="column">The target vase's lawn column.</param>
        /// <returns>Whether an available vase accepted a new hammer swing.</returns>
        public bool SmashModeVase(int row, int column)
        {
            return _vaseMode.SmashModeVase(row, column);
        }

        /// <summary>Gets or sets mode izombie columns.</summary>
        internal int ModeIZombieColumns
        {
            get
            {
                return _iZombieMode.ModeIZombieColumns;
            }
        }

        /// <summary>Performs mode zombie cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting mode zombie cost.</returns>
        public static int ModeZombieCost(ZombieKind kind)
        {
            return IZombieMode.ModeZombieCost(kind);
        }

        /// <summary>Performs place mode zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting place mode zombie.</returns>
        public bool PlaceModeZombie(ZombieKind kind, int row, int column)
        {
            return _iZombieMode.PlaceModeZombie(kind, row, column);
        }

        /// <summary>Advances the original feedback clock for one native opening or dialogue update.</summary>
        internal void AdvanceNativeFeedbackClock()
        {
            _simulationRandom.AdvanceNativeFeedbackClock();
        }

        /// <summary>Stops an original voice at an explicit UI action boundary.</summary>
        /// <param name="foley">The original category being stopped.</param>
        internal void StopAdventureFoley(int foley)
        {
            _simulationRandom.StopAdventureFoley(foley);
        }

        /// <summary>Consumes a shared native integer draw at an explicit opening or UI action boundary.</summary>
        /// <param name="maximum">The original exclusive bound.</param>
        /// <returns>The next original remainder-selected integer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The exclusive bound is negative.</exception>
        internal int DrawAdventureRandomInt(int maximum)
        {
            return _simulationRandom.DrawAdventureRandomInt(maximum);
        }

        /// <summary>Consumes a shared native scaled float at an explicit opening action boundary.</summary>
        /// <param name="scale">The original single-precision scale.</param>
        /// <returns>The original scaled float result.</returns>
        internal float DrawAdventureRandomFloat(float scale)
        {
            return _simulationRandom.DrawAdventureRandomFloat(scale);
        }

        /// <summary>Resolves an explicit original UI Foley request without a separately seeded audio stream.</summary>
        /// <param name="sound">The request receiving immutable native sample and pitch choices.</param>
        internal void ResolveAdventureFoley(AdventureZombieSoundEvent sound)
        {
            _simulationRandom.ResolveAdventureFoley(sound);
        }

        /// <summary>Gets or sets battle random draw count.</summary>
        public ulong BattleRandomDrawCount
        {
            get
            {
                return _simulationRandom.BattleRandomDrawCount;
            }
        }

        /// <summary>Performs capture battle random state.</summary>
        /// <returns>The resulting capture battle random state.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public OriginalRandomState CaptureBattleRandomState()
        {
            return _simulationRandom.CaptureBattleRandomState();
        }

        /// <summary>Performs restore battle random state.</summary>
        /// <param name = "snapshot">The snapshot.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void RestoreBattleRandomState(OriginalRandomState snapshot)
        {
            _simulationRandom.RestoreBattleRandomState(snapshot);
        }

        /// <summary>Gets or sets wave schedule.</summary>
        public global::PlantsVsZombies.Waves.AdventureWaveScheduleStatus? WaveSchedule
        {
            get
            {
                return _world.WaveSchedule;
            }
            private set
            {
                _world.WaveSchedule = value;
            }
        }

        /// <summary>Gets the wave schedule, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public global::PlantsVsZombies.Waves.AdventureWaveScheduleStatus RequiredWaveSchedule
        {
            get
            {
                return _waveSystem.RequiredWaveSchedule;
            }
        }

        /// <summary>Stores wave schedule event raised.</summary>
        public Action<global::PlantsVsZombies.Waves.WaveScheduleEvent>? WaveScheduleEventRaised
        {
            get
            {
                return _world.WaveScheduleEventRaised;
            }
            set
            {
                _world.WaveScheduleEventRaised = value;
            }
        }
        /// <summary>Performs notify adventure ice trap.</summary>
        /// <param name = "ticks">The ticks.</param>
        public void NotifyAdventureIceTrap(int ticks = 300)
        {
            _waveSystem.NotifyAdventureIceTrap(ticks);
        }

        /// <summary>After replacing Adventure.Waves/current wave, reset only the scheduler. It preserves CurrentWave and FlagsSpawned.</summary>
        public void ResetAdventureWaveSchedule(int firstCountdownTicks = -1, bool includeFinalAmbush = true, bool honorAdventureTutorials = false)
        {
            _waveSystem.ResetAdventureWaveSchedule(firstCountdownTicks, includeFinalAmbush, honorAdventureTutorials);
        }

        /// <summary>Performs advance scheduled adventure waves.</summary>
        /// <param name = "dt">The dt.</param>
        public void AdvanceScheduledAdventureWaves(float dt)
        {
            _waveSystem.AdvanceScheduledAdventureWaves(dt);
        }
    }
}
