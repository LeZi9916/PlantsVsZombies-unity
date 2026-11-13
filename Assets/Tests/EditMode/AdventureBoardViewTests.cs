#nullable enable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using Unity.Profiling;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks live read-only adventure collections and immutable presentation identities.</summary>
    public sealed class AdventureBoardViewTests
    {
        /// <summary>Checks every board view is stable, reflects mutations, and prevents external collection changes.</summary>
        [Test]
        public void ViewsRemainStableLiveAndExternallyReadOnly()
        {
            var board = new GameSimulation(new GameRules { AdventureLevel = 8 }).Adventure;
            CheckView(board._packets, () => board.Packets, new AdventurePacket());
            CheckView(board._vases, () => board.Vases, new AdventureVase());
            CheckView(board._graves, () => board.Graves, new AdventureCellTimer());
            CheckView(board._craters, () => board.Craters, new AdventureCellTimer());
            CheckView(board._ice, () => board.IceTrails, new AdventureIceTrail());
            CheckView(board._balls, () => board.BossBalls, new AdventureBossBall());
            CheckView(board._nuts, () => board.BowlingNuts, new AdventureBowlingNut());
            CheckView(board._coins, () => board.Coins, new AdventureCoin());
            CheckView(board._cobShells, () => board.CobShells, new AdventureCobShell());
        }

        /// <summary>Checks views sharing an element type or level remain independent.</summary>
        [Test]
        public void ViewsAreIndependentAcrossCollectionsAndBoards()
        {
            var first = new GameSimulation(new GameRules { AdventureLevel = 8 }).Adventure;
            var second = new GameSimulation(new GameRules { AdventureLevel = 8 }).Adventure;
            first._graves.Clear();
            first._craters.Clear();
            second._graves.Clear();
            var grave = new AdventureCellTimer();
            first._graves.Add(grave);

            Assert.That(first.Graves, Is.Not.SameAs(first.Craters));
            Assert.That(first.Graves, Is.Not.SameAs(second.Graves));
            Assert.That(first.Graves, Is.EqualTo(new[] { grave }));
            Assert.That(first.Craters, Is.Empty);
            Assert.That(second.Graves, Is.Empty);
        }

        /// <summary>Checks warmed access to all board views has no managed allocation.</summary>
        [Test]
        public void RepeatedViewQueriesAllocateNoBytes()
        {
            var board = new GameSimulation(new GameRules { AdventureLevel = 8 }).Adventure;
            ReadViews(board);
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 32768,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            recorder.Stop();
            recorder.Reset();
            recorder.Start();
            for (var i = 0; i < 1000; i++)
            {
                ReadViews(board);
            }

            recorder.Stop();
            Assert.That(recorder.Valid, Is.True);
            Assert.That(recorder.Count, Is.Zero);
        }

        /// <summary>Checks the allocation observer detects a deliberate allocation on the active Unity runtime.</summary>
        [Test]
        public void AllocationObserverDetectsAllocatedArray()
        {
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 32768,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            recorder.Stop();
            recorder.Reset();
            recorder.Start();
            var allocated = new byte[1024];
            recorder.Stop();

            GC.KeepAlive(allocated);
            Assert.That(recorder.Valid, Is.True);
            Assert.That(recorder.Count, Is.GreaterThan(0));
        }

        /// <summary>Checks original ground lookup case semantics and allocation-free repeated animation queries.</summary>
        [Test]
        public void NativeGroundQueriesRetainCaseInsensitiveResultsWithoutFormattingKeys()
        {
            Assert.That(AdventureZombieGroundData.TryGet("Zombie", "anim_walk", out var expected), Is.True);
            Assert.That(AdventureZombieGroundData.TryGet("zOmBiE", "ANIM_WALK", out var actual), Is.True);
            Assert.That(actual, Is.SameAs(expected));
            Assert.That(AdventureZombieGroundData.TryGet("Zombie", "missing_clip", out _), Is.False);
            Assert.That(AdventureZombieGroundData.TryGet("missing_asset", "anim_walk", out _), Is.False);
            Assert.That(AdventureZombieGroundData.TryGet(null!, "anim_walk", out _), Is.False);
            Assert.That(AdventureZombieGroundData.TryGet("Zombie", null!, out _), Is.False);
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 32768,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            recorder.Stop();
            recorder.Reset();
            recorder.Start();
            for (var index = 0; index < 1000; index++)
            {
                AdventureZombieGroundData.TryGet("Zombie", "anim_walk", out _);
                AdventureZombieGroundData.TryGet("zOmBiE", "ANIM_WALK", out _);
            }
            recorder.Stop();

            Assert.That(recorder.Valid, Is.True);
            Assert.That(recorder.Count, Is.Zero);
        }

        /// <summary>Checks particle resource names retain the enum text and remain allocation-free for supported effects.</summary>
        [Test]
        public void ExplosionNamesMatchEnumTextWithoutRepeatedFormatting()
        {
            var effects = (AdventureExplosionEffect[])Enum.GetValues(typeof(AdventureExplosionEffect));
            var explosion = new AdventureExplosion();
            foreach (var effect in effects)
            {
                explosion.Effect = effect;
                Assert.That(explosion.Particle, Is.EqualTo(effect.ToString()));
            }

            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 32768,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            recorder.Stop();
            recorder.Reset();
            recorder.Start();
            for (var i = 0; i < 1000; i++)
            {
                explosion.Effect = effects[i % effects.Length];
                GC.KeepAlive(explosion.Particle);
            }

            recorder.Stop();
            Assert.That(recorder.Valid, Is.True);
            Assert.That(recorder.Count, Is.Zero);
            explosion.Effect = (AdventureExplosionEffect)(-1);
            Assert.That(explosion.Particle, Is.EqualTo("-1"));
        }

        /// <summary>Checks effect identities preserve their text, reuse active entries, and retire after two absent frames.</summary>
        [Test]
        public void EffectKeysReuseExactTextAndRetireInactiveEntries()
        {
            var cache = new PresentationKeyCache();
            cache.BeginFrame();
            var explosion = cache.Get("native-explosion-", 123);
            var trail = cache.Get("snow-trail-", 123);
            Assert.That(explosion, Is.EqualTo("native-explosion-123"));
            Assert.That(trail, Is.EqualTo("snow-trail-123"));
            cache.BeginFrame();
            Assert.That(cache.Get("native-explosion-", 123), Is.SameAs(explosion));
            Assert.That(cache.Get("snow-trail-", 123), Is.SameAs(trail));
            cache.BeginFrame();
            cache.BeginFrame();
            Assert.That(cache.Get("native-explosion-", 123), Is.Not.SameAs(explosion));
            Assert.That(cache.Get("snow-trail-", 123), Is.Not.SameAs(trail));
        }

        /// <summary>Checks explicit disposal clears both current and preceding frame identities.</summary>
        [Test]
        public void ClearingEffectKeysReleasesBothRetainedFrames()
        {
            var cache = new PresentationKeyCache();
            var previous = cache.Get("arm-", 1);
            cache.BeginFrame();
            var current = cache.Get("arm-", 2);
            cache.Clear();
            cache.Clear();

            Assert.That(cache.Get("arm-", 1), Is.Not.SameAs(previous));
            Assert.That(cache.Get("arm-", 2), Is.Not.SameAs(current));
        }

        /// <summary>Checks frozen silhouette head and equipment rules across consecutive callback contexts.</summary>
        /// <param name="kind">The original zombie species.</param>
        /// <param name="headless">Whether its head has detached.</param>
        /// <param name="modeHead">The replacement plant head identifier.</param>
        /// <param name="variant">The retained equipment variant.</param>
        /// <param name="animation">The animation resource name.</param>
        /// <param name="track">The tested original track.</param>
        /// <param name="expected">Whether that track must remain visible.</param>
        [TestCase(ZombieKind.Normal, false, 0, ZombieVariant.Normal, "Zombie", "anim_head1", true)]
        [TestCase(ZombieKind.Normal, true, 0, ZombieVariant.Normal, "Zombie", "anim_head1", false)]
        [TestCase(ZombieKind.Normal, true, 0, ZombieVariant.Normal, "Zombie", "anim_hair", false)]
        [TestCase(ZombieKind.Normal, false, 26, ZombieVariant.Normal, "Zombie", "anim_head1", false)]
        [TestCase(ZombieKind.Normal, false, 26, ZombieVariant.Normal, "Zombie", "Zombie_tie", true)]
        [TestCase(ZombieKind.Normal, false, 27, ZombieVariant.Normal, "Zombie", "Zombie_tie", false)]
        [TestCase(ZombieKind.Normal, false, 29, ZombieVariant.Normal, "Zombie", "Zombie_tie", true)]
        [TestCase(ZombieKind.Normal, false, 30, ZombieVariant.Normal, "Zombie", "Zombie_tie", true)]
        [TestCase(ZombieKind.Normal, false, 31, ZombieVariant.Normal, "Zombie", "Zombie_tie", false)]
        [TestCase(ZombieKind.Conehead, false, 0, ZombieVariant.Conehead, "Zombie", "anim_cone", true)]
        [TestCase(ZombieKind.Conehead, true, 0, ZombieVariant.Conehead, "Zombie", "anim_cone", false)]
        [TestCase(ZombieKind.Buckethead, false, 0, ZombieVariant.Normal, "Zombie", "anim_bucket", false)]
        [TestCase(ZombieKind.DuckyTube, false, 0, ZombieVariant.Normal, "Zombie", "Zombie_duckytube", true)]
        [TestCase(ZombieKind.Football, false, 0, ZombieVariant.Normal, "Zombie_football", "anim_cone", true)]
        public void FrozenCorpseVisibilityPreservesHeadAndEquipmentRules(ZombieKind kind, bool headless, int modeHead,
            ZombieVariant variant, string animation, string track, bool expected)
        {
            var zombie = new Zombie { Kind = kind, Headless = headless, ModeHead = modeHead };
            var plantHead = modeHead >= 26 && modeHead <= 31;

            Assert.That(GamePresenter.IsBurnedCorpseTrackVisible(zombie, animation, variant, plantHead, track), Is.EqualTo(expected));
        }

        /// <summary>Checks the shared live-view contract for one element type.</summary>
        /// <typeparam name="T">The collection's reference element type.</typeparam>
        /// <param name="source">The internal mutable collection.</param>
        /// <param name="getView">The public view accessor.</param>
        /// <param name="item">The unique value added during the check.</param>
        private static void CheckView<T>(List<T> source, Func<IReadOnlyList<T>> getView, T item)
        {
            source.Clear();
            var view = getView();
            Assert.That(getView(), Is.SameAs(view));
            source.Add(item);
            Assert.That(view.Count, Is.EqualTo(1));
            Assert.That(view[0], Is.SameAs(item));
            var mutableInterface = view as IList<T>;
            Assert.That(mutableInterface, Is.Not.Null);
            Assert.That(mutableInterface!.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableInterface.Add(item));
            Assert.Throws<NotSupportedException>(() => mutableInterface[0] = item);
            Assert.Throws<NotSupportedException>(() => mutableInterface.RemoveAt(0));
            source.RemoveAt(0);
            Assert.That(view, Is.Empty);
            Assert.That(getView(), Is.SameAs(view));
        }

        /// <summary>Consumes each public view without boxing a collection enumerator.</summary>
        /// <param name="board">The board whose retained collection identities are consumed.</param>
        private static void ReadViews(AdventureBoard board)
        {
            GC.KeepAlive(board.Packets);
            GC.KeepAlive(board.Vases);
            GC.KeepAlive(board.Graves);
            GC.KeepAlive(board.Craters);
            GC.KeepAlive(board.IceTrails);
            GC.KeepAlive(board.BossBalls);
            GC.KeepAlive(board.BowlingNuts);
            GC.KeepAlive(board.Coins);
            GC.KeepAlive(board.CobShells);
        }
    }
}
