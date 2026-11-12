#nullable enable

using System;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines original PC Starfruit targeting, scheduled firing, and star vectors.</summary>
    public static class AdventureStarfruitRules
    {
        /// <summary>The shooting counter assigned when a target starts the original body animation.</summary>
        public const int ShootingCountdown = 40;

        /// <summary>The original star speed in board pixels per native tick.</summary>
        public const float StarSpeed = 3.33f;

        /// <summary>Gets the original horizontal star spawn coordinate in board pixels.</summary>
        /// <param name="column">The plant's lawn column.</param>
        /// <returns>The original plant x coordinate plus 25 pixels.</returns>
        public static int LaunchX(int column)
        {
            return 40 + column * 80 + 25;
        }

        /// <summary>Gets the original vertical star spawn coordinate in board pixels.</summary>
        /// <param name="plantY">The plant's original board-pixel top coordinate.</param>
        /// <returns>The plant y coordinate plus 25 pixels.</returns>
        public static int LaunchY(int plantY)
        {
            return plantY + 25;
        }

        /// <summary>Checks one zombie against the original five-direction Starfruit target paths.</summary>
        /// <param name="plant">The Starfruit attempting to shoot.</param>
        /// <param name="zombie">The potential target.</param>
        /// <param name="body">The zombie's original full board-pixel rectangle, including vertical position.</param>
        /// <param name="plantY">The plant's original board-pixel top coordinate, including roof slope.</param>
        /// <param name="nativeVelocityX">The zombie's effective leftward speed in pixels per native tick; zero when stationary.</param>
        /// <param name="damageEligible">Whether original EffectedByDamage accepts this zombie for a star.</param>
        /// <returns>Whether this zombie can start a Starfruit attack.</returns>
        public static bool CanTarget(Plant plant, Zombie zombie, NativeRect body, int plantY, float nativeVelocityX, bool damageEligible)
        {
            if (zombie.Health <= 0f || !damageEligible)
            {
                return false;
            }

            if (zombie.Kind == ZombieKind.Boss && plant.Column >= 5)
            {
                return true;
            }

            var centerX = 40 + plant.Column * 80 + 40;
            if (zombie.Row == plant.Row)
            {
                return body.X + body.Width < centerX;
            }

            var centerY = plantY + 40;
            var width = body.Width + (zombie.Kind == ZombieKind.Digger ? 10 : 0);
            var bodyCenterX = body.X + body.Width / 2;
            var bodyCenterY = body.Y + body.Height / 2;
            var aimCenterX = body.X + width / 2;
            var distance = Math.Sqrt((double)(aimCenterX - centerX) * (aimCenterX - centerX)
                + (double)(bodyCenterY - centerY) * (bodyCenterY - centerY));
            var travelTicks = distance / StarSpeed;
            var predictedLeft = (int)(bodyCenterX - nativeVelocityX * travelTicks - width / 2f);
            if (predictedLeft + width > centerX && predictedLeft < centerX)
            {
                return true;
            }

            var predictedCenterX = predictedLeft + width / 2;
            var angle = Math.Atan2(bodyCenterY - centerY, predictedCenterX - centerX) * 180d / Math.PI;
            var nearbyRow = Math.Abs(zombie.Row - plant.Row) < 2;
            return nearbyRow
                ? angle > 20d && angle < 40d || angle > -45d && angle < -25d
                : angle > 25d && angle < 35d || angle > -38d && angle < -28d;
        }

        /// <summary>Starts the original Starfruit body animation only when a target exists.</summary>
        /// <param name="hasTarget">Whether a target passed the native five-direction check.</param>
        /// <param name="recentlyEaten">Whether the native recently eaten counter permits an attack without a target.</param>
        /// <returns>The scheduled shooting counter, or zero when no animation starts.</returns>
        public static int StartAttack(bool hasTarget, bool recentlyEaten = false)
        {
            return hasTarget || recentlyEaten ? ShootingCountdown : 0;
        }

        /// <summary>Advances an already scheduled attack without checking its target again.</summary>
        /// <param name="countdown">The pending shooting counter, updated for this native tick.</param>
        /// <param name="animationResetReady">Whether the body animation can leave its held final frame.</param>
        /// <returns>Whether five stars must be fired on this tick.</returns>
        public static bool AdvanceAttack(ref int countdown, bool animationResetReady)
        {
            if (countdown <= 0)
            {
                return false;
            }

            countdown--;
            if (countdown == 1)
            {
                return true;
            }

            if (countdown == 0 && !animationResetReady)
            {
                countdown = 1;
            }

            return false;
        }

        /// <summary>Gets one original star's horizontal speed in pixels per native tick.</summary>
        /// <param name="index">The star index from zero through four.</param>
        /// <returns>The original horizontal velocity.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is outside the five-star burst.</exception>
        public static float VelocityX(int index)
        {
            if (index < 0 || index > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return index == 0 ? -StarSpeed : index <= 2 ? 0f : (float)(Math.Cos(Math.PI / 6d) * StarSpeed);
        }

        /// <summary>Gets one original star's vertical speed in pixels per native tick.</summary>
        /// <param name="index">The star index from zero through four.</param>
        /// <returns>The original vertical velocity; positive points down the original board.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is outside the five-star burst.</exception>
        public static float VelocityY(int index)
        {
            if (index < 0 || index > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return index == 1 ? StarSpeed : index == 2 ? -StarSpeed
                : index == 3 ? StarSpeed * .5f : index == 4 ? -StarSpeed * .5f : 0f;
        }
    }
}
