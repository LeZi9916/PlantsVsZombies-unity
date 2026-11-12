#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects animation lookup, sampling, and native placement from repeated frame work.</summary>
    public sealed class ReanimationHotPathTests
    {
        /// <summary>The isolated directory containing a synthetic original animation.</summary>
        private string _root = null!;
        /// <summary>The renderer owning the synthetic texture and animation caches.</summary>
        private ReanimationRenderer _renderer = null!;
        /// <summary>The number of tracks visited by the current native drawing filter.</summary>
        private int _filterCalls;

        /// <summary>Creates a four-frame animation with two distinct named clips and a ground track.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "PvZReanimationTests-" + Guid.NewGuid().ToString("N"));
            var reanim = Directory.CreateDirectory(Path.Combine(_root, "reanim")).FullName;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                File.WriteAllBytes(Path.Combine(reanim, "sprite.png"), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            File.WriteAllText(Path.Combine(reanim, "Synthetic.reanim"),
                "<fps>12</fps>"
                + "<track><name>anim_idle</name><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_alt</name><t><f>-1</f></t><t/><t><f>0</f></t><t/></track>"
                + "<track><name>body</name><t><i>IMAGE_REANIM_sprite</i><x>0</x></t><t><x>8</x></t><t><x>16</x></t><t><x>24</x></t></track>"
                + "<track><name>_ground</name><t><x>0</x></t><t><x>12</x></t><t><x>24</x></t><t><x>36</x></t></track>");
            File.WriteAllText(Path.Combine(reanim, "SplitPea.reanim"),
                File.ReadAllText(Path.Combine(reanim, "Synthetic.reanim"))
                + "<track><name>anim_head_idle</name><t/><t/><t/><t/></track>"
                + "<track><name>anim_splitpea_idle</name><t/><t/><t/><t/></track>");
            var particles = Directory.CreateDirectory(Path.Combine(_root, "particles")).FullName;
            File.WriteAllText(Path.Combine(particles, "Synthetic.xml"),
                "<Emitter><Name>Synthetic</Name><Image>IMAGE_UNUSED</Image>"
                + "<SpawnMinActive>1</SpawnMinActive><ParticleDuration>1</ParticleDuration>"
                + "<SystemDuration>10000</SystemDuration><ParticleAlpha>0</ParticleAlpha></Emitter>");
            _renderer = new ReanimationRenderer(_root);
        }

        /// <summary>Releases native resources and removes only this test's isolated source directory.</summary>
        [TearDown]
        public void TearDown()
        {
            _renderer.Dispose();
            Directory.Delete(_root, true);
        }

        /// <summary>Preserves default selection, aliases, and case-insensitive clip identities after caching.</summary>
        /// <param name="animation">The requested alias for the idle clip.</param>
        [TestCase(null)]
        [TestCase("idle")]
        [TestCase("ANIM_IDLE")]
        [TestCase(" anim_idle ")]
        public void CachedAliasesPreserveClipSelection(string? animation)
        {
            for (var iteration = 0; iteration < 3; iteration++)
            {
                Assert.That(_renderer.TryGetNativeAnimationInfo("Synthetic", animation!, out var idle), Is.True);
                Assert.That(idle.Start, Is.Zero);
                Assert.That(idle.Count, Is.EqualTo(2));
                Assert.That(idle.GroundDistance, Is.EqualTo(12f));
                Assert.That(_renderer.TryGetNativeAnimationInfo("Synthetic", "anim_alt", out var alternative), Is.True);
                Assert.That(alternative.Start, Is.EqualTo(2));
                Assert.That(alternative.Count, Is.EqualTo(2));
            }
        }

        /// <summary>Performs cached clip and anchor sampling without closures, strings, or enumerator allocations.</summary>
        [Test]
        public void WarmedClipAndTrackLookupsDoNotAllocate()
        {
            var valid = true;
            for (var iteration = 0; iteration < 16; iteration++)
            {
                valid &= SampleTrack();
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < 512; iteration++)
            {
                valid &= SampleTrack();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(valid, Is.True);
            Assert.That(allocated, Is.Zero, "Warmed animation and track lookups must reuse their cached identities.");
        }

        /// <summary>A native transform samples each track once without recalculating every pose's fitting bounds.</summary>
        [Test]
        public void NativePlacementFiltersOnlyTheSampledTracks()
        {
            Assert.That(_renderer.TryGetNativeAnimationInfo("Synthetic", "anim_idle", out _), Is.True);
            ObjectSpriteRenderer.BeginSceneFrame();
            _filterCalls = 0;
            Assert.That(_renderer.DrawNativePlayback("Synthetic", Vector2.zero, 1f, 0f, "anim_idle", true, visible: IncludeTrack), Is.True);
            Assert.That(_filterCalls, Is.EqualTo(4), "Native placement must not run the custom filter again to fit unused clip bounds.");
        }

        /// <summary>Reuses resolved image identities when resource sprites repeat in projectile and particle paths.</summary>
        [Test]
        public void WarmedResourceSpritesDoNotAllocate()
        {
            var valid = true;
            for (var iteration = 0; iteration < 16; iteration++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                valid &= _renderer.DrawResourceSprite("IMAGE_REANIM_sprite", Vector2.zero, 1f);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < 128; iteration++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                valid &= _renderer.DrawResourceSprite("IMAGE_REANIM_sprite", Vector2.zero, 1f);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(valid, Is.True);
            Assert.That(allocated, Is.Zero, "Repeated image identifiers must not create prefix substrings for each sprite.");
        }

        /// <summary>Reuses multihead clip and anchor identities for cursor and thumbnail animations.</summary>
        [Test]
        public void WarmedNativeMultiHeadDoesNotAllocate()
        {
            var valid = true;
            for (var iteration = 0; iteration < 16; iteration++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                valid &= _renderer.DrawNative("SplitPea", Vector2.zero, 1f, .1f, "anim_idle");
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < 128; iteration++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                valid &= _renderer.DrawNative("SplitPea", Vector2.zero, 1f, .1f, "anim_idle");
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(valid, Is.True);
            Assert.That(allocated, Is.Zero, "Multihead previews must not recreate their clip and anchor name arrays.");
        }

        /// <summary>Retains only currently visible body and head identities during an unlimited stream of actors.</summary>
        [Test]
        public void CompletedFramesRetireActorAndHeadBlendIdentities()
        {
            var heads = new[]
            {
                new HeadPlayback { Animation = "anim_head_idle", Anchor = "anim_idle", Rate = 12f, Loop = true }
            };
            for (var actor = 0; actor < 128; actor++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                _renderer.BeginObjectFrame();
                try
                {
                    Assert.That(_renderer.DrawNativePlayback("Synthetic", Vector2.zero, 1f, .1f, "anim_idle", true, instanceKey: "body-" + actor), Is.True);
                    Assert.That(_renderer.DrawNativeAttachedHeads("SplitPea", Vector2.zero, 1f, .1f, 12f, heads, instanceKey: "head-actor-" + actor), Is.True);
                }
                finally
                {
                    _renderer.EndObjectFrame();
                }

                Assert.That(_renderer.NativePoseBlendCount, Is.EqualTo(2), "Retired actor dictionaries must not accumulate during endless play.");
                Assert.That(_renderer.HeadInstanceKeyCount, Is.EqualTo(1));
            }

            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            _renderer.EndObjectFrame();
            Assert.That(_renderer.NativePoseBlendCount, Is.Zero);
            Assert.That(_renderer.HeadInstanceKeyCount, Is.Zero);
        }

        /// <summary>Defers retirement until the complete outer frame has consumed every nested board and actor scope.</summary>
        [Test]
        public void NestedFramesAndStandaloneScopesPreserveLiveBlends()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            try
            {
                Assert.That(_renderer.DrawNativePlayback("Synthetic", Vector2.zero, 1f, .1f, "anim_idle", true, instanceKey: "outer"), Is.True);
                _renderer.BeginObjectFrame();
                try
                {
                    Assert.That(_renderer.DrawNativePlayback("Synthetic", Vector2.zero, 1f, .1f, "anim_idle", true, instanceKey: "inner"), Is.True);
                }
                finally
                {
                    _renderer.EndObjectFrame();
                }

                Assert.That(_renderer.NativePoseBlendCount, Is.EqualTo(2));
            }
            finally
            {
                _renderer.EndObjectFrame();
            }

            Assert.That(_renderer.NativePoseBlendCount, Is.EqualTo(2));
            Assert.That(_renderer.DrawNativePlayback("Synthetic", Vector2.zero, 1f, .2f, "anim_alt", true, instanceKey: "standalone"), Is.True);
            Assert.That(_renderer.NativePoseBlendCount, Is.EqualTo(3), "Individual draw scopes must not retire unrelated playback identities.");
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            try
            {
                Assert.That(_renderer.DrawNativePlayback("Synthetic", Vector2.zero, 1f, .3f, "anim_alt", true, instanceKey: "outer"), Is.True);
            }
            finally
            {
                _renderer.EndObjectFrame();
            }

            Assert.That(_renderer.NativePoseBlendCount, Is.EqualTo(1));
            _renderer.ResetNativePlayback();
            Assert.That(_renderer.NativePoseBlendCount, Is.Zero);
        }

        /// <summary>Reuses particles and random buffers for continuous emitters after their initial spawn.</summary>
        [Test]
        public void WarmedContinuousParticlesDoNotAllocate()
        {
            var particles = new OriginalParticleRenderer(_root, _renderer);
            var valid = true;
            for (var iteration = 0; iteration < 16; iteration++)
            {
                particles.BeginFrame();
                valid &= particles.Draw("effect", "Synthetic", iteration * .01f, Vector2.zero, 123);
                particles.EndFrame();
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 16; iteration < 528; iteration++)
            {
                particles.BeginFrame();
                valid &= particles.Draw("effect", "Synthetic", iteration * .01f, Vector2.zero, 123);
                particles.EndFrame();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(valid, Is.True);
            Assert.That(particles.CreatedParticleCount, Is.EqualTo(1));
            Assert.That(allocated, Is.Zero, "Continuous emission must reuse expired particles and their random buffers.");
        }

        /// <summary>Returns live particles when effects leave presentation or the scene explicitly clears playback.</summary>
        [Test]
        public void RetiredAndClearedSystemsReturnParticles()
        {
            var particles = new OriginalParticleRenderer(_root, _renderer);
            particles.BeginFrame();
            Assert.That(particles.Draw("first", "Synthetic", 0f, Vector2.zero, 123), Is.True);
            particles.EndFrame();
            particles.BeginFrame();
            particles.EndFrame();
            particles.BeginFrame();
            Assert.That(particles.Draw("second", "Synthetic", 0f, Vector2.zero, 456), Is.True);
            particles.EndFrame();
            Assert.That(particles.CreatedParticleCount, Is.EqualTo(1));
            particles.Clear();
            particles.BeginFrame();
            Assert.That(particles.Draw("third", "Synthetic", 0f, Vector2.zero, 789), Is.True);
            particles.EndFrame();
            Assert.That(particles.CreatedParticleCount, Is.EqualTo(1), "Scene resets must return active particle buffers as well as expired ones.");
        }

        /// <summary>Combines clip metadata lookup and case-insensitive track interpolation for allocation measurements.</summary>
        /// <returns>Whether both lookups selected the expected clip and sampled anchor pose.</returns>
        private bool SampleTrack()
        {
            return _renderer.TryGetNativeAnimationInfo("Synthetic", "anim_idle", out var info)
                && info.Count == 2
                && _renderer.TryGetNativeTrackPosition("Synthetic", "anim_idle", "BODY", .5f, 1f, true, out var position)
                && position.x >= 0f;
        }

        /// <summary>Counts visible-track evaluations without changing the animation's selected tracks.</summary>
        /// <param name="track">The source track being tested for visibility.</param>
        /// <returns>True so every original track remains eligible for drawing.</returns>
        private bool IncludeTrack(string track)
        {
            _filterCalls++;
            return true;
        }
    }
}
