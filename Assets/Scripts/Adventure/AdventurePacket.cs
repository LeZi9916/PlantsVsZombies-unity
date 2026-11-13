#nullable enable

using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure packet.</summary>
    public sealed class AdventurePacket
    {
        /// <summary>Gets whether this packet was released as a usable seed coin from a vase.</summary>
        public bool IsVaseDrop => FromVaseRow.HasValue && FromVaseColumn.HasValue;
        /// <summary>Gets the original usable seed coin's horizontal position in board pixels.</summary>
        public float PresentationX { get; internal set; }
        /// <summary>Gets the original usable seed coin's vertical position in board pixels.</summary>
        public float PresentationY { get; internal set; }
        /// <summary>Gets the horizontal position before the latest native update.</summary>
        public float PresentationPreviousX { get; internal set; }
        /// <summary>Gets the vertical position before the latest native update.</summary>
        public float PresentationPreviousY { get; internal set; }
        /// <summary>Stores horizontal velocity in pixels per original ten-millisecond update.</summary>
        internal float FallVelocityX;
        /// <summary>Stores vertical velocity in pixels per original ten-millisecond update.</summary>
        internal float FallVelocityY;
        /// <summary>Stores the original integer landing height in board pixels.</summary>
        internal int GroundY;
        /// <summary>Gets whether the usable seed has reached the ground.</summary>
        public bool HitGround { get; internal set; }
        /// <summary>Gets the native disappearance counter, incremented on the ground and while held.</summary>
        public int GroundTicks { get; internal set; }
        /// <summary>Gets the remaining native fade updates, or zero before expiration.</summary>
        public int FadeTicks { get; internal set; }
        /// <summary>Gets whether the cursor is currently carrying this usable seed.</summary>
        public bool IsHeld { get; internal set; }
        /// <summary>Counts cancellations so only the first release grants the original three-second grace.</summary>
        public int TimesDropped { get; internal set; }
        /// <summary>Gets the original coin fade opacity before the small seed's grayscale color override.</summary>
        public float PresentationAlpha => FadeTicks == 0 ? 1f : FadeTicks / 15f;
        /// <summary>Gets the original held or expiring small seed color multiplier.</summary>
        public int PresentationGrayness => IsHeld ? 128 : GroundTicks > 1200 && GroundTicks % 60 < 30 ? 192 : 255;

        /// <summary>Initializes the native FromPlant usable seed coin at its vase's original pixel origin.</summary>
        /// <param name="random">The board's original random stream, consumed in native velocity and ground order.</param>
        /// <exception cref="InvalidOperationException">The packet has no originating vase cell.</exception>
        internal void InitializeVaseDrop(Random random)
        {
            PresentationX = 60f + FromVaseColumn!.Value * 80f;
            PresentationY = 80f + FromVaseRow!.Value * 100f;
            PresentationPreviousX = PresentationX;
            PresentationPreviousY = PresentationY;
            FallVelocityY = -1.7f - AdventureSpawnRandom.NextFloat(random, 1.7f);
            FallVelocityX = AdventureSpawnRandom.NextFloat(random, .8f) - .4f;
            GroundY = (int)(PresentationY + 15f + random.Next(20));
        }

        /// <summary>Advances native falling, held counting, and expiration without aging airborne seeds.</summary>
        /// <param name="random">The board's original random stream for a possible horizontal edge bounce.</param>
        /// <returns>Whether the original fifteen-update fade has completed and the packet must be removed.</returns>
        internal bool TickVaseDrop(Random random)
        {
            PresentationPreviousX = PresentationX;
            PresentationPreviousY = PresentationY;
            if (FadeTicks > 0)
            {
                FadeTicks--;
                return FadeTicks == 0;
            }

            if (IsHeld)
            {
                GroundTicks++;
                return false;
            }

            // PC 43C23B compares the extended sum before storing the new single-precision position.
            var nextY = (double)PresentationY + FallVelocityY;
            if (nextY < GroundY)
            {
                PresentationY = (float)nextY;
                FallVelocityY += .09f;
                PresentationX += FallVelocityX;
                if (PresentationX > 750f)
                {
                    PresentationX = 750f;
                    FallVelocityX = -.4f - AdventureSpawnRandom.NextFloat(random, .4f);
                }
                else if (PresentationX < 0f)
                {
                    PresentationX = 0f;
                    FallVelocityX = .4f + AdventureSpawnRandom.NextFloat(random, .4f);
                }
            }
            else
            {
                PresentationY = GroundY;
                PresentationX = (int)((double)PresentationX + (PresentationX > 0f ? .5 : -.5));
                HitGround = true;
                GroundTicks++;
                if (GroundTicks >= 1500)
                {
                    FadeTicks = 15;
                }
            }

            return false;
        }

        /// <summary>Collects the usable seed without moving its original board representation.</summary>
        internal void CollectVaseDrop()
        {
            IsHeld = true;
            GroundY = (int)PresentationY;
            FadeTicks = 0;
        }

        /// <summary>Returns the usable seed to its original position with only the native first-cancel grace.</summary>
        internal void DropVaseDrop()
        {
            IsHeld = false;
            if (TimesDropped == 0)
            {
                GroundTicks = Math.Min(GroundTicks, 1200);
            }

            TimesDropped++;
        }

        /// <summary>Stores conveyor initialized.</summary>
        internal bool _conveyorInitialized;
        /// <summary>Gets or sets conveyor offset x.</summary>
        public int ConveyorOffsetX { get; internal set; }
        /// <summary>Gets or sets id.</summary>
        public int Id { get; internal set; }
        /// <summary>Gets or sets kind.</summary>
        public PlantKind Kind { get; internal set; }
        /// <summary>Gets or sets age.</summary>
        public float Age { get; internal set; }
        /// <summary>Gets or sets from vase column.</summary>
        public int? FromVaseColumn { get; internal set; }
        /// <summary>Gets or sets from vase row.</summary>
        public int? FromVaseRow { get; internal set; }
    }
}
