#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns LadderSystem responsibilities for one deterministic match.</summary>
    internal sealed class LadderSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides VaultZombieActions operations.</summary>
        private VaultZombieActions _vaultZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal LadderSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="vaultZombieActions">The VaultZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, BoardQueries boardQueries, VaultZombieActions vaultZombieActions, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _vaultZombieActions = vaultZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Checks the original cell-based ladder independently of the targeted plant layer.</summary>
        /// <param name="row">The native board row.</param>
        /// <param name="column">The native board column.</param>
        /// <returns>Whether any surviving plant in this cell retains the placed ladder.</returns>
        internal bool AdventureHasLadderAt(int row, int column)
        {
            foreach (var plant in _world.Plants)
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
        internal void RemoveAdventureLadder(int row, int column)
        {
            foreach (var plant in _world.Plants)
            {
                if (plant.Row == row && plant.Column == column)
                {
                    plant.HasLadder = false;
                }
            }
        }

        /// <summary>Removes PC 4294C0's square grid-item range independently of zombie damage and circular blast radius.</summary>
        /// <param name="x">The original explosion center's horizontal pixel coordinate.</param>
        /// <param name="y">The original explosion center's vertical pixel coordinate.</param>
        /// <param name="range">The original row range, also used as the ladder column range.</param>
        internal void RemoveAdventureLaddersInBlast(int x, int y, int range)
        {
            // PixelToGridXKeepOnBoard (4280D0) and PixelToGridYKeepOnBoard (428160).
            var column = Math.Max(0, Math.Min(8, (Math.Max(40, x) - 40) / 80));
            var roofOffset = _adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 4 - column) * 20 : 0;
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85 : 100;
            var row = Math.Max(0, Math.Min(_world.RowCount - 1, (y - 80 - roofOffset) / pitch));
            foreach (var plant in _world.Plants)
            {
                if (Math.Abs(plant.Column - column) <= range && Math.Abs(plant.Row - row) <= range)
                {
                    plant.HasLadder = false;
                }
            }
        }

        /// <summary>Runs PC 546620's retained twelve-frame placement before detaching the carried shield.</summary>
        /// <param name="zombie">The live ladder carrier whose original action is being advanced.</param>
        /// <returns>Whether the placing body must hold its position for this update.</returns>
        internal bool AdvanceAdventureLadderPlacing(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.Ladder)
            {
                return false;
            }

            var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(zombie);
            if (phase == 77 && zombie.ShieldHealth <= 0f)
            {
                // Native DetachShield interrupts the placing phase independently of the remaining head state.
                _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, 0);
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

                var target = _boardQueries.FindAdventureNativePlantTarget(zombie, AdventureBiteAttack.Ladder);
                if (target != null)
                {
                    target.HasLadder = true;
                    zombie.UseLadderColumn = target.Column;
                    zombie.PresentationNativeHeight = 6;
                    zombie.ShieldHealth = 0f;
                    zombie.UsedAbility = true;
                    _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, 0);
                    zombie.PresentationBodyBlendTicks = 0;
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(-7);
                }
                else
                {
                    _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, 76);
                    zombie.PresentationBodyBlendTicks = 0;
                }

                return false;
            }

            if (phase == 76 && zombie.ShieldHealth > 0f && zombie.PresentationNativeHeight == 0
                && _boardQueries.FindAdventureNativePlantTarget(zombie, AdventureBiteAttack.Ladder) != null)
            {
                _biteSystem.StopAdventureNativeEating(zombie);
                zombie.PresentationNativePhase = 77;
                zombie.PresentationAnimation = string.Empty;
                _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
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
        internal static void AdvanceAdventureLadderFall(Zombie zombie)
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
        internal bool AdvanceAdventureLadderTraversal(Zombie zombie, float dt)
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
            zombie.X += (direction ? 1f : -1f) * _zombieFeedbackSystem.AdventurePresentationWalkingDistance(zombie, dt, zombie.Speed * dt * slow);
            _zombieFeedbackSystem.RecordAdventureZombieFootsteps(zombie, previousWalkingX);
            return true;
        }
    }
}
