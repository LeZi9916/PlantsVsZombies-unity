#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
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

    }
}
