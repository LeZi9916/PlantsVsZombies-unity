#nullable enable
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns DancerZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class DancerZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal DancerZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, ZombieFeedbackSystem zombieFeedbackSystem, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieSpawnSystem = zombieSpawnSystem;
        }

        /// <summary>Advances the original dancer phase and fills only this leader's vacant backup slots.</summary>
        /// <param name="zombie">The live dancer being advanced.</param>
        internal void AdvanceAdventureDancer(Zombie zombie)
        {
            if (zombie.DancerState == null)
            {
                AdventureDancerRules.Initialize(zombie, zombie.Kind == ZombieKind.Dancing ? _world._random.Next(12) : 0);
            }

            var previousPhase = zombie.PresentationNativePhase;
            var needsBackup = zombie.Kind == ZombieKind.Dancing
                && AdventureDancerRules.NeedsBackup(zombie, _world.Zombies, _world.Presentation.Corpses, _adventureModeRunner.Adventure.Level);
            var chilled = AdventureDancerRules.GroupAffected(zombie, _world.Zombies, true);
            if (!AdventureDancerRules.Tick(zombie, _world._dancerClockTicks, needsBackup, chilled))
            {
                return;
            }

            if (previousPhase == 41)
            {
                var enemies = 0;
                foreach (var other in _world.Zombies)
                {
                    if (other.Health > 0f && !other.Headless && !other.Hypnotized)
                    {
                        enemies++;
                    }
                }

                if (enemies <= 15)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(100);
                }
            }

            if (zombie.Headless)
            {
                return;
            }

            var state = zombie.DancerState!;
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            for (var slot = 0; slot < 4; slot++)
            {
                var row = zombie.Row + (slot == 0 ? -1 : slot == 1 ? 1 : 0);
                if (!AdventureDancerRules.CanSummonInRow(_adventureModeRunner.Adventure.Level, row)
                    || AdventureDancerRules.Occupied(_world.Zombies, _world.Presentation.Corpses, state.Followers[slot]))
                {
                    continue;
                }

                // PC 0x5443F0: four independently owned slots, exactly one hundred native pixels apart.
                var backupX = nativeX + (slot == 2 ? -100 : slot == 3 ? 100 : 0);
                var backup = _zombieSpawnSystem.SpawnAdventureZombie(ZombieKind.BackupDancer, row, 0f, zombie.Wave);
                backup.X = AdventureZombieNativeRules.GridX(backupX, AdventureDancerRules.Scale(backup));
                backup.PresentationPreviousX = backup.X;
                backup.Hypnotized = zombie.Hypnotized;
                backup.PresentationNativePhase = 50;
                backup.PresentationAnimationAge = 0f;
                backup.DancerState!.LeaderId = zombie.Id;
                backup.DancerState.PhaseTicks = AdventureDancerRules.RiseTicks;
                state.Followers[slot] = backup.Id;
                _world.Presentation.ZombieRiseEffects.Add(new AdventureZombieRiseEffect
                {
                    Id = ++_world.Presentation._effectId,
                    Row = row,
                    X = backupX + 60f,
                    Y = _biteSystem.AdventureBiteNativeY(backup, backupX) + 110f,
                    Dancer = true
                });
                _zombieFeedbackSystem.RequestAdventureZombieFoley(79);
            }
        }
    }
}
