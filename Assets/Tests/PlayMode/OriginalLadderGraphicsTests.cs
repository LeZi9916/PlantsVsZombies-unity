#nullable enable

using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original ladder artwork, placing playback and lifted walking poses through the production presenter.</summary>
    public sealed class OriginalLadderGraphicsTests
    {
        /// <summary>Checks PC 459610 geometry and row 302800 order relative to the protected plant and a following zombie.</summary>
        /// <returns>Real board frames comparing independent original-art composition.</returns>
        [UnityTest]
        public IEnumerator PlacedLadderUsesOriginalGeometryAndOcclusion()
        {
            foreach (var level in new[] { 19, 49 })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(level);
                var game = fixture.Owner.RequiredGame;
                game.ChooseDefaultAdventureSeeds();
                game.Plants.Clear();
                var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.TallNut, 1, 4)!;
                plant.HasLadder = true;
                var zombie = Spawn(game, ZombieKind.Normal);
                Assert.That(fixture.Animations.TryGetNativeSpriteSize("IMAGE_REANIM_ZOMBIE_LADDER_5", out var size), Is.True);
                yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
                var actual = fixture.Pixels.GetPixels32();
                plant.HasLadder = false;
                game.Zombies.Remove(zombie);
                yield return fixture.Capture("DrawAdventureBoard", () =>
                {
                    // GridToPixel(4,1) is (360,180) on day/night and (360,175) on the roof.
                    var cellY = level == 49 ? 175f : 180f;
                    fixture.Animations.DrawResourceSprite("IMAGE_REANIM_ZOMBIE_LADDER_5",
                        new Vector2(385f + size.x * .5f, cellY - 4f + size.y * .5f), .8f);
                    fixture.DrawPass("DrawAdventureZombie", zombie);
                }, true);
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "PC ladder geometry and plant/zombie order on level " + level);
                yield return fixture.Capture("DrawAdventureBoard", () =>
                {
                    fixture.DrawPass("DrawAdventureZombie", zombie);
                    var cellY = level == 49 ? 175f : 180f;
                    fixture.Animations.DrawResourceSprite("IMAGE_REANIM_ZOMBIE_LADDER_5",
                        new Vector2(385f + size.x * .5f, cellY - 4f + size.y * .5f), .8f);
                }, true);
                Assert.That(fixture.Pixels.GetPixels32(), Is.Not.EqualTo(actual), "The fixture must exercise visible ladder/zombie overlap.");
            }
        }

        /// <summary>Checks the placing pose uses the original source clip at 24 FPS instead of the carrying walk.</summary>
        /// <returns>Real original-art frame boundaries.</returns>
        [UnityTest]
        public IEnumerator CarrierUsesOriginalPlaceLadderClip()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            var zombie = Spawn(fixture.Owner.RequiredGame, ZombieKind.Ladder);
            zombie.PresentationNativePhase = 77;
            zombie.PresentationAnimation = "anim_placeladder";
            zombie.PresentationAnimationAge = .25f;
            Assert.That(fixture.Animations.TryGetNativeAnimationInfo("Zombie_ladder", "anim_placeladder", out var clip), Is.True);
            Assert.That(clip.Count, Is.EqualTo(12));
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
            var actual = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () =>
            {
                // PC AttachShield assigns the carried ladder to group one and outer hands to group two.
                for (var group = 0; group < 3; group++)
                {
                    var selected = group;
                    Assert.That(fixture.Animations.DrawNativePlayback("Zombie_ladder", new Vector2(415f, 117f),
                        1f, .25f, "anim_placeladder", false, 24f, track => NativeLadderGroup(track) == selected), Is.True);
                }
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
        }

        /// <summary>Checks both climbing and falling lift the original walking body, including plain pool-body state.</summary>
        /// <returns>Actual render frames for ascent and descent pose comparisons.</returns>
        [UnityTest]
        public IEnumerator ClimbingAndFallingRenderTheOriginalRaisedWalkingPose()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            var game = fixture.Owner.RequiredGame;
            var zombie = Spawn(game, ZombieKind.Normal);
            zombie.PresentationWalk2 = false;
            zombie.PresentationAnimation = "anim_walk";
            zombie.PresentationAnimationAge = .25f;
            zombie.UseLadderColumn = 4;
            foreach (var phase in new[] { (6, 32f), (7, 19f) })
            {
                zombie.PresentationNativeHeight = phase.Item1;
                zombie.PresentationAltitude = phase.Item2;
                zombie.PoolState = new AdventureZombiePoolState { Altitude = phase.Item2 };
                var rate = game.AdventurePresentationZombieRate(zombie);
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    fixture.Animations.DrawZombiePlayback(new Vector2(415f, 117f - phase.Item2), 1f, .25f,
                        ZombieVariant.Normal, "anim_walk", true, rate, true);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Original ladder height state " + phase.Item1);
            }
        }

        /// <summary>Transcribes the original AttachShield render groups independently of the production renderer.</summary>
        /// <param name="track">The unchanged ladder reanimation track.</param>
        /// <returns>The native body, shield or foreground-arm group.</returns>
        private static int NativeLadderGroup(string track)
        {
            return string.Equals(track, "Zombie_ladder_1", StringComparison.OrdinalIgnoreCase) ? 1
                : track.StartsWith("Zombie_outerarm", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        }

        /// <summary>Creates a production zombie at the original controlled roof coordinate.</summary>
        /// <param name="game">The production board receiving the actor.</param>
        /// <param name="kind">The original zombie type.</param>
        /// <returns>The initialized actor with a fixed native body origin.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind)
        {
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", kind, 1, AdventureZombieNativeRules.GridX(400f), 0)!;
            zombie.X = AdventureZombieNativeRules.GridX(400f);
            zombie.PresentationPreviousX = zombie.X;
            return zombie;
        }

        /// <summary>Invokes a production simulation entry point without replacing behavior.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="method">The original method name.</param>
        /// <param name="arguments">The original arguments.</param>
        /// <returns>The original method result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
