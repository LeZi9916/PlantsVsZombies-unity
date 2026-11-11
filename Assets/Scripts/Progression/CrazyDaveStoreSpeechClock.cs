#nullable enable
using System;

namespace PlantsVsZombies.Progression
{
    /// <summary>Tracks the original store's 100 Hz speech and hover timing.</summary>
    public sealed class CrazyDaveStoreSpeechClock
    {
        /// <summary>Gets the number of ticks elapsed since Dave appeared.</summary>
        public int StoreTicks { get; private set; }

        /// <summary>Gets the remaining bubble ticks.</summary>
        public int BubbleTicks { get; private set; }

        /// <summary>Gets the remaining idle ticks before an ambient message.</summary>
        public int AmbientTicks { get; private set; } = 200;

        /// <summary>Gets the active message index, or -1 when the bubble is closed.</summary>
        public int MessageIndex { get; private set; } = -1;

        /// <summary>Gets the previous ambient message index, or -1 before the first one.</summary>
        public int PreviousAmbientIndex { get; private set; } = -1;

        /// <summary>Sets the message and bubble duration after an external store event.</summary>
        /// <param name="messageIndex">The original message index.</param>
        /// <param name="ticks">The bubble duration in original update ticks.</param>
        public void SetMessage(int messageIndex, int ticks)
        {
            MessageIndex = messageIndex;
            BubbleTicks = ticks;
        }

        /// <summary>Clears the current bubble when the store stops Dave talking.</summary>
        public void ClearMessage()
        {
            MessageIndex = -1;
            BubbleTicks = 0;
        }

        /// <summary>Advances one original store update and dispatches any speech changes.</summary>
        /// <param name="davePresent">Whether Dave has entered the store.</param>
        /// <param name="daveEntering">Whether Dave's entrance animation is still playing.</param>
        /// <param name="waitingForDialog">Whether a modal dialog pauses store updates.</param>
        /// <param name="hatchBusyAtStart">Whether a hatch animation was active at the start of this update.</param>
        /// <param name="hatchBusyAfterUpdate">Whether the hatch animation remains active for hover handling.</param>
        /// <param name="introPending">Whether an introductory message is waiting to start.</param>
        /// <param name="clickToContinue">Whether a dialogue requires a click to advance.</param>
        /// <param name="voicePlaying">Whether a short, long, or extra-long Dave voice is playing.</param>
        /// <param name="hoveredMessage">The hovered item's message index, or null when no item is hovered.</param>
        /// <param name="adventureCompleted">Whether the final ambient message is eligible.</param>
        /// <param name="randomBelow">Returns an integer from zero up to, but excluding, the supplied limit.</param>
        /// <param name="speak">Starts the supplied original message index.</param>
        /// <param name="stop">Stops the current speech and closes the bubble.</param>
        /// <exception cref="ArgumentNullException">A required callback is null.</exception>
        public void Advance(bool davePresent, bool daveEntering, bool waitingForDialog, bool hatchBusyAtStart, bool hatchBusyAfterUpdate, bool introPending, bool clickToContinue, bool voicePlaying, int? hoveredMessage, bool adventureCompleted, Func<int, int> randomBelow, Action<int> speak, Action stop)
        {
            if (randomBelow == null)
            {
                throw new ArgumentNullException(nameof(randomBelow));
            }

            if (speak == null)
            {
                throw new ArgumentNullException(nameof(speak));
            }

            if (stop == null)
            {
                throw new ArgumentNullException(nameof(stop));
            }

            if (waitingForDialog || !davePresent)
            {
                return;
            }

            StoreTicks++;
            if (!daveEntering && !hatchBusyAtStart && !introPending && !clickToContinue)
            {
                if (BubbleTicks > 0)
                {
                    BubbleTicks--;
                    if (BubbleTicks == 0)
                    {
                        if (voicePlaying)
                        {
                            BubbleTicks = 1;
                        }
                        else
                        {
                            ClearMessage();
                            stop();
                        }
                    }
                }
                else
                {
                    AmbientTicks--;
                    if (AmbientTicks <= 0)
                    {
                        var selected = SelectAmbient(adventureCompleted, randomBelow);
                        PreviousAmbientIndex = selected;
                        SetMessage(selected, 800);
                        speak(selected);
                        AmbientTicks = 500 + randomBelow(501);
                    }
                }
            }

            if (StoreTicks >= 120 && !clickToContinue && !hatchBusyAfterUpdate && hoveredMessage.HasValue)
            {
                var message = hoveredMessage.Value;
                if (MessageIndex != message)
                {
                    SetMessage(message, 100);
                    speak(message);
                }
                else
                {
                    BubbleTicks = 100;
                }
            }
        }

        /// <summary>Selects an ambient message with the original weights and repetition rule.</summary>
        /// <param name="adventureCompleted">Whether the last message may be selected.</param>
        /// <param name="randomBelow">Returns a random integer below its argument.</param>
        /// <returns>The selected original message index.</returns>
        /// <exception cref="InvalidOperationException">The random callback returns an out-of-range value.</exception>
        private int SelectAmbient(bool adventureCompleted, Func<int, int> randomBelow)
        {
            var total = 0;
            for (var index = 2015; index <= 2018; index++)
            {
                total += AmbientWeight(index, adventureCompleted);
            }

            var choice = randomBelow(total);
            for (var index = 2015; index <= 2018; index++)
            {
                var weight = AmbientWeight(index, adventureCompleted);
                if (choice < weight)
                {
                    return index;
                }

                choice -= weight;
            }

            throw new InvalidOperationException("The random value was outside the requested range.");
        }

        /// <summary>Gets an ambient message's original selection weight.</summary>
        /// <param name="messageIndex">The candidate message index.</param>
        /// <param name="adventureCompleted">Whether adventure has been completed.</param>
        /// <returns>The selection weight.</returns>
        private int AmbientWeight(int messageIndex, bool adventureCompleted)
        {
            if (messageIndex == PreviousAmbientIndex)
            {
                return 0;
            }

            return messageIndex == 2018 ? (adventureCompleted ? 20 : 0) : 100;
        }
    }
}
