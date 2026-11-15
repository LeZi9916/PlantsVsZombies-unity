#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC survival opening and view-lawn clocks independently of rendering or device hardware.</summary>
    public sealed class SurvivalPresentationTests
    {
        /// <summary>Checks every original PC survival identifier uses a scrolling first preview and the original re-selection timing.</summary>
        /// <param name="mode">The original mode identifier.</param>
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        public void AllSurvivalModesKeepOriginalOpeningRules(int mode)
        {
            var first = Clock(mode, false);
            var repick = Clock(mode, true);
            Assert.That(first.Plan.ChooseSeeds && repick.Plan.ChooseSeeds, Is.True);
            Assert.That(first.Plan.NonScrolling || repick.Plan.NonScrolling, Is.False);
            Assert.That(first.Frame.BoardX, Is.EqualTo(220));
            Assert.That(repick.Frame.BoardX, Is.Zero);
            Assert.That(first.Plan.LawnMowerTime, Is.EqualTo(550));
            Assert.That(repick.Plan.LawnMowerTime, Is.Zero);
            WaitForChooser(first);
            WaitForChooser(repick);
            Assert.That(first.CanViewLawn, Is.False, "PC 499170 hides this widget on the initial selection.");
            Assert.That(repick.CanViewLawn, Is.True);
            Assert.That(repick.Frame.BoardX, Is.EqualTo(-380));
        }

        /// <summary>Checks literal PC 49A8D0 bounds, the hold at 250 and the 310..350 chooser return.</summary>
        [Test]
        public void ViewLawnUsesOriginalPanHoldAndReturnCurves()
        {
            var clock = Clock(13, true);
            WaitForChooser(clock);
            Assert.That(clock.BeginLawnView(), Is.True);
            Assert.That(clock.Frame.BoardX, Is.EqualTo(-380));
            for (var tick = 1; tick <= 100; tick++)
            {
                clock.Tick();
                Assert.That(clock.Frame.BoardX, Is.EqualTo(-AdventureIntroPresentation.Curve(0, 100, tick, 380, 0, 4)));
                Assert.That(clock.Frame.ChooserY, Is.EqualTo(AdventureIntroPresentation.Curve(0, 40, tick, 3, 516, 4)));
            }
            Assert.That(clock.Frame.BoardX, Is.Zero);
            Assert.That(clock.Frame.ChooserVisible, Is.False);
            for (var tick = 0; tick < 300; tick++)
            {
                clock.Tick();
            }
            Assert.That(clock.ViewLawnTicks, Is.EqualTo(250));
            Assert.That(clock.NativeTime, Is.EqualTo(clock.Plan.ChooserOnEnd));
            clock.CancelLawnView();
            Assert.That(clock.ViewLawnTicks, Is.EqualTo(251));
            while (clock.ViewingLawn)
            {
                var next = clock.ViewLawnTicks + 1;
                clock.Tick();
                if (next <= 350)
                {
                    Assert.That(clock.Frame.BoardX, Is.EqualTo(-AdventureIntroPresentation.Curve(250, 350, next, 0, 380, 4)));
                    Assert.That(clock.Frame.ChooserY, Is.EqualTo(AdventureIntroPresentation.Curve(310, 350, next, 516, 3, 4)));
                }
            }
            Assert.That(clock.WaitingForSeeds, Is.True);
            Assert.That(clock.Frame.ChooserY, Is.EqualTo(3));
            Assert.That(clock.Frame.BoardX, Is.EqualTo(-380));
        }

        /// <summary>Prevents early clicks or seed handoffs from bypassing the original outbound lawn inspection.</summary>
        [Test]
        public void LawnViewCannotBeSkippedOrStartedOnFirstSelection()
        {
            var first = Clock(1, false);
            WaitForChooser(first);
            Assert.That(first.BeginLawnView(), Is.False);
            var repick = Clock(1, true);
            WaitForChooser(repick);
            repick.BeginLawnView();
            repick.CancelLawnView();
            repick.BeginReturn();
            Assert.That(repick.ViewLawnTicks, Is.Zero);
            Assert.That(repick.WaitingForSeeds, Is.True);
        }

        /// <summary>Checks PC GetSurvivalFlagsCompleted does not count the flag wave still being fought.</summary>
        /// <param name="mode">The normal, hard or endless mode.</param>
        /// <param name="stage">The one-based survival stage.</param>
        /// <param name="wave">The already spawned wave count.</param>
        /// <param name="expected">The original completed flag count.</param>
        [TestCase(1, 1, 10, 0)]
        [TestCase(6, 1, 11, 1)]
        [TestCase(6, 1, 20, 1)]
        [TestCase(6, 2, 0, 2)]
        [TestCase(13, 3, 11, 5)]
        public void CompletedFlagsExcludeTheCurrentlyFightingFlagWave(int mode, int stage, int wave, int expected)
        {
            var session = new ModeSession(mode);
            session.Stage = stage;
            session.Phase = ModePhase.Playing;
            session.Game.Adventure.CurrentWave = wave;
            Assert.That(session.NativeSurvivalFlagsCompleted, Is.EqualTo(expected));
        }

        /// <summary>Checks PC 439C20 reveals locked endless thumbnails during the final three required trophies.</summary>
        /// <param name="trophies">The count of completed finite survival challenges.</param>
        /// <param name="expected">The original visible-card distance.</param>
        [TestCase(6, 4)]
        [TestCase(7, 1)]
        [TestCase(8, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 0)]
        public void EndlessDirectoryRevealKeepsOriginalThreeTrophyBoundary(int trophies, int expected)
        {
            for (var id = 11; id <= 15; id++)
            {
                Assert.That(ModeUnlockRules.DirectoryAccomplishmentsNeeded(id, true, index => index < trophies ? index < 5 ? 5 : 10 : 0), Is.EqualTo(expected));
            }
        }

        /// <summary>Builds the original survival context for a first selection or a later re-selection.</summary>
        /// <param name="mode">The native survival identifier.</param>
        /// <param name="repick">Whether a previous stage has completed.</param>
        /// <returns>The production intro clock.</returns>
        private static AdventureIntroClock Clock(int mode, bool repick)
        {
            return new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
            {
                NativeMode = mode, Level = 0, NativeBackground = (mode - 1) % 5,
                FirstAdventure = false, SurvivalRepick = repick, ChooseSeedsOverride = true
            }));
        }

        /// <summary>Advances only the native intro clock until its chooser pause.</summary>
        /// <param name="clock">The production clock being checked.</param>
        private static void WaitForChooser(AdventureIntroClock clock)
        {
            for (var i = 0; i < 1000 && !clock.WaitingForSeeds; i++)
            {
                clock.Tick();
            }
            Assert.That(clock.WaitingForSeeds, Is.True);
        }
    }
}
