using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PvZReplica.Levels;
using UnityEngine;

namespace PvZReplica
{
    // Only enabled by the explicit isolated Player smoke-test switch.
    internal sealed class LevelOneVisualSmokeTest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "-pvz-level-one-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-level-one-logic-smoke") >= 0)
                new GameObject("Level one smoke test").AddComponent<LevelOneVisualSmokeTest>();
        }

        private IEnumerator Start()
        {
            yield return null;
            PvZGame controller = FindFirstObjectByType<PvZGame>();
            if (controller == null) { Fail("controller missing"); yield break; }
            Type type = typeof(PvZGame);
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo gameField = type.GetField("game", flags);
            FieldInfo deckField = type.GetField("deck", flags);
            FieldInfo texturesField = type.GetField("textures", flags);
            MethodInfo click = type.GetMethod("HandleClick", flags);
            MethodInfo loadTexture = type.GetMethod("Texture", flags);
            MethodInfo cancelSelection = type.GetMethod("CancelBoardSelection", flags);
            if (gameField == null || deckField == null || texturesField == null || click == null || loadTexture == null ||
                cancelSelection == null)
            {
                Fail("controller fields unavailable"); yield break;
            }
            click.Invoke(controller, new object[] { new Vector2(500, 105) });
            GameSimulation game = (GameSimulation)gameField.GetValue(controller);
            var deck = (List<PlantKind>)deckField.GetValue(controller);
            if (!game.IsExperimentalLevelOne || !game.LevelOneUsesNativeSkySunRules ||
                deck.Count != 1 || deck[0] != PlantKind.Peashooter)
            {
                Fail("experimental board or deck not selected"); yield break;
            }
            game.Update(0.02f);
            bool logicOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-level-one-logic-smoke") >= 0;
            if (logicOnly)
            {
                loadTexture.Invoke(controller, new object[] { "background1unsodded.jpg" });
                loadTexture.Invoke(controller, new object[] { "sod1row.png" });
            }
            else yield return new WaitForEndOfFrame();
            var textures = (Dictionary<string, Texture2D>)texturesField.GetValue(controller);
            Texture2D background, grass;
            if (!textures.TryGetValue("background1unsodded.jpg", out background) || background == null ||
                !textures.TryGetValue("sod1row.png", out grass) || grass == null)
            {
                Fail("single-row board images were not loaded"); yield break;
            }
            string screenshot = null;
            if (!logicOnly)
            {
                screenshot = Path.Combine(SmokeTestArtifacts.DirectoryPath,
                    "level-one-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
                SmokeTestArtifacts.CaptureFrame(screenshot);
                float timeout = Time.realtimeSinceStartup + 10f;
                while (!File.Exists(screenshot) && Time.realtimeSinceStartup < timeout) yield return null;
                if (!File.Exists(screenshot)) { Fail("screenshot was not written"); yield break; }
                Texture2D captured = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!captured.LoadImage(File.ReadAllBytes(screenshot)))
                {
                    Destroy(captured); Fail("screenshot could not be decoded"); yield break;
                }
                Color32[] pixels = captured.GetPixels32();
                int visible = 0;
                foreach (Color32 pixel in pixels)
                    if (pixel.r + pixel.g + pixel.b > 24) visible++;
                Destroy(captured);
                if (visible < pixels.Length / 100) { Fail("screenshot is blank"); yield break; }
            }

            click.Invoke(controller, new object[] { new Vector2(105, 40) });
            if (!game.LevelOneSelectedSeed.HasValue) { Fail("seed was not selected"); yield break; }
            cancelSelection.Invoke(controller, null);
            if (game.LevelOneSelectedSeed.HasValue ||
                game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.AwaitingSeedSelection)
            {
                Fail("right-click cancellation did not restore the tutorial state"); yield break;
            }
            click.Invoke(controller, new object[] { new Vector2(105, 40) });
            if (!game.LevelOneSelectedSeed.HasValue) { Fail("seed was not reselected after cancellation"); yield break; }
            click.Invoke(controller, new object[] { new Vector2(110, 130) });
            if (game.Plants.Count != 0 || !game.LevelOneSelectedSeed.HasValue)
            {
                Fail("planting outside the grass row was accepted"); yield break;
            }
            click.Invoke(controller, new object[] { new Vector2(110, 330) });
            if (game.Plants.Count != 1 || game.SunAmount != 50 || game.LevelOneSelectedSeed.HasValue ||
                game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.AwaitingUsableSeed)
            {
                Fail("first planting or tutorial state failed"); yield break;
            }
            int startedCollections = 0;
            for (int tick = 0; tick < 2000 &&
                (game.SunAmount < 100 || game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.AwaitingSeedSelection); tick++)
            {
                game.Update(0.02f);
                if (game.SpawnedZombies != 0) { Fail("tutorial wave gate failed"); yield break; }
                for (int i = game.LevelOneNativeSkySuns.Count - 1; i >= 0; i--)
                {
                    LevelOneNativeSkySun sun = game.LevelOneNativeSkySuns[i];
                    if (sun.IsCollecting) continue;
                    int previousBalance = game.SunAmount;
                    click.Invoke(controller, new object[] { new Vector2(sun.PixelX + 30f, sun.PixelY + 30f) });
                    if (!sun.IsCollecting || game.SunAmount != previousBalance)
                    {
                        Fail("sky-sun click did not start delayed collection"); yield break;
                    }
                    startedCollections++;
                }
            }
            if (startedCollections < 2 || game.SunAmount < 100 ||
                game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.AwaitingSeedSelection)
            {
                Fail("sun collection did not release the seed tutorial"); yield break;
            }
            click.Invoke(controller, new object[] { new Vector2(105, 40) });
            click.Invoke(controller, new object[] { new Vector2(190, 330) });
            game.Update(1.1f);
            if (game.Plants.Count != 2 || game.CurrentLevelOneTutorialStage != LevelOneTutorialStage.ZombieOnslaughtPrompt ||
                game.SpawnedZombies != 1 || game.Zombies.Count != 1 || game.Zombies[0].Row != LevelOneData.GrassRow)
            {
                Fail("second planting or first experimental wave failed"); yield break;
            }
            Debug.Log((logicOnly ? "PVZ_LEVEL_ONE_LOGIC_SMOKE_PASS" : "PVZ_LEVEL_ONE_SMOKE_PASS") +
                ": single-row image loading, seed cancellation, planting, sun, tutorial gate and first wave; screenshot=" +
                (screenshot ?? "not captured"));
            controller.RequestQuit(0);
        }

        private static void Fail(string message)
        {
            Debug.LogError("PVZ_LEVEL_ONE_SMOKE_FAIL: " + message);
            SmokeTestArtifacts.Quit(1);
        }
    }
}
