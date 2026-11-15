#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Supplies explicit plant action dependencies without exposing the session facade.</summary>
    internal sealed class PlantBehaviorContext
    {
        /// <summary>Provides AdventureModeRunner operations.</summary>
        internal readonly AdventureModeRunner _adventureModeRunner;
        /// <summary>Provides BoardQueries operations.</summary>
        internal readonly BoardQueries _boardQueries;
        /// <summary>Provides CoinSystem operations.</summary>
        internal readonly CoinSystem _coinSystem;
        /// <summary>Provides CombatSystem operations.</summary>
        internal readonly CombatSystem _combatSystem;
        /// <summary>Provides ExplosionEffects operations.</summary>
        internal readonly ExplosionEffects _explosionEffects;
        /// <summary>Provides ExplosivePlantActions operations.</summary>
        internal readonly ExplosivePlantActions _explosivePlantActions;
        /// <summary>Provides LootSystem operations.</summary>
        internal readonly LootSystem _lootSystem;
        /// <summary>Provides MagnetPlantActions operations.</summary>
        internal readonly MagnetPlantActions _magnetPlantActions;
        /// <summary>Provides PlantTimingSystem operations.</summary>
        internal readonly PlantTimingSystem _plantTimingSystem;
        /// <summary>Provides PlantingSystem operations.</summary>
        internal readonly PlantingSystem _plantingSystem;
        /// <summary>Provides ProjectileSystem operations.</summary>
        internal readonly ProjectileSystem _projectileSystem;
        /// <summary>Provides SpecialPlantActions operations.</summary>
        internal readonly SpecialPlantActions _specialPlantActions;
        /// <summary>Provides SunSystem operations.</summary>
        internal readonly SunSystem _sunSystem;
        /// <summary>Provides UpgradePlantActions operations.</summary>
        internal readonly UpgradePlantActions _upgradePlantActions;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        internal readonly ZombieFeedbackSystem _zombieFeedbackSystem;
        /// <summary>Provides SimulationWorld operations.</summary>
        internal readonly SimulationWorld _world;
        /// <summary>Connects plant action dependencies once per match.</summary>
        /// <param name="adventureModeRunner">The plant action dependency.</param>
        /// <param name="boardQueries">The plant action dependency.</param>
        /// <param name="coinSystem">The plant action dependency.</param>
        /// <param name="combatSystem">The plant action dependency.</param>
        /// <param name="explosionEffects">The plant action dependency.</param>
        /// <param name="explosivePlantActions">The plant action dependency.</param>
        /// <param name="lootSystem">The plant action dependency.</param>
        /// <param name="magnetPlantActions">The plant action dependency.</param>
        /// <param name="plantTimingSystem">The plant action dependency.</param>
        /// <param name="plantingSystem">The plant action dependency.</param>
        /// <param name="projectileSystem">The plant action dependency.</param>
        /// <param name="specialPlantActions">The plant action dependency.</param>
        /// <param name="sunSystem">The plant action dependency.</param>
        /// <param name="upgradePlantActions">The plant action dependency.</param>
        /// <param name="zombieFeedbackSystem">The plant action dependency.</param>
        /// <param name="world">The plant action dependency.</param>
        internal PlantBehaviorContext(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, CoinSystem coinSystem, CombatSystem combatSystem, ExplosionEffects explosionEffects, ExplosivePlantActions explosivePlantActions, LootSystem lootSystem, MagnetPlantActions magnetPlantActions, PlantTimingSystem plantTimingSystem, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, SpecialPlantActions specialPlantActions, SunSystem sunSystem, UpgradePlantActions upgradePlantActions, ZombieFeedbackSystem zombieFeedbackSystem, SimulationWorld world)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _coinSystem = coinSystem;
            _combatSystem = combatSystem;
            _explosionEffects = explosionEffects;
            _explosivePlantActions = explosivePlantActions;
            _lootSystem = lootSystem;
            _magnetPlantActions = magnetPlantActions;
            _plantTimingSystem = plantTimingSystem;
            _plantingSystem = plantingSystem;
            _projectileSystem = projectileSystem;
            _specialPlantActions = specialPlantActions;
            _sunSystem = sunSystem;
            _upgradePlantActions = upgradePlantActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _world = world;
        }
    }
}
