#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns GargantuarActions responsibilities for one deterministic match.</summary>
    internal sealed class GargantuarActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides VaseMode operations.</summary>
        private VaseMode _vaseMode = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Provides VaultZombieActions operations.</summary>
        private VaultZombieActions _vaultZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal GargantuarActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="vaseMode">The VaseMode collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        /// <param name="vaultZombieActions">The VaultZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, BoardQueries boardQueries, CombatSystem combatSystem, PlantingSystem plantingSystem, VaseMode vaseMode, VaseSystem vaseSystem, VaultZombieActions vaultZombieActions, ZombieFeedbackSystem zombieFeedbackSystem, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _combatSystem = combatSystem;
            _plantingSystem = plantingSystem;
            _vaseMode = vaseMode;
            _vaseSystem = vaseSystem;
            _vaultZombieActions = vaultZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieSpawnSystem = zombieSpawnSystem;
        }

        /// <summary>Starts the original giant body action without replacing its retained walking velocity.</summary>
        /// <param name="zombie">The giant starting or completing an authored action.</param>
        /// <param name="phase">The original normal, throwing or smashing phase.</param>
        internal void SetAdventureGargantuarPhase(Zombie zombie, int phase)
        {
            zombie.Eating = false;
            (zombie.GargantuarState ??= new AdventureGargantuarState()).ActionTriggered = false;
            if (phase == 0)
            {
                _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, phase);
            }
            else
            {
                zombie.PresentationNativePhase = phase;
                zombie._presentationSpeedPhase = phase;
                zombie.PresentationAnimation = string.Empty;
                _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
            }

            zombie.PresentationBodyBlendTicks = 20;
        }

        /// <summary>Runs PC 542790's animation events, smash reacquisition and original throw admission.</summary>
        /// <param name="zombie">The live giant whose action advances after its native body clock.</param>
        /// <returns>Whether this update is occupied by the giant's authored action.</returns>
        internal bool AdvanceAdventureGargantuar(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.Gargantuar)
            {
                return false;
            }

            var state = zombie.GargantuarState ??= new AdventureGargantuarState();
            var phase = zombie.PresentationNativePhase;
            if (phase == 69 || phase == 70)
            {
                var progress = zombie.PresentationAnimationAge * (phase == 69 ? 24f / 34f : 16f / 33f);
                if (!state.ActionTriggered && progress + .000001f >= (phase == 69 ? .74f : .64f))
                {
                    state.ActionTriggered = true;
                    if (phase == 69)
                    {
                        ThrowAdventureImp(zombie);
                    }
                    else
                    {
                        var target = _boardQueries.FindAdventureNativePlantTarget(zombie, AdventureBiteAttack.Chew);
                        if (target?.Kind == PlantKind.Spikerock)
                        {
                            _combatSystem.HitAdventureZombie(zombie, 20f, true, 32U);
                            target.Health -= 50f;
                            if (target.Health <= 0f)
                            {
                                _zombieFeedbackSystem.RequestAdventureZombieFoley(21);
                                _plantingSystem.RemoveAdventurePlantAndStack(target);
                                _biteSystem.CrushAdventurePlantCell(target.Row, target.Column, false);
                            }
                        }
                        else if (target != null)
                        {
                            _biteSystem.CrushAdventurePlantCell(target.Row, target.Column, false);
                        }

                        var vase = FindAdventureGargantuarVase(zombie);
                        if (vase != null)
                        {
                            if (_world.ModeSession == null)
                            {
                                _vaseSystem.OpenAdventureVase(vase);
                            }
                            else
                            {
                                _vaseMode.OpenModeVase(vase);
                            }
                        }

                        _zombieFeedbackSystem.RequestAdventureZombieFoley(40);
                        _world.Presentation.ScreenShake.Start(0, 3);
                    }
                }

                if (progress + .000001f >= 1f && _world.Zombies.Contains(zombie))
                {
                    SetAdventureGargantuarPhase(zombie, 0);
                    return false;
                }

                return true;
            }

            if (zombie.Headless || zombie.Hypnotized)
            {
                return false;
            }

            var x = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            if (!zombie.ThrownImp && zombie.Health < (int)zombie.StartingHealth / 2 && x - 360f > 40f)
            {
                SetAdventureGargantuarPhase(zombie, 69);
                return true;
            }

            if (_boardQueries.FindAdventureNativePlantTarget(zombie, AdventureBiteAttack.Chew) != null
                || FindAdventureGargantuarVase(zombie) != null)
            {
                SetAdventureGargantuarPhase(zombie, 70);
                _zombieFeedbackSystem.RequestAdventureZombieFoley(70);
                return true;
            }

            return false;
        }

        /// <summary>Finds the original vase under the giant's own native column without allocating a per-update predicate.</summary>
        /// <param name="zombie">The giant whose current native position probes the vase grid.</param>
        /// <returns>The original vase, or null when this cell contains no unopened vase.</returns>
        internal AdventureVase? FindAdventureGargantuarVase(Zombie zombie)
        {
            if (_adventureModeRunner.Adventure._vases.Count == 0)
            {
                return null;
            }

            var x = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var column = Math.Max(0, Math.Min(8, (x - 40) / 80));
            foreach (var vase in _adventureModeRunner.Adventure._vases)
            {
                if (vase.Row == zombie.Row && vase.Column == column)
                {
                    return vase;
                }
            }

            return null;
        }

        /// <summary>Creates PC 5429B7's thrown imp at the .74 body event, preserving roof and chill rules.</summary>
        /// <param name="giant">The giant whose authored throwing hand releases its imp.</param>
        internal void ThrowAdventureImp(Zombie giant)
        {
            giant.ThrownImp = true;
            _zombieFeedbackSystem.RequestAdventureZombieFoley(47);
            var x = AdventureZombieNativeRules.NativePositionX(giant.X, AdventureDancerRules.Scale(giant));
            var distance = x - 360f;
            var minimum = 40f;
            if (_adventureModeRunner.Adventure.Level.HasRoof)
            {
                distance -= 180f;
                minimum = -140f;
            }

            if (distance < minimum)
            {
                distance = minimum;
            }
            else if (distance > 140f)
            {
                distance -= (float)_world._random.NextDouble() * 100f;
            }

            var imp = _zombieSpawnSystem.SpawnAdventureBoardZombie(ZombieKind.Imp, giant.Row, AdventureZombieNativeRules.GridX(x - 133f), giant.Wave);
            imp.NativeVariant = false;
            imp.PresentationNativePhase = 71;
            imp.PresentationAltitude = 88f;
            imp.PresentationBiteBodyY = -88;
            imp.ChillRemaining = giant.ChillRemaining;
            imp.ThrownImpState = new AdventureThrownImpState
            {
                VelocityZ = .5f * (distance / 3f) * .05f,
                NativeY = _biteSystem.AdventureBiteNativeY(giant, x)
            };
            imp.PresentationBodyBlendTicks = 0;
            imp.PresentationAnimation = string.Empty;
            _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(imp);
            _zombieFeedbackSystem.RequestAdventureZombieFoley(88);
        }

        /// <summary>Runs PC 542D60's unchilled ballistic movement and six-frame landing before walking resumes.</summary>
        /// <param name="zombie">The imp with a retained native thrown state.</param>
        /// <returns>Whether flight or landing occupies this update.</returns>
        internal bool AdvanceAdventureThrownImp(Zombie zombie)
        {
            var state = zombie.ThrownImpState;
            if (zombie.Kind != ZombieKind.Imp || state == null)
            {
                return false;
            }

            if (zombie.PresentationNativePhase == 71)
            {
                state.VelocityZ -= .05f;
                zombie.PresentationAltitude += state.VelocityZ;
                zombie.X -= 3f / 80f;
                var x = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                var y = _biteSystem.AdventureBiteNativeY(zombie, x);
                zombie.PresentationAltitude += y - state.NativeY;
                state.NativeY = y;
                if (zombie.PresentationAltitude <= 0f)
                {
                    zombie.PresentationAltitude = 0f;
                    _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, 72);
                    zombie.PresentationBodyBlendTicks = 0;
                }

                zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
                return true;
            }

            if (zombie.PresentationNativePhase == 72)
            {
                if (zombie.PresentationAnimationAge * 24f + .0001f >= 6f)
                {
                    _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, 0);
                    zombie.PresentationBodyBlendTicks = 0;
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
