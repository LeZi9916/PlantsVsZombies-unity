#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns DiggerZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class DiggerZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal DiggerZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(BiteSystem biteSystem, BoardQueries boardQueries, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Applies PC DiggerLoseAxe 0x543CC0 without removing its nonmagnetic hardhat.</summary>
        /// <param name="zombie">The digger whose retained pickaxe has been removed.</param>
        internal void LoseAdventureDiggerAxe(Zombie zombie)
        {
            var state = zombie.DiggerState ??= _world.LifetimePool.Rent<AdventureDiggerState>();
            if (_zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie) == 32)
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
        internal void BeginAdventureDiggerRise(Zombie zombie, bool withoutAxe)
        {
            var state = zombie.DiggerState ??= _world.LifetimePool.Rent<AdventureDiggerState>();
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
            var pooledRecord1 = _world.LifetimePool.Rent<AdventureZombieRiseEffect>();
            pooledRecord1.Id = ++_world.Presentation._effectId;
            pooledRecord1.Row = zombie.Row;
            pooledRecord1.X = nativeX + 60f;
            pooledRecord1.Y = _biteSystem.AdventureBiteNativeY(zombie, nativeX) + 118f;
            pooledRecord1.Digger = true;
            _world.Presentation.ZombieRiseEffects.Add(pooledRecord1);
            _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
            _zombieFeedbackSystem.RequestAdventureZombieFoley(80);
            if (!withoutAxe)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(83);
            }
        }

        /// <summary>Runs the original PC digger's tunnelling, emergence, axe-loss pause, and dizzy phases.</summary>
        /// <param name="zombie">The actor updated at the original one-hundred-hertz simulation rate.</param>
        /// <param name="dt">The current fixed simulation interval in seconds.</param>
        /// <returns>Whether the original phase blocks ordinary walking and eating this update.</returns>
        internal bool AdvanceAdventureDigger(Zombie zombie, float dt)
        {
            if (zombie.Kind != ZombieKind.Digger)
            {
                return false;
            }

            var state = zombie.DiggerState ??= _world.LifetimePool.Rent<AdventureDiggerState>();
            var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie);
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
                    var mine = _boardQueries.FindAdventureNativePlantTarget(zombie, AdventureBiteAttack.Chew);
                    if (!_biteSystem.AdvanceAdventureNativeEating(zombie, mine, dt))
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
                    _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
                }

                if (state.PhaseTicks == 0)
                {
                    zombie.PresentationAltitude = 0f;
                    zombie.PresentationNativePhase = phase == 35 ? 38 : 36;
                    state.AnimationBlendTicks = phase == 35 ? 20 : 10;
                    if (phase == 35)
                    {
                        zombie.AbilityPhase = 3;
                        _zombieFeedbackSystem.PickAdventureZombieNativeSpeed(zombie, 38);
                    }

                    _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
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
                _zombieFeedbackSystem.PickAdventureZombieNativeSpeed(zombie, 37);
                _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
            }

            return false;
        }
    }
}
