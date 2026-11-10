using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.UI;

namespace PvZReplica
{
    // Only this explicit QA flag creates these isolated scenarios; never touches the selected real profile.
    internal sealed class FeedbackPlayerSmoke : MonoBehaviour
    {
        private PvZGame controller;
        private string loggedError;
        private int checks;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameSimulation Sim { get { return (GameSimulation)Field("game"); } }
        private object Field(string name) { return typeof(PvZGame).GetField(name, Private).GetValue(controller); }
        private object Call(string name, params object[] args) { return typeof(PvZGame).GetMethod(name, Private).Invoke(controller, args); }
        private object SimCall(string name, params object[] args)
        {
            MethodInfo method = typeof(GameSimulation).GetMethod(name, Private);
            ParameterInfo[] parameters = method.GetParameters();
            if (args.Length < parameters.Length)
            {
                int supplied = args.Length; Array.Resize(ref args, parameters.Length);
                for (int i = supplied; i < parameters.Length; i++) args[i] = parameters[i].DefaultValue;
            }
            return method.Invoke(Sim, args);
        }
        private void Check(bool passed, string message) { checks++; if (!passed) throw new InvalidOperationException(message); }
        private void OnLog(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) loggedError = message; }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-feedback-smoke") >= 0) new GameObject("Feedback Player smoke").AddComponent<FeedbackPlayerSmoke>(); }
        private IEnumerator Start()
        {
            yield return null; controller = FindFirstObjectByType<PvZGame>(); Application.logMessageReceived += OnLog;
            IEnumerator run = Run();
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception error) { Debug.LogError("PVZ_FEEDBACK_SMOKE_FAIL: " + error); SmokeTestArtifacts.Quit(1); yield break; }
                if (!next) break;
                yield return run.Current;
            }
            if (loggedError != null) { Debug.LogError("PVZ_FEEDBACK_SMOKE_FAIL: " + loggedError); SmokeTestArtifacts.Quit(1); }
            else { Debug.Log("PVZ_FEEDBACK_SMOKE_PASS: " + checks + " focused integration checks"); SmokeTestArtifacts.Quit(0); }
        }
        private IEnumerator Capture(string name) { yield return new WaitForEndOfFrame(); SmokeTestArtifacts.CaptureFrame(Path.Combine(SmokeTestArtifacts.DirectoryPath, name + ".png")); }
        private IEnumerator Run()
        {
            float deadline = Time.unscaledTime + 60;
            while (controller.OriginalLoadingActive && Time.unscaledTime < deadline) yield return null;
            Check(!controller.OriginalLoadingActive, "Startup did not finish");
            controller.StartAdventureTestLevel(8); Check(Sim.ChooseDefaultAdventureSeeds(), "Could not choose test seeds");
            typeof(PvZGame).GetField("choosingSeeds", Private).SetValue(controller, false);
            var sunflower = (Plant)SimCall("PlaceAdventurePlant", PlantKind.Sunflower, 1, 1);
            sunflower.Timing = PvZUnity.Timing.PlantTiming.FromRaw(PvZUnity.Timing.PlantKind.Sunflower, 150, 0); sunflower.ActionTimer = 1.5f;
            var twin = (Plant)SimCall("PlaceAdventurePlant", PlantKind.TwinSunflower, 2, 1); twin.ActionTimer = 1.5f;
            var shroom = (Plant)SimCall("PlaceAdventurePlant", PlantKind.SunShroom, 3, 1); shroom.Asleep = false; shroom.ActionTimer = 1.5f;
            var marigold = (Plant)SimCall("PlaceAdventurePlant", PlantKind.Marigold, 4, 1); marigold.ActionTimer = 1.5f;
            var zombie = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 1, 6.5f, 0);
            var door = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.ScreenDoor, 3, 6.6f, 0);
            yield return Capture("00-producers-before-hint"); yield return new WaitForSecondsRealtime(.9f);
            Check(sunflower.ProducerGlow.FlashTicks > 0 && twin.ProducerGlow.FlashTicks > 0 && shroom.ProducerGlow.FlashTicks > 0 && marigold.ProducerGlow.FlashTicks > 0, "Actual production countdown did not start original hint");
            yield return Capture("01-producers-brightening");
            SimCall("HitAdventureZombie", zombie, 100f, false); SimCall("HitAdventureZombie", door, 20f, false);
            Check(zombie.PresentationArmLost && !zombie.Headless && Sim.Presentation.ArmDrops.Count > 0, "Body-health threshold did not detach arm");
            Check(door.PresentationArmorFlash > 0 && door.PresentationFlash == 0, "Door hit also flashed the body");
            yield return Capture("02-arm-drop-and-hit"); yield return new WaitForSecondsRealtime(.1f); yield return Capture("03-hit-decay");
            yield return new WaitForSecondsRealtime(.6f);
            Check(Sim.Suns.Count >= 4 && marigold.ActionCount > 0, "Real sun/coin production did not follow its hint");
            yield return Capture("04-production-complete");
            var movement = new List<float>();
            for (int i = 0; i < 160; i++) { float before = zombie.X; Sim.Update(.01f); movement.Add(before - zombie.X); }
            float low = float.MaxValue, high = float.MinValue; foreach (float move in movement) { low = Math.Min(low, move); high = Math.Max(high, move); }
            Check(high - low > .0001f, "Zombie movement remained linear across the original walking cycle");
            File.WriteAllText(Path.Combine(SmokeTestArtifacts.DirectoryPath, "walk-displacements.txt"), string.Join("\n", movement));
            yield return Capture("05-walk-cycle");
            Call("HandleSelector", SelectorAction.Adventure); Check(Sim.IsAdventure && Sim.Adventure.Level.Number == 1, "Normal menu did not create campaign");
            var final = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 2, 5.4f, 0);
            SimCall("HitAdventureZombie", final, 2000f, false);
            // Force only the end-of-level condition to inspect the real reward UI without replaying a whole level.
            typeof(GameSimulation).GetProperty("State").SetValue(Sim, MatchState.Won);
            Sim.Adventure.Phase = AdventurePhase.Reward; SimCall("SpawnAdventureRewardPresentation");
            var campaign = (AdventureCampaign)Field("adventureCampaign"); Check(campaign.Progress.NextLevel == 1, "A drop saved progress before pickup");
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("06-seed-reward-falling");
            deadline = Time.unscaledTime + 5; while (!Sim.Adventure.RewardPresentation.OnGround && Time.unscaledTime < deadline) yield return null;
            Check(Sim.Adventure.RewardPresentation.OnGround, "Seed reward did not land"); yield return Capture("07-seed-reward-pickup-arrow");
            Call("HandleClick", new Vector2(50, 500)); Check(campaign.Progress.NextLevel == 1, "Unrelated click collected the reward");
            var reward = Sim.Adventure.RewardPresentation; Call("HandleClick", new Vector2(reward.X + 25, reward.Y + 35));
            Check(reward.Stage == AdventureAwardStage.Collecting && campaign.Progress.NextLevel == 2, "Visible seed pickup did not persist and start original animation");
            yield return new WaitForSecondsRealtime(.8f); yield return Capture("08-seed-reward-collecting");
            deadline = Time.unscaledTime + 8; while ((reward.Stage != AdventureAwardStage.Introduction || reward.IntroductionTicks < 180) && Time.unscaledTime < deadline) yield return null;
            Check(reward.Stage == AdventureAwardStage.Introduction, "Original pickup transition did not show introduction");
            Check(!string.IsNullOrEmpty((string)Call("OriginalProgressionText", "SUNFLOWER_TOOLTIP")), "Original plant tooltip missing");
            yield return Capture("09-original-seed-introduction");
            Call("HandleClick", new Vector2(402, 566)); Check(Sim.Adventure.Level.Number == 2 && Sim.State == MatchState.Playing, "Original next button did not advance");
            yield return Capture("10-next-adventure-level");
            var progress = (AdventureProgressStore)Field("adventureStore"); progress.Import(campaign.ProfileId, 1, 1);
            Call("HandleSelector", SelectorAction.Adventure); Check(Sim.Adventure.Level.IsReplay && Sim.ChooseDefaultAdventureSeeds(), "Replay setup failed");
            typeof(PvZGame).GetField("choosingSeeds", Private).SetValue(controller, false);
            typeof(GameSimulation).GetProperty("State").SetValue(Sim, MatchState.Won); Sim.Adventure.Phase = AdventurePhase.Reward;
            SimCall("SpawnAdventureRewardPresentation"); reward = Sim.Adventure.RewardPresentation;
            Call("HandleClick", new Vector2(reward.X + reward.Width / 2, reward.Y + reward.Height / 2));
            Check(reward.NativeType == 18 && reward.Stage == AdventureAwardStage.Collecting, "Replay did not award a moneybag");
            Sim.Update(6.1f); Call("UpdateAdventureRewardContinuation");
            Check(Sim.Adventure.Level.IsReplay && Sim.Adventure.Level.Number == 2, "Replay incorrectly displayed a new-plant award");
            yield return Capture("11-replay-native-continuation"); Check(loggedError == null, "Player reported an error");
        }
    }
}
