#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares production catapult clips, damage resources and ball geometry against unchanged original artwork.</summary>
    public sealed class OriginalCatapultGraphicsTests
    {
        /// <summary>Checks walking, shooting, reloading and bouncing clips, damaged pole variants and depleted basket tracks.</summary>
        /// <returns>Real camera frame boundaries comparing independent source-track composition.</returns>
        [UnityTest]
        public IEnumerator OriginalClipsDamagePanelsAndAmmunitionMatchNativeSourceTracks()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            var source = XElement.Parse("<reanimation>" + File.ReadAllText(Path.Combine(fixture.Root, "reanim", "Zombie_catapult.reanim")) + "</reanimation>");
            var tracks = source.Elements("track").Select(track => track.Element("name")!.Value).ToArray();
            Assert.That(fixture.Animations.TryGetNativeAnimationInfo("Zombie_catapult", "anim_shoot", out var shooting), Is.True);
            Assert.That(shooting.Count, Is.EqualTo(32));
            Assert.That(fixture.Animations.TryGetNativeAnimationInfo("Zombie_catapult", "anim_bounce", out var bouncing), Is.True);
            Assert.That(bouncing.Count, Is.EqualTo(15));
            var id = 0;
            foreach (var sample in new[] {
                (0, 850f, 20, .3f, "anim_walk", 5.5f, false),
                (67, 850f, 20, .4f, "anim_shoot", 24f, false),
                (67, 282f, 20, .4f, "anim_shoot", 24f, false),
                (67, 282f, 20, .85f, "anim_shoot", 24f, false),
                (68, 565f, 4, .3f, "anim_idle", 12f, false),
                (68, 282f, 1, .3f, "anim_idle", 12f, false),
                (0, 282f, 0, .3f, "anim_walk", 6f, false),
                (1, 830f, 20, .3f, "anim_bounce", 12f, true) })
            {
                var zombie = new Zombie { Id = ++id, Kind = ZombieKind.Catapult, Row = 1,
                    X = AdventureZombieNativeRules.GridX(400f), StartingHealth = 850f, Health = sample.Item2,
                    PresentationNativePhase = sample.Item1, PresentationAnimation = sample.Item5,
                    PresentationAnimationAge = sample.Item4, PresentationBodyBlendTicks = 0,
                    CatapultState = new AdventureCatapultState { Ammunition = sample.Item3,
                        WalkingRate = sample.Item6, FlatTires = sample.Item7 } };
                fixture.Owner.RequiredGame.Zombies.Clear();
                fixture.Owner.RequiredGame.Zombies.Add(zombie);
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    // PC body overlay: native (400,125) + (15,-8) + species (-25,-18).
                    foreach (var track in tracks)
                    {
                        var remaining = sample.Item3;
                        if (track == "Zombie_catapult_basketball" && remaining <= 4
                            || track == "Zombie_catapult_basketball2" && remaining <= 3
                            || track == "Zombie_catapult_basketball3" && remaining <= 2
                            || track == "Zombie_catapult_basketball4" && remaining <= 1)
                        {
                            continue;
                        }

                        string? image = null;
                        if (track == "Zombie_catapult_siding" && (sample.Item2 < 566f || sample.Item7))
                        {
                            image = "IMAGE_REANIM_ZOMBIE_CATAPULT_SIDING_DAMAGE";
                        }

                        if (track == "Zombie_catapult_pole")
                        {
                            var frame = sample.Item5 == "anim_shoot" ? 25 + (int)(sample.Item4 * 24f) : 0;
                            var ballVisible = remaining > 0 && (frame < 42 || frame >= 56);
                            if (sample.Item2 < 283f || sample.Item7)
                            {
                                image = ballVisible ? "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE_DAMAGE_WITHBALL"
                                    : "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE_DAMAGE";
                            }
                            else if (remaining == 0)
                            {
                                image = "IMAGE_REANIM_ZOMBIE_CATAPULT_POLE";
                            }
                        }

                        fixture.Animations.DrawNativePlayback("Zombie_catapult", new Vector2(390f, 99f), 1f,
                            sample.Item4, sample.Item5, sample.Item1 != 67 && !sample.Item7,
                            sample.Item6, name => name == track, track, image);
                    }
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Original catapult state " + sample);
            }
        }

        /// <summary>Checks the ball's image-centered 1.1-scale rotation and top-left scaled shadow on day and night boards.</summary>
        /// <returns>Real camera frame boundaries comparing independent native matrix geometry.</returns>
        [UnityTest]
        public IEnumerator BasketballUsesOriginalImageCenterRotationAndShadow()
        {
            foreach (var level in new[] { 9, 19 })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(level);
                var ball = new Pea { Basketball = true, Row = 1, X = 4f, PresentationPreviousX = 4f,
                    PresentationLaunchY = 106f, PresentationLobHeight = -40f, PresentationPreviousLobHeight = -40f,
                    PresentationLobNativeZ = -40f, PresentationStarShadowY = 247f,
                    PresentationRotation = .4f, PresentationSpin = .07f, PresentationNativeAge = .5f };
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureProjectile", ball));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    Assert.That(fixture.Animations.TryGetNativeSpriteSize("IMAGE_PEA_SHADOWS", out var shadow), Is.True);
                    Assert.That(fixture.Animations.TryGetNativeSpriteSize("IMAGE_REANIM_ZOMBIE_CATAPULT_BASKETBALL", out var image), Is.True);
                    const float shadowScale = 200f / 240f;
                    fixture.Animations.DrawResourceSprite("IMAGE_PEA_SHADOWS",
                        new Vector2(360f + shadow.x * .25f * shadowScale, 247f + shadow.y * .5f * shadowScale),
                        shadowScale, 0f, level == 19 ? 1 : 0);
                    // TodScaleRotateTransformMatrix uses -mRotation, with the unscaled source half-size at the center.
                    fixture.Animations.DrawResourceSprite("IMAGE_REANIM_ZOMBIE_CATAPULT_BASKETBALL",
                        new Vector2(360f + image.x * .5f, 66f + image.y * .5f), 1.1f, -3.9f * Mathf.Rad2Deg);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
            }
        }

        /// <summary>Checks damaged engine smoke and the actual native catapult explosion source are submitted by production passes.</summary>
        /// <returns>Real source-backed particle camera frames.</returns>
        [UnityTest]
        public IEnumerator OriginalSmokeAndExplosionResourcesReachTheProductionRenderer()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            var zombie = new Zombie { Id = 91, Kind = ZombieKind.Catapult, Row = 1,
                X = AdventureZombieNativeRules.GridX(400f), StartingHealth = 850f, Health = 282f, Age = .4f,
                PresentationNativePhase = 68, PresentationAnimation = "anim_idle", PresentationAnimationAge = .3f,
                CatapultState = new AdventureCatapultState { SmokeStartAge = 0f } };
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", zombie));
            AssertSource(fixture.Animations, "ZamboniSmoke.png");
            fixture.Owner.RequiredGame.Presentation.ZomboniParticles.Add(new AdventureZomboniParticle
            {
                Id = 99, Row = 1, Seed = 91, Particle = "CatapultExplosion", X = 480f, Y = 185f, Age = .3f
            });
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureExplosionRow", 1));
            Assert.That(Array.Exists(fixture.Pixels.GetPixels32(), pixel => pixel.r < 200 || pixel.g < 200 || pixel.b < 200), Is.True);
            AssertSource(fixture.Animations, "Zombie_catapult_pole_damage.png");
        }

        /// <summary>Checks a submitted source image independently of pooled object names.</summary>
        /// <param name="animations">The production renderer owning the current frame.</param>
        /// <param name="filename">The original source filename suffix.</param>
        private static void AssertSource(ReanimationRenderer animations, string filename)
        {
            var renderer = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            Assert.That(Enumerable.Range(0, renderer.SubmittedObjectCount)
                .Any(index => renderer.SubmittedSprites[index].SourceImage?.SourceIdentity.EndsWith(filename, StringComparison.OrdinalIgnoreCase) == true),
                Is.True, "Original source " + filename);
        }
    }
}
