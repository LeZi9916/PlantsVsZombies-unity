#nullable enable

using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original reload phases, corn payload visibility and PC butter head placement with real artwork.</summary>
    public sealed class OriginalButterAndReloadGraphicsTests
    {
        /// <summary>Checks completed lobber shooting restarts the source idle at twelve FPS and a fresh clock.</summary>
        /// <returns>Real rendering frames comparing independent original source playback.</returns>
        [UnityTest]
        public IEnumerator ReloadedLobbersUseTheOriginalFreshIdlePose()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            var game = fixture.Owner.RequiredGame;
            game.Plants.Clear();
            var id = 0;
            foreach (var species in new[] { (PlantKind.CabbagePult, "Cabbagepult", 38), (PlantKind.KernelPult, "Cornpult", 34),
                (PlantKind.MelonPult, "Melonpult", 38), (PlantKind.WinterMelon, "WinterMelon", 38) })
            {
                var plant = new Plant { Id = ++id, Kind = species.Item1, Row = 1, Column = 2, Health = 300f,
                    Age = 9.2f, PresentationRate = 12f, PresentationShotAge = 2f, PresentationLobberIdleAge = .14f };
                game.Plants.Clear();
                game.Plants.Add(plant);
                Assert.That(fixture.Animations.TryGetNativeAnimationInfo(species.Item2, "anim_shooting", out var clip), Is.True);
                Assert.That(clip.Count, Is.EqualTo(species.Item3));
                Assert.That(AdventurePresentationRules.LobberShootingFrames(species.Item1), Is.EqualTo(clip.Count));
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    Assert.That(fixture.Animations.DrawNativePlayback(species.Item2, new Vector2(200f, 215f),
                        1f, .14f, "anim_idle", true, 12f, track => !track.StartsWith("Cornpult_butter", StringComparison.OrdinalIgnoreCase)), Is.True);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), species.Item1 + " fresh reload clock");
            }
        }

        /// <summary>Checks PC StartBlend skips a hidden cabbage track instead of reusing its last visible throw pose.</summary>
        /// <returns>Real body frames before comparing the original loaded projectile geometry.</returns>
        [UnityTest]
        public IEnumerator CabbageReloadDoesNotBlendFromTheLastVisibleThrowPose()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            var game = fixture.Owner.RequiredGame;
            game.Plants.Clear();
            var plant = new Plant { Id = 9, Kind = PlantKind.CabbagePult, Row = 1, Column = 2,
                Health = 300f, PresentationRate = 12f, PresentationShotAge = .30f };
            game.Plants.Add(plant);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
            plant.PresentationShotAge = 1.08f;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
            plant.PresentationShotAge = 1.09f;
            plant.PresentationLobberIdleAge = 0f;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var image = (SpriteImage)typeof(ReanimationRenderer).GetMethod("LoadTexture", flags)!
                .Invoke(fixture.Animations, new object[] { "IMAGE_REANIM_CABBAGEPULT_CABBAGE" })!;
            var renderer = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(fixture.Animations)!;
            var actual = PayloadVertices(renderer, image);
            yield return fixture.Capture(null, () =>
            {
                fixture.Animations.DrawNativePlayback("Cabbagepult", new Vector2(200f, 215f), 1f, 0f,
                    "anim_idle", true, 12f, track => track == "Cabbagepult_cabbage");
            });
            var expected = PayloadVertices(renderer, image);
            for (var corner = 0; corner < 4; corner++)
            {
                Assert.That(actual[corner].x, Is.EqualTo(expected[corner].x).Within(.0001f), "Reload x corner " + corner);
                Assert.That(actual[corner].y, Is.EqualTo(expected[corner].y).Within(.0001f), "Reload y corner " + corner);
            }
        }

        /// <summary>Checks firing butter changes the basket back to the kernel render group at the native launch tick.</summary>
        /// <returns>Real original-art camera frames before and after weapon reset.</returns>
        [UnityTest]
        public IEnumerator ButterShotRestoresTheOriginalVisibleKernelGroup()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            var game = fixture.Owner.RequiredGame;
            game.Plants.Clear();
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.KernelPult, 1, 2)!;
            plant.PresentationButter = true;
            plant._kernelWeaponPrepared = true;
            plant.PresentationShotAge = .28f;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
            Invoke(game, "FireAdventureNativeTimedProjectile", plant);
            Assert.That(game.Peas[0].Butter, Is.True);
            Assert.That(plant.PresentationButter, Is.False);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
            var actual = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () =>
            {
                Assert.That(fixture.Animations.DrawNativePlayback("Cornpult", new Vector2(200f, 215f),
                    1f, .29f, "anim_shooting", false, 35f,
                    track => !track.StartsWith("Cornpult_butter", StringComparison.OrdinalIgnoreCase)), Is.True);
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
        }

        /// <summary>Checks PC 549EC0 head-center attachment, special tracks, unscaled offsets and species patch scales.</summary>
        /// <returns>Actual original-art frames for normal, small, mirrored and specialized head anchors.</returns>
        [UnityTest]
        public IEnumerator ButterUsesTheOriginalTransformedHeadAndSpeciesOffsets()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            Assert.That(fixture.Animations.TryGetNativeSpriteSize("IMAGE_REANIM_CORNPULT_BUTTER_SPLAT", out var imageSize), Is.True);
            Assert.That(imageSize, Is.EqualTo(new Vector2Int(52, 49)));
            foreach (var species in new[] {
                (ZombieKind.Normal, "Zombie", "anim_walk", "anim_head1", 0, 1f, false, -3.5f, -12f, 1f),
                (ZombieKind.Normal, "Zombie", "anim_walk", "anim_head1", 0, .5f, false, -3.5f, -12f, 1f),
                (ZombieKind.Digger, "Zombie_digger", "anim_walk", "anim_head1", 37, 1f, true, -3.5f, -12f, 1f),
                (ZombieKind.Newspaper, "Zombie_paper", "anim_gasp", "anim_head_look", 30, 1f, false, -3.5f, -12f, 1f),
                (ZombieKind.Catapult, "Zombie_catapult", "anim_walk", "Zombie_catapult_driver_head", 0, 1f, false, -3.5f, -12f, 1f),
                (ZombieKind.Gargantuar, "Zombie_gargantuar", "anim_walk", "anim_head1", 0, 1f, false, -3.3f, -22.1f, 1.2f),
                (ZombieKind.Bungee, "Zombie_bungi", "anim_idle", "anim_head1", 6, 1f, false, 1.7f, -7.1f, 1.2f),
                (ZombieKind.Pogo, "Zombie_pogo", "anim_walk", "anim_head1", 29, 1f, false, -3.5f, -17f, 1f) })
            {
                var zombie = new Zombie { Kind = species.Item1, PresentationNativePhase = species.Item5,
                    IsSmall = species.Item6 < 1f, PresentationWalkingBackwards = species.Item7,
                    PresentationButter = 4f, UsedAbility = true };
                var origin = new Vector2(300f, 260f);
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureButter", zombie, species.Item2,
                    species.Item3, origin, .25f, 12f, species.Item6));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    Assert.That(fixture.Animations.TryGetNativeTrackCenter(species.Item2, species.Item3,
                        species.Item4, .25f, 12f, out var head), Is.True, species.Item2 + " original head track");
                    // PC 527EF0 adds scaled half-size after (-29,-36); GetTrackMatrix contributes -.5.
                    var x = 300f + (species.Item7 ? 90f - head.x : head.x) * species.Item6 + species.Item8;
                    var y = 260f + head.y * species.Item6 + species.Item9;
                    fixture.Animations.DrawResourceSprite("IMAGE_REANIM_CORNPULT_BUTTER_SPLAT", new Vector2(x, y), species.Item10);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), species.Item2 + " original butter head position");
            }
        }

        /// <summary>Checks a newly immobilized head retains the actual pre-transition pose during the original blend.</summary>
        /// <returns>Real frames comparing the already-drawn head transform with the butter attachment.</returns>
        [UnityTest]
        public IEnumerator ButterFollowsTheBodyDuringAnEatingTransitionBlend()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            var game = fixture.Owner.RequiredGame;
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Normal, 1,
                AdventureZombieNativeRules.GridX(500f), 0)!;
            zombie.PresentationWalk2 = false;
            zombie.PresentationAnimation = "anim_walk";
            zombie.PresentationAnimationAge = .4f;
            var walkRate = game.AdventurePresentationZombieRate(zombie);
            Assert.That(fixture.Animations.TryGetNativeTrackCenter("Zombie", "anim_walk", "anim_head1",
                .4f, walkRate, out var oldHead), Is.True);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
            zombie.Eating = true;
            zombie.PresentationAnimation = "anim_eat";
            zombie.PresentationAnimationAge = 0f;
            zombie.PresentationButter = 4f;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
            var actual = fixture.Pixels.GetPixels32();
            zombie.PresentationButter = 0f;
            yield return fixture.Capture(null, () =>
            {
                fixture.DrawPass("DrawAdventureZombie", zombie);
                // At blend time zero the displayed head is still the independently sampled previous walking pose.
                fixture.Animations.DrawResourceSprite("IMAGE_REANIM_CORNPULT_BUTTER_SPLAT",
                    new Vector2(515f + oldHead.x - 3.5f, 117f + oldHead.y - 12f), 1f);
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
        }

        /// <summary>Copies the production projectile sprite corners without depending on its submission index.</summary>
        /// <param name="renderer">The production sprite batch owner.</param>
        /// <param name="image">The unchanged original projectile image.</param>
        /// <returns>The submitted original projectile corners.</returns>
        private static Vector3[] PayloadVertices(ObjectSpriteRenderer renderer, SpriteImage image)
        {
            for (var index = 0; index < renderer.SubmittedObjectCount; index++)
            {
                var sprite = renderer.SubmittedSprites[index];
                if (ReferenceEquals(sprite.SourceImage, image))
                {
                    return new[] { sprite.Vertices[0], sprite.Vertices[1], sprite.Vertices[2], sprite.Vertices[3] };
                }
            }

            Assert.Fail("The original loaded cabbage sprite was not submitted.");
            return Array.Empty<Vector3>();
        }

        /// <summary>Invokes an existing production simulation entry point.</summary>
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
