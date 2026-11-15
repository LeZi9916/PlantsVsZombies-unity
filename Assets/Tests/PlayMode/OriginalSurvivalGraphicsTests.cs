#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks survival with the original PC artwork and the production chooser/presenter.</summary>
    public sealed class OriginalSurvivalGraphicsTests
    {
        /// <summary>Checks all fifteen cards request their original eleven-cell atlas or separate endless images.</summary>
        /// <returns>Original native menu render frames.</returns>
        [UnityTest]
        public IEnumerator MenuUsesOriginalSurvivalArtworkAndWidgets()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            var requested = new List<string>();
            var view = new ModeSelectionView(fixture.Root, (Func<string, SpriteImage?>)(name =>
            {
                requested.Add(name);
                return fixture.Owner.Textures.LoadImage(name);
            }), CanvasUI.DefaultFont) { CanLaunch = _ => true };
            view.Open(0);
            yield return fixture.Capture(null, view.Draw, wholeBoard: true);
            Assert.That(requested, Does.Contain("Survival_Thumbnails.jpg"));
            Assert.That(requested, Does.Not.Contain("Challenge_Thumbnails.jpg"));
            foreach (var name in new[] { "endless_day.jpg", "endless_night.jpg", "endless_fog.jpg", "endless_roof.jpg", "SeedChooser_Button2.png" })
            {
                Assert.That(requested, Does.Contain(name));
            }
            Assert.That(requested, Does.Not.Contain("Store_MainMenuButton.png"));
            Assert.That(requested.Count(name => name == "Survival_Thumbnails.jpg"), Is.EqualTo(11));
            var atlas = fixture.Owner.Textures.LoadImage("Survival_Thumbnails.jpg")!;
            var graphics = Resources.FindObjectsOfTypeAll<CanvasTextureGraphic>().Where(graphic => graphic.gameObject.activeInHierarchy
                && graphic.mainTexture == atlas.SourceTexture).ToArray();
            Assert.That(graphics.Length, Is.EqualTo(11));
            var destination = typeof(CanvasTextureGraphic).GetField("_destination", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var uv = typeof(CanvasTextureGraphic).GetField("_uv", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var positions = new[] { 51f, 206f, 361f, 516f, 671f };
            for (var image = 0; image <= 10; image++)
            {
                var expected = image < 10 ? new Rect(positions[image % 5], image < 5 ? 129 : 274, 80, 65)
                    : new Rect(361, 419, 80, 65);
                var graphic = graphics.Single(item =>
                {
                    var bounds = (Rect)destination.GetValue(item)!;
                    return Mathf.Abs(bounds.x - expected.x) < .001f && Mathf.Abs(bounds.y - expected.y) < .001f;
                });
                var source = (Rect)uv.GetValue(graphic)!;
                Assert.That(source.x, Is.EqualTo(image / 11f).Within(.00001f));
                Assert.That(source.width, Is.EqualTo(1f / 11f).Within(.00001f));
            }
            Save(fixture, "survival-menu");
            Assert.That(fixture.Pixels.GetPixel(fixture.Target.width / 2, fixture.Target.height / 2).a, Is.GreaterThan(.9f));
        }

        /// <summary>Checks native locks and press/release semantics without the prototype lock dialog.</summary>
        /// <returns>One real Canvas rendering frame.</returns>
        [UnityTest]
        public IEnumerator LockedCardsAndReleaseSemanticsMatchPcDirectory()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            var launched = 0;
            var requested = new List<string>();
            var view = new ModeSelectionView(fixture.Root, (Func<string, SpriteImage?>)(name =>
            {
                requested.Add(name);
                return fixture.Owner.Textures.LoadImage(name);
            }), CanvasUI.DefaultFont)
            {
                CanLaunch = id => id <= 3,
                AccomplishmentsNeededLookup = id => id == 4 ? 1 : 2,
                LaunchRequested = id => { launched = id; return true; }
            };
            view.Open(0);
            yield return fixture.Capture(null, view.Draw, wholeBoard: true);
            Assert.That(requested, Does.Contain("Challenge_Blank.png"));
            Assert.That(requested, Does.Contain("lock.png"));
            view.HandleClick(new Vector2(38 + 3 * 155 + 52, 150));
            Assert.That(launched, Is.Zero);
            view.HandlePointer(new Vector2(90, 150), PointerPhase.Down);
            Assert.That(launched, Is.Zero);
            view.HandlePointer(new Vector2(90, 150), PointerPhase.Up);
            Assert.That(launched, Is.EqualTo(1));
            launched = 0;
            view.HandlePointer(new Vector2(90, 150), PointerPhase.Down);
            view.HandlePointer(new Vector2(-1, -1), PointerPhase.Up);
            Assert.That(launched, Is.Zero);
            view.HandleClick(new Vector2(220, 570));
            Assert.That(view.IsSurvivalPage, Is.True, "PC 439A70 hides page buttons on survival.");
        }

        /// <summary>Checks the production opening starts with the native home offset and an empty first-stage chooser.</summary>
        /// <returns>Native board and chooser rendering frames.</returns>
        [UnityTest]
        public IEnumerator FirstSurvivalSelectionUsesNativePreviewAndEmptyBank()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            Prepare(fixture, 1);
            var opening = fixture.Owner.Opening;
            Assert.That(opening.AdventureIntroActive, Is.True);
            Assert.That(opening.RequiredAdventureIntroFrame.BoardX, Is.EqualTo(220));
            Assert.That(fixture.Owner.RequiredPendingSeeds.Count, Is.Zero);
            yield return fixture.Capture("DrawAdventureBoard", () => fixture.DrawPass("DrawModesInterface"), wholeBoard: true);
            WaitForChooser(fixture.Owner);
            yield return fixture.Capture("DrawAdventureBoard", () => fixture.DrawPass("DrawModesInterface"), wholeBoard: true);
            Assert.That(opening.RequiredAdventureIntroFrame.BoardX, Is.EqualTo(-380));
            Assert.That(opening.RequiredAdventureIntro.CanViewLawn, Is.False);
            Save(fixture, "survival-first-chooser");
            var first = opening.AdventureCampaignChooserRect(PlantKind.Peashooter);
            Assert.That(first, Is.EqualTo(new Rect(22, 123, 50, 70)), "PC 499F40 uses expanded rows after adventure completion.");
            Assert.That(fixture.Owner.Layout.AdventureSeedBankOrigin.y, Is.Zero);
            var before = fixture.Owner.RequiredGame.Elapsed;
            fixture.Owner.Opening._adventureIntroDrawn = true;
            opening.UpdateAdventureOpening(.1f);
            Assert.That(fixture.Owner.RequiredGame.Elapsed, Is.EqualTo(before));
        }

        /// <summary>Checks all survival terrains enter the original preview and re-selection context.</summary>
        /// <returns>Native opening frames for day, night, pool, fog and roof.</returns>
        [UnityTest]
        public IEnumerator EverySurvivalIdentifierUsesItsOriginalTerrainAndReSelection()
        {
            for (var id = 1; id <= 15; id++)
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(ModeCatalog.Get(id).BoardLevel);
                Prepare(fixture, id);
                var session = fixture.Owner.IndependentModes.RequiredModeSession;
                Assert.That((int)fixture.Owner.RequiredGame.Adventure.Level.Terrain, Is.EqualTo((id - 1) % 5));
                session.Stage = 2;
                fixture.Owner.Opening.InitializeAdventureOpening();
                WaitForChooser(fixture.Owner);
                Assert.That(fixture.Owner.Opening.RequiredAdventureIntro.CanViewLawn, Is.True, "Mode " + id);
                Assert.That(fixture.Owner.RequiredPendingSeeds.Count, Is.EqualTo(session.Game.Adventure.SelectedSeeds.Count));
                var seedCount = fixture.Owner.RequiredPendingSeeds.Count;
                var sun = session.Game.SunAmount;
                Assert.That(fixture.Owner.IndependentModes.TryHandleModeClick(new Vector2(60, 570)), Is.True);
                Assert.That(fixture.Owner.Opening.RequiredAdventureIntro.ViewingLawn, Is.True);
                for (var tick = 0; tick < 110; tick++)
                {
                    fixture.Owner.Opening.RequiredAdventureIntro.Tick();
                }
                fixture.Owner.Opening._adventureIntroFrame = fixture.Owner.Opening.RequiredAdventureIntro.Frame;
                yield return fixture.Capture("DrawAdventureBoard", () => fixture.DrawPass("DrawModesInterface"), wholeBoard: true);
                Assert.That(fixture.Owner.RequiredPendingSeeds.Count, Is.EqualTo(seedCount));
                Assert.That(session.Game.SunAmount, Is.EqualTo(sun));
                Save(fixture, "survival-view-lawn-" + id);
                fixture.Owner.Opening.RequiredAdventureIntro.CancelLawnView();
                while (fixture.Owner.Opening.RequiredAdventureIntro.ViewingLawn)
                {
                    fixture.Owner.Opening.RequiredAdventureIntro.Tick();
                }
                Assert.That(fixture.Owner.Opening.AdventureIntroAwaitingSeeds, Is.True);
            }
        }

        /// <summary>Checks the live endless HUD displays original progress art and its lower-right area cannot activate the hidden prototype exit.</summary>
        /// <returns>Original-resource HUD readback frames and native menu interactions.</returns>
        [UnityTest]
        public IEnumerator EndlessProgressIsVisibleAndItsCornerDoesNotExit()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(28);
            var owner = fixture.Owner;
            var session = new ModeSession(13);
            Assert.That(session.ChooseSeeds(session.Game.Adventure.SelectedSeeds), Is.True);
            owner._game = session.Game;
            owner._currentView = PvZGame.ScreenView.Board;
            owner.IndependentModes._modeSession = session;
            owner._deck.Clear();
            owner._deck.AddRange(session.Game.Adventure.SelectedSeeds);
            owner._selected = -1;
            owner._presenter = (GamePresenter)typeof(OriginalStormGraphicsTests.StormFixture)
                .GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture)!;
            session.Game.ResetAdventureWaveSchedule(100000, false, false);
            session.Game.Adventure.CurrentWave = 1;
            for (var tick = 0; tick < 20; tick++)
            {
                session.Update(.01f);
            }
            Assert.That(session.Game.Presentation.ProgressMeter.Visible, Is.True);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawModesInterface"));
            var meter = owner.Textures.RequiredLoadImage("FlagMeter.png");
            Assert.That(Resources.FindObjectsOfTypeAll<CanvasTextureGraphic>().Any(graphic =>
                graphic.gameObject.activeInHierarchy && graphic.mainTexture == meter.SourceTexture), Is.True,
                "The production endless HUD must submit the original FlagMeter, not only a caption.");
            Save(fixture, "survival-endless-progress");
            Assert.That(owner.IndependentModes.TryHandleModeClick(new Vector2(700f, 583f)), Is.True);
            Assert.That(owner._currentView, Is.EqualTo(PvZGame.ScreenView.Board));
            Assert.That(owner._game, Is.SameAs(session.Game));
            Assert.That(session.Phase, Is.EqualTo(ModePhase.Playing));
            Assert.That(owner.IndependentModes.TryHandleModeClick(AdventureBoardController.s_adventureMenuButton.center), Is.True);
            Assert.That(owner.RequiredOptionsView.IsOpen, Is.True, "The original menu remains the route for leaving a live survival board.");
            Assert.That(owner.IndependentModes.TryHandleModeClick(new Vector2(700f, 583f)), Is.True);
            Assert.That(owner._currentView, Is.EqualTo(PvZGame.ScreenView.Board));
        }

        /// <summary>Saves optional real-GPU captures into the explicitly selected ignored output directory.</summary>
        /// <param name="fixture">The captured production presenter.</param>
        /// <param name="name">The original-state capture label.</param>
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

        /// <summary>Creates a real standalone survival session without profiles or persistent stores.</summary>
        /// <param name="fixture">The original-resource production presenter fixture.</param>
        /// <param name="id">The original survival mode.</param>
        private static void Prepare(OriginalStormGraphicsTests.StormFixture fixture, int id)
        {
            var owner = fixture.Owner;
            owner._presenter = (GamePresenter)typeof(OriginalStormGraphicsTests.StormFixture)
                .GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture)!;
            owner.Dave.Initialize(owner.gameObject.AddComponent<AudioSource>(), 0f);
            owner.Opening.PlayOpeningInIsolatedSession = true;
            owner._currentView = PvZGame.ScreenView.Board;
            var session = new ModeSession(id, packetSlots: 6, roofCleaners: true, poolCleaners: true,
                waveContext: new ModeWaveContext(7529, profileProgress: 1, profileLevel: 50));
            owner.IndependentModes._modeSession = session;
            owner.Campaign.AttachAdventureGame(session.Game);
        }

        /// <summary>Advances the production opening clock to the native chooser without simulating a match.</summary>
        /// <param name="owner">The isolated production host.</param>
        private static void WaitForChooser(PvZGame owner)
        {
            for (var i = 0; i < 1000 && !owner.Opening.AdventureIntroAwaitingSeeds; i++)
            {
                owner.Opening.RequiredAdventureIntro.Tick();
            }
            owner.Opening._adventureIntroFrame = owner.Opening.RequiredAdventureIntro.Frame;
            Assert.That(owner.Opening.AdventureIntroAwaitingSeeds, Is.True);
        }
    }
}
