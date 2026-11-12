#nullable enable
using PlantsVsZombies.Simulation;

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie corpse.</summary>
    public sealed class AdventureZombieCorpse
    {
        /// <summary>Stores zombie.</summary>
        public Zombie? Zombie;

        /// <summary>Gets the zombie, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public Zombie RequiredZombie => Zombie ?? throw new System.InvalidOperationException("The member 'Zombie' is not available.");
        /// <summary>Stores animation.</summary>
        public string? Animation;
        /// <summary>Stores burn.</summary>
        public AdventureBurnPose? Burn;

        /// <summary>Gets the burn, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public AdventureBurnPose RequiredBurn => Burn ?? throw new System.InvalidOperationException("The member 'Burn' is not available.");
        /// <summary>Stores frozen seconds, frozen rate, frozen head seconds.</summary>
        public float FrozenSeconds, FrozenRate, FrozenHeadSeconds;
        /// <summary>Stores age, rate, rate scale, duration.</summary>
        public float Age, Rate, RateScale = 1f, Duration = float.PositiveInfinity;
        /// <summary>Stores animation duration, fade duration.</summary>
        public float AnimationDuration, FadeDuration;
        /// <summary>Stores in pool, fall sound played.</summary>
        public bool InPool, FallSoundPlayed;
        /// <summary>Performs alpha.</summary>
        /// <param name = "renderRemainder">The render remainder.</param>
        /// <returns>The resulting alpha.</returns>
        public float Alpha(float renderRemainder = 0f)
        {
            if (Burn != null)
            {
                return 1f;
            }

            var fadeAge = Age + renderRemainder - AnimationDuration;
            if (fadeAge <= 0f)
            {
                return 1f;
            }

            var remaining = Math.Max(0, (int)Math.Round(FadeDuration * 100f) - (int)Math.Floor(fadeAge * 100f + 0.0001f));
            // PC 0x548D40 uses min(255,255*fade/10), NOT fade/100 or mobile fade/30.
            return Math.Min(255, 255 * remaining / 10) / 255f;
        }
    }
}
