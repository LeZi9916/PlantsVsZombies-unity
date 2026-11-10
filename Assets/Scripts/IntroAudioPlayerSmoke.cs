using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Profiles;
using PvZReplica.UI;
namespace PvZReplica
{
    // Explicit isolated opening/audio/card inspection. Normal player never creates these fixtures.
    internal sealed class IntroAudioPlayerSmoke : MonoBehaviour
    {
        private PvZGame controller; private int checks; private string errorLog;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private object Field(string name) { return typeof(PvZGame).GetField(name, Private).GetValue(controller); }
        private object Call(string name, params object[] args) { return typeof(PvZGame).GetMethod(name, Private).Invoke(controller, args); }
        private GameSimulation Sim { get { return (GameSimulation)Field("game"); } }
        private AdventureIntroClock Opening { get { return (AdventureIntroClock)Field("adventureIntro"); } }
        private void Check(bool yes,string reason) { checks++; if (!yes) throw new InvalidOperationException(reason); }
        private void OnLog(string message,string stack,LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errorLog = message; }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-intro-audio-smoke") >= 0) new GameObject("Opening audio inspection").AddComponent<IntroAudioPlayerSmoke>(); }
        private IEnumerator Start()
        {
            yield return null; controller = FindFirstObjectByType<PvZGame>(); Application.logMessageReceived += OnLog;
            IEnumerator run = Run(); while (true) { bool more; try { more = run.MoveNext(); } catch (Exception error) { Debug.LogError("PVZ_INTRO_AUDIO_FAIL: " + error); SmokeTestArtifacts.Quit(1); yield break; } if (!more) break; yield return run.Current; }
            if (errorLog != null) { Debug.LogError("PVZ_INTRO_AUDIO_FAIL: " + errorLog); SmokeTestArtifacts.Quit(1); }
            else { Debug.Log("PVZ_INTRO_AUDIO_PASS: " + checks + " focused host checks"); SmokeTestArtifacts.Quit(0); }
        }
        private IEnumerator Capture(string name) { yield return new WaitForEndOfFrame(); SmokeTestArtifacts.CaptureFrame(Path.Combine(SmokeTestArtifacts.DirectoryPath, name + ".png")); }
        private void StartLevel(int level)
        {
            var catalog = (ProfileCatalog)Field("profiles"); var progress = (AdventureProgressStore)Field("adventureStore");
            progress.Import(catalog.SelectedId.Value, level, 0); Call("HandleSelector", SelectorAction.Adventure);
        }
        private IEnumerator Run()
        {
            float timeout = Time.unscaledTime + 60; while (controller.OriginalLoadingActive && Time.unscaledTime < timeout) yield return null;
            Check(!controller.OriginalLoadingActive, "Loading never completed");
            Call("HandleSelector", SelectorAction.Adventure); Check(Opening != null && !Opening.Complete, "Normal adventure skipped opening");
            controller.BeginMusicOutputProbe(Path.Combine(SmokeTestArtifacts.DirectoryPath, "actual-music-output.json"), 3f);
            Check(Opening.Frame.BoardX == 220 && Sim.Elapsed == 0, "Initial house/lawn camera or gameplay gate wrong");
            yield return Capture("00-house-before-pan");
            timeout = Time.unscaledTime + 5; while (Opening.NativeTime < 2450 && Time.unscaledTime < timeout) yield return null;
            Check(Opening.Frame.BoardX < 220 && Opening.Frame.BoardX > -380 && Sim.Elapsed == 0, "Original camera curve or frozen gameplay missing");
            yield return Capture("01-pan-to-street");
            while (Opening.NativeTime < 3650) yield return null; Check(Opening.Frame.BoardX == -380, "Street endpoint wrong");
            yield return Capture("02-street-zombie-preview");
            while (Opening.NativeTime < 7000) yield return null;
            Check(Opening.Frame.SodPosition >= 500 && Opening.Frame.SodPosition < 700 && Sim.Elapsed == 0, "First row sod was fully drawn before rolling");
            yield return Capture("03-first-row-sod-rolling");
            while (!Opening.Complete) yield return null; yield return Capture("04-first-row-ready");
            Check(Opening.Frame.SodPosition == 1000 && Opening.Frame.BoardX == 0, "Opening did not restore normal lawn");
            string audioFile = Path.Combine(SmokeTestArtifacts.DirectoryPath, "actual-music-output.json");
            Check(File.Exists(audioFile), "PCM report was not written"); var report = JsonUtility.FromJson<PvZGame.MusicOutputReport>(File.ReadAllText(audioFile));
            Check(report.status == "PASS" && report.sourcePcmNonzero && report.musicOnlyPcmNonzero && report.listeners == 1, "Actual music output stayed silent");
            foreach (int level in new[] { 2, 4 })
            {
                StartLevel(level); yield return Capture("05-level-" + level + "-before-sod"); Call("UpdateAdventureOpening", 7f);
                Check(Opening.Frame.SodPosition >= 500 && Opening.Frame.SodPosition < 650, "Native new-sod level was skipped");
                yield return Capture("06-level-" + level + "-sod-rolling");
            }
            StartLevel(8); yield return Capture("07-chooser-home"); Call("UpdateAdventureOpening", 4.3f);
            Check(Opening.WaitingForSeeds && Opening.NativeTime == 4250 && Sim.Elapsed == 0, "Chooser did not stop the native intro clock");
            yield return Capture("08-native-chooser-with-preview"); Call("UpdateAdventureOpening", .5f);
            Check(Opening.NativeTime == 4250, "Clock advanced while waiting for player seeds");
            for (int i = 0; i < 6; i++) Call("HandleClick", new Vector2(47 + i * 53, 163));
            Call("HandleClick", new Vector2(232, 566));
            Check(!Opening.WaitingForSeeds && Opening.NativeTime >= 4260 && Sim.Elapsed == 0, "Chooser confirmation started the battle before returning");
            Call("UpdateAdventureOpening", .9f); yield return Capture("09-return-from-street");
            Call("UpdateAdventureOpening", 1.4f); Check(Opening.Frame.Stage == AdventureIntroStage.ReadySetPlant, "Ready set plant original reanim absent");
            yield return Capture("10-original-ready-set-plant"); while (!Opening.Complete) yield return null;
            typeof(GameSimulation).GetProperty("SunAmount").SetValue(Sim, 500);
            var cooldowns = (int[])typeof(GameSimulation).GetField("seedCooldownTicks", Private).GetValue(Sim);
            cooldowns[0] = 751; yield return Capture("11-seed-native-full-cooldown");
            cooldowns[0] = 376; yield return Capture("12-seed-native-half-cooldown");
            Sim.Adventure.SeedFlashes.Clear(); cooldowns[0] = 2; Sim.Update(.01f);
            Check(cooldowns[0] == 1 && !Sim.Adventure.SeedFlashes.Exists(x => x.Kind == PlantKind.Peashooter), "Cooldown completed one tick early");
            Sim.Update(.01f); Check(cooldowns[0] == 0 && Sim.Adventure.SeedFlashes.Exists(x => x.Kind == PlantKind.Peashooter), "Actual cooldown completion never requested original flash");
            yield return new WaitForSecondsRealtime(.08f); yield return Capture("13-original-seed-ready-flash");
            var sun = (Sun)typeof(GameSimulation).GetMethod("CreateAdventurePresentationDroppedSun", Private).Invoke(Sim, new object[] { 1f, 0f, 25 });
            Sim.Suns.Add(sun); Check(Sim.CollectSun(sun) && sun.PresentationPickup.Collecting, "Test sunlight did not start real pickup flight");
            yield return Capture("14-sun-above-seed-bank");
            controller.tooltipInspection = new OriginalTooltipRequest { Title = (string)Call("OriginalProgressionText", "SUNFLOWER"), Body = (string)Call("OriginalProgressionText", "SUNFLOWER_TOOLTIP"), Warning = (string)Call("OriginalProgressionText", "WAITING_TO_REFRESH"), X = 155, Y = 85, Centered = true };
            yield return Capture("14b-original-card-tooltip");
            var tip = (OriginalTooltip)Field("originalTooltip"); Check(tip.LastLayout != null && tip.LastError == null, "Native tooltip fonts/layout failed"); controller.tooltipInspection = null;
            Call("SetUnavailable", "提示", "此操作暂时无法继续，请返回后重试。"); yield return Capture("15-original-message-dialog");
            typeof(PvZGame).GetField("unavailableFeature", Private).SetValue(controller, null);
            var catalog = (ProfileCatalog)Field("profiles"); var progress = (AdventureProgressStore)Field("adventureStore");
            progress.Import(catalog.SelectedId.Value,25,0);
            var accounts = (PvZReplica.Progression.PlayerAccountStore)Field("accountStore"); accounts.CreditBattleCoins(catalog.SelectedId.Value,"ui-material-probe",20000);
            Call("OpenStore"); yield return Capture("16-original-store-buttons");
            Call("HandleClick", new Vector2(407,350)); Check((int)Field("selectedShopItem") >= 0,"Original shop control stopped opening the confirmation");
            yield return Capture("17-original-purchase-dialog");
            var view = typeof(PvZGame).GetField("currentView", Private); view.SetValue(controller,Enum.Parse(view.FieldType,"Import"));
            yield return Capture("18-original-import-controls");
            progress.Import(catalog.SelectedId.Value,1,1); Check((bool)Call("StartIndependentMode",61),"Zombie-card mode did not open"); yield return Capture("19-original-zombie-seed-cards");
            Check(errorLog == null, "Player logged an error");
        }
    }
}
