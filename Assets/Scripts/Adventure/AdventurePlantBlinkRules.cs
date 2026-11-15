#nullable enable

using System;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Implements PC PlantInit, DoBlink 472670 and UpdateBlink 472860 without rendering-time random draws.</summary>
    internal static class AdventurePlantBlinkRules
    {
        /// <summary>Consumes the original common animation initialization prefix in its native order.</summary>
        /// <param name="plant">The newly allocated plant.</param>
        /// <param name="random">The original shared board stream.</param>
        internal static void Initialize(Plant plant, Random random)
        {
            plant.AnimationFrameLength = 12 + random.Next(7);
            plant.PresentationRate = AdventureSpawnRandom.NextRange(random, 10f, 15f);
            plant.BlinkCountdown = 400 + random.Next(400);
            plant._presentationKind = plant.Kind;
        }

        /// <summary>Applies the species-specific random overrides after the native launch-counter draw.</summary>
        /// <param name="plant">The plant whose common animation state is initialized.</param>
        /// <param name="random">The original shared board stream.</param>
        internal static void CompleteInitialization(Plant plant, Random random)
        {
            if (AdventurePresentationRules.AttachedHead(plant.Kind) || plant.Kind == PlantKind.SplitPea || plant.Kind == PlantKind.Threepeater)
            {
                plant.PresentationRate = AdventureSpawnRandom.NextRange(random, 15f, 20f);
            }
            else if (plant.Kind == PlantKind.Imitater)
            {
                plant.PresentationRate = AdventureSpawnRandom.NextRange(random, 25f, 30f);
            }

            if (IsNut(plant.Kind))
            {
                plant.BlinkCountdown = 1000 + random.Next(1000);
            }
        }

        /// <summary>Advances the original free blink, then its awake countdown, once per native plant update.</summary>
        /// <param name="plant">The plant after its main action and before its next update.</param>
        /// <param name="random">The original shared board stream.</param>
        internal static void Tick(Plant plant, Random random)
        {
            if (plant.BlinkAnimation != null && ++plant.BlinkAnimationTicks * 15 >= FrameCount(plant.Kind, plant.BlinkAnimation) * 100)
            {
                plant.BlinkAnimation = null;
            }

            if (plant.Asleep || plant.BlinkCountdown <= 0 || --plant.BlinkCountdown != 0)
            {
                return;
            }

            // PC resets this counter before every rejection, including shooting and airborne states.
            plant.BlinkCountdown = 400 + random.Next(400);
            var shooting = plant.Timing?.ShootingCountdown > 0 || plant.MultiHeadTiming?.ShootingCountdown > 0
                || plant.UpgradeBurstTicks > 0 || plant.StarfruitShootingCountdown > 0;
            if (plant.PresentationBiteNotOnGround || plant.BungeeState == 2 || shooting
                || plant.Kind == PlantKind.PotatoMine && plant.MinePhase != MinePhase.Armed
                || plant.CactusPresentation?.NativePhase is 31 or 32 or 33
                || plant.MagnetPresentation?.NativePhase is 26 or 27)
            {
                return;
            }

            plant.BlinkAnimation = null;
            if (plant.Kind == PlantKind.TallNut && AdventureDefensivePlantRules.DamageStage(plant.Kind, plant.Health) == 2
                || plant.Kind == PlantKind.Garlic && plant.Health < 400 / 3)
            {
                return;
            }

            var animation = "anim_blink";
            plant.BlinkHead = 0;
            if (IsNut(plant.Kind))
            {
                plant.BlinkCountdown = 1000 + random.Next(1000);
                var choice = random.Next(10);
                animation = choice == 0 && plant.Kind != PlantKind.TallNut ? "anim_blink_twitch"
                    : choice < 7 ? "anim_blink_twice" : "anim_blink_thrice";
            }
            else if (plant.Kind == PlantKind.Threepeater)
            {
                plant.BlinkHead = random.Next(3);
                animation = plant.BlinkHead == 0 ? "anim_blink1" : plant.BlinkHead == 1 ? "anim_blink2" : "anim_blink3";
            }
            else if (plant.Kind == PlantKind.SplitPea)
            {
                plant.BlinkHead = random.Next(2);
                animation = plant.BlinkHead == 0 ? "anim_blink" : "anim_SplitPea_blink";
            }
            else if (plant.Kind == PlantKind.TwinSunflower)
            {
                plant.BlinkHead = random.Next(2);
                animation = plant.BlinkHead == 0 ? "anim_blink" : "anim_blink2";
            }

            if (FrameCount(plant.Kind, animation) > 0)
            {
                plant.BlinkAnimation = animation;
                plant.BlinkAnimationTicks = 0;
                plant.BlinkBaseAnimation ??= plant.ScaredyShroomPresentation?.Animation ?? plant.SquashPresentation?.Animation
                    ?? (plant.Kind == PlantKind.PotatoMine ? "anim_armed" : "anim_idle");
            }
        }

        /// <summary>Gets the blink marker's frame count from the unchanged original reanimation definitions.</summary>
        /// <param name="kind">The original plant species.</param>
        /// <param name="animation">The selected original blink marker.</param>
        /// <returns>The source frame count, or zero when the original marker does not exist.</returns>
        internal static int FrameCount(PlantKind kind, string animation)
        {
            if (IsNut(kind))
            {
                return animation == "anim_blink_twitch" ? kind == PlantKind.TallNut ? 0 : 5
                    : animation == "anim_blink_twice" ? 7 : animation == "anim_blink_thrice" ? 11 : 0;
            }

            if (kind == PlantKind.TwinSunflower)
            {
                return animation == "anim_blink" ? 5 : animation == "anim_blink2" ? 4 : 0;
            }

            if (kind == PlantKind.SeaShroom || kind == PlantKind.Torchwood || kind == PlantKind.UmbrellaLeaf)
            {
                return 4;
            }

            if (kind == PlantKind.GloomShroom || kind == PlantKind.KernelPult || kind == PlantKind.Squash
                || kind == PlantKind.WinterMelon || kind == PlantKind.Plantern || kind == PlantKind.Cattail
                || kind == PlantKind.Jalapeno || kind == PlantKind.Cactus || kind == PlantKind.TangleKelp || kind == PlantKind.Starfruit
                || kind == PlantKind.Marigold || kind == PlantKind.MelonPult || kind == PlantKind.CabbagePult)
            {
                return 5;
            }

            return kind == PlantKind.Peashooter || kind == PlantKind.Sunflower || kind == PlantKind.PotatoMine
                || kind == PlantKind.SnowPea || kind == PlantKind.Repeater || kind == PlantKind.PuffShroom
                || kind == PlantKind.SunShroom || kind == PlantKind.FumeShroom || kind == PlantKind.ScaredyShroom
                || kind == PlantKind.IceShroom || kind == PlantKind.LilyPad || kind == PlantKind.Threepeater
                || kind == PlantKind.TangleKelp || kind == PlantKind.Spikeweed || kind == PlantKind.SplitPea
                || kind == PlantKind.MagnetShroom || kind == PlantKind.Garlic || kind == PlantKind.GatlingPea
                || kind == PlantKind.GoldMagnet || kind == PlantKind.CobCannon || kind == PlantKind.Imitater || kind == PlantKind.Spikerock
                || (int)kind == 52 ? 3 : 0;
        }

        /// <summary>Checks the native nut blink override branches.</summary>
        /// <param name="kind">The original plant identifier.</param>
        /// <returns>Whether the species uses the thousand-update countdown and multiple-blink selection.</returns>
        private static bool IsNut(PlantKind kind)
        {
            return kind == PlantKind.Wallnut || kind == PlantKind.TallNut || (int)kind == 49 || (int)kind == 50;
        }
    }
}
