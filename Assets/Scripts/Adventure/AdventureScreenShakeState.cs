#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC board widget's twelve-tick screen shake (Board::Update, 0x42134B).</summary>
    public sealed class AdventureScreenShakeState
    {
        /// <summary>Stores the horizontal amount whose sign can reverse on each native tick.</summary>
        private int _horizontal;
        /// <summary>Stores the vertical amount used by the native bounce curve.</summary>
        private int _vertical;

        /// <summary>Gets the number of native board updates remaining in this shake.</summary>
        public int TicksRemaining { get; private set; }
        /// <summary>Gets the board widget's current horizontal pixel displacement.</summary>
        public int OffsetX { get; private set; }
        /// <summary>Gets the board widget's current vertical pixel displacement.</summary>
        public int OffsetY { get; private set; }

        /// <summary>Restarts the original shake without moving the board until its next update.</summary>
        /// <param name="horizontal">The signed horizontal amount in original board pixels.</param>
        /// <param name="vertical">The signed vertical amount in original board pixels.</param>
        public void Start(int horizontal, int vertical)
        {
            TicksRemaining = 12;
            _horizontal = horizontal;
            _vertical = vertical;
        }

        /// <summary>Advances the PC countdown and its TodAnimateCurve(12, 0, counter, 0, amount, Bounce).</summary>
        /// <param name="reverseHorizontal">Whether the original Rand(2) draw was zero.</param>
        public void Tick(bool reverseHorizontal)
        {
            if (TicksRemaining <= 0)
            {
                return;
            }

            TicksRemaining--;
            if (TicksRemaining == 0)
            {
                OffsetX = 0;
                OffsetY = 0;
                return;
            }

            if (reverseHorizontal)
            {
                _horizontal = -_horizontal;
            }

            // Preserve the PC float stores between x87 curve stages and before integer rounding.
            var time = (float)((TicksRemaining - 12) / -12d);
            var centered = (float)(time * 2d - 1d);
            var bounce = (float)(1d - Math.Abs(centered));
            OffsetX = (int)Math.Round((float)(_horizontal * (double)bounce), MidpointRounding.AwayFromZero);
            OffsetY = (int)Math.Round((float)(_vertical * (double)bounce), MidpointRounding.AwayFromZero);
        }
    }
}
