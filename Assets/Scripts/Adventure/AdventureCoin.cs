#nullable enable

using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure coin.</summary>
    public sealed class AdventureCoin
    {
        /// <summary>Gets or sets id.</summary>
        public int Id { get; internal set; }
        /// <summary>Gets or sets kind.</summary>
        public AdventureCoinKind Kind { get; internal set; }
        /// <summary>Gets or sets gift plant.</summary>
        public PlantKind? GiftPlant { get; internal set; }

        /// <summary>Gets or sets value units.</summary>
        public int ValueUnits
        {
            get
            {
                return Kind == AdventureCoinKind.Silver ? 1 : Kind == AdventureCoinKind.Gold ? 5 : Kind == AdventureCoinKind.Diamond ? 100 : 0;
            }
        }

        /// <summary>Gets or sets x.</summary>
        public float X { get; internal set; }
        /// <summary>Gets or sets row.</summary>
        public float Row { get; internal set; }
        /// <summary>Gets or sets age.</summary>
        public float Age { get; internal set; }
        /// <summary>Gets or sets is being collected.</summary>
        public bool IsBeingCollected { get; internal set; }
        /// <summary>Gets or sets persistence pending.</summary>
        public bool PersistencePending { get; internal set; }
        /// <summary>Gets or sets collector plant id.</summary>
        public int CollectorPlantId { get; internal set; }

        /// <summary>Gets the pickup's current logical pixel position on the original 800 by 600 board.</summary>
        public float PresentationX { get; internal set; }
        /// <summary>Gets the pickup's current logical vertical pixel position.</summary>
        public float PresentationY { get; internal set; }
        /// <summary>Gets the horizontal position before the latest native tick.</summary>
        public float PresentationPreviousX { get; internal set; }
        /// <summary>Gets the vertical position before the latest native tick.</summary>
        public float PresentationPreviousY { get; internal set; }
        /// <summary>Gets the native pickup scale during collection.</summary>
        public float PresentationScale { get; internal set; } = 1f;
        /// <summary>Gets the native pickup opacity during collection.</summary>
        public float PresentationAlpha { get; internal set; } = 1f;

        /// <summary>Stores the original drop's horizontal velocity in pixels per native tick.</summary>
        internal float FallVelocityX;
        /// <summary>Stores the original drop's vertical velocity in pixels per native tick.</summary>
        internal float FallVelocityY;
        /// <summary>Stores the original drop's clamped landing height in board pixels.</summary>
        internal float GroundY;
        /// <summary>Gets whether this currency has reached its original landing height.</summary>
        public bool HitGround { get; internal set; }
        /// <summary>Gets the original silver or gold animation's randomized playback multiplier.</summary>
        public float AnimationRate { get; internal set; } = 1f;
        /// <summary>Gets the original silver or gold animation's randomized starting time in seconds.</summary>
        public float AnimationOffset { get; internal set; }

        /// <summary>Advances one original PC gravity tick, stopping exactly at the landing height.</summary>
        internal void TickFall()
        {
            PresentationPreviousX = PresentationX;
            PresentationPreviousY = PresentationY;
            if (HitGround || PersistencePending || ValueUnits == 0)
            {
                return;
            }

            // PC Coin::UpdateFall 43C1F0 integrates position before adding 0.15 pixels of gravity.
            if (PresentationY + FallVelocityY < GroundY)
            {
                PresentationX += FallVelocityX;
                PresentationY += FallVelocityY;
                FallVelocityY += .15f;
            }
            else
            {
                PresentationY = GroundY;
                PresentationX = (int)(PresentationX + (PresentationX < 0f ? -.5f : .5f));
                HitGround = true;
            }
        }

        /// <summary>Advances one original PC collection tick towards the money counter.</summary>
        /// <returns>Whether the pre-move distance is below the native twelve-pixel scoring threshold.</returns>
        internal bool TickCollection()
        {
            // PC Coin::UpdateCollected (0x43C570): money targets (39, 558), not the sun counter.
            PresentationPreviousX = PresentationX;
            PresentationPreviousY = PresentationY;
            var dx = System.Math.Abs(PresentationX - 39f);
            var dy = System.Math.Abs(PresentationY - 558f);
            PresentationX = PresentationX > 39f ? (float)(PresentationX - (double)dx / 21.0)
                : PresentationX < 39f ? (float)(PresentationX + (double)dx / 21.0) : PresentationX;
            PresentationY = PresentationY > 558f ? (float)(PresentationY - (double)dy / 21.0)
                : PresentationY < 558f ? (float)(PresentationY + (double)dy / 21.0) : PresentationY;
            var distance = (float)System.Math.Sqrt((float)((double)dx * dx + (double)dy * dy));
            PresentationScale = System.Math.Max(.5f, System.Math.Min(1f, distance * .05f));
            PresentationAlpha = System.Math.Max(.35f, System.Math.Min(1f, distance * .035f));
            return distance < 12f;
        }

        /// <summary>Stores collection remaining.</summary>
        internal float _collectionRemaining;
    }
}
