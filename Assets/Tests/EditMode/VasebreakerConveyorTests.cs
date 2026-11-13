#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks that native conveyor selection stays separate from Vasebreaker packet rewards.</summary>
    public sealed class VasebreakerConveyorTests
    {
        /// <summary>Keeps both first-adventure and replay Vasebreaker stages off the conveyor path.</summary>
        /// <param name="cycle">The adventure playthrough whose level 4-5 is initialized.</param>
        [TestCase(1)]
        [TestCase(2)]
        public void AdventureVasebreakerDoesNotUseConveyor(int cycle)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35, AdventureCycle = cycle }, 7529);
            Assert.That(game.Adventure.Level.Mode, Is.EqualTo(AdventureMode.Vasebreaker));
            Assert.That(game.Adventure.Level.HasConveyor, Is.False);
            Assert.That(game.UsesNativeAdventureConveyor, Is.False);
        }

        /// <summary>Checks every standalone Vasebreaker puzzle, including the endless variant.</summary>
        /// <param name="mode">The original PC Vasebreaker mode identifier.</param>
        [TestCase(51)]
        [TestCase(52)]
        [TestCase(53)]
        [TestCase(54)]
        [TestCase(55)]
        [TestCase(56)]
        [TestCase(57)]
        [TestCase(58)]
        [TestCase(59)]
        [TestCase(60)]
        public void IndependentVasebreakerDoesNotUseConveyor(int mode)
        {
            var session = new ModeSession(mode, randomSeed: 7529);
            Assert.That(session.Definition.Mechanic, Is.EqualTo(ModeMechanic.Vases));
            Assert.That(session.Definition.UsesPackets, Is.True);
            Assert.That(session.Game.UsesNativeAdventureConveyor, Is.False);
        }

        /// <summary>Preserves the original adventure levels that really have conveyor seed banks.</summary>
        /// <param name="level">The adventure level using a native conveyor.</param>
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(25)]
        [TestCase(30)]
        [TestCase(40)]
        [TestCase(45)]
        [TestCase(50)]
        public void NativeAdventureConveyorsRemainEnabled(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.Adventure.Level.HasConveyor, Is.True);
            Assert.That(game.UsesNativeAdventureConveyor, Is.True);
        }
    }
}
