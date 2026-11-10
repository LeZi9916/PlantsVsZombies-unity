using System;
using System.Collections;
using System.IO;
using System.Reflection;
using PvZReplica.Profiles;
using PvZReplica.UI;
using UnityEngine;

namespace PvZReplica
{
    // Enabled only for explicit player smoke tests.
    internal sealed class UISmokeTest : MonoBehaviour
    {
        private PvZGame controller;
        private MethodInfo click;
        private FieldInfo viewField;
        private FieldInfo optionsField;
        private FieldInfo almanacField;
        private FieldInfo profilesField;
        private FieldInfo profileDialogField;
        private FieldInfo helpField;
        private FieldInfo quitField;
        private FieldInfo unavailableField;
        private MethodInfo helpPointer;
        private MethodInfo almanacPointer;
        private FieldInfo gameField;
        private MethodInfo applyOptions;
        private MethodInfo closeOptions;
        private FieldInfo musicField;
        private string directory;
        private string session;
        private bool logicOnly;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "-pvz-ui-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-ui-logic-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0)
                new GameObject("UI smoke test").AddComponent<UISmokeTest>();
        }

        private IEnumerator Start()
        {
            yield return null;
            controller = FindFirstObjectByType<PvZGame>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = typeof(PvZGame);
            click = type.GetMethod("HandleClick", flags);
            viewField = type.GetField("currentView", flags);
            optionsField = type.GetField("optionsView", flags);
            almanacField = type.GetField("almanacView", flags);
            profilesField = type.GetField("profiles", flags);
            profileDialogField = type.GetField("profileDialog", flags);
            helpField = type.GetField("helpView", flags);
            quitField = type.GetField("confirmingQuit", flags);
            unavailableField = type.GetField("unavailableFeature", flags);
            helpPointer = type.GetMethod("HandleHelpPointer", flags);
            almanacPointer = type.GetMethod("HandleAlmanacPointer", flags);
            gameField = type.GetField("game", flags);
            applyOptions = type.GetMethod("ApplyOptions", flags);
            closeOptions = type.GetMethod("CloseOptions", flags);
            musicField = type.GetField("musicSource", flags);
            if (controller == null || click == null || viewField == null || optionsField == null ||
                almanacField == null || profilesField == null || profileDialogField == null || helpField == null ||
                quitField == null || unavailableField == null || helpPointer == null || almanacPointer == null || gameField == null ||
                applyOptions == null || closeOptions == null || musicField == null) { Fail("UI controller is unavailable"); yield break; }
            directory = SmokeTestArtifacts.DirectoryPath;
            session = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            string[] arguments = Environment.GetCommandLineArgs();
            logicOnly = Array.IndexOf(arguments, "-pvz-ui-logic-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0;
            if (!ViewIs("Selector")) { Fail("Startup did not open selector"); yield break; }
            if (Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0)
            {
                ProfileDialog initialDialog = (ProfileDialog)profileDialogField.GetValue(controller);
                ProfileCatalog initialProfiles = (ProfileCatalog)profilesField.GetValue(controller);
                if (initialDialog == null || !initialDialog.IsOpen || initialProfiles.Profiles.Count != 0)
                {
                    Fail("First launch did not request a user");
                    yield break;
                }
                yield return Capture("first-user");
                FieldInfo initialName = typeof(ProfileDialog).GetField("editName", flags);
                if (initialName == null) { Fail("Profile name input is unavailable"); yield break; }
                initialName.SetValue(initialDialog, "首次用户");
                initialDialog.HandleClick(new Vector2(300, 470));
                if (initialDialog.IsOpen || initialProfiles.Profiles.Count != 1 || !initialProfiles.SelectedId.HasValue)
                {
                    Fail("First user was not created and selected");
                    yield break;
                }
                Debug.Log((logicOnly ? "PVZ_FIRST_USER_LOGIC_SMOKE_PASS" : "PVZ_FIRST_USER_SMOKE_PASS") +
                    ": first-run prompt and isolated profile creation" + (logicOnly ? "; no screenshot captured" : ""));
                controller.RequestQuit(0);
                yield break;
            }
            yield return Capture("selector");
            if (!logicOnly)
            {
                string selectorPath = ScreenshotPath("selector");
                if (!File.Exists(selectorPath)) { Fail("Selector screenshot missing"); yield break; }
                Texture2D screenshot = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!screenshot.LoadImage(File.ReadAllBytes(selectorPath)))
                {
                    Destroy(screenshot);
                    Fail("Selector screenshot could not be decoded");
                    yield break;
                }
                Color background = screenshot.GetPixel((int)(screenshot.width * 0.43f), (int)(screenshot.height * 0.5f));
                Destroy(screenshot);
                if (background.r + background.g + background.b < 0.08f)
                {
                    Fail("Selector background mask rendered black");
                    yield break;
                }
            }

            Click(100, 150);
            ProfileDialog profileDialog = (ProfileDialog)profileDialogField.GetValue(controller);
            ProfileCatalog profiles = (ProfileCatalog)profilesField.GetValue(controller);
            if (profileDialog == null || !profileDialog.IsOpen) { Fail("Profile dialog did not open"); yield break; }
            yield return Capture("profiles");
            profileDialog.HandleClick(new Vector2(300, 244));
            FieldInfo editName = typeof(ProfileDialog).GetField("editName", flags);
            if (editName == null) { Fail("Profile name input is unavailable"); yield break; }
            editName.SetValue(profileDialog, "界面测试");
            yield return Capture("profile-create");
            profileDialog.HandleClick(new Vector2(300, 470));
            if (profileDialog.IsOpen || profiles.Profiles.Count != 2 || !profiles.SelectedId.HasValue ||
                profiles.Profiles[1].Id != profiles.SelectedId.Value)
            {
                Fail("Creating and selecting a profile failed");
                yield break;
            }

            Click(670, 515);
            if (!ViewIs("Help") || !((HelpView)helpField.GetValue(controller)).IsOpen)
            {
                Fail("Help did not open");
                yield break;
            }
            yield return Capture("help");
            helpPointer.Invoke(controller, new object[] { new Vector2(400, 520), EventType.MouseDown });
            helpPointer.Invoke(controller, new object[] { new Vector2(400, 520), EventType.MouseUp });
            if (!ViewIs("Selector")) { Fail("Help did not return to selector"); yield break; }

            Click(500, 215);
            if (!ViewIs("Modes")) { Fail("Mini-games did not open"); yield break; }
            yield return Capture("modes");
            Click(80, 120);
            yield return Capture("unimplemented-mode");
            Click(400, 350);
            Click(40, 580);
            if (!ViewIs("Selector")) { Fail("Mode screen did not return to selector"); yield break; }

            Click(470, 535);
            if (!ViewIs("Selector") || !string.Equals(unavailableField.GetValue(controller) as string,
                "商店", StringComparison.Ordinal))
            {
                Fail("Corrected store entry did not show its unavailable notice"); yield break;
            }
            yield return Capture("unimplemented-store");
            Click(400, 350);
            if (unavailableField.GetValue(controller) != null)
            {
                Fail("Store notice did not close"); yield break;
            }

            Click(360, 460);
            if (!ViewIs("Almanac")) { Fail("Almanac did not open"); yield break; }
            yield return Capture("almanac-home");
            Click(200, 365);
            AlmanacView almanac = (AlmanacView)almanacField.GetValue(controller);
            if (almanac.Screen != AlmanacScreen.PlantIndex && almanac.Screen != AlmanacScreen.PlantDetail)
            {
                Fail("Plant index did not open");
                yield break;
            }
            yield return Capture("almanac-plants");
            for (int id = 0; id < 49; id++)
            {
                Click(id == 48 ? 21 : 27 + id % 8 * 52, id == 48 ? 24 : 93 + id / 8 * 78);
                if (almanac.SelectedId != id) { Fail("Almanac plant selection failed: " + id); yield break; }
                if (!logicOnly) yield return null;
                if (id == 18) yield return Capture("almanac-threepeater");
                if (id == 23) yield return Capture("almanac-tallnut");
                if (id == 35) yield return Capture("almanac-coffee");
                if (id == 40) yield return Capture("almanac-gatling");
            }
            Click(50, 580);
            Click(550, 365);
            if (almanac.Screen != AlmanacScreen.ZombieDetail || almanac.SelectedId != 0)
            {
                Fail("Almanac zombie category did not default to normal zombie"); yield break;
            }
            yield return Capture("almanac-zombies");
            for (int id = 0; id < 26; id++)
            {
                Click(id == 25 ? 193 : 23 + id % 5 * 85, id == 25 ? 487 : 87 + id / 5 * 80);
                if (almanac.SelectedId != id) { Fail("Almanac zombie selection failed: " + id); yield break; }
                if (!logicOnly) yield return null;
                if (id == 1) yield return Capture("almanac-flag");
                if (id == 24) yield return Capture("almanac-imp");
                if (id == 25) yield return Capture("almanac-boss");
            }
            Click(50, 580);
            Click(200, 365);
            if (almanac.SelectedId != 48) { Fail("Almanac lost the previous plant selection"); yield break; }
            Click(50, 580);
            Click(550, 365);
            if (almanac.SelectedId != 25) { Fail("Almanac lost the previous zombie selection"); yield break; }
            if (almanac.Diagnostics.Count > 0)
            {
                Fail("Almanac portrait/resource diagnostics: " + string.Join(" | ", almanac.Diagnostics)); yield break;
            }
            AlmanacPointer(720, 580, EventType.MouseDown);
            if (!ViewIs("Almanac")) { Fail("Almanac close committed on press"); yield break; }
            AlmanacPointer(600, 550, EventType.MouseUp);
            if (!ViewIs("Almanac")) { Fail("Almanac close did not cancel after dragging out"); yield break; }
            AlmanacPointer(720, 580, EventType.MouseDown);
            AlmanacPointer(720, 580, EventType.MouseUp);
            if (!ViewIs("Selector")) { Fail("Almanac did not return to selector on release"); yield break; }

            Click(500, 105);
            if (!ViewIs("Board")) { Fail("Adventure entry did not open prototype board"); yield break; }
            Click(750, 23);
            OptionsView options = (OptionsView)optionsField.GetValue(controller);
            if (!options.IsOpen) { Fail("Options did not open"); yield break; }
            yield return Capture("options");
            AudioSource music = (AudioSource)musicField.GetValue(controller);
            float appliedMusic = music.volume;
            options.HandlePointer(new Vector2(515, 183), EventType.MouseDown);
            options.HandlePointer(new Vector2(515, 183), EventType.MouseUp);
            float draftMusic = options.MusicVolume;
            if (Mathf.Approximately(draftMusic, appliedMusic)) { Fail("Music draft did not change"); yield break; }
            options.HandlePointer(new Vector2(400, 310), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 310), EventType.MouseUp);
            if (options.CloseResult != OptionsCloseResult.OpenAlmanac)
            {
                Fail("Options almanac button did not dispatch"); yield break;
            }
            applyOptions.Invoke(controller, null);
            GameSimulation pausedGame = (GameSimulation)gameField.GetValue(controller);
            float pausedElapsed = pausedGame.Elapsed;
            if (!ViewIs("Almanac") || options.IsOpen || !Mathf.Approximately(music.volume, appliedMusic))
            {
                Fail("Opening modal almanac applied or lost the options draft"); yield break;
            }
            yield return Capture("almanac-from-options");
            for (int frame = 0; frame < 6; frame++) yield return null;
            if (pausedGame.Elapsed != pausedElapsed) { Fail("Combat advanced behind the almanac"); yield break; }
            AlmanacPointer(720, 580, EventType.MouseDown);
            AlmanacPointer(720, 580, EventType.MouseUp);
            if (!ViewIs("Board") || !options.IsOpen || !Mathf.Approximately(options.MusicVolume, draftMusic) ||
                !Mathf.Approximately(music.volume, appliedMusic))
            {
                Fail("Closing modal almanac did not restore the unchanged options draft"); yield break;
            }
            yield return Capture("almanac-return-options");
            for (int frame = 0; frame < 6; frame++) yield return null;
            if (pausedGame.Elapsed != pausedElapsed) { Fail("Closing almanac resumed combat before options closed"); yield break; }
            options.HandlePointer(new Vector2(400, 355), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 355), EventType.MouseUp);
            if (!options.IsOpen || options.CloseResult != OptionsCloseResult.None)
            {
                Fail("Options restart confirmation did not open");
                yield break;
            }
            closeOptions.Invoke(controller, null);
            if (!options.IsOpen || options.CloseResult != OptionsCloseResult.None ||
                !Mathf.Approximately(music.volume, appliedMusic))
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
            applyOptions.Invoke(controller, null);
            if (!Mathf.Approximately(music.volume, draftMusic)) { Fail("Confirmed options draft was not applied"); yield break; }
            Click(750, 23);
            options.HandlePointer(new Vector2(400, 405), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 405), EventType.MouseUp);
            if (options.IsOpen || options.CloseResult != OptionsCloseResult.MainMenu)
            {
                Fail("Options main menu button did not dispatch");
                yield break;
            }
            applyOptions.Invoke(controller, null);
            if (!ViewIs("Selector")) { Fail("Options main menu did not navigate"); yield break; }
            Click(740, 550);
            if (!(bool)quitField.GetValue(controller)) { Fail("Quit confirmation did not open"); yield break; }
            yield return Capture("quit");
            Click(480, 370);
            if ((bool)quitField.GetValue(controller)) { Fail("Quit cancellation failed"); yield break; }
            Debug.Log((logicOnly ? "PVZ_UI_LOGIC_SMOKE_PASS" : "PVZ_UI_SMOKE_PASS") +
                ": selector, profiles, help, modes, corrected store/almanac entries, 75 almanac selections, modal options pause/return, options, restart, main menu and quit cancellation" +
                (logicOnly ? "; no screenshots captured" : ""));
            controller.RequestQuit(0);
        }

        private IEnumerator Capture(string name)
        {
            if (logicOnly) yield break;
            yield return new WaitForEndOfFrame();
            string path = ScreenshotPath(name);
            SmokeTestArtifacts.CaptureFrame(path);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
            if (!File.Exists(path)) Fail("Screenshot was not written: " + name);
        }

        private string ScreenshotPath(string name) { return Path.Combine(directory, "ui-" + name + "-" + session + ".png"); }
        private void Click(float x, float y) { click.Invoke(controller, new object[] { new Vector2(x, y) }); }
        private void AlmanacPointer(float x, float y, EventType type)
        {
            almanacPointer.Invoke(controller, new object[] { new Vector2(x, y), type });
        }
        private bool ViewIs(string name) { return string.Equals(viewField.GetValue(controller).ToString(), name, StringComparison.Ordinal); }
        private static void Fail(string message) { Debug.LogError("PVZ_UI_SMOKE_FAIL: " + message); SmokeTestArtifacts.Quit(1); }
    }
}
