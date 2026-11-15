#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns PogoZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class PogoZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal PogoZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Removes the original pogo stick, preserving its current altitude until native falling reaches the lawn.</summary>
        /// <param name="zombie">The pogo zombie whose retained stick is being removed.</param>
        /// <param name="damageFlags">The original damage mask; bit sixteen suppresses the detached stick particle.</param>
        internal void BreakAdventurePogo(Zombie zombie, uint damageFlags = 0)
        {
            if (zombie.Kind != ZombieKind.Pogo || zombie.UsedAbility)
            {
                return;
            }

            // PC PogoBreak 540DF0 emits particle sixteen except when the magnet supplies flag sixteen.
            if ((damageFlags & 16U) == 0)
            {
                _zombieFeedbackSystem.AddAdventureZombiePart(zombie, (int)ZombieKind.Pogo, 16, "Zombie_pogo_stick", null);
            }

            var state = zombie.PogoState ??= new AdventurePogoState();
            state.AnimationBlendTicks = 0;
            zombie.PresentationNativeHeight = 7;
            zombie.PresentationNativePhase = 0;
            zombie.ShieldHealth = 0f;
            zombie.UsedAbility = true;
            zombie.Eating = false;
            _zombieFeedbackSystem.PickAdventureZombieNativeSpeed(zombie, 0);
            _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
        }

        /// <summary>Applies PC UpdateZombieFalling's one-pixel descent after the pogo stick has been lost.</summary>
        /// <param name="zombie">The live actor, including a pogo falling after a magnetic removal or Tall-nut block.</param>
        internal static void AdvanceAdventurePogoFalling(Zombie zombie)
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
        internal Plant? FindAdventurePogoVaultTarget(Zombie zombie, int nativeX)
        {
            var backwards = zombie.PresentationWalkingBackwards ?? zombie.Hypnotized;
            var attack = AdventureZombieBiteRules.AttackRect((int)ZombieKind.Pogo, _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie),
                _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Bowling, backwards, nativeX, 0);
            foreach (var plant in _world.Plants)
            {
                if (_biteSystem.AdventureBiteCanTarget(zombie, plant, AdventureBiteAttack.Vault)
                    && AdventureZombieBiteRules.Reaches(attack, _biteSystem.AdventurePlantEatingRectangle(plant)))
                {
                    return plant;
                }
            }

            return null;
        }

        /// <summary>Runs PC 540F10 after ordinary position movement and before storing the next integer mX.</summary>
        /// <param name="zombie">The live pogo zombie whose eighty-update bounce may finish.</param>
        /// <param name="previousGridX">The previous simulation position supplying the original retained integer mX.</param>
        internal void AdvanceAdventurePogo(Zombie zombie, float previousGridX)
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
                _zombieFeedbackSystem.RequestAdventureZombieFoley(26);
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
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(48);
                    _world.Presentation.PlantBiteEffects.Add(new AdventurePlantBiteEffect
                    {
                        Id = ++_world.Presentation._effectId,
                        ParticleId = 29,
                        Row = barrier.Row,
                        NativeX = 40f + barrier.Column * 80f + 60f,
                        NativeY = _biteSystem.AdventurePlantEatingRectangle(barrier).Y - 20f,
                        Seed = zombie.Id * 397 ^ _world.Presentation._effectId
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
                _zombieFeedbackSystem.PickAdventureZombieNativeSpeed(zombie, 20);
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
    }
}
