#nullable enable

using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ShooterPlantBehavior state and ordered action hooks.</summary>
    internal sealed class ShooterPlantBehavior : PlantBehavior
    {
        /// <inheritdoc/>
        public override void Initialize(Plant plant, PlantBehaviorContext context)
        {
            context._plantTimingSystem.InitializeTimedPlant(plant);
        }
        /// <inheritdoc/>
        public override bool AdvanceNative(Plant plant, PlantBehaviorContext context)
        {
            return context._plantTimingSystem.AdvanceTimedPlant(plant);
        }
        /// <inheritdoc/>
        public override void Act(Plant p, PlantBehaviorContext context)
        {
            var x = p.Column + 0.5f;
            var range = p.Kind == PlantKind.PuffShroom || p.Kind == PlantKind.SeaShroom ? 3f : 20f;
            var air = p.Kind == PlantKind.Cactus;
            if (p.Kind == PlantKind.ScaredyShroom && context._boardQueries.HasZombieNear(p.Row, x, 1, 1.5f, true))
            {
                return;
            }

            var shortPuff = AdventurePuffShroomRules.IsShortRange(p.Kind);
            var shooterTarget = shortPuff ? context._boardQueries.FindAdventurePuffTarget(p) : context._boardQueries.FindAdventureTarget(p.Row, x - 0.1f, x + range, air);
            if (shooterTarget != null || !shortPuff && context._projectileSystem.CanShootAdventureBoss(p))
            {
                var lob = p.Kind == PlantKind.CabbagePult || p.Kind == PlantKind.KernelPult || p.Kind == PlantKind.MelonPult;
                var shot = context._projectileSystem.FireAdventurePea(p, p.Row, false, lob ? p.Kind == PlantKind.MelonPult ? 80f : p.Kind == PlantKind.CabbagePult ? 40f : 20f : 20f);
                shot.IsSnow = p.Kind == PlantKind.SnowPea;
                shot.HitsAir = air;
                shot.Range = shortPuff ? float.MaxValue : range;
                shot.Lobbed = lob;
                shot.PiercesShield = lob;
                shot.Splash = p.Kind == PlantKind.MelonPult;
                if (p.Kind == PlantKind.KernelPult)
                {
                    shot.Butter = p._kernelWeaponPrepared ? p.PresentationButter : context._world._random.Next(4) == 0;
                    p._kernelWeaponPrepared = false;
                    p.PresentationButter = false;
                    if (shot.Butter)
                    {
                        shot.Damage = 40;
                    }
                }

                if (p.Kind == PlantKind.Repeater)
                {
                    context._projectileSystem.FireAdventurePea(p, p.Row, false, 20f).X -= 0.3f;
                }

                p.ActionTimer = lob ? 2.9f : 1.425f;
            }

            return;
        }
    }
}
