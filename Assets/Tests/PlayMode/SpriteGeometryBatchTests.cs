#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares deferred geometry with the immediate oracle and checks explicit ordering and lifetime barriers.</summary>
    public sealed class SpriteGeometryBatchTests
    {
        /// <summary>The original executor restored after each test.</summary>
        private SpriteGeometryExecution _previousExecution;
        /// <summary>The patterned source exercising UV interpolation and translucent edges.</summary>
        private Texture2D _texture = null!;
        /// <summary>The stable external image wrapper.</summary>
        private SpriteImage _image = null!;
        /// <summary>The offscreen destination used for exact pixel comparisons.</summary>
        private RenderTexture _target = null!;
        /// <summary>The readable destination copy.</summary>
        private Texture2D _pixels = null!;
        /// <summary>The renderer owning the current logical and GPU buffers.</summary>
        private ObjectSpriteRenderer _renderer = null!;

        /// <summary>Creates the independent source and render target after graphics initialization.</summary>
        /// <returns>A frame boundary initializing the configured render pipeline.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null));
            _previousExecution = ObjectSpriteRenderer.GeometryExecution;
            _texture = new Texture2D(8, 8, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    _texture.SetPixel(x, y, new Color(x / 7f, y / 7f, (x + y) / 14f, (x + y) % 3 == 0 ? .35f : 1f));
                }
            }
            _texture.Apply();
            _image = SpriteImage.FromTexture(_texture);
            _target = new RenderTexture(47, 37, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _target.Create();
            _pixels = new Texture2D(47, 37, TextureFormat.RGBA32, false, true);
            _renderer = new ObjectSpriteRenderer();
        }

        /// <summary>Completes all queued work and restores global rendering configuration.</summary>
        /// <returns>A frame boundary completing deferred Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ObjectSpriteRenderer.FlushPending();
            ObjectSpriteRenderer.GeometryExecution = _previousExecution;
            ObjectSpriteRenderer.CaptureObserved = null;
            SpriteDrawState.Matrix = Matrix4x4.identity;
            SpriteDrawState.Color = Color.white;
            SpriteDrawState.Clip = new Rect(0, 0, Screen.width, Screen.height);
            _renderer.Dispose();
            _target.Release();
            UnityEngine.Object.Destroy(_target);
            UnityEngine.Object.Destroy(_pixels);
            UnityEngine.Object.Destroy(_texture);
            yield return null;
        }

        /// <summary>Checks complete vertex streams, triangle order, polygon area, logical diagnostics, and rendered pixels.</summary>
        /// <param name="executionValue">The numeric value of the deferred executor.</param>
        /// <param name="sample">The mirrored, cropped, sheared, half-pixel, or degenerate geometry case.</param>
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(1, 3)]
        [TestCase(1, 4)]
        [TestCase(1, 5)]
        [TestCase(1, 6)]
        [TestCase(1, 7)]
        [TestCase(2, 0)]
        [TestCase(2, 1)]
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        [TestCase(2, 4)]
        [TestCase(2, 5)]
        [TestCase(2, 6)]
        [TestCase(2, 7)]
        public void DeferredGeometryAndPixelsMatchImmediateOracle(int executionValue, int sample)
        {
            var execution = (SpriteGeometryExecution)executionValue;
            var expected = CaptureCase(SpriteGeometryExecution.Immediate, sample);
            var actual = CaptureCase(execution, sample);

            LogGeometryDifference(expected, actual, sample, execution);
            Assert.That(actual.Positions, Is.EqualTo(expected.Positions), "Position and painter depth must remain exact.");
            Assert.That(actual.Coordinates, Is.EqualTo(expected.Coordinates), "Atlas UV interpolation must remain exact.");
            Assert.That(actual.Indices, Is.EqualTo(expected.Indices), "Triangle and region order must remain exact.");
            Assert.That(actual.Colors, Is.EqualTo(expected.Colors));
            Assert.That(actual.Effects, Is.EqualTo(expected.Effects));
            Assert.That(actual.Highlights, Is.EqualTo(expected.Highlights));
            Assert.That(actual.Pixels, Is.EqualTo(expected.Pixels), "GPU pixels must match the immediate managed geometry.");
            Assert.That(actual.Statistics.SubmittedSpriteCount, Is.EqualTo(expected.Statistics.SubmittedSpriteCount));
            Assert.That(actual.Statistics.CulledSpriteCount, Is.EqualTo(expected.Statistics.CulledSpriteCount));
            Assert.That(actual.Statistics.CoveredPixelArea, Is.EqualTo(expected.Statistics.CoveredPixelArea));
        }

        /// <summary>Checks a fixed-seed segment of mixed affine transforms and source crops against every oracle vertex stream.</summary>
        /// <param name="executionValue">The numeric value of the deferred executor.</param>
        [TestCase(1)]
        [TestCase(2)]
        public void MixedAffineSegmentMatchesImmediateOracle(int executionValue)
        {
            var expected = CaptureMixedSegment(SpriteGeometryExecution.Immediate);
            var actual = CaptureMixedSegment((SpriteGeometryExecution)executionValue);
            LogGeometryDifference(expected, actual, 100, (SpriteGeometryExecution)executionValue);

            Assert.That(actual.Positions, Is.EqualTo(expected.Positions));
            Assert.That(actual.Coordinates, Is.EqualTo(expected.Coordinates));
            Assert.That(actual.Indices, Is.EqualTo(expected.Indices));
            Assert.That(actual.Statistics.SubmittedSpriteCount, Is.EqualTo(expected.Statistics.SubmittedSpriteCount));
            Assert.That(actual.Statistics.CulledSpriteCount, Is.EqualTo(expected.Statistics.CulledSpriteCount));
            Assert.That(actual.Statistics.CoveredPixelArea, Is.EqualTo(expected.Statistics.CoveredPixelArea));
            Assert.That(actual.Pixels, Is.EqualTo(expected.Pixels));
        }

        /// <summary>Checks noninteger scene target pixel spacing preserves double sample-boundary decisions and pixels.</summary>
        /// <param name="executionValue">The numeric value of the deferred executor.</param>
        [TestCase(1)]
        [TestCase(2)]
        public void NonintegerScenePixelGridMatchesImmediateOracle(int executionValue)
        {
            var expected = CaptureSceneCase(SpriteGeometryExecution.Immediate);
            var actual = CaptureSceneCase((SpriteGeometryExecution)executionValue);

            Assert.That(actual.Positions, Is.EqualTo(expected.Positions));
            Assert.That(actual.Coordinates, Is.EqualTo(expected.Coordinates));
            Assert.That(actual.Indices, Is.EqualTo(expected.Indices));
            Assert.That(actual.Statistics.CoveredPixelArea, Is.EqualTo(expected.Statistics.CoveredPixelArea));
            Assert.That(actual.Pixels, Is.EqualTo(expected.Pixels));
        }

        /// <summary>Checks a native capture between scene commands does not reserve scene order or reset its live batch.</summary>
        [Test]
        public void SceneCaptureScenePreservesTheLiveSceneBatchAndOrder()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            using var capture = new ObjectSpriteRenderer();
            ObjectSpriteRenderer.BeginSceneFrame();
            using (ObjectSpriteRenderer.BeginDeferred())
            using (_renderer.BeginFrame())
            {
                Enqueue(_renderer, Color.red);
                capture.BeginBatch();
                using (ObjectSpriteRenderer.BeginDeferred())
                {
                    Enqueue(capture, Color.blue);
                    capture.RenderTo(_target);
                }
                Enqueue(_renderer, Color.yellow);
            }

            Assert.That(_renderer.BatchCount, Is.EqualTo(1));
            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(2));
            Assert.That(_renderer.SubmittedSprites[0].Vertices[0].z, Is.EqualTo(0f));
            Assert.That(_renderer.SubmittedSprites[1].Vertices[0].z, Is.EqualTo(-1f / 32768f));
            Assert.That(capture.SubmittedObjectCount, Is.EqualTo(1));
        }

        /// <summary>Checks arena capacity boundaries never split a compatible GPU batch or lose a logical command.</summary>
        /// <param name="count">The number of single-region commands crossing a bounded numeric segment.</param>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1024)]
        [TestCase(1025)]
        [TestCase(2051)]
        public void RegionChunkBoundariesKeepCommandOrderAndOneCompatibleBatch(int count)
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            _renderer.BeginBatch();
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                for (var index = 0; index < count; index++)
                {
                    Enqueue(_renderer, Color.white);
                }
            }

            var statistics = _renderer.LastFrameStatistics;
            Assert.That(statistics.SubmittedSpriteCount, Is.EqualTo(count));
            Assert.That(statistics.VertexCount, Is.EqualTo(count * 4));
            Assert.That(statistics.TriangleCount, Is.EqualTo(count * 2));
            Assert.That(statistics.BatchCount, Is.EqualTo(count == 0 ? 0 : 1));
            if (count > 0)
            {
                Assert.That(ObjectSpriteRenderer.NativeRegionCapacity, Is.EqualTo(1024));
                Assert.That(ObjectSpriteRenderer.NativeVertexCapacity, Is.EqualTo(12288));
                Assert.That(Batches(_renderer)[0].LastOrder, Is.EqualTo(count - 1));
            }
        }

        /// <summary>Checks synchronized statistics and public immediate draws preserve batching and command order.</summary>
        [Test]
        public void StatisticsReadsAndImmediateDrawsFlushWithoutCreatingExtraBatches()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            _renderer.BeginBatch();
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                Enqueue(_renderer, Color.red);
                Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1));
                Assert.That(_renderer.PooledObjectCount, Is.EqualTo(1));
                Assert.That(_renderer.BatchCount, Is.EqualTo(1));
                Assert.That(_renderer.CachedMaterialCount, Is.EqualTo(3));
                Assert.That(_renderer.SubmittedSprites[0].Colors[0], Is.EqualTo(Color.red));
                Enqueue(_renderer, Color.green);
                _renderer.DrawQuad(_image, 20f, 20f, new Rect(0, 0, 1, 1), Matrix4x4.identity, Color.blue, 0);
                Enqueue(_renderer, Color.yellow);
                Assert.That(_renderer.LastFrameStatistics.SubmittedSpriteCount, Is.EqualTo(4));
            }

            Assert.That(_renderer.BatchCount, Is.EqualTo(1));
            Assert.That(_renderer.SubmittedSprites[1].Colors[0], Is.EqualTo(Color.green));
            Assert.That(_renderer.SubmittedSprites[2].Colors[0], Is.EqualTo(Color.blue));
            Assert.That(_renderer.SubmittedSprites[3].Colors[0], Is.EqualTo(Color.yellow));
        }

        /// <summary>Checks renderer switches and Canvas/water-style painter barriers preserve scene insertion order.</summary>
        [Test]
        public void RendererSwitchesAndExternalOrderBarriersPreserveSceneOrder()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            using var other = new ObjectSpriteRenderer();
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(-100f, -100f, 1000f, 1000f);
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                using (_renderer.BeginFrame())
                using (other.BeginFrame())
                {
                    Enqueue(_renderer, Color.red);
                    Enqueue(other, Color.green);
                    Enqueue(_renderer, Color.blue);
                    Assert.That(SpriteDrawState.NextOrder(), Is.EqualTo(3));
                    Enqueue(_renderer, Color.yellow);
                }
            }

            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(3));
            Assert.That(other.SubmittedObjectCount, Is.EqualTo(1));
            Assert.That(_renderer.BatchCount, Is.EqualTo(3));
            Assert.That(_renderer.SubmittedSprites[0].Vertices[0].z, Is.EqualTo(0f));
            Assert.That(other.SubmittedSprites[0].Vertices[0].z, Is.EqualTo(-1f / 32768f));
            Assert.That(_renderer.SubmittedSprites[1].Vertices[0].z, Is.EqualTo(-2f / 32768f));
            Assert.That(_renderer.SubmittedSprites[2].Vertices[0].z, Is.EqualTo(-4f / 32768f));
        }

        /// <summary>Checks fully clipped and zero-area queued commands neither reserve source identity nor consume painter order.</summary>
        [Test]
        public void FullyCulledCommandsConsumeNoOrderOrSourceSlot()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            ObjectSpriteRenderer.BeginSceneFrame();
            using (ObjectSpriteRenderer.BeginDeferred())
            using (_renderer.BeginFrame())
            {
                _renderer.EnqueueQuad(_image, 20f, 20f, new Rect(0, 0, 1, 1), Matrix4x4.identity,
                    Color.white, 0, clipOverride: new Rect(100f, 100f, 10f, 10f));
                _renderer.EnqueueQuad(_image, 0f, 20f, new Rect(0, 0, 1, 1), Matrix4x4.identity, Color.white, 0);
                Assert.That(SpriteDrawState.NextOrder(), Is.Zero);
            }

            Assert.That(_renderer.SubmittedObjectCount, Is.Zero);
            Assert.That(_renderer.LastFrameStatistics.CulledSpriteCount, Is.EqualTo(2));
        }

        /// <summary>Checks source state and node visibility are fixed at enqueue while hierarchy metadata remains immediate.</summary>
        [Test]
        public void EnqueueSnapshotsStateAndHierarchyVisibilityBeforeLaterChanges()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            using var hierarchy = new SpriteHierarchy();
            hierarchy.BeginFrame(this);
            _renderer.BeginBatch();
            using (ObjectSpriteRenderer.BeginDeferred())
            using (hierarchy.BeginActor("Zombies", 1, "Normal"))
            using (hierarchy.BeginNamedSprite("Body"))
            {
                var node = SpriteHierarchy.CurrentSpriteObject!;
                var matrix = Matrix4x4.Translate(new Vector3(4f, 7f, 0f));
                _renderer.EnqueueQuad(_image, 20f, 20f, new Rect(0, 0, 1, 1), matrix, Color.red, 0,
                    clipOverride: new Rect(3f, 6f, 12f, 13f));
                Assert.That(node.GetComponent<SpriteHierarchySprite>().SourceIdentity, Is.EqualTo(_image.SourceIdentity));
                node.SetActive(false);
                SpriteDrawState.Matrix = Matrix4x4.Scale(Vector3.one * 9f);
                SpriteDrawState.Color = Color.blue;
                SpriteDrawState.Clip = new Rect(0, 0, 0, 0);
                _renderer.EnqueueQuad(_image, 20f, 20f, new Rect(0, 0, 1, 1), matrix, Color.green, 0);
            }

            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1));
            Assert.That(_renderer.SubmittedSprites[0].Colors[0], Is.EqualTo(Color.red));
            Assert.That(_renderer.SubmittedSprites[0].Clip, Is.EqualTo(new Rect(3f, 6f, 12f, 13f)));
            Assert.That(_renderer.SubmittedSprites[0].Vertices[0].x, Is.EqualTo(4f));
            Assert.That(_renderer.LastFrameStatistics.CulledSpriteCount, Is.EqualTo(1));
        }

        /// <summary>Checks nested scopes and same-frame capture reuse complete each surface and preserve caller render state.</summary>
        /// <param name="batcher">Whether the SRP Batcher is active during repeated camera requests.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void NestedScopesAndEightCapturesCompleteBeforeReturning(bool batcher)
        {
            var previousBatcher = GraphicsSettings.useScriptableRenderPipelineBatching;
            var previousActive = RenderTexture.active;
            var captures = 0;
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            ObjectSpriteRenderer.CaptureObserved = (camera, destination) =>
            {
                captures++;
                Assert.That(destination, Is.SameAs(_target));
                Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1));
                Assert.That(Batches(_renderer)[0].Mesh.vertexCount, Is.EqualTo(4));
            };
            try
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = batcher;
                using (ObjectSpriteRenderer.BeginDeferred())
                {
                    for (var index = 0; index < 8; index++)
                    {
                        _renderer.BeginBatch();
                        using (ObjectSpriteRenderer.BeginDeferred())
                        {
                            Enqueue(_renderer, index % 2 == 0 ? Color.red : Color.green);
                            _renderer.RenderTo(_target);
                            Assert.That(captures, Is.EqualTo(index + 1));
                            Assert.That(RenderTexture.active, Is.SameAs(previousActive));
                        }
                    }
                }
                Assert.That(_renderer.BatchCount, Is.EqualTo(1));
                Assert.That(_renderer.PooledObjectCount, Is.EqualTo(1));
            }
            finally
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = previousBatcher;
                ObjectSpriteRenderer.CaptureObserved = null;
            }
        }

        /// <summary>Checks copied inputs survive bounded provider-cache eviction while preserving multi-page tight seams.</summary>
        /// <param name="executionValue">The numeric value of the deferred executor.</param>
        /// <param name="reversed">Whether both normalized source axes run backwards.</param>
        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(2, true)]
        public void MultiPageTightRegionsSurviveBorrowedBufferEviction(int executionValue, bool reversed)
        {
            var pages = Array.Empty<Texture2D>();
            try
            {
                using var provider = CreateTiledProvider(_ =>
                {
                }, out pages);
                var image = provider.LoadImage("images/tiled.png") ?? throw new InvalidOperationException("Tiled fixture missing.");
                var uv = reversed ? new Rect(1, 1, -1, -1) : new Rect(0, 0, 1, 1);
                var matrix = Matrix4x4.Translate(new Vector3(4.2f, 1.7f, 0));
                matrix.m01 = .21f;
                var clip = new Rect(0, 0, 39, 34);
                ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Immediate;
                _renderer.BeginBatch();
                _renderer.DrawQuad(image, 29.3f, 26.7f, uv, matrix, new Color(1, 1, 1, .7f), 0, clipOverride: clip);
                _renderer.RenderTo(_target);
                var expected = ReadSnapshot();

                ObjectSpriteRenderer.GeometryExecution = (SpriteGeometryExecution)executionValue;
                _renderer.BeginBatch();
                using (ObjectSpriteRenderer.BeginDeferred())
                {
                    _renderer.EnqueueQuad(image, 29.3f, 26.7f, uv, matrix, new Color(1, 1, 1, .7f), 0, clipOverride: clip);
                    for (var index = 1; index <= 520; index++)
                    {
                        // The original view must be evicted and its arrays reused before this queued command is consumed.
                        image.GetRegions(new Rect(index * .0001f, 0, 1f - index * .0001f, 1));
                    }
                    Assert.That(provider.PreparedRegionCacheCount, Is.EqualTo(512));
                    _renderer.RenderTo(_target);
                }
                var actual = ReadSnapshot();

                Assert.That(actual.Positions, Is.EqualTo(expected.Positions));
                Assert.That(actual.Coordinates, Is.EqualTo(expected.Coordinates));
                Assert.That(actual.Indices, Is.EqualTo(expected.Indices));
                Assert.That(actual.Statistics.CoveredPixelArea, Is.EqualTo(expected.Statistics.CoveredPixelArea));
                Assert.That(actual.Pixels, Is.EqualTo(expected.Pixels));
                Assert.That(actual.Statistics.SubmittedSpriteCount, Is.EqualTo(1));
                Assert.That(actual.Statistics.BatchCount, Is.EqualTo(2));
            }
            finally
            {
                foreach (var page in pages)
                {
                    UnityEngine.Object.Destroy(page);
                }
            }
        }

        /// <summary>Checks final provider release and next-frame page retirement resolve pending geometry before releasing textures.</summary>
        /// <param name="retire">Whether to retire idle pages across frame boundaries rather than dispose the final provider.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void ProviderReleaseAndPageRetirementFlushBeforeTextureRelease(bool retire)
        {
            var pages = Array.Empty<Texture2D>();
            var released = 0;
            try
            {
                using var provider = CreateTiledProvider(_ =>
                {
                    Assert.That(UnflushedSourceCount(_renderer), Is.EqualTo(1), "Page release must observe already committed source geometry.");
                    released++;
                }, out pages);
                var image = provider.LoadImage("images/tiled.png") ?? throw new InvalidOperationException("Tiled fixture missing.");
                ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
                _renderer.BeginBatch();
                using (ObjectSpriteRenderer.BeginDeferred())
                {
                    _renderer.EnqueueQuad(image, 20f, 20f, new Rect(0, 0, 1, 1), Matrix4x4.identity, Color.white, 0);
                    if (retire)
                    {
                        var collect = typeof(SpriteAssetProvider).GetMethod("CollectRetiredPages", BindingFlags.Instance | BindingFlags.NonPublic)!;
                        for (var frame = 0; frame < 3; frame++)
                        {
                            SpriteDrawState.BeginFrame();
                            collect.Invoke(provider, null);
                        }
                    }
                    else
                    {
                        provider.Dispose();
                    }
                    Assert.That(released, Is.EqualTo(2));
                    _renderer.RenderTo(_target);
                }
                Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1));
                Assert.That(_renderer.BatchCount, Is.EqualTo(2));
            }
            finally
            {
                foreach (var page in pages)
                {
                    UnityEngine.Object.Destroy(page);
                }
            }
        }

        /// <summary>Checks a failed final scene flush restores the frame depth and permits the next scene to reset normally.</summary>
        [Test]
        public void FailedFinalSceneFlushRestoresScopeDepthAndClearsPendingInputs()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            ObjectSpriteRenderer.BeginSceneFrame();
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                var frame = _renderer.BeginFrame();
                for (var index = 0; index < 32767; index++)
                {
                    SpriteDrawState.NextOrder();
                }
                Enqueue(_renderer, Color.red);
                Assert.Throws<InvalidOperationException>(() => frame.Dispose());
                Assert.That(_renderer.IsFrameActive, Is.False);
                ObjectSpriteRenderer.BeginSceneFrame();
                using (_renderer.BeginFrame())
                {
                    Enqueue(_renderer, Color.green);
                }
                Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1));
                Assert.That(_renderer.SubmittedSprites[0].Colors[0], Is.EqualTo(Color.green));
            }
        }

        /// <summary>Checks disabled Burst uses the immediate production fallback without creating or replacing numeric native storage.</summary>
        [Test]
        public void DisabledBurstUsesImmediateGeometryWithoutGrowingTheArena()
        {
            var compiler = Assembly.Load("Unity.Burst").GetType("Unity.Burst.BurstCompiler")!;
            var options = compiler.GetField("Options", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
            var enabled = options.GetType().GetProperty("EnableBurstCompilation")!;
            var previous = (bool)enabled.GetValue(options)!;
            var arena = typeof(ObjectSpriteRenderer).GetField("s_geometry", BindingFlags.NonPublic | BindingFlags.Static)!;
            var previousArena = arena.GetValue(null);
            var expected = CaptureCase(SpriteGeometryExecution.Immediate, 6);
            try
            {
                enabled.SetValue(options, false);
                var actual = CaptureCase(SpriteGeometryExecution.Burst, 6);
                Assert.That(actual.Positions, Is.EqualTo(expected.Positions));
                Assert.That(actual.Coordinates, Is.EqualTo(expected.Coordinates));
                Assert.That(actual.Indices, Is.EqualTo(expected.Indices));
                Assert.That(actual.Statistics.CoveredPixelArea, Is.EqualTo(expected.Statistics.CoveredPixelArea));
                Assert.That(actual.Pixels, Is.EqualTo(expected.Pixels));
                _renderer.BeginBatch();
                using (ObjectSpriteRenderer.BeginDeferred())
                {
                    Enqueue(_renderer, Color.green);
                    Assert.That(UnflushedSourceCount(_renderer), Is.EqualTo(1), "Disabled Burst must commit each source immediately.");
                    Assert.That(arena.GetValue(null), Is.SameAs(previousArena), "Disabled Burst must not grow native geometry storage.");
                }
            }
            finally
            {
                enabled.SetValue(options, previous);
            }
        }

        /// <summary>Checks default and repeated disposal of one deferred scope do not leave later submissions accidentally deferred.</summary>
        [Test]
        public void DefaultAndRepeatedDeferredScopeDisposalKeepOutsideDrawsImmediate()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            var empty = default(DeferredSpriteScope);
            empty.Dispose();
            _renderer.BeginBatch();
            var scope = ObjectSpriteRenderer.BeginDeferred();
            Enqueue(_renderer, Color.red);
            scope.Dispose();
            scope.Dispose();
            Assert.That(UnflushedSourceCount(_renderer), Is.EqualTo(1));
            _renderer.BeginBatch();
            Enqueue(_renderer, Color.green);
            Assert.That(UnflushedSourceCount(_renderer), Is.EqualTo(1), "Submission outside an active scope must remain immediate.");
        }

        /// <summary>Checks oversized fallback preserves source identity and enforces the limit after completing accepted pending commands.</summary>
        /// <param name="atSortingLimit">Whether the accepted pending prefix fills the final source slot before fallback.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void OversizedSingleSpriteUsesImmediateFallbackWithoutSplittingItsIdentity(bool atSortingLimit)
        {
            var texture = new Texture2D(1025, 1, TextureFormat.RGBA32, false, true);
            var pixels = new Color[1025];
            var pieces = new Rect[1025];
            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = Color.white;
                pieces[index] = new Rect(index, 0, 1, 1);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            var metadata = new SpriteAtlasImage
            {
                SourceIdentity = "oversized.png", Width = 1025, Height = 1, VisiblePixels = pieces,
                Tiles = new[] { new SpriteAtlasTile { Page = 0, PageWidth = 1025, PageHeight = 1,
                    SourcePixels = new Rect(0, 0, 1025, 1), TextureUv = new Rect(0, 0, 1, 1) } }
            };
            var catalog = new SpriteAtlasCatalog
            {
                Layouts = new[] { new SpriteAtlasLayout { MaximumPageSize = 2048,
                    Pages = new[] { "oversized-page" }, Images = new[] { metadata } } }
            };
            try
            {
                using var provider = new SpriteAssetProvider(catalog, 2048, _ => texture,
                    _ =>
                    {
                    });
                var image = provider.LoadImage(metadata.SourceIdentity);
                Assert.That(image, Is.Not.Null);
                Assert.That(image!.GetRegionCount(new Rect(0, 0, 1, 1)), Is.EqualTo(1025));
                ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
                _renderer.BeginBatch();
                using (ObjectSpriteRenderer.BeginDeferred())
                {
                    if (atSortingLimit)
                    {
                        for (var index = 0; index < short.MaxValue - 1; index++)
                        {
                            Enqueue(_renderer, Color.white);
                        }
                        ObjectSpriteRenderer.FlushPending();
                    }
                    Enqueue(_renderer, Color.red);
                    if (atSortingLimit)
                    {
                        Assert.That(UnflushedSourceCount(_renderer), Is.EqualTo(short.MaxValue - 1),
                            "The final accepted source must remain pending until the oversized fallback flushes it.");
                        Assert.Throws<ArgumentOutOfRangeException>(() => _renderer.EnqueueQuad(image, 20f, 20f,
                            new Rect(0, 0, 1, 1), Matrix4x4.identity, Color.white, 0));
                    }
                    else
                    {
                        _renderer.EnqueueQuad(image, 20f, 20f, new Rect(0, 0, 1, 1), Matrix4x4.identity, Color.white, 0);
                        Enqueue(_renderer, Color.green);
                    }
                }

                if (atSortingLimit)
                {
                    Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(short.MaxValue));
                    Assert.That(_renderer.PooledObjectCount, Is.EqualTo(short.MaxValue), "A rejected oversized source must not reserve an extra identity.");
                    Assert.That(_renderer.SubmittedSprites[short.MaxValue - 1].Colors[0], Is.EqualTo(Color.red));
                    return;
                }
                Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(3));
                Assert.That(_renderer.LastFrameStatistics.VertexCount, Is.EqualTo(4 + 1025 * 4 + 4));
                Assert.That(_renderer.BatchCount, Is.EqualTo(3));
                Assert.That(Batches(_renderer)[1].LastOrder, Is.EqualTo(1));
                Assert.That(_renderer.SubmittedSprites[1].SourceImage, Is.SameAs(image));
                Assert.That(_renderer.SubmittedSprites[2].Vertices[0].z, Is.EqualTo(-2f / 32768f));
                Assert.That(ObjectSpriteRenderer.NativeRegionCapacity, Is.EqualTo(1024));
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }

        /// <summary>Checks portable mesh capacity splits happen at 65532 vertices independently of numeric chunks.</summary>
        [Test]
        public void MeshCapacityBoundaryKeepsPortableIndicesAndOneAdditionalBatch()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            _renderer.BeginBatch();
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                for (var index = 0; index < 16384; index++)
                {
                    Enqueue(_renderer, Color.white);
                }
            }

            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(16384));
            Assert.That(_renderer.BatchCount, Is.EqualTo(2));
            Assert.That(Batches(_renderer)[0].VertexCount, Is.EqualTo(65532));
            Assert.That(Batches(_renderer)[1].VertexCount, Is.EqualTo(4));
            Assert.That(_renderer.LastFrameStatistics.CapacitySplitCount, Is.EqualTo(1));
        }

        /// <summary>Checks the final scene sorting slot is accepted and an additional external order is rejected.</summary>
        [Test]
        public void SceneSortingLimitFlushesTheFinalAcceptedCommand()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            ObjectSpriteRenderer.BeginSceneFrame();
            for (var index = 0; index < 32766; index++)
            {
                SpriteDrawState.NextOrder();
            }
            using (ObjectSpriteRenderer.BeginDeferred())
            using (_renderer.BeginFrame())
            {
                Enqueue(_renderer, Color.white);
                Assert.Throws<InvalidOperationException>(() => SpriteDrawState.NextOrder());
                Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1));
                Assert.That(_renderer.SubmittedSprites[0].Vertices[0].z, Is.EqualTo(-32766f / 32768f));
            }
        }

        /// <summary>Checks resetting a batch or disposing a pending renderer resolves its command references safely.</summary>
        [Test]
        public void BatchResetAndIdempotentDisposalResolvePendingCommands()
        {
            ObjectSpriteRenderer.GeometryExecution = SpriteGeometryExecution.Managed;
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                _renderer.BeginBatch();
                Enqueue(_renderer, Color.red);
                _renderer.BeginBatch();
                Assert.That(_renderer.SubmittedObjectCount, Is.Zero);
                Enqueue(_renderer, Color.blue);
                _renderer.Dispose();
                _renderer.Dispose();
            }

            Assert.Throws<ObjectDisposedException>(() => Enqueue(_renderer, Color.green));
            using var replacement = new ObjectSpriteRenderer();
            replacement.BeginBatch();
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                Enqueue(replacement, Color.yellow);
            }
            Assert.That(replacement.SubmittedObjectCount, Is.EqualTo(1));
        }

        /// <summary>Captures a sheared half-pixel case on the production scene target's noninteger logical grid.</summary>
        /// <param name="execution">The immediate oracle or deferred numeric executor.</param>
        /// <returns>The published scene geometry and pixels.</returns>
        private Snapshot CaptureSceneCase(SpriteGeometryExecution execution)
        {
            var camera = ObjectSpriteRenderer.SceneCamera;
            var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = _target;
                ObjectSpriteRenderer.BeginSceneFrame();
                using (_renderer.BeginFrame())
                {
                    return CaptureCase(execution, 4, false);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
            }
        }

        /// <summary>Submits one differential geometry case and snapshots its published streams and pixels.</summary>
        /// <param name="execution">The oracle or numeric executor.</param>
        /// <param name="sample">The deterministic transformation and clip case.</param>
        /// <param name="resetBatch">Whether to begin an independent capture batch rather than retain the scene pixel grid.</param>
        /// <returns>The actual published geometry and GPU pixels.</returns>
        private Snapshot CaptureCase(SpriteGeometryExecution execution, int sample, bool resetBatch = true)
        {
            ObjectSpriteRenderer.GeometryExecution = execution;
            if (resetBatch)
            {
                _renderer.BeginBatch();
            }
            var matrix = Matrix4x4.Translate(new Vector3(3.2f, 2.7f, 0f));
            var uv = new Rect(0, 0, 1, 1);
            Rect? clip = null;
            if (sample == 1 || sample >= 4)
            {
                matrix.m01 = .37f;
                matrix.m10 = -.13f;
                clip = new Rect(sample == 4 ? 10.499f : sample == 5 ? 10.5f : 10.501f, 3.3f, 22.2f, 24.6f);
            }
            if (sample == 2)
            {
                matrix = Matrix4x4.TRS(new Vector3(31.2f, 28.7f, 0f), Quaternion.identity, new Vector3(-1f, -1f, 1f));
                uv = new Rect(1f, 1f, -1f, -1f);
            }
            if (sample == 3)
            {
                matrix.m00 = 0f;
                matrix.m10 = 0f;
            }
            if (sample == 6)
            {
                uv = new Rect(.17f, .14f, .6f, .73f);
            }
            if (sample == 7)
            {
                clip = new Rect(100f, 100f, 10f, 10f);
            }
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                _renderer.EnqueueQuad(_image, 21.7f, 19.3f, uv, matrix, new Color(.83f, .71f, .92f, .77f), 0,
                    .2f, .1f, .03f, new Color(.07f, .02f, .04f), clip, true);
                _renderer.RenderTo(_target);
            }
            return ReadSnapshot();
        }

        /// <summary>Captures a reproducible numeric segment containing one hundred independent source states.</summary>
        /// <param name="execution">The immediate oracle or deferred executor.</param>
        /// <returns>The ordered published geometry and final GPU pixels.</returns>
        private Snapshot CaptureMixedSegment(SpriteGeometryExecution execution)
        {
            var random = new System.Random(0x5eed);
            ObjectSpriteRenderer.GeometryExecution = execution;
            _renderer.BeginBatch();
            using (ObjectSpriteRenderer.BeginDeferred())
            {
                for (var index = 0; index < 100; index++)
                {
                    var matrix = Matrix4x4.identity;
                    matrix.m00 = (float)(random.NextDouble() * 2d - 1d);
                    matrix.m01 = (float)(random.NextDouble() * .8d - .4d);
                    matrix.m10 = (float)(random.NextDouble() * .6d - .3d);
                    matrix.m11 = (float)(random.NextDouble() * 2d - 1d);
                    matrix.m03 = (float)(random.NextDouble() * 30d + 5d);
                    matrix.m13 = (float)(random.NextDouble() * 25d + 5d);
                    var width = (float)(random.NextDouble() * 35d + .3d);
                    var height = (float)(random.NextDouble() * 30d + .3d);
                    var cropWidth = (float)(random.NextDouble() * .6d + .2d);
                    var cropHeight = (float)(random.NextDouble() * .6d + .2d);
                    var reversed = index % 3 == 0;
                    var uv = reversed ? new Rect(.9f, .9f, -cropWidth, -cropHeight)
                        : new Rect(.1f, .1f, cropWidth, cropHeight);
                    var clip = new Rect(10.5f + (index % 3 - 1) * .001f, 3.3f, 23.7f, 26.1f);
                    _renderer.EnqueueQuad(_image, width, height, uv, matrix, new Color(.8f, .7f, .9f, .43f), 0,
                        clipOverride: clip);
                }
                _renderer.RenderTo(_target);
            }
            return ReadSnapshot();
        }

        /// <summary>Copies the actual published mesh streams and GPU pixels before the renderer is reused.</summary>
        /// <returns>The independent geometry and pixel snapshot.</returns>
        private Snapshot ReadSnapshot()
        {
            var snapshot = new Snapshot { Statistics = _renderer.LastFrameStatistics };
            for (var index = 0; index < _renderer.BatchCount; index++)
            {
                var mesh = Batches(_renderer)[index].Mesh;
                snapshot.Positions.AddRange(mesh.vertices);
                snapshot.Coordinates.AddRange(mesh.uv);
                snapshot.Indices.AddRange(mesh.triangles);
                snapshot.Colors.AddRange(mesh.colors);
                var channels = new List<Vector4>();
                mesh.GetUVs(1, channels);
                snapshot.Effects.AddRange(channels);
                mesh.GetUVs(2, channels);
                snapshot.Highlights.AddRange(channels);
            }
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
                _pixels.Apply();
                snapshot.Pixels = _pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
            }
            return snapshot;
        }

        /// <summary>Reports exact float components only when a differential case disagrees.</summary>
        /// <param name="expected">The original immediate implementation's output.</param>
        /// <param name="actual">The deferred numeric output.</param>
        /// <param name="sample">The deterministic source geometry case.</param>
        /// <param name="execution">The deferred executor under examination.</param>
        private static void LogGeometryDifference(Snapshot expected, Snapshot actual, int sample, SpriteGeometryExecution execution)
        {
            var mismatch = expected.Positions.Count != actual.Positions.Count
                || expected.Statistics.CoveredPixelArea != actual.Statistics.CoveredPixelArea;
            for (var index = 0; index < Math.Min(expected.Positions.Count, actual.Positions.Count); index++)
            {
                mismatch |= !expected.Positions[index].Equals(actual.Positions[index]);
                mismatch |= !expected.Coordinates[index].Equals(actual.Coordinates[index]);
            }
            if (!mismatch)
            {
                return;
            }
            TestContext.WriteLine("Geometry difference sample=" + sample + " executor=" + execution
                + " areaExpected=" + expected.Statistics.CoveredPixelArea.ToString("R")
                + " areaActual=" + actual.Statistics.CoveredPixelArea.ToString("R"));
            for (var index = 0; index < Math.Min(24, Math.Max(expected.Positions.Count, actual.Positions.Count)); index++)
            {
                var expectedPosition = index < expected.Positions.Count ? expected.Positions[index] : Vector3.positiveInfinity;
                var actualPosition = index < actual.Positions.Count ? actual.Positions[index] : Vector3.positiveInfinity;
                var expectedUv = index < expected.Coordinates.Count ? expected.Coordinates[index] : Vector2.positiveInfinity;
                var actualUv = index < actual.Coordinates.Count ? actual.Coordinates[index] : Vector2.positiveInfinity;
                TestContext.WriteLine("v" + index
                    + " expected=(" + expectedPosition.x.ToString("R") + "," + expectedPosition.y.ToString("R") + ")"
                    + " actual=(" + actualPosition.x.ToString("R") + "," + actualPosition.y.ToString("R") + ")"
                    + " uvExpected=(" + expectedUv.x.ToString("R") + "," + expectedUv.y.ToString("R") + ")"
                    + " uvActual=(" + actualUv.x.ToString("R") + "," + actualUv.y.ToString("R") + ")");
            }
        }

        /// <summary>Queues a single compatible source region without a destination clip.</summary>
        /// <param name="renderer">The renderer whose segment receives the command.</param>
        /// <param name="color">The diagnostic source tint.</param>
        private void Enqueue(ObjectSpriteRenderer renderer, Color color)
        {
            renderer.EnqueueQuad(_image, 20f, 20f, new Rect(0, 0, 1, 1), Matrix4x4.identity, color, 0);
        }

        /// <summary>Builds two padded pages with four tight horizontal pieces crossing their shared source edge.</summary>
        /// <param name="releasePage">The observed texture-release callback.</param>
        /// <param name="pages">Receives the fixture-owned page textures.</param>
        /// <returns>The independently owned provider using the two exact page mappings.</returns>
        private SpriteAssetProvider CreateTiledProvider(Action<Texture2D> releasePage, out Texture2D[] pages)
        {
            pages = new Texture2D[2];
            for (var page = 0; page < pages.Length; page++)
            {
                var texture = new Texture2D(6, 10, TextureFormat.RGBA32, false, true)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                for (var y = 0; y < 10; y++)
                {
                    for (var x = 0; x < 6; x++)
                    {
                        texture.SetPixel(x, y, _texture.GetPixel(Mathf.Clamp(page * 4 + x - 1, 0, 7), Mathf.Clamp(y - 1, 0, 7)));
                    }
                }
                texture.Apply();
                pages[page] = texture;
            }
            var metadata = new SpriteAtlasImage
            {
                SourceIdentity = "images/tiled.png", Width = 8, Height = 8,
                VisiblePixels = new[] { new Rect(0, 0, 8, 2), new Rect(0, 2, 8, 2), new Rect(0, 4, 8, 2), new Rect(0, 6, 8, 2) },
                Tiles = new[]
                {
                    new SpriteAtlasTile { Page = 0, PageWidth = 6, PageHeight = 10, SourcePixels = new Rect(0, 0, 4, 8),
                        TextureUv = new Rect(1f / 6f, 1f / 10f, 4f / 6f, 8f / 10f) },
                    new SpriteAtlasTile { Page = 1, PageWidth = 6, PageHeight = 10, SourcePixels = new Rect(4, 0, 4, 8),
                        TextureUv = new Rect(1f / 6f, 1f / 10f, 4f / 6f, 8f / 10f) }
                }
            };
            var catalog = new SpriteAtlasCatalog
            {
                Layouts = new[] { new SpriteAtlasLayout { MaximumPageSize = 256,
                    Pages = new[] { "left", "right" }, Images = new[] { metadata } } }
            };
            var sourcePages = pages;
            return new SpriteAssetProvider(catalog, 256, name => name == "left" ? sourcePages[0] : sourcePages[1], releasePage);
        }

        /// <summary>Reads source usage without invoking a synchronizing production diagnostic getter.</summary>
        /// <param name="renderer">The renderer whose pending state is being checked.</param>
        /// <returns>The number of source commands already committed before any implicit flush.</returns>
        private static int UnflushedSourceCount(ObjectSpriteRenderer renderer)
        {
            return (int)typeof(ObjectSpriteRenderer).GetField("_used", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(renderer)!;
        }

        /// <summary>Reads the actual retained GPU batches without adding a production diagnostic API.</summary>
        /// <param name="renderer">The renderer owning the retained batches.</param>
        /// <returns>The renderer's ordered batch pool.</returns>
        private static List<SpriteMeshBatch> Batches(ObjectSpriteRenderer renderer)
        {
            return (List<SpriteMeshBatch>)typeof(ObjectSpriteRenderer).GetField("_batches", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(renderer)!;
        }

        /// <summary>Retains copied geometry and pixels after the renderer is reused by the next executor.</summary>
        private sealed class Snapshot
        {
            /// <summary>The original ordered vertex positions and painter depths.</summary>
            internal readonly List<Vector3> Positions = new List<Vector3>();
            /// <summary>The clipped atlas UV coordinates.</summary>
            internal readonly List<Vector2> Coordinates = new List<Vector2>();
            /// <summary>The triangulated ordered polygon indices.</summary>
            internal readonly List<int> Indices = new List<int>();
            /// <summary>The uploaded source tints.</summary>
            internal readonly List<Color> Colors = new List<Color>();
            /// <summary>The uploaded source effect stream.</summary>
            internal readonly List<Vector4> Effects = new List<Vector4>();
            /// <summary>The uploaded highlight stream.</summary>
            internal readonly List<Vector4> Highlights = new List<Vector4>();
            /// <summary>The captured GPU pixels.</summary>
            internal Color32[] Pixels = Array.Empty<Color32>();
            /// <summary>The source and polygon workload counters.</summary>
            internal SpriteBatchStatistics Statistics;
        }
    }
}
