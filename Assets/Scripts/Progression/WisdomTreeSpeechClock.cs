#nullable enable
using System;

namespace PlantsVsZombies.Progression
{
    /// <summary>Runs the original Tree of Wisdom speech timing and message selection.</summary>
    public sealed class WisdomTreeSpeechClock
    {
        /// <summary>Identifies the tree's timed speech states.</summary>
        private enum State
        {
            /// <summary>Waits for the next idle message.</summary>
            Waiting,
            /// <summary>Waits for the growth animation to finish.</summary>
            Growing,
            /// <summary>Displays an idle message.</summary>
            Babbling,
            /// <summary>Displays a newly earned tip.</summary>
            Wisdom
        }
        /// <summary>Supplies a random integer below the requested exclusive bound.</summary>
        private readonly Func<int, int> _randomBelow;
        /// <summary>Stores the current native speech state.</summary>
        private State _state;
        /// <summary>Counts remaining native hundredth-second updates.</summary>
        private int _remaining;
        /// <summary>Gets the currently visible original string index, or -1.</summary>
        public int MessageIndex { get; private set; } = -1;

        /// <summary>Creates the initial waiting period using the native inclusive range.</summary>
        /// <param name="randomBelow">Returns an integer from zero up to the exclusive bound.</param>
        /// <exception cref="ArgumentNullException">The random source is null.</exception>
        public WisdomTreeSpeechClock(Func<int, int> randomBelow)
        {
            _randomBelow = randomBelow ?? throw new ArgumentNullException(nameof(randomBelow));
            _remaining = 700 + _randomBelow(801);
        }

        /// <summary>Starts the native post-growth pause before the tree gives wisdom.</summary>
        public void StartGrowth()
        {
            _state = State.Growing;
            _remaining = 120;
            MessageIndex = -1;
        }

        /// <summary>Advances one unpaused original update.</summary>
        /// <param name="height">The current original tree height.</param>
        public void Tick(int height)
        {
            if (--_remaining > 0)
            {
                return;
            }

            if (_state == State.Growing)
            {
                _state = State.Wisdom;
                _remaining = 1000;
                MessageIndex = height == 100 ? 800 : height == 500 ? 900 : height == 1000 ? 1000 : height > 1000 ? 1100 : Math.Max(1, Math.Min(49, height - 1));
            }
            else if (_state == State.Waiting)
            {
                _state = State.Babbling;
                _remaining = 400;
                var selection = _randomBelow(3);
                MessageIndex = height <= 1 ? 600 : selection == 0 && height >= 5 ? 500 : selection == 1 ? 101 + _randomBelow(10) : (height < 12 ? 201 : height < 50 ? 301 : 401) + _randomBelow(4);
            }
            else if (MessageIndex == 500)
            {
                if (height >= 100 && _randomBelow(47) == 0)
                {
                    MessageIndex = 800;
                }
                else if (height >= 500 && _randomBelow(47) == 0)
                {
                    MessageIndex = 900;
                }
                else if (height >= 1000 && _randomBelow(47) == 0)
                {
                    MessageIndex = 1000;
                }
                else
                {
                    MessageIndex = 2 + _randomBelow(Math.Max(3, Math.Min(49, height)) - 1);
                }

                _remaining = 600;
            }
            else
            {
                _state = State.Waiting;
                _remaining = 700 + _randomBelow(301);
                MessageIndex = -1;
            }
        }
    }
}
