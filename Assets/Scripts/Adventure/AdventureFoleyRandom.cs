#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Resolves PC Foley pitch, eight native voice slots and sample selection at the simulation request boundary.</summary>
    internal sealed class AdventureFoleyRandom
    {
        /// <summary>Retains the latest native app update used by voice cleanup and explicit stops.</summary>
        private int _latestTick;
        /// <summary>Stores the original eight voice end times per Foley category.</summary>
        private readonly double[] _end = new double[104 * 8];
        /// <summary>Stores the last successful start update per voice slot.</summary>
        private readonly int[] _start = new int[104 * 8];
        /// <summary>Stores the original reference counts for shared looping voices.</summary>
        private readonly int[] _references = new int[104 * 8];
        /// <summary>Stores each category's last sample index for original flag 0x10.</summary>
        private readonly int[] _last = new int[104];

        /// <summary>Creates original Foley state without drawing or reseeding any generator.</summary>
        internal AdventureFoleyRandom()
        {
            for (var index = 0; index < _last.Length; index++)
            {
                _last[index] = -1;
            }
        }

        /// <summary>Consumes only the original admission, pitch and variation draws at their request boundary.</summary>
        /// <param name="sound">The queued original Foley request receiving immutable playback choices.</param>
        /// <param name="random">The same stream used by board and actor logic.</param>
        /// <param name="muted">The original SFX-muted gate, which suppresses both playback and draws.</param>
        internal void Resolve(AdventureZombieSoundEvent sound, Random random, bool muted)
        {
            _latestTick = Math.Max(_latestTick, sound.Tick);
            sound.Resolved = true;
            sound.SampleIndex = -1;
            if (muted || !AdventureZombieFoleyData.Definitions.TryGetValue(sound.Foley, out var definition))
            {
                return;
            }

            // PC PlayFoley draws the optional pitch before 52A5A0 performs voice admission.
            sound.ResolvedPitch = sound.Pitch + (definition.PitchRange == 0f ? 0f : AdventureSpawnRandom.NextFloat(random, definition.PitchRange));
            var first = sound.Foley * 8;
            var empty = -1;
            var active = -1;
            for (var index = first; index < first + 8; index++)
            {
                if (_end[index] > sound.Tick * .01)
                {
                    if (active < 0)
                    {
                        active = index;
                    }
                    if ((definition.Flags & 1) == 0 && sound.Tick - _start[index] < 10)
                    {
                        return;
                    }
                }
                else
                {
                    _references[index] = 0;
                    if (empty < 0)
                    {
                        empty = index;
                    }
                }
            }

            if ((definition.Flags & 2) != 0 && active >= 0)
            {
                _references[active]++;
                _start[active] = sound.Tick;
                return;
            }

            if (empty < 0)
            {
                return;
            }

            var exclude = (definition.Flags & 16) != 0 ? _last[sound.Foley] : -1;
            var count = definition.Samples.Length - (exclude >= 0 ? 1 : 0);
            var choice = random.Next(count);
            if (exclude >= 0 && choice >= exclude)
            {
                choice++;
            }

            sound.SampleIndex = choice;
            _last[sound.Foley] = choice;
            _start[empty] = sound.Tick;
            _references[empty] = 1;
            var length = AdventureFoleySampleData.Lengths[definition.Samples[choice]];
            var frequency = (int)Math.Max(100d, Math.Min(100000d, length.Rate * Math.Pow(1.0594630943592953d, sound.ResolvedPitch)));
            _end[empty] = (definition.Flags & 1) != 0 ? double.PositiveInfinity : sound.Tick * .01 + length.Samples / (double)frequency;
        }

        /// <summary>Stops native looping voices when their owning board or actor explicitly stops the Foley.</summary>
        /// <param name="foley">The original Foley category.</param>
        /// <param name="tick">The current native app update for releasing finished instances first.</param>
        internal void Stop(int foley, int tick = 0)
        {
            _latestTick = Math.Max(_latestTick, tick);
            for (var index = foley * 8; index < foley * 8 + 8; index++)
            {
                if (_end[index] <= _latestTick * .01)
                {
                    _references[index] = 0;
                    continue;
                }

                if (_references[index] > 0)
                {
                    if (--_references[index] == 0)
                    {
                        _end[index] = 0d;
                    }

                    return;
                }
            }
        }
    }
}
