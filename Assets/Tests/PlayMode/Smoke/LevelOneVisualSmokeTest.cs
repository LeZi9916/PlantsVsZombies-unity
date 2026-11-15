#nullable enable
using PlantsVsZombies.Game;
using PlantsVsZombies.Levels;
using PlantsVsZombies.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using PlantsVsZombies.Tests.Shared;
using static UnityEngine.Object;

namespace PlantsVsZombies.Tests.PlayMode.Smoke
{
    /// <summary>Defines level one visual smoke test.</summary>
    public sealed class LevelOneVisualSmokeTest : PlayerSmokeFixture
    {
        /// <summary>Uses the experimental single-row board for both migrated cases.</summary>
        protected override bool ExperimentalLevelOne => true;

        /// <summary>Checks the original single-row scenario with captured frames.</summary>
        /// <returns>The original interaction and capture waits.</returns>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator VisualScenario()
        {
            yield return Run(false);
        }

        /// <summary>Checks the original single-row scenario without captures.</summary>
        /// <returns>The original interaction waits.</returns>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator LogicScenario()
        {
            yield return Run(true);
        }

        /// <summary>Checks the original single-row tutorial interactions.</summary>
        /// <param name="logicOnly">Whether to omit screenshot checks.</param>
        /// <returns>The original interaction and capture waits.</returns>
        private IEnumerator Run(bool logicOnly)
        {
            yield return null;
            var controller = Controller;
            if (controller == null)
            {
                Fail("controller missing");
                yield break;
            }

            var startupDeadline = Time.realtimeSinceStartup + 60f;
            while (controller.Loading.OriginalLoadingActive && Time.realtimeSinceStartup < startupDeadline)
            {
                yield return null;
            }

            if (controller.Loading.OriginalLoadingActive)
            {
                Fail("Original startup loading did not complete within 60 seconds");
                yield break;
            }

            var gameField = GameMemberReflection.BindField(controller, "_game");
            var deckField = GameMemberReflection.BindField(controller, "_deck");
            var click = GameMemberReflection.BindMethod(controller, "HandleClick");
            var cancelSelection = GameMemberReflection.BindMethod(controller, "CancelBoardSelection");
            if (gameField == null || deckField == null || click == null || cancelSelection == null)
            {
                Fail("controller fields unavailable");
                yield break;
            }

            click.Invoke(controller, new object[] { new Vector2(500, 105) });
            var game = (GameSimulation)gameField.GetValue(controller);
            var deck = (List<PlantKind>)deckField.GetValue(controller);
            if (!game.IsExperimentalLevelOne || !game.LevelOneUsesNativeSkySunRules || deck.Count != 1 || deck[0] != PlantKind.Peashooter)
            {
                Fail("experimental board or deck not selected");
                yield break;
            }

            game.Update(0.02f);
            if (logicOnly)
            {
                controller.Textures.LoadImage("background1unsodded.jpg");
                controller.Textures.LoadImage("sod1row.png");
            }
            else
            {
                yield return SmokeTestArtifacts.WaitForFrame();
            }

            if (!SpriteSourceTestUtilities.TryGetImage(controller.Textures, "background1unsodded.jpg", out var background) || background == null
                || !SpriteSourceTestUtilities.TryGetImage(controller.Textures, "sod1row.png", out var grass) || grass == null)
            {
                Fail("single-row board images were not loaded");
                yield break;
            }

            string? screenshot = null;
            if (!logicOnly)
            {
                screenshot = Path.Combine(SmokeTestArtifacts.DirectoryPath, "level-one-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
                SmokeTestArtifacts.CaptureFrame(screenshot);
                var timeout = Time.realtimeSinceStartup + 10f;
                while (!File.Exists(screenshot) && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                if (!File.Exists(screenshot))
                {
                    Fail("screenshot was not written");
                    yield break;
                }

                var captured = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!captured.LoadImage(File.ReadAllBytes(screenshot)))
                {
                    Destroy(captured);
                    Fail("screenshot could not be decoded");
                    yield break;
                }

                var pixels = captured.GetPixels32();
                var visible = 0;
                foreach (Color32 pixel in pixels)
                {
                    if (pixel.r + pixel.g + pixel.b > 24)
                    {
                        visible++;
                    }
                }

                Destroy(captured);
                if (visible < pixels.Length / 100)
                {
                    Fail("screenshot is blank");
                    yield break;
                }
            }

            click.Invoke(controller, new object[] { new Vector2(105, 40) });
            if (!game.LevelOneSelectedSeed.HasValue)
            {
                Fail("seed was not selected");
                yield break;
            }

            cancelSelection.Invoke(controller, null);
            if (game.LevelOneSelectedSeed.HasValue || game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.AwaitingSeedSelection)
            {
                Fail("right-click cancellation did not restore the tutorial state");
                yield break;
            }

            click.Invoke(controller, new object[] { new Vector2(105, 40) });
            if (!game.LevelOneSelectedSeed.HasValue)
            {
                Fail("seed was not reselected after cancellation");
                yield break;
            }

            click.Invoke(controller, new object[] { new Vector2(110, 130) });
            if (game.Plants.Count != 0 || !game.LevelOneSelectedSeed.HasValue)
            {
                Fail("planting outside the grass row was accepted");
                yield break;
            }

            click.Invoke(controller, new object[] { new Vector2(110, 330) });
            if (game.Plants.Count != 1 || game.SunAmount != 50 || game.LevelOneSelectedSeed.HasValue || game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.AwaitingUsableSeed)
            {
                Fail("first planting or tutorial state failed");
                yield break;
            }

            var startedCollections = 0;
            for (var tick = 0; tick < 2000 && (game.SunAmount < 100 || game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.AwaitingSeedSelection); tick++)
            {
                game.Update(0.02f);
                if (game.SpawnedZombies != 0)
                {
                    Fail("tutorial wave gate failed");
                    yield break;
                }

                for (var i = game.LevelOneNativeSkySuns.Count - 1; i >= 0; i--)
                {
                    var sun = game.LevelOneNativeSkySuns[i];
                    if (sun.IsCollecting)
                    {
                        continue;
                    }

                    var previousBalance = game.SunAmount;
                    click.Invoke(controller, new object[] { new Vector2(sun.PixelX + 30f, sun.PixelY + 30f) });
                    if (!sun.IsCollecting || game.SunAmount != previousBalance)
                    {
                        Fail("sky-sun click did not start delayed collection");
                        yield break;
                    }

                    startedCollections++;
                }
            }

            if (startedCollections < 2 || game.SunAmount < 100 || game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.AwaitingSeedSelection)
            {
                Fail("sun collection did not release the seed tutorial");
                yield break;
            }

            click.Invoke(controller, new object[] { new Vector2(105, 40) });
            click.Invoke(controller, new object[] { new Vector2(190, 330) });
            game.Update(1.1f);
            if (game.Plants.Count != 2 || game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.ZombieOnslaughtPrompt || game.SpawnedZombies != 1 || game.Zombies.Count != 1 || game.Zombies[0].Row != LevelOneData.GrassRow)
            {
                Fail("second planting or first experimental wave failed");
                yield break;
            }

        }

        /// <summary>Performs fail.</summary>
        /// <param name = "message">The message.</param>
        private static void Fail(string message)
        {
            Assert.Fail(message);
        }
    }
}
