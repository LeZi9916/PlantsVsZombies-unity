#nullable enable
using System;

namespace PlantsVsZombies.Audio
{
    /// <summary>Defines native music clip.</summary>
    [Serializable]
    public sealed class NativeMusicClip
    {
        /// <summary>Stores key.</summary>
        public string key = string.Empty;
        /// <summary>Stores resource.</summary>
        public string resource = string.Empty;
        /// <summary>Stores loop start sample.</summary>
        public int loopStartSample;
        /// <summary>Stores frames.</summary>
        public int frames;
        /// <summary>Stores sample rate.</summary>
        public int sampleRate;
        /// <summary>Stores native order.</summary>
        public int nativeOrder;
        /// <summary>Stores position scale.</summary>
        public int positionScale = 4;
        /// <summary>Stores row time map.</summary>
        public NativeMusicRow[]? rowTimeMap;
        /// <summary>Reconstruct the native BASS position, including quarter rows, from the decoded PCM row boundaries.</summary>
        public int PositionAtSample(int sample)
        {
            if (rowTimeMap == null || rowTimeMap.Length == 0)
            {
                return nativeOrder;
            }

            if (sample >= frames && frames > loopStartSample)
            {
                sample = loopStartSample + (sample - frames) % (frames - loopStartSample);
            }

            sample = Math.Max(0, sample);
            int lo = 0, hi = rowTimeMap.Length - 1;
            while (lo < hi)
            {
                var mid = lo + (hi - lo + 1) / 2;
                if (rowTimeMap[mid].sample <= sample)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            var current = rowTimeMap[lo];
            var end = lo + 1 < rowTimeMap.Length ? rowTimeMap[lo + 1].sample : frames;
            var scale = Math.Max(1, positionScale);
            var fraction = end > current.sample ? (int)Math.Min(scale - 1, Math.Max(0L, (long)(sample - current.sample) * scale / (end - current.sample))) : 0;
            return (current.order & 65535) | ((current.row + fraction) << 16);
        }
    }
}
