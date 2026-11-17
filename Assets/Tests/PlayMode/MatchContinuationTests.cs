#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Tests.PlayMode.Smoke;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks production exit, suspension and native continuation using disposable profile files.</summary>
    public sealed class MatchContinuationTests : PlayerSmokeFixture
    {
        /// <summary>Checks a running survival board survives the real options/main-menu path and modal cancellation.</summary>
        /// <returns>Frames for production loading and the native continued-game dialog.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator SurvivalExitReentryRetainsBoardAndOriginalDialogLifecycle()
        {
            yield return StartMode(1);
            var game = Controller.RequiredGame;
            var session = game.ModeSession!;
            var plant = new Plant { Id = 88001, Kind = PlantKind.Wallnut, Row = 2, Column = 4, Health = 1789f };
            SimulationFixture.Plants(game).Add(plant);
            SimulationFixture.Zombies(game).Add(new Zombie { Id = 88002, Row = 2, X = 7f, Health = 137f });
            SimulationInspection.GetProperty("SunAmount").SetValue(game, 777);
            game.Adventure.CurrentWave = 3;
            game.Update(.13f);
            var id = Controller.RequiredProfiles.RequiredSelectedId;
            var run = session.RunId;
            ExitThroughOptions();
            Assert.That(Controller._currentView, Is.EqualTo(PvZGame.ScreenView.Selector));
            Assert.That(Controller.Continuation.Store.Exists(id, 1), Is.True);
            var expected = game.CaptureCheckpoint();
            Assert.That(Controller.RequiredModeView.LaunchRequested!(1), Is.True);
            Assert.That(Controller.Continuation.DialogOpen, Is.True);
            Assert.That(Controller.Opening.AdventureIntroActive, Is.False);
            Assert.That(Controller.RequiredGame.CaptureCheckpoint(), Is.EqualTo(expected), "Attaching must not consume random draws or reinitialize the lawn.");
            Assert.That(Controller.RequiredGame.ModeSession!.RunId, Is.EqualTo(run));
            var elapsed = Controller.RequiredGame.Elapsed;
            yield return null;
            Assert.That(Controller.RequiredGame.Elapsed, Is.EqualTo(elapsed), "The native continue dialog suspends gameplay.");
            yield return SmokeTestArtifacts.WaitForFrame();
            SmokeTestArtifacts.CaptureFrame(Path.Combine(SmokeTestArtifacts.DirectoryPath, "continue-dialog.png"));
            Controller.Continuation.Cancel();
            Assert.That(Controller._currentView, Is.EqualTo(PvZGame.ScreenView.Modes));
            Assert.That(Controller.Continuation.Store.Exists(id, 1), Is.True, "Cancel preserves the continuation.");
            Assert.That(Controller.RequiredModeView.LaunchRequested!(1), Is.True);
            Controller.Continuation.Accept();
            Assert.That(Controller.Continuation.DialogOpen, Is.False);
            Assert.That(Controller.Continuation.Store.Exists(id, 1), Is.False, "PC 43EDF0 erases an ordinary accepted continuation.");
            Assert.That(Controller.RequiredGame.Plants.Single(item => item.Id == plant.Id).Health, Is.EqualTo(1789f));
            Assert.That(Controller.RequiredGame.SunAmount, Is.EqualTo(777));
            Assert.That(Controller.RequiredGame.Zombies.Single(item => item.Id == 88002).Health, Is.EqualTo(137f));
            Assert.That(Controller.RequiredGame.Adventure.CurrentWave, Is.EqualTo(3));
            yield return WaitForBattlePreparation();
            var preparedElapsed = Controller.RequiredGame.Elapsed;
            yield return null;
            Assert.That(Controller.RequiredGame.Elapsed, Is.GreaterThan(elapsed));
            Assert.That(Controller.RequiredGame.Elapsed, Is.GreaterThan(preparedElapsed),
                "Accepted continuation must advance on the first measured frame after preparation completes.");
        }

        /// <summary>Checks native survival update-one autosave remains available throughout the next chooser.</summary>
        /// <returns>Frames for the actual game host.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator SurvivalBoundaryCheckpointSurvivesRepickAndLossErasesIt()
        {
            yield return StartMode(1);
            var game = Controller.RequiredGame;
            game.Adventure.CurrentWave = game.Adventure.Waves.Count;
            SimulationInspection.GetMethod("ClearModeStage").Invoke(game, null);
            Controller.IndependentModes.TickModes(.01f);
            for (var tick = 0; tick < 499; tick++)
            {
                Controller.IndependentModes.TickModes(.01f);
            }
            Assert.That(Controller.IndependentModes.SurvivalTransitionTicks, Is.EqualTo(1));
            var id = Controller.RequiredProfiles.RequiredSelectedId;
            var saved = Controller.Continuation.Store.Load(id, 1, out _);
            Assert.That(saved.ModeSession!.ReSelectionTicks, Is.EqualTo(1));
            Assert.That(saved.ModeSession.HasNextStage, Is.True);
            Controller._currentView = PvZGame.ScreenView.Modes;
            Assert.That(Controller.RequiredModeView.LaunchRequested!(1), Is.True);
            Controller.Continuation.Accept();
            Assert.That(Controller.Continuation.Store.Exists(id, 1), Is.True);
            Controller.IndependentModes.TickModes(.01f);
            Assert.That(Controller.RequiredGame.ModeSession!.Stage, Is.EqualTo(2));
            Assert.That(Controller._choosingSeeds, Is.True);
            Assert.That(Controller.Continuation.TrySaveActive(), Is.True);
            Assert.That(Controller.Continuation.Store.Load(id, 1, out _).ModeSession!.ReSelectionTicks, Is.EqualTo(1));
            Controller.RequiredGame.ModeSession.Phase = ModePhase.Lost;
            SimulationInspection.GetProperty("State").SetValue(Controller.RequiredGame, MatchState.Lost);
            Controller.Continuation.DiscardFinishedCheckpoint();
            Assert.That(Controller.Continuation.Store.Exists(id, 1), Is.False);
        }

        /// <summary>Checks application suspension saves and confirmed restart discards the saved formation.</summary>
        /// <returns>Production loading and continuation presentation frames.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator SuspensionSavesAndConfirmedRestartStartsANewAttempt()
        {
            yield return StartMode(1);
            var run = Controller.RequiredGame.ModeSession!.RunId;
            typeof(PvZGame).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Controller, new object[] { true });
            var id = Controller.RequiredProfiles.RequiredSelectedId;
            Assert.That(Controller.Continuation.Store.Exists(id, 1), Is.True);
            Controller._currentView = PvZGame.ScreenView.Selector;
            Assert.That(Controller.RequiredModeView.LaunchRequested!(1), Is.True);
            var no = Controller._presenter.RequiredContinueDialog.NoBounds.center;
            SetPointerFrame(no);
            Controller.Continuation.HandleInput();
            Assert.That(Controller.Continuation.ConfirmingRestart, Is.True);
            Controller.Continuation.Cancel();
            Assert.That(Controller.Continuation.DialogOpen, Is.True);
            SetPointerFrame(no);
            Controller.Continuation.HandleInput();
            Controller.Continuation.Accept();
            Assert.That(Controller.Continuation.Store.Exists(id, 1), Is.False);
            Assert.That(Controller.RequiredGame.ModeSession!.RunId, Is.Not.EqualTo(run));
            typeof(GameInput).GetProperty(nameof(GameInput.Current))!.SetValue(null, default(GameInputSnapshot));
        }

        /// <summary>Checks an adventure vase interlude restores its existing message and pose without re-entering Dave.</summary>
        /// <returns>Production loading and a continued dialogue frame.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator AdventureVaseDialogueResumesItsSavedNativePose()
        {
            yield return StartMode(1);
            Controller._currentView = PvZGame.ScreenView.Selector;
            Controller.Campaign.RequiredAdventureStore.Import(Controller.RequiredProfiles.RequiredSelectedId, 35, 1);
            Assert.That(Controller.Campaign.StartAdventureCampaign(), Is.True);
            Controller.RequiredGame.Adventure.VaseInterludeActive = true;
            Controller.RequiredGame.Adventure.VaseStage = 1;
            Controller.Opening.StartVaseInterlude(2700);
            Controller.Dave._crazyDaveAnimationAge = .23f;
            Controller.WaveNotices._hugeWaveNoticeStart = .07f;
            Assert.That(Controller.Continuation.TrySaveActive(), Is.True);
            var expected = Controller.RequiredGame.CaptureCheckpoint();
            var message = Controller.Dave._crazyDaveMessageIndex;
            var animation = Controller.Dave._crazyDaveAnimation;
            Controller._currentView = PvZGame.ScreenView.Selector;
            Assert.That(Controller.Campaign.StartAdventureCampaign(), Is.True);
            Assert.That(Controller.Continuation.DialogOpen, Is.True);
            Assert.That(Controller.Opening._vaseDialogueActive, Is.True);
            Assert.That(Controller.Dave._crazyDaveMessageIndex, Is.EqualTo(message));
            Assert.That(Controller.Dave._crazyDaveAnimation, Is.EqualTo(animation));
            Assert.That(Controller.Dave._crazyDaveAnimationAge, Is.EqualTo(.23f));
            Assert.That(Controller.WaveNotices._hugeWaveNoticeStart, Is.EqualTo(.07f));
            Assert.That(Controller.RequiredGame.CaptureCheckpoint(), Is.EqualTo(expected));
            Controller.Continuation.Accept();
            yield return null;
            Assert.That(Controller.Dave._crazyDaveAnimationAge, Is.GreaterThan(.23f));
        }

        /// <summary>Provides a single isolated logical press to the production modal input route.</summary>
        /// <param name="point">The native dialog button center.</param>
        private static void SetPointerFrame(Vector2 point)
        {
            typeof(GameInput).GetProperty(nameof(GameInput.Current))!.SetValue(null,
                new GameInputSnapshot(point, true, true, false, false, false, 0f, false, false));
        }

        /// <summary>Starts a playing native mode through the production directory callback.</summary>
        /// <param name="mode">The native mode ID.</param>
        /// <returns>The bounded original loading frames.</returns>
        private IEnumerator StartMode(int mode)
        {
            var deadline = Time.realtimeSinceStartup + 60f;
            while (!Controller.Loading.OriginalLoadingReady && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(Controller.Loading.OriginalLoadingReady, Is.True);
            Controller.Loading.CompleteOriginalLoading(false);
            Assert.That(Controller.Campaign.EnsureAdventureStore(), Is.True);
            Controller.Campaign.RequiredAdventureStore.Import(Controller.RequiredProfiles.RequiredSelectedId, 1, 1);
            Assert.That(Controller.RequiredModeView.LaunchRequested!(mode), Is.True);
            Assert.That(Controller.IndependentModes.RequiredModeSession.ChooseSeeds(new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Wallnut, PlantKind.PotatoMine, PlantKind.SnowPea, PlantKind.Chomper }), Is.True);
            Controller._choosingSeeds = false;
            yield return WaitForBattlePreparation();
        }

        /// <summary>Invokes the actual options pointer handling and close-result persistence route.</summary>
        private void ExitThroughOptions()
        {
            Controller.Menus.OpenOptions();
            var rect = (Rect)typeof(UI.OptionsView).GetField("s_mainMenuButton", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Controller.RequiredOptionsView.HandlePointer(rect.center, PointerPhase.Down);
            Controller.RequiredOptionsView.HandlePointer(rect.center, PointerPhase.Up);
            Controller.Menus.ApplyOptions();
        }
    }
}
