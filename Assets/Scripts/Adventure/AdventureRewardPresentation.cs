#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure reward presentation.</summary>
    public sealed class AdventureRewardPresentation
    {
        /// <summary>Gets or sets native type.</summary>
        public int NativeType { get; private set; }
        /// <summary>Gets or sets stage.</summary>
        public AdventureAwardStage Stage { get; private set; }
        /// <summary>Gets or sets x.</summary>
        public float X { get; private set; }
        /// <summary>Gets or sets y.</summary>
        public float Y { get; private set; }
        /// <summary>Gets or sets previous x.</summary>
        public float PreviousX { get; private set; }
        /// <summary>Gets or sets previous y.</summary>
        public float PreviousY { get; private set; }
        /// <summary>Gets or sets scale.</summary>
        public float Scale { get; private set; } = 1f;
        /// <summary>Gets or sets previous scale.</summary>
        public float PreviousScale { get; private set; } = 1f;
        /// <summary>Gets or sets width.</summary>
        public int Width { get; private set; }
        /// <summary>Gets or sets height.</summary>
        public int Height { get; private set; }
        /// <summary>Gets or sets ground y.</summary>
        public int GroundY { get; private set; }
        /// <summary>Gets or sets age ticks.</summary>
        public int AgeTicks { get; private set; }
        /// <summary>Gets or sets ground ticks.</summary>
        public int GroundTicks { get; private set; }
        /// <summary>Gets or sets collection ticks.</summary>
        public int CollectionTicks { get; private set; }
        /// <summary>Gets or sets introduction ticks.</summary>
        public int IntroductionTicks { get; private set; }

        /// <summary>Gets or sets fade ticks.</summary>
        public int FadeTicks
        {
            get
            {
                return NativeType == 15 ? 500 : 600;
            }
        }

        /// <summary>Gets or sets fade alpha.</summary>
        public float FadeAlpha
        {
            get
            {
                return Stage == AdventureAwardStage.Collecting ? Clamp((CollectionTicks - (FadeTicks - 200)) / 200f) : 0f;
            }
        }

        /// <summary>Gets or sets introduction alpha.</summary>
        public float IntroductionAlpha
        {
            get
            {
                return Stage == AdventureAwardStage.Introduction ? Clamp(1f - IntroductionTicks / 60f) : 0f;
            }
        }

        /// <summary>Gets or sets on ground.</summary>
        public bool OnGround
        {
            get
            {
                return Stage == AdventureAwardStage.Waiting;
            }
        }

        /// <summary>Gets or sets collection start x.</summary>
        public float CollectionStartX { get; private set; }
        /// <summary>Gets or sets collection start y.</summary>
        public float CollectionStartY { get; private set; }

        /// <summary>Stores velocity x,  velocity y.</summary>
        private float _velocityX, _velocityY;
        /// <summary>Stores remainder.</summary>
        private double _remainder;
        /// <summary>Initializes adventure reward presentation.</summary>
        /// <param name = "nativeType">The native type.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "horizontalDraw">The horizontal draw.</param>
        /// <param name = "verticalDraw">The vertical draw.</param>
        /// <param name = "groundDraw">The ground draw.</param>
        /// <param name = "boss">The boss.</param>
        public AdventureRewardPresentation(int nativeType, float x, float y, int width, int height, float horizontalDraw, float verticalDraw, int groundDraw, bool boss = false)
        {
            NativeType = nativeType;
            Width = width;
            Height = height;
            Stage = AdventureAwardStage.Falling;
            X = x;
            Y = y;
            if (boss)
            {
                X = 750f;
                Y = nativeType == 21 ? 130f : 245f;
                _velocityX = -3f;
                _velocityY = -5f;
                GroundY = (int)(Y + 40f);
            }
            else
            {
                _velocityX = horizontalDraw - .5f;
                _velocityY = -3f - verticalDraw * 2f;
                GroundY = Math.Max(80, Math.Min(521, (int)(y + 45f + groundDraw)));
                if (nativeType == 21 || nativeType == 22)
                {
                    Y -= 100f;
                    GroundY = Math.Min(400, (int)(Y + 45f));
                }

                if (nativeType == 7 || nativeType >= 8 && nativeType <= 15 || nativeType == 16)
                {
                    GroundY -= 30;
                }
            }

            PreviousX = X;
            PreviousY = Y;
        }

        /// <summary>Performs native type for.</summary>
        /// <param name = "level">The level.</param>
        /// <returns>The resulting native type for.</returns>
        public static int NativeTypeFor(AdventureLevelSpec level)
        {
            // PC Zombie::TrySpawnLevelAward 0x54BBC0 (note rewards precede replay money).
            if (level.Number % 10 == 9)
            {
                return 15;
            }

            if (level.Number == 50)
            {
                return level.IsReplay ? 18 : 21;
            }

            if (level.IsReplay)
            {
                return 18;
            }

            switch (level.RewardKind)
            {
                case AdventureRewardKind.Shovel:
                    return 9;
                case AdventureRewardKind.Almanac:
                    return 10;
                case AdventureRewardKind.CarKeys:
                    return 11;
                case AdventureRewardKind.Taco:
                    return 14;
                case AdventureRewardKind.WateringCan:
                    return 13;
                default:
                    return 7;
            }
        }

        /// <summary>Performs contains.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <returns>The resulting contains.</returns>
        public bool Contains(float x, float y)
        {
            if (Stage != AdventureAwardStage.Falling && Stage != AdventureAwardStage.Waiting)
            {
                return false;
            }

            return x >= X && x < X + Width && y >= Y && y < Y + Height;
        }

        /// <summary>Performs begin collection.</summary>
        /// <returns>The resulting begin collection.</returns>
        public bool BeginCollection()
        {
            if (Stage != AdventureAwardStage.Falling && Stage != AdventureAwardStage.Waiting)
            {
                return false;
            }

            CollectionStartX = X;
            CollectionStartY = Y;
            CollectionTicks = 0;
            Stage = AdventureAwardStage.Collecting;
            return true;
        }

        /// <summary>Performs update.</summary>
        /// <param name = "dt">The dt.</param>
        /// <param name = "nativeTick">The native tick.</param>
        public void Update(float dt, Action? nativeTick = null)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                return;
            }

            _remainder += dt;
            while (_remainder + .000000001 >= (double).01f)
            {
                _remainder -= (double).01f;
                if (Stage != AdventureAwardStage.Introduction && nativeTick != null)
                {
                    nativeTick();
                }

                Tick();
            }
        }

        /// <summary>Gets or sets render fraction.</summary>
        public float RenderFraction
        {
            get
            {
                return Clamp((float)(_remainder / (double).01f));
            }
        }

        /// <summary>Performs tick.</summary>
        private void Tick()
        {
            PreviousX = X;
            PreviousY = Y;
            PreviousScale = Scale;
            AgeTicks++;
            if (Stage == AdventureAwardStage.Introduction)
            {
                IntroductionTicks++;
                return;
            }

            if (Stage == AdventureAwardStage.Collecting)
            {
                CollectionTicks++;
                float t = Clamp(CollectionTicks / 350f), ease = 2f * t - t * t;
                X = CollectionStartX + (400f - Width / 2 - CollectionStartX) * ease;
                Y = CollectionStartY + (340f - CollectionStartY) * ease;
                var s = Smooth(Smooth(Clamp(CollectionTicks / 400f)));
                Scale = 1.01f + .99f * s;
                if (CollectionTicks >= FadeTicks)
                {
                    Stage = AdventureAwardStage.Introduction;
                }

                return;
            }

            if (Stage == AdventureAwardStage.Waiting)
            {
                GroundTicks++;
                return;
            }

            var nextY = Y + _velocityY;
            if (nextY < GroundY)
            {
                Y = nextY;
                _velocityY += .15f;
                X += _velocityX;
                if (X > 800 - Width && NativeType != 21)
                {
                    X = 800 - Width;
                }
                else if (X < 0)
                {
                    X = 0;
                }
            }
            else
            {
                Y = GroundY;
                X = (float)Math.Round(X, MidpointRounding.AwayFromZero);
                Stage = AdventureAwardStage.Waiting;
                GroundTicks = 0;
            }
        }

        /// <summary>Performs clamp.</summary>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting clamp.</returns>
        private static float Clamp(float value)
        {
            return Math.Max(0, Math.Min(1, value));
        }

        /// <summary>Performs smooth.</summary>
        /// <param name = "t">The t.</param>
        /// <returns>The resulting smooth.</returns>
        private static float Smooth(float t)
        {
            return 3f * t * t - 2f * t * t * t;
        }
    }
}
