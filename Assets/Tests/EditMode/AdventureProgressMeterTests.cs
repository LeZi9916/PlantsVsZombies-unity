#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original PC progress meter timing, asset coordinates, and level exceptions.</summary>
    public sealed class AdventureProgressMeterTests
    {
        /// <summary>Checks small increments happen only on the native twentieth tick and do not move backwards.</summary>
        [Test]
        public void ProgressUsesNativeTwentyTickCadence()
        {
            var meter = new AdventureProgressMeterState();
            meter.Advance(19, 1, 10, 10, true, 1000, 1000, 100, -1, 100);
            Assert.That(meter.Visible, Is.False);
            meter.Advance(20, 1, 10, 10, true, 1000, 1000, 100, -1, 100);
            Assert.That(meter.Counter, Is.EqualTo(1));
            meter.Advance(40, 1, 10, 10, true, 1000, 1000, 100, -1, 100);
            Assert.That(meter.Counter, Is.EqualTo(1));
        }

        /// <summary>Checks large gaps catch up only every fifth native tick.</summary>
        [Test]
        public void HealthProgressUsesNativeFiveTickCatchup()
        {
            var meter = new AdventureProgressMeterState();
            meter.Advance(4, 5, 10, 10, true, 1000, 1000, 100, 50, 50);
            Assert.That(meter.Counter, Is.Zero);
            meter.Advance(5, 5, 10, 10, true, 1000, 1000, 100, 50, 50);
            Assert.That(meter.Counter, Is.EqualTo(1));
        }

        /// <summary>Checks native original fill width, head motion, and boss health use the 150-unit scale.</summary>
        [Test]
        public void BossHealthUsesOriginalAssetPixelDistances()
        {
            var meter = new AdventureProgressMeterState();
            meter.SetBossHealth(20000, 40000);
            Assert.That(meter.Counter, Is.EqualTo(75));
            Assert.That(meter.FillWidth, Is.EqualTo(72));
            Assert.That(meter.HeadX, Is.EqualTo(670));
            meter.SetBossHealth(0, 40000);
            Assert.That(meter.FillWidth, Is.EqualTo(143));
            Assert.That(meter.HeadX, Is.EqualTo(603));
            Assert.That(AdventureProgressMeterState.FlagX(10, 20), Is.EqualTo(677));
            Assert.That(AdventureProgressMeterState.FlagX(20, 20), Is.EqualTo(606));
        }

        /// <summary>Checks reached flags rise 14 pixels over 100 native ticks.</summary>
        [Test]
        public void FlagPostsRiseOverOneNativeSecond()
        {
            var meter = new AdventureProgressMeterState();
            for (var tick = 100; tick < 150; tick++)
            {
                meter.Advance(tick, 10, 20, 10, true, 1000, 1000, 100, -1, 100);
            }

            Assert.That(meter.FlagLift(10), Is.EqualTo(7));
            Assert.That(meter.FlagLift(20), Is.Zero);
            for (var tick = 150; tick < 200; tick++)
            {
                meter.Advance(tick, 10, 20, 10, true, 1000, 1000, 100, -1, 100);
            }

            Assert.That(meter.FlagLift(10), Is.EqualTo(14));
        }

        /// <summary>Checks original adventure flag-post exceptions without substituting mobile mode rules.</summary>
        /// <param name="level">The adventure level.</param>
        /// <param name="replay">Whether this is a later adventure cycle.</param>
        /// <param name="expected">Whether the original PC meter has flag posts.</param>
        [TestCase(1, false, false)]
        [TestCase(1, true, true)]
        [TestCase(15, false, false)]
        [TestCase(50, false, false)]
        [TestCase(8, false, true)]
        public void AdventureFlagExceptionsMatchOriginalPc(int level, bool replay, bool expected)
        {
            Assert.That(AdventureProgressMeterState.HasFlags(level, replay), Is.EqualTo(expected));
        }
    }
}
