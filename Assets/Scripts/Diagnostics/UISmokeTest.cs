#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Inputs;
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
        private PvZGame? _controller;
        /// <summary>Stores click.</summary>
        private BoundGameMethod? _click;

        /// <summary>Gets the click, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private BoundGameMethod RequiredClick => _click ?? throw new System.InvalidOperationException("The member '_click' is not available.");
        /// <summary>Stores view field.</summary>
        private BoundGameField? _viewField;

        /// <summary>Gets the view field, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private BoundGameField RequiredViewField => _viewField ?? throw new System.InvalidOperationException("The member '_viewField' is not available.");
        /// <summary>Stores options field.</summary>
        private BoundGameField? _optionsField;
        /// <summary>Stores almanac field.</summary>
        private BoundGameField? _almanacField;
        /// <summary>Stores profiles field.</summary>
        private BoundGameField? _profilesField;
        /// <summary>Stores profile dialog field.</summary>
        private BoundGameField? _profileDialogField;
        /// <summary>Stores help field.</summary>
        private BoundGameField? _helpField;
        /// <summary>Stores the independent-mode directory inspected by this smoke scenario.</summary>
        private BoundGameField? _modeField;
        /// <summary>Stores the isolated Adventure progress used to enter the implemented store.</summary>
        private BoundGameField? _adventureStoreField;
        /// <summary>Stores quit field.</summary>
        private BoundGameField? _quitField;
        /// <summary>Stores unavailable field.</summary>
        private BoundGameField? _unavailableField;
        /// <summary>Stores help pointer.</summary>
        private BoundGameMethod? _helpPointer;
        /// <summary>Stores almanac pointer.</summary>
        private BoundGameMethod? _almanacPointer;

        /// <summary>Gets the almanac pointer, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private BoundGameMethod RequiredAlmanacPointer => _almanacPointer ?? throw new System.InvalidOperationException("The member '_almanacPointer' is not available.");
        /// <summary>Stores game field.</summary>
        private BoundGameField? _gameField;
        /// <summary>Stores apply options.</summary>
        private BoundGameMethod? _applyOptions;
        /// <summary>Stores close options.</summary>
        private BoundGameMethod? _closeOptions;
        /// <summary>Stores directory.</summary>
        private string _directory = string.Empty;
        /// <summary>Stores session.</summary>
        private string _session = string.Empty;
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
            _click = GameMemberReflection.BindMethod(_controller, "HandleClick");
            _viewField = GameMemberReflection.BindField(_controller, "_currentView");
            _optionsField = GameMemberReflection.BindField(_controller, "_optionsView");
            _almanacField = GameMemberReflection.BindField(_controller, "_almanacView");
            _profilesField = GameMemberReflection.BindField(_controller, "_profiles");
            _profileDialogField = GameMemberReflection.BindField(_controller, "_profileDialog");
            _helpField = GameMemberReflection.BindField(_controller, "_helpView");
            _modeField = GameMemberReflection.BindField(_controller, "_modeView");
            _adventureStoreField = GameMemberReflection.BindField(_controller, "_adventureStore");
            _quitField = GameMemberReflection.BindField(_controller, "_confirmingQuit");
            _unavailableField = GameMemberReflection.BindField(_controller, "_unavailableFeature");
            _helpPointer = GameMemberReflection.BindMethod(_controller, "HandleHelpPointer");
            _almanacPointer = GameMemberReflection.BindMethod(_controller, "HandleAlmanacPointer");
            _gameField = GameMemberReflection.BindField(_controller, "_game");
            _applyOptions = GameMemberReflection.BindMethod(_controller, "ApplyOptions");
            _closeOptions = GameMemberReflection.BindMethod(_controller, "CloseOptions");
            if (_controller == null || _click == null || _viewField == null || _optionsField == null || _almanacField == null || _profilesField == null || _profileDialogField == null || _helpField == null || _modeField == null || _adventureStoreField == null || _quitField == null || _unavailableField == null || _helpPointer == null || _almanacPointer == null || _gameField == null || _applyOptions == null || _closeOptions == null)
            {
                Fail("UI controller is unavailable");
                yield break;
            }

            var startupDeadline = Time.realtimeSinceStartup + 60f;
            while (_controller.Loading.OriginalLoadingActive && Time.realtimeSinceStartup < startupDeadline)
            {
                yield return null;
            }

            if (_controller.Loading.OriginalLoadingActive)
            {
                Fail("Original startup loading did not complete within 60 seconds");
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
            _helpPointer.Invoke(_controller, new object[] { new Vector2(400, 520), PointerPhase.Down });
            _helpPointer.Invoke(_controller, new object[] { new Vector2(400, 520), PointerPhase.Up });
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
            var modeView = (ModeSelectionView)_modeField.GetValue(_controller);
            var lockedModeNotice = typeof(ModeSelectionView).GetField("_lockedNotice", flags);
            if (modeView == null || lockedModeNotice == null)
            {
                Fail("Mode lock notice is unavailable");
                yield break;
            }

            Click(80, 120);
            if (!ViewIs("Modes") || string.IsNullOrEmpty(lockedModeNotice.GetValue(modeView) as string))
            {
                Fail("Locked mini-game did not show its progression notice");
                yield break;
            }

            // Preserve the historical artifact name for existing screenshot comparison workflows.
            yield return Capture("unimplemented-mode");
            Click(400, 350);
            if (lockedModeNotice.GetValue(modeView) != null)
            {
                Fail("Mode progression notice did not close");
                yield break;
            }

            Click(40, 580);
            if (!ViewIs("Selector"))
            {
                Fail("Mode screen did not return to selector");
                yield break;
            }

            var adventureStore = (AdventureProgressStore)_adventureStoreField.GetValue(_controller);
            if (adventureStore == null || !profiles.SelectedId.HasValue)
            {
                Fail("Isolated store progress is unavailable");
                yield break;
            }

            var storeProfile = profiles.SelectedId.Value;
            var priorStoreProgress = adventureStore.Get(storeProfile);
            try
            {
                adventureStore.Import(storeProfile, 25, 0);
                Click(470, 535);
                if (!ViewIs("Store") || _unavailableField.GetValue(_controller) != null)
                {
                    Fail("Unlocked store entry did not open the store");
                    yield break;
                }

                // Preserve the historical artifact name while capturing the implemented store.
                yield return Capture("unimplemented-store");
                Click(435, 552);
                if (!ViewIs("Selector") || _unavailableField.GetValue(_controller) != null)
                {
                    Fail("Store return button did not return to selector");
                    yield break;
                }
            }
            finally
            {
                adventureStore.Import(storeProfile, priorStoreProgress.NextLevel, priorStoreProgress.CompletedRuns);
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

            AlmanacPointer(720, 580, PointerPhase.Down);
            if (!ViewIs("Almanac"))
            {
                Fail("Almanac close committed on press");
                yield break;
            }

            AlmanacPointer(600, 550, PointerPhase.Up);
            if (!ViewIs("Almanac"))
            {
                Fail("Almanac close did not cancel after dragging out");
                yield break;
            }

            AlmanacPointer(720, 580, PointerPhase.Down);
            AlmanacPointer(720, 580, PointerPhase.Up);
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
            var musicReport = _controller.Music.GetMusicOutputReport();
            var appliedMusic = musicReport.savedMusicVolume;
            options.HandlePointer(new Vector2(515, 183), PointerPhase.Down);
            options.HandlePointer(new Vector2(515, 183), PointerPhase.Up);
            var draftMusic = options.MusicVolume;
            if (Mathf.Approximately(draftMusic, appliedMusic))
            {
                Fail("Music draft did not change");
                yield break;
            }

            options.HandlePointer(new Vector2(400, 310), PointerPhase.Down);
            options.HandlePointer(new Vector2(400, 310), PointerPhase.Up);
            if (options.CloseResult != OptionsCloseResult.OpenAlmanac)
            {
                Fail("Options almanac button did not dispatch");
                yield break;
            }

            _applyOptions.Invoke(_controller, null);
            var pausedGame = (GameSimulation)_gameField.GetValue(_controller);
            var pausedElapsed = pausedGame.Elapsed;
            musicReport = _controller.Music.GetMusicOutputReport();
            if (!ViewIs("Almanac") || options.IsOpen || !Mathf.Approximately(musicReport.savedMusicVolume, appliedMusic))
            {
                Fail("Opening modal almanac applied or lost the options draft; saved=" + musicReport.savedMusicVolume + "; expected=" + appliedMusic);
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

            AlmanacPointer(720, 580, PointerPhase.Down);
            AlmanacPointer(720, 580, PointerPhase.Up);
            musicReport = _controller.Music.GetMusicOutputReport();
            if (!ViewIs("Board") || !options.IsOpen || !Mathf.Approximately(options.MusicVolume, draftMusic) || !Mathf.Approximately(musicReport.savedMusicVolume, appliedMusic))
            {
                Fail("Closing modal almanac did not restore the unchanged options draft; saved=" + musicReport.savedMusicVolume + "; expected=" + appliedMusic);
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

            options.HandlePointer(new Vector2(400, 355), PointerPhase.Down);
            options.HandlePointer(new Vector2(400, 355), PointerPhase.Up);
            if (!options.IsOpen || options.CloseResult != OptionsCloseResult.None)
            {
                Fail("Options restart confirmation did not open");
                yield break;
            }

            _closeOptions.Invoke(_controller, null);
            musicReport = _controller.Music.GetMusicOutputReport();
            if (!options.IsOpen || options.CloseResult != OptionsCloseResult.None || !Mathf.Approximately(musicReport.savedMusicVolume, appliedMusic))
            {
                Fail("Cancelling restart applied or closed the options draft; saved=" + musicReport.savedMusicVolume + "; expected=" + appliedMusic);
                yield break;
            }

            options.HandlePointer(new Vector2(400, 355), PointerPhase.Down);
            options.HandlePointer(new Vector2(400, 355), PointerPhase.Up);
            options.HandlePointer(new Vector2(300, 386), PointerPhase.Down);
            options.HandlePointer(new Vector2(300, 386), PointerPhase.Up);
            if (options.IsOpen || options.CloseResult != OptionsCloseResult.RestartLevel)
            {
                Fail("Options restart confirmation did not dispatch");
                yield break;
            }

            _applyOptions.Invoke(_controller, null);
            musicReport = _controller.Music.GetMusicOutputReport();
            if (!Mathf.Approximately(musicReport.savedMusicVolume, draftMusic))
            {
                Fail("Confirmed options draft was not applied; saved=" + musicReport.savedMusicVolume + "; expected=" + draftMusic);
                yield break;
            }

            Click(750, 23);
            options.HandlePointer(new Vector2(400, 405), PointerPhase.Down);
            options.HandlePointer(new Vector2(400, 405), PointerPhase.Up);
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
            RequiredClick.Invoke(_controller, new object[] { new Vector2(x, y) });
        }

        /// <summary>Performs almanac pointer.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "type">The type.</param>
        private void AlmanacPointer(float x, float y, PointerPhase type)
        {
            RequiredAlmanacPointer.Invoke(_controller, new object[] { new Vector2(x, y), type });
        }

        /// <summary>Performs view is.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting view is.</returns>
        private bool ViewIs(string name)
        {
            return string.Equals(RequiredViewField.GetValue(_controller).ToString(), name, StringComparison.Ordinal);
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
