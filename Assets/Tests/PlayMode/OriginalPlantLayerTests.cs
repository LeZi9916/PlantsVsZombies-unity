#nullable enable

using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares original pumpkin groups and global magnetic-metal layers with independent native-art composition.</summary>
    public sealed class OriginalPlantLayerTests
    {
        /// <summary>Checks the back group behind the contained plant, the front group above it, and original damage cels.</summary>
        /// <returns>Real original-art camera frame boundaries.</returns>
        [UnityTest]
        public IEnumerator PumpkinBackAndFrontEncloseThePlantWithoutCoveringItsBody()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(11);
            var game = fixture.Owner.RequiredGame;
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow2.png", null!);
            var place = typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var main = (Plant)place.Invoke(game, new object[] { PlantKind.TallNut, 2, 3 })!;
            var shell = (Plant)place.Invoke(game, new object[] { PlantKind.Pumpkin, 2, 3 })!;
            var origin = new Vector2(280f, 280f);
            var shellOrigin = origin + new Vector2(0f, 15f);
            var images = new string?[] { null, "IMAGE_REANIM_PUMPKIN_DAMAGE1", "IMAGE_REANIM_PUMPKIN_DAMAGE3" };
            var health = new[] { 4000f, 2665f, 1332f };
            for (var stage = 0; stage < health.Length; stage++)
            {
                shell.Health = health[stage];
                yield return fixture.Capture(null, () =>
                {
                    fixture.DrawPass("DrawAdventurePlant", main);
                    fixture.DrawPass("DrawAdventurePlant", shell);
                });
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    fixture.Animations.DrawNativePlayback("Pumpkin", shellOrigin, 1f, 0f, "anim_idle", true, 12f,
                        track => string.Equals(track, "Pumpkin_back", StringComparison.OrdinalIgnoreCase));
                    fixture.Animations.DrawNativePlayback("Tallnut", origin, 1f, 0f, "anim_idle", true, 12f);
                    fixture.Animations.DrawNativePlayback("Pumpkin", shellOrigin, 1f, 0f, "anim_idle", true, 12f,
                        track => !string.Equals(track, "Pumpkin_back", StringComparison.OrdinalIgnoreCase), "Pumpkin_front", images[stage]);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Native Pumpkin_back must precede the protected plant; damage only replaces Pumpkin_front.");
            }

            game.Plants.Remove(main);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventurePlant", shell));
            var empty = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () => fixture.Animations.DrawNativePlayback("Pumpkin", shellOrigin, 1f, 0f,
                "anim_idle", true, 12f, overrideTrack: "Pumpkin_front", overrideImage: images[2]));
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(empty), "An empty shell must still draw both original halves exactly once.");
        }

        /// <summary>Checks every original retained metal type is locally drawn after its Magnet-shroom on land and water.</summary>
        /// <returns>Original-art frames comparing all metal cels with the native body-then-item composition.</returns>
        [UnityTest]
        public IEnumerator AllHeldMagneticItemsUseLocalForegroundOnLandAndWater()
        {
            foreach (var level in new[] { 11, 31 })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(level);
                var game = fixture.Owner.RequiredGame;
                SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
                SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow2.png", null!);
                var place = typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
                if (level == 31)
                {
                    place.Invoke(game, new object[] { PlantKind.LilyPad, 2, 3 });
                }

                var plant = (Plant)place.Invoke(game, new object[] { PlantKind.MagnetShroom, 2, 3 })!;
                var state = new AdventureMagnetState { NativePhase = 27, AnimationSeconds = .5f, AnimationRate = 2f, BlendTicks = 0 };
                plant.MagnetPresentation = state;
                game.Adventure._ticks = 37;
                var nativeY = level == 31 ? 250f : 280f;
                var height = AdventurePresentationRules.WaterBobbingOffset(PlantKind.MagnetShroom, 2, 3, level == 31, 37);
                foreach (var kind in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 20, 21 })
                {
                    var destinationX = kind <= 3 ? 25f : kind <= 6 ? 20f : kind <= 9 || kind >= 14 && kind <= 16 ? 30f
                        : kind <= 12 ? 44f : kind == 13 ? 20f : kind == 20 ? 10f : 45f;
                    var destinationY = kind <= 6 ? 20f : kind <= 9 || kind >= 14 && kind <= 16 || kind == 20 ? 0f
                        : kind <= 12 ? 10f : 15f;
                    state.Item = new AdventureMagnetItem
                    {
                        NativeKind = kind, AgeTicks = 500, PositionResolved = true,
                        X = 280f + destinationX, Y = nativeY + destinationY,
                        DestinationX = destinationX, DestinationY = destinationY
                    };
                    yield return fixture.Capture(null, () =>
                    {
                        fixture.DrawPass("DrawAdventurePlant", plant);
                        fixture.DrawPass("DrawAdventureMagnetItemsOnTop");
                    });
                    var actual = fixture.Pixels.GetPixels32();
                    yield return fixture.Capture(null, () =>
                    {
                        fixture.Animations.DrawNativePlayback("MagnetShroom", new Vector2(280f, nativeY + height),
                            1f, .5f, "anim_nonactive_idle2", true, 2f);
                        var resource = AdventureMagnetRules.Image(kind);
                        Assert.That(fixture.Animations.TryGetNativeSpriteSize(resource, out var size), Is.True);
                        fixture.Animations.DrawResourceSprite(resource,
                            new Vector2(280f + destinationX + size.x * .5f, nativeY + destinationY + height + size.y * .5f),
                            .8f, frame: kind >= 10 && kind <= 12 ? kind - 10 : 0);
                    });
                    Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual),
                        "PC 47420B draws the body before 4744D3 draws held metal " + kind + " on adventure stage " + level);
                }
            }
        }

        /// <summary>Checks flying metal crosses a later zombie row at native Top layer 399999.</summary>
        /// <returns>Real production-board camera boundaries before reading overlap pixels.</returns>
        [UnityTest]
        public IEnumerator FlyingMetalDrawsAboveOtherRowsRatherThanOnlyAboveItsMagnet()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(11);
            var game = fixture.Owner.RequiredGame;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var magnet = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                .Invoke(game, new object[] { PlantKind.MagnetShroom, 0, 3 })!;
            typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                .Invoke(game, new object[] { ZombieKind.Normal, 3, 5.7f, 0 });
            var item = new AdventureMagnetItem { NativeKind = 1, X = 464f, Y = 300f, PositionResolved = true };
            var state = new AdventureMagnetState { Item = item };
            magnet.MagnetPresentation = state;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var actual = fixture.Pixels.GetPixels32();
            state.Item = null;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var without = fixture.Pixels.GetPixels32();
            yield return fixture.Capture("DrawAdventureBoard", () => fixture.Animations.DrawResourceSprite(
                "IMAGE_REANIM_ZOMBIE_BUCKET1", new Vector2(495f, 333.5f), .8f), true);
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "The native metal pass belongs above every row's ordinary zombies.");
            var changed = 0;
            for (var index = 0; index < actual.Length; index++)
            {
                if (Math.Abs(actual[index].r - without[index].r) + Math.Abs(actual[index].g - without[index].g)
                    + Math.Abs(actual[index].b - without[index].b) > 12)
                {
                    changed++;
                }
            }

            Assert.That(changed, Is.GreaterThan(100));
        }
    }
}
