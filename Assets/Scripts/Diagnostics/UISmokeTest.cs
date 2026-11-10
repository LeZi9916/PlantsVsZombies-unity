#nullable enable
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
    // Enabled only for explicit player smoke tests.
    /// <summary>Defines uismoke test.</summary>
    internal sealed class UISmokeTest : MonoBehaviour
    {
        /// <summary>Stores controller.</summary>
        private PvZGame _controller;
        /// <summary>Stores click.</summary>
        private MethodInfo _click;
        /// <summary>Stores view field.</summary>
        private FieldInfo _viewField;
        /// <summary>Stores options field.</summary>
        private FieldInfo _optionsField;
        /// <summary>Stores almanac field.</summary>
        private FieldInfo _almanacField;
        /// <summary>Stores profiles field.</summary>
        private FieldInfo _profilesField;
        /// <summary>Stores profile dialog field.</summary>
        private FieldInfo _profileDialogField;
        /// <summary>Stores help field.</summary>
        private FieldInfo _helpField;
        /// <summary>Stores quit field.</summary>
        private FieldInfo _quitField;
        /// <summary>Stores unavailable field.</summary>
        private FieldInfo _unavailableField;
        /// <summary>Stores help pointer.</summary>
        private MethodInfo _helpPointer;
        /// <summary>Stores almanac pointer.</summary>
        private MethodInfo _almanacPointer;
        /// <summary>Stores game field.</summary>
        private FieldInfo _gameField;
        /// <summary>Stores apply options.</summary>
        private MethodInfo _applyOptions;
        /// <summary>Stores close options.</summary>
        private MethodInfo _closeOptions;
        /// <summary>Stores music field.</summary>
        private FieldInfo _musicField;
        /// <summary>Stores directory.</summary>
        private string _directory;
        /// <summary>Stores session.</summary>
        private string _session;
        /// <summary>Stores logic only.</summary>
        private bool _logicOnly;
        /// <summary>Performs start if requested.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "-pvz-ui-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-ui-logic-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0)
            {
                new GameObject("UI smoke test").AddComponent<UISmokeTest>();
            }
        }

        /// <summary>Performs start.</summary>
        /// <returns>The resulting start.</returns>
        private IEnumerator Start()
        {
            yield return null;
            _controller = FindFirstObjectByType<PvZGame>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(PvZGame);
            _click = type.GetMethod("HandleClick", flags);
            _viewField = type.GetField("_currentView", flags);
            _optionsField = type.GetField("_optionsView", flags);
            _almanacField = type.GetField("_almanacView", flags);
            _profilesField = type.GetField("_profiles", flags);
            _profileDialogField = type.GetField("_profileDialog", flags);
            _helpField = type.GetField("_helpView", flags);
            _quitField = type.GetField("_confirmingQuit", flags);
            _unavailableField = type.GetField("_unavailableFeature", flags);
            _helpPointer = type.GetMethod("HandleHelpPointer", flags);
            _almanacPointer = type.GetMethod("HandleAlmanacPointer", flags);
            _gameField = type.GetField("_game", flags);
            _applyOptions = type.GetMethod("ApplyOptions", flags);
            _closeOptions = type.GetMethod("CloseOptions", flags);
            _musicField = type.GetField("_musicSource", flags);
            if (_controller == null || _click == null || _viewField == null || _optionsField == null || _almanacField == null || _profilesField == null || _profileDialogField == null || _helpField == null || _quitField == null || _unavailableField == null || _helpPointer == null || _almanacPointer == null || _gameField == null || _applyOptions == null || _closeOptions == null || _musicField == null)
            {
                Fail("UI controller is unavailable");
                yield break;
            }

            _directory = SmokeTestArtifacts.DirectoryPath;
            _session = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var arguments = Environment.GetCommandLineArgs();
            _logicOnly = Array.IndexOf(arguments, "-pvz-ui-logic-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0;
            if (!ViewIs("Selector"))
            {
                Fail("Startup did not open selector");
                yield break;
            }

            if (Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0)
            {
                var initialDialog = (ProfileDialog)_profileDialogField.GetValue(_controller);
                var initialProfiles = (ProfileCatalog)_profilesField.GetValue(_controller);
                if (initialDialog == null || !initialDialog.IsOpen || initialProfiles.Profiles.Count != 0)
                {
                    Fail("First launch did not request a user");
                    yield break;
                }

                yield return Capture("first-user");
                var initialName = typeof(ProfileDialog).GetField("_editName", flags);
                if (initialName == null)
                {
                    Fail("Profile name input is unavailable");
                    yield break;
                }

                initialName.SetValue(initialDialog, "首次用户");
                initialDialog.HandleClick(new Vector2(300, 470));
                if (initialDialog.IsOpen || initialProfiles.Profiles.Count != 1 || !initialProfiles.SelectedId.HasValue)
                {
                    Fail("First user was not created and selected");
                    yield break;
                }

                Debug.Log((_logicOnly ? "PVZ_FIRST_USER_LOGIC_SMOKE_PASS" : "PVZ_FIRST_USER_SMOKE_PASS") + ": first-run prompt and isolated profile creation" + (_logicOnly ? "; no screenshot captured" : ""));
                _controller.RequestQuit(0);
                yield break;
            }

            yield return Capture("selector");
            if (!_logicOnly)
            {
                var selectorPath = ScreenshotPath("selector");
                if (!File.Exists(selectorPath))
                {
                    Fail("Selector screenshot missing");
                    yield break;
                }

                var screenshot = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!screenshot.LoadImage(File.ReadAllBytes(selectorPath)))
                {
                    Destroy(screenshot);
                    Fail("Selector screenshot could not be decoded");
                    yield break;
                }

                var background = screenshot.GetPixel((int)(screenshot.width * 0.43f), (int)(screenshot.height * 0.5f));
                Destroy(screenshot);
                if (background.r + background.g + background.b < 0.08f)
                {
                    Fail("Selector background mask rendered black");
                    yield break;
                }
            }

            Click(100, 150);
            var profileDialog = (ProfileDialog)_profileDialogField.GetValue(_controller);
            var profiles = (ProfileCatalog)_profilesField.GetValue(_controller);
            if (profileDialog == null || !profileDialog.IsOpen)
            {
                Fail("Profile dialog did not open");
                yield break;
            }

            yield return Capture("profiles");
            profileDialog.HandleClick(new Vector2(300, 244));
            var editName = typeof(ProfileDialog).GetField("_editName", flags);
            if (editName == null)
            {
                Fail("Profile name input is unavailable");
                yield break;
            }

            editName.SetValue(profileDialog, "界面测试");
            yield return Capture("profile-create");
            profileDialog.HandleClick(new Vector2(300, 470));
            if (profileDialog.IsOpen || profiles.Profiles.Count != 2 || !profiles.SelectedId.HasValue || profiles.Profiles[1].Id != profiles.SelectedId.Value)
            {
                Fail("Creating and selecting a profile failed");
                yield break;
            }

            Click(670, 515);
            if (!ViewIs("Help") || !((HelpView)_helpField.GetValue(_controller)).IsOpen)
            {
                Fail("Help did not open");
                yield break;
            }

            yield return Capture("help");
            _helpPointer.Invoke(_controller, new object[] { new Vector2(400, 520), EventType.MouseDown });
            _helpPointer.Invoke(_controller, new object[] { new Vector2(400, 520), EventType.MouseUp });
            if (!ViewIs("Selector"))
            {
                Fail("Help did not return to selector");
                yield break;
            }

            Click(500, 215);
            if (!ViewIs("Modes"))
            {
                Fail("Mini-games did not open");
                yield break;
            }

            yield return Capture("modes");
            Click(80, 120);
            yield return Capture("unimplemented-mode");
            Click(400, 350);
            Click(40, 580);
            if (!ViewIs("Selector"))
            {
                Fail("Mode screen did not return to selector");
                yield break;
            }

            Click(470, 535);
            if (!ViewIs("Selector") || !string.Equals(_unavailableField.GetValue(_controller) as string, "商店", StringComparison.Ordinal))
            {
                Fail("Corrected store entry did not show its unavailable notice");
                yield break;
            }

            yield return Capture("unimplemented-store");
            Click(400, 350);
            if (_unavailableField.GetValue(_controller) != null)
            {
                Fail("Store notice did not close");
                yield break;
            }

            Click(360, 460);
            if (!ViewIs("Almanac"))
            {
                Fail("Almanac did not open");
                yield break;
            }

            yield return Capture("almanac-home");
            Click(200, 365);
            var almanac = (AlmanacView)_almanacField.GetValue(_controller);
            if (almanac.Screen != AlmanacScreen.PlantIndex && almanac.Screen != AlmanacScreen.PlantDetail)
            {
                Fail("Plant index did not open");
                yield break;
            }

            yield return Capture("almanac-plants");
            for (var id = 0; id < 49; id++)
            {
                Click(id == 48 ? 21 : 27 + id % 8 * 52, id == 48 ? 24 : 93 + id / 8 * 78);
                if (almanac.SelectedId != id)
                {
                    Fail("Almanac plant selection failed: " + id);
                    yield break;
                }

                if (!_logicOnly)
                {
                    yield return null;
                }

                if (id == 18)
                {
                    yield return Capture("almanac-threepeater");
                }

                if (id == 23)
                {
                    yield return Capture("almanac-tallnut");
                }

                if (id == 35)
                {
                    yield return Capture("almanac-coffee");
                }

                if (id == 40)
                {
                    yield return Capture("almanac-gatling");
                }
            }

            Click(50, 580);
            Click(550, 365);
            if (almanac.Screen != AlmanacScreen.ZombieDetail || almanac.SelectedId != 0)
            {
                Fail("Almanac zombie category did not default to normal zombie");
                yield break;
            }

            yield return Capture("almanac-zombies");
            for (var id = 0; id < 26; id++)
            {
                Click(id == 25 ? 193 : 23 + id % 5 * 85, id == 25 ? 487 : 87 + id / 5 * 80);
                if (almanac.SelectedId != id)
                {
                    Fail("Almanac zombie selection failed: " + id);
                    yield break;
                }

                if (!_logicOnly)
                {
                    yield return null;
                }

                if (id == 1)
                {
                    yield return Capture("almanac-flag");
                }

                if (id == 24)
                {
                    yield return Capture("almanac-imp");
                }

                if (id == 25)
                {
                    yield return Capture("almanac-boss");
                }
            }

            Click(50, 580);
            Click(200, 365);
            if (almanac.SelectedId != 48)
            {
                Fail("Almanac lost the previous plant selection");
                yield break;
            }

            Click(50, 580);
            Click(550, 365);
            if (almanac.SelectedId != 25)
            {
                Fail("Almanac lost the previous zombie selection");
                yield break;
            }

            if (almanac.Diagnostics.Count > 0)
            {
                Fail("Almanac portrait/resource diagnostics: " + string.Join(" | ", almanac.Diagnostics));
                yield break;
            }

            AlmanacPointer(720, 580, EventType.MouseDown);
            if (!ViewIs("Almanac"))
            {
                Fail("Almanac close committed on press");
                yield break;
            }

            AlmanacPointer(600, 550, EventType.MouseUp);
            if (!ViewIs("Almanac"))
            {
                Fail("Almanac close did not cancel after dragging out");
                yield break;
            }

            AlmanacPointer(720, 580, EventType.MouseDown);
            AlmanacPointer(720, 580, EventType.MouseUp);
            if (!ViewIs("Selector"))
            {
                Fail("Almanac did not return to selector on release");
                yield break;
            }

            Click(500, 105);
            if (!ViewIs("Board"))
            {
                Fail("Adventure entry did not open prototype board");
                yield break;
            }

            Click(750, 23);
            var options = (OptionsView)_optionsField.GetValue(_controller);
            if (!options.IsOpen)
            {
                Fail("Options did not open");
                yield break;
            }

            yield return Capture("options");
            var music = (AudioSource)_musicField.GetValue(_controller);
            var appliedMusic = music.volume;
            options.HandlePointer(new Vector2(515, 183), EventType.MouseDown);
            options.HandlePointer(new Vector2(515, 183), EventType.MouseUp);
            var draftMusic = options.MusicVolume;
            if (Mathf.Approximately(draftMusic, appliedMusic))
            {
                Fail("Music draft did not change");
                yield break;
            }

            options.HandlePointer(new Vector2(400, 310), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 310), EventType.MouseUp);
            if (options.CloseResult != OptionsCloseResult.OpenAlmanac)
            {
                Fail("Options almanac button did not dispatch");
                yield break;
            }

            _applyOptions.Invoke(_controller, null);
            var pausedGame = (GameSimulation)_gameField.GetValue(_controller);
            var pausedElapsed = pausedGame.Elapsed;
            if (!ViewIs("Almanac") || options.IsOpen || !Mathf.Approximately(music.volume, appliedMusic))
            {
                Fail("Opening modal almanac applied or lost the options draft");
                yield break;
            }

            yield return Capture("almanac-from-options");
            for (var frame = 0; frame < 6; frame++)
            {
                yield return null;
            }

            if (pausedGame.Elapsed != pausedElapsed)
            {
                Fail("Combat advanced behind the almanac");
                yield break;
            }

            AlmanacPointer(720, 580, EventType.MouseDown);
            AlmanacPointer(720, 580, EventType.MouseUp);
            if (!ViewIs("Board") || !options.IsOpen || !Mathf.Approximately(options.MusicVolume, draftMusic) || !Mathf.Approximately(music.volume, appliedMusic))
            {
                Fail("Closing modal almanac did not restore the unchanged options draft");
                yield break;
            }

            yield return Capture("almanac-return-options");
            for (var frame = 0; frame < 6; frame++)
            {
                yield return null;
            }

            if (pausedGame.Elapsed != pausedElapsed)
            {
                Fail("Closing almanac resumed combat before options closed");
                yield break;
            }

            options.HandlePointer(new Vector2(400, 355), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 355), EventType.MouseUp);
            if (!options.IsOpen || options.CloseResult != OptionsCloseResult.None)
            {
                Fail("Options restart confirmation did not open");
                yield break;
            }

            _closeOptions.Invoke(_controller, null);
            if (!options.IsOpen || options.CloseResult != OptionsCloseResult.None || !Mathf.Approximately(music.volume, appliedMusic))
            {
                Fail("Cancelling restart applied or closed the options draft");
                yield break;
            }

            options.HandlePointer(new Vector2(400, 355), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 355), EventType.MouseUp);
            options.HandlePointer(new Vector2(300, 386), EventType.MouseDown);
            options.HandlePointer(new Vector2(300, 386), EventType.MouseUp);
            if (options.IsOpen || options.CloseResult != OptionsCloseResult.RestartLevel)
            {
                Fail("Options restart confirmation did not dispatch");
                yield break;
            }

            _applyOptions.Invoke(_controller, null);
            if (!Mathf.Approximately(music.volume, draftMusic))
            {
                Fail("Confirmed options draft was not applied");
                yield break;
            }

            Click(750, 23);
            options.HandlePointer(new Vector2(400, 405), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 405), EventType.MouseUp);
            if (options.IsOpen || options.CloseResult != OptionsCloseResult.MainMenu)
            {
                Fail("Options main menu button did not dispatch");
                yield break;
            }

            _applyOptions.Invoke(_controller, null);
            if (!ViewIs("Selector"))
            {
                Fail("Options main menu did not navigate");
                yield break;
            }

            Click(740, 550);
            if (!(bool)_quitField.GetValue(_controller))
            {
                Fail("Quit confirmation did not open");
                yield break;
            }

            yield return Capture("quit");
            Click(480, 370);
            if ((bool)_quitField.GetValue(_controller))
            {
                Fail("Quit cancellation failed");
                yield break;
            }

            Debug.Log((_logicOnly ? "PVZ_UI_LOGIC_SMOKE_PASS" : "PVZ_UI_SMOKE_PASS") + ": selector, profiles, help, modes, corrected store/almanac entries, 75 almanac selections, modal options pause/return, options, restart, main menu and quit cancellation" + (_logicOnly ? "; no screenshots captured" : ""));
            _controller.RequestQuit(0);
        }

        /// <summary>Performs capture.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting capture.</returns>
        private IEnumerator Capture(string name)
        {
            if (_logicOnly)
            {
                yield break;
            }

            yield return new WaitForEndOfFrame();
            var path = ScreenshotPath(name);
            SmokeTestArtifacts.CaptureFrame(path);
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!File.Exists(path))
            {
                Fail("Screenshot was not written: " + name);
            }
        }

        /// <summary>Performs screenshot path.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting screenshot path.</returns>
        private string ScreenshotPath(string name)
        {
            return Path.Combine(_directory, "ui-" + name + "-" + _session + ".png");
        }

        /// <summary>Performs click.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        private void Click(float x, float y)
        {
            _click.Invoke(_controller, new object[] { new Vector2(x, y) });
        }

        /// <summary>Performs almanac pointer.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "type">The type.</param>
        private void AlmanacPointer(float x, float y, EventType type)
        {
            _almanacPointer.Invoke(_controller, new object[] { new Vector2(x, y), type });
        }

        /// <summary>Performs view is.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting view is.</returns>
        private bool ViewIs(string name)
        {
            return string.Equals(_viewField.GetValue(_controller).ToString(), name, StringComparison.Ordinal);
        }

        /// <summary>Performs fail.</summary>
        /// <param name = "message">The message.</param>
        private static void Fail(string message)
        {
            Debug.LogError("PVZ_UI_SMOKE_FAIL: " + message);
            SmokeTestArtifacts.Quit(1);
        }
    }
}
