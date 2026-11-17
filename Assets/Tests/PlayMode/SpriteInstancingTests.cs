#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Rendering.Pipeline;
using PlantsVsZombies.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks ordered texture-bank instancing against the independent mesh path on a real graphics device.</summary>
    public sealed class SpriteInstancingTests
    {
        /// <summary>The backend restored after each isolated case.</summary>
        private SpriteRenderingBackend _previousBackend;
        /// <summary>The synthetic independent source textures.</summary>
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        /// <summary>The renderer submitting both oracle and instance draws.</summary>
        private ObjectSpriteRenderer _renderer = null!;
        /// <summary>The scene camera whose ordering includes other transparent renderers.</summary>
        private Camera _camera = null!;
        /// <summary>The original scene destination.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>The original scene clear color.</summary>
        private Color _previousBackground;
        /// <summary>The isolated output texture.</summary>
        private RenderTexture _target = null!;
        /// <summary>The readable pixel snapshot.</summary>
        private Texture2D _pixels = null!;
        /// <summary>The native warnings caused by missing submitted structured-buffer bindings during this case.</summary>
        private int _missingBufferWarnings;

        /// <summary>Creates different source pages and an actual scene-camera destination.</summary>
        /// <returns>A frame initializing the production pipeline.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _missingBufferWarnings = 0;
            Application.logMessageReceived += CountMissingBufferWarning;
            _previousBackend = ObjectSpriteRenderer.Backend;
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            _renderer = new ObjectSpriteRenderer();
            _camera = ObjectSpriteRenderer.SceneCamera;
            _previousTarget = _camera.targetTexture;
            _previousBackground = _camera.backgroundColor;
            _target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _target.Create();
            _camera.targetTexture = _target;
            _camera.backgroundColor = Color.clear;
            _pixels = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            for (var index = 0; index < 9; index++)
            {
                var texture = new Texture2D(3 + index, 2 + index, TextureFormat.RGBA32, false, true)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var color = new Color((index % 3) * .3f + .1f, (index % 4) * .2f + .1f, (index % 2) * .5f + .1f, .5f);
                var colors = new Color[texture.width * texture.height];
                for (var pixel = 0; pixel < colors.Length; pixel++)
                {
                    colors[pixel] = color;
                }
                texture.SetPixels(colors);
                texture.Apply(false, true);
                _textures.Add(texture);
            }
            ObjectSpriteRenderer.PrepareSceneCapacityStep(1);
            if (!ObjectSpriteRenderer.InstancingAvailable)
            {
                Assert.Ignore("The current graphics device does not support the instanced structured-buffer shader.");
            }
        }

        /// <summary>Restores the shared camera and releases all test resources.</summary>
        /// <returns>A frame completing deferred resource destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ObjectSpriteRenderer.EndSceneFrame();
            ObjectSpriteRenderer.Backend = _previousBackend;
            _camera.targetTexture = _previousTarget;
            _camera.backgroundColor = _previousBackground;
            _renderer.Dispose();
            foreach (var texture in _textures)
            {
                UnityEngine.Object.Destroy(texture);
            }
            _textures.Clear();
            _target.Release();
            UnityEngine.Object.Destroy(_target);
            UnityEngine.Object.Destroy(_pixels);
            yield return null;
            Application.logMessageReceived -= CountMissingBufferWarning;
            Assert.That(_missingBufferWarnings, Is.Zero, "Submitted native blocks must retain both required buffers until their Unity frame expires.");
        }

        /// <summary>Checks fixed native layouts used by both structured buffers and the mesh fallback.</summary>
        [Test]
        public void UploadedRecordsHavePortableExactLayouts()
        {
            Assert.That(Marshal.SizeOf<SpriteInstanceData>(), Is.EqualTo(64));
            Assert.That(Marshal.SizeOf<SpriteBatchVertex>(), Is.EqualTo(68));
        }

        /// <summary>Checks single-unit loading preparation completes native bindings before their first new painter spans are submitted.</summary>
        /// <returns>Frames expiring the earlier native binding snapshots.</returns>
        [UnityTest]
        public IEnumerator PreparedPainterSpansReuseBindingsAndMaterialsOnTheirFirstSubmission()
        {
            var ready = false;
            var draws = ObjectSpriteRenderer.InstancedDrawCount;
            var uploads = ObjectSpriteRenderer.InstanceTotalUploadBytes;
            for (var step = 0; step < 1024 && !ready; step++)
            {
                var before = ObjectSpriteRenderer.InstanceRunCreationCount
                    + ObjectSpriteRenderer.InstanceSubmissionBindingCreationCount
                    + ObjectSpriteRenderer.InstanceSubmissionMaterialCreationCount
                    + ObjectSpriteRenderer.InstanceBufferCreationCount / 2;
                ready = ObjectSpriteRenderer.PrepareSceneCapacityStep(9, 1);
                var after = ObjectSpriteRenderer.InstanceRunCreationCount
                    + ObjectSpriteRenderer.InstanceSubmissionBindingCreationCount
                    + ObjectSpriteRenderer.InstanceSubmissionMaterialCreationCount
                    + ObjectSpriteRenderer.InstanceBufferCreationCount / 2;
                Assert.That(after - before, Is.LessThanOrEqualTo(1), "One step prepares only one independently bounded resource unit.");
            }
            Assert.That(ready, Is.True);
            Assert.That(ObjectSpriteRenderer.PreparedInstanceSubmissionBindingCapacity, Is.GreaterThanOrEqualTo(128));
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.EqualTo(draws));
            Assert.That(ObjectSpriteRenderer.InstanceTotalUploadBytes, Is.EqualTo(uploads));
            var bindings = ObjectSpriteRenderer.InstanceSubmissionBindingCreationCount;
            var materials = ObjectSpriteRenderer.InstanceSubmissionMaterialCreationCount;
            var runMisses = ObjectSpriteRenderer.InstanceRunCapacityMissCount;
            var bindingMisses = ObjectSpriteRenderer.InstanceSubmissionBindingCapacityMissCount;
            DrawFrame(9, false, false);
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.EqualTo(2));
            yield return null;
            DrawFrame(9, false, false);
            Assert.That(ObjectSpriteRenderer.InstanceSubmissionBindingCreationCount, Is.EqualTo(bindings));
            Assert.That(ObjectSpriteRenderer.InstanceSubmissionMaterialCreationCount, Is.EqualTo(materials));
            Assert.That(ObjectSpriteRenderer.InstanceRunCapacityMissCount, Is.EqualTo(runMisses));
            Assert.That(ObjectSpriteRenderer.InstanceSubmissionBindingCapacityMissCount, Is.EqualTo(bindingMisses));
        }

        /// <summary>Checks compatibility preparation creates hidden meshes and fixed buffers before their first submitted runs.</summary>
        /// <returns>A frame retiring the explicitly prepared mesh scene.</returns>
        [UnityTest]
        public IEnumerator PreparedMeshRunsReuseTheirFirstNativeBuffers()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            var ready = false;
            for (var step = 0; step < 32 && !ready; step++)
            {
                ready = _renderer.PrepareMeshCapacityStep(9, 512);
            }
            Assert.That(ready, Is.True);
            var meshes = ObjectSpriteRenderer.MeshCreationCount;
            var buffers = ObjectSpriteRenderer.MeshBufferReconfigurationCount;
            DrawFrame(9, false, false);
            Assert.That(ObjectSpriteRenderer.MeshCreationCount, Is.EqualTo(meshes));
            Assert.That(ObjectSpriteRenderer.MeshBufferReconfigurationCount, Is.EqualTo(buffers));
            yield return null;
        }

        /// <summary>Checks each active native snapshot supplies the same required chunk buffers to both material validation and the draw property block.</summary>
        [Test]
        public void MaterialValidationBuffersMatchTheSubmittedPropertyBlock()
        {
            DrawFrame(9, false, false);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var renderer = typeof(ObjectSpriteRenderer).GetField("s_instancing", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var runs = (IList)renderer.GetType().GetField("_runs", flags)!.GetValue(renderer)!;
            var chunks = (IList)renderer.GetType().GetField("_chunks", flags)!.GetValue(renderer)!;
            var verified = 0;
            for (var index = 0; index < ObjectSpriteRenderer.InstancedDrawCount; index++)
            {
                var run = runs[index]!;
                var chunkIndex = (int)run.GetType().GetField("ChunkIndex", flags)!.GetValue(run)!;
                var chunk = chunks[chunkIndex]!;
                var vertices = (GraphicsBuffer)chunk.GetType().GetField("VertexBuffer", flags)!.GetValue(chunk)!;
                var instances = (GraphicsBuffer)chunk.GetType().GetField("InstanceBuffer", flags)!.GetValue(chunk)!;
                var bindings = (IList)run.GetType().GetField("_bindings", flags)!.GetValue(run)!;
                foreach (var binding in bindings)
                {
                    if ((int)binding!.GetType().GetField("SubmittedFrame", flags)!.GetValue(binding)! != Time.frameCount)
                    {
                        continue;
                    }
                    var material = (Material)binding.GetType().GetField("_material", flags)!.GetValue(binding)!;
                    var properties = (MaterialPropertyBlock)binding.GetType().GetField("Properties", flags)!.GetValue(binding)!;
                    Assert.That(vertices.IsValid(), Is.True);
                    Assert.That(instances.IsValid(), Is.True);
                    Assert.That(material.GetBuffer("_PvZVertices"), Is.EqualTo(vertices.bufferHandle));
                    Assert.That(material.GetBuffer("_PvZInstances"), Is.EqualTo(instances.bufferHandle));
                    Assert.That(properties.HasBuffer("_PvZVertices"), Is.True);
                    Assert.That(properties.HasBuffer("_PvZInstances"), Is.True);
                    verified++;
                }
            }
            Assert.That(verified, Is.EqualTo(2), "Both the eight-texture bank and its overflow span must have complete native bindings.");
        }

        /// <summary>Checks queue-first scene sorting preserves Canvas sibling order and leaves all other layers and captures unchanged.</summary>
        [Test]
        public void QueueSortingOnlyAppliesToTheReservedSceneLayer()
        {
            var bridged = PvZRenderPipeline.GetTransparentSortingCriteria(31, true);
            Assert.That(bridged & SortingCriteria.SortingLayer, Is.EqualTo((SortingCriteria)0));
            Assert.That(bridged & SortingCriteria.CanvasOrder, Is.Not.EqualTo((SortingCriteria)0));
            Assert.That(PvZRenderPipeline.GetTransparentSortingCriteria(30, true), Is.EqualTo(SortingCriteria.CommonTransparent));
            Assert.That(PvZRenderPipeline.GetTransparentSortingCriteria(31, false), Is.EqualTo(SortingCriteria.CommonTransparent));
            DrawFrame(3, false, false);
            Assert.That(ObjectSpriteRenderer.UsesSceneQueuesForCamera(_camera), Is.True);
            var capture = new GameObject("Independent mesh capture", typeof(Camera));
            try
            {
                Assert.That(ObjectSpriteRenderer.UsesSceneQueuesForCamera(capture.GetComponent<Camera>()), Is.False);
            }
            finally
            {
                UnityEngine.Object.Destroy(capture);
            }
        }

        /// <summary>Checks interleaved A/B/A pages become one draw without changing overlapping alpha pixels.</summary>
        [Test]
        public void AlternatingPagesShareOneOrderedDrawAndMatchMeshPixels()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            DrawFrame(3, true, false);
            Render();
            var expected = _pixels.GetPixels32();
            Assert.That(_renderer.BatchCount, Is.EqualTo(3));
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            DrawFrame(3, true, false);
            Render();
            Assert.That(_renderer.BatchCount, Is.EqualTo(1));
            Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
        }

        /// <summary>Checks the ninth page and explicit painter gaps split GPU runs while preserving the source sequence.</summary>
        /// <returns>A new Unity frame between different staged populations so the second case can use its actual instance backend.</returns>
        [UnityTest]
        public IEnumerator TextureBankOverflowAndPainterBarriersCreateSeparateRuns()
        {
            DrawFrame(9, false, false);
            Assert.That(_renderer.BatchCount, Is.EqualTo(2));
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.EqualTo(2));
            yield return null;
            DrawFrame(3, true, true);
            Assert.That(_renderer.BatchCount, Is.EqualTo(3));
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.EqualTo(3));
        }

        /// <summary>Checks identical complete frames retain GPU data and avoid repeated structured-buffer uploads.</summary>
        [Test]
        public void IdenticalSceneContentDoesNotUploadAgain()
        {
            DrawFrame(8, false, false);
            DrawFrame(8, false, false);
            Assert.That(ObjectSpriteRenderer.InstanceUploadCount, Is.Zero);
        }

        /// <summary>Checks cumulative diagnostics record real uploads and split causes without being reset by identical presentations.</summary>
        [Test]
        public void CumulativeDiagnosticsRetainActualUploadsAndSplitReasons()
        {
            var bankSplits = ObjectSpriteRenderer.InstanceTextureBankSplitCount;
            var uploadBytes = ObjectSpriteRenderer.InstanceTotalUploadBytes;
            var buffers = ObjectSpriteRenderer.InstanceBufferCreationCount;
            DrawFrame(9, false, false);
            Assert.That(ObjectSpriteRenderer.InstanceTextureBankSplitCount, Is.EqualTo(bankSplits + 1));
            Assert.That(ObjectSpriteRenderer.InstanceTotalUploadBytes, Is.GreaterThan(uploadBytes));
            Assert.That(ObjectSpriteRenderer.InstanceUploadBytes, Is.GreaterThan(0));
            uploadBytes = ObjectSpriteRenderer.InstanceTotalUploadBytes;
            DrawFrame(9, false, false);
            Assert.That(ObjectSpriteRenderer.InstanceUploadBytes, Is.Zero);
            Assert.That(ObjectSpriteRenderer.InstanceTotalUploadBytes, Is.EqualTo(uploadBytes));
            Assert.That(ObjectSpriteRenderer.InstanceBufferCreationCount, Is.EqualTo(buffers));
            var painterSplits = ObjectSpriteRenderer.InstancePainterOrderSplitCount;
            DrawFrame(3, true, true);
            Assert.That(ObjectSpriteRenderer.InstancePainterOrderSplitCount, Is.EqualTo(painterSplits + 2));
            var blendSplits = ObjectSpriteRenderer.InstanceBlendSplitCount;
            using (ObjectSpriteRenderer.BeginSceneFrameScope())
            {
                using (_renderer.BeginFrame())
                {
                    for (var index = 0; index < 3; index++)
                    {
                        _renderer.DrawQuad(_textures[0], 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, index % 2);
                    }
                }
            }
            Assert.That(ObjectSpriteRenderer.InstanceBlendSplitCount, Is.EqualTo(blendSplits + 2));
        }

        /// <summary>Checks a shorter identical same-frame prefix keeps submitted buffers immutable while invalidating the previous draw generation.</summary>
        [Test]
        public void MultiplePresentsInOneUnityFrameRenderOnlyLatestGeneration()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            DrawFrame(2, true, false);
            Render();
            var expected = _pixels.GetPixels32();
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            DrawFrame(8, true, false);
            var fallbacks = ObjectSpriteRenderer.InstanceSameFrameBufferFallbackCount;
            var uploaded = ObjectSpriteRenderer.InstanceTotalUploadBytes;
            DrawFrame(2, true, false);
            Render();
            Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
            Assert.That(ObjectSpriteRenderer.InstanceSameFrameBufferFallbackCount, Is.EqualTo(fallbacks));
            Assert.That(ObjectSpriteRenderer.InstanceTotalUploadBytes, Is.EqualTo(uploaded));
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.EqualTo(1));
        }

        /// <summary>Checks texture-bank offsets and overlapping alpha remain exact when a changed same-frame scene uses the complete mesh fallback.</summary>
        /// <returns>A new Unity frame that can safely upload the next complete instance generation.</returns>
        [UnityTest]
        public IEnumerator ChangedSameFrameTextureBanksDoNotOverwriteSubmittedGpuData()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            DrawFrame(9, false, false);
            Render();
            var expected = _pixels.GetPixels32();
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            DrawFrame(3, true, false);
            var uploaded = ObjectSpriteRenderer.InstanceTotalUploadBytes;
            var fallbacks = ObjectSpriteRenderer.InstanceSameFrameBufferFallbackCount;
            DrawFrame(9, false, false);
            Render();
            Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
            Assert.That(ObjectSpriteRenderer.InstanceTotalUploadBytes, Is.EqualTo(uploaded));
            Assert.That(ObjectSpriteRenderer.InstanceSameFrameBufferFallbackCount, Is.EqualTo(fallbacks + 1));
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.Zero);
            yield return null;
            DrawFrame(9, false, false);
            Render();
            Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
            Assert.That(ObjectSpriteRenderer.InstanceTotalUploadBytes, Is.GreaterThan(uploaded));
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.EqualTo(2));
        }

        /// <summary>Checks identical same-frame generations use distinct immutable native snapshots, then reuse those snapshots without capacity growth.</summary>
        /// <returns>Frames expiring prior native submissions before another identical pair is presented.</returns>
        [UnityTest]
        public IEnumerator IdenticalSameFrameGenerationsReuseOnlyCompletedNativeBindings()
        {
            var enabled = _camera.enabled;
            _camera.enabled = false;
            try
            {
                DrawFrame(9, false, false);
                var firstBindings = CurrentGenerationSubmissionBindings();
                DrawFrame(9, false, false);
                var secondBindings = CurrentGenerationSubmissionBindings();
                Render();
                Assert.That(firstBindings.Length, Is.EqualTo(2));
                Assert.That(secondBindings.Length, Is.EqualTo(2));
                foreach (var current in secondBindings)
                {
                    foreach (var previous in firstBindings)
                    {
                        Assert.That(current, Is.Not.SameAs(previous), "A submission must not reuse any native property block still owned by an earlier generation in this Unity frame.");
                    }
                }
                Assert.That(secondBindings[0], Is.Not.SameAs(secondBindings[1]));
                Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.EqualTo(2));
                var expected = _pixels.GetPixels32();
                var bindings = ObjectSpriteRenderer.InstanceSubmissionBindingCreationCount;
                yield return null;
                DrawFrame(9, false, false);
                DrawFrame(9, false, false);
                Render();
                Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
                Assert.That(ObjectSpriteRenderer.InstanceSubmissionBindingCreationCount, Is.EqualTo(bindings));
            }
            finally
            {
                _camera.enabled = enabled;
            }
        }

        /// <summary>Reads the exact native blocks submitted by the active scene generation outside every measured allocation scope.</summary>
        /// <returns>The property blocks whose captured generation and submission frame match the active scene.</returns>
        private static MaterialPropertyBlock[] CurrentGenerationSubmissionBindings()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var renderer = typeof(ObjectSpriteRenderer).GetField("s_instancing", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var generation = (int)renderer.GetType().GetField("_generation", flags)!.GetValue(renderer)!;
            var runs = (IList)renderer.GetType().GetField("_runs", flags)!.GetValue(renderer)!;
            var result = new List<MaterialPropertyBlock>();
            for (var index = 0; index < ObjectSpriteRenderer.InstancedDrawCount; index++)
            {
                var run = runs[index]!;
                var bindings = (IList)run.GetType().GetField("_bindings", flags)!.GetValue(run)!;
                foreach (var binding in bindings)
                {
                    var properties = (MaterialPropertyBlock)binding!.GetType().GetField("Properties", flags)!.GetValue(binding)!;
                    if ((int)binding.GetType().GetField("SubmittedFrame", flags)!.GetValue(binding)! == Time.frameCount
                        && properties.GetInt("_PvZRunGeneration") == generation)
                    {
                        result.Add(properties);
                    }
                }
            }
            return result.ToArray();
        }

        /// <summary>Checks disposing the last instance resource owner preserves pending native buffers through every same-frame render request.</summary>
        /// <returns>A new Unity frame whose completed context releases the retired owner's resources.</returns>
        [UnityTest]
        public IEnumerator LastResourceOwnerDisposalKeepsNativeBuffersUntilItsDrawFrameExpires()
        {
            var shader = Resources.Load<Shader>("Shaders/PvZSpriteInstanced");
            var instances = new SpriteInstancingRenderer(shader);
            var vertices = new[]
            {
                new SpriteClipVertex(Vector2.zero, Vector2.up),
                new SpriteClipVertex(Vector2.right * 32f, Vector2.one),
                new SpriteClipVertex(Vector2.one * 32f, Vector2.right),
                new SpriteClipVertex(Vector2.up * 32f, Vector2.zero)
            };
            var cameraObject = new GameObject("Retired native draw inspection", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.CopyFrom(_camera);
            camera.enabled = false;
            var pending = ObjectSpriteRenderer.PendingRetiredInstanceSessionCount;
            try
            {
                instances.PrepareCapacityStep(1);
                instances.BeginFrame(123456);
                Assert.That(instances.TryAppend(_renderer, _textures[0], 0, 0, vertices, 0, 4, Color.white, Vector4.zero, Vector4.zero), Is.True);
                instances.Publish();
                instances.Dispose();
                Assert.That(ObjectSpriteRenderer.PendingRetiredInstanceSessionCount, Is.EqualTo(pending + 1));
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = _target });
                Assert.That(ObjectSpriteRenderer.PendingRetiredInstanceSessionCount, Is.EqualTo(pending + 1));
                instances.Dispose();
                Assert.That(ObjectSpriteRenderer.PendingRetiredInstanceSessionCount, Is.EqualTo(pending + 1));
                yield return null;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = _target });
                Assert.That(ObjectSpriteRenderer.PendingRetiredInstanceSessionCount, Is.Zero);
                Assert.That(_missingBufferWarnings, Is.Zero);
            }
            finally
            {
                instances.Dispose();
                UnityEngine.Object.Destroy(cameraObject);
            }
        }

        /// <summary>Checks a retained presentation remains visible across camera frames without another Present call.</summary>
        /// <returns>Two camera frames after the single complete scene submission.</returns>
        [UnityTest]
        public IEnumerator RetainedSceneInstancesRemainVisibleAcrossUnityFrames()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            DrawFrame(3, true, false);
            Render();
            var expected = _pixels.GetPixels32();
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            DrawFrame(3, true, false);
            yield return null;
            yield return null;
            ReadPixels();
            Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
        }

        /// <summary>Checks disposing one source retires its complete retained generation while another source keeps rendering resources alive.</summary>
        /// <returns>Camera frames exercising retirement, later presentation, and an explicitly empty scene.</returns>
        [UnityTest]
        public IEnumerator DisposedSourceRetiresRetainedDrawsBeforeItsTexturesAreDestroyed()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            DrawFrame(3, true, false);
            Render();
            var expected = _pixels.GetPixels32();
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            var retiredOwner = new ObjectSpriteRenderer();
            var retiredTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            retiredTexture.SetPixel(0, 0, Color.magenta);
            retiredTexture.Apply();
            try
            {
                var retirements = ObjectSpriteRenderer.InstanceOwnerRetirementCount;
                var retiredRuns = ObjectSpriteRenderer.InstanceRetiredRunCount;
                using (ObjectSpriteRenderer.BeginSceneFrameScope())
                {
                    using (retiredOwner.BeginFrame())
                    {
                        retiredOwner.DrawQuad(retiredTexture, 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
                    }
                    using (_renderer.BeginFrame())
                    {
                        _renderer.DrawQuad(_textures[0], 32f, 32f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
                    }
                }
                retiredOwner.Dispose();
                UnityEngine.Object.Destroy(retiredTexture);
                Render();
                Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.Zero);
                Assert.That(ObjectSpriteRenderer.InstanceOwnerRetirementCount, Is.EqualTo(retirements + 1));
                Assert.That(ObjectSpriteRenderer.InstanceRetiredRunCount, Is.EqualTo(retiredRuns + 2));
                Assert.That(_pixels.GetPixels32(), Is.All.EqualTo(new Color32(0, 0, 0, 0)));
                yield return null;
                yield return null;
                Render();
                Assert.That(ObjectSpriteRenderer.InstanceOwnerRetirementCount, Is.EqualTo(retirements + 1));
                DrawFrame(3, true, false);
                Render();
                Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
                using (ObjectSpriteRenderer.BeginSceneFrameScope())
                {
                }
                Render();
                Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.Zero);
                Assert.That(_pixels.GetPixels32(), Is.All.EqualTo(new Color32(0, 0, 0, 0)));
            }
            finally
            {
                retiredOwner.Dispose();
                if (retiredTexture != null)
                {
                    UnityEngine.Object.Destroy(retiredTexture);
                }
            }
        }

        /// <summary>Checks an unexpectedly destroyed live texture retires the complete scene once instead of rebinding substitute pixels.</summary>
        /// <returns>A frame expiring retired GPU submissions before the resource-owning scene is presented again.</returns>
        [UnityTest]
        public IEnumerator DestroyedLiveTextureRetiresTheCompleteGenerationAndReportsItsReason()
        {
            DrawFrame(3, false, false);
            var retirements = ObjectSpriteRenderer.InstanceMissingTextureRetirementCount;
            var retiredRuns = ObjectSpriteRenderer.InstanceRetiredRunCount;
            var texture = _textures[0];
            _textures.RemoveAt(0);
            UnityEngine.Object.DestroyImmediate(texture);
            LogAssert.Expect(LogType.Warning, "SpriteInstancingRenderer: Retired the retained scene because a sampled texture was destroyed before presentation.");
            Render();
            Assert.That(ObjectSpriteRenderer.InstanceMissingTextureRetirementCount, Is.EqualTo(retirements + 1));
            Assert.That(ObjectSpriteRenderer.InstanceRetiredRunCount, Is.EqualTo(retiredRuns + 1));
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.Zero);
            Assert.That(_pixels.GetPixels32(), Is.All.EqualTo(new Color32(0, 0, 0, 0)));
            Render();
            Assert.That(ObjectSpriteRenderer.InstanceMissingTextureRetirementCount, Is.EqualTo(retirements + 1));
            yield return null;
            DrawFrame(3, false, false);
            Render();
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.GreaterThan(0));
            Assert.That(_pixels.GetPixels32(), Is.Not.All.EqualTo(new Color32(0, 0, 0, 0)));
        }

        /// <summary>Checks a legacy scene remains open across a synchronous local capture and publishes the later native interface spans.</summary>
        [Test]
        public void LegacySceneCaptureKeepsLaterCanvasAndForegroundInTheCompleteQueueMap()
        {
            CanvasUI.Dispose();
            using var capture = new ObjectSpriteRenderer();
            var portrait = new RenderTexture(8, 8, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            portrait.Create();
            try
            {
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
                DrawLegacyCapturedBankFrame(capture, portrait);
                Render();
                var expected = _pixels.GetPixels32();
                Assert.That(_pixels.GetPixel(32, 32).b, Is.EqualTo(1f));
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
                DrawLegacyCapturedBankFrame(capture, portrait);
                Render();
                Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
                Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.GreaterThan(0));
            }
            finally
            {
                CanvasUI.Dispose();
                portrait.Release();
                UnityEngine.Object.Destroy(portrait);
            }
        }

        /// <summary>Checks a legacy empty scene retires only portraits while the unchanged native Canvas keeps its exact original pixels.</summary>
        /// <returns>Camera frames allowing the complete retained Canvas and empty scene to render.</returns>
        [UnityTest]
        public IEnumerator LegacyEmptyScenePreservesTheUnchangedNativeCanvas()
        {
            CanvasUI.Dispose();
            try
            {
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
                DrawLateBankFrame();
                Render();
                ObjectSpriteRenderer.BeginSceneFrame();
                Render();
                var expected = _pixels.GetPixels32();
                Assert.That(_pixels.GetPixel(32, 32).r, Is.EqualTo(1f));
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
                DrawLateBankFrame();
                Render();
                Assert.That(_pixels.GetPixel(32, 32).b, Is.EqualTo(1f));
                ObjectSpriteRenderer.BeginSceneFrame();
                yield return null;
                yield return null;
                Render();
                Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
                Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.Zero);
                DrawLateBankFrame();
                Render();
                Assert.That(_pixels.GetPixel(32, 32).b, Is.EqualTo(1f));
            }
            finally
            {
                CanvasUI.Dispose();
            }
        }

        /// <summary>Checks explicit captures materialize the same instance geometry without reusing a later portrait's data.</summary>
        [Test]
        public void SceneInstancesCanBeCapturedThroughIndependentMeshFallback()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            DrawFrame(3, true, false);
            _renderer.RenderTo(_target);
            ReadPixels();
            var expected = _pixels.GetPixels32();
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            DrawFrame(3, true, false);
            _renderer.RenderTo(_target);
            ReadPixels();
            Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
        }

        /// <summary>Checks actual original water grids and native Canvas spans remain ordered around GPU instance draws.</summary>
        /// <param name="batcher">Whether SRP batching is active in the real pipeline.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void RealWaterAndCanvasInterleavingMatchesMeshOracle(bool batcher)
        {
            var previousBatcher = GraphicsSettings.useScriptableRenderPipelineBatching;
            var caustics = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
            caustics.SetPixels32(new Color32[256 * 256]);
            caustics.Apply();
            CanvasUI.Dispose();
            using var pool = new OriginalPoolRenderer(new Vector2(Screen.width, Screen.height), _textures[2], _textures[2],
                _textures[4], _textures[4], caustics);
            try
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = batcher;
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
                DrawWaterFrame(pool);
                Render();
                var expected = _pixels.GetPixels32();
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
                DrawWaterFrame(pool);
                Render();
                Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.GreaterThan(0));
                Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
            }
            finally
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = previousBatcher;
                CanvasUI.Dispose();
                UnityEngine.Object.Destroy(caustics);
            }
        }

        /// <summary>Checks a scene mesh fallback span stays between earlier and later instances with overlapping alpha.</summary>
        /// <param name="batcher">Whether SRP batching is active in the real pipeline.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void SceneMeshFallbackInterleavesWithInstances(bool batcher)
        {
            var previousBatcher = GraphicsSettings.useScriptableRenderPipelineBatching;
            try
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = batcher;
                DrawMixedBackendFrame(false);
                Render();
                var expected = _pixels.GetPixels32();
                DrawMixedBackendFrame(true);
                Render();
                Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.EqualTo(2));
                Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
            }
            finally
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = previousBatcher;
            }
        }

        /// <summary>Checks explicit requests to a disabled scene camera preserve the same native Canvas ordering.</summary>
        [Test]
        public void DisabledSceneCameraRequestPreservesWaterAndCanvasOrdering()
        {
            var enabled = _camera.enabled;
            try
            {
                _camera.enabled = false;
                RealWaterAndCanvasInterleavingMatchesMeshOracle(false);
            }
            finally
            {
                _camera.enabled = enabled;
            }
        }

        /// <summary>Checks a native bank following background sprites stays below later sprites, including disabled-camera requests.</summary>
        /// <param name="enabled">Whether the scene camera is automatically enabled.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void PositiveOrderCanvasBankRemainsBelowForegroundSprites(bool enabled)
        {
            var previousEnabled = _camera.enabled;
            CanvasUI.Dispose();
            try
            {
                _camera.enabled = enabled;
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
                DrawLateBankFrame();
                Render();
                var expected = _pixels.GetPixels32();
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
                DrawLateBankFrame();
                Render();
                Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
            }
            finally
            {
                _camera.enabled = previousEnabled;
                CanvasUI.Dispose();
            }
        }

        /// <summary>Checks queue exhaustion converts every instance span to the original scene mesh path.</summary>
        [Test]
        public void QueueRangeExhaustionSelectsCompleteSceneMeshFallback()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            using (ObjectSpriteRenderer.BeginSceneFrameScope())
            {
                for (var span = 0; span <= SpriteSceneRenderQueues.MaximumSpans; span++)
                {
                    SpriteSceneRenderQueues.RegisterOrder(span);
                }
                using (_renderer.BeginFrame())
                {
                    _renderer.DrawQuad(_textures[0], Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f),
                        Matrix4x4.identity, Color.white, 0);
                }
            }
            Assert.That(SpriteSceneRenderQueues.RequiresMeshFallback, Is.True);
            Assert.That(SpriteSceneRenderQueues.FallbackReason, Is.Not.Empty);
            Assert.That(ObjectSpriteRenderer.InstancedDrawCount, Is.Zero);
            Assert.That(_renderer.BatchCount, Is.EqualTo(1));
        }

        /// <summary>Checks active clipping results and reversed UVs match the mesh oracle exactly.</summary>
        [Test]
        public void ClippedSkewedReversedSourcesMatchMeshOracle()
        {
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
            DrawClippedFrame();
            Render();
            var expected = _pixels.GetPixels32();
            ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Instanced;
            DrawClippedFrame();
            Render();
            Assert.That(_pixels.GetPixels32(), Is.EqualTo(expected));
        }

        /// <summary>Checks one warmed complete presentation per Unity frame does not emit managed allocation events.</summary>
        /// <returns>Real frame boundaries outside the narrowly recorded presentation operation.</returns>
        [UnityTest]
        public IEnumerator WarmedFrameSubmissionDoesNotAllocateManagedMemory()
        {
            for (var frame = 0; frame < 8; frame++)
            {
                DrawFrame(8, false, false);
                yield return null;
            }
            using var allocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 8192,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(allocations.Valid, Is.True);
            for (var frame = 0; frame < 16; frame++)
            {
                allocations.Start();
                DrawFrame(8, false, false);
                allocations.Stop();
                Assert.That(allocations.Count, Is.Zero);
                yield return null;
            }
        }

        /// <summary>Records backend warnings that indicate a submitted native property block lost one of its required buffers.</summary>
        /// <param name="message">The native or managed log message.</param>
        /// <param name="stackTrace">The engine-provided stack associated with the message.</param>
        /// <param name="type">The severity of the logged message.</param>
        private void CountMissingBufferWarning(string message, string stackTrace, LogType type)
        {
            if (message.Contains("Shader requires a compute buffer", StringComparison.Ordinal)
                && (message.Contains("_PvZVertices", StringComparison.Ordinal) || message.Contains("_PvZInstances", StringComparison.Ordinal)))
            {
                _missingBufferWarnings++;
            }
        }

        /// <summary>Draws a deterministic ordered page sequence without delegates or per-frame collections.</summary>
        /// <param name="count">The number of source polygons.</param>
        /// <param name="alternate">Whether to use A/B/A rather than distinct pages.</param>
        /// <param name="barrier">Whether an external painter slot separates every source.</param>
        private void DrawFrame(int count, bool alternate, bool barrier)
        {
            using var frame = ObjectSpriteRenderer.BeginSceneFrameScope();
            using (_renderer.BeginFrame())
            {
                for (var index = 0; index < count; index++)
                {
                    if (barrier)
                    {
                        _ = SpriteDrawState.NextOrder();
                    }
                    var texture = _textures[alternate ? index % 2 : index % _textures.Count];
                    _renderer.DrawQuad(texture, 32f, 32f, new Rect(0f, 0f, 1f, 1f),
                        Matrix4x4.Translate(new Vector3(index * 2f, index * 2f)), Color.white, 0);
                }
            }
        }

        /// <summary>Draws a fixed skew crossing fractional pixel clipping boundaries.</summary>
        private void DrawClippedFrame()
        {
            using var frame = ObjectSpriteRenderer.BeginSceneFrameScope();
            using (_renderer.BeginFrame())
            {
                var matrix = Matrix4x4.identity;
                matrix.m00 = 1.125f;
                matrix.m01 = .375f;
                matrix.m10 = -.125f;
                matrix.m11 = .875f;
                matrix.m03 = 4.125f;
                matrix.m13 = 5.375f;
                _renderer.DrawQuad(_textures[0], Screen.width * .7f, Screen.height * .8f, new Rect(1f, 1f, -1f, -1f),
                    matrix, new Color(1f, .8123456f, .4f, .7f), 0, clipOverride: new Rect(7.25f, 8.5f, Screen.width * .4f, Screen.height * .6f));
            }
        }

        /// <summary>Submits native water meshes between two sprite spans and behind a native foreground interface.</summary>
        /// <param name="pool">The original three-grid pool renderer.</param>
        private void DrawWaterFrame(OriginalPoolRenderer pool)
        {
            using var frame = ObjectSpriteRenderer.BeginSceneFrameScope();
            CanvasUI.BeginFrame();
            CanvasUI.Color = Color.red;
            CanvasUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            CanvasUI.Color = Color.white;
            using (_renderer.BeginFrame())
            {
                _renderer.DrawQuad(_textures[0], Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
                pool.Draw(137, false);
                _renderer.DrawQuad(_textures[1], Screen.width * .75f, Screen.height * .75f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
            }
            CanvasUI.Color = Color.blue;
            CanvasUI.DrawTexture(new Rect(Screen.width * .5f, Screen.height * .5f, Screen.width * .5f, Screen.height * .5f), Texture2D.whiteTexture);
            CanvasUI.EndFrame();
        }

        /// <summary>Submits the same three alpha layers with an optional middle mesh-only span.</summary>
        /// <param name="mixed">Whether the first and final layers use GPU instancing.</param>
        private void DrawMixedBackendFrame(bool mixed)
        {
            ObjectSpriteRenderer.Backend = mixed ? SpriteRenderingBackend.Instanced : SpriteRenderingBackend.Mesh;
            using var frame = ObjectSpriteRenderer.BeginSceneFrameScope();
            using (_renderer.BeginFrame())
            {
                _renderer.DrawQuad(_textures[0], Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.red, 0);
                ObjectSpriteRenderer.Backend = SpriteRenderingBackend.Mesh;
                _renderer.DrawQuad(_textures[0], Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.green, 0);
                ObjectSpriteRenderer.Backend = mixed ? SpriteRenderingBackend.Instanced : SpriteRenderingBackend.Mesh;
                _renderer.DrawQuad(_textures[0], Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.blue, 0);
            }
        }

        /// <summary>Places the first Canvas at a positive painter order after a real background sprite.</summary>
        private void DrawLateBankFrame()
        {
            using var frame = ObjectSpriteRenderer.BeginSceneFrameScope();
            CanvasUI.BeginFrame();
            using (_renderer.BeginFrame())
            {
                _renderer.DrawQuad(Texture2D.whiteTexture, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.green, 0);
                CanvasUI.Color = Color.red;
                CanvasUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                _renderer.DrawQuad(Texture2D.whiteTexture, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.blue, 0);
            }
            CanvasUI.EndFrame();
        }

        /// <summary>Draws a legacy unscoped scene whose nested portrait request occurs before a positive-order native bank.</summary>
        /// <param name="capture">The independent mesh-only local portrait renderer.</param>
        /// <param name="portrait">The surface written by the synchronous capture.</param>
        private void DrawLegacyCapturedBankFrame(ObjectSpriteRenderer capture, RenderTexture portrait)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            using (_renderer.BeginFrame())
            {
                _renderer.DrawQuad(Texture2D.whiteTexture, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.green, 0);
                capture.BeginBatch();
                capture.DrawQuad(Texture2D.whiteTexture, 8f, 8f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.magenta, 0);
                capture.RenderTo(portrait);
                CanvasUI.Color = Color.red;
                CanvasUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                _renderer.DrawQuad(Texture2D.whiteTexture, Screen.width, Screen.height, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.blue, 0);
            }
            CanvasUI.EndFrame();
        }

        /// <summary>Executes the production camera path and reads its completed target.</summary>
        private void Render()
        {
            RenderPipeline.SubmitRenderRequest(_camera, new RenderPipeline.StandardRequest { destination = _target });
            ReadPixels();
        }

        /// <summary>Reads the completed target while preserving the caller's active render texture.</summary>
        private void ReadPixels()
        {
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, 64f, 64f), 0, 0);
                _pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }
    }
}
