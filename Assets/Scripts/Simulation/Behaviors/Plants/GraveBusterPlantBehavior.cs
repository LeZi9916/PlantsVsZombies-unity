#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns GraveBusterPlantBehavior state and ordered action hooks.</summary>
    internal sealed class GraveBusterPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override bool AdvanceAction(Plant plant, PlantBehaviorContext context, float dt)
        {
            context._explosionEffects.StartAdventureGraveBusterEating(plant);
            return false;
        }
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            var x = p.Column + 0.5f;
            for (var i = context._adventureModeRunner.Adventure._graves.Count - 1; i >= 0; i--)
            {
                var grave = context._adventureModeRunner.Adventure._graves[i];
                if (grave.Row == p.Row && grave.Column == p.Column)
                {
                    context._adventureModeRunner.Adventure._graves.RemoveAt(i);
                }
            }

            // Plant::UpdateGraveBuster: GraveBusterDie particles, Die(), then DropLootPiece(mX + 40, mY, 12).
            context._explosionEffects.AdventurePresentationGraveBuster(p, global::PlantsVsZombies.Adventure.AdventureExplosionEffect.GraveBusterDie);
            context._plantingSystem.RemoveAdventurePlantAndStack(p);
            context._lootSystem.DropAdventureLootPiece(x, p.Row, global::PlantsVsZombies.Adventure.AdventureGraveBusterRules.LootDropFactor);
            return;
        }
    }
}
