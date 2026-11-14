#nullable enable

using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises the complete native 4-10 opening, pause/resume and naturally reached loss weather sequence.</summary>
    public sealed class OriginalStormFlowTests
    {
        /// <summary>Follows actual opening and battle ticks through the PC loss camera, banner and pausing retry dialog.</summary>
        /// <returns>Production graphics frame boundaries at the relevant original transition points.</returns>
        [UnityTest]
        public IEnumerator OpeningPauseBattleAndNaturalLossKeepOriginalWeatherTransitions()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture();
            var owner = fixture.Owner;
            var game = owner.RequiredGame;
            var storm = game.Adventure.Storm;
            var plan = AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
            {
                Level = 40, FirstAdventure = true, NativeBackground = 3, IsConveyor = true, IsStormyNight = true
            });
            owner._currentView = Game.PvZGame.ScreenView.Board;
            owner.Dave._crazyDaveMessageIndex = -1;
            owner.Opening._adventureIntro = new AdventureIntroClock(plan);
            owner.Opening._adventureIntroFrame = owner.Opening.RequiredAdventureIntro.Frame;
            owner.Opening._adventureIntroEventHandler = owner.Opening.HandleAdventureIntroEvent;
            game.BeginAdventureStormIntro();
            owner.Opening.UpdateAdventureOpening(.5f);
            Assert.That(storm.Countdown, Is.EqualTo(150));
            Assert.That(storm.EffectTicks, Is.Zero);
            owner.Opening._adventureIntroDrawn = true;
            for (var tick = 0; tick < 250; tick++)
            {
                owner.Opening.UpdateAdventureOpening(.01f);
            }

            Assert.That(owner.Opening.RequiredAdventureIntro.NativeTime, Is.EqualTo(2500));
            Assert.That(storm.Phase, Is.EqualTo(6));
            Assert.That(storm.Countdown, Is.EqualTo(309), "PC AnimateBoard forces countdown 310 before the same tick's Challenge update.");
            for (var tick = 0; tick < 10; tick++)
            {
                owner.Opening.UpdateAdventureOpening(.01f);
            }

            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 17).Count, Is.EqualTo(1));
            storm.GetFlashAlpha(out var black, out var white);
            Assert.That(black, Is.Zero);
            Assert.That(white, Is.EqualTo(252));
            owner.RequiredOptionsView.Open(.7f, .7f, false);
            var effectTicks = storm.EffectTicks;
            for (var tick = 0; tick < 400; tick++)
            {
                owner.Opening.UpdateAdventureOpening(.01f);
            }

            Assert.That(owner.Opening.RequiredAdventureIntro.NativeTime, Is.EqualTo(2600));
            Assert.That(storm.Countdown, Is.EqualTo(1));
            Assert.That(storm.EffectTicks, Is.EqualTo(effectTicks));
            Assert.That(storm.MainTicks, Is.Zero);
            owner.RequiredOptionsView.Close();
            while (owner.Opening.RequiredAdventureIntro.NativeTime < 6000)
            {
                owner.Opening.UpdateAdventureOpening(.01f);
            }

            Assert.That(storm.Phase, Is.EqualTo(6));
            Assert.That(storm.Countdown, Is.EqualTo(309), "Returning to the lawn starts the second original forced flash.");
            while (owner.Opening.AdventureIntroActive)
            {
                owner.Opening.UpdateAdventureOpening(.01f);
            }

            Assert.That(owner.Opening.RequiredAdventureIntro.NativeTime, Is.EqualTo(8380));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 17).Count, Is.EqualTo(2));
            Assert.That(storm.Phase, Is.EqualTo(5));
            Assert.That(storm.Countdown, Is.EqualTo(399));
            Assert.That(storm.MainTicks, Is.EqualTo(1));
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            game.TogglePause();
            var elapsed = game.Elapsed;
            effectTicks = storm.EffectTicks;
            var main = storm.MainTicks;
            for (var tick = 0; tick < 500; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(storm.Countdown, Is.EqualTo(1));
            Assert.That(storm.EffectTicks, Is.EqualTo(effectTicks));
            Assert.That(storm.MainTicks, Is.EqualTo(main));
            Assert.That(game.Elapsed, Is.EqualTo(elapsed));
            game.TogglePause();
            for (var tick = 0; tick < 30000 && game.State == MatchState.Playing; tick++)
            {
                game.Update(.01f);
                if (tick % 1000 == 0)
                {
                    yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
                }
            }

            Assert.That(game.State, Is.EqualTo(MatchState.Lost), "The original level must naturally reach loss after the unplanted lawn exhausts its mowers.");
            Assert.That(storm.Lost, Is.True);
            Assert.That(storm.LostTicks, Is.Zero);
            main = storm.MainTicks;
            effectTicks = storm.EffectTicks;
            for (var tick = 0; tick < 250; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(storm.LossBoardX, Is.EqualTo(110));
            Assert.That(storm.MainTicks, Is.EqualTo(main));
            Assert.That(storm.EffectTicks, Is.EqualTo(effectTicks + 250));
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            for (var tick = 250; tick < 650; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(storm.LossBoardX, Is.EqualTo(220));
            Assert.That(storm.LossDialogReady, Is.False);
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 85).Count, Is.EqualTo(1));
            var chomps = game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 5);
            Assert.That(chomps.Count, Is.EqualTo(2));
            Assert.That(chomps[1].Tick - chomps[0].Tick, Is.EqualTo(50), "The two original loss chomps use advancing presentation time while combat is frozen.");
            yield return fixture.Capture("DrawAdventureStormLoss");
            var pixel = fixture.Pixels.GetPixel(20, 20);
            Assert.That(pixel.r * 255f, Is.EqualTo(188f).Within(1f), "PC 6000-ms banner at 12 FPS samples authored fullscreen alpha .2646 after 51 updates.");
            Assert.That(pixel.g, Is.EqualTo(pixel.r));
            Assert.That(pixel.b, Is.EqualTo(pixel.r));
            for (var tick = 650; tick < 1100; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(storm.LossDialogReady, Is.True);
            effectTicks = storm.EffectTicks;
            game.Update(4f);
            Assert.That(storm.LostTicks, Is.EqualTo(1100));
            Assert.That(storm.MainTicks, Is.EqualTo(main));
            Assert.That(storm.EffectTicks, Is.EqualTo(effectTicks), "The native retry dialog pauses rain and board effects while the flash settles.");
            Assert.That(storm.Countdown, Is.EqualTo(1));
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
        }
    }
}
