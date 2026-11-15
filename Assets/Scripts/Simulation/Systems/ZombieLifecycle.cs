#nullable enable
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ZombieLifecycle responsibilities for one deterministic match.</summary>
    internal sealed class ZombieLifecycle
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides BungeeZombieActions operations.</summary>
        private BungeeZombieActions _bungeeZombieActions = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides LootSystem operations.</summary>
        private LootSystem _lootSystem = null!;
        /// <summary>Provides RewardSystem operations.</summary>
        private RewardSystem _rewardSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ZombieLifecycle(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="bungeeZombieActions">The BungeeZombieActions collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="lootSystem">The LootSystem collaborator.</param>
        /// <param name="rewardSystem">The RewardSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(BungeeZombieActions bungeeZombieActions, LegacySimulation legacySimulation, LootSystem lootSystem, RewardSystem rewardSystem, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _bungeeZombieActions = bungeeZombieActions;
            _legacySimulation = legacySimulation;
            _lootSystem = lootSystem;
            _rewardSystem = rewardSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs kill adventure zombie.</summary>
        /// <param name = "z">The z.</param>
        /// <param name="damageFlags">The original damage flags controlling whether death animation is permitted.</param>
        internal void KillAdventureZombie(Zombie z, uint damageFlags = 0)
        {
            if (!_world.Zombies.Remove(z))
            {
                return;
            }

            if (z.Kind == ZombieKind.Bungee)
            {
                _bungeeZombieActions.ReleaseAdventureBungeeCargo(z);
            }

            if (z.BobsledState != null)
            {
                var leaderId = z.BobsledState.LeaderId;
                z.BobsledState = null;
                foreach (var member in new PooledSnapshot<Zombie>(_world.Zombies))
                {
                    if (member.BobsledState?.LeaderId != leaderId)
                    {
                        continue;
                    }

                    member.BobsledState = null;
                    if (z.PresentationDeath == AdventureZombieDeathPresentation.Burned)
                    {
                        // PC BobsledBurn preserves every rider's burned pose without another loot roll.
                        member.PresentationDeath = AdventureZombieDeathPresentation.Burned;
                        member.PresentationHealthBeforeHit = member.Health;
                        member.Health = 0f;
                        _world.Zombies.Remove(member);
                        _zombieFeedbackSystem.AdventurePresentationZombieDeath(member, damageFlags);
                        _legacySimulation.Raise(GameEvent.ZombieDied);
                    }
                    else
                    {
                        member.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                        KillAdventureZombie(member, damageFlags);
                    }
                }
            }

            if (_world.ModeSession == null)
            {
                _rewardSystem.RememberAdventureRewardPosition(z);
            }

            _zombieFeedbackSystem.AdventurePresentationZombieDeath(z, damageFlags);
            if (z.PresentationDeath != AdventureZombieDeathPresentation.Removed)
            {
                _lootSystem.DropAdventureLoot(z);
            }

            _legacySimulation.Raise(GameEvent.ZombieDied);
        }
    }
}
