#nullable enable
using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC UpdateCactus (0x46F5B0) body states and the original authored animation clocks.</summary>
    public sealed class AdventureCactusState
    {
        /// <summary>Gets the native plant state: low 30, rising 31, high 32 or lowering 33.</summary>
        public int NativePhase { get; private set; } = 30;
        /// <summary>Gets the body animation selected by the last native transition.</summary>
        public string Animation { get; private set; } = "anim_idle";
        /// <summary>Gets elapsed native updates within the current body animation.</summary>
        public int AnimationTicks { get; private set; }
        /// <summary>Gets the current body animation's frame rate.</summary>
        public float Rate { get; private set; }
        /// <summary>Gets whether the current body animation repeats.</summary>
        public bool Loop => Animation == "anim_idle" || Animation == "anim_idlehigh";
        /// <summary>Gets whether the non-looping authored animation has reached its original final frame.</summary>
        public bool AnimationFinished => Loop || AnimationTicks * Rate >= FrameCount * 100f;

        /// <summary>Gets the frame count from the unchanged original Cactus.reanim animation marker tracks.</summary>
        private int FrameCount => Animation == "anim_rise" ? 15 : Animation == "anim_lower" ? 13
            : Animation == "anim_shootinghigh" ? 14 : 18;

        /// <summary>Creates the original low cactus while retaining its randomized initial idle rate.</summary>
        /// <param name="initialRate">The existing plant idle rate; a nonpositive rate selects the authored twelve fps.</param>
        public AdventureCactusState(float initialRate = 12f)
        {
            Rate = initialRate > 0f ? initialRate : 12f;
        }

        /// <summary>Advances the body animation before the original shooting and state updates.</summary>
        public void AdvanceAnimation()
        {
            AnimationTicks++;
        }

        /// <summary>Runs the original cactus transitions after shooting but before the launch counter update.</summary>
        /// <param name="airTarget">Whether the primary air-only targeting query currently succeeds.</param>
        /// <param name="shooting">Whether UpdateShooting still owns the original shooting counter.</param>
        public void Update(bool airTarget, bool shooting)
        {
            if (shooting)
            {
                return;
            }

            switch (NativePhase)
            {
                case 30:
                    if (airTarget)
                    {
                        Enter(31, "anim_rise", 12f);
                    }

                    break;
                case 31:
                    if (AnimationFinished)
                    {
                        Enter(32, "anim_idlehigh", 12f);
                    }

                    break;
                case 32:
                    if (!airTarget)
                    {
                        Enter(33, "anim_lower", 12f);
                    }

                    break;
                case 33:
                    if (AnimationFinished)
                    {
                        Enter(30, "anim_idle", 12f);
                    }

                    break;
            }
        }

        /// <summary>Starts the high or low body shot selected by the original weapon gate.</summary>
        public void BeginShooting()
        {
            Enter(NativePhase, NativePhase == 32 ? "anim_shootinghigh" : "anim_shooting", 35f);
        }

        /// <summary>Restores the original twelve-fps idle once UpdateShooting observes animation completion.</summary>
        public void FinishShooting()
        {
            Enter(NativePhase, NativePhase == 32 ? "anim_idlehigh" : "anim_idle", 12f);
        }

        /// <summary>Samples the authored body clock without advancing simulation.</summary>
        /// <param name="remainder">The nonnegative time after the latest native update.</param>
        /// <returns>The elapsed animation time in seconds.</returns>
        public float SampleSeconds(float remainder)
        {
            return AnimationTicks * .01f + Math.Max(0f, remainder);
        }

        /// <summary>Applies the original air-only primary or ground-only secondary attack rectangle.</summary>
        /// <param name="plant">The cactus acquiring its native weapon target.</param>
        /// <param name="zombie">The potential zombie target.</param>
        /// <param name="air">Whether the original primary air weapon is being queried.</param>
        /// <returns>Whether the zombie passes native state, row, range and damage-mask checks.</returns>
        public static bool CanTarget(Plant plant, Zombie zombie, bool air)
        {
            if (zombie.Hypnotized || zombie.Row != plant.Row || zombie.Airborne != air || zombie.Submerged
                || zombie.DropRemaining > 0f || zombie.Kind == ZombieKind.Bungee
                || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, PlantKind.Cactus))
            {
                return false;
            }

            // PC GetPlantAttackRect 0x4765B0 defaults to [plant.mX + 60, plant.mX + 860].
            var left = 40 + plant.Column * 80 + 60;
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            return body.X + body.Width >= left && body.X <= left + 800;
        }

        /// <summary>Starts one original body animation and resets its native elapsed update count.</summary>
        /// <param name="phase">The native cactus phase that owns the animation.</param>
        /// <param name="animation">The original Cactus.reanim marker track.</param>
        /// <param name="rate">The selected authored playback rate.</param>
        private void Enter(int phase, string animation, float rate)
        {
            NativePhase = phase;
            Animation = animation;
            Rate = rate;
            AnimationTicks = 0;
        }
    }
}
