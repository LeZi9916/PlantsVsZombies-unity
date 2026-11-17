#nullable enable
using System;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Rendering;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks allocation-free model identities and compatibility with external string callers.</summary>
    public sealed class PresentationIdentityTests
    {
        /// <summary>Checks independent roles, parts, attachments and matches cannot share cached playback state.</summary>
        [Test]
        public void CompleteIdentitySeparatesRolesPartsAttachmentsAndSessions()
        {
            var first = PresentationIdentity.Create("plant-", 17, -1, 1);
            Assert.That(first, Is.EqualTo(PresentationIdentity.Create("plant-", 17, -1, 1)));
            Assert.That(first, Is.Not.EqualTo(PresentationIdentity.Create("zombie-", 17, -1, 1)));
            Assert.That(first, Is.Not.EqualTo(PresentationIdentity.Create("plant-", 17, 0, 1)));
            Assert.That(first, Is.Not.EqualTo(PresentationIdentity.Create("plant-", 17, -1, 2)));
            Assert.That(first.WithAttachment(1), Is.Not.EqualTo(first.WithAttachment(2)));
            Assert.That(first.WithRole("producer-"), Is.Not.EqualTo(first));
        }

        /// <summary>Preserves ordinal external string equality without conflating it with a numeric source identity.</summary>
        [Test]
        public void LegacyKeysRemainOrdinalAndDistinctFromNumericIdentities()
        {
            var first = PresentationIdentity.FromExternal("plant-17");
            var copied = PresentationIdentity.FromExternal(new string("plant-17".ToCharArray()));
            Assert.That(first, Is.EqualTo(copied));
            Assert.That(first.GetHashCode(), Is.EqualTo(copied.GetHashCode()));
            Assert.That(first, Is.Not.EqualTo(PresentationIdentity.FromExternal("Plant-17")));
            Assert.That(first, Is.Not.EqualTo(PresentationIdentity.Create("plant-", 17, -1, 0)));
            Assert.That(PresentationIdentity.FromExternal(null).IsEmpty, Is.True);
            Assert.That(first.WithRole("producer-"), Is.Not.EqualTo(first.WithRole("other-")));
        }

        /// <summary>Ensures previously unseen entity identifiers do not require strings or growing identity dictionaries.</summary>
        [Test]
        public void DistinctEntityKeysAllocateNoManagedMemory()
        {
            var cache = new PresentationKeyCache();
            cache.SetSession(this);
            var hash = cache.Get("effect-", 1).GetHashCode();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var id = 2; id < 10002; id++)
            {
                hash ^= cache.Get("effect-", id, id % 3).WithAttachment(1).GetHashCode();
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            GC.KeepAlive(hash);
            Assert.That(allocated, Is.Zero);
        }
    }
}
