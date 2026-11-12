#nullable enable
using System;

namespace PlantsVsZombies.Audio
{
    /// <summary>
    /// Direct state machine at local EXE 0x469530. It runs once per 100 Hz update against the actual tracker position.
    /// Drum gating is quantized by the native position comparison; it has no guessed beat duration.
    /// </summary>
    public sealed class OriginalMusicBurst
    {
        /// <summary>Gets or sets state.</summary>
        public NativeBurstState State { get; private set; }
        /// <summary>Gets or sets drums state.</summary>
        public NativeDrumsState DrumsState { get; private set; }
        /// <summary>Gets or sets burst counter.</summary>
        public int BurstCounter { get; private set; }
        /// <summary>Gets or sets drums counter.</summary>
        public int DrumsCounter { get; private set; }
        /// <summary>Gets or sets recorded position.</summary>
        public int RecordedPosition { get; private set; }
        /// <summary>Gets or sets burst override.</summary>
        public int BurstOverride { get; private set; } = -1;
        /// <summary>Gets or sets drums.</summary>
        public float Drums { get; private set; }
        /// <summary>Gets or sets hi hats.</summary>
        public float HiHats { get; private set; }
        /// <summary>Gets or sets night base.</summary>
        public float NightBase { get; private set; } = 1f;
        /// <summary>Gets or sets requested night order.</summary>
        public int RequestedNightOrder { get; private set; } = -1;

        /// <summary>Performs reset.</summary>
        public void Reset()
        {
            State = NativeBurstState.Off;
            DrumsState = NativeDrumsState.Off;
            BurstCounter = DrumsCounter = RecordedPosition = 0;
            BurstOverride = -1;
            Drums = HiHats = 0f;
            NightBase = 1f;
            RequestedNightOrder = -1;
        }

        // Local 0x469510 changes only an idle burst; it does not latch the force-on override.
        /// <summary>Performs force burst.</summary>
        public void ForceBurst()
        {
            if (State == NativeBurstState.Off)
            {
                State = NativeBurstState.Starting;
                BurstCounter = 400;
            }
        }

        /// <summary>Performs set burst override.</summary>
        /// <param name = "value">The value.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public void SetBurstOverride(int value)
        {
            if (value != -1 && value != 1 && value != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            BurstOverride = value;
        }

        /// <summary>Performs quantized position.</summary>
        /// <param name = "position">The position.</param>
        /// <param name = "night">The night.</param>
        /// <returns>The resulting quantized position.</returns>
        public static int QuantizedPosition(int position, bool night)
        {
            var row = (int)((uint)position >> 16);
            return night ? (position & 65535) + (row > 252 ? 1 : 0) : row / 128;
        }

        /// <summary>Performs clamp.</summary>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting clamp.</returns>
        private static float Clamp(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        /// <summary>Performs tick.</summary>
        /// <param name = "zombies">The zombies.</param>
        /// <param name = "night">The night.</param>
        /// <param name = "position">The position.</param>
        public void Tick(int zombies, bool night, int position)
        {
            if (BurstCounter > 0)
            {
                BurstCounter--;
            }

            if (DrumsCounter > 0)
            {
                DrumsCounter--;
            }

            Drums = HiHats = 0f;
            NightBase = 1f;
            RequestedNightOrder = -1;
            switch (State)
            {
                case NativeBurstState.Off:
                    if (zombies >= 10 || BurstOverride == 1)
                    {
                        State = NativeBurstState.Starting;
                        BurstCounter = 400;
                    }

                    break;
                case NativeBurstState.Starting:
                    if (!night)
                    {
                        HiHats = Clamp(1f - BurstCounter / 400f);
                        if (BurstCounter == 100)
                        {
                            DrumsState = NativeDrumsState.QueuedOn;
                            RecordedPosition = position;
                        }
                    }
                    else if (DrumsState == NativeDrumsState.Off)
                    {
                        DrumsState = NativeDrumsState.QueuedOn;
                        RecordedPosition = position;
                        BurstCounter = 400;
                        break;
                    }
                    else if (DrumsState == NativeDrumsState.QueuedOn)
                    {
                        BurstCounter = 400;
                        break;
                    }
                    else
                    {
                        NightBase = Clamp(BurstCounter / 400f);
                    }

                    if (BurstCounter == 0)
                    {
                        State = NativeBurstState.On;
                        BurstCounter = 800;
                    }

                    break;
                case NativeBurstState.On:
                    HiHats = 1f;
                    if (night)
                    {
                        NightBase = 0f;
                    }

                    if (BurstCounter == 0 && (zombies < 4 && BurstOverride == -1 || BurstOverride == 2))
                    {
                        State = NativeBurstState.Finishing;
                        if (night)
                        {
                            BurstCounter = 1100;
                            DrumsState = NativeDrumsState.Fading;
                            DrumsCounter = 800;
                        }
                        else
                        {
                            BurstCounter = 800;
                            DrumsState = NativeDrumsState.QueuedOff;
                            RecordedPosition = position;
                        }
                    }

                    break;
                case NativeBurstState.Finishing:
                    if (night)
                    {
                        NightBase = Clamp(1f - BurstCounter / 400f);
                    }
                    else
                    {
                        HiHats = Clamp(BurstCounter / 800f);
                    }

                    if (BurstCounter == 0 && DrumsState == NativeDrumsState.Off)
                    {
                        State = NativeBurstState.Off;
                    }

                    break;
            }

            var boundary = QuantizedPosition(position, night);
            var recorded = QuantizedPosition(RecordedPosition, night);
            switch (DrumsState)
            {
                case NativeDrumsState.QueuedOn:
                    if (boundary != recorded)
                    {
                        Drums = 1f;
                        DrumsState = NativeDrumsState.On;
                        if (night)
                        {
                            RequestedNightOrder = 76 + (boundary % 2 != 0 ? 1 : 0);
                        }
                    }

                    break;
                case NativeDrumsState.On:
                    Drums = 1f;
                    break;
                case NativeDrumsState.QueuedOff:
                    Drums = 1f;
                    if (boundary != recorded && !night)
                    {
                        DrumsState = NativeDrumsState.Fading;
                        DrumsCounter = 50;
                    }

                    break;
                case NativeDrumsState.Fading:
                    Drums = Clamp(DrumsCounter / (night ? 800f : 50f));
                    if (DrumsCounter == 0)
                    {
                        DrumsState = NativeDrumsState.Off;
                    }

                    break;
            }
        }
    }
}
