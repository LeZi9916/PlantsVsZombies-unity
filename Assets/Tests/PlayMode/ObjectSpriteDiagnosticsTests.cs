#nullable enable
using System.Collections;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Protects the latest logical source diagnostics while GPU geometry remains independently submitted.</summary>
    public sealed class ObjectSpriteDiagnosticsTests
    {
        /// <summary>The isolated source texture retained across all submissions in a test.</summary>
        private Texture2D _texture = null!;
        /// <summary>The logical wrapper reused without allocating on warm draws.</summary>
        private SpriteImage _image = null!;
        /// <summary>The production source and batch pool under test.</summary>
        private ObjectSpriteRenderer _renderer = null!;

        /// <summary>Initializes an independent source and production pool after the graphics pipeline starts.</summary>
        /// <returns>A boundary before creating Unity resources.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            _texture.SetPixel(0, 0, Color.white);
            _texture.Apply();
            _image = SpriteImage.FromTexture(_texture);
            _renderer = new ObjectSpriteRenderer();
        }

        /// <summary>Releases only fixture-owned scene and source resources.</summary>
        /// <returns>A boundary completing deferred destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer?.Dispose();
            UnityEngine.Object.Destroy(_texture);
            yield return null;
        }

        /// <summary>Snapshots the newest unobserved submission while publishing non-geometric metadata immediately.</summary>
        [Test]
        public void ConsecutiveUnobservedSubmissionsExposeLatestPoseAndImmediateMetadata()
        {
            var firstMatrix = Matrix4x4.Translate(new Vector3(1f, 2f));
            _renderer.BeginBatch();
            _renderer.DrawQuad(_image, 18f, 14f, new Rect(0f, 0f, 1f, 1f), firstMatrix, Color.red, 0);
            var first = _renderer.SubmittedSprites[0];
            Assert.That(first.SourceImage, Is.SameAs(_image));
            Assert.That(first.Mode, Is.Zero);

            var matrix = Affine(1.3f, .2f, -.15f, .8f, 21.25f, 32.5f);
            var expectedMatrix = matrix;
            var uv = new Rect(.85f, .9f, -.7f, -.6f);
            var tint = new Color(1.25f, -.125f, .1234567f, .8123456f);
            var clip = CenterClip(matrix, 12.5f, 10.25f);
            var highlight = new Color(.2f, .3f, .4f, 0f);
            _renderer.BeginBatch();
            _renderer.DrawQuad(_image, 12.5f, 10.25f, uv, matrix, tint, 2, .3f, .4f, .2f,
                highlight, clip, true);
            matrix.m00 = matrix.m01 = matrix.m03 = 999f;
            var current = _renderer.SubmittedSprites[0];
            Assert.That(current, Is.SameAs(first));
            Assert.That(current.SourceImage, Is.SameAs(_image));
            Assert.That(current.GameObject, Is.Null);
            Assert.That(current.Mode, Is.EqualTo(2));
            Assert.That(current.Grayscale, Is.EqualTo(.3f));
            Assert.That(current.ProducerAdditive, Is.EqualTo(.4f));
            Assert.That(current.Additive, Is.EqualTo(.2f));
            Assert.That(current.AddColor, Is.EqualTo(highlight));
            Assert.That(current.Clip, Is.EqualTo(clip));
            Assert.That(current.ClipEnabled, Is.True);
            AssertDiagnostics(current, 12.5f, 10.25f, uv, expectedMatrix, tint, 0f);
        }

        /// <summary>Keeps all three array identities across source-slot reuse and does not rebuild an already-read pose.</summary>
        [Test]
        public void DiagnosticArraysKeepIdentityAndPrepareEachRecordedPoseOnce()
        {
            var matrix = Matrix4x4.Translate(new Vector3(3f, 4f));
            var uv = new Rect(.1f, .2f, .8f, .7f);
            _renderer.BeginBatch();
            _renderer.DrawQuad(_image, 9f, 7f, uv, matrix, Color.red, 0);
            var source = _renderer.SubmittedSprites[0];
            var vertices = source.Vertices;
            var coordinates = source.TextureCoordinates;
            var colors = source.Colors;
            var snapshot = (Vector3[])vertices.Clone();
            var sentinel = new Vector3(123f, 456f, 789f);
            vertices[0] = sentinel;
            Assert.That(source.TextureCoordinates, Is.SameAs(coordinates));
            Assert.That(source.Colors, Is.SameAs(colors));
            Assert.That(source.Vertices[0], Is.EqualTo(sentinel), "Other diagnostic readers must not overwrite an already prepared borrowed array.");

            matrix.m03 = 22f;
            matrix.m13 = 28f;
            _renderer.BeginBatch();
            _renderer.DrawQuad(_image, 13f, 5f, uv, matrix, Color.green, 0);
            var next = _renderer.SubmittedSprites[0];
            Assert.That(next, Is.SameAs(source));
            Assert.That(next.Colors, Is.SameAs(colors), "Any first diagnostic reader must publish the new pose into the original buffers.");
            Assert.That(next.Vertices, Is.SameAs(vertices));
            Assert.That(next.TextureCoordinates, Is.SameAs(coordinates));
            AssertDiagnostics(next, 13f, 5f, uv, matrix, Color.green, 0f);
            Assert.That(snapshot[0], Is.EqualTo(new Vector3(3f, -4f, 0f)), "An explicit copy must remain a cross-frame snapshot.");
        }

        /// <summary>Retains an unused pooled slot's last source pose even when its first geometry read occurs in a later frame.</summary>
        [Test]
        public void ShrinkingSubmissionCountLeavesUnusedSourceSlotIntact()
        {
            var uv = new Rect(0f, 0f, 1f, 1f);
            var retainedMatrix = Affine(.75f, -.2f, .1f, 1.2f, 14f, 23f);
            _renderer.BeginBatch();
            _renderer.DrawQuad(_image, 8f, 9f, uv, Matrix4x4.identity, Color.red, 0);
            _renderer.DrawQuad(_image, 10f, 12f, uv, retainedMatrix, Color.green, 1);
            var retained = _renderer.SubmittedSprites[1];
            Assert.That(retained.Mode, Is.EqualTo(1));
            _renderer.BeginBatch();
            _renderer.DrawQuad(_image, 3f, 4f, uv, Matrix4x4.identity, Color.blue, 0);
            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(1));
            Assert.That(_renderer.SubmittedSprites.Count, Is.EqualTo(2));
            Assert.That(_renderer.SubmittedSprites[1], Is.SameAs(retained));
            Assert.That(retained.Mode, Is.EqualTo(1));
            AssertDiagnostics(retained, 10f, 12f, uv, retainedMatrix, Color.green, -1f / 32768f);
        }

        /// <summary>Preserves the original full-quad formula rather than substituting clipped mesh vertices.</summary>
        /// <param name="sample">The skew, mirrored-axis, out-of-range UV or fractional-position case.</param>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void OriginalSourceFormulaSurvivesSkewMirroringAndPartialClipping(int sample)
        {
            var matrix = Affine(sample % 2 == 0 ? 1.25f : -1.15f, .27f - sample * .03f,
                -.18f + sample * .02f, .85f + sample * .05f, 43.125f, 28.75f);
            // The original source diagnostic formula deliberately uses only the two 2D affine rows.
            matrix.m20 = 7f;
            matrix.m21 = -4f;
            matrix.m23 = 3f;
            matrix.m30 = .25f;
            matrix.m31 = -.125f;
            matrix.m33 = .5f;
            var width = 12.5f;
            var height = 10.25f;
            var uv = sample < 2 ? new Rect(.1f, .2f, .8f, .7f)
                : sample < 4 ? new Rect(.9f, .85f, -.75f, -.6f) : new Rect(-.2f, .1f, 1.4f, .8f);
            var tint = new Color(1.25f, -.125f, .1234567f, .8123456f);
            var clip = CenterClip(matrix, width, height);
            _renderer.BeginBatch();
            _renderer.DrawQuad(_image, 1f, 1f, new Rect(0f, 0f, 1f, 1f), Matrix4x4.identity, Color.white, 0);
            _renderer.DrawQuad(_image, width, height, uv, matrix, tint, sample % 4, clipOverride: clip);
            Assert.That(_renderer.SubmittedObjectCount, Is.EqualTo(2));
            var source = _renderer.SubmittedSprites[1];
            Assert.That(source.Clip, Is.EqualTo(clip));
            Assert.That(source.ClipEnabled, Is.True);
            AssertDiagnostics(source, width, height, uv, matrix, tint, -1f / 32768f);
            Assert.That(source.Vertices[0].x < clip.xMin || source.Vertices[0].x > clip.xMax
                || -source.Vertices[0].y < clip.yMin || -source.Vertices[0].y > clip.yMax, Is.True,
                "Source diagnostics must retain unclipped logical corners even when the GPU polygon is clipped.");
        }

        /// <summary>Reuses source and diagnostic arrays when a warm reader observes every newly submitted pose.</summary>
        [Test]
        public void WarmRecordAndDiagnosticReadsDoNotAllocate()
        {
            var matrix = Matrix4x4.identity;
            var uv = new Rect(.1f, .2f, .8f, .7f);
            var tint = new Color(.5f, .25f, .75f, .7f);
            var sum = 0f;
            for (var warm = 0; warm < 32; warm++)
            {
                _renderer.BeginBatch();
                _renderer.DrawQuad(_image, 12f, 10f, uv, matrix, tint, 0);
                var source = _renderer.SubmittedSprites[0];
                sum += source.Vertices[0].x + source.TextureCoordinates[2].x + source.Colors[0].r;
            }
            using var allocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 1024,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(allocations.Valid, Is.True);
            allocations.Start();
            for (var frame = 0; frame < 64; frame++)
            {
                matrix.m03 = frame + 4f;
                _renderer.BeginBatch();
                _renderer.DrawQuad(_image, 12f, 10f, uv, matrix, tint, 0);
                var source = _renderer.SubmittedSprites[0];
                sum += source.Vertices[0].x + source.TextureCoordinates[2].x + source.Colors[0].r;
            }
            allocations.Stop();
            Assert.That(allocations.Count, Is.Zero);
            Assert.That(sum, Is.GreaterThan(0f));
        }

        /// <summary>Checks the same ordered float expressions used by the original eager source recorder.</summary>
        /// <param name="source">The retained logical source slot.</param>
        /// <param name="width">The original full logical width.</param>
        /// <param name="height">The original full logical height.</param>
        /// <param name="uv">The requested source coordinates before mapping.</param>
        /// <param name="matrix">The original full input matrix.</param>
        /// <param name="tint">The original unquantized source color.</param>
        /// <param name="depth">The painter-order depth assigned to this source.</param>
        private static void AssertDiagnostics(ObjectSprite source, float width, float height, Rect uv,
            Matrix4x4 matrix, Color tint, float depth)
        {
            var corners = new[] { Vector2.zero, new Vector2(width, 0f), new Vector2(width, height), new Vector2(0f, height) };
            var coordinates = new[] { new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax),
                new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMin, uv.yMin) };
            for (var index = 0; index < 4; index++)
            {
                var point = corners[index];
                var x = (matrix.m00 * point.x) + (matrix.m01 * point.y) + matrix.m03;
                var y = (matrix.m10 * point.x) + (matrix.m11 * point.y) + matrix.m13;
                Assert.That(source.Vertices[index], Is.EqualTo(new Vector3(x, -y, depth)));
                Assert.That(source.TextureCoordinates[index], Is.EqualTo(coordinates[index]));
                Assert.That(source.Colors[index], Is.EqualTo(tint));
            }
        }

        /// <summary>Creates an explicit affine pose without decomposing source rotation or skew.</summary>
        /// <param name="m00">The horizontal contribution from source X.</param>
        /// <param name="m01">The horizontal contribution from source Y.</param>
        /// <param name="m10">The vertical contribution from source X.</param>
        /// <param name="m11">The vertical contribution from source Y.</param>
        /// <param name="x">The horizontal source origin.</param>
        /// <param name="y">The vertical source origin.</param>
        /// <returns>The full matrix containing the requested 2D rows.</returns>
        private static Matrix4x4 Affine(float m00, float m01, float m10, float m11, float x, float y)
        {
            var matrix = Matrix4x4.identity;
            matrix.m00 = m00;
            matrix.m01 = m01;
            matrix.m10 = m10;
            matrix.m11 = m11;
            matrix.m03 = x;
            matrix.m13 = y;
            return matrix;
        }

        /// <summary>Clips around a source quad's transformed midpoint while retaining visible geometry.</summary>
        /// <param name="matrix">The full source-to-destination matrix.</param>
        /// <param name="width">The original logical width.</param>
        /// <param name="height">The original logical height.</param>
        /// <returns>A small axis-aligned crop contained around the affine midpoint.</returns>
        private static Rect CenterClip(Matrix4x4 matrix, float width, float height)
        {
            var x = (matrix.m00 * (width * .5f)) + (matrix.m01 * (height * .5f)) + matrix.m03;
            var y = (matrix.m10 * (width * .5f)) + (matrix.m11 * (height * .5f)) + matrix.m13;
            return new Rect(x - 2f, y - 2f, 4f, 4f);
        }
    }
}
