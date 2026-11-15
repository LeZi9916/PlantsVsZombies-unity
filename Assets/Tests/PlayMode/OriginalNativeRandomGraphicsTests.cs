#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original blink marker metadata and real source artwork without consuming simulation randomness during drawing.</summary>
    public sealed class OriginalNativeRandomGraphicsTests
    {
        /// <summary>Checks every original plant blink frame count against the production reanimation loader, including implicit initial frame visibility.</summary>
        [Test]
        public void BlinkFrameCountsMatchEveryOriginalSourceDefinition()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
            var resolve = typeof(GamePresenter).GetMethod("AdventurePlantAnimation", BindingFlags.Static | BindingFlags.NonPublic)!;
            for (var id = 0; id < 53; id++)
            {
                var kind = (PlantKind)id;
                var name = (string)resolve.Invoke(null, new object[] { kind })!;
                var clips = id == 3 || id == 23 || id == 49 || id == 50
                    ? new[] { "anim_blink_twitch", "anim_blink_twice", "anim_blink_thrice" }
                    : id == 18 ? new[] { "anim_blink1", "anim_blink2", "anim_blink3" }
                    : id == 28 ? new[] { "anim_blink", "anim_SplitPea_blink" }
                    : id == 41 ? new[] { "anim_blink", "anim_blink2" } : new[] { "anim_blink" };
                foreach (var clip in clips)
                {
                    var found = fixture.Animations.TryGetNativeAnimationInfo(name, clip, out var info);
                    Assert.That(AdventurePlantBlinkRules.FrameCount(kind, clip), Is.EqualTo(found ? info.Count : 0), name + "/" + clip);
                }
            }
        }

        /// <summary>Checks original blink artwork appears on ordinary and attached-head plants, restores their source pose, and leaves random state unchanged.</summary>
        /// <returns>Real source-art camera frames for the independent blink and restored parent pose.</returns>
        [UnityTest]
        public IEnumerator OriginalBlinksRenderAndRestoreWithoutAdvancingRandomness()
        {
            foreach (var sample in new[] { (PlantKind.PuffShroom, "anim_blink", 0),
                (PlantKind.Peashooter, "anim_blink", 0), (PlantKind.Wallnut, "anim_blink_twice", 0),
                (PlantKind.Threepeater, "anim_blink3", 2), (PlantKind.SplitPea, "anim_SplitPea_blink", 1),
                (PlantKind.TwinSunflower, "anim_blink2", 1) })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(19);
                var plant = new Plant { Id = 991, Kind = sample.Item1, Row = 1, Column = 2,
                    Health = 4000f, Age = .3f, PresentationRate = 12f };
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
                var idle = fixture.Pixels.GetPixels32();
                var random = fixture.Owner.RequiredGame.CaptureBattleRandomState();
                plant.BlinkAnimation = sample.Item2;
                plant.BlinkHead = sample.Item3;
                plant.BlinkAnimationTicks = sample.Item1 == PlantKind.TwinSunflower ? 7 : 1;
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
                Assert.That(fixture.Pixels.GetPixels32().Where((color, index) => !color.Equals(idle[index])).Count(), Is.GreaterThan(0), sample.Item1.ToString());
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
                Assert.That(fixture.Owner.RequiredGame.CaptureBattleRandomState().DrawCount, Is.EqualTo(random.DrawCount));
                plant.BlinkAnimation = null;
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(idle), sample.Item1 + " restored body and eye tracks");
            }
        }

        /// <summary>Checks that a blink preserves inherited draw state and the same-frame artwork of subsequent plants, zombies and sun.</summary>
        /// <returns>Real camera frames comparing following actors with and without an earlier plant blink.</returns>
        [UnityTest]
        public IEnumerator PlantBlinksPreserveFollowingActorsInTheSameFrame()
        {
            foreach (var kind in new[] { PlantKind.PuffShroom, PlantKind.Threepeater })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
                var blinking = new Plant { Id = 991, Kind = kind, Row = 1, Column = 0,
                    Health = 300f, Age = .3f, PresentationRate = 12f };
                var following = new Plant { Id = 992, Kind = PlantKind.Sunflower, Row = 0, Column = 5,
                    Health = 300f, Age = .3f, PresentationRate = 12f };
                var zombie = new Zombie { Id = 993, Kind = ZombieKind.Normal, Row = 1,
                    X = AdventureZombieNativeRules.GridX(550f), Health = 270f, PresentationAnimationAge = .3f };
                var sun = new global::PlantsVsZombies.Simulation.Sun { X = 6f, Y = 3f, Age = .3f };
                var tint = new Color(.8f, .6f, .9f, .7f);
                Action drawFollowers = () =>
                {
                    fixture.DrawPass("DrawAdventurePlant", following);
                    fixture.DrawPass("DrawAdventureZombie", zombie);
                    fixture.DrawPass("DrawAdventureSun", sun);
                };
                yield return fixture.Capture(null, () =>
                {
                    SpriteDrawState.Color = tint;
                    fixture.DrawPass("DrawAdventurePlant", blinking);
                    drawFollowers();
                });
                var expected = fixture.Pixels.GetPixels32();
                blinking.BlinkAnimation = kind == PlantKind.Threepeater ? "anim_blink3" : "anim_blink";
                blinking.BlinkHead = kind == PlantKind.Threepeater ? 2 : 0;
                blinking.BlinkAnimationTicks = 1;
                var inherited = Matrix4x4.identity;
                var returned = Matrix4x4.identity;
                var returnedTint = Color.clear;
                yield return fixture.Capture(null, () =>
                {
                    inherited = SpriteDrawState.Matrix;
                    SpriteDrawState.Color = tint;
                    fixture.DrawPass("DrawAdventurePlant", blinking);
                    returned = SpriteDrawState.Matrix;
                    returnedTint = SpriteDrawState.Color;
                    drawFollowers();
                });
                Assert.That(returned, Is.EqualTo(inherited), kind + " blink must restore the caller's matrix.");
                Assert.That(returnedTint, Is.EqualTo(tint), kind + " blink must restore the caller's tint.");
                var actual = fixture.Pixels.GetPixels32();
                var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
                var rightHalf = (Screen.width - 800f * scale) * .5f + 400f * scale;
                var changed = 0;
                var visible = 0;
                for (var index = 0; index < actual.Length; index++)
                {
                    if (index % fixture.Target.width < rightHalf)
                    {
                        continue;
                    }

                    if (!actual[index].Equals(expected[index]))
                    {
                        changed++;
                    }
                    if (expected[index].r < 250 || expected[index].g < 250 || expected[index].b < 250)
                    {
                        visible++;
                    }
                }

                Assert.That(visible, Is.GreaterThan(100), "The following actors must be visible in the camera comparison.");
                Assert.That(changed, Is.Zero, kind + " blink must not change following plants, zombies or sun.");
            }
        }

        /// <summary>Checks attached blink artwork precedes the original foreground leaves on both idle and action clips.</summary>
        /// <returns>Original source-art frames compared with explicit PC per-track attachment painter order.</returns>
        [UnityTest]
        public IEnumerator BlinksKeepOriginalForegroundLeavesVisible()
        {
            foreach (var sample in new[] { (PlantKind.MelonPult, "Melonpult", false), (PlantKind.MelonPult, "Melonpult", true),
                (PlantKind.UmbrellaLeaf, "Umbrellaleaf", false), (PlantKind.UmbrellaLeaf, "Umbrellaleaf", true) })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
                SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
                var plant = new Plant { Id = 991, Kind = sample.Item1, Row = 1, Column = 2, Health = 300f,
                    Age = .3f, PresentationRate = 12f, BlinkAnimation = "anim_blink", BlinkAnimationTicks = 7 };
                var clip = sample.Item3 ? sample.Item1 == PlantKind.MelonPult ? "anim_shooting" : "anim_block" : "anim_idle";
                var seconds = sample.Item3 ? .6f : .3f;
                var rate = sample.Item3 ? sample.Item1 == PlantKind.MelonPult ? AdventurePresentationRules.ShootingRate(sample.Item1) : 22f : 12f;
                if (sample.Item3)
                {
                    if (sample.Item1 == PlantKind.MelonPult)
                    {
                        plant.PresentationShotAge = seconds;
                    }
                    else
                    {
                        plant.PresentationUmbrellaAge = seconds;
                    }
                }

                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", plant));
                var actual = fixture.Pixels.GetPixels32();
                var tracks = XElement.Parse("<r>" + File.ReadAllText(Path.Combine(fixture.Root, "reanim", sample.Item2 + ".reanim")) + "</r>")
                    .Elements("track").Select(track => track.Element("name")!.Value).ToArray();
                var face = Array.IndexOf(tracks, "anim_face");
                Assert.That(face, Is.GreaterThanOrEqualTo(0));
                yield return fixture.Capture(null, () =>
                {
                    var index = -1;
                    fixture.Animations.DrawNativePlayback(sample.Item2, new Vector2(200f, 180f), 1f, seconds, clip, !sample.Item3, rate,
                        track => ++index <= face && !track.StartsWith("anim_eye", StringComparison.OrdinalIgnoreCase));
                    fixture.Animations.DrawNativePlantBlink(plant, sample.Item2, new Vector2(200f, 180f), 1f, seconds, clip, !sample.Item3, rate, null, 0f);
                    index = -1;
                    fixture.Animations.DrawNativePlayback(sample.Item2, new Vector2(200f, 180f), 1f, seconds, clip, !sample.Item3, rate,
                        track => ++index > face && !track.StartsWith("anim_eye", StringComparison.OrdinalIgnoreCase));
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), sample + " blink must be drawn at the face track before foreground leaves.");
            }
        }

        /// <summary>Checks the live boss renderer samples its original action clock rather than whole-level elapsed time.</summary>
        /// <returns>Source-art camera frames comparing distinct native action poses and a paused repeat.</returns>
        [UnityTest]
        public IEnumerator BossUsesOriginalActionMarkersAndItsOwnClock()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(50);
            var state = new AdventureBossState();
            state.Enter(80, "anim_spawn_2", 12f);
            state.AnimationSeconds = .4f;
            yield return fixture.Capture(null, () => fixture.Animations.DrawBossNative(Vector2.zero, 1f, 0f, false, state));
            var first = fixture.Pixels.GetPixels32();
            state.Enter(81, "anim_stomp_3", 12f);
            state.AnimationSeconds = 1.2f;
            yield return fixture.Capture(null, () => fixture.Animations.DrawBossNative(Vector2.zero, 1f, 0f, false, state));
            var next = fixture.Pixels.GetPixels32();
            Assert.That(next.Where((color, index) => !color.Equals(first[index])).Count(), Is.GreaterThan(100));
            yield return fixture.Capture(null, () => fixture.Animations.DrawBossNative(Vector2.zero, 1f, 0f, false, state));
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(next));
        }
    }
}
