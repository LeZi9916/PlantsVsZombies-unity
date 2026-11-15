#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ZombieSystem responsibilities for one deterministic match.</summary>
    internal sealed class ZombieSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Retains the ordered action capabilities.</summary>
        private ZombieBehaviorContext _behaviorContext = null!;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BalloonZombieActions operations.</summary>
        private BalloonZombieActions _balloonZombieActions = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardEnvironment operations.</summary>
        private BoardEnvironment _boardEnvironment = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides BobsledZombieActions operations.</summary>
        private BobsledZombieActions _bobsledZombieActions = null!;
        /// <summary>Provides BungeeZombieActions operations.</summary>
        private BungeeZombieActions _bungeeZombieActions = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides DancerZombieActions operations.</summary>
        private DancerZombieActions _dancerZombieActions = null!;
        /// <summary>Provides DiggerZombieActions operations.</summary>
        private DiggerZombieActions _diggerZombieActions = null!;
        /// <summary>Provides GargantuarActions operations.</summary>
        private GargantuarActions _gargantuarActions = null!;
        /// <summary>Provides JackZombieActions operations.</summary>
        private JackZombieActions _jackZombieActions = null!;
        /// <summary>Provides LadderSystem operations.</summary>
        private LadderSystem _ladderSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantTimingSystem operations.</summary>
        private PlantTimingSystem _plantTimingSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides PogoZombieActions operations.</summary>
        private PogoZombieActions _pogoZombieActions = null!;
        /// <summary>Provides RewardSystem operations.</summary>
        private RewardSystem _rewardSystem = null!;
        /// <summary>Provides VaultZombieActions operations.</summary>
        private VaultZombieActions _vaultZombieActions = null!;
        /// <summary>Provides VehicleZombieActions operations.</summary>
        private VehicleZombieActions _vehicleZombieActions = null!;
        /// <summary>Provides WaterZombieActions operations.</summary>
        private WaterZombieActions _waterZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ZombieSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="balloonZombieActions">The BalloonZombieActions collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardEnvironment">The BoardEnvironment collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="bobsledZombieActions">The BobsledZombieActions collaborator.</param>
        /// <param name="bungeeZombieActions">The BungeeZombieActions collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="dancerZombieActions">The DancerZombieActions collaborator.</param>
        /// <param name="diggerZombieActions">The DiggerZombieActions collaborator.</param>
        /// <param name="gargantuarActions">The GargantuarActions collaborator.</param>
        /// <param name="jackZombieActions">The JackZombieActions collaborator.</param>
        /// <param name="ladderSystem">The LadderSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantTimingSystem">The PlantTimingSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="pogoZombieActions">The PogoZombieActions collaborator.</param>
        /// <param name="rewardSystem">The RewardSystem collaborator.</param>
        /// <param name="vaultZombieActions">The VaultZombieActions collaborator.</param>
        /// <param name="vehicleZombieActions">The VehicleZombieActions collaborator.</param>
        /// <param name="waterZombieActions">The WaterZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BalloonZombieActions balloonZombieActions, BiteSystem biteSystem, BoardEnvironment boardEnvironment, BoardQueries boardQueries, BobsledZombieActions bobsledZombieActions, BungeeZombieActions bungeeZombieActions, CombatSystem combatSystem, DancerZombieActions dancerZombieActions, DiggerZombieActions diggerZombieActions, GargantuarActions gargantuarActions, JackZombieActions jackZombieActions, LadderSystem ladderSystem, LegacySimulation legacySimulation, PlantTimingSystem plantTimingSystem, PlantingSystem plantingSystem, PogoZombieActions pogoZombieActions, RewardSystem rewardSystem, VaultZombieActions vaultZombieActions, VehicleZombieActions vehicleZombieActions, WaterZombieActions waterZombieActions, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle)
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
            _behaviorContext = new ZombieBehaviorContext(_adventureModeRunner, _balloonZombieActions, _biteSystem, _boardEnvironment, _boardQueries, _bobsledZombieActions, _bungeeZombieActions, _combatSystem, _dancerZombieActions, _diggerZombieActions, _gargantuarActions, _jackZombieActions, _ladderSystem, _legacySimulation, _plantTimingSystem, _plantingSystem, _pogoZombieActions, _rewardSystem, _vaultZombieActions, _vehicleZombieActions, _waterZombieActions, _zombieFeedbackSystem, _zombieLifecycle, _world);
        }

        /// <summary>Applies the existing escape, mower and loss boundaries to ordinary and ladder movement.</summary>
        /// <param name="z">The live actor whose horizontal movement has completed.</param>
        /// <returns>Whether the actor caused the current match to be lost.</returns>
        internal bool CheckAdventureZombieBoardEdge(Zombie z)
        {
            if (z.X > 12f && (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)))
            {
                z.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                _zombieLifecycle.KillAdventureZombie(z);
                return false;
            }

            if (!z.Hypnotized && z.X < 0.15f && _world.Mowers[z.Row].Available)
            {
                _world.Mowers[z.Row].Available = false;
                _world.Mowers[z.Row].Active = true;
                _adventureModeRunner.Adventure.RowPicker.LastMowedWave[z.Row] = _adventureModeRunner.Adventure.CurrentWave;
                _legacySimulation.Raise(GameEvent.Mower);
            }
            else if (!z.Hypnotized && z.X < -0.65f && !_world.Mowers[z.Row].Active)
            {
                _world.State = MatchState.Lost;
                _adventureModeRunner.Adventure.Phase = AdventurePhase.Lost;
                _legacySimulation.Raise(GameEvent.Lost);
                return true;
            }

            return false;
        }

        /// <summary>Performs advance adventure zombies.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureZombies(float dt)
        {
            _plantTimingSystem.AdvanceAdventurePlantHitCounters(dt);
            _world._dancerClockTicks++;
            foreach (Zombie z in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if (!_world.Zombies.Contains(z))
                {
                    continue;
                }

                z.Age += dt;
                if (z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.Wind, dt))
                {
                    continue;
                }

                _zombieFeedbackSystem.AdvanceAdventureZombieGroan(z);
                if (z.CatapultState?.FlatTires == true)
                {
                    // Native dying updates still expire cold counters while their independent death timer runs.
                    z.ChillRemaining = Math.Max(0f, z.ChillRemaining - dt);
                    z.FrozenRemaining = Math.Max(0f, z.FrozenRemaining - dt);
                    if (--z.CatapultState.DeathTicks <= 0)
                    {
                        _zombieLifecycle.KillAdventureZombie(z);
                    }

                    continue;
                }

                if (z.Kind == ZombieKind.Zomboni && z.ZomboniState?.FlatTires == true)
                {
                    if (--z.ZomboniState.DeathTicks <= 0)
                    {
                        _vehicleZombieActions.ExplodeAdventureZomboni(z);
                        _zombieLifecycle.KillAdventureZombie(z);
                    }

                    continue;
                }

                if (z.PoolState != null)
                {
                    if (z.PoolState.SplashAge >= 0f)
                    {
                        z.PoolState.SplashAge += dt;
                    }

                    if (z.PoolState.SeaweedAge >= 0f)
                    {
                        z.PoolState.SeaweedAge += dt;
                    }
                }

                if (z.Wave != -4 && AdventureZomboniRules.HasCriticalDamage(z) && _world._random.Next(5) == 0)
                {
                    // PC 5471DD..547221 applies silent body damage with a one-in-five chance per native tick.
                    _combatSystem.HitAdventureZombie(z, z.StartingHealth >= 500f ? 3f : 1f, true, 9U);
                    if (!_world.Zombies.Contains(z))
                    {
                        continue;
                    }
                }

                if (z.Headless)
                {
                    if (_world._random.Next(5) == 0)
                    {
                        z.Health -= z.StartingHealth >= 500f ? 3f : 1f;
                    }

                    if (z.Health <= 0)
                    {
                        _zombieLifecycle.KillAdventureZombie(z);
                        continue;
                    }
                }

                if (z.GraveRiseTicks > 0)
                {
                    // PC 0x54DA20 / 0x544970: no movement or eating during the 150-update grave phase.
                    if (z.FrozenRemaining <= 0f)
                    {
                        z.GraveRiseTicks--;
                        if (z.PoolState != null && z.PresentationInPool == true)
                        {
                            z.PoolState.Altitude = AdventureZombiePoolRules.RiseAltitude(z.GraveRiseTicks, AdventureDancerRules.Scale(z));
                        }

                        if (z.GraveRiseTicks == 0)
                        {
                            z.PresentationNativePhase = -1;
                            WaterZombieActions.UpdateAdventurePoolBodyRectangle(z);
                        }
                    }
                    else
                    {
                        z.FrozenRemaining = Math.Max(0f, z.FrozenRemaining - dt);
                    }

                    continue;
                }

                if (z.DropRemaining > 0f)
                {
                    z.DropRemaining -= dt;
                    continue;
                }

                if (z.FrozenRemaining > 0f || z.PresentationButter > 0f)
                {
                    z.FrozenRemaining = Math.Max(0, z.FrozenRemaining - dt);
                    continue;
                }

                if (_ladderSystem.AdvanceAdventureLadderTraversal(z, dt))
                {
                    if (CheckAdventureZombieBoardEdge(z))
                    {
                        return;
                    }

                    continue;
                }

                PogoZombieActions.AdvanceAdventurePogoFalling(z);
                var slow = (AdventureDancerRules.IsDancer(z)
                    ? AdventureDancerRules.GroupAffected(z, _world.Zombies, true) : z.ChillRemaining > 0f) ? .5f : 1f;
                _waterZombieActions.AdvanceAdventureZombiePool(z, dt);
                if (z.FrozenRemaining > 0f)
                {
                    continue;
                }

                z.ChillRemaining = Math.Max(0, z.ChillRemaining - dt);
                if (z.PlantZombieState?.Detached == true)
                {
                    z.Eating = false;
                    continue;
                }

                if (z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.TimedSpecial, dt))
                {
                    continue;
                }

                if (_biteSystem.AdvanceAdventureGarlicBite(z, dt))
                {
                    continue;
                }

                // Head loss does not call StopEating in PC. Preserve a latched meal, but deal no new bite damage.
                if (z.Headless && z.Eating)
                {
                    continue;
                }

                if (z.PresentationNewspaperGaspRemaining > 0f)
                {
                    continue;
                }

                if (z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.Motion, dt))
                {
                    continue;
                }

                if (z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.AfterMotion, dt))
                {
                    continue;
                }

                z.AbilityTimer -= dt * slow;
                if (z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.Carrier, dt))
                {
                    continue;
                }

                if (z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.Dance, dt))
                {
                    continue;
                }

                if (z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.Giant, dt))
                {
                    continue;
                }

                z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.Equipment, dt);

                if (z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.Underground, dt))
                {
                    continue;
                }

                var catapultStopped = z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.Vehicle, dt);

                Zombie? opponent = null;
                foreach (var other in _world.Zombies)
                {
                    if (other != z && other.Row == z.Row && other.Hypnotized != z.Hypnotized && !other.Airborne && Math.Abs(other.X - z.X) < 0.5f)
                    {
                        opponent = other;
                        break;
                    }
                }
                if (opponent != null && (z.Hypnotized || opponent.Hypnotized) && !z.Headless && z.TangleKelpGrab == null
                    && !AdventurePogoRules.IsBouncing(z))
                {
                    if (_biteSystem.AdvanceAdventureNativeEating(z, null, dt, true))
                    {
                        var bites = global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.EatingChecks(z.Age, dt, z.ChillRemaining > 0);
                        if (bites > 0)
                        {
                            _combatSystem.HitAdventureZombie(opponent, 4f * bites, true, 9U);
                        }
                    }

                    continue;
                }

                Plant? target = null;
                var vault = z.Kind == ZombieKind.PoleVaulter;
                var attack = vault && !z.UsedAbility ? AdventureBiteAttack.Vault : z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult ? AdventureBiteAttack.DriveOver : AdventureBiteAttack.Chew;
                if (!z.Airborne && !z.Hypnotized && !z.Headless)
                {
                    if (z.Kind == ZombieKind.Ladder && !z.UsedAbility)
                    {
                        target = _boardQueries.FindAdventureNativePlantTarget(z, AdventureBiteAttack.Ladder);
                    }

                    if (target == null)
                    {
                        target = _boardQueries.FindAdventureNativePlantTarget(z, attack);
                    }
                }

                if (target != null && _ladderSystem.AdventureHasLadderAt(target.Row, target.Column) && z.Kind != ZombieKind.Digger
                    && (_biteSystem.AdventureNativeChewer(z) || z.Kind == ZombieKind.PoleVaulter && z.PresentationNativePhase == 11))
                {
                    _biteSystem.StopAdventureNativeEating(z);
                    if (z.PresentationNativeHeight == 0 && z.UseLadderColumn != target.Column)
                    {
                        z.PresentationNativeHeight = 6;
                        z.UseLadderColumn = target.Column;
                    }

                    target = null;
                }

                if (target != null && z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility && !target.HasLadder)
                {
                    _vaultZombieActions.BeginAdventurePoleVault(z, target);
                    continue;
                }

                var tallBarrier = target != null && (target.Kind == PlantKind.TallNut || _boardQueries.AdventurePlantAt(target.Row, target.Column, PlantLayer.Main) != null && _boardQueries.AdventurePlantAt(target.Row, target.Column, PlantLayer.Main).Kind == PlantKind.TallNut);
                if (target != null && vault && !z.UsedAbility && !tallBarrier && z.AbilityTimer > 0)
                {
                    continue;
                }

                if (target != null && vault && !z.UsedAbility && !tallBarrier)
                {
                    z.X -= 1.1f;
                    z.AbilityTimer = 0.75f;
                    if (z.Kind != ZombieKind.Pogo)
                    {
                        z.UsedAbility = true;
                        _zombieFeedbackSystem.RefreshAdventureZombieNativeSpeed(z);
                    }

                    continue;
                }

                if (target != null && z.Kind == ZombieKind.Dancing && z.PresentationNativePhase == 40)
                {
                    // PC EatPlant shortens the moonwalk instead of biting before the first point animation.
                    z.DancerState!.PhaseTicks = 1;
                    continue;
                }

                if (!z.Headless && _biteSystem.AdvanceAdventureNativeEating(z, target, dt))
                {
                    continue;
                }

                if (z.Kind != ZombieKind.Zomboni && z.Kind != ZombieKind.Catapult)
                {
                    target = null;
                }

                // PC catapults crush through CheckForZombieStep without entering an eating animation.
                z.Eating = target != null && z.Kind != ZombieKind.Catapult;
                if (target != null)
                {
                    if (z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult)
                    {
                        _biteSystem.CrushAdventurePlantCell(target.Row, target.Column, true);
                        if (target.Kind == PlantKind.Spikeweed)
                        {
                            _combatSystem.HitAdventureZombie(z, 20f, true, 32U);
                            _plantingSystem.RemoveAdventurePlantAndStack(target);
                        }
                        else if (target.Kind == PlantKind.Spikerock)
                        {
                            _combatSystem.HitAdventureZombie(z, 1800f, true, 32U);
                            target.Health -= 50;
                            target.VehicleHitsTaken++;
                            if (target.Health <= 0)
                            {
                                _plantingSystem.RemoveAdventurePlantAndStack(target);
                            }
                        }
                        // Squish owns ordinary plants, including instant-special and triggered-Squash immunity.
                        continue;
                    }

                    z.BiteTimer -= dt * slow;
                    if (z.BiteTimer <= 0)
                    {
                        if (target.Kind == PlantKind.HypnoShroom && !target.Asleep && z.Kind != ZombieKind.Gargantuar)
                        {
                            z.Hypnotized = true;
                            _zombieFeedbackSystem.RequestAdventureZombieFoley(-5);
                            AdventureDancerRules.DetachForMindControl(z, _world.Zombies);
                            _rewardSystem.RememberAdventureRewardPosition(z);
                            z.Submerged = false;
                            _plantingSystem.RemoveAdventurePlantAndStack(target);
                            continue;
                        }

                        target.Health -= 4f;
                        z.BiteTimer = .04f;
                        if (target.Kind == PlantKind.Garlic && z.Kind != ZombieKind.Gargantuar)
                        {
                            using var rowsLease = _world.IntegerScratch.Rent();
                            var rows = rowsLease.Items;
                            for (var r = Math.Max(0, z.Row - 1); r <= Math.Min(_world.RowCount - 1, z.Row + 1); r++)
                            {
                                if (r != z.Row && _adventureModeRunner.Adventure.Level.IsLawnRow(r) && _adventureModeRunner.Adventure.Level.IsWaterRow(r) == _adventureModeRunner.Adventure.Level.IsWaterRow(z.Row))
                                {
                                    rows.Add(r);
                                }
                            }

                            if (rows.Count > 0)
                            {
                                z.Row = rows[_world._random.Next(rows.Count)];
                            }
                        }

                        if (target.Health <= 0)
                        {
                            _plantingSystem.RemoveAdventurePlantAndStack(target);
                        }
                    }
                }
                else
                {
                    z.BiteTimer = 0;
                    if (catapultStopped)
                    {
                        continue;
                    }

                    var direction = z.Hypnotized || z.PresentationNativePhase == 40
                        || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z) ? 1f : -1f;
                    var previousX = z.X;
                    z.X += direction * _zombieFeedbackSystem.AdventurePresentationWalkingDistance(z, dt, z.Speed * dt * slow);
                    _zombieFeedbackSystem.RecordAdventureZombieFootsteps(z, previousX);
                    z.Behavior.Advance(z, _behaviorContext, ZombieActionPhase.AfterWalking, dt, previousX);
                }

                if (CheckAdventureZombieBoardEdge(z))
                {
                    return;
                }
            }
        }
    }
}
