#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Verifies untimed capture diagnostics preserve observers and count independent targets correctly.</summary>
    public sealed class RenderingCoverageCaptureTests
    {
        /// <summary>The diagnostic callback restored after each test.</summary>
        private Action<Camera, RenderTexture>? _previous;
        /// <summary>The synthetic scene object containing a four-pixel-wide quad.</summary>
        private GameObject _quad = null!;
        /// <summary>The synthetic orthographic projection camera.</summary>
        private GameObject _cameraObject = null!;
        /// <summary>The owned synthetic quad geometry.</summary>
        private Mesh _mesh = null!;
        /// <summary>The first target dimensions without requiring a GPU allocation.</summary>
        private RenderTexture _target = null!;

        /// <summary>Creates an isolated projected quad for CPU coverage checks.</summary>
        [SetUp]
        public void SetUp()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _previous = ObjectSpriteRenderer.CaptureObserved;
            ObjectSpriteRenderer.CaptureObserved = null;
            _cameraObject = new GameObject("Capture coverage camera");
            var camera = _cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            camera.aspect = 1f;
            camera.cullingMask = 1 << 31;
            _quad = new GameObject("Capture coverage quad") { layer = 31 };
            _mesh = new Mesh
            {
                vertices = new[] { new Vector3(-2f, -2f, 0f), new Vector3(2f, -2f, 0f), new Vector3(2f, 2f, 0f), new Vector3(-2f, 2f, 0f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            _quad.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _quad.AddComponent<MeshRenderer>();
            _target = new RenderTexture(4, 4, 0);
        }

        /// <summary>Releases only synthetic test objects and restores the preceding production observer.</summary>
        [TearDown]
        public void TearDown()
        {
            ObjectSpriteRenderer.CaptureObserved = _previous;
            UnityEngine.Object.DestroyImmediate(_quad);
            UnityEngine.Object.DestroyImmediate(_cameraObject);
            UnityEngine.Object.DestroyImmediate(_mesh);
            UnityEngine.Object.DestroyImmediate(_target);
        }

        /// <summary>Checks separate targets accumulate work without treating their shared diagonal as extra overdraw.</summary>
        [Test]
        public void CountsIndependentTargetsAndSharedEdgesWithoutDoubleCounting()
        {
            var capture = new RenderingCoverageCapture();
            var camera = _cameraObject.GetComponent<Camera>();
            using (capture.Begin())
            {
                ObjectSpriteRenderer.CaptureObserved!(camera, _target);
                ObjectSpriteRenderer.CaptureObserved!(camera, _target);
            }

            Assert.That(capture.CaptureCount, Is.EqualTo(2));
            Assert.That(capture.TargetPixelCount, Is.EqualTo(32));
            Assert.That(capture.CoveredPixelSamples, Is.EqualTo(32));
            Assert.That(capture.MeanGeometryCoverage, Is.EqualTo(1d));
            Assert.That(capture.PeakGeometryCoverage, Is.EqualTo(1));
            Assert.That(capture.MeshCount, Is.EqualTo(2));
            Assert.That(capture.Targets, Has.Count.EqualTo(1));
            foreach (var pixel in capture.Targets[0].Pixels)
            {
                Assert.That(pixel, Is.EqualTo(2));
            }
        }

        /// <summary>Checks repeated scopes reuse heatmap storage and restore a chained observer even when presentation fails.</summary>
        [Test]
        public void ReusesSizeBuffersAndRestoresExistingObserverAfterException()
        {
            var observations = 0;
            Action<Camera, RenderTexture> previous = (_, _) =>
            {
                observations++;
            };
            ObjectSpriteRenderer.CaptureObserved = previous;
            var capture = new RenderingCoverageCapture();
            var camera = _cameraObject.GetComponent<Camera>();
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (capture.Begin())
                {
                    ObjectSpriteRenderer.CaptureObserved!(camera, _target);
                    throw new InvalidOperationException("Synthetic presentation failure.");
                }
            });
            var buffer = capture.Targets[0].Pixels;
            Assert.That(ObjectSpriteRenderer.CaptureObserved, Is.SameAs(previous));
            using (capture.Begin())
            {
                ObjectSpriteRenderer.CaptureObserved!(camera, _target);
            }

            Assert.That(capture.Targets[0].Pixels, Is.SameAs(buffer));
            Assert.That(capture.CaptureCount, Is.EqualTo(1));
            Assert.That(capture.CoveredPixelSamples, Is.EqualTo(16));
            Assert.That(observations, Is.EqualTo(2));
            Assert.That(ObjectSpriteRenderer.CaptureObserved, Is.SameAs(previous));
        }

        /// <summary>Checks the real render hook observes published meshes before their temporary capture state is retired.</summary>
        /// <returns>Frame boundaries allowing the project pipeline and deferred renderer destruction to complete.</returns>
        [UnityTest]
        public IEnumerator RealRenderRequestObservesGeometryBeforeCapturePoolIsHidden()
        {
            yield return null;
            _quad.SetActive(false);
            using var renderer = new ObjectSpriteRenderer();
            renderer.BeginBatch();
            for (var index = 0; index < 2; index++)
            {
                renderer.DrawQuad(Texture2D.whiteTexture, 4f, 4f, new Rect(0f, 0f, 1f, 1f),
                    Matrix4x4.identity, new Color(1f, 1f, 1f, .5f), 0);
            }

            var capture = new RenderingCoverageCapture();
            _target.Create();
            using (capture.Begin())
            {
                renderer.RenderTo(_target);
            }

            Assert.That(capture.CaptureCount, Is.EqualTo(1));
            Assert.That(capture.MeshCount, Is.EqualTo(1));
            Assert.That(capture.CoveredPixelSamples, Is.EqualTo(32));
            Assert.That(capture.MeanGeometryCoverage, Is.EqualTo(2d));
            Assert.That(capture.PeakGeometryCoverage, Is.EqualTo(2));
            Assert.That(ObjectSpriteRenderer.CaptureObserved, Is.Null);
            _target.Release();
            yield return null;
        }
    }
}
