#nullable enable

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks Jalapeno's original PC fuse independently of the authored animation's completion.</summary>
    public sealed class AdventureJalapenoTests
    {
        /// <summary>Checks native initialization, pausing, two shake offsets, final-frame hold, and exactly one hundred updates.</summary>
        [Test]
        public void FuseUsesOriginalRateShakeAndHundredTicks()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            var place = typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var plant = (Plant)place.Invoke(game, new object[] { PlantKind.Jalapeno, 1, 4 })!;
            var state = plant.JalapenoPresentation!;
            var events = new List<GameEvent>();
            game.EventRaised += events.Add;
            Assert.That(state.Countdown, Is.EqualTo(100));
            Assert.That(state.AnimationTicks, Is.Zero);
            Assert.That(state.Rate, Is.InRange(10f, 15f));
            Assert.That(plant.PresentationRate, Is.EqualTo(state.Rate));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 57), Has.Count.EqualTo(1));
            // A fast original rate completes the thirteen-frame pose before the independent fuse expires.
            state.Rate = 15f;
            plant.PresentationRate = state.Rate;
            game.Update(.10f);
            var shakeX = state.ShakeX;
            var shakeY = state.ShakeY;
            Assert.That(state.AnimationTicks, Is.EqualTo(10));
            Assert.That(state.Countdown, Is.EqualTo(90));
            Assert.That(state.ShakeX, Is.InRange(-1f, 1f));
            Assert.That(state.ShakeY, Is.InRange(-1f, 1f));
            game.TogglePause();
            game.Update(.5f);
            Assert.That(state.Countdown, Is.EqualTo(90));
            Assert.That(state.AnimationTicks, Is.EqualTo(10));
            Assert.That(state.ShakeX, Is.EqualTo(shakeX));
            Assert.That(state.ShakeY, Is.EqualTo(shakeY));
            game.TogglePause();
            game.Update(.78f);
            Assert.That(state.Countdown, Is.EqualTo(12));
            Assert.That(state.NormalizedTime, Is.EqualTo(.999999f));
            Assert.That(state.SampleSeconds(0f), Is.EqualTo(state.SampleSeconds(.1f)));
            Assert.That(game.Plants, Does.Contain(plant), "Original animation completion must not burn the row early.");
            game.Update(.11f);
            Assert.That(state.Countdown, Is.EqualTo(1));
            Assert.That(state.AnimationTicks, Is.EqualTo(99));
            Assert.That(game.Plants, Does.Contain(plant));
            Assert.That(game.Presentation.JalapenoFires, Is.Empty);
            game.Update(.01f);
            Assert.That(state.Countdown, Is.Zero);
            Assert.That(state.AnimationTicks, Is.EqualTo(100), "Animate still runs after DoSpecial on the last native update.");
            Assert.That(state.Detonated, Is.True);
            Assert.That(game.Plants, Has.No.Member(plant));
            Assert.That(game.Presentation.JalapenoFires, Has.Count.EqualTo(1));
            Assert.That(events.FindAll(value => value == GameEvent.JalapenoExplosion), Has.Count.EqualTo(1));
            game.Update(.01f);
            Assert.That(events.FindAll(value => value == GameEvent.JalapenoExplosion), Has.Count.EqualTo(1));
        }
    }
}
