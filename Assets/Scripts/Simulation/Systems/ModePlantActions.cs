#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ModePlantActions responsibilities for one deterministic match.</summary>
    internal sealed class ModePlantActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ModePlantActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        internal void Connect(ProjectileSystem projectileSystem)
        {
            _projectileSystem = projectileSystem;
        }

        /// <summary>Performs tick mode backward plants.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModeBackwardPlants(float dt)
        {
            foreach (var p in _world.Plants)
            {
                if (p.ModeFacesLeft && p.Kind != PlantKind.Repeater)
                {
                    p.ActionTimer = 100000f;
                    p._modeFireTimer -= dt;
                    if (p._modeFireTimer <= 0f && HasBackwardTarget(p))
                    {
                        _projectileSystem.FireAdventurePea(p, p.Row, true, 20f);
                        _projectileSystem.FireAdventurePea(p, p.Row, true, 20f).X += 0.3f;
                        p._modeFireTimer = 1.45f;
                    }
                }
            }
        }
        /// <summary>Checks the existing ordered zombie view without creating a per-plant captured predicate.</summary>
        /// <param name="plant">The backward-facing plant whose native row and horizontal boundary are queried.</param>
        /// <returns>Whether the first matching non-hypnotized zombie lies strictly left of the native firing boundary.</returns>
        private bool HasBackwardTarget(Plant plant)
        {
            foreach (var zombie in _world.Zombies)
            {
                if (zombie.Row == plant.Row && zombie.X < plant.Column + 0.5f && !zombie.Hypnotized)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
