#nullable enable
using System;
using NUnit.Framework;
using PlantsVsZombies.Progression;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Verifies original Tree of Wisdom speech selection and timing.</summary>
    public sealed class WisdomTreeSpeechTests
    {
        /// <summary>Checks that newly grown trees speak at native times and stop after ten seconds.</summary>
        [Test]
        public void GrowthShowsMilestoneWisdomAfterDelay()
        {
            var clock = new WisdomTreeSpeechClock(_ => 0);
            clock.StartGrowth();
            for (var tick = 0; tick < 119; tick++)
            {
                clock.Tick(100);
            }

            Assert.That(clock.MessageIndex, Is.EqualTo(-1));
            clock.Tick(100);
            Assert.That(clock.MessageIndex, Is.EqualTo(800));
            for (var tick = 0; tick < 1000; tick++)
            {
                clock.Tick(100);
            }

            Assert.That(clock.MessageIndex, Is.EqualTo(-1));
        }

        /// <summary>Checks the small tree's original request for food and speech duration.</summary>
        [Test]
        public void SmallTreeBabblesOnlyAfterInitialWait()
        {
            var clock = new WisdomTreeSpeechClock(_ => 0);
            for (var tick = 0; tick < 699; tick++)
            {
                clock.Tick(1);
            }

            Assert.That(clock.MessageIndex, Is.EqualTo(-1));
            clock.Tick(1);
            Assert.That(clock.MessageIndex, Is.EqualTo(600));
            for (var tick = 0; tick < 400; tick++)
            {
                clock.Tick(1);
            }

            Assert.That(clock.MessageIndex, Is.EqualTo(-1));
        }
    }
}
