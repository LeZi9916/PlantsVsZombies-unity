#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using UnityEngine;
using UnityEngine.TestTools;
using PlantsVsZombies.Diagnostics;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Protects fixed audio capacity, category admission, state reuse, and component ownership.</summary>
    public sealed class AudioPoolingTests
    {
        /// <summary>Owns the template and the pool's reusable native sources.</summary>
        private GameObject _owner = null!;
        /// <summary>Provides routing and listener settings without being leased by the pool.</summary>
        private AudioSource _template = null!;
        /// <summary>Supplies a long sound whose lease remains active throughout synchronous checks.</summary>
        private AudioClip _clip = null!;
        /// <summary>Retains the pool for deterministic cleanup after each test.</summary>
        private AudioVoicePool _pool = null!;
        /// <summary>Preserves the caller's global audio pause state.</summary>
        private bool _previousPause;
        /// <summary>Preserves the caller's simulation time scale across unscaled playback checks.</summary>
        private float _previousTimeScale;

        /// <summary>Creates silent audio data and three prewarmed sound voices.</summary>
        [SetUp]
        public void SetUp()
        {
            _previousPause = AudioListener.pause;
            _previousTimeScale = Time.timeScale;
            AudioListener.pause = false;
            _owner = new GameObject("Audio pooling regression");
            _template = _owner.AddComponent<AudioSource>();
            _template.playOnAwake = false;
            _clip = AudioClip.Create("Pool sound", 441000, 1, 44100, false);
            _pool = new AudioVoicePool(_owner, _template, 3);
        }

        /// <summary>Releases the pool and synthetic audio without changing the caller's pause state.</summary>
        /// <returns>A frame boundary allowing Unity to destroy the native objects.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _pool.Dispose();
            UnityEngine.Object.Destroy(_owner);
            UnityEngine.Object.Destroy(_clip);
            AudioListener.pause = _previousPause;
            Time.timeScale = _previousTimeScale;
            yield return null;
        }

        /// <summary>Caps both category overlap and total overlap without creating or interrupting voices.</summary>
        [Test]
        public void EnforcesCategoryAndSharedCapacity()
        {
            var before = _owner.GetComponents<AudioSource>();
            Assert.That(before.Length, Is.EqualTo(_pool.Capacity + 1));
            Assert.That(_pool.CanPlay(25, 2), Is.True);
            Assert.That(_pool.TryPlay(_clip, .4f, group: 25, groupLimit: 2), Is.True);
            Assert.That(_pool.TryPlay(_clip, .4f, group: 25, groupLimit: 2), Is.True);
            Assert.That(_pool.CanPlay(25, 2), Is.False);
            Assert.That(_pool.CanPlay(-1), Is.True);
            Assert.That(_pool.TryPlay(_clip, .4f, group: 25, groupLimit: 2), Is.False);
            Assert.That(_pool.TryPlay(_clip, .8f, group: -1), Is.True);
            Assert.That(_pool.CanPlay(28), Is.False);
            Assert.That(_pool.TryPlay(_clip, .8f, group: 28), Is.False);
            Assert.That(_owner.GetComponents<AudioSource>(), Is.EqualTo(before));
            Assert.That(before[1].clip, Is.SameAs(_clip));
            Assert.That(before[2].clip, Is.SameAs(_clip));
            Assert.That(before[3].clip, Is.SameAs(_clip));
        }

        /// <summary>Replaces the oldest matching hit voice by start order rather than sample end time.</summary>
        [Test]
        public void SaturatedImpactCategoryReplacesItsOldestVoiceWithoutInterruptingOtherSounds()
        {
            var sources = _owner.GetComponents<AudioSource>();
            Assert.That(_pool.TryPlay(_clip, .1f, .5f, group: 1, groupLimit: 2), Is.True);
            Assert.That(_pool.TryPlay(_clip, .2f, 2f, group: 1, groupLimit: 2), Is.True);
            Assert.That(_pool.TryPlay(_clip, .3f, group: 25), Is.True);
            Assert.That(_pool.CanPlay(1, 2), Is.False);
            Assert.That(_pool.CanPlay(1, 2, replaceOldest: true), Is.True);
            Assert.That(_pool.TryPlay(_clip, .9f, 1.2f, 1, 2, replaceOldest: true), Is.True);
            Assert.That(sources[1].volume, Is.EqualTo(.9f).Within(.0001f));
            Assert.That(sources[1].pitch, Is.EqualTo(1.2f).Within(.0001f));
            Assert.That(sources[2].volume, Is.EqualTo(.2f).Within(.0001f));
            Assert.That(sources[3].volume, Is.EqualTo(.3f).Within(.0001f));
            Assert.That(_pool.CanPlay(58, 8, replaceOldest: true), Is.False);
            Assert.That(_pool.TryPlay(_clip, 1f, group: 58, groupLimit: 8, replaceOldest: true), Is.False);
            Assert.That(_owner.GetComponents<AudioSource>(), Is.EqualTo(sources));
        }

        /// <summary>Admits every request in a rapid hit burst while retaining eight original-category voices.</summary>
        [Test]
        public void RapidImpactBurstReusesEightVoicesWithoutGrowingThePool()
        {
            using var impacts = new AudioVoicePool(_owner, _template, 8);
            var sources = _owner.GetComponents<AudioSource>();
            for (var request = 0; request < 64; request++)
            {
                Assert.That(impacts.CanPlay(1, 8, replaceOldest: true), Is.True);
                Assert.That(impacts.TryPlay(_clip, request / 64f, group: 1, groupLimit: 8, replaceOldest: true), Is.True);
            }

            var active = 0;
            foreach (var source in sources)
            {
                if (source.clip != null)
                {
                    active++;
                    Assert.That(source.volume, Is.GreaterThanOrEqualTo(56f / 64f));
                }
            }

            Assert.That(active, Is.EqualTo(8));
            Assert.That(_owner.GetComponents<AudioSource>(), Is.EqualTo(sources));
        }

        /// <summary>Passes consecutive projectile hits through the game Foley route without the ambient ten-tick suppression.</summary>
        /// <param name="foley">The original projectile-impact sample category.</param>
        [TestCase(1)]
        [TestCase(32)]
        [TestCase(38)]
        [TestCase(43)]
        [TestCase(46)]
        [TestCase(58)]
        [TestCase(62)]
        public void GameFoleyRoutePlaysEveryRapidProjectileImpact(int foley)
        {
            using var impacts = new AudioVoicePool(_owner, _template, 8);
            var game = CreateFoleyFixture(impacts, out var host);
            try
            {
                for (var request = 0; request < 64; request++)
                {
                    GameMemberReflection.SetField(game, "_soundVolume", request / 64f);
                    PlayFoley(game, foley, 100);
                }

                var active = 0;
                foreach (var source in _owner.GetComponents<AudioSource>())
                {
                    if (source.clip != null)
                    {
                        active++;
                        Assert.That(source.volume, Is.GreaterThanOrEqualTo(56f / 64f));
                    }
                }

                Assert.That(active, Is.EqualTo(8));
            }
            finally
            {
                ((Dictionary<string, AudioClip>)GameMemberReflection.GetField(game, "_presentationSounds")!).Clear();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Preserves the original ten-update boundary for chewing and body-part Foley.</summary>
        /// <param name="foley">The category whose native admission window remains in effect.</param>
        [TestCase(5)]
        [TestCase(25)]
        public void GameFoleyRoutePreservesNonImpactSuppressionBoundary(int foley)
        {
            var game = CreateFoleyFixture(_pool, out var host);
            try
            {
                PlayFoley(game, foley, 100);
                PlayFoley(game, foley, 109);
                var sources = _owner.GetComponents<AudioSource>();
                Assert.That(sources[1].clip, Is.SameAs(_clip));
                Assert.That(sources[2].clip, Is.Null);
                PlayFoley(game, foley, 110);
                Assert.That(sources[2].clip, Is.SameAs(_clip));
            }
            finally
            {
                ((Dictionary<string, AudioClip>)GameMemberReflection.GetField(game, "_presentationSounds")!).Clear();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Releases occupied categories and resets gain, pitch, and listener settings on reuse.</summary>
        [Test]
        public void ReusesStoppedVoicesWithCurrentPlaybackSettings()
        {
            var sources = _owner.GetComponents<AudioSource>();
            Assert.That(_pool.TryPlay(_clip, .2f, .5f, -1), Is.True);
            Assert.That(_pool.TryPlay(_clip, .6f, 1.5f, 25), Is.True);
            _pool.SetVolume(.8f, -1);
            Assert.That(sources[1].volume, Is.EqualTo(.8f).Within(.0001f));
            Assert.That(sources[2].volume, Is.EqualTo(.6f).Within(.0001f));

            _pool.StopAll();
            foreach (var source in sources)
            {
                Assert.That(source.clip, Is.Null);
            }

            _template.ignoreListenerPause = true;
            _template.priority = 42;
            Assert.That(_pool.TryPlay(_clip, .9f, 1.7f, 25, 1), Is.True);
            Assert.That(sources[1].pitch, Is.EqualTo(1.7f).Within(.0001f));
            Assert.That(sources[1].volume, Is.EqualTo(.9f).Within(.0001f));
            Assert.That(sources[1].ignoreListenerPause, Is.True);
            Assert.That(sources[1].priority, Is.EqualTo(42));
            Assert.That(_pool.TryPlay(_clip, .9f, group: 25, groupLimit: 1), Is.False);
            Assert.That(_owner.GetComponents<AudioSource>(), Is.EqualTo(sources));
        }

        /// <summary>Keeps sounds requested while globally paused reserved instead of repeatedly reusing one source.</summary>
        /// <returns>A frame boundary while global listener playback remains paused.</returns>
        [UnityTest]
        public IEnumerator RetainsVoiceLeasesWhileListenerIsPaused()
        {
            AudioListener.pause = true;
            Assert.That(_pool.TryPlay(_clip, 1f, group: 5, groupLimit: 1), Is.True);
            yield return null;
            Assert.That(_pool.TryPlay(_clip, 1f, group: 5, groupLimit: 1), Is.False);
            Assert.That(_pool.TryPlay(_clip, 1f, group: 6), Is.True);
            Assert.That(_pool.TryPlay(_clip, 1f, group: 25), Is.True);
            Assert.That(_pool.TryPlay(_clip, 1f, group: 28), Is.False);
        }

        /// <summary>Reclaims listener-independent sounds even while listener and gameplay clocks are paused.</summary>
        /// <returns>Frame boundaries until the short synthetic sound has completed.</returns>
        [UnityTest]
        public IEnumerator ReclaimsCompletedVoices()
        {
            var shortClip = AudioClip.Create("Short pool sound", 441, 1, 44100, false);
            try
            {
                AudioListener.pause = true;
                Time.timeScale = 0f;
                _template.ignoreListenerPause = true;
                Assert.That(_pool.TryPlay(shortClip, 1f, group: 25, groupLimit: 1), Is.True);
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That(_pool.TryPlay(_clip, 1f, group: 25, groupLimit: 1), Is.True);
                Assert.That(_owner.GetComponents<AudioSource>().Length, Is.EqualTo(4));
            }
            finally
            {
                _pool.StopAll();
                UnityEngine.Object.Destroy(shortClip);
            }
        }

        /// <summary>Uses the audio clock to release normal sounds while simulation time is stopped.</summary>
        /// <returns>Frame boundaries until the short synthetic sound has completed.</returns>
        [UnityTest]
        public IEnumerator ReclaimsCompletedVoicesWhileGameplayIsStopped()
        {
            var shortClip = AudioClip.Create("Unscaled pool sound", 441, 1, 44100, false);
            try
            {
                Time.timeScale = 0f;
                Assert.That(_pool.TryPlay(shortClip, 1f, group: 25, groupLimit: 1), Is.True);
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That(_pool.TryPlay(_clip, 1f, group: 25, groupLimit: 1), Is.True);
            }
            finally
            {
                _pool.StopAll();
                UnityEngine.Object.Destroy(shortClip);
            }
        }

        /// <summary>Reuses warmed voices across bursts without introducing managed garbage.</summary>
        [Test]
        public void RepeatedBurstsDoNotAllocateManagedMemory()
        {
            for (var i = 0; i < 8; i++)
            {
                _pool.TryPlay(_clip, .5f, group: 25, groupLimit: 1);
                _pool.StopAll();
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++)
            {
                _pool.CanPlay(25, 1);
                _pool.TryPlay(_clip, .5f, group: 25, groupLimit: 1);
                _pool.SetVolume(.6f, 25);
                _pool.StopAll();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        /// <summary>Keeps saturated hit replacement free of managed allocation after the fixed voices are initialized.</summary>
        [Test]
        public void RepeatedImpactReplacementDoesNotAllocateManagedMemory()
        {
            for (var request = 0; request < 8; request++)
            {
                _pool.TryPlay(_clip, .5f, group: 1, groupLimit: 2, replaceOldest: true);
            }

            var admitted = true;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var request = 0; request < 128; request++)
            {
                admitted &= _pool.CanPlay(1, 2, replaceOldest: true);
                admitted &= _pool.TryPlay(_clip, .5f, group: 1, groupLimit: 2, replaceOldest: true);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(admitted, Is.True);
            Assert.That(allocated, Is.Zero);
        }

        /// <summary>Creates an inactive game coordinator with synthetic samples and the caller's active voice pool.</summary>
        /// <param name="pool">The fixed voice pool receiving Foley requests.</param>
        /// <param name="host">The inactive object owned by the test.</param>
        /// <returns>The coordinator configured without bootstrapping scenes, resources, or saved progress.</returns>
        private PvZGame CreateFoleyFixture(AudioVoicePool pool, out GameObject host)
        {
            host = new GameObject("Inactive Foley route regression");
            host.SetActive(false);
            var game = host.AddComponent<PvZGame>();
            GameMemberReflection.SetField(game, "_soundVoices", pool);
            GameMemberReflection.SetField(game, "_soundVolume", .7f);
            var samples = (Dictionary<string, AudioClip>)GameMemberReflection.GetField(game, "_presentationSounds")!;
            foreach (var definition in AdventureZombieFoleyData.Definitions.Values)
            {
                foreach (var sample in definition.Samples)
                {
                    samples[sample] = _clip;
                }
            }

            return game;
        }

        /// <summary>Invokes the production Foley admission and native sample selection path.</summary>
        /// <param name="game">The inactive coordinator configured by the fixture.</param>
        /// <param name="foley">The native sample category.</param>
        /// <param name="tick">The native simulation update associated with the request.</param>
        private static void PlayFoley(PvZGame game, int foley, int tick)
        {
            GameMemberReflection.Invoke(game, "PlayAdventureZombieFoley", new object[] { new AdventureZombieSoundEvent { Foley = foley, Tick = tick } });
        }

        /// <summary>Destroys only leased components and tolerates repeated disposal.</summary>
        /// <returns>A frame boundary allowing the owned audio components to disappear.</returns>
        [UnityTest]
        public IEnumerator DisposesOwnedVoicesWithoutDestroyingTemplate()
        {
            _pool.TryPlay(_clip, 1f);
            _pool.Dispose();
            _pool.Dispose();
            Assert.That(_pool.TryPlay(_clip, 1f), Is.False);
            yield return null;
            Assert.That(_owner.GetComponents<AudioSource>(), Is.EqualTo(new[] { _template }));
        }
    }
}
