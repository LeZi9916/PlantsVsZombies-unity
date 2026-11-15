#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Supplies the explicit capabilities used by native zombie actions.</summary>
    internal sealed class ZombieBehaviorContext
    {
        /// <summary>Provides AdventureModeRunner operations.</summary>
        internal readonly AdventureModeRunner _adventureModeRunner;
        /// <summary>Provides BalloonZombieActions operations.</summary>
        internal readonly BalloonZombieActions _balloonZombieActions;
        /// <summary>Provides BiteSystem operations.</summary>
        internal readonly BiteSystem _biteSystem;
        /// <summary>Provides BoardEnvironment operations.</summary>
        internal readonly BoardEnvironment _boardEnvironment;
        /// <summary>Provides BoardQueries operations.</summary>
        internal readonly BoardQueries _boardQueries;
        /// <summary>Provides BobsledZombieActions operations.</summary>
        internal readonly BobsledZombieActions _bobsledZombieActions;
        /// <summary>Provides BungeeZombieActions operations.</summary>
        internal readonly BungeeZombieActions _bungeeZombieActions;
        /// <summary>Provides CombatSystem operations.</summary>
        internal readonly CombatSystem _combatSystem;
        /// <summary>Provides DancerZombieActions operations.</summary>
        internal readonly DancerZombieActions _dancerZombieActions;
        /// <summary>Provides DiggerZombieActions operations.</summary>
        internal readonly DiggerZombieActions _diggerZombieActions;
        /// <summary>Provides GargantuarActions operations.</summary>
        internal readonly GargantuarActions _gargantuarActions;
        /// <summary>Provides JackZombieActions operations.</summary>
        internal readonly JackZombieActions _jackZombieActions;
        /// <summary>Provides LadderSystem operations.</summary>
        internal readonly LadderSystem _ladderSystem;
        /// <summary>Provides LegacySimulation operations.</summary>
        internal readonly LegacySimulation _legacySimulation;
        /// <summary>Provides PlantTimingSystem operations.</summary>
        internal readonly PlantTimingSystem _plantTimingSystem;
        /// <summary>Provides PlantingSystem operations.</summary>
        internal readonly PlantingSystem _plantingSystem;
        /// <summary>Provides PogoZombieActions operations.</summary>
        internal readonly PogoZombieActions _pogoZombieActions;
        /// <summary>Provides RewardSystem operations.</summary>
        internal readonly RewardSystem _rewardSystem;
        /// <summary>Provides VaultZombieActions operations.</summary>
        internal readonly VaultZombieActions _vaultZombieActions;
        /// <summary>Provides VehicleZombieActions operations.</summary>
        internal readonly VehicleZombieActions _vehicleZombieActions;
        /// <summary>Provides WaterZombieActions operations.</summary>
        internal readonly WaterZombieActions _waterZombieActions;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        internal readonly ZombieFeedbackSystem _zombieFeedbackSystem;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        internal readonly ZombieLifecycle _zombieLifecycle;
        /// <summary>Provides SimulationWorld operations.</summary>
        internal readonly SimulationWorld _world;
        /// <summary>Connects zombie action dependencies once per match.</summary>
        /// <param name="adventureModeRunner">The zombie action dependency.</param>
        /// <param name="balloonZombieActions">The zombie action dependency.</param>
        /// <param name="biteSystem">The zombie action dependency.</param>
        /// <param name="boardEnvironment">The zombie action dependency.</param>
        /// <param name="boardQueries">The zombie action dependency.</param>
        /// <param name="bobsledZombieActions">The zombie action dependency.</param>
        /// <param name="bungeeZombieActions">The zombie action dependency.</param>
        /// <param name="combatSystem">The zombie action dependency.</param>
        /// <param name="dancerZombieActions">The zombie action dependency.</param>
        /// <param name="diggerZombieActions">The zombie action dependency.</param>
        /// <param name="gargantuarActions">The zombie action dependency.</param>
        /// <param name="jackZombieActions">The zombie action dependency.</param>
        /// <param name="ladderSystem">The zombie action dependency.</param>
        /// <param name="legacySimulation">The zombie action dependency.</param>
        /// <param name="plantTimingSystem">The zombie action dependency.</param>
        /// <param name="plantingSystem">The zombie action dependency.</param>
        /// <param name="pogoZombieActions">The zombie action dependency.</param>
        /// <param name="rewardSystem">The zombie action dependency.</param>
        /// <param name="vaultZombieActions">The zombie action dependency.</param>
        /// <param name="vehicleZombieActions">The zombie action dependency.</param>
        /// <param name="waterZombieActions">The zombie action dependency.</param>
        /// <param name="zombieFeedbackSystem">The zombie action dependency.</param>
        /// <param name="zombieLifecycle">The zombie action dependency.</param>
        /// <param name="world">The zombie action dependency.</param>
        internal ZombieBehaviorContext(AdventureModeRunner adventureModeRunner, BalloonZombieActions balloonZombieActions, BiteSystem biteSystem, BoardEnvironment boardEnvironment, BoardQueries boardQueries, BobsledZombieActions bobsledZombieActions, BungeeZombieActions bungeeZombieActions, CombatSystem combatSystem, DancerZombieActions dancerZombieActions, DiggerZombieActions diggerZombieActions, GargantuarActions gargantuarActions, JackZombieActions jackZombieActions, LadderSystem ladderSystem, LegacySimulation legacySimulation, PlantTimingSystem plantTimingSystem, PlantingSystem plantingSystem, PogoZombieActions pogoZombieActions, RewardSystem rewardSystem, VaultZombieActions vaultZombieActions, VehicleZombieActions vehicleZombieActions, WaterZombieActions waterZombieActions, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle, SimulationWorld world)
        {
            _adventureModeRunner = adventureModeRunner;
            _balloonZombieActions = balloonZombieActions;
            _biteSystem = biteSystem;
            _boardEnvironment = boardEnvironment;
            _boardQueries = boardQueries;
            _bobsledZombieActions = bobsledZombieActions;
            _bungeeZombieActions = bungeeZombieActions;
            _combatSystem = combatSystem;
            _dancerZombieActions = dancerZombieActions;
            _diggerZombieActions = diggerZombieActions;
            _gargantuarActions = gargantuarActions;
            _jackZombieActions = jackZombieActions;
            _ladderSystem = ladderSystem;
            _legacySimulation = legacySimulation;
            _plantTimingSystem = plantTimingSystem;
            _plantingSystem = plantingSystem;
            _pogoZombieActions = pogoZombieActions;
            _rewardSystem = rewardSystem;
            _vaultZombieActions = vaultZombieActions;
            _vehicleZombieActions = vehicleZombieActions;
            _waterZombieActions = waterZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
            _world = world;
        }
    }
}
