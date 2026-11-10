using System;

namespace PvZReplica.Audio
{
    public enum MusicCue { None, Title, ChooseSeeds, Day, Night, Pool, Fog, Roof, Boss, Minigame, Puzzle, Conveyor, ZenGarden }
    [Serializable] public struct NativeMusicRow
    {
        public int sample;
        public int order;
        public int row;
    }
    [Serializable] public sealed class NativeMusicClip
    {
        public string key;
        public string resource;
        public int loopStartSample;
        public int frames;
        public int sampleRate;
        public int nativeOrder;
        public int positionScale = 4;
        public NativeMusicRow[] rowTimeMap;

        /// <summary>Reconstruct the native BASS position, including quarter rows, from the decoded PCM row boundaries.</summary>
        public int PositionAtSample(int sample)
        {
            if (rowTimeMap == null || rowTimeMap.Length == 0) return nativeOrder;
            if (sample >= frames && frames > loopStartSample)
                sample = loopStartSample + (sample - frames) % (frames - loopStartSample);
            sample = Math.Max(0, sample);
            int lo = 0, hi = rowTimeMap.Length - 1;
            while (lo < hi)
            {
                int mid = lo + (hi - lo + 1) / 2;
                if (rowTimeMap[mid].sample <= sample) lo = mid; else hi = mid - 1;
            }
            NativeMusicRow current = rowTimeMap[lo];
            int end = lo + 1 < rowTimeMap.Length ? rowTimeMap[lo + 1].sample : frames;
            int scale = Math.Max(1, positionScale);
            int fraction = end > current.sample ?
                (int)Math.Min(scale - 1, Math.Max(0L, (long)(sample - current.sample) * scale / (end - current.sample))) : 0;
            return (current.order & 65535) | ((current.row + fraction) << 16);
        }
    }
    [Serializable] public sealed class NativeMusicManifest { public NativeMusicClip[] clips; }
    public enum NativeBurstState { Off = 0, Starting = 1, On = 2, Finishing = 3 }
    public enum NativeDrumsState { Off = 0, QueuedOn = 1, On = 2, QueuedOff = 3, Fading = 4 }

    /// <summary>
    /// Direct state machine at local EXE 0x469530. It runs once per 100 Hz update against the actual tracker position.
    /// Drum gating is quantized by the native position comparison; it has no guessed beat duration.
    /// </summary>
    public sealed class OriginalMusicBurst
    {
        public NativeBurstState State { get; private set; }
        public NativeDrumsState DrumsState { get; private set; }
        public int BurstCounter { get; private set; }
        public int DrumsCounter { get; private set; }
        public int RecordedPosition { get; private set; }
        public int BurstOverride { get; private set; } = -1;
        public float Drums { get; private set; }
        public float HiHats { get; private set; }
        public float NightBase { get; private set; } = 1f;
        public int RequestedNightOrder { get; private set; } = -1;

        public void Reset()
        {
            State = NativeBurstState.Off; DrumsState = NativeDrumsState.Off;
            BurstCounter = DrumsCounter = RecordedPosition = 0; BurstOverride = -1;
            Drums = HiHats = 0f; NightBase = 1f; RequestedNightOrder = -1;
        }
        // Local 0x469510 changes only an idle burst; it does not latch the force-on override.
        public void ForceBurst()
        { if (State == NativeBurstState.Off) { State = NativeBurstState.Starting; BurstCounter = 400; } }
        public void SetBurstOverride(int value)
        {
            if (value != -1 && value != 1 && value != 2) throw new ArgumentOutOfRangeException(nameof(value));
            BurstOverride = value;
        }
        public static int QuantizedPosition(int position, bool night)
        {
            int row = (int)((uint)position >> 16);
            return night ? (position & 65535) + (row > 252 ? 1 : 0) : row / 128;
        }
        private static float Clamp(float value) { return Math.Max(0f, Math.Min(1f, value)); }

        public void Tick(int zombies, bool night, int position)
        {
            if (BurstCounter > 0) BurstCounter--;
            if (DrumsCounter > 0) DrumsCounter--;
            Drums = HiHats = 0f; NightBase = 1f; RequestedNightOrder = -1;
            switch (State)
            {
                case NativeBurstState.Off:
                    if (zombies >= 10 || BurstOverride == 1)
                    { State = NativeBurstState.Starting; BurstCounter = 400; }
                    break;
                case NativeBurstState.Starting:
                    if (!night)
                    {
                        HiHats = Clamp(1f - BurstCounter / 400f);
                        if (BurstCounter == 100)
                        { DrumsState = NativeDrumsState.QueuedOn; RecordedPosition = position; }
                    }
                    else if (DrumsState == NativeDrumsState.Off)
                    {
                        DrumsState = NativeDrumsState.QueuedOn; RecordedPosition = position; BurstCounter = 400;
                        break;
                    }
                    else if (DrumsState == NativeDrumsState.QueuedOn)
                    { BurstCounter = 400; break; }
                    else NightBase = Clamp(BurstCounter / 400f);
                    if (BurstCounter == 0) { State = NativeBurstState.On; BurstCounter = 800; }
                    break;
                case NativeBurstState.On:
                    HiHats = 1f;
                    if (night) NightBase = 0f;
                    if (BurstCounter == 0 && (zombies < 4 && BurstOverride == -1 || BurstOverride == 2))
                    {
                        State = NativeBurstState.Finishing;
                        if (night)
                        { BurstCounter = 1100; DrumsState = NativeDrumsState.Fading; DrumsCounter = 800; }
                        else
                        { BurstCounter = 800; DrumsState = NativeDrumsState.QueuedOff; RecordedPosition = position; }
                    }
                    break;
                case NativeBurstState.Finishing:
                    if (night) NightBase = Clamp(1f - BurstCounter / 400f);
                    else HiHats = Clamp(BurstCounter / 800f);
                    if (BurstCounter == 0 && DrumsState == NativeDrumsState.Off) State = NativeBurstState.Off;
                    break;
            }
            int boundary = QuantizedPosition(position, night);
            int recorded = QuantizedPosition(RecordedPosition, night);
            switch (DrumsState)
            {
                case NativeDrumsState.QueuedOn:
                    if (boundary != recorded)
                    {
                        Drums = 1f; DrumsState = NativeDrumsState.On;
                        if (night) RequestedNightOrder = 76 + (boundary % 2 != 0 ? 1 : 0);
                    }
                    break;
                case NativeDrumsState.On: Drums = 1f; break;
                case NativeDrumsState.QueuedOff:
                    Drums = 1f;
                    if (boundary != recorded && !night)
                    { DrumsState = NativeDrumsState.Fading; DrumsCounter = 50; }
                    break;
                case NativeDrumsState.Fading:
                    Drums = Clamp(DrumsCounter / (night ? 800f : 50f));
                    if (DrumsCounter == 0) DrumsState = NativeDrumsState.Off;
                    break;
            }
        }
    }
}
