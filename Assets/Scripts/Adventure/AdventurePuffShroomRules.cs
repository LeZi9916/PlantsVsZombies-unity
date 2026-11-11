#nullable enable

using System;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines original PC short-range mushroom targeting and projectile coordinates.</summary>
    public static class AdventurePuffShroomRules
    {
        /// <summary>The original number of 100 Hz updates before a short-range spore disappears.</summary>
        public const int LifetimeTicks = 75;
        /// <summary>The original 3.33 pixels per native tick expressed in logical lawn columns per second.</summary>
        public const float GridSpeed = 3.33f * 100f / 80f;

        /// <summary>Checks whether the source uses the PC short-range puff motion.</summary>
        /// <param name="kind">The plant that fired the spore.</param>
        /// <returns>Whether the projectile uses the seventy-five-tick puff lifetime.</returns>
        public static bool IsShortRange(PlantKind kind)
        {
            return kind == PlantKind.PuffShroom || kind == PlantKind.SeaShroom;
        }

        /// <summary>Gets the original 230-pixel attack rectangle starting sixty pixels into the plant cell.</summary>
        /// <param name="column">The plant's lawn column.</param>
        /// <returns>The original horizontal targeting interval in board pixels.</returns>
        public static NativeRect AttackRectangle(int column)
        {
            // PC Plant::GetPlantAttackRect 0x4765B0, seeds 8 and 24.
            return new NativeRect(40 + column * 80 + 60, 0, 230, 80);
        }

        /// <summary>Checks the original target body rectangle rather than the zombie's actor origin.</summary>
        /// <param name="plant">The mushroom acquiring a target.</param>
        /// <param name="zombie">The potential combat target.</param>
        /// <returns>Whether the zombie passes the native state and attack-rectangle checks.</returns>
        public static bool CanTarget(Plant plant, Zombie zombie)
        {
            if (zombie.Hypnotized || zombie.Row != plant.Row || zombie.Airborne || zombie.Submerged
                || zombie.DropRemaining > 0f || zombie.Kind == ZombieKind.Bungee
                || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, plant.Kind))
            {
                return false;
            }

            var attack = AttackRectangle(plant.Column);
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            return body.X + body.Width >= attack.X && body.X <= attack.X + attack.Width;
        }

        /// <summary>Gets the native sprite center at the original mushroom muzzle.</summary>
        /// <param name="kind">The short-range mushroom species.</param>
        /// <param name="column">The plant's lawn column.</param>
        /// <returns>The initial sprite center in logical lawn columns.</returns>
        public static float LaunchCenterX(PlantKind kind, int column)
        {
            // PC Plant::Fire 0x4752E0 creates a 32x27 puff at plant+(40,40), or sea+(45,63).
            return column + ((kind == PlantKind.SeaShroom ? 45f : 40f) + 16f) / 80f;
        }

        /// <summary>Gets the native projectile sprite's center height at launch.</summary>
        /// <param name="kind">The short-range mushroom species.</param>
        /// <param name="plantY">The original plant cell Y coordinate, including the roof slope.</param>
        /// <param name="onFlowerPot">Whether the plant sits on a flower pot.</param>
        /// <returns>The unscaled 32x27 puff image center in original board pixels.</returns>
        public static float LaunchCenterY(PlantKind kind, float plantY, bool onFlowerPot)
        {
            return plantY + (kind == PlantKind.SeaShroom ? 63f : 40f) + 13.5f - (onFlowerPot ? 5f : 0f);
        }

        /// <summary>Checks puff retirement before collision on native update seventy-five.</summary>
        /// <param name="seconds">The elapsed projectile age in seconds.</param>
        /// <returns>Whether the original puff lifetime has ended.</returns>
        public static bool Expired(float seconds)
        {
            return (int)Math.Round(seconds * 100f) >= LifetimeTicks;
        }

        /// <summary>Checks the original forty-pixel collision box against the zombie's body rectangle.</summary>
        /// <param name="projectile">The current short-range spore.</param>
        /// <param name="zombie">The potential collision target.</param>
        /// <returns>Whether the original horizontal collision rectangles touch or overlap.</returns>
        public static bool Collides(Pea projectile, Zombie zombie)
        {
            // PC ProjectileInitialize 0x47BB30 gives puff a 40x40 logical hitbox; image size is independent.
            var projectileLeft = (int)(40f + projectile.X * 80f - 16f);
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            return projectileLeft + 40 >= body.X && projectileLeft <= body.X + body.Width;
        }

        /// <summary>Samples the original puff image growth over its first thirty native ticks.</summary>
        /// <param name="seconds">The elapsed projectile age in seconds.</param>
        /// <returns>The native sprite scale, from 0.3 to 1.0.</returns>
        public static float ImageScale(float seconds)
        {
            // PC Projectile::Draw 0x47D950 uses curve1 over age0..30.
            return .3f + .7f * Math.Max(0f, Math.Min(1f, seconds / .3f));
        }
    }
}
