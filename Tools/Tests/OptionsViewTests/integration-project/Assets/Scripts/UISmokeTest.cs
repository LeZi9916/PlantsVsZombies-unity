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
        private MethodInfo helpPointer;
        private MethodInfo applyOptions;
        private string directory;
        private string session;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "-pvz-ui-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0)
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
            helpPointer = type.GetMethod("HandleHelpPointer", flags);
            applyOptions = type.GetMethod("ApplyOptions", flags);
            if (controller == null || click == null || viewField == null || optionsField == null ||
                almanacField == null || profilesField == null || profileDialogField == null || helpField == null ||
                quitField == null || helpPointer == null ||
                applyOptions == null) { Fail("UI controller is unavailable"); yield break; }
            directory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            session = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            if (!ViewIs("Selector")) { Fail("Startup did not open selector"); yield break; }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-first-user-smoke") >= 0)
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
                Debug.Log("PVZ_FIRST_USER_SMOKE_PASS: first-run prompt and isolated profile creation");
                Application.Quit(0);
                yield break;
            }
            yield return Capture("selector");
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

            Click(450, 540);
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
            if (almanac.Diagnostics.Count > 0)
                Debug.LogWarning("PVZ_UI_SMOKE_ALMANAC_DIAGNOSTICS: " + string.Join(" | ", almanac.Diagnostics));
            Click(720, 580);
            if (!ViewIs("Selector")) { Fail("Almanac did not return to selector"); yield break; }

            Click(500, 105);
            if (!ViewIs("Board")) { Fail("Adventure entry did not open prototype board"); yield break; }
            Click(750, 23);
            OptionsView options = (OptionsView)optionsField.GetValue(controller);
            if (!options.IsOpen) { Fail("Options did not open"); yield break; }
            yield return Capture("options");
            options.HandlePointer(new Vector2(400, 355), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 355), EventType.MouseUp);
            if (!options.IsOpen || options.CloseResult != OptionsCloseResult.None)
            {
                Fail("Restart confirmation did not open");
                yield break;
            }
            yield return Capture("options-restart");
            options.HandlePointer(new Vector2(450, 380), EventType.MouseDown);
            options.HandlePointer(new Vector2(450, 380), EventType.MouseUp);
            if (!options.IsOpen) { Fail("Restart cancellation closed options"); yield break; }
            options.HandlePointer(new Vector2(400, 355), EventType.MouseDown);
            options.HandlePointer(new Vector2(400, 355), EventType.MouseUp);
            options.HandlePointer(new Vector2(250, 380), EventType.MouseDown);
            options.HandlePointer(new Vector2(250, 380), EventType.MouseUp);
            if (options.IsOpen || options.CloseResult != OptionsCloseResult.RestartLevel)
            {
                Fail("Options restart button did not dispatch");
                yield break;
            }
            applyOptions.Invoke(controller, null);
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
            Click(605, 505);
            if (!options.IsOpen) { Fail("Selector options did not open"); yield break; }
            yield return Capture("options-selector");
            options.HandlePointer(new Vector2(300, 400), EventType.MouseDown);
            options.HandlePointer(new Vector2(300, 400), EventType.MouseUp);
            if (!options.IsOpen) { Fail("Selector options exposed a gameplay action"); yield break; }
            options.Close();
            applyOptions.Invoke(controller, null);
            Click(740, 550);
            if (!(bool)quitField.GetValue(controller)) { Fail("Quit confirmation did not open"); yield break; }
            yield return Capture("quit");
            Click(480, 370);
            if ((bool)quitField.GetValue(controller)) { Fail("Quit cancellation failed"); yield break; }
            Debug.Log("PVZ_UI_SMOKE_PASS: selector, profiles, help, modes, almanac, options, restart, main menu and quit cancellation");
            Application.Quit(0);
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = ScreenshotPath(name);
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
            if (!File.Exists(path)) Fail("Screenshot was not written: " + name);
        }

        private string ScreenshotPath(string name) { return Path.Combine(directory, "ui-" + name + "-" + session + ".png"); }
        private void Click(float x, float y) { click.Invoke(controller, new object[] { new Vector2(x, y) }); }
        private bool ViewIs(string name) { return string.Equals(viewField.GetValue(controller).ToString(), name, StringComparison.Ordinal); }
        private static void Fail(string message) { Debug.LogError("PVZ_UI_SMOKE_FAIL: " + message); Application.Quit(1); }
    }
}
