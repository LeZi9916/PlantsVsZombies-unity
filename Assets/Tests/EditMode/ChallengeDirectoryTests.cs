#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Modes;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC 43AD10 directory tooltip branches for both puzzle chains and mini-games.</summary>
    public sealed class ChallengeDirectoryTests
    {
        /// <summary>Checks original localized keys without a replacement lock dialog.</summary>
        /// <param name="mode">The original challenge identifier.</param>
        /// <param name="finished">Whether adventure has been completed.</param>
        /// <param name="key">The original tooltip key.</param>
        [TestCase(19, false, "FINISH_ADVENTURE_TOOLTIP")]
        [TestCase(19, true, "ONE_MORE_CHALLENGE_TOOLTIP")]
        [TestCase(52, false, "ONE_MORE_SCARY_POTTER_TOOLTIP")]
        [TestCase(54, false, "FINISH_ADVENTURE_TOOLTIP")]
        [TestCase(54, true, "ONE_MORE_SCARY_POTTER_TOOLTIP")]
        [TestCase(62, false, "ONE_MORE_IZOMBIE_TOOLTIP")]
        [TestCase(64, false, "FINISH_ADVENTURE_TOOLTIP")]
        [TestCase(64, true, "ONE_MORE_IZOMBIE_TOOLTIP")]
        [TestCase(60, true, "ONE_MORE_SCARY_POTTER_TOOLTIP")]
        [TestCase(70, true, "ONE_MORE_IZOMBIE_TOOLTIP")]
        public void LockedPreviewsUseNativeTooltip(int mode, bool finished, string key)
        {
            Assert.That(ModeUnlockRules.LockTooltipKey(mode, finished), Is.EqualTo(key));
        }

        /// <summary>Checks that a vasebreaker trophy cannot advance the independent I, Zombie unlock chain.</summary>
        [Test]
        public void PuzzleGroupsProgressIndependentlyAndStopBeforeAdventureCompletion()
        {
            Assert.That(ModeUnlockRules.DirectoryAccomplishmentsNeeded(54, false, index => index >= 50 && index <= 52 ? 1 : 0), Is.EqualTo(1));
            Assert.That(ModeUnlockRules.DirectoryAccomplishmentsNeeded(54, true, index => index >= 50 && index <= 52 ? 1 : 0), Is.Zero);
            Assert.That(ModeUnlockRules.DirectoryAccomplishmentsNeeded(62, true, index => index >= 50 && index <= 58 ? 1 : 0), Is.EqualTo(1));
            Assert.That(ModeUnlockRules.DirectoryAccomplishmentsNeeded(70, true, index => index >= 60 && index <= 68 ? 1 : 0), Is.Zero);
        }
    }
}
