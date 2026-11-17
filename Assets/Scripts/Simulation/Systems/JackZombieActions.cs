#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns JackZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class JackZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides SpecialPlantActions operations.</summary>
        private SpecialPlantActions _specialPlantActions = null!;
        /// <summary>Provides VaseMode operations.</summary>
        private VaseMode _vaseMode = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal JackZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="specialPlantActions">The SpecialPlantActions collaborator.</param>
        /// <param name="vaseMode">The VaseMode collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, CombatSystem combatSystem, PlantingSystem plantingSystem, SpecialPlantActions specialPlantActions, VaseMode vaseMode, VaseSystem vaseSystem, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _combatSystem = combatSystem;
            _plantingSystem = plantingSystem;
            _specialPlantActions = specialPlantActions;
            _vaseMode = vaseMode;
            _vaseSystem = vaseSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Advances the original running and stopped box-opening phases without chilling the phase clock.</summary>
        /// <param name="zombie">The live zombie whose native action is being updated.</param>
        /// <returns>Whether box opening or detonation consumes this zombie's movement update.</returns>
        internal bool AdvanceAdventureJackInTheBox(Zombie zombie)
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
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(-6);
                    _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
                    return true;
                }

                return false;
            }

            if (zombie.JackInTheBoxTicks == 80)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(34);
            }

            if (zombie.JackInTheBoxTicks <= 0)
            {
                var x = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie)) + 60;
                var y = (int)_biteSystem.AdventureBiteNativeY(zombie, x - 60) + 60;
                _combatSystem.BlastAdventureRadius(x, y, zombie.Row, 115, 1, zombie.Hypnotized ? 0x7FU : 0xFFU);
                if (!zombie.Hypnotized)
                {
                    foreach (var plant in new PooledSnapshot<Plant>(_world.Plants))
                    {
                        var rect = AdventureZombieBiteRules.PlantRect((int)plant.Kind, 40 + plant.Column * 80 + plant.PositionOffsetX,
                            _specialPlantActions.AdventureSquashCellY(plant.Row, plant.Column) + plant.PositionOffsetY, plant.Kind == PlantKind.Pumpkin ? 120 : 80);
                        var dx = Math.Max(0, Math.Max(rect.X - x, x - rect.X - rect.Width));
                        var dy = Math.Max(0, Math.Max(rect.Y - y, y - rect.Y - rect.Height));
                        if (dx * dx + dy * dy <= 90 * 90)
                        {
                            _plantingSystem.RemoveAdventurePlantAndStack(plant);
                        }
                    }
                }

                var pooledRecord1 = _world.LifetimePool.Rent<AdventureExplosion>();
                pooledRecord1.Id = ++_world.Presentation._effectId;
                pooledRecord1.Seed = _world._random.Next();
                pooledRecord1.Row = zombie.Row;
                pooledRecord1.NativeX = x;
                pooledRecord1.NativeY = y;
                pooledRecord1.Effect = AdventureExplosionEffect.JackExplode;
                _world.Presentation.Explosions.Add(pooledRecord1);
                _world.Presentation.ScreenShake.Start(4, -6);
                _zombieFeedbackSystem.RequestAdventureZombieFoley(23);
                zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                _zombieLifecycle.KillAdventureZombie(zombie);
                OpenVasesInJackExplosion(x, y);
            }

            return true;
        }

        /// <summary>Opens the original three-by-three vase grid after a Jack-in-the-Box explosion.</summary>
        /// <param name="x">The same integer board-pixel center used by the zombie's native explosion.</param>
        /// <param name="y">The same integer vertical center used by the native explosion.</param>
        internal void OpenVasesInJackExplosion(int x, int y)
        {
            if (_adventureModeRunner.Adventure.Level.Mode != AdventureMode.Vasebreaker)
            {
                return;
            }

            // PC ScaryPotterJackExplode 0x435050 uses clamped grid coordinates, not the plant blast radius.
            var column = x < 40 ? -1 : Math.Min(8, (x - 40) / 80);
            var row = column < 0 || y < 80 ? -1 : Math.Min(4, (y - 80) / 100);
            for (var index = 0; index < _adventureModeRunner.Adventure._vases.Count;)
            {
                var vase = _adventureModeRunner.Adventure._vases[index];
                if (Math.Abs(vase.Column - column) > 1 || Math.Abs(vase.Row - row) > 1)
                {
                    index++;
                    continue;
                }

                _adventureModeRunner.Adventure._vases.RemoveAt(index);
                if (_world.ModeSession == null)
                {
                    _vaseSystem.OpenAdventureVase(vase);
                }
                else
                {
                    _vaseMode.OpenModeVase(vase);
                }

                _world.Presentation.VaseBreakSoundSerial++;
            }
        }
    }
}
