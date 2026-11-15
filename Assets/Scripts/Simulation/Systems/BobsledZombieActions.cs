#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BobsledZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class BobsledZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides VaultZombieActions operations.</summary>
        private VaultZombieActions _vaultZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BobsledZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="vaultZombieActions">The VaultZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, CombatSystem combatSystem, VaultZombieActions vaultZombieActions, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _combatSystem = combatSystem;
            _vaultZombieActions = vaultZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Updates PC 543AD0's sled push, authored boarding, ice damage, and crash detachment.</summary>
        /// <param name="zombie">The member whose independent native phase counter advances this update.</param>
        internal void AdvanceAdventureBobsled(Zombie zombie)
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
                    _zombieFeedbackSystem.RefreshAdventureZombieNativeSpeed(zombie);
                }

                return;
            }

            if (zombie.PresentationNativePhase == 17 && --state.PhaseTicks <= 0)
            {
                _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, 18);
            }
            else if (zombie.PresentationNativePhase == 18)
            {
                var age = (int)(AdventureBobsledRules.BoardingProgress(zombie.PresentationAnimationAge) * 50f);
                var start = state.Position == 1 || state.Position == 3 ? 8f : -9f;
                zombie.PresentationAltitude = start + (18f - start) * age / 50f;
            }

            // PC retains an eight-hundred-pixel edge when a Jalapeno has completely removed the row's ice.
            var iceEdge = 800;
            foreach (var ice in _adventureModeRunner.Adventure._ice)
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
                _combatSystem.HitAdventureZombie(zombie, 6f, false, 8U);
            }
        }

        /// <summary>Starts PC 5439A0's shared sled break without removing its four living riders.</summary>
        /// <param name="leader">The front zombie whose three-hundred-point sled helmet was destroyed.</param>
        internal void CrashAdventureBobsled(Zombie leader)
        {
            foreach (var member in _world.Zombies)
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
                var animation = _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(member);
                member._presentationWalkLoops = 0;
                member._presentationWalkPhase = member == leader ? 0f : (float)_world._random.NextDouble();
                member.PresentationNativeVelocity = leader.PresentationNativeVelocity;
                member.Speed = leader.Speed;
                if (AdventureZombieGroundData.TryGet(ZombieFeedbackSystem.AdventureZombiePresentationAsset(member), animation, out var clip))
                {
                    member.PresentationAnimationAge = member._presentationWalkPhase * clip.Count / _zombieFeedbackSystem.SynchronizeAdventureZombieRate(member);
                }
            }
        }
    }
}
