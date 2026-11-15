#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Tests.PlayMode.Smoke;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks a complete native mini-game entry and chooser confirmation using the actual game host and disposable profile.</summary>
    public sealed class OriginalChallengeFlowTests : PlayerSmokeFixture
    {
        /// <summary>Enables the complete original CutScene in the isolated production host.</summary>
        protected override bool PlayOpening => true;

        /// <summary>Checks native resource loading, re-picking, confirmation and the playable board handoff.</summary>
        /// <returns>Bounded original loading and production interface frames.</returns>
        [UnityTest]
        [Timeout(90000)]
        public IEnumerator NativeMiniGameChooserLoadsConfirmationSampleAndStartsBattle()
        {
            var deadline = Time.realtimeSinceStartup + 60;
            while ((!Controller.Loading.OriginalLoadingReady || !Controller.Sounds._audioLoadingFinished) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(Controller.Loading.OriginalLoadingReady && Controller.Sounds._audioLoadingFinished, Is.True);
            var clips = (Dictionary<string, AudioClip>)typeof(GameSoundEffects)
                .GetField("_awardSounds", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Controller.Sounds)!;
            Assert.That(clips.ContainsKey("gravebutton.ogg"), Is.True, "PC LawnDialog::ButtonPress uses SOUND_GRAVEBUTTON.");
            Assert.That(clips["gravebutton.ogg"].samples, Is.GreaterThan(0));
            Controller.Loading.CompleteOriginalLoading(false);
            Assert.That(Controller.Campaign.EnsureAdventureStore(), Is.True);
            Controller.Campaign.RequiredAdventureStore.Import(Controller.RequiredProfiles.RequiredSelectedId, 1, 1);
            Controller.RequiredModeView.Open(1);
            Assert.That(Controller.RequiredModeView.LaunchRequested!(16), Is.True);
            for (var tick = 0; tick < 1000 && !Controller.Opening.AdventureIntroAwaitingSeeds; tick++)
            {
                Controller.Opening.RequiredAdventureIntro.Tick(emit: Controller.Opening.HandleAdventureIntroEvent);
            }
            Controller.Opening._adventureIntroFrame = Controller.Opening.RequiredAdventureIntro.Frame;
            Assert.That(Controller.Opening.AdventureIntroAwaitingSeeds, Is.True);
            Assert.That(Controller.RequiredPendingSeeds.Count, Is.Zero);
            foreach (var seed in new[] { PlantKind.Peashooter, PlantKind.Wallnut, PlantKind.PotatoMine, PlantKind.CherryBomb, PlantKind.SnowPea, PlantKind.Chomper })
            {
                Controller.IndependentModes.TryHandleModeClick(Controller.Opening.AdventureCampaignChooserRect(seed).center);
                Controller.RequiredPendingSeeds.LandFlyingSeeds();
            }
            Controller.IndependentModes.TryHandleModeClick(Controller.Opening.AdventureChooserStartRect.center);
            Assert.That(Controller.IndependentModes.ChooserWarning?.Key, Is.EqualTo("SEED_CHOOSER_SUN_WARNING"));
            yield return null;
            Assert.That(Controller.RequiredGame.Elapsed, Is.Zero);
            Controller.IndependentModes.TryHandleModeClick(Controller._presenter.RequiredModeChooserDialog.NoBounds.center);
            Assert.That(Controller.IndependentModes.HasChooserModal, Is.False);
            Assert.That(Controller.RequiredPendingSeeds.Count, Is.EqualTo(6));
            Controller.IndependentModes.TryHandleModeClick(Controller.Opening.AdventureChooserStartRect.center);
            Controller.IndependentModes.TryHandleModeClick(Controller._presenter.RequiredModeChooserDialog.YesBounds.center);
            for (var tick = 0; tick < 1000 && Controller.Opening.AdventureIntroActive; tick++)
            {
                Controller.Opening.RequiredAdventureIntro.Tick(emit: Controller.Opening.HandleAdventureIntroEvent);
            }
            Controller.Opening._adventureIntroFrame = Controller.Opening.RequiredAdventureIntro.Frame;
            Assert.That(Controller.Opening.AdventureIntroActive, Is.False);
            Assert.That(Controller.IndependentModes.RequiredModeSession.Phase, Is.EqualTo(ModePhase.Playing));
            Assert.That(Controller.RequiredGame.Adventure.SelectedSeeds.Count, Is.EqualTo(6));
            yield return null;
            Assert.That(Controller._currentView, Is.EqualTo(PvZGame.ScreenView.Board));
        }
    }
}
