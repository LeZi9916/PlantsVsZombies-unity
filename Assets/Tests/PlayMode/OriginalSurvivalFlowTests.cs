#nullable enable
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Tests.PlayMode.Smoke;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks survival's real host, native re-selection and persisted on-lawn reward flow with disposable profiles.</summary>
    public sealed class OriginalSurvivalFlowTests : PlayerSmokeFixture
    {
        /// <summary>Runs the complete original intro in this isolated production host.</summary>
        protected override bool PlayOpening => true;

        /// <summary>Checks PC's interactive 500-tick stage delay, retained lawn and native re-selection.</summary>
        /// <returns>Frames for the real loading, board and lawn-view presenter.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator StageClearAutomaticallyReopensNativeChooserAndKeepsLawn()
        {
            yield return StartSurvival();
            var session = Controller.IndependentModes.RequiredModeSession;
            var plant = new Plant { Id = 10001, Kind = PlantKind.Wallnut, Row = 0, Column = 0, Health = 3210 };
            SimulationFixture.Plants(session.Game).Add(plant);
            SimulationInspection.GetProperty("SunAmount").SetValue(session.Game, 777);
            session.Game.Adventure.CurrentWave = session.Game.Adventure.Waves.Count;
            SimulationInspection.GetMethod("ClearModeStage").Invoke(session.Game, null);
            Controller.IndependentModes.TickModes(.01f);
            Assert.That(Controller.IndependentModes.SurvivalTransitionTicks, Is.EqualTo(500));
            var game = session.Game;
            game.Adventure._nextSkySun = .01f;
            var waveCountdown = game.Adventure.WaveCountdown;
            var initialPlantAge = plant.Age;
            var sun = (global::PlantsVsZombies.Simulation.Sun)SimulationInspection.GetMethod("CreateAdventurePresentationDroppedSun")
                .Invoke(game, new object[] { 4f, 2f, 25 })!;
            SimulationFixture.Suns(game).Add(sun);
            Controller.IndependentModes.TryHandleModeClick(Controller.Layout.AdventureSunRect(sun).center);
            Assert.That(sun.PresentationPickup.Collecting, Is.True);
            Controller.IndependentModes.TryHandleModeClick(Controller.IndependentModes.ModeSeedRect(0).center);
            Controller.IndependentModes.TryHandleModeClick(new Vector2(BoardLayout.AdventureGridX + BoardLayout.CellWidth * 1.5f,
                BoardLayout.AdventureGridY + Controller.Layout.AdventurePitch * .5f));
            Assert.That(game.Plants.Count, Is.EqualTo(2));
            var plantedSun = 777 - GameSimulation.Cost(Controller._deck[0]);
            Assert.That(game.SunAmount, Is.EqualTo(plantedSun));

            Controller.IndependentModes.TickModes(2.5f);
            Assert.That(Controller.IndependentModes.SurvivalTransitionTicks, Is.EqualTo(250));
            Assert.That(Controller.IndependentModes.SurvivalTransitionGain, Is.EqualTo(.5f));
            Assert.That(session.Stage, Is.EqualTo(1));
            Assert.That(plant.Age, Is.GreaterThan(initialPlantAge));
            Assert.That(game.SunAmount, Is.EqualTo(plantedSun + 25));
            Assert.That(game.Suns, Is.Empty);
            Assert.That(game.Adventure._nextSkySun, Is.EqualTo(.01f));
            Assert.That(game.Adventure.WaveCountdown, Is.EqualTo(waveCountdown));
            Controller.IndependentModes.TickModes(2.5f);
            Assert.That(session.Stage, Is.EqualTo(2));
            Assert.That(session.Game.Plants, Has.Member(plant));
            Assert.That(plant.Health, Is.EqualTo(3210));
            Assert.That(session.Game.SunAmount, Is.EqualTo(plantedSun + 25));
            WaitForChooser();
            Assert.That(Controller.Opening.RequiredAdventureIntro.CanViewLawn, Is.True);
            var selected = Controller.RequiredPendingSeeds.Count;
            Controller.IndependentModes.TryHandleModeClick(new Vector2(60, 570));
            Assert.That(Controller.Opening.RequiredAdventureIntro.ViewingLawn, Is.True);
            for (var tick = 0; tick < 110; tick++)
            {
                Controller.Opening.RequiredAdventureIntro.Tick();
            }
            Controller.Opening._adventureIntroFrame = Controller.Opening.RequiredAdventureIntro.Frame;
            yield return null;
            Assert.That(session.Game.Plants, Has.Member(plant));
            Assert.That(session.Game.SunAmount, Is.EqualTo(plantedSun + 25));
            Assert.That(Controller.RequiredPendingSeeds.Count, Is.EqualTo(selected));
            Assert.That(Controller.Accounts.RequiredAccountStore.Get(Controller.RequiredProfiles.RequiredSelectedId).ModeRecord(0), Is.EqualTo(1));
        }

        /// <summary>Checks the finite-mode native trophy is collected once and fades back to the original directory.</summary>
        /// <returns>Frames for production loading and the final directory presentation.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator FinalSurvivalAwardIsOnTheLawnAndPersistsOnlyOnce()
        {
            yield return StartSurvival();
            var session = Controller.IndependentModes.RequiredModeSession;
            session.Stage = 5;
            session.ClearedStages = 4;
            session.Game.Adventure.CurrentWave = session.Game.Adventure.Waves.Count;
            session.Game.Adventure._lastRewardX = 400;
            session.Game.Adventure._lastRewardY = 220;
            SimulationInspection.GetMethod("ClearModeStage").Invoke(session.Game, null);
            Controller.IndependentModes.TickModes(.01f);
            var award = session.Game.Adventure.RewardPresentation;
            Assert.That(award, Is.Not.Null);
            Assert.That(award!.NativeType, Is.EqualTo(8));
            var id = Controller.RequiredProfiles.RequiredSelectedId;
            var balance = Controller.Accounts.RequiredAccountStore.Get(id).Coins;
            var point = new Vector2(award.X + award.Width * .5f, award.Y + award.Height * .5f);
            Controller.IndependentModes.TryHandleModeClick(point);
            Assert.That(award.Stage, Is.EqualTo(AdventureAwardStage.Collecting));
            Assert.That(Controller.Accounts.RequiredAccountStore.Get(id).Coins, Is.EqualTo(balance + 100));
            Controller.IndependentModes.TryHandleModeClick(point);
            Assert.That(Controller.Accounts.RequiredAccountStore.Get(id).Coins, Is.EqualTo(balance + 100));
            Controller.IndependentModes.TickModes(6.1f);
            Assert.That(Controller._currentView, Is.EqualTo(PvZGame.ScreenView.Modes));
            Assert.That(Controller.RequiredModeView.IsSurvivalPage, Is.True);
            Assert.That(Controller.Accounts.RequiredAccountStore.Get(id).ModeRecord(0), Is.EqualTo(5));
            yield return null;
        }

        /// <summary>Checks the shared PC loss pan/banner/dialog clock and survival retry's return to the directory.</summary>
        /// <returns>Production loading and loss rendering frames.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator SurvivalLossUsesOriginalCutsceneAndReturnsToDirectory()
        {
            yield return StartSurvival();
            var session = Controller.IndependentModes.RequiredModeSession;
            session.Phase = ModePhase.Lost;
            SimulationInspection.GetProperty("State").SetValue(session.Game, MatchState.Lost);
            Controller.IndependentModes.TickModes(5.1f);
            Assert.That(session.Game.Adventure.Storm.LostTicks, Is.EqualTo(510));
            Assert.That(session.Game.Adventure.Storm.LossDialogReady, Is.False);
            Controller.IndependentModes.TickModes(6f);
            Assert.That(session.Game.Adventure.Storm.LossDialogReady, Is.True);
            yield return null;
            Controller.IndependentModes.TryHandleModeClick(new Vector2(400, 364));
            Assert.That(Controller._currentView, Is.EqualTo(PvZGame.ScreenView.Modes));
        }

        /// <summary>Checks a loss records a new completed flag even when the current two-flag stage has not cleared.</summary>
        /// <returns>Bounded original loading and a persisted hard-mode stage handoff.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator HardLossStoresCompletedFlagBeyondTheLastSavedStage()
        {
            yield return StartSurvival(6);
            var session = Controller.IndependentModes.RequiredModeSession;
            session.Game.Adventure.CurrentWave = session.Game.Adventure.Waves.Count;
            SimulationInspection.GetMethod("ClearModeStage").Invoke(session.Game, null);
            Controller.IndependentModes.TickModes(.01f);
            Controller.IndependentModes.TickModes(5f);
            Assert.That(session.Stage, Is.EqualTo(2));
            Assert.That(Controller.Accounts.RequiredAccountStore.Get(Controller.RequiredProfiles.RequiredSelectedId).ModeRecord(5), Is.EqualTo(2));
            session.Game.Adventure.CurrentWave = 11;
            session.Phase = ModePhase.Lost;
            SimulationInspection.GetProperty("State").SetValue(session.Game, MatchState.Lost);
            Controller.IndependentModes.TickModes(.01f);
            Assert.That(session.NativeSurvivalFlagsCompleted, Is.EqualTo(3));
            Assert.That(Controller.Accounts.RequiredAccountStore.Get(Controller.RequiredProfiles.RequiredSelectedId).ModeRecord(5), Is.EqualTo(3));
        }

        /// <summary>Starts a native first survival board through the actual mode directory callback.</summary>
        /// <param name="mode">The normal or hard survival identifier to launch.</param>
        /// <returns>Bounded original loading frames.</returns>
        private IEnumerator StartSurvival(int mode = 1)
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
            if (mode == 6)
            {
                for (var index = 0; index < 3; index++)
                {
                    Controller.Accounts.RequiredAccountStore.RecordModeResult(Controller.RequiredProfiles.RequiredSelectedId,
                        index, 5, "survival-flow-prerequisite-" + index, 0);
                }
            }
            Controller.RequiredModeView.Open(0);
            Assert.That(Controller.RequiredModeView.LaunchRequested!(mode), Is.True);
            Assert.That(Controller.RequiredPendingSeeds.Count, Is.Zero);
            WaitForChooser();
            var available = Controller.RequiredGame.Adventure.AvailableSeeds;
            for (var i = 0; i < Controller.IndependentModes.RequiredModeSession.PacketSlots; i++)
            {
                Controller.IndependentModes.TryHandleModeClick(Controller.Opening.AdventureCampaignChooserRect(available[i]).center);
                Controller.RequiredPendingSeeds.LandFlyingSeeds();
            }
            Controller.IndependentModes.TryHandleModeClick(Controller.Opening.AdventureChooserStartRect.center);
            Assert.That(Controller.Opening.AdventureIntroAwaitingSeeds, Is.False);
            for (var tick = 0; tick < 1000 && Controller.Opening.AdventureIntroActive; tick++)
            {
                Controller.Opening.RequiredAdventureIntro.Tick(emit: Controller.Opening.HandleAdventureIntroEvent);
            }
            Controller.Opening._adventureIntroFrame = Controller.Opening.RequiredAdventureIntro.Frame;
            Assert.That(Controller.IndependentModes.RequiredModeSession.Phase, Is.EqualTo(ModePhase.Playing));
        }

        /// <summary>Advances the original preview clock to its native chooser pause.</summary>
        private void WaitForChooser()
        {
            for (var tick = 0; tick < 1000 && !Controller.Opening.AdventureIntroAwaitingSeeds; tick++)
            {
                Controller.Opening.RequiredAdventureIntro.Tick(emit: Controller.Opening.HandleAdventureIntroEvent);
            }
            Controller.Opening._adventureIntroFrame = Controller.Opening.RequiredAdventureIntro.Frame;
            Assert.That(Controller.Opening.AdventureIntroAwaitingSeeds, Is.True);
        }
    }
}
