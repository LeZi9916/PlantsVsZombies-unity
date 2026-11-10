#nullable enable

using System;
using NUnit.Framework;
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks stalled introduction clocks reuse snapshots without changing earlier frame values.</summary>
    public sealed class AdventureIntroPerformanceTests
    {
        /// <summary>Checks seed selection keeps the same frame and allocates no managed memory between ticks.</summary>
        [Test]
        public void SeedChooserWaitReusesFrameWithoutAllocating()
        {
            var clock = CreateClock();
            for (var i = 0; i < 1000 && !clock.WaitingForSeeds; i++)
            {
                clock.Tick();
            }

            Assert.That(clock.WaitingForSeeds, Is.True);
            var frame = clock.Frame;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                clock.Tick();
                GC.KeepAlive(clock.Frame);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(clock.Frame, Is.SameAs(frame));
            Assert.That(frame.Stage, Is.EqualTo(AdventureIntroStage.ChoosingSeeds));
        }

        /// <summary>Checks advancing or returning from the chooser creates a new independent frame.</summary>
        [Test]
        public void FrameInvalidatesOnClockChangesAndPreservesPreviousSnapshots()
        {
            var clock = CreateClock();
            var first = clock.Frame;
            clock.Tick();
            var next = clock.Frame;
            Assert.That(next, Is.Not.SameAs(first));
            Assert.That(first.NativeTime, Is.Zero);
            Assert.That(next.NativeTime, Is.EqualTo(AdventureIntroPresentation.NativeUpdate));

            for (var i = 0; i < 1000 && !clock.WaitingForSeeds; i++)
            {
                clock.Tick();
            }

            var waiting = clock.Frame;
            clock.BeginReturn();
            Assert.That(clock.Frame, Is.Not.SameAs(waiting));
            Assert.That(waiting.Stage, Is.EqualTo(AdventureIntroStage.ChoosingSeeds));
            Assert.That(clock.Frame.NativeTime, Is.EqualTo(clock.Plan.ChooserOnEnd + 10));
        }

        /// <summary>Creates a scrolling introduction that stops at its seed chooser.</summary>
        /// <returns>The unadvanced introduction clock.</returns>
        private static AdventureIntroClock CreateClock()
        {
            var plan = AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
            {
                Level = 8,
                FirstAdventure = true
            });
            return new AdventureIntroClock(plan);
        }
    }
}
