#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC 541460's twenty-shot ammunition, independent reload timer and punctured vehicle death.</summary>
    public sealed class AdventureCatapultState
    {
        /// <summary>Stores the original summon counter, decremented after the complete shooting clip.</summary>
        public int Ammunition = 20;
        /// <summary>Stores native updates until the next launch, counted from the preceding launch start.</summary>
        public int PhaseTicks;
        /// <summary>Prevents repeating the shooting clip's normalized .545 launch event.</summary>
        public bool Fired;
        /// <summary>Stores the original initial 5.5-FPS walk, replaced by six FPS after leaving the firing position.</summary>
        public float WalkingRate = 5.5f;
        /// <summary>Identifies the original 280-update tire-puncture death.</summary>
        public bool FlatTires;
        /// <summary>Stores updates before the punctured vehicle explodes.</summary>
        public int DeathTicks;
        /// <summary>Stores the actor age at which original engine smoke was attached, or minus one before damage.</summary>
        public float SmokeStartAge = -1f;
        /// <summary>Prevents duplicate native explosion particles and Foley.</summary>
        public bool Exploded;

        /// <summary>Resolves the authored clip for the original native phase.</summary>
        /// <param name="phase">The retained ZombiePhase identifier.</param>
        /// <returns>The original walking, shooting, idling or bouncing clip.</returns>
        internal string Animation(int phase)
        {
            return FlatTires ? "anim_bounce" : phase == 67 ? "anim_shoot" : phase == 68 ? "anim_idle" : "anim_walk";
        }

        /// <summary>Resolves the original animation rate independently of its movement velocity.</summary>
        /// <param name="phase">The retained ZombiePhase identifier.</param>
        /// <returns>The original source playback rate in frames per second.</returns>
        internal float Rate(int phase)
        {
            return FlatTires || phase == 68 ? 12f : phase == 67 ? 24f : WalkingRate;
        }

        /// <summary>Checks PC 541518's progressive basket-ball render-group removal.</summary>
        /// <param name="track">The original reanimation track name.</param>
        /// <returns>Whether the retained ammunition keeps this track visible.</returns>
        internal bool TrackVisible(string track)
        {
            if (string.Equals(track, "Zombie_catapult_basketball", StringComparison.OrdinalIgnoreCase))
            {
                return Ammunition > 4;
            }

            if (string.Equals(track, "Zombie_catapult_basketball2", StringComparison.OrdinalIgnoreCase))
            {
                return Ammunition > 3;
            }

            if (string.Equals(track, "Zombie_catapult_basketball3", StringComparison.OrdinalIgnoreCase))
            {
                return Ammunition > 2;
            }

            return !string.Equals(track, "Zombie_catapult_basketball4", StringComparison.OrdinalIgnoreCase) || Ammunition > 1;
        }

        /// <summary>Resolves the original damaged siding and pole without replacing the source clip's ball visibility.</summary>
        /// <param name="track">The original body track.</param>
        /// <param name="source">The current frame's unchanged source image identity.</param>
        /// <param name="damage">The native body damage index, from zero to two.</param>
        /// <returns>The original override resource, or null to preserve the source image.</returns>
        internal string? Image(string track, string source, int damage)
        {
            if (string.Equals(track, "Zombie_catapult_siding", StringComparison.OrdinalIgnoreCase))
            {
                return damage > 0 || FlatTires ? "IMAGE_REANIM_ZOMBIE_CATAPULT_SIDING_DAMAGE" : null;
            }

            if (!string.Equals(track, "Zombie_catapult_pole", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (damage == 2 || FlatTires)
            {
                return Ammunition > 0 && source.EndsWith("Zombie_catapult_pole_withball.png", StringComparison.OrdinalIgnoreCase)
                    ? "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE_DAMAGE_WITHBALL" : "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE_DAMAGE";
            }

            return Ammunition == 0 ? "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE" : null;
        }
    }
}
