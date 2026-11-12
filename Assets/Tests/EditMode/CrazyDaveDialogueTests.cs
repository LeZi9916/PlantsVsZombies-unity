#nullable enable
using System;
using NUnit.Framework;
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original adventure-opening dialogue selection and entry progression.</summary>
    public sealed class CrazyDaveDialogueTests
    {
        /// <summary>Checks level and special-mode dialogue starts.</summary>
        /// <param name="level">The adventure level.</param>
        /// <param name="firstAdventure">Whether this is the first adventure.</param>
        /// <param name="mode">The level mode.</param>
        /// <param name="restart">Whether the previous board was lost or restarted.</param>
        /// <param name="expected">The original first text index, or -1.</param>
        [TestCase(1, true, AdventureMode.Lawn, false, -1)]
        [TestCase(11, true, AdventureMode.Lawn, false, 201)]
        [TestCase(12, true, AdventureMode.Lawn, false, 1401)]
        [TestCase(21, true, AdventureMode.Lawn, false, 501)]
        [TestCase(31, true, AdventureMode.Lawn, false, 801)]
        [TestCase(41, true, AdventureMode.Lawn, false, 1201)]
        [TestCase(11, false, AdventureMode.Lawn, false, -1)]
        [TestCase(1, false, AdventureMode.Lawn, false, 1601)]
        [TestCase(5, true, AdventureMode.Bowling, false, 2400)]
        [TestCase(5, false, AdventureMode.Bowling, false, 2411)]
        [TestCase(15, true, AdventureMode.WhackAZombie, false, 401)]
        [TestCase(25, true, AdventureMode.LittleTrouble, false, 701)]
        [TestCase(35, true, AdventureMode.Vasebreaker, false, 2500)]
        [TestCase(39, true, AdventureMode.Storm, false, 1101)]
        [TestCase(45, true, AdventureMode.Bungee, false, 1301)]
        [TestCase(45, false, AdventureMode.Bungee, false, 1304)]
        [TestCase(50, true, AdventureMode.Boss, false, 2300)]
        [TestCase(50, true, AdventureMode.Boss, true, -1)]
        public void SelectsOriginalOpening(int level, bool firstAdventure, AdventureMode mode, bool restart, int expected)
        {
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(level, firstAdventure, mode, false, false, restart), Is.EqualTo(expected));
        }

        /// <summary>Checks original slot upgrade boundaries, exclusions and branch precedence.</summary>
        [Test]
        public void SelectsPacketUpgradeInOriginalOrder()
        {
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(12, true, AdventureMode.Lawn, true, true, false), Is.EqualTo(1401));
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(13, true, AdventureMode.Lawn, true, false, false), Is.EqualTo(1501));
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(15, true, AdventureMode.WhackAZombie, true, false, false), Is.EqualTo(401));
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(16, true, AdventureMode.Lawn, true, true, false), Is.EqualTo(1501));
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(16, true, AdventureMode.Lawn, false, true, false), Is.EqualTo(1551));
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(20, true, AdventureMode.Lawn, true, true, false), Is.EqualTo(-1));
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(21, true, AdventureMode.Lawn, true, true, false), Is.EqualTo(501));
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(24, true, AdventureMode.Lawn, true, false, false), Is.EqualTo(1501));
            Assert.That(CrazyDaveDialogue.SelectAdventureStart(25, true, AdventureMode.LittleTrouble, true, true, false), Is.EqualTo(701));
        }

        /// <summary>Checks which original opening sequences are supported.</summary>
        /// <param name="index">The original starting dialogue index.</param>
        /// <param name="expected">Whether the opening is supported.</param>
        [TestCase(701, true)]
        [TestCase(1101, true)]
        [TestCase(1301, true)]
        [TestCase(1304, true)]
        [TestCase(1601, true)]
        [TestCase(2500, true)]
        [TestCase(401, true)]
        [TestCase(1501, true)]
        [TestCase(1551, true)]
        [TestCase(2300, true)]
        [TestCase(2400, true)]
        [TestCase(2411, true)]
        public void EnablesSupportedOpenings(int index, bool expected)
        {
            Assert.That(CrazyDaveDialogue.IsSupportedAdventureOpening(index), Is.EqualTo(expected));
        }

        /// <summary>Checks the native post-Dave jump for a non-scrolling vase opening.</summary>
        [Test]
        public void NonScrollingOpeningSkipsStreetPanAfterDave()
        {
            var plan = AdventureIntroPresentation.CreatePlan(new AdventureIntroContext
            {
                Level = 35,
                HasCrazyDaveDialog = true,
                ChooseSeedsOverride = false
            });
            Assert.That(plan.NonScrolling, Is.True);
            var clock = new AdventureIntroClock(plan);
            for (var tick = 0; tick < 399; tick++)
            {
                clock.Tick();
            }

            Assert.That(clock.NativeTime, Is.EqualTo(3990));
            clock.Tick();
            Assert.That(clock.NativeTime, Is.EqualTo(plan.PanLeftEnd));
            Assert.That(clock.Frame.BoardX, Is.EqualTo(0));
        }

        /// <summary>Checks that missing entries end dialogue instead of skipping to another group.</summary>
        [Test]
        public void AdvancesOnlyWhenNextEntryExists()
        {
            Assert.That(CrazyDaveDialogue.NextMessageIndex(201, index => index == 202), Is.EqualTo(202));
            Assert.That(CrazyDaveDialogue.NextMessageIndex(207, index => index == 301), Is.EqualTo(-1));
            Assert.That(CrazyDaveDialogue.NextMessageIndex(1303, index => index == 1304), Is.EqualTo(1304));
            Assert.That(CrazyDaveDialogue.NextMessageIndex(1311, index => index == 1401), Is.EqualTo(-1));
            Assert.That(CrazyDaveDialogue.NextMessageIndex(-1, index => true), Is.EqualTo(-1));
            Assert.That(CrazyDaveDialogue.NextMessageIndex(int.MaxValue, index => true), Is.EqualTo(-1));
            Assert.Throws<ArgumentNullException>(() => CrazyDaveDialogue.NextMessageIndex(201, null!));
        }

        /// <summary>Checks original currency formatting at its grouping boundaries.</summary>
        /// <param name="amount">The displayed amount to format.</param>
        /// <param name="expected">The expected original display.</param>
        [TestCase(9990, "$9990")]
        [TestCase(10000, "$10,000")]
        [TestCase(1000000, "$1,000,000")]
        public void FormatsOriginalMoney(long amount, string expected)
        {
            Assert.That(CrazyDaveDialogue.FormatMoney(amount), Is.EqualTo(expected));
        }

        /// <summary>Checks the original idle mouth image for each control marker.</summary>
        /// <param name="message">The original message.</param>
        /// <param name="expected">The requested original image identifier.</param>
        [TestCase("text {MOUTH_BIG_SMILE}", "IMAGE_REANIM_CRAZYDAVE_MOUTH1")]
        [TestCase("{MOUTH_SMALL_SMILE}", "IMAGE_REANIM_CRAZYDAVE_MOUTH5")]
        [TestCase("{MOUTH_BIG_OH}", "IMAGE_REANIM_CRAZYDAVE_MOUTH4")]
        [TestCase("{MOUTH_SMALL_OH}", "IMAGE_REANIM_CRAZYDAVE_MOUTH6")]
        [TestCase("{MOUTH_SMALL_OH}{MOUTH_BIG_SMILE}", "IMAGE_REANIM_CRAZYDAVE_MOUTH1")]
        [TestCase("no mouth marker", null)]
        public void SelectsOriginalIdleMouth(string message, string? expected)
        {
            Assert.That(CrazyDaveDialogue.IdleMouthImage(message), Is.EqualTo(expected));
        }
    }
}
