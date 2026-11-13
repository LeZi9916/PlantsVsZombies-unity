#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original Scaredy-shroom lowering, frightened idle, and raising animations.</summary>
    public sealed class AdventureScaredyShroomState
    {
        /// <summary>Gets the original plant state: ready 1, lowering 20, scared 21, and raising 22.</summary>
        public int NativePhase { get; private set; } = 1;
        /// <summary>Gets the native updates elapsed since the current body animation started.</summary>
        public int AnimationTicks { get; private set; }
        /// <summary>Gets the authored body animation selected by the original state transition.</summary>
        public string Animation { get; private set; } = "anim_idle";
        /// <summary>Gets the body's animation rate, with zero selecting the plant's initial idle rate.</summary>
        public float Rate { get; private set; }
        /// <summary>Gets whether the current authored animation repeats.</summary>
        public bool Loop => NativePhase == 1 || NativePhase == 21;

        /// <summary>Checks whether the next original transition chooses a random animation rate.</summary>
        /// <param name="nearby">Whether a hostile zombie overlaps the original fear circle.</param>
        /// <param name="shooting">Whether the original shooting counter still owns the body.</param>
        /// <returns>Whether one uniform random value is required by the next update.</returns>
        public bool NeedsRandom(bool nearby, bool shooting)
        {
            return !shooting && (NativePhase == 21 && !nearby || NativePhase == 22 && (AnimationTicks + 1) * Rate >= 1000f);
        }

        /// <summary>Advances Plant::UpdateScaredyShroom after shooting and before the launch counter.</summary>
        /// <param name="nearby">Whether the original 120-pixel fear circle touches a hostile zombie.</param>
        /// <param name="shooting">Whether the active shot has not finished its authored animation.</param>
        /// <param name="randomUnit">The uniform value in [0,1) used only when choosing a new rate.</param>
        /// <param name="finishedShooting">Whether the original shooting update has just restored the idle body.</param>
        public void Tick(bool nearby, bool shooting, float randomUnit, bool finishedShooting = false)
        {
            AnimationTicks++;
            if (shooting)
            {
                return;
            }

            if (finishedShooting)
            {
                Enter(1, "anim_idle", 12f);
            }

            switch (NativePhase)
            {
                case 1:
                    if (nearby)
                    {
                        Enter(20, "anim_scared", 10f);
                    }

                    break;
                case 20:
                    // The original anim_scared layer contains thirteen frames.
                    if (AnimationTicks * Rate >= 1300f)
                    {
                        Enter(21, "anim_scaredidle", Rate);
                    }

                    break;
                case 21:
                    if (!nearby)
                    {
                        Enter(22, "anim_grow", 7f + randomUnit * 5f);
                    }

                    break;
                case 22:
                    // The original anim_grow layer contains ten frames.
                    if (AnimationTicks * Rate >= 1000f)
                    {
                        Enter(1, "anim_idle", 10f + randomUnit * 5f);
                    }

                    break;
            }
        }

        /// <summary>Samples the current original body animation without advancing simulation state.</summary>
        /// <param name="remainder">The nonnegative render fraction after the latest native update.</param>
        /// <returns>The elapsed playback time within the current body animation.</returns>
        public float SampleSeconds(float remainder)
        {
            return AnimationTicks * .01f + Math.Max(0f, remainder);
        }

        /// <summary>Starts the authored animation selected by one original state transition.</summary>
        /// <param name="phase">The original plant state identifier.</param>
        /// <param name="animation">The original animation layer name.</param>
        /// <param name="rate">The animation's frame rate.</param>
        private void Enter(int phase, string animation, float rate)
        {
            NativePhase = phase;
            Animation = animation;
            Rate = rate;
            AnimationTicks = 0;
        }
    }
}
