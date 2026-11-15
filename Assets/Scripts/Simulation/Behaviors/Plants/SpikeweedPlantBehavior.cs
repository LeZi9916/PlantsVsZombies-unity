#nullable enable
using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SpikeweedPlantBehavior state and ordered action hooks.</summary>
    internal sealed class SpikeweedPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            var x = p.Column + 0.5f;
            foreach (Zombie z in new PooledSnapshot<Zombie>(context._world.Zombies))
            {
                if (!z.Airborne && !z.Hypnotized && z.Row == p.Row && Math.Abs(z.X - x) < 0.6f)
                {
                    if (z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult)
                    {
                        if (z.Kind == ZombieKind.Zomboni)
                        {
                            context._combatSystem.HitAdventureZombie(z, 20f, true, 32U);
                        }
                        else
                        {
                            // PC DoRowAreaDamage promotes vehicle spike damage to 1800 and retains puncture death.
                            context._combatSystem.HitAdventureZombie(z, 1800f, true, 32U);
                        }

                        context._plantingSystem.RemoveAdventurePlantAndStack(p);
                        context._zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                        break;
                    }

                    context._combatSystem.HitAdventureZombie(z, 20f, true);
                    context._zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                }
            }

            p.ActionTimer = 1f;
            return;
        }
    }
}
