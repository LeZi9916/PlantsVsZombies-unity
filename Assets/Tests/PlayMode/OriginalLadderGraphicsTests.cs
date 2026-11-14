#nullable enable

using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
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
            foreach (var sample in new[] { (19, 0), (19, 4), (19, 8), (49, 0), (49, 4), (49, 8) })
            {
                var level = sample.Item1;
                var column = sample.Item2;
                using var fixture = new OriginalStormGraphicsTests.StormFixture(level);
                var game = fixture.Owner.RequiredGame;
                game.ChooseDefaultAdventureSeeds();
                game.Plants.Clear();
                var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.TallNut, 1, column)!;
                plant.HasLadder = true;
                var zombie = Spawn(game, ZombieKind.Normal);
                zombie.X = AdventureZombieNativeRules.GridX(80f + column * 80f);
                zombie.PresentationPreviousX = zombie.X;
                var image = (SpriteImage)typeof(ReanimationRenderer).GetMethod("LoadTexture", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(fixture.Animations, new object[] { "IMAGE_REANIM_ZOMBIE_LADDER_5" })!;
                Assert.That(fixture.Animations.TryGetNativeSpriteSize("IMAGE_REANIM_ZOMBIE_LADDER_5", out var size), Is.True);
                var cellY = level == 49 ? 155f + Math.Max(0, 5 - column) * 20f : 180f;
                var destination = new Rect(65f + column * 80f, cellY - 4f, size.x * .8f, size.y * .8f);
                yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
                var actual = fixture.Pixels.GetPixels32();
                plant.HasLadder = false;
                game.Zombies.Remove(zombie);
                yield return fixture.Capture("DrawAdventureBoard", () =>
                {
                    // PC 527EF0 preserves the requested top-left while scaling the original source dimensions.
                    fixture.Animations.DrawNativeImage(image, destination, new Rect(0f, 0f, 1f, 1f));
                    fixture.DrawPass("DrawAdventureZombie", zombie);
                }, true);
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "PC ladder geometry and plant/zombie order on level " + level);
                yield return fixture.Capture("DrawAdventureBoard", () =>
                {
                    fixture.DrawPass("DrawAdventureZombie", zombie);
                    fixture.Animations.DrawNativeImage(image, destination, new Rect(0f, 0f, 1f, 1f));
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
            foreach (var kind in new[] { ZombieKind.Normal, ZombieKind.Ladder })
            {
                var zombie = Spawn(game, kind);
                zombie.PresentationWalk2 = false;
                var clip = kind == ZombieKind.Ladder ? "anim_ladderwalk" : "anim_walk";
                zombie.PresentationAnimation = clip;
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
                        var origin = new Vector2(415f, 117f - phase.Item2);
                        if (kind == ZombieKind.Normal)
                        {
                            fixture.Animations.DrawZombiePlayback(origin, 1f, .25f,
                                ZombieVariant.Normal, "anim_walk", true, rate, true);
                        }
                        else
                        {
                            for (var group = 0; group < 3; group++)
                            {
                                var selected = group;
                                fixture.Animations.DrawNativePlayback("Zombie_ladder", origin,
                                    1f, .25f, clip, true, rate, track => NativeLadderGroup(track) == selected);
                            }
                        }
                    });
                    Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), kind + " original ladder height state " + phase.Item1);
                }
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
