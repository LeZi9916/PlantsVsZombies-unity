#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure zombie ground clip.</summary>
    public sealed class AdventureZombieGroundClip
    {
        /// <summary>Stores count.</summary>
        public readonly int Count;
        /// <summary>Stores source rate.</summary>
        public readonly float SourceRate;
        /// <summary>Stores ground.</summary>
        public readonly float[]? Ground;
        /// <summary>Initializes adventure zombie ground clip.</summary>
        /// <param name = "count">The count.</param>
        /// <param name = "rate">The rate.</param>
        /// <param name = "ground">The ground.</param>
        public AdventureZombieGroundClip(int count, float rate, float[]? ground)
        {
            Count = count;
            SourceRate = rate;
            Ground = ground;
        }

        /// <summary>Gets or sets ground distance.</summary>
        public float GroundDistance
        {
            get
            {
                return Ground == null || Ground.Length < 2 ? 0f : Ground[Ground.Length - 1] - Ground[0];
            }
        }

        // PC 0x482180 / 0x482DB0: adjacent-frame velocity, not the slope of a fitted curve.
        /// <summary>Performs velocity.</summary>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "playbackRate">The playback rate.</param>
        /// <param name = "velocityRate">The velocity rate.</param>
        /// <returns>The resulting velocity.</returns>
        public float Velocity(float seconds, float playbackRate, float velocityRate)
        {
            if (Ground == null || Ground.Length < 2 || Count < 2)
            {
                return 0f;
            }

            var phase = (float)(Math.Max(0d, seconds) * Math.Max(0d, playbackRate) / Count);
            phase -= (float)Math.Floor(phase);
            return VelocityAtPhase(phase, velocityRate);
        }

        /// <summary>Performs velocity at phase.</summary>
        /// <param name = "phase">The phase.</param>
        /// <param name = "velocityRate">The velocity rate.</param>
        /// <returns>The resulting velocity at phase.</returns>
        public float VelocityAtPhase(float phase, float velocityRate)
        {
            if (Ground == null || Ground.Length < 2 || Count < 2)
            {
                return 0f;
            }

            var before = Math.Min(Count - 1, (int)((Count - 1) * phase));
            var after = Math.Min(Count - 1, before + 1);
            return (Ground[after] - Ground[before]) * .01f * velocityRate;
        }
    }
}
