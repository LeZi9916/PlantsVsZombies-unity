#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks PC 438EE0/439CB0 challenge cards using original artwork and a real Canvas.</summary>
    public sealed class OriginalChallengeDirectoryTests
    {
        /// <summary>Checks both four-row directories against the original PC atlas indices and card coordinates.</summary>
        /// <returns>The original-artwork rendering frames.</returns>
        [UnityTest]
        public IEnumerator MiniGamesAndPuzzlesUseNativeCardsAndRecords()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            foreach (var category in new[] { 1, 3 })
            {
                var requested = new List<string>();
                var view = new ModeSelectionView(fixture.Root, (Func<string, SpriteImage?>)(name =>
                {
                    requested.Add(name);
                    return fixture.Owner.Textures.LoadImage(name);
                }), CanvasUI.DefaultFont)
                {
                    CanLaunch = _ => true,
                    ProgressLookup = id => id == 60 || id == 70 ? 12 : 1
                };
                view.Open(category);
                yield return fixture.Capture(null, view.Draw, wholeBoard: true);
                Assert.That(requested.Count(name => name == "Challenge_Thumbnails.jpg"), Is.EqualTo(20));
                Assert.That(requested.Count(name => name == "MiniGame_trophy.png"), Is.EqualTo(category == 1 ? 20 : 18));
                Assert.That(requested, Does.Not.Contain("Store_MainMenuButton.png"));
                Assert.That(requested, Does.Not.Contain("Survival_Thumbnails.jpg"));
                var atlas = fixture.Owner.Textures.LoadImage("Challenge_Thumbnails.jpg")!;
                var graphics = Resources.FindObjectsOfTypeAll<CanvasTextureGraphic>().Where(item => item.gameObject.activeInHierarchy
                    && item.mainTexture == atlas.SourceTexture).ToArray();
                Assert.That(graphics.Length, Is.EqualTo(20));
                var destination = typeof(CanvasTextureGraphic).GetField("_destination", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var uv = typeof(CanvasTextureGraphic).GetField("_uv", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var indices = category == 1 ? new[] { 0, 6, 2, 3, 1, 8, 5, 7, 20, 12, 15, 4, 17, 18, 16, 21, 0, 6, 14, 19 }
                    : new[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11 };
                for (var card = 0; card < 20; card++)
                {
                    var x = 51 + 155 * (card % 5);
                    var y = 97 + 119 * (card / 5);
                    var graphic = graphics.Single(item =>
                    {
                        var bounds = (Rect)destination.GetValue(item)!;
                        return Mathf.Abs(bounds.x - x) < .001f && Mathf.Abs(bounds.y - y) < .001f;
                    });
                    var source = (Rect)uv.GetValue(graphic)!;
                    Assert.That(source.x, Is.EqualTo(indices[card] / 22f).Within(.00001f));
                    Assert.That(source.width, Is.EqualTo(1f / 22f).Within(.00001f));
                }
                Save(fixture, category == 1 ? "minigames-menu" : "puzzles-menu");
            }
        }

        /// <summary>Checks both groups of puzzle locks and enabled ButtonWidget release semantics.</summary>
        /// <returns>A real original-artwork rendering frame.</returns>
        [UnityTest]
        public IEnumerator PuzzleLocksKeepOriginalPreviewsAndGroupTooltips()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            var requested = new List<string>();
            var launched = 0;
            var view = new ModeSelectionView(fixture.Root, (Func<string, SpriteImage?>)(name =>
            {
                requested.Add(name);
                return fixture.Owner.Textures.LoadImage(name);
            }), CanvasUI.DefaultFont)
            {
                CanLaunch = id => id == 51 || id == 61,
                AccomplishmentsNeededLookup = id => ModeUnlockRules.DirectoryAccomplishmentsNeeded(id, false, _ => 0),
                LaunchRequested = id =>
                {
                    launched = id;
                    return true;
                }
            };
            view.Open(3);
            yield return fixture.Capture(null, view.Draw, wholeBoard: true);
            Assert.That(requested.Count(name => name == "Challenge_Thumbnails.jpg"), Is.EqualTo(4));
            Assert.That(requested.Count(name => name == "Challenge_Blank.png"), Is.EqualTo(16));
            Assert.That(requested.Count(name => name == "lock.png"), Is.EqualTo(2));
            view.HandleClick(new Vector2(245, 125));
            Assert.That(launched, Is.Zero);
            view.HandlePointer(new Vector2(90, 350), PointerPhase.Down);
            Assert.That(launched, Is.Zero);
            view.HandlePointer(new Vector2(90, 350), PointerPhase.Up);
            Assert.That(launched, Is.EqualTo(61));
            launched = 0;
            view.HandlePointer(new Vector2(90, 125), PointerPhase.Down);
            view.HandlePointer(new Vector2(245, 125), PointerPhase.Up);
            Assert.That(launched, Is.Zero);
            view.HandleClick(new Vector2(220, 570));
            view.HandleClick(new Vector2(90, 125));
            Assert.That(launched, Is.EqualTo(51), "PC normal directories hide debug page tabs.");
            Save(fixture, "puzzles-locked");
        }

        /// <summary>Preserves the existing external garden/tree entries without querying unsupported debug-mode unlock rules.</summary>
        /// <returns>One real original-resource directory frame.</returns>
        [UnityTest]
        public IEnumerator HiddenDebugPagePreservesOnlySupportedExternalEntries()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            var view = new ModeSelectionView(fixture.Root, (Func<string, SpriteImage?>)(name => fixture.Owner.Textures.LoadImage(name)), CanvasUI.DefaultFont)
            {
                CanLaunch = id => id == 43 || id == 50,
                AccomplishmentsNeededLookup = id => ModeUnlockRules.DirectoryAccomplishmentsNeeded(id, true, _ => 0)
            };
            view.Open(2);
            yield return fixture.Capture(null, view.Draw, wholeBoard: true);
        }

        /// <summary>Saves optional original-artwork captures outside the production assets.</summary>
        /// <param name="fixture">The real GPU capture fixture.</param>
        /// <param name="name">The capture label.</param>
        private static void Save(OriginalStormGraphicsTests.StormFixture fixture, string name)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), fixture.Pixels.EncodeToPNG());
        }
    }
}
