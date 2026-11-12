#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines the original Grave Buster landing, eating, sinking and effect timings (Plant::UpdateGraveBuster).</summary>
    public static class AdventureGraveBusterRules
    {
        /// <summary>The duration of the original anim_land reanimation: 7 frames at 12 fps, ended on the 59th 1/100 s tick.</summary>
        public const float LandingSeconds = 0.59f;
        /// <summary>The original GRAVE_BUSTER_EAT_TIME of 400 ticks, in seconds.</summary>
        public const float EatSeconds = 4f;
        /// <summary>The total lifetime of a Grave Buster, from planting until the gravestone is removed.</summary>
        public const float TotalSeconds = LandingSeconds + EatSeconds;
        /// <summary>The tolerance that absorbs accumulated float error while the action timer counts down.</summary>
        private const float TimerTolerance = 0.0005f;
        /// <summary>The number of pixels the plant sinks into the ground while it eats (Plant::Animate).</summary>
        public const float SinkPixels = 30f;
        /// <summary>The drop factor passed to Board::DropLootPiece when the gravestone is eaten.</summary>
        public const int LootDropFactor = 12;

        /// <summary>Tests whether the plant has finished landing and is eating the gravestone.</summary>
        /// <param name="actionTimer">The plant's remaining lifetime in seconds.</param>
        /// <returns>Whether the plant is in the original GravebusterEating state.</returns>
        public static bool IsEating(float actionTimer)
        {
            return actionTimer <= EatSeconds + TimerTolerance;
        }

        /// <summary>Returns the original eating countdown in 1/100 s ticks, counting down from 400 to 0.</summary>
        /// <param name="actionTimer">The plant's remaining lifetime in seconds.</param>
        /// <returns>The remaining eating ticks, clamped to 0..400, or 400 while the plant is still landing.</returns>
        public static int EatingTicksRemaining(float actionTimer)
        {
            var ticks = (int)System.Math.Round(actionTimer * 100f);
            return ticks < 0 ? 0 : ticks > 400 ? 400 : ticks;
        }

        /// <summary>Returns how far the plant has sunk while eating.</summary>
        /// <param name="actionTimer">The plant's remaining lifetime in seconds.</param>
        /// <returns>The downward offset in pixels, from 0 (just landed) to <see cref="SinkPixels"/>.</returns>
        public static float SinkOffset(float actionTimer)
        {
            return IsEating(actionTimer) ? SinkPixels * (1f - EatingTicksRemaining(actionTimer) / 400f) : 0f;
        }
    }
}
