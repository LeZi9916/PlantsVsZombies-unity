#nullable enable
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the production survival update advances and resets PC 420360/416900 progress state.</summary>
    public sealed class SurvivalProgressTests
    {
        /// <summary>Checks all original endless terrains and finite survival variants drive the native meter.</summary>
        /// <param name="mode">The original survival identifier.</param>
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        public void SurvivalTickAdvancesTheOriginalMeter(int mode)
        {
            var session = Board(mode);
            session.Game.Adventure.CurrentWave = 1;
            Tick(session, 19);
            Assert.That(session.Game.Presentation.ProgressMeter.Counter, Is.Zero);
            Tick(session, 1);
            Assert.That(session.Game.Presentation.ProgressMeter.Counter, Is.EqualTo(1));
            Assert.That(session.Game.Presentation.ProgressMeter.Visible, Is.True);
            session.Game.Adventure.CurrentWave = 5;
            Tick(session, 100);
            Assert.That(session.Game.Presentation.ProgressMeter.Counter, Is.GreaterThan(1));
            session.Game.TogglePause();
            var counter = session.Game.Presentation.ProgressMeter.Counter;
            Tick(session, 20);
            Assert.That(session.Game.Presentation.ProgressMeter.Counter, Is.EqualTo(counter));
        }

        /// <summary>Checks a survival stage resets the meter and flag lift while keeping native accumulated flag counts.</summary>
        [Test]
        public void ReselectionStartsWithAnEmptyProgressMeter()
        {
            var session = Board(13);
            session.Game.Adventure.CurrentWave = 10;
            Tick(session, 100);
            Assert.That(session.Game.Presentation.ProgressMeter.Counter, Is.GreaterThan(0));
            Assert.That(session.Game.Presentation.ProgressMeter.FlagLift(10), Is.EqualTo(14));
            session.Game.Adventure.CurrentWave = 20;
            SimulationInspection.GetMethod("ClearModeStage", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(session.Game, null);
            Assert.That(session.Continue(), Is.True);
            Assert.That(session.Stage, Is.EqualTo(2));
            Assert.That(session.NativeSurvivalFlagsCompleted, Is.EqualTo(2));
            Assert.That(session.Game.Presentation.ProgressMeter.Counter, Is.Zero);
            Assert.That(session.Game.Presentation.ProgressMeter.FlagLift(10), Is.Zero);
            Assert.That(session.Game.Presentation.ProgressMeter.Visible, Is.False);
        }

        /// <summary>Creates a quiet selected native survival bank.</summary>
        /// <param name="mode">The original survival identifier.</param>
        /// <returns>The isolated production mode.</returns>
        private static ModeSession Board(int mode)
        {
            var session = new ModeSession(mode);
            Assert.That(session.ChooseSeeds(session.Game.Adventure.SelectedSeeds), Is.True);
            session.Game.ResetAdventureWaveSchedule(100000, false, false);
            return session;
        }

        /// <summary>Advances only the requested number of native hundred-Hz production updates.</summary>
        /// <param name="session">The selected survival session.</param>
        /// <param name="ticks">The exact update count.</param>
        private static void Tick(ModeSession session, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                session.Update(.01f);
            }
        }
    }
}
