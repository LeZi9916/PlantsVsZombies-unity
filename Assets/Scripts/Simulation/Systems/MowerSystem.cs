#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns MowerSystem responsibilities for one deterministic match.</summary>
    internal sealed class MowerSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal MowerSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, ZombieLifecycle zombieLifecycle)
        {
            _adventureModeRunner = adventureModeRunner;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Performs advance adventure mowers.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureMowers(float dt)
        {
            foreach (Mower m in _world.Mowers)
            {
                if (!m.Active)
                {
                    continue;
                }

                m.X += 5f * dt;
                foreach (Zombie z in new PooledSnapshot<Zombie>(_world.Zombies))
                {
                    if (!z.Hypnotized && z.Row == m.Row && Math.Abs(z.X - m.X) < 0.7f)
                    {
                        z.PresentationDeath = AdventureZombieDeathPresentation.Mowed;
                        _zombieLifecycle.KillAdventureZombie(z);
                    }
                }

                if (m.X > 10f || _adventureModeRunner.Adventure.Level.IsWaterRow(m.Row) && !_world.Rules.AdventurePoolCleaners && m.X > 1.5f)
                {
                    m.Active = false;
                }
            }
        }
    }
}
