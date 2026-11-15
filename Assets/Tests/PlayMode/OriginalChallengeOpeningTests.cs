#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks all PC mini-game and puzzle intro branches through the production original-artwork presenter.</summary>
    public sealed class OriginalChallengeOpeningTests
    {
        /// <summary>Checks scrolling, selection, forced Starfruit, non-scrolling puzzles and original-sized widgets.</summary>
        /// <returns>Original board and chooser GPU frames for the forty requested challenges.</returns>
        [UnityTest]
        public IEnumerator AllMiniGamesAndPuzzlesUseNativeOpeningPredicates()
        {
            for (var id = 16; id <= 70; id++)
            {
                if (id == 36)
                {
                    id = 51;
                }
                using var fixture = new OriginalStormGraphicsTests.StormFixture(ModeCatalog.Get(id).BoardLevel);
                Prepare(fixture, id);
                var owner = fixture.Owner;
                var opening = owner.Opening;
                var plan = opening.RequiredAdventureIntro.Plan;
                var chooses = id == 16 || id == 22 || id == 28 || id == 29 || id == 31 || id == 32 || id == 34;
                Assert.That(plan.ChooseSeeds, Is.EqualTo(chooses), "Mode " + id);
                Assert.That(plan.NonScrolling, Is.EqualTo(id == 17 || id == 23 || id == 30 || id == 33 || id >= 51), "Mode " + id);
                Assert.That(plan.RequiredContext.NativeMode, Is.EqualTo(id));
                Assert.That(plan.RequiredContext.IsLittleTrouble, Is.EqualTo(id == 25));
                Assert.That(plan.RequiredContext.IsFinalBoss, Is.EqualTo(id == 35));
                Assert.That(owner.RequiredGame.Adventure.Boss, Is.Null);
                yield return fixture.Capture("DrawAdventureBoard", () => fixture.DrawPass("DrawModesInterface"), wholeBoard: true);
                if (chooses)
                {
                    WaitForChooser(owner);
                    Assert.That(owner.RequiredPendingSeeds.Count, Is.EqualTo(id == 22 ? 1 : 0), "PC 499170 preselects only Starfruit.");
                    if (id == 22)
                    {
                        Assert.That(owner.RequiredPendingSeeds[0], Is.EqualTo(PlantKind.Starfruit));
                    }
                    Assert.That(opening.RequiredAdventureIntroFrame.BoardX, Is.EqualTo(-380));
                    Assert.That(opening.RequiredAdventureIntro.CanViewLawn, Is.False);
                    Assert.That(opening.AdventureCampaignChooserRect(PlantKind.Peashooter), Is.EqualTo(new Rect(22, 123, 50, 70)));
                    Assert.That(opening.AdventureCampaignChooserRect(PlantKind.WinterMelon), Is.EqualTo(new Rect(234, 473, 50, 70)));
                    var time = owner.RequiredGame.Elapsed;
                    opening._adventureIntroDrawn = true;
                    opening.UpdateAdventureOpening(.1f);
                    Assert.That(owner.RequiredGame.Elapsed, Is.EqualTo(time));
                    if (id == 16 || id == 32)
                    {
                        Assert.That(opening._adventurePreview.Exists(z => z.NativeKind >= 26), Is.True);
                    }
                    yield return fixture.Capture("DrawAdventureBoard", () => fixture.DrawPass("DrawModesInterface"), wholeBoard: true);
                    Save(fixture, "chooser-" + id);
                }
                else
                {
                    Save(fixture, "preview-" + id);
                }
            }
        }

        /// <summary>Checks native 35 delays Zomboss allocation until the entrance boundary instead of drawing him in the street.</summary>
        /// <returns>Native boss preview and entrance rendering frames.</returns>
        [UnityTest]
        public IEnumerator BossRevengeAllocatesAtNativeEntranceBoundary()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(50);
            Prepare(fixture, 35);
            var owner = fixture.Owner;
            var clock = owner.Opening.RequiredAdventureIntro;
            while (clock.NativeTime < clock.Plan.BossEnterStart - 10)
            {
                clock.Tick(emit: owner.Opening.HandleAdventureIntroEvent);
            }
            Assert.That(owner.RequiredGame.Adventure.Boss, Is.Null);
            Assert.That(owner.Opening._adventurePreview, Is.Empty);
            clock.Tick(emit: owner.Opening.HandleAdventureIntroEvent);
            Assert.That(owner.RequiredGame.Adventure.Boss!.Phase, Is.EqualTo(78));
            Assert.That(owner.RequiredGame.Adventure.Boss!.Animation, Is.EqualTo("anim_enter"));
            owner.Opening._adventureIntroFrame = clock.Frame;
            yield return fixture.Capture("DrawAdventureBoard", () => fixture.DrawPass("DrawModesInterface"), wholeBoard: true);
            Save(fixture, "boss-revenge-enter");
        }

        /// <summary>Checks PC 498920's independent copy dialog and native Last Stand disabled-card input.</summary>
        /// <returns>Native chooser and Imitater modal GPU frames.</returns>
        [UnityTest]
        public IEnumerator ImitaterUsesNativeIndependentDialogAndDisabledTargets()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(28);
            Prepare(fixture, 31, AdventureSeedAccess.DevelopmentAll());
            var owner = fixture.Owner;
            WaitForChooser(owner);
            owner.IndependentModes.TryHandleModeClick(owner.Opening.AdventureCampaignChooserRect(PlantKind.PuffShroom).center);
            Assert.That(owner.RequiredPendingSeeds.Count, Is.Zero);
            owner.IndependentModes.TryHandleModeClick(owner.Opening.AdventureCampaignChooserRect(PlantKind.Imitater).center);
            Assert.That(owner.IndependentModes.HasChooserModal, Is.True);
            var dialog = owner._presenter.RequiredModeChooserDialog;
            Assert.That(dialog.CardRect(PlantKind.Peashooter).x, Is.EqualTo(dialog.Bounds.x + (int)dialog.Bounds.width / 2 - 210));
            Assert.That(dialog.CardRect(PlantKind.MelonPult).y, Is.EqualTo(dialog.Bounds.y + 398));
            yield return fixture.Capture("DrawAdventureBoard", () =>
            {
                fixture.DrawPass("DrawModesInterface");
                fixture.DrawPass("DrawModeChooserDialog");
            }, wholeBoard: true);
            Save(fixture, "imitater-dialog");
            owner.IndependentModes.TryHandleModeClick(dialog.CardRect(PlantKind.Sunflower).center);
            Assert.That(owner.IndependentModes.HasChooserModal, Is.True);
            Assert.That(owner.RequiredPendingSeeds.Count, Is.Zero);
            owner.IndependentModes.TryHandleModeClick(dialog.CardRect(PlantKind.Peashooter).center);
            Assert.That(owner.IndependentModes.HasChooserModal, Is.False);
            Assert.That(owner.RequiredPendingSeeds[0], Is.EqualTo(PlantKind.Imitater));
            Assert.That(owner.RequiredGame.Adventure.ImitaterTarget, Is.EqualTo(PlantKind.Peashooter));
            Assert.That(owner.RequiredPendingSeeds.HasFlyingSeeds, Is.True);
        }

        /// <summary>Checks the original re-pick/yes continuation leaves the bank intact and commits only after confirmation.</summary>
        /// <returns>The actual original warning dialog GPU frame.</returns>
        [UnityTest]
        public IEnumerator NativeWarningCanRepickOrContinueWithoutLosingSelection()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            Prepare(fixture, 16);
            var owner = fixture.Owner;
            WaitForChooser(owner);
            foreach (var seed in new[] { PlantKind.Peashooter, PlantKind.Wallnut, PlantKind.PotatoMine, PlantKind.CherryBomb, PlantKind.SnowPea, PlantKind.Chomper })
            {
                owner.IndependentModes.TryHandleModeClick(owner.Opening.AdventureCampaignChooserRect(seed).center);
                owner.RequiredPendingSeeds.LandFlyingSeeds();
            }
            owner.IndependentModes.TryHandleModeClick(owner.Opening.AdventureChooserStartRect.center);
            Assert.That(owner.IndependentModes.ChooserWarning?.Key, Is.EqualTo("SEED_CHOOSER_SUN_WARNING"));
            Assert.That(owner.IndependentModes.RequiredModeSession.Phase, Is.EqualTo(ModePhase.ChoosingSeeds));
            yield return fixture.Capture("DrawAdventureBoard", () =>
            {
                fixture.DrawPass("DrawModesInterface");
                fixture.DrawPass("DrawModeChooserDialog");
            }, wholeBoard: true);
            Save(fixture, "chooser-warning");
            owner.IndependentModes.TryHandleModeClick(owner._presenter.RequiredModeChooserDialog.NoBounds.center);
            Assert.That(owner.IndependentModes.HasChooserModal, Is.False);
            Assert.That(owner.RequiredPendingSeeds.Count, Is.EqualTo(6));
            owner.IndependentModes.TryHandleModeClick(owner.Opening.AdventureChooserStartRect.center);
            owner.IndependentModes.TryHandleModeClick(owner._presenter.RequiredModeChooserDialog.YesBounds.center);
            Assert.That(owner.IndependentModes.RequiredModeSession.Phase, Is.EqualTo(ModePhase.Playing));
            Assert.That(owner.Opening.AdventureIntroAwaitingSeeds, Is.False);
            Assert.That(owner.RequiredGame.Adventure.SelectedSeeds.Count, Is.EqualTo(6));
        }

        /// <summary>Creates an isolated production host with the exact native mode metadata.</summary>
        /// <param name="fixture">The original-artwork presenter.</param>
        /// <param name="id">The original challenge identifier.</param>
        /// <param name="access">The original purchased seed entitlements.</param>
        private static void Prepare(OriginalStormGraphicsTests.StormFixture fixture, int id, AdventureSeedAccess? access = null)
        {
            var owner = fixture.Owner;
            owner._presenter = (GamePresenter)typeof(OriginalStormGraphicsTests.StormFixture)
                .GetField("_presenter", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture)!;
            owner.Dave.Initialize(owner.gameObject.AddComponent<AudioSource>(), 0f);
            owner.Opening.PlayOpeningInIsolatedSession = true;
            owner._currentView = PvZGame.ScreenView.Board;
            var session = new ModeSession(id, access, packetSlots: 6, roofCleaners: true, poolCleaners: true,
                waveContext: new ModeWaveContext(7529, profileProgress: 1, profileLevel: 50), deferBossEntrance: true);
            owner.IndependentModes._modeSession = session;
            owner.Campaign.AttachAdventureGame(session.Game);
        }

        /// <summary>Advances only CutScene until the seed chooser stops the intro clock.</summary>
        /// <param name="owner">The isolated original host.</param>
        private static void WaitForChooser(PvZGame owner)
        {
            for (var tick = 0; tick < 1000 && !owner.Opening.AdventureIntroAwaitingSeeds; tick++)
            {
                owner.Opening.RequiredAdventureIntro.Tick(emit: owner.Opening.HandleAdventureIntroEvent);
            }
            owner.Opening._adventureIntroFrame = owner.Opening.RequiredAdventureIntro.Frame;
            Assert.That(owner.Opening.AdventureIntroAwaitingSeeds, Is.True);
        }

        /// <summary>Saves optional native-resource GPU captures outside production assets.</summary>
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
