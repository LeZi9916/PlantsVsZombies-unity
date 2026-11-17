#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks periodic listener validation preserves dynamic external ownership without allocating lookup arrays.</summary>
    public sealed class AudioListenerScanTests
    {
        /// <summary>The inactive owner keeping resource initialization outside this focused fixture.</summary>
        private GameObject? _host;
        /// <summary>The production music coordinator under test.</summary>
        private DynamicMusicPlayer _music = null!;
        /// <summary>Owns only listener objects created by this fixture.</summary>
        private readonly List<GameObject> _objects = new List<GameObject>();
        /// <summary>Preserves enabled state for listeners already owned by the surrounding runner.</summary>
        private readonly List<AudioListener> _previousListeners = new List<AudioListener>();
        /// <summary>The private fallback listener created by the unchanged missing-listener policy.</summary>
        private static readonly FieldInfo s_ownedListener = typeof(DynamicMusicPlayer).GetField("_ownedMusicListener", BindingFlags.Instance | BindingFlags.NonPublic)!;
        /// <summary>The hidden persistent-scene lifetime anchor created by the production scan.</summary>
        private static readonly FieldInfo s_sceneAnchor = typeof(DynamicMusicPlayer).GetField("_audioListenerSceneAnchor", BindingFlags.Instance | BindingFlags.NonPublic)!;

        /// <summary>Creates an isolated production coordinator while temporarily disabling preexisting active listeners.</summary>
        /// <returns>A real frame so listener component activation has completed.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            foreach (var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (listener != null && listener.enabled)
                {
                    _previousListeners.Add(listener);
                    listener.enabled = false;
                }
            }
            _host = new GameObject("Inactive listener validation fixture");
            _host.SetActive(false);
            var game = _host.AddComponent<PvZGame>();
            _music = game.Music;
        }

        /// <summary>Releases fixture-owned listeners and restores runner-owned listener activation.</summary>
        /// <returns>A real frame completing deferred Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var owned = s_ownedListener.GetValue(_music) as AudioListener;
            if (owned != null)
            {
                UnityEngine.Object.DestroyImmediate(owned.gameObject);
            }
            var anchor = s_sceneAnchor.GetValue(_music) as GameObject;
            if (anchor != null)
            {
                UnityEngine.Object.DestroyImmediate(anchor);
            }
            foreach (var node in _objects)
            {
                if (node != null)
                {
                    UnityEngine.Object.DestroyImmediate(node);
                }
            }
            _objects.Clear();
            if (_host != null)
            {
                UnityEngine.Object.DestroyImmediate(_host);
                _host = null;
            }
            foreach (var listener in _previousListeners)
            {
                if (listener != null)
                {
                    listener.enabled = true;
                }
            }
            _previousListeners.Clear();
            yield return null;
        }

        /// <summary>Checks new listeners, disabled components, inactive parents and destroyed native instances remain discoverable safely.</summary>
        [Test]
        public void ExternalListenerActivityAndNativeDestructionStayLive()
        {
            var disabled = CreateListener("Disabled external", false);
            _music.EnsureOriginalAudioListener();
            var owned = (AudioListener)s_ownedListener.GetValue(_music)!;
            Assert.That(owned, Is.Not.Null);
            Assert.That(owned.enabled, Is.True);
            disabled.enabled = true;
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.False);
            disabled.enabled = false;
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.True);
            var added = CreateListener("Newly enabled external", true);
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.False, "A newly created external listener must replace the owned fallback without a stale cache.");
            added.gameObject.SetActive(false);
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.True);
            added.gameObject.SetActive(true);
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.False);
            UnityEngine.Object.DestroyImmediate(added);
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.True, "Destroyed native component wrappers must be skipped instead of dereferenced.");
            Assert.DoesNotThrow(_music.EnsureOriginalAudioListener);
        }

        /// <summary>Checks repeated warmed scans and externally toggled listeners allocate no managed bytes.</summary>
        [Test]
        public void WarmListenerChecksAllocateNoManagedMemory()
        {
            var external = CreateListener("Warmed external", false);
            _music.EnsureOriginalAudioListener();
            external.enabled = true;
            _music.EnsureOriginalAudioListener();
            external.enabled = false;
            _music.EnsureOriginalAudioListener();
            var growth = _music.AudioListenerScanCapacityGrowthCount;
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var check = 0; check < 100; check++)
            {
                external.enabled = check % 2 == 0;
                _music.EnsureOriginalAudioListener();
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(_music.AudioListenerScanCapacityGrowthCount, Is.EqualTo(growth));
            Assert.That(allocated, Is.Zero, "Periodic validation must reuse scene roots and component results after preparation.");
        }

        /// <summary>Checks dynamically created persistent listeners retain their original priority over the owned fallback.</summary>
        [Test]
        public void PersistentSceneListenersAreNotOmittedFromValidation()
        {
            _music.EnsureOriginalAudioListener();
            var owned = (AudioListener)s_ownedListener.GetValue(_music)!;
            var external = CreateListener("Persistent external", true);
            UnityEngine.Object.DontDestroyOnLoad(external.gameObject);
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.False);
            external.enabled = false;
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.True);
        }

        /// <summary>Checks explicit teardown releases the persistent-scene anchor once and a later check rebuilds it safely.</summary>
        [Test]
        public void ScanOwnershipReleaseIsIdempotentAndDoesNotTouchExternalListeners()
        {
            var external = CreateListener("External preserved across scan release", true);
            _music.EnsureOriginalAudioListener();
            var anchor = s_sceneAnchor.GetValue(_music);
            Assert.That(anchor, Is.Not.Null);
            _music.ReleaseAudioListenerScan();
            _music.ReleaseAudioListenerScan();
            Assert.That(s_sceneAnchor.GetValue(_music), Is.Null);
            Assert.That(external.enabled, Is.True);
            _music.EnsureOriginalAudioListener();
            Assert.That(s_sceneAnchor.GetValue(_music), Is.Not.Null.And.Not.SameAs(anchor));
            Assert.That(external.enabled, Is.True);
        }

        /// <summary>Checks additive scene activation is scanned again without invalidating a permanent listener-selection cache.</summary>
        /// <returns>Frames unloading only the fixture-created additive scene.</returns>
        [UnityTest]
        public IEnumerator AddedLoadedSceneListenerOverridesFallbackAndUnloadRestoresIt()
        {
            _music.EnsureOriginalAudioListener();
            var owned = (AudioListener)s_ownedListener.GetValue(_music)!;
            var scene = SceneManager.CreateScene("Listener scan additive " + Guid.NewGuid().ToString("N"));
            var external = CreateListener("Additive external", true);
            SceneManager.MoveGameObjectToScene(external.gameObject, scene);
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.False);
            yield return SceneManager.UnloadSceneAsync(scene);
            _music.EnsureOriginalAudioListener();
            Assert.That(owned.enabled, Is.True);
        }

        /// <summary>Creates one fixture-owned external listener without starting the real game's resource lifecycle.</summary>
        /// <param name="name">The diagnostic object name.</param>
        /// <param name="enabled">The requested external component activation.</param>
        /// <returns>The external listener.</returns>
        private AudioListener CreateListener(string name, bool enabled)
        {
            var node = new GameObject(name);
            _objects.Add(node);
            var listener = node.AddComponent<AudioListener>();
            listener.enabled = enabled;
            return listener;
        }
    }
}
