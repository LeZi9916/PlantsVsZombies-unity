#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BiteSystem responsibilities for one deterministic match.</summary>
    internal sealed class BiteSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides ExplosionEffects operations.</summary>
        private ExplosionEffects _explosionEffects = null!;
        /// <summary>Provides ExplosivePlantActions operations.</summary>
        private ExplosivePlantActions _explosivePlantActions = null!;
        /// <summary>Provides LadderSystem operations.</summary>
        private LadderSystem _ladderSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantTimingSystem operations.</summary>
        private PlantTimingSystem _plantTimingSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides RewardSystem operations.</summary>
        private RewardSystem _rewardSystem = null!;
        /// <summary>Provides SpecialPlantActions operations.</summary>
        private SpecialPlantActions _specialPlantActions = null!;
        /// <summary>Provides VaultZombieActions operations.</summary>
        private VaultZombieActions _vaultZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BiteSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="explosionEffects">The ExplosionEffects collaborator.</param>
        /// <param name="explosivePlantActions">The ExplosivePlantActions collaborator.</param>
        /// <param name="ladderSystem">The LadderSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantTimingSystem">The PlantTimingSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="rewardSystem">The RewardSystem collaborator.</param>
        /// <param name="specialPlantActions">The SpecialPlantActions collaborator.</param>
        /// <param name="vaultZombieActions">The VaultZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, ExplosionEffects explosionEffects, ExplosivePlantActions explosivePlantActions, LadderSystem ladderSystem, LegacySimulation legacySimulation, PlantTimingSystem plantTimingSystem, PlantingSystem plantingSystem, RewardSystem rewardSystem, SpecialPlantActions specialPlantActions, VaultZombieActions vaultZombieActions, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _explosionEffects = explosionEffects;
            _explosivePlantActions = explosivePlantActions;
            _ladderSystem = ladderSystem;
            _legacySimulation = legacySimulation;
            _plantTimingSystem = plantTimingSystem;
            _plantingSystem = plantingSystem;
            _rewardSystem = rewardSystem;
            _specialPlantActions = specialPlantActions;
            _vaultZombieActions = vaultZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs adventure zombie attack rectangle.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie attack rectangle.</returns>
        internal NativeRect AdventureZombieAttackRectangle(Zombie z)
        {
            int kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind, phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(z);
            var x = (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var y = (int)AdventureBiteNativeY(z, x);
            var backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z));
            return global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.AttackRect(kind, phase, _adventureModeRunner.Adventure.Level.Mode == global::PlantsVsZombies.Adventure.AdventureMode.Bowling, backwards, x, y, z.PresentationBiteBodyY, z.PresentationBiteClipHeight, z.Kind == ZombieKind.Gargantuar ? 180 : 120);
        }

        /// <summary>Performs adventure bite native y.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "x">The x.</param>
        /// <returns>The resulting adventure bite native y.</returns>
        internal float AdventureBiteNativeY(Zombie z, float x)
        {
            return 80f + z.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f) + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - x / 80f) * 20f - 10f : 0f) - 30f - (z.Kind == ZombieKind.Balloon ? 30f : 0f) - (z.Kind == ZombieKind.Pogo ? 16f : 0f) + z.PresentationVaultPositionY;
        }

        /// <summary>Performs adventure plant eating rectangle.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting adventure plant eating rectangle.</returns>
        internal NativeRect AdventurePlantEatingRectangle(Plant p)
        {
            return global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.PlantRect((int)p.Kind, 40 + p.Column * 80, 80 + p.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85 : 100) + (_adventureModeRunner.Adventure.Level.HasRoof ? (int)(Math.Max(0f, 5f - p.Column) * 20f - 10f) : 0), p.Kind == PlantKind.Pumpkin ? 120 : 80);
        }

        /// <summary>Uses the original BungeeOrder/CatapultOrder instead of plant creation order for a layered cell.</summary>
        /// <param name="row">The native lawn row.</param>
        /// <param name="column">The native lawn column.</param>
        /// <returns>The flying, normal, pumpkin or support plant in original priority order.</returns>
        internal Plant? AdventureAirAttackPlantAt(int row, int column)
        {
            return _boardQueries.AdventurePlantAt(row, column, PlantLayer.Effect) ?? _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main)
                ?? _boardQueries.AdventurePlantAt(row, column, PlantLayer.Armor) ?? _boardQueries.AdventurePlantAt(row, column, PlantLayer.Support);
        }

        /// <summary>Applies the original SquishAllInSquare stack removal rather than letting a pumpkin absorb a whole-cell crush.</summary>
        /// <param name="row">The crushed lawn row.</param>
        /// <param name="column">The crushed lawn column.</param>
        /// <param name="driveOver">Whether to preserve spiky plants for the separate vehicle/spike collision.</param>
        internal void CrushAdventurePlantCell(int row, int column, bool driveOver)
        {
            foreach (var plant in new PooledSnapshot<Plant>(_world.Plants))
            {
                if (plant.Row == row && plant.Column == column && plant.Kind != PlantKind.Spikerock
                    && (!driveOver || plant.Kind != PlantKind.Spikeweed))
                {
                    SquishAdventurePlant(plant);
                }
            }
        }

        /// <summary>Applies PC 470FA0's instant-special exceptions and retained flattened plant picture.</summary>
        /// <param name="plant">The actual allocated plant contacted by a crush.</param>
        internal void SquishAdventurePlant(Plant plant)
        {
            if (!_world.Plants.Contains(plant) || plant.PresentationBiteNotOnGround || plant.BungeeState == 2
                || plant.Kind == PlantKind.Squash && plant.SquashPresentation?.NativePhase > 0)
            {
                return;
            }

            if (!plant.Asleep)
            {
                if (plant.Kind == PlantKind.CherryBomb)
                {
                    _explosivePlantActions.DetonateAdventureCherryBomb(plant);
                    return;
                }

                if (plant.Kind == PlantKind.Jalapeno)
                {
                    _explosivePlantActions.DetonateAdventureJalapeno(plant);
                    return;
                }

                if (plant.Kind == PlantKind.DoomShroom)
                {
                    (plant.DoomShroomPresentation ??= new AdventureDoomShroomState()).Countdown = 0;
                    _specialPlantActions.AdvanceAdventureDoomShroom(plant);
                    return;
                }

                if (plant.Kind == PlantKind.IceShroom)
                {
                    _specialPlantActions.DetonateAdventureIceShroom(plant);
                    return;
                }

                if (plant.Kind == PlantKind.PotatoMine && plant.MinePhase != MinePhase.Buried)
                {
                    _specialPlantActions.DamageAdventurePotatoMine(plant);
                    _explosionEffects.AdventurePresentationExplosion(PlantKind.PotatoMine, plant.Column + .5f, plant.Row);
                    _plantingSystem.RemoveAdventurePlantAndStack(plant);
                    _legacySimulation.Raise(GameEvent.MineExplosion);
                    return;
                }
            }

            _ladderSystem.RemoveAdventureLadder(plant.Row, plant.Column);
            plant.SleepingPresentation = null;
            _world.Plants.Remove(plant);
            if (_adventureModeRunner.Adventure.SelectedCobCannonId == plant.Id)
            {
                _adventureModeRunner.Adventure.SelectedCobCannonId = null;
            }

            _world.Presentation.SquishedPlants.Add(new AdventureSquishedPlant(plant));
            _zombieFeedbackSystem.RequestAdventureZombieFoley(21);
        }

        /// <summary>Performs adventure bite can target.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "p">The p.</param>
        /// <param name = "attack">The attack.</param>
        /// <param name = "priority">The priority.</param>
        /// <returns>The resulting adventure bite can target.</returns>
        internal bool AdventureBiteCanTarget(Zombie z, Plant p, global::PlantsVsZombies.Adventure.AdventureBiteAttack attack, bool priority = true)
        {
            if (p.Health <= 0 || p.Row != z.Row || p.Layer == global::PlantsVsZombies.Adventure.PlantLayer.Effect || p.BungeeState == 2 || p.PresentationBiteNotOnGround || p.Kind == PlantKind.TangleKelp || p.Kind == PlantKind.Squash && p.PresentationBiteNativeState >= 5 && p.PresentationBiteNativeState <= 7)
            {
                return false;
            }

            if (_adventureModeRunner.Adventure.Level.Mode == global::PlantsVsZombies.Adventure.AdventureMode.Bowling && attack != global::PlantsVsZombies.Adventure.AdventureBiteAttack.Vault)
            {
                return false;
            }

            var pool = z.PresentationInPool ?? (_adventureModeRunner.Adventure.Level.IsWaterRow(z.Row) && z.X >= 0 && z.X < 9);
            if (!pool && _adventureModeRunner.Adventure.Level.IsWaterRow(p.Row))
            {
                return false;
            }

            if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 0)
            {
                return p.Kind == PlantKind.PotatoMine && p.MinePhase == MinePhase.Buried;
            }

            if (p.Kind == PlantKind.Spikeweed || p.Kind == PlantKind.Spikerock)
            {
                return z.Kind == ZombieKind.Gargantuar || z.Kind == ZombieKind.Zomboni || _adventureModeRunner.Adventure.Level.IsWaterRow(p.Row) || _boardQueries.HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column);
            }

            if (attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.DriveOver)
            {
                if (p.Kind == PlantKind.CherryBomb || p.Kind == PlantKind.Jalapeno || p.Kind == PlantKind.Blover || p.Kind == PlantKind.Squash)
                {
                    return false;
                }

                if ((p.Kind == PlantKind.DoomShroom || p.Kind == PlantKind.IceShroom) && !p.Asleep)
                {
                    return false;
                }
            }

            if (z.Kind == ZombieKind.Ladder && z.ShieldHealth > 0)
            {
                var barrier = (p.Kind == PlantKind.Wallnut || p.Kind == PlantKind.TallNut || p.Kind == PlantKind.Pumpkin) && !_ladderSystem.AdventureHasLadderAt(p.Row, p.Column);
                if (attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.Chew && barrier)
                {
                    return false;
                }

                if (attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.Ladder && !barrier)
                {
                    return false;
                }
            }

            if (priority && (attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.Chew || attack == global::PlantsVsZombies.Adventure.AdventureBiteAttack.Vault))
            {
                foreach (var q in _world.Plants)
                {
                    if (q != p && q.Row == p.Row && q.Column == p.Column && q.Layer > p.Layer && (attack != global::PlantsVsZombies.Adventure.AdventureBiteAttack.Vault || q.Layer == global::PlantsVsZombies.Adventure.PlantLayer.Main) && AdventureBiteCanTarget(z, q, attack, false))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Performs adventure native chewer.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure native chewer.</returns>
        internal bool AdventureNativeChewer(Zombie z)
        {
            return global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.CanChew((int)z.Kind, _zombieFeedbackSystem.AdventureZombieFeedbackPhase(z), z.PresentationNativeHeight, z.Airborne, z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility));
        }

        /// <summary>Performs adventure plant bite immune.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting adventure plant bite immune.</returns>
        internal bool AdventurePlantBiteImmune(Plant p)
        {
            if (p.Kind == PlantKind.PotatoMine && p.MinePhase != MinePhase.Buried)
            {
                return true;
            }

            var state = p.PresentationBiteNativeState;
            return !p.Asleep && (p.Kind == PlantKind.CherryBomb || p.Kind == PlantKind.Jalapeno || p.Kind == PlantKind.DoomShroom || p.Kind == PlantKind.IceShroom || p.Kind == PlantKind.HypnoShroom || state == 47 || state == 48 || state == 3 || state == 4);
        }

        /// <summary>Performs advance adventure native eating.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "target">The target.</param>
        /// <param name = "dt">The dt.</param>
        /// <param name = "zombieTarget">The zombie target.</param>
        /// <returns>The resulting advance adventure native eating.</returns>
        internal bool AdvanceAdventureNativeEating(Zombie z, Plant? target, float dt, bool zombieTarget = false)
        {
            if (z.TangleKelpGrab != null)
            {
                // PC FindPlantTarget 0x54B392 rejects kelp targets without clearing the existing eating latch.
                return z.Eating;
            }

            if (z.Headless || !AdventureNativeChewer(z) || z.PresentationGarlicCounter >= 0)
            {
                return false;
            }

            var checks = global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.EatingChecks(z.Age, dt, z.ChillRemaining > 0);
            if (checks > 0)
            {
                if (target != null || zombieTarget)
                {
                    z.Eating = true;
                    if (z.Kind == ZombieKind.Snorkel)
                    {
                        z.Submerged = false;
                    }
                }
                else
                {
                    StopAdventureNativeEating(z);
                }
            }

            if (!z.Eating)
            {
                z._presentationBiteClip = null;
                return false;
            }

            var clip = _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(z);
            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(ZombieFeedbackSystem.AdventureZombiePresentationAsset(z), clip, out data))
            {
                double cycle = z.PresentationAnimationAge * _zombieFeedbackSystem.SynchronizeAdventureZombieRate(z) / data.Count;
                if (z._presentationBiteClip == clip)
                {
                    float first, second;
                    global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.ChewPhases((int)z.Kind, out first, out second);
                    if (z.FrozenRemaining <= 0 && !z.Headless && _zombieFeedbackSystem.AdventureZombieFeedbackPhase(z) != 60 && (global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.Crossed(z._presentationBiteCycle, cycle, first) || global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.Crossed(z._presentationBiteCycle, cycle, second)))
                    {
                        AnimateAdventureChew(z, target, zombieTarget);
                    }
                }

                z._presentationBiteClip = clip;
                z._presentationBiteCycle = cycle;
            }

            if (target != null && checks > 0 && _world.Plants.Contains(target) && !AdventurePlantBiteImmune(target) && z.PresentationGarlicCounter < 0)
            {
                target.Health -= 4f * checks;
                target.RecentlyEatenTicks = 50;
                if (target.Health <= 0)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(-1);
                    _plantingSystem.RemoveAdventurePlantAndStack(target);
                }
            }

            return true;
        }

        /// <summary>Restarts native walking and selects its species speed when a latched meal ends.</summary>
        /// <param name="z">The zombie whose plant or opposing zombie is no longer in eating range.</param>
        internal void StopAdventureNativeEating(Zombie z)
        {
            if (!z.Eating)
            {
                return;
            }

            z.Eating = false;
            if (z.Kind == ZombieKind.Snorkel)
            {
                z.Submerged = z.PresentationInPool == true;
                // The native UpdateZombieSnorkel sees StopEating before the next position update.
                // Start the reverse pose now so this tick cannot move while still displaying anim_eat.
                if (z.PresentationNativePhase == 60 || z.PresentationNativePhase == 61)
                {
                    _vaultZombieActions.SetAdventureZombieMotionPhase(z, 62);
                }
            }

            var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(z);
            // PC StopEating 0x54B1C0 skips StartWalkAnim for tunnelling diggers and snorkel zombies.
            // StartWalkAnim 0x54B050 calls PickRandomSpeed 0x540500 before selecting the walking clip.
            if (phase != 32 && z.Kind != ZombieKind.Snorkel)
            {
                _zombieFeedbackSystem.PickAdventureZombieNativeSpeed(z, phase);
                _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(z);
            }
        }

        /// <summary>Performs animate adventure chew.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "target">The target.</param>
        /// <param name = "zombieTarget">The zombie target.</param>
        internal void AnimateAdventureChew(Zombie z, Plant? target, bool zombieTarget)
        {
            if (target != null && target.Kind == PlantKind.HypnoShroom && !target.Asleep)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(68);
                _plantingSystem.RemoveAdventurePlantAndStack(target);
                z.Hypnotized = true;
                _zombieFeedbackSystem.RequestAdventureZombieFoley(-5);
                AdventureDancerRules.DetachForMindControl(z, _world.Zombies);
                // PC 0x547610 calls TrySpawnLevelAward using this zombie after mind control.
                _rewardSystem.RememberAdventureRewardPosition(z);
                z.Submerged = false;
                // PC 0x547610 retains the eating latch: 0.17 is replaced when StopEating restarts walking.
                z.PresentationNativeVelocity = .17f;
                z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(.17f);
                _world.Presentation.PlantBiteEffects.Add(new global::PlantsVsZombies.Adventure.AdventurePlantBiteEffect {
                    Id = ++_world.Presentation._effectId,
                    ParticleId = 88,
                    Row = z.Row,
                    Kind = z.Kind,
                    NativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z)),
                    NativeY = AdventureBiteNativeY(z, global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z))),
                    OffsetX = 60,
                    OffsetY = 40,
                    Seed = z.Id * 397 ^ _world.Presentation._effectId
                });
                return;
            }

            _zombieFeedbackSystem.RequestAdventureZombieFoley(global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.ChewFoley(target == null ? -1 : (int)target.Kind, z.Hypnotized));
            _plantTimingSystem.RecordAdventureChewEffect(z, target);
            if (target != null && target.Kind == PlantKind.Garlic)
            {
                z.PresentationGarlicCounter = 0;
                z._presentationGarlicPlant = target;
                z._presentationGarlicHeldAge = z.PresentationAnimationAge;
                z._presentationGarlicHeldHeadAge = z.PresentationPlantHeadAge;
            }
        }

        /// <summary>Performs advance adventure garlic bite.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting advance adventure garlic bite.</returns>
        internal bool AdvanceAdventureGarlicBite(Zombie z, float dt)
        {
            if (z.PresentationGarlicCounter < 0)
            {
                return false;
            }

            var ticks = Math.Max(0, (int)Math.Round(dt * 100));
            for (var i = 0; i < ticks && z.PresentationGarlicCounter >= 0; i++)
            {
                var c = ++z.PresentationGarlicCounter;
                if (c > 20 && c < 170 && (z._presentationGarlicPlant == null || !_world.Plants.Contains(z._presentationGarlicPlant)))
                {
                    c = 170;
                    z.PresentationGarlicCounter = c;
                    z.Eating = false;
                }

                if (c == 70)
                {
                    z.Eating = false;
                    _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(z);
                    z._presentationGarlicHeldAge = 0;
                    if (_world.Zombies.Count <= 5 || _world.Zombies.Count <= 10 && _world._random.Next(2) == 0)
                    {
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(72);
                    }
                }

                if (c == 170)
                {
                    z.Eating = false;
                    var rows = new System.Collections.Generic.List<int>();
                    for (var row = Math.Max(0, z.Row - 1); row <= Math.Min(_world.RowCount - 1, z.Row + 1); row++)
                    {
                        if (row != z.Row && _adventureModeRunner.Adventure.Level.IsLawnRow(row) && _adventureModeRunner.Adventure.Level.IsWaterRow(row) == _adventureModeRunner.Adventure.Level.IsWaterRow(z.Row))
                        {
                            rows.Add(row);
                        }
                    }

                    if (rows.Count > 0)
                    {
                        z.Row = rows[_world._random.Next(rows.Count)];
                    }

                    z._presentationBiteClip = null;
                    _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(z);
                }

                if (c > 270)
                {
                    z.PresentationGarlicCounter = -1;
                    z._presentationGarlicPlant = null;
                }
            }

            if (z.PresentationGarlicCounter >= 0 && z.PresentationGarlicCounter < 170)
            {
                z.PresentationAnimationAge = z._presentationGarlicHeldAge;
                z.PresentationPlantHeadAge = z._presentationGarlicHeldHeadAge;
                global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
                var clip = _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(z);
                if (global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(ZombieFeedbackSystem.AdventureZombiePresentationAsset(z), clip, out data))
                {
                    var phase = z.PresentationAnimationAge * _zombieFeedbackSystem.SynchronizeAdventureZombieRate(z) / data.Count;
                    z._presentationWalkLoops = (int)Math.Floor(phase);
                    z._presentationWalkPhase = phase - z._presentationWalkLoops;
                }

                return true;
            }

            return false;
        }
    }
}
