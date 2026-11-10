#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects primary touch ownership and cancellation independently of device hardware.</summary>
    public sealed class PrimaryTouchTrackerTests
    {
        /// <summary>Keeps the original finger when Unity reorders the native touch list.</summary>
        [Test]
        public void KeepsPrimaryFingerAcrossReorderedTouches()
        {
            var tracker = new PrimaryTouchTracker();
            tracker.BeginFrame();
            tracker.AddTouch(10, TouchPhase.Began, new Vector2(100f, 200f));
            Assert.That(tracker.CompleteFrame().Pressed, Is.True);

            tracker.BeginFrame();
            tracker.AddTouch(20, TouchPhase.Began, new Vector2(500f, 600f));
            tracker.AddTouch(10, TouchPhase.Moved, new Vector2(120f, 220f));
            var sample = tracker.CompleteFrame();
            Assert.That(sample.ScreenPosition, Is.EqualTo(new Vector2(120f, 220f)));
            Assert.That(sample.Held, Is.True);
            Assert.That(sample.Pressed, Is.False);
        }

        /// <summary>Releases a canceled primary finger without producing a normal clickable release.</summary>
        [Test]
        public void MarksCanceledPrimaryAsInterruptedRelease()
        {
            var tracker = BeginPrimary();
            tracker.BeginFrame();
            tracker.AddTouch(10, TouchPhase.Canceled, new Vector2(100f, 200f));
            var sample = tracker.CompleteFrame();
            Assert.That(sample.Released, Is.True);
            Assert.That(sample.Held, Is.False);
            Assert.That(sample.Canceled, Is.True);
        }

        /// <summary>Releases capture when the primary finger disappears without a terminal native event.</summary>
        [Test]
        public void CancelsMissingPrimaryFinger()
        {
            var tracker = BeginPrimary();
            tracker.BeginFrame();
            tracker.AddTouch(20, TouchPhase.Stationary, new Vector2(500f, 600f));
            var sample = tracker.CompleteFrame();
            Assert.That(sample.Released, Is.True);
            Assert.That(sample.Held, Is.False);
            Assert.That(sample.Canceled, Is.True);
        }

        /// <summary>Waits for remaining fingers to leave before accepting another primary gesture.</summary>
        [Test]
        public void DoesNotPromoteSecondaryFingerAfterPrimaryEnds()
        {
            var tracker = BeginPrimary();
            tracker.BeginFrame();
            tracker.AddTouch(10, TouchPhase.Ended, new Vector2(120f, 220f));
            tracker.AddTouch(20, TouchPhase.Began, new Vector2(500f, 600f));
            var release = tracker.CompleteFrame();
            Assert.That(release.Released, Is.True);
            Assert.That(release.Canceled, Is.False);
            Assert.That(release.ScreenPosition, Is.EqualTo(new Vector2(120f, 220f)));

            tracker.BeginFrame();
            tracker.AddTouch(20, TouchPhase.Moved, new Vector2(520f, 620f));
            Assert.That(tracker.CompleteFrame().Held, Is.False);

            tracker.BeginFrame();
            tracker.CompleteFrame();
            tracker.BeginFrame();
            tracker.AddTouch(30, TouchPhase.Began, new Vector2(200f, 300f));
            Assert.That(tracker.CompleteFrame().Pressed, Is.True);
        }

        /// <summary>Cancels a gesture when the native touch list becomes empty unexpectedly.</summary>
        [Test]
        public void ReleasesCaptureWhenAllTouchesDisappear()
        {
            var tracker = BeginPrimary();
            tracker.BeginFrame();
            var sample = tracker.CompleteFrame();
            Assert.That(sample.Released, Is.True);
            Assert.That(sample.Canceled, Is.True);
            tracker.BeginFrame();
            Assert.That(tracker.CompleteFrame().Released, Is.False);
        }

        /// <summary>Creates an active tracked primary finger for an interruption test.</summary>
        /// <returns>A tracker owning finger ten.</returns>
        private static PrimaryTouchTracker BeginPrimary()
        {
            var tracker = new PrimaryTouchTracker();
            tracker.BeginFrame();
            tracker.AddTouch(10, TouchPhase.Began, new Vector2(100f, 200f));
            tracker.CompleteFrame();
            return tracker;
        }
    }
}
