#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Unity.Profiling;
using System.Reflection;
using System.Text;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
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

        /// <summary>Preserves native group order, custom-filter calls, and immediate changes after a shield is lost.</summary>
        /// <param name="kind">The species whose held shield selects the source body and native groups.</param>
        [TestCase(ZombieKind.ScreenDoor)]
        [TestCase(ZombieKind.Newspaper)]
        [TestCase(ZombieKind.Ladder)]
        public void CachedShieldGroupsPreserveSourceOrderAndLiveShieldState(ZombieKind kind)
        {
            var tracks = new[]
            {
                "body-first", "Zombie_outerarm_screendoor_back", "zombie_paper_paper", "zombie_ladder_1",
                "body-middle", "anim_screendoor", "Zombie_paper_hands_back", "Zombie_outerarm_upper", "body-last"
            };
            var zombie = new Zombie { Kind = kind, ShieldHealth = 1100f, PresentationMaxShield = 1100f };
            var asset = GameSimulation.AdventureZombiePresentationAsset(zombie);
            WriteTrackFixture(asset, tracks);
            var calls = new List<string>();
            Func<string, bool> visible = track =>
            {
                calls.Add(track);
                return track.StartsWith("body-", StringComparison.Ordinal);
            };
            Func<bool> draw = () => _renderer.DrawNativePlayback(asset, new Vector2(30f, 30f), 1f, 0f, "anim_idle", true, visible: visible);
            var shield = AdventureZombieFeedbackRules.ShieldType(zombie);
            var session = new object();
            var previousNodes = new Dictionary<string, GameObject>();
            for (var pass = 0; pass < 3; pass++)
            {
                zombie.ShieldHealth = pass == 1 ? 0f : 1100f;
                ObjectSpriteRenderer.BeginSceneFrame();
                calls.Clear();
                _renderer.Hierarchy.BeginFrame(session);
                using var actor = _renderer.Hierarchy.BeginActor("Zombies", 51, asset);
                Assert.That(_renderer.DrawNativeZombieCombat(zombie, new Rect(0f, 0f, 800f, 600f), draw), Is.True);
                var expected = new List<string>();
                var currentShield = zombie.ShieldHealth > 0f ? shield : 0;
                var groupCount = currentShield == 0 ? 1 : 3;
                for (var group = 0; group < groupCount; group++)
                {
                    if (group == 0)
                    {
                        expected.Add("anim_idle");
                    }

                    for (var index = 0; index < tracks.Length; index++)
                    {
                        if (AdventureZombieFeedbackRules.RenderGroup(currentShield, tracks[index]) == group)
                        {
                            expected.Add(tracks[index]);
                        }
                    }
                }

                Assert.That(calls, Is.EqualTo(expected), "Custom callbacks must observe original track order within the current shield groups.");
                var pool = ObjectPool();
                var sprites = pool.SubmittedSprites;
                for (var index = 0; index < pool.SubmittedObjectCount; index++)
                {
                    var node = sprites[index].GameObject!;
                    if (previousNodes.TryGetValue(node.name, out var previous))
                    {
                        Assert.That(node, Is.SameAs(previous), "Shield grouping must keep the original track index as the hierarchy identity.");
                    }
                    else
                    {
                        previousNodes.Add(node.name, node);
                    }
                }
            }
        }

        /// <summary>Keeps cached default variants live across equipment changes and preserves custom callback precedence.</summary>
        [Test]
        public void CachedVariantVisibilityPreservesEquipmentChangesAndCustomOverrides()
        {
            var tracks = new[]
            {
                "body", "anim_cone", "anim_bucket", "anim_hair", "Zombie_flaghand", "Zombie_innerarm_screendoor",
                "anim_screendoor", "Zombie_outerarm_screendoor", "Zombie_innerarm_screendoor_hand", "anim_innerarm1",
                "Zombie_outerarm_hand", "Zombie_outerarm_lower", "Zombie_outerarm_upper", "Zombie_duckytube",
                "Zombie_whitewater", "Zombie_whitewater2", "Zombie_mustache", "anim_tongue"
            };
            WriteTrackFixture("Zombie", tracks);
            WriteTrackFixture("Zombie_flagpole", new[] { "flag" });
            var session = new object();
            for (var pass = 0; pass < 2; pass++)
            {
                for (var variant = ZombieVariant.Normal; variant <= ZombieVariant.ScreenDoor; variant++)
                {
                    for (var intact = 0; intact < 2; intact++)
                    {
                        ObjectSpriteRenderer.BeginSceneFrame();
                        _renderer.Hierarchy.BeginFrame(session);
                        using var actor = _renderer.Hierarchy.BeginActor("Zombies", 1, "Zombie");
                        Assert.That(_renderer.DrawZombiePlayback(Vector2.one * 30f, 1f, 0f, variant, "anim_idle", true, 12f, intact != 0), Is.True);
                        var expected = new List<string>();
                        for (var index = 0; index < tracks.Length; index++)
                        {
                            if (ReanimationRenderer.IsZombieTrackVisible(tracks[index], variant, intact != 0))
                            {
                                expected.Add(tracks[index]);
                            }
                        }

                        var actual = new List<string>();
                        var pool = ObjectPool();
                        var sprites = pool.SubmittedSprites;
                        for (var index = 0; index < pool.SubmittedObjectCount; index++)
                        {
                            var node = sprites[index].GameObject!;
                            if (node.transform.parent.name != "Flag")
                            {
                                actual.Add(node.name);
                            }
                        }

                        Assert.That(actual, Is.EqualTo(expected), variant + " equipment=" + intact);
                    }
                }
            }

            ObjectSpriteRenderer.BeginSceneFrame();
            var customCalls = new List<string>();
            Func<string, bool> custom = track =>
            {
                customCalls.Add(track);
                return true;
            };
            Assert.That(_renderer.DrawZombiePlayback(Vector2.one * 30f, 1f, 0f, ZombieVariant.Normal, "anim_idle", true, 12f, visible: custom), Is.True);
            Assert.That(customCalls.Count, Is.EqualTo(tracks.Length + 1), "Custom visibility must still override every cached default, including hidden water tracks.");
        }

        /// <summary>Retains the original left-associated parent transform and stable hierarchy track identities.</summary>
        [Test]
        public void CachedParentTransformPreservesAffinePoseAndHierarchyIdentity()
        {
            var inherited = Matrix4x4.identity;
            inherited.m00 = 1.17f;
            inherited.m01 = -.37f;
            inherited.m10 = .29f;
            inherited.m11 = .81f;
            inherited.m03 = 110.25f;
            inherited.m13 = 45.375f;
            var placement = Matrix4x4.identity;
            placement.m00 = 1.31f;
            placement.m11 = 1.31f;
            placement.m03 = 18.2f;
            placement.m13 = 23.7f;
            var part = Matrix4x4.identity;
            part.m03 = 6f;
            var expected = inherited * placement * part;
            GameObject? previousNode = null;
            var session = new object();
            for (var pass = 0; pass < 2; pass++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                SpriteDrawState.Matrix = inherited;
                _renderer.Hierarchy.BeginFrame(session);
                using var actor = _renderer.Hierarchy.BeginActor("Zombies", 2, "Synthetic");
                Assert.That(_renderer.DrawNativePlayback("Synthetic", new Vector2(18.2f, 23.7f), 1.31f, .125f, "anim_idle", false, 12f), Is.True);
                var sprite = ObjectPool().SubmittedSprites[0];
                var node = sprite.GameObject!;
                Assert.That(node.GetComponent<SpriteHierarchySprite>().DestinationTransform, Is.EqualTo(expected));
                Assert.That(SpriteDrawState.Matrix, Is.EqualTo(inherited), "The caller's inherited transform must be restored.");
                if (previousNode != null)
                {
                    Assert.That(node, Is.SameAs(previousNode), "Cached group positions must never replace the original hierarchy track index.");
                }

                previousNode = node;
            }
        }

        /// <summary>Checks detached arms and other parts preserve their particle identities without steady managed allocations.</summary>
        /// <param name="explicitFrames">Whether the caller brackets drawing in an explicit outer object frame.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void WarmedDetachedPartsDoNotAllocate(bool explicitFrames)
        {
            var definitions = Path.Combine(_root, "particles");
            File.Copy(Path.Combine(definitions, "Synthetic.xml"), Path.Combine(definitions, "ZombieArm.xml"));
            File.Copy(Path.Combine(definitions, "Synthetic.xml"), Path.Combine(definitions, "ZombieHead.xml"));
            var particles = new OriginalParticleRenderer(_root, _renderer);
            var arm = new AdventureZombieArmDrop
            {
                Id = 17, Asset = "Synthetic", Animation = "anim_idle", Anchor = "body", Scale = 1f, Rate = 12f,
                AnimationAge = .1f, Age = .2f, Seed = 123
            };
            var part = new AdventureZombiePartDrop
            {
                Id = 17, ParticleId = 6, Asset = "Synthetic", Animation = "anim_idle", Anchor = "body", Scale = 1f,
                Rate = 12f, AnimationAge = .1f, Age = .2f, Seed = 321
            };
            var valid = true;
            for (var frame = 0; frame < 64; frame++)
            {
                valid &= DrawDetachedParts(particles, arm, part, explicitFrames);
            }

            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 4096,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            var sentinel = new byte[1024];
            recorder.Stop();
            GC.KeepAlive(sentinel);
            Assert.That(recorder.Valid, Is.True);
            Assert.That(recorder.Count, Is.GreaterThan(0), "The allocation recorder must detect its deliberate positive control.");
            recorder.Reset();
            recorder.Start();
            for (var frame = 0; frame < 128; frame++)
            {
                valid &= DrawDetachedParts(particles, arm, part, explicitFrames);
            }
            recorder.Stop();

            Assert.That(valid, Is.True);
            Assert.That(recorder.Count, Is.Zero, "Warmed native arm and detached-part identities must emit no GC.Alloc events.");
            var systems = (IDictionary)typeof(OriginalParticleRenderer).GetField("_systems", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(particles)!;
            Assert.That(systems.Contains("native-arm-17"), Is.True);
            Assert.That(systems.Contains("native-zombie-part-17"), Is.True);
            Assert.That(systems.Count, Is.EqualTo(2), "Different native effect roles with the same entity ID must remain independent.");
            particles.Clear();
        }

        /// <summary>Traverses both native detached-part paths using the same stable presentation inputs.</summary>
        /// <param name="particles">The live reusable particle systems.</param>
        /// <param name="arm">The original detached arm state.</param>
        /// <param name="part">The original detached head state.</param>
        /// <param name="explicitFrames">Whether to exercise explicit or standalone renderer scopes.</param>
        /// <returns>Whether both original particle definitions remain successfully drawn.</returns>
        private bool DrawDetachedParts(OriginalParticleRenderer particles, AdventureZombieArmDrop arm,
            AdventureZombiePartDrop part, bool explicitFrames)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            if (explicitFrames)
            {
                _renderer.BeginObjectFrame();
            }
            try
            {
                particles.BeginFrame();
                var valid = _renderer.DrawNativeZombieArmDrop(particles, arm, Vector2.zero);
                valid &= _renderer.DrawNativeZombiePartDrop(particles, part, Vector2.zero);
                particles.EndFrame();
                return valid;
            }
            finally
            {
                if (explicitFrames)
                {
                    _renderer.EndObjectFrame();
                }
            }
        }

        /// <summary>Creates an original-format fixture with drawable named tracks in a deliberately mixed source order.</summary>
        /// <param name="asset">The original animation resource name.</param>
        /// <param name="tracks">The drawable track names whose source order must be preserved.</param>
        private void WriteTrackFixture(string asset, IReadOnlyList<string> tracks)
        {
            var text = new StringBuilder("<fps>12</fps><track><name>anim_idle</name><t><f>0</f></t><t/></track>");
            for (var index = 0; index < tracks.Count; index++)
            {
                text.Append("<track><name>").Append(tracks[index]).Append("</name><t><i>IMAGE_REANIM_sprite</i><x>")
                    .Append(index * 3).Append("</x></t><t/></track>");
            }

            File.WriteAllText(Path.Combine(_root, "reanim", asset + ".reanim"), text.ToString());
        }

        /// <summary>Finds the synthetic renderer's retained scene submissions for behavior assertions.</summary>
        /// <returns>The scene mesh pool owned by the animation renderer.</returns>
        private ObjectSpriteRenderer ObjectPool()
        {
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_renderer)!;
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
