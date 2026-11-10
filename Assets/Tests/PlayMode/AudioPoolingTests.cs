#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Audio;
using UnityEngine;
using UnityEngine.TestTools;

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
