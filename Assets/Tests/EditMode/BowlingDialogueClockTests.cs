#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the native two-part level-five opening clock.</summary>
    public sealed class BowlingDialogueClockTests
    {
        /// <summary>Pauses for the shovel lesson and restarts Dave's entrance after the last plant is removed.</summary>
        [Test]
        public void ShovelLessonFreezesAndRestartsDaveEntrance()
        {
            var plan = AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
            {
                Level = 5,
                FirstAdventure = true,
                HasCrazyDaveDialog = true,
                ChooseSeedsOverride = false
            });
            var clock = new AdventureIntroClock(plan);
            for (var tick = 0; tick < 275; tick++)
            {
                clock.Tick();
            }

            Assert.That(clock.NativeTime, Is.EqualTo(2750));
            for (var tick = 0; tick < 50; tick++)
            {
                clock.Tick(shovelTutorialActive: true);
            }

            Assert.That(clock.NativeTime, Is.EqualTo(2750));
            clock.RestartAfterShovelTutorial();
            Assert.That(clock.NativeTime, Is.EqualTo(1500));
            for (var tick = 0; tick < 125; tick++)
            {
                clock.Tick();
            }

            Assert.That(clock.NativeTime, Is.EqualTo(2750));
        }
    }
}
