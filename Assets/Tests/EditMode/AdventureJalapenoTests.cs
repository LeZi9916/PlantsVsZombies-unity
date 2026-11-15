#nullable enable

using System.Collections.Generic;
using System.Reflection;
using PlantsVsZombies.Adventure;
using NUnit.Framework;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks Jalapeno's original PC fuse independently of the authored animation's completion.</summary>
    public sealed class AdventureJalapenoTests
    {
        /// <summary>Checks the existing ordinary BurnRow path removes cold only from admitted same-row targets across zombie species.</summary>
        /// <param name="frozen">Whether each chilled target also has a freeze counter.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void RowBurnClearsColdForOtherEligibleZombieKinds(bool frozen)
        {
            var checkedKinds = 0;
            foreach (ZombieKind kind in System.Enum.GetValues(typeof(ZombieKind)))
            {
                if (kind == ZombieKind.Boss)
                {
                    continue;
                }
                var game = new GameSimulation(new GameRules { AdventureLevel = 29 }, 7529);
                Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
                game.ResetAdventureWaveSchedule(100000, false, false);
                var row = kind == ZombieKind.DuckyTube || kind == ZombieKind.Snorkel || kind == ZombieKind.DolphinRider ? 2 : 1;
                var zombie = (Zombie)Call(game, "SpawnAdventureBoardZombie", kind, row, 5f, 0)!;
                if (kind == ZombieKind.Bungee)
                {
                    zombie.BungeeState!.Phase = 6;
                    zombie.PresentationNativePhase = 6;
                }
                else if (kind == ZombieKind.Digger)
                {
                    zombie.PresentationNativePhase = 37;
                }
                else if (kind == ZombieKind.Bobsled)
                {
                    zombie.BobsledState = null;
                    zombie.UsedAbility = true;
                    zombie.PresentationHasSled = false;
                    zombie.PresentationNativePhase = 0;
                }
                if (!(bool)Call(game, "CanAdventureChill", zombie)! || !(bool)Call(game, "CanTakeAdventureAshDamage", zombie, 0x7FU)!)
                {
                    continue;
                }
                checkedKinds++;
                zombie.Health = zombie.StartingHealth = 5000f;
                zombie.ChillRemaining = 20f;
                zombie.FrozenRemaining = frozen ? 4f : 0f;
                var other = (Zombie)Call(game, "SpawnAdventureBoardZombie", ZombieKind.Normal, (zombie.Row + 1) % game.RowCount, 5f, 0)!;
                other.ChillRemaining = 20f;
                other.FrozenRemaining = 4f;
                var pepper = (Plant)Call(game, "PlaceAdventurePlant", PlantKind.Jalapeno, zombie.Row, 2)!;
                Call(game, "DetonateAdventureJalapeno", pepper);
                Assert.That(zombie.ChillRemaining, Is.Zero, kind + " chill");
                Assert.That(zombie.FrozenRemaining, Is.Zero, kind + " freeze");
                Assert.That(other.ChillRemaining, Is.EqualTo(20f), kind + " must not thaw another row");
                Assert.That(other.FrozenRemaining, Is.EqualTo(4f));
            }
            Assert.That(checkedKinds, Is.GreaterThan(20));
        }

        /// <summary>Calls an existing simulation boundary without substituting its native burn or cold behavior.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="method">The unambiguous collaborator method name.</param>
        /// <param name="arguments">The original method arguments.</param>
        /// <returns>The production method result.</returns>
        private static object? Call(GameSimulation game, string method, params object[] arguments)
        {
            return SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }

        /// <summary>Checks native initialization, pausing, two shake offsets, final-frame hold, and exactly one hundred updates.</summary>
        [Test]
        public void FuseUsesOriginalRateShakeAndHundredTicks()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 8 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            var place = SimulationInspection.GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
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
