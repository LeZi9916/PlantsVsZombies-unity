#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original mushroom sleep artwork, free Z animation, crater atlases and native chooser warnings.</summary>
    public sealed class OriginalMushroomGraphicsTests
    {
        /// <summary>Checks all ten source sleeping bodies and independently placed original Z artwork, including roof and small bodies.</summary>
        /// <returns>Real camera frame boundaries comparing original source compositions.</returns>
        [UnityTest]
        public IEnumerator SleepingBodiesAndZUseTheOriginalClipsAndIndependentOffsets()
        {
            var samples = new[] { (PlantKind.PuffShroom, "PuffShroom", 9, 1f),
                (PlantKind.SunShroom, "SunShroom", 9, 1f), (PlantKind.FumeShroom, "FumeShroom", 9, 1f),
                (PlantKind.HypnoShroom, "HypnoShroom", 9, 1f), (PlantKind.ScaredyShroom, "ScaredyShroom", 9, 1f),
                (PlantKind.IceShroom, "IceShroom", 9, 1f), (PlantKind.DoomShroom, "DoomShroom", 9, 1f),
                (PlantKind.SeaShroom, "SeaShroom", 29, 1f), (PlantKind.MagnetShroom, "MagnetShroom", 9, 1f),
                (PlantKind.GloomShroom, "GloomShroom", 9, 1f), (PlantKind.PuffShroom, "PuffShroom", 49, .5f) };
            foreach (var sample in samples)
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(sample.Item3);
                SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
                var plant = new Plant { Id = 91, Kind = sample.Item1, Row = 1, Column = 2, Asleep = true,
                    Health = 300f, Age = .37f, PresentationRate = 12f, IsSmall = sample.Item4 < 1f,
                    SleepingPresentation = new AdventureSleepingState { Rate = 7f, AnimationSeconds = .4f } };
                fixture.Owner.RequiredGame.Plants.Clear();
                fixture.Owner.RequiredGame.Plants.Add(plant);
                if (sample.Item3 == 49)
                {
                    fixture.Owner.RequiredGame.Plants.Add(new Plant { Kind = PlantKind.FlowerPot,
                        Layer = PlantLayer.Support, Row = 1, Column = 2, Health = 300f });
                }

                yield return fixture.Capture(null, () =>
                {
                    fixture.DrawPass("DrawAdventurePlant", plant);
                    fixture.DrawPass("DrawAdventureSleepingPlant", plant);
                });
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    var cellY = sample.Item3 == 49 ? 215f : sample.Item3 == 29 ? 165f : 180f;
                    var bodyOffset = sample.Item1 == PlantKind.PuffShroom ? 5f
                        : sample.Item1 == PlantKind.ScaredyShroom ? -14f : sample.Item1 == PlantKind.SeaShroom ? 28f : 0f;
                    if (sample.Item3 == 49)
                    {
                        bodyOffset -= 5f;
                    }

                    fixture.Animations.DrawNativePlayback(sample.Item2,
                        new Vector2(200f + 40f * (1f - sample.Item4), cellY + bodyOffset + 80f * (1f - sample.Item4)),
                        sample.Item4, .37f, "anim_sleep", true, 12f);
                    var zX = sample.Item1 == PlantKind.FumeShroom ? 262f : 250f;
                    var zY = cellY + (sample.Item1 == PlantKind.ScaredyShroom ? 20f : sample.Item1 == PlantKind.GloomShroom ? 28f : 40f);
                    // PC 46CB90 creates reanimation 68 (Z), scale one, independently of the body's pot/small offsets.
                    fixture.Animations.DrawNativePlayback("Z", new Vector2(zX, zY), 1f, .4f, null, true, 7f);
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), sample.Item1 + " original sleeping composition");
            }
        }

        /// <summary>Checks captured sleeping plants retain the free cell-positioned Z even after their body leaves normal plant order.</summary>
        /// <returns>Real board camera frames comparing the independently retained native sleeping animation.</returns>
        [UnityTest]
        public IEnumerator LiftedSleepingPlantKeepsItsOriginalFreeZOnTheBoard()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(9);
            fixture.Owner.RequiredGame.Plants.Clear();
            var plant = new Plant { Id = 91, Kind = PlantKind.PuffShroom, Row = 1, Column = 2,
                Asleep = true, BungeeState = 2, Health = 300f,
                SleepingPresentation = new AdventureSleepingState { Rate = 7f, AnimationSeconds = .4f } };
            fixture.Owner.RequiredGame.Plants.Add(plant);
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var actual = fixture.Pixels.GetPixels32();
            plant.Asleep = false;
            yield return fixture.Capture("DrawAdventureBoard", () =>
                fixture.Animations.DrawNativePlayback("Z", new Vector2(250f, 220f), 1f, .4f, null, true, 7f), true);
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
        }

        /// <summary>Checks terrain-specific crater cells, the ninety-second aging split and the last twenty-five-update alpha.</summary>
        /// <returns>Real camera frames comparing native top-left atlas composition.</returns>
        [UnityTest]
        public IEnumerator CratersUseOriginalGroundWaterAndRoofArtworkAndFade()
        {
            foreach (var sample in new[] { (9, 1, 2, 180f), (19, 1, 2, 89f), (29, 2, 2, 180f),
                (39, 2, 2, 89f), (49, 1, 2, 180f), (49, 1, 7, .12f), (50, 1, 2, 89f) })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(sample.Item1);
                fixture.Owner.RequiredGame.Adventure._ticks = 25;
                var crater = new AdventureCellTimer { Row = sample.Item2, Column = sample.Item3, Remaining = sample.Item4 };
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureCrater", crater));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    var water = sample.Item1 == 29 || sample.Item1 == 39;
                    var roof = sample.Item1 == 49 || sample.Item1 == 50;
                    var old = sample.Item4 < 90f;
                    var x = 32f + sample.Item3 * 80f;
                    var y = 120f + sample.Item2 * (water || roof ? 85f : 100f);
                    var image = sample.Item1 == 19 ? old ? "crater_fading.png" : "crater.png"
                        : old ? "crater_fading.png" : "crater.png";
                    var cel = sample.Item1 == 19 ? 1 : 0;
                    if (water)
                    {
                        image = sample.Item1 == 39 ? "crater_water_night.png" : "crater_water_day.png";
                        cel = old ? 1 : 0;
                        y += (float)Math.Sin(sample.Item2 * Mathf.PI + sample.Item3 * Mathf.PI * .25f + 25 * Mathf.PI * 2f / 200f) * 2f;
                    }
                    else if (roof)
                    {
                        var left = sample.Item3 < 5;
                        image = left ? "crater_roof_left.png" : "crater_roof_center.png";
                        x += left ? 16f : 18f;
                        y += Math.Max(0, 5 - sample.Item3) * 20f - 10f + (left ? -16f : -9f);
                        cel = old ? 1 : 0;
                    }

                    var texture = fixture.Owner.Textures.LoadImage(image)!;
                    var color = CanvasUI.Color;
                    CanvasUI.Color = new Color(1f, 1f, 1f, sample.Item4 < .25f ? 122f / 255f : 1f);
                    fixture.Animations.DrawNativeImage(texture, new Rect(x, y, texture.Width * .5f, texture.Height), new Rect(cel * .5f, 0f, .5f, 1f));
                    CanvasUI.Color = color;
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Original crater " + sample);
            }
        }

        /// <summary>Checks real chooser hover routes and original bitmap-font warning text, including selected coffee and Last Stand.</summary>
        /// <returns>Real source-font camera frames comparing expected native warning requests.</returns>
        [UnityTest]
        public IEnumerator ChooserHoverDisplaysOriginalWarningsAndCoffeeSuppressesOnlySleepAdvice()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            fixture.Owner._currentView = PvZGame.ScreenView.Board;
            fixture.Owner._choosingSeeds = true;
            fixture.Owner._pendingSeeds = new AdventureSeedChooserState(Array.Empty<PlantKind>());
            foreach (var sample in new[] { (PlantKind.PuffShroom, false, "NOCTURNAL_WARNING"),
                (PlantKind.PuffShroom, true, ""), (PlantKind.LilyPad, true, "NOT_RECOMMENDED_FOR_LEVEL") })
            {
                fixture.Owner._pendingSeeds = new AdventureSeedChooserState(sample.Item2 ? new[] { PlantKind.CoffeeBean } : Array.Empty<PlantKind>());
                var card = fixture.Owner.Opening.AdventureCampaignChooserRect(sample.Item1);
                fixture.Owner.AdventureBoard._adventurePointer = card.center;
                yield return fixture.Capture("DrawOriginalHoverTips");
                var actual = fixture.Pixels.GetPixels32();
                Assert.That(fixture.Owner.RequiredOriginalTooltip.LastError, Is.Null);
                Assert.That(fixture.Owner.RequiredOriginalTooltip.RequiredLastLayout.RequiredWarning.Glyphs.Count > 0, Is.EqualTo(sample.Item3.Length > 0));
                yield return fixture.Capture(null, () =>
                {
                    var key = sample.Item1 == PlantKind.PuffShroom ? "PUFF_SHROOM" : "LILY_PAD";
                    var text = new GamePresenter(fixture.Owner);
                    fixture.DrawPass("DrawOriginalTooltipRequest", new OriginalTooltipRequest
                    {
                        Title = text.OriginalProgressionText(key), Body = text.OriginalProgressionText(key + "_TOOLTIP"),
                        Warning = sample.Item3.Length == 0 ? "" : text.OriginalProgressionText(sample.Item3),
                        X = (int)card.center.x, Y = (int)card.yMax + 7, Centered = true, UseSeedChooserLineWidth = true
                    });
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
            }

            // Selected chooser-bank cards retain chooser descriptions and warnings instead of combat availability advice.
            fixture.Owner._pendingSeeds = new AdventureSeedChooserState(new[] { PlantKind.PuffShroom });
            var bank = fixture.Owner.Layout.AdventureSeedBankOrigin;
            fixture.Owner.AdventureBoard._adventurePointer = new Vector2(bank.x + AdventureSeedSlotLayout.PositionX(0, fixture.Owner.Layout.SeedBankCapacity) + 25f, bank.y + 43f);
            yield return fixture.Capture("DrawOriginalHoverTips");
            Assert.That(fixture.Owner.RequiredOriginalTooltip.RequiredLastLayout.BodyLines.Count, Is.GreaterThan(0));
            Assert.That(fixture.Owner.RequiredOriginalTooltip.RequiredLastLayout.RequiredWarning.Glyphs.Count, Is.GreaterThan(0));

            var definition = ModeCatalog.All.First(mode => mode.Mechanic == ModeMechanic.LastStand);
            var session = new ModeSession(definition.Id, randomSeed: 7529);
            fixture.Owner.IndependentModes._modeSession = session;
            fixture.Owner._game = session.Game;
            fixture.Owner._pendingSeeds = new AdventureSeedChooserState(Array.Empty<PlantKind>());
            var index = fixture.Owner._game.Adventure.AvailableSeeds.ToList().IndexOf(PlantKind.PuffShroom);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            fixture.Owner.AdventureBoard._adventurePointer = BoardLayout.AdventureChooserRect(index).center;
            yield return fixture.Capture("DrawOriginalHoverTips");
            Assert.That(fixture.Owner.RequiredOriginalTooltip.LastError, Is.Null);
            Assert.That(fixture.Owner.RequiredOriginalTooltip.RequiredLastLayout.RequiredWarning.Glyphs.Count, Is.GreaterThan(0));
        }
    }
}
