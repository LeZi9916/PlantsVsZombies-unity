#nullable enable
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using System;
using PlantsVsZombies.Adventure;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.Diagnostics
{
    // This runner is enabled only by the explicit -pvz-smoke command-line switch.
    /// <summary>Defines visual smoke test.</summary>
    internal sealed class VisualSmokeTest : MonoBehaviour
    {
        /// <summary>Performs start if requested.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-smoke") >= 0)
            {
                new GameObject("Visual smoke test").AddComponent<VisualSmokeTest>();
            }
        }

        /// <summary>Performs start.</summary>
        /// <returns>The resulting start.</returns>
        private IEnumerator Start()
        {
            yield return null;
            var controller = FindFirstObjectByType<PvZGame>();
            if (controller == null)
            {
                Fail("Game controller missing");
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
            var rendererField = GameMemberReflection.BindField(controller, "_animations");
            var soundsField = GameMemberReflection.BindField(controller, "_sounds");
            var musicField = GameMemberReflection.BindField(controller, "_musicSource");
            var deckField = GameMemberReflection.BindField(controller, "_deck");
            var pendingField = GameMemberReflection.BindField(controller, "_pendingSeeds");
            var click = GameMemberReflection.BindMethod(controller, "HandleClick");
            if (gameField == null || rendererField == null || soundsField == null || musicField == null || deckField == null || pendingField == null || click == null)
            {
                Fail("Smoke test could not access the game state");
                yield break;
            }

            click.Invoke(controller, new object[] { new Vector2(500, 105) });
            var deck = (List<PlantKind>)deckField.GetValue(controller);
            click.Invoke(controller, new object[] { new Vector2(620, 23) });
            yield return new WaitForEndOfFrame();
            var chooserPath = Path.Combine(SmokeTestArtifacts.DirectoryPath, "chooser-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
            SmokeTestArtifacts.CaptureFrame(chooserPath);
            yield return null;
            click.Invoke(controller, new object[] { new Vector2(288, 269) });
            var pending = (AdventureSeedChooserState)pendingField.GetValue(controller);
            if (pending.Count != 7 || pending.Contains(PlantKind.Jalapeno))
            {
                Fail("Seed selection accepted more than seven plants");
                yield break;
            }

            foreach (Vector2 point in new[]
            {
                new Vector2(110, 43),
                new Vector2(110, 43),
                new Vector2(288, 269),
                new Vector2(353, 269),
                new Vector2(400, 461)
            }

            )
            {
                click.Invoke(controller, new object[] { point });
            }

            if (deck.Count != 7 || !deck.Contains(PlantKind.Jalapeno) || !deck.Contains(PlantKind.TallNut) || deck.Contains(PlantKind.Peashooter) || deck.Contains(PlantKind.Sunflower))
            {
                Fail("Seed chooser did not commit the selected plants");
                yield break;
            }

            var game = new GameSimulation(new GameRules {
                StartingSun = 1000,
                ZombieCount = 0,
                SkySunInterval = 0,
                ZombieSpeed = 0
            });
            gameField.SetValue(controller, game);
            foreach (Vector2 point in new[]
            {
                new Vector2(355, 40),
                new Vector2(110, 130),
                new Vector2(405, 40),
                new Vector2(110, 230)
            }

            )
            {
                click.Invoke(controller, new object[] { point });
            }

            if (game.PlantAt(0, 0) == null || game.PlantAt(0, 0).Kind != PlantKind.Jalapeno || game.PlantAt(1, 0) == null || game.PlantAt(1, 0).Health != 8000f)
            {
                Fail("Newly selected plants could not be planted");
                yield break;
            }

            game.Zombies.Add(new Zombie { Row = 0, X = 2.5f, Health = 270f });
            game.Update(1.1f);
            if (game.Zombies.Count != 0 || game.Flames.Count != 1)
            {
                Fail("Jalapeno did not burn its lane");
                yield break;
            }

            yield return new WaitForEndOfFrame();
            var flamePath = Path.Combine(SmokeTestArtifacts.DirectoryPath, "flame-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png");
            SmokeTestArtifacts.CaptureFrame(flamePath);
            yield return null;
            click.Invoke(controller, new object[] { new Vector2(620, 23) });
            foreach (Vector2 point in new[]
            {
                new Vector2(288, 269),
                new Vector2(353, 269),
                new Vector2(223, 185),
                new Vector2(288, 185),
                new Vector2(400, 461)
            }

            )
            {
                click.Invoke(controller, new object[] { point });
            }

            if (deck.Count != 7 || !deck.Contains(PlantKind.Peashooter) || !deck.Contains(PlantKind.Sunflower))
            {
                Fail("Seed chooser did not restore the original deck");
                yield break;
            }

            game = new GameSimulation(new GameRules {
                StartingSun = 1000,
                ZombieCount = 0,
                SkySunInterval = 0,
                ZombieSpeed = 0
            });
            gameField.SetValue(controller, game);
            Vector2[] clicks =
            {
                new Vector2(105, 40),
                new Vector2(110, 130),
                new Vector2(155, 40),
                new Vector2(190, 230),
                new Vector2(205, 40),
                new Vector2(270, 330),
                new Vector2(255, 40),
                new Vector2(350, 430),
                new Vector2(305, 40),
                new Vector2(430, 530),
                new Vector2(355, 40),
                new Vector2(110, 530),
                new Vector2(405, 40),
                new Vector2(190, 130)
            };
            foreach (Vector2 point in clicks)
            {
                click.Invoke(controller, new object[] { point });
            }

            if (game.Plants.Count != 7 || game.SunAmount != 250)
            {
                Fail("Seed selection and planting did not produce the expected state");
                yield break;
            }

            game.PlantAt(3, 3).ActionTimer = 100f;
            game.PlantAt(2, 2).Health = 2000f;
            game.Zombies.Add(new Zombie { Row = 0, X = 6.5f, Health = 270f });
            game.Zombies.Add(new Zombie {
                Row = 1,
                X = 7.5f,
                Health = 270f,
                ArmorHealth = 370f,
                Kind = ZombieKind.Conehead
            });
            game.Zombies.Add(new Zombie {
                Row = 2,
                X = 7.5f,
                Health = 270f,
                ArmorHealth = 1100f,
                Kind = ZombieKind.Buckethead
            });
            game.Zombies.Add(new Zombie {
                Row = 3,
                X = 7.5f,
                Health = 270f,
                Kind = ZombieKind.Flag
            });
            game.Zombies.Add(new Zombie {
                Row = 3,
                X = 5.7f,
                Health = 270f,
                ShieldHealth = 1100f,
                Kind = ZombieKind.ScreenDoor
            });
            game.Zombies.Add(new Zombie { Row = 4, X = 7.5f, Health = 270f });
            game.Suns.Add(new global::PlantsVsZombies.Simulation.Sun { X = 6.5f, Y = 2f, TargetY = 2f });
            game.Suns.Add(new global::PlantsVsZombies.Simulation.Sun { X = 5.5f, Y = 1.5f, TargetY = 1.5f });
            click.Invoke(controller, new object[] { new Vector2(510, 230) });
            if (game.Suns.Count != 1 || game.SunAmount != 275)
            {
                Fail("Clicking a sun did not collect it");
                yield break;
            }

            var renderer = (ReanimationRenderer)rendererField.GetValue(controller);
            if (ReanimationRenderer.IsZombieTrackVisible("anim_bucket", ZombieVariant.Normal) || !ReanimationRenderer.IsZombieTrackVisible("anim_cone", ZombieVariant.Conehead) || ReanimationRenderer.IsZombieTrackVisible("anim_cone", ZombieVariant.Conehead, false) || !ReanimationRenderer.IsZombieTrackVisible("anim_bucket", ZombieVariant.Buckethead))
            {
                Fail("Zombie equipment track filtering is incorrect");
                yield break;
            }

            foreach (string name in new[]
            {
                "PeaShooterSingle",
                "PeaShooter",
                "SunFlower",
                "Wallnut",
                "Tallnut",
                "CherryBomb",
                "Jalapeno",
                "fire",
                "SnowPea",
                "PotatoMine",
                "Zombie",
                "Zombie_flagpole",
                "Sun",
                "LawnMower"
            }

            )
            {
                global::PlantsVsZombies.Reanimation.AnimationInfo info;
                if (!renderer.TryGetAnimationInfo(name, null, out info) || info.FrameCount <= 0)
                {
                    Fail("Animation " + name + ": " + renderer.LastError);
                    yield break;
                }
            }

            var sounds = (Dictionary<GameEvent, AudioClip>)soundsField.GetValue(controller);
            var music = (AudioSource)musicField.GetValue(controller);
            var audioTimeout = Time.realtimeSinceStartup + 10f;
            while ((sounds.Count < 10 || music.clip == null) && Time.realtimeSinceStartup < audioTimeout)
            {
                yield return null;
            }

            if (sounds.Count < 10 || music.clip == null)
            {
                Fail("Audio did not finish loading: " + sounds.Count + " effects, music=" + (music.clip != null));
                yield break;
            }

            yield return new WaitForSeconds(1f);
            yield return new WaitForEndOfFrame();
            var fileName = "smoke-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".png";
            var path = Path.Combine(SmokeTestArtifacts.DirectoryPath, fileName);
            SmokeTestArtifacts.CaptureFrame(path);
            var timeout = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            if (!File.Exists(path))
            {
                Fail("Screenshot was not written");
                yield break;
            }

            Debug.Log("PVZ_SMOKE_PASS: seed chooser, 9 plant types, lane fire, sun, 14 animation assets, 10 effects and music; screenshot=" + path + "; chooser=" + chooserPath + "; flame=" + flamePath);
            controller.RequestQuit(0);
        }

        /// <summary>Performs fail.</summary>
        /// <param name = "message">The message.</param>
        private static void Fail(string message)
        {
            Debug.LogError("PVZ_SMOKE_FAIL: " + message);
            SmokeTestArtifacts.Quit(1);
        }
    }
}
