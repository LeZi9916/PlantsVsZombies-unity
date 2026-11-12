#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using System;
using System.Collections;

using System.IO;
using System.Reflection;
using UnityEngine;

namespace PlantsVsZombies.Diagnostics
{
    // Explicit isolated opening/audio/card inspection. Normal player never creates these fixtures.
    /// <summary>Defines intro audio player smoke.</summary>
    internal sealed class IntroAudioPlayerSmoke : MonoBehaviour
    {
        /// <summary>Stores controller.</summary>
        private PvZGame _controller;
        /// <summary>Stores checks.</summary>
        private int _checks;
        /// <summary>Stores error log.</summary>
        private string _errorLog;
        /// <summary>Stores private.</summary>
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        /// <summary>Performs field.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting field.</returns>
        private object Field(string name)
        {
            return GameMemberReflection.GetField(_controller, name);
        }

        /// <summary>Performs call.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "args">The args.</param>
        /// <returns>The resulting call.</returns>
        private object Call(string name, params object[] args)
        {
            var method = GameMemberReflection.FindMethod(_controller, name, out var owner);
            if (method != null)
            {
                return method.Invoke(owner, args);
            }

            var presenter = GameMemberReflection.GetField(_controller, "_presenter");
            return presenter.GetType().GetMethod(name, Private).Invoke(presenter, args);
        }

        /// <summary>Gets or sets sim.</summary>
        private GameSimulation Sim
        {
            get
            {
                return (GameSimulation)Field("_game");
            }
        }

        /// <summary>Gets or sets opening.</summary>
        private AdventureIntroClock Opening
        {
            get
            {
                return (AdventureIntroClock)Field("_adventureIntro");
            }
        }

        /// <summary>Performs check.</summary>
        /// <param name = "yes">The yes.</param>
        /// <param name = "reason">The reason.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private void Check(bool yes, string reason)
        {
            _checks++;
            if (!yes)
            {
                throw new InvalidOperationException(reason);
            }
        }

        /// <summary>Performs on log.</summary>
        /// <param name = "message">The message.</param>
        /// <param name = "stack">The stack.</param>
        /// <param name = "type">The type.</param>
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _errorLog = message;
            }
        }

        /// <summary>Performs on destroy.</summary>
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        /// <summary>Performs bootstrap.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-intro-audio-smoke") >= 0)
            {
                new GameObject("Opening audio inspection").AddComponent<IntroAudioPlayerSmoke>();
            }
        }

        /// <summary>Performs start.</summary>
        /// <returns>The resulting start.</returns>
        private IEnumerator Start()
        {
            yield return null;
            _controller = FindFirstObjectByType<PvZGame>();
            Application.logMessageReceived += OnLog;
            var run = Run();
            while (true)
            {
                bool more;
                try
                {
                    more = run.MoveNext();
                }
                catch (Exception error)
                {
                    Debug.LogError("PVZ_INTRO_AUDIO_FAIL: " + error);
                    SmokeTestArtifacts.Quit(1);
                    yield break;
                }

                if (!more)
                {
                    break;
                }

                yield return run.Current;
            }

            if (_errorLog != null)
            {
                Debug.LogError("PVZ_INTRO_AUDIO_FAIL: " + _errorLog);
                SmokeTestArtifacts.Quit(1);
            }
            else
            {
                Debug.Log("PVZ_INTRO_AUDIO_PASS: " + _checks + " focused host checks");
                SmokeTestArtifacts.Quit(0);
            }
        }

        /// <summary>Performs capture.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting capture.</returns>
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            SmokeTestArtifacts.CaptureFrame(Path.Combine(SmokeTestArtifacts.DirectoryPath, name + ".png"));
        }

        /// <summary>Performs start level.</summary>
        /// <param name = "level">The level.</param>
        private void StartLevel(int level)
        {
            var catalog = (ProfileCatalog)Field("_profiles");
            var progress = (AdventureProgressStore)Field("_adventureStore");
            progress.Import(catalog.SelectedId.Value, level, 0);
            Call("HandleSelector", SelectorAction.Adventure);
        }

        /// <summary>Performs run.</summary>
        /// <returns>The resulting run.</returns>
        private IEnumerator Run()
        {
            var timeout = Time.unscaledTime + 60;
            while (_controller.Loading.OriginalLoadingActive && Time.unscaledTime < timeout)
            {
                yield return null;
            }

            Check(!_controller.Loading.OriginalLoadingActive, "Loading never completed");
            Call("HandleSelector", SelectorAction.Adventure);
            Check(Opening != null && !Opening.Complete, "Normal adventure skipped opening");
            _controller.Music.BeginMusicOutputProbe(Path.Combine(SmokeTestArtifacts.DirectoryPath, "actual-music-output.json"), 3f);
            Check(Opening.Frame.BoardX == 220 && Sim.Elapsed == 0, "Initial house/lawn camera or gameplay gate wrong");
            yield return Capture("00-house-before-pan");
            timeout = Time.unscaledTime + 5;
            while (Opening.NativeTime < 2450 && Time.unscaledTime < timeout)
            {
                yield return null;
            }

            Check(Opening.Frame.BoardX < 220 && Opening.Frame.BoardX > -380 && Sim.Elapsed == 0, "Original camera curve or frozen gameplay missing");
            yield return Capture("01-pan-to-street");
            while (Opening.NativeTime < 3650)
            {
                yield return null;
            }

            Check(Opening.Frame.BoardX == -380, "Street endpoint wrong");
            yield return Capture("02-street-zombie-preview");
            while (Opening.NativeTime < 7000)
            {
                yield return null;
            }

            Check(Opening.Frame.SodPosition >= 500 && Opening.Frame.SodPosition < 700 && Sim.Elapsed == 0, "First row sod was fully drawn before rolling");
            yield return Capture("03-first-row-sod-rolling");
            while (!Opening.Complete)
            {
                yield return null;
            }

            yield return Capture("04-first-row-ready");
            Check(Opening.Frame.SodPosition == 1000 && Opening.Frame.BoardX == 0, "Opening did not restore normal lawn");
            var audioFile = Path.Combine(SmokeTestArtifacts.DirectoryPath, "actual-music-output.json");
            Check(File.Exists(audioFile), "PCM report was not written");
            var report = JsonUtility.FromJson<global::PlantsVsZombies.Game.MusicOutputReport>(File.ReadAllText(audioFile));
            Check(report.status == "PASS" && report.sourcePcmNonzero && report.musicOnlyPcmNonzero && report.listeners == 1, "Actual music output stayed silent");
            foreach (int level in new[]
            {
                2,
                4
            }

            )
            {
                StartLevel(level);
                yield return Capture("05-level-" + level + "-before-sod");
                Call("UpdateAdventureOpening", 7f);
                Check(Opening.Frame.SodPosition >= 500 && Opening.Frame.SodPosition < 650, "Native new-sod level was skipped");
                yield return Capture("06-level-" + level + "-sod-rolling");
            }

            StartLevel(8);
            yield return Capture("07-chooser-home");
            Call("UpdateAdventureOpening", 4.3f);
            Check(Opening.WaitingForSeeds && Opening.NativeTime == 4250 && Sim.Elapsed == 0, "Chooser did not stop the native intro clock");
            yield return Capture("08-native-chooser-with-preview");
            Call("UpdateAdventureOpening", .5f);
            Check(Opening.NativeTime == 4250, "Clock advanced while waiting for player seeds");
            for (var i = 0; i < 6; i++)
            {
                Call("HandleClick", new Vector2(47 + i * 53, 163));
                if (i == 0)
                {
                    yield return new WaitForSecondsRealtime(.12f);
                    yield return Capture("08b-native-packet-flying-to-bank");
                }
            }

            Call("HandleClick", new Vector2(AdventureSeedSlotLayout.PositionX(5, 6) + 25, 43));
            Check(_controller._pendingSeeds.Count == 5 && !_controller._pendingSeeds.Contains(PlantKind.SnowPea),
                "The visible bank packet could not be returned to the chooser");
            yield return new WaitForSecondsRealtime(.12f);
            yield return Capture("08c-native-packet-returning-to-chooser");
            Call("HandleClick", new Vector2(47 + 5 * 53, 163));
            Check(_controller._pendingSeeds.Count == 6 && _controller._pendingSeeds[5] == PlantKind.SnowPea,
                "Reselection did not append the packet in click order");
            Call("HandleClick", new Vector2(232, 566));
            Check(!Opening.WaitingForSeeds && Opening.NativeTime >= 4260 && Sim.Elapsed == 0, "Chooser confirmation started the battle before returning");
            Call("UpdateAdventureOpening", .9f);
            yield return Capture("09-return-from-street");
            Call("UpdateAdventureOpening", 1.4f);
            Check(Opening.Frame.Stage == AdventureIntroStage.ReadySetPlant, "Ready set plant original reanim absent");
            yield return Capture("10-original-ready-set-plant");
            while (!Opening.Complete)
            {
                yield return null;
            }

            typeof(GameSimulation).GetProperty("SunAmount").SetValue(Sim, 500);
            var cooldowns = (int[])typeof(GameSimulation).GetField("_seedCooldownTicks", Private).GetValue(Sim);
            cooldowns[0] = 751;
            yield return Capture("11-seed-native-full-cooldown");
            cooldowns[0] = 376;
            yield return Capture("12-seed-native-half-cooldown");
            Sim.Adventure.SeedFlashes.Clear();
            cooldowns[0] = 2;
            Sim.Update(.01f);
            Check(cooldowns[0] == 1 && !Sim.Adventure.SeedFlashes.Exists(x => x.Kind == PlantKind.Peashooter), "Cooldown completed one tick early");
            Sim.Update(.01f);
            Check(cooldowns[0] == 0 && Sim.Adventure.SeedFlashes.Exists(x => x.Kind == PlantKind.Peashooter), "Actual cooldown completion never requested original flash");
            yield return new WaitForSecondsRealtime(.08f);
            yield return Capture("13-original-seed-ready-flash");
            global::PlantsVsZombies.Simulation.Sun sun = (global::PlantsVsZombies.Simulation.Sun)typeof(GameSimulation).GetMethod("CreateAdventurePresentationDroppedSun", Private).Invoke(Sim, new object[] { 1f, 0f, 25 });
            Sim.Suns.Add(sun);
            Check(Sim.CollectSun(sun) && sun.PresentationPickup.Collecting, "Test sunlight did not start real pickup flight");
            yield return Capture("14-sun-above-seed-bank");
            _controller._tooltipInspection = new OriginalTooltipRequest
            {
                Title = (string)Call("OriginalProgressionText", "SUNFLOWER"),
                Body = (string)Call("OriginalProgressionText", "SUNFLOWER_TOOLTIP"),
                Warning = (string)Call("OriginalProgressionText", "WAITING_TO_REFRESH"),
                X = 155,
                Y = 85,
                Centered = true
            };
            yield return Capture("14b-original-card-tooltip");
            var tip = (OriginalTooltip)Field("_originalTooltip");
            Check(tip.LastLayout != null && tip.LastError == null, "Native tooltip fonts/layout failed");
            _controller._tooltipInspection = null;
            Call("SetUnavailable", "提示", "此操作暂时无法继续，请返回后重试。");
            yield return Capture("15-original-message-dialog");
            GameMemberReflection.SetField(_controller, "_unavailableFeature", null);
            var catalog = (ProfileCatalog)Field("_profiles");
            var progress = (AdventureProgressStore)Field("_adventureStore");
            progress.Import(catalog.SelectedId.Value, 25, 0);
            var accounts = (global::PlantsVsZombies.Progression.PlayerAccountStore)Field("_accountStore");
            accounts.CreditBattleCoins(catalog.SelectedId.Value, "ui-material-probe", 20000);
            Call("OpenStore");
            yield return Capture("16-original-store-buttons");
            Call("HandleClick", new Vector2(407, 350));
            Check((int)Field("_selectedShopItem") >= 0, "Original shop control stopped opening the confirmation");
            yield return Capture("17-original-purchase-dialog");
            var view = GameMemberReflection.FindField(_controller, "_currentView", out var viewOwner)!;
            view.SetValue(viewOwner, Enum.Parse(view.FieldType, "Import"));
            yield return Capture("18-original-import-controls");
            progress.Import(catalog.SelectedId.Value, 1, 1);
            Check((bool)Call("StartIndependentMode", 61), "Zombie-card mode did not open");
            yield return Capture("19-original-zombie-seed-cards");

            // Explicit presentation fixture: this verifies the original note artwork without claiming an earned 1-9 victory.
            _controller.Campaign.StartAdventureTestLevel(9);
            _controller._choosingSeeds = false;
            typeof(GameSimulation).GetProperty("State").SetValue(Sim, MatchState.Won);
            Sim.Adventure.Phase = AdventurePhase.Finished;
            var note = new AdventureRewardPresentation(15, 400, 250, 50, 70, .5f, 0f, 0);
            Check(note.BeginCollection(), "The original note fixture could not start its normal collection transition");
            note.Update(7f);
            Sim.Adventure.RewardPresentation = note;
            Check(note.Stage == AdventureAwardStage.Introduction && note.IntroductionAlpha == 0f,
                "The note fixture did not reach its fully visible introduction");
            yield return Capture("20-original-1-9-note-alpha-grid");
            var paper = _controller.Textures.Load("ZombieNote.jpg");
            var ink = _controller.Textures.LoadAlphaGrid("ZombieNoteBlack1.png", "ZombieNote1.png");
            Check(paper != null && paper.GetPixels32()[0].a < 10, "The original JPEG paper border is opaque");
            Check(ink != null, "The original resources.xml note alpha grid did not load");
            var inkPixels = ink.GetPixels32();
            var transparentPixels = 0;
            var blackInkPixels = 0;
            foreach (var pixel in inkPixels)
            {
                if (pixel.a == 0)
                {
                    transparentPixels++;
                }
                else if (pixel.a > 128 && pixel.r == 0 && pixel.g == 0 && pixel.b == 0)
                {
                    blackInkPixels++;
                }
            }

            Check(transparentPixels > inkPixels.Length / 2 && blackInkPixels > 100,
                "Original note glyphs are not black ink over a transparent alpha grid");
            Check(_errorLog == null, "Player logged an error");
        }
    }
}
