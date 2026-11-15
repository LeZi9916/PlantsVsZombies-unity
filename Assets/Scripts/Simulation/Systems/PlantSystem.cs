#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns PlantSystem responsibilities for one deterministic match.</summary>
    internal sealed class PlantSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Retains plant behavior dependencies for allocation-free updates.</summary>
        private PlantBehaviorContext _behaviorContext = null!;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides CoinSystem operations.</summary>
        private CoinSystem _coinSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides ExplosionEffects operations.</summary>
        private ExplosionEffects _explosionEffects = null!;
        /// <summary>Provides ExplosivePlantActions operations.</summary>
        private ExplosivePlantActions _explosivePlantActions = null!;
        /// <summary>Provides LootSystem operations.</summary>
        private LootSystem _lootSystem = null!;
        /// <summary>Provides MagnetPlantActions operations.</summary>
        private MagnetPlantActions _magnetPlantActions = null!;
        /// <summary>Provides PlantTimingSystem operations.</summary>
        private PlantTimingSystem _plantTimingSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides SpecialPlantActions operations.</summary>
        private SpecialPlantActions _specialPlantActions = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides UpgradePlantActions operations.</summary>
        private UpgradePlantActions _upgradePlantActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal PlantSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="coinSystem">The CoinSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="explosionEffects">The ExplosionEffects collaborator.</param>
        /// <param name="explosivePlantActions">The ExplosivePlantActions collaborator.</param>
        /// <param name="lootSystem">The LootSystem collaborator.</param>
        /// <param name="magnetPlantActions">The MagnetPlantActions collaborator.</param>
        /// <param name="plantTimingSystem">The PlantTimingSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="specialPlantActions">The SpecialPlantActions collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="upgradePlantActions">The UpgradePlantActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, CoinSystem coinSystem, CombatSystem combatSystem, ExplosionEffects explosionEffects, ExplosivePlantActions explosivePlantActions, LootSystem lootSystem, MagnetPlantActions magnetPlantActions, PlantTimingSystem plantTimingSystem, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, SpecialPlantActions specialPlantActions, SunSystem sunSystem, UpgradePlantActions upgradePlantActions, ZombieFeedbackSystem zombieFeedbackSystem)
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
            _behaviorContext = new PlantBehaviorContext(_adventureModeRunner, _boardQueries, _coinSystem, _combatSystem, _explosionEffects, _explosivePlantActions, _lootSystem, _magnetPlantActions, _plantTimingSystem, _plantingSystem, _projectileSystem, _specialPlantActions, _sunSystem, _upgradePlantActions, _zombieFeedbackSystem, _world);
            _plantTimingSystem.SetBehaviorContext(_behaviorContext);
        }

        /// <summary>Performs advance adventure plants.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventurePlants(float dt)
        {
            for (var i = _world.Presentation.SquishedPlants.Count - 1; i >= 0; i--)
            {
                if (--_world.Presentation.SquishedPlants[i].Remaining < 0)
                {
                    _world.Presentation.SquishedPlants.RemoveAt(i);
                }
            }

            foreach (Plant p in new PooledSnapshot<Plant>(_world.Plants))
            {
                var producerAdvanced = false;
                int producerSunCount = _world.Suns.Count, producerActions = p.ActionCount;
                try
                {
                    if (!_world.Plants.Contains(p) || p.BungeeState != 0)
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
                        if (wakeTicks == 60)
                        {
                            // PC Plant::Update plays Wakeup at the sixty-tick milestone.
                            _zombieFeedbackSystem.RequestAdventureZombieFoley(83);
                        }
                        if (p.WakeTimer <= 0f)
                        {
                            p.Asleep = false;
                            p.SleepingPresentation = null;
                            p.AwakeByCoffee = true;
                            p.PresentationCoffeeWakeAge = p.Age;
                        }
                    }

                    if (p.Asleep)
                    {
                        continue;
                    }

                    if (p.Behavior.AdvanceNative(p, _behaviorContext))
                    {
                        producerAdvanced = p.Kind == PlantKind.Sunflower;
                        continue;
                    }

                    p.SpecialTimer = Math.Max(0f, p.SpecialTimer - dt);
                    if (p.Behavior.BlocksAction(p))
                    {
                        continue;
                    }

                    p.ActionTimer -= dt;
                    producerAdvanced = AdventureProducerGlow.HasNativeProductionHint(p.Kind);
                    if (p.Behavior.AdvanceAction(p, _behaviorContext, dt))
                    {
                        continue;
                    }

                    if (p.ActionTimer > 0.000001f)
                    {
                        continue;
                    }

                    p.Behavior.Act(p, _behaviorContext);
                }
                finally
                {
                    AdventurePlantBlinkRules.Tick(p, _world._random);
                    if (AdventureProducerGlow.HasNativeProductionHint(p.Kind))
                    {
                        AdventureProducerGlow.Tick(p, producerAdvanced, p.Kind == PlantKind.Sunflower && p.Timing.HasValue ? p.Timing.Value.Countdown : AdventureProducerGlow.CounterFromSeconds(p.ActionTimer), p.Kind == PlantKind.Marigold ? p.ActionCount > producerActions : _world.Suns.Count > producerSunCount);
                    }
                }
            }
        }
    }
}
