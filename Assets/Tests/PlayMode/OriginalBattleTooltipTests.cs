#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks production seed tooltip content in combat and in the seed chooser.</summary>
    public sealed class OriginalBattleTooltipTests
    {
        /// <summary>Checks original board advice retains its remaining lifetime throughout a gameplay pause.</summary>
        [Test]
        public void BoardPausePreservesAdviceLifetime()
        {
            var host = new GameObject("Inactive original advice pause fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            try
            {
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
                owner._currentView = PvZGame.ScreenView.Board;
                owner.AdventureBoard._adventureFeedback = "fixture";
                owner.AdventureBoard._adventureFeedbackUntil = Time.unscaledTime + 5f;
                var until = owner.AdventureBoard._adventureFeedbackUntil;
                owner._game.TogglePause();
                owner.AdventureBoard.PreserveAdventureFeedbackDuringPause(10f);
                Assert.That(owner.AdventureBoard._adventureFeedbackUntil, Is.EqualTo(until + 10f).Within(.001f));
                owner._game.TogglePause();
                owner.AdventureBoard.PreserveAdventureFeedbackDuringPause(1f);
                Assert.That(owner.AdventureBoard._adventureFeedbackUntil, Is.EqualTo(until + 10f).Within(.001f),
                    "Advice resumes its ordinary countdown after gameplay resumes.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        /// <summary>Checks original flag placeholders and preserves runtime diagnostic text at the presentation boundary.</summary>
        /// <returns>A frame boundary allowing an isolated inactive host to be created safely.</returns>
        [UnityTest]
        public IEnumerator OriginalModeAdviceResolvesFlagsAndPreservesDiagnostics()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "properties", "LawnStrings.txt")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original advice resolution fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var presenter = new GamePresenter(owner);
            try
            {
                owner._root = root;
                foreach (var id in new[] { 1, 6, 31 })
                {
                    owner.IndependentModes._modeSession = new ModeSession(id);
                    owner._game = owner.IndependentModes._modeSession.Game;
                    var flags = id == 6 ? "10" : "5";
                    var original = presenter.OriginalProgressionText("ADVICE_SURVIVE_FLAGS");
                    Assert.That(presenter.ResolveAdventureAdvice(owner.IndependentModes._modeSession.Advice), Is.EqualTo(original.Replace("{FLAGS}", flags)));
                }

                const string diagnostic = "Storage failure: isolated regression fixture";
                Assert.That(presenter.ResolveAdventureAdvice(diagnostic), Is.EqualTo(diagnostic));
                Assert.That(presenter.ResolveAdventureAdvice("[MISSING_ORIGINAL_ADVICE_KEY]"), Is.Empty);
                Assert.That(presenter.ResolveAdventureAdvice(null), Is.Empty);
            }
            finally
            {
                presenter.Dispose();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Checks that combat hides plant descriptions while retaining original names and availability warnings.</summary>
        /// <returns>A frame boundary allowing Canvas and native bitmap font resources to initialize.</returns>
        [UnityTest]
        public IEnumerator CombatHidesDescriptionAndChooserKeepsOriginalDescription()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!File.Exists(Path.Combine(root, "properties", "LawnStrings.txt")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            var host = new GameObject("Inactive original seed tooltip fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var presenter = new GamePresenter(owner);
            try
            {
                owner._root = root;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                Assert.That(owner._game.AdventureSunAvailable, Is.LessThan(owner._game.AdventureSeedCost(PlantKind.Peashooter)));
                var draw = typeof(GamePresenter).GetMethod("DrawOriginalPlantTooltip", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ObjectSpriteRenderer.BeginSceneFrame();
                CanvasUI.BeginFrame();
                try
                {
                    draw.Invoke(presenter, new object[] { PlantKind.Peashooter, new Rect(79, 6, 50, 70), false });
                    var tooltip = owner.RequiredOriginalTooltip;
                    Assert.That(tooltip, Is.Not.Null);
                    Assert.That(tooltip.LastError, Is.Null, "The original combat tooltip must lay out with native bitmap fonts.");
                    Assert.That(tooltip.LastLayout, Is.Not.Null);
                    Assert.That(tooltip.RequiredLastLayout.BodyLines, Is.Empty, "Combat hover must not display the plant description.");
                    Assert.That(tooltip.RequiredLastLayout.RequiredTitle.Glyphs.Count, Is.GreaterThan(0), "Combat keeps the original plant name.");
                    Assert.That(tooltip.RequiredLastLayout.RequiredWarning.Glyphs.Count, Is.GreaterThan(0), "Combat keeps the original insufficient-sun warning.");
                    draw.Invoke(presenter, new object[] { PlantKind.Peashooter, new Rect(36, 154, 50, 70), true });
                    Assert.That(tooltip.LastError, Is.Null);
                    Assert.That(tooltip.RequiredLastLayout.BodyLines.Count, Is.GreaterThan(0), "The seed chooser retains the original plant description.");
                    var body = Regex.Replace(string.Join(" ", tooltip.RequiredLastLayout.BodyLines), "\\s", "");
                    var expected = Regex.Replace(presenter.OriginalProgressionText("PEASHOOTER_TOOLTIP"), "\\s", "");
                    Assert.That(body, Is.EqualTo(expected), "The chooser uses only the original LawnStrings description.");
                    Assert.That(tooltip.RequiredLastLayout.RequiredWarning.Glyphs, Is.Empty, "The chooser does not show a combat availability warning.");
                }
                finally
                {
                    CanvasUI.EndFrame();
                }
            }
            finally
            {
                owner._originalTooltip?.Dispose();
                presenter.Dispose();
                UnityEngine.Object.Destroy(host);
            }
        }
    }
}
