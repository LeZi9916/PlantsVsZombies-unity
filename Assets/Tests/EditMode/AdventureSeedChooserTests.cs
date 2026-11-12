#nullable enable

using System;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Regresses original PC seed chooser ordering, packet motion, and bank spacing.</summary>
    public sealed class AdventureSeedChooserTests
    {
        /// <summary>Checks 25-tick packet flights and selection order survive cancelling and selecting again.</summary>
        [Test]
        public void SelectionPreservesClickOrderAndReturnsPacketsFromTheBank()
        {
            var state = new AdventureSeedChooserState(Array.Empty<PlantKind>());
            Assert.That(state.Choose(PlantKind.Wallnut, 181, 128, 8), Is.True);
            state.GetPosition(PlantKind.Wallnut, 181, 128, 8, out var x, out var y);
            Assert.That((x, y), Is.EqualTo((181, 128)));
            state.Advance(0.12f);
            state.GetPosition(PlantKind.Wallnut, 181, 128, 8, out x, out y);
            Assert.That(x, Is.InRange(82, 180));
            Assert.That(y, Is.InRange(9, 127));
            state.Advance(0.12f);
            Assert.That(state.IsMoving(PlantKind.Wallnut), Is.True);
            state.Advance(0.01f);
            Assert.That(state.IsMoving(PlantKind.Wallnut), Is.False);
            state.GetPosition(PlantKind.Wallnut, 181, 128, 8, out x, out y);
            Assert.That((x, y), Is.EqualTo((81, 8)));

            Assert.That(state.Choose(PlantKind.Peashooter, 22, 128, 8), Is.True);
            state.LandFlyingSeeds();
            Assert.That(state.Choose(PlantKind.Sunflower, 75, 128, 8), Is.True);
            state.LandFlyingSeeds();
            CollectionAssert.AreEqual(new[] { PlantKind.Wallnut, PlantKind.Peashooter, PlantKind.Sunflower }, state);
            Assert.That(state.Return(PlantKind.Peashooter, 22, 128, 8), Is.True);
            CollectionAssert.AreEqual(new[] { PlantKind.Wallnut, PlantKind.Sunflower }, state);
            state.Advance(0.14f);
            Assert.That(state.IsMoving(PlantKind.Sunflower), Is.True);
            state.Advance(0.01f);
            Assert.That(state.IsMoving(PlantKind.Sunflower), Is.False);
            Assert.That(state.IsMoving(PlantKind.Peashooter), Is.True);
            state.Advance(0.10f);
            Assert.That(state.HasFlyingSeeds, Is.False);
            state.GetPosition(PlantKind.Peashooter, 22, 128, 8, out x, out y);
            Assert.That((x, y), Is.EqualTo((22, 128)));
            Assert.That(state.Choose(PlantKind.Peashooter, 22, 128, 8), Is.True);
            CollectionAssert.AreEqual(new[] { PlantKind.Wallnut, PlantKind.Sunflower, PlantKind.Peashooter }, state);
        }

        /// <summary>Checks a new click can finish earlier flights before appending another selected packet.</summary>
        [Test]
        public void LandingFlightsAllowsRapidOrderedSelectionWithoutDuplicates()
        {
            var state = new AdventureSeedChooserState(Array.Empty<PlantKind>());
            Assert.That(state.Choose(PlantKind.Sunflower, 75, 128, 2), Is.True);
            state.LandFlyingSeeds();
            Assert.That(state.Choose(PlantKind.Peashooter, 22, 128, 2), Is.True);
            state.LandFlyingSeeds();
            Assert.That(state.Choose(PlantKind.Wallnut, 181, 128, 2), Is.False);
            Assert.That(state.Choose(PlantKind.Peashooter, 22, 128, 2), Is.False);
            Assert.That(state.Count, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { PlantKind.Sunflower, PlantKind.Peashooter }, state);
        }

        /// <summary>Checks packet spacing follows the actual capacity used by the original PC board.</summary>
        /// <param name="slots">The configured seed bank capacity.</param>
        /// <param name="first">The first packet's native X coordinate.</param>
        /// <param name="stride">The native distance between packet origins.</param>
        [TestCase(6, 85, 59)]
        [TestCase(7, 85, 59)]
        [TestCase(8, 81, 54)]
        [TestCase(9, 80, 52)]
        [TestCase(10, 79, 51)]
        public void PacketSpacingUsesNativeCapacity(int slots, int first, int stride)
        {
            var state = new AdventureSeedChooserState(Array.Empty<PlantKind>());
            Assert.That(state.Choose(PlantKind.Wallnut, 181, 128, slots), Is.True);
            state.LandFlyingSeeds();
            state.GetPosition(PlantKind.Wallnut, 181, 128, slots, out var x, out var y);
            Assert.That((x, y), Is.EqualTo((first, 8)));
            Assert.That(AdventureSeedSlotLayout.PositionX(1, slots), Is.EqualTo(first + stride));
        }

        /// <summary>Keeps four selected cards in the ten-slot layout and preserves that layout during a return flight.</summary>
        [Test]
        public void PartialSelectionDoesNotChangeTheConfiguredBankDensity()
        {
            var state = new AdventureSeedChooserState(new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.CherryBomb, PlantKind.Wallnut });
            for (var index = 0; index < state.Count; index++)
            {
                state.GetPosition(state[index], 22, 128, 10, out var x, out var y);
                Assert.That((x, y), Is.EqualTo((79 + index * 51, 8)));
            }

            Assert.That(state.Return(PlantKind.Sunflower, 75, 128, 10), Is.True);
            state.Advance(.15f);
            state.GetPosition(PlantKind.Wallnut, 181, 128, 10, out var lastX, out var lastY);
            Assert.That((lastX, lastY), Is.EqualTo((181, 8)));
            Assert.That(state.Count, Is.EqualTo(3));
        }

        /// <summary>Checks settled sampling and animation updates reuse all managed state.</summary>
        [Test]
        public void SettledPacketSamplingDoesNotAllocate()
        {
            var state = new AdventureSeedChooserState(new[] { PlantKind.Wallnut, PlantKind.Peashooter });
            state.GetPosition(PlantKind.Peashooter, 22, 128, 8, out _, out _);
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                state.Advance(0.01f);
                state.GetPosition(PlantKind.Peashooter, 22, 128, 8, out _, out _);
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}
