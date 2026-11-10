using System;
using PvZReplica.Adventure;

namespace PvZReplica.Adventure
{
    // PC Coin::CoinInitialize 0x43B610, UpdateFall 0x43C1F0,
    // UpdateCollected 0x43C570; Board::FadeOutLevel 0x418140.
    public enum AdventureAwardStage { Falling, Waiting, Collecting, Introduction }
    public sealed class AdventureRewardPresentation
    {
        public int NativeType { get; private set; }
        public AdventureAwardStage Stage { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float PreviousX { get; private set; }
        public float PreviousY { get; private set; }
        public float Scale { get; private set; } = 1f;
        public float PreviousScale { get; private set; } = 1f;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int GroundY { get; private set; }
        public int AgeTicks { get; private set; }
        public int GroundTicks { get; private set; }
        public int CollectionTicks { get; private set; }
        public int IntroductionTicks { get; private set; }
        public int FadeTicks { get { return NativeType == 15 ? 500 : 600; } }
        public float FadeAlpha { get { return Stage == AdventureAwardStage.Collecting ? Clamp((CollectionTicks - (FadeTicks - 200)) / 200f) : 0f; } }
        public float IntroductionAlpha { get { return Stage == AdventureAwardStage.Introduction ? Clamp(1f - IntroductionTicks / 60f) : 0f; } }
        public bool OnGround { get { return Stage == AdventureAwardStage.Waiting; } }
        public float CollectionStartX { get; private set; }
        public float CollectionStartY { get; private set; }
        private float velocityX, velocityY;
        private double remainder;

        public AdventureRewardPresentation(int nativeType, float x, float y, int width, int height, float horizontalDraw, float verticalDraw, int groundDraw, bool boss = false)
        {
            NativeType = nativeType; Width = width; Height = height;
            Stage = AdventureAwardStage.Falling; X = x; Y = y;
            if (boss) { X = 750f; Y = nativeType == 21 ? 130f : 245f; velocityX = -3f; velocityY = -5f; GroundY = (int)(Y + 40f); }
            else
            {
                velocityX = horizontalDraw - .5f; velocityY = -3f - verticalDraw * 2f;
                GroundY = Math.Max(80, Math.Min(521, (int)(y + 45f + groundDraw)));
                if (nativeType == 21 || nativeType == 22) { Y -= 100f; GroundY = Math.Min(400, (int)(Y + 45f)); }
                if (nativeType == 7 || nativeType >= 8 && nativeType <= 15 || nativeType == 16) GroundY -= 30;
            }
            PreviousX = X; PreviousY = Y;
        }
        public static int NativeTypeFor(AdventureLevelSpec level)
        {
            // PC Zombie::TrySpawnLevelAward 0x54BBC0 (note rewards precede replay money).
            if (level.Number % 10 == 9) return 15;
            if (level.Number == 50) return level.IsReplay ? 18 : 21;
            if (level.IsReplay) return 18;
            switch (level.RewardKind)
            {
                case AdventureRewardKind.Shovel: return 9;
                case AdventureRewardKind.Almanac: return 10;
                case AdventureRewardKind.CarKeys: return 11;
                case AdventureRewardKind.Taco: return 14;
                case AdventureRewardKind.WateringCan: return 13;
                default: return 7;
            }
        }
        public bool Contains(float x, float y)
        {
            if (Stage != AdventureAwardStage.Falling && Stage != AdventureAwardStage.Waiting) return false;
            return x >= X && x < X + Width && y >= Y && y < Y + Height;
        }
        public bool BeginCollection()
        {
            if (Stage != AdventureAwardStage.Falling && Stage != AdventureAwardStage.Waiting) return false;
            CollectionStartX = X; CollectionStartY = Y; CollectionTicks = 0;
            Stage = AdventureAwardStage.Collecting; return true;
        }
        public void Update(float dt, Action nativeTick = null)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            remainder += dt;
            while (remainder + .000000001 >= (double).01f)
            {
                remainder -= (double).01f;
                if (Stage != AdventureAwardStage.Introduction && nativeTick != null) nativeTick();
                Tick();
            }
        }
        public float RenderFraction { get { return Clamp((float)(remainder / (double).01f)); } }
        private void Tick()
        {
            PreviousX = X; PreviousY = Y; PreviousScale = Scale; AgeTicks++;
            if (Stage == AdventureAwardStage.Introduction) { IntroductionTicks++; return; }
            if (Stage == AdventureAwardStage.Collecting)
            {
                CollectionTicks++;
                float t = Clamp(CollectionTicks / 350f), ease = 2f * t - t * t;
                X = CollectionStartX + (400f - Width / 2 - CollectionStartX) * ease;
                Y = CollectionStartY + (340f - CollectionStartY) * ease;
                float s = Smooth(Smooth(Clamp(CollectionTicks / 400f)));
                Scale = 1.01f + .99f * s;
                if (CollectionTicks >= FadeTicks) Stage = AdventureAwardStage.Introduction;
                return;
            }
            if (Stage == AdventureAwardStage.Waiting) { GroundTicks++; return; }
            float nextY = Y + velocityY;
            if (nextY < GroundY)
            {
                Y = nextY; velocityY += .15f; X += velocityX;
                if (X > 800 - Width && NativeType != 21) X = 800 - Width;
                else if (X < 0) X = 0;
            }
            else
            {
                Y = GroundY; X = (float)Math.Round(X, MidpointRounding.AwayFromZero);
                Stage = AdventureAwardStage.Waiting; GroundTicks = 0;
            }
        }
        private static float Clamp(float value) { return Math.Max(0, Math.Min(1, value)); }
        private static float Smooth(float t) { return 3f * t * t - 2f * t * t * t; }
    }
    public sealed partial class AdventureBoard
    {
        public AdventureRewardPresentation RewardPresentation { get; internal set; }
        internal float LastRewardX = 400f, LastRewardY = 300f;
        internal bool RewardLightfillPlayed;
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private void RememberAdventureRewardPosition(Zombie zombie)
        {
            // PC 0x54DE40: GetZombieRect centre. The basic body rect is (36,0,42,115).
            float scale = zombie.IsSmall ? .5f : 1f;
            float sourceX = 40f + zombie.X * 80f - 57f * scale - 20f * (1f - scale);
            float floor = 80f + zombie.Row * (Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f);
            if (Adventure.Level.HasRoof) floor += Math.Max(0, 440f - (sourceX + 40f)) * .25f - 10f;
            float sourceY = floor - 30f;
            if (zombie.Kind == ZombieKind.Balloon) sourceY -= 30f;
            int rectX = 36, rectY = 0, rectW = 42, rectH = 115;
            // Literal initial body rectangles from PC ZombieInitialize 0x53DD90.
            switch (zombie.Kind)
            {
                case ZombieKind.Football: rectX = 50; rectW = 57; break;
                case ZombieKind.Digger: rectX = 50; rectW = 28; break;
                case ZombieKind.Gargantuar: rectX = -17; rectY = -38; rectW = 125; rectH = 154; break;
                case ZombieKind.Zomboni: case ZombieKind.Catapult: rectX = 0; rectY = -13; rectW = 153; rectH = 140; break;
                case ZombieKind.Snorkel: rectX = 12; rectW = 62; break;
                case ZombieKind.Bobsled: if (zombie.PresentationHasSled ?? !zombie.UsedAbility) { rectX = -50; rectW = 275; } break;
                case ZombieKind.Balloon: rectY = 30; break;
                case ZombieKind.Bungee: rectX = -20; rectY = 22; rectW = 110; rectH = 94; break;
                case ZombieKind.Boss: rectX = 700; rectY = 80; rectW = 90; rectH = 430; break;
            }
            Adventure.LastRewardX = (int)sourceX + (rectX + rectW / 2) * scale + 20f * (1f - scale);
            Adventure.LastRewardY = (int)sourceY + (rectY + rectH / 2) * scale + 120f * (1f - scale);
        }
        private void SpawnAdventureRewardPresentation()
        {
            int kind = AdventureRewardPresentation.NativeTypeFor(Adventure.Level);
            int width = 60, height = 60;
            if (kind == 7) { width = 50; height = 70; }
            else if (kind == 15) { width = 78; height = 52; }
            else if (kind >= 9 && kind <= 14) { width = 80; height = 80; }
            else if (kind == 21) { width = 157; height = 269; }
            else if (kind == 18) { width = 91; height = 78; }
            Adventure.RewardPresentation = new AdventureRewardPresentation(kind, Adventure.LastRewardX, Adventure.LastRewardY,
                width, height, (float)random.NextDouble(), (float)random.NextDouble(), random.Next(20), Adventure.Level.Mode == AdventureMode.Boss);
        }
        private void AdvanceAdventureRewardPresentation(float delta)
        {
            if (!IsAdventure || ModeSession != null || State != MatchState.Won || Adventure.RewardPresentation == null) return;
            Adventure.RewardPresentation.Update(delta, () => { Elapsed += .01f; AdventurePresentationTick(.01f); AdvanceAdventurePresentationSuns(.01f);
                foreach (Plant plant in Plants)
                {
                    plant.Age += .01f;
                    if (AdventureProducerGlow.HasNativeProductionHint(plant.Kind)) AdventureProducerGlow.Tick(plant, false, 0, false);
                }
            });
            if (!Adventure.RewardLightfillPlayed && Adventure.RewardPresentation.NativeType != 15 && Adventure.RewardPresentation.CollectionTicks >= 300)
            { Adventure.RewardLightfillPlayed = true; Adventure.Notice(AdventureNoticeKind.RewardFade); }
        }
    }
}
