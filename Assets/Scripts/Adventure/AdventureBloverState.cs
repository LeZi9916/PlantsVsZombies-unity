#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original Blover's authored blow, looping body, delayed effect, and special-state lifetime.</summary>
    public sealed class AdventureBloverState
    {
        /// <summary>Gets elapsed native updates since planting began the twenty-fps blow animation.</summary>
        public int AgeTicks { get; private set; }
        /// <summary>Gets the remaining original fifty-update delay before wind and fog clearing begin.</summary>
        public int SpecialTicks { get; private set; } = 50;
        /// <summary>Gets the original two-hundred-update lifetime remaining after its special action starts.</summary>
        public int DisappearTicks { get; private set; } = 200;
        /// <summary>Gets whether the original one-time wind effect has started.</summary>
        public bool Triggered { get; private set; }
        /// <summary>Gets whether Plant::UpdateAbilities has passed the final zero-count special update.</summary>
        public bool Expired => DisappearTicks < 0;
        /// <summary>Gets whether the original twenty-frame blow clip has finished and the ten-frame wind loop repeats.</summary>
        public bool Loop => AgeTicks >= 100;
        /// <summary>Gets the unchanged original Blover.reanim marker used by its current body phase.</summary>
        public string Animation => Loop ? "anim_loop" : "anim_blow";
        /// <summary>Gets the original body playback rate retained when UpdateBlover switches to its loop.</summary>
        public const float Rate = 20f;

        /// <summary>Advances one native playing update using Plant::UpdateAbilities and UpdateBlover ordering.</summary>
        /// <returns>Whether the original fifty-update special counter reached zero on this update.</returns>
        internal bool Advance()
        {
            AgeTicks++;
            if (Triggered)
            {
                DisappearTicks--;
                return false;
            }

            SpecialTicks--;
            if (SpecialTicks != 0)
            {
                return false;
            }

            Triggered = true;
            return true;
        }

        /// <summary>Samples the active original body clip independently of frame presentation frequency.</summary>
        /// <param name="remainder">The nonnegative time after the latest native update.</param>
        /// <returns>The elapsed time within the initial blow or its repeating wind clip.</returns>
        public float SampleSeconds(float remainder)
        {
            return (Loop ? AgeTicks - 100 : AgeTicks) * .01f + Math.Max(0f, remainder);
        }
    }
}
