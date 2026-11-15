#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares cached fixed poses with the original sampler without changing animation or composition semantics.</summary>
    public sealed class NativeThumbnailCacheTests
    {
        /// <summary>The isolated original-format resource directory.</summary>
        private string _root = "";
        /// <summary>The production animation sampler and cache under test.</summary>
        private ReanimationRenderer _renderer = null!;
        /// <summary>The target receiving sampled source pixels.</summary>
        private RenderTexture _target = null!;
        /// <summary>The reusable readback texture.</summary>
        private Texture2D _pixels = null!;

        /// <summary>Creates overlapping translucent tracks and an independently attached plant head.</summary>
        /// <returns>A boundary for graphics initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _root = Path.Combine(Application.temporaryCachePath, "PvZThumbnail-" + Guid.NewGuid().ToString("N"));
            var folder = Directory.CreateDirectory(Path.Combine(_root, "reanim")).FullName;
            WriteImage(folder, "red", new Color(1f, 0f, 0f, .65f));
            WriteImage(folder, "green", new Color(0f, 1f, 0f, .7f));
            var body = "<fps>12</fps><track><name>anim_idle</name><t><f>0</f></t><t/></track>"
                + "<track><name>body</name><t><i>IMAGE_REANIM_red</i><x>2</x><f>0</f><a>0.8</a></t><t/></track>"
                + "<track><name>leaf</name><t><i>IMAGE_REANIM_green</i><x>7</x><y>3</y><f>0</f></t><t/></track>";
            for (var index = 0; index < 70; index++)
            {
                File.WriteAllText(Path.Combine(folder, "Fixture" + index + ".reanim"), body);
            }
            File.WriteAllText(Path.Combine(folder, "PeaShooter.reanim"),
                "<fps>12</fps><track><name>anim_idle</name><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_head_idle</name><t><f>-1</f></t><t/><t><f>0</f></t><t/></track>"
                + "<track><name>anim_stem</name><t><x>3</x><f>0</f></t><t/><t/><t/></track>"
                + "<track><name>body</name><t><i>IMAGE_REANIM_red</i><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>head</name><t><i>IMAGE_REANIM_green</i><x>9</x><f>-1</f></t><t/><t><f>0</f></t><t/></track>");
            _renderer = new ReanimationRenderer(_root);
            _target = new RenderTexture(128, 96, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(128, 96, TextureFormat.RGBA32, false);
        }

        /// <summary>Releases only this fixture's resources.</summary>
        /// <returns>A boundary for deferred Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer.Dispose();
            _target.Release();
            UnityEngine.Object.Destroy(_target);
            UnityEngine.Object.Destroy(_pixels);
            Directory.Delete(_root, true);
            yield return null;
        }

        /// <summary>Preserves fixed-pose pixels after warmed placement, scale, alpha, clip, and capture changes.</summary>
        /// <param name="name">The ordinary or attached-head resource.</param>
        /// <param name="captured">Whether grayscale offscreen composition surrounds the thumbnail.</param>
        [TestCase("Fixture0", false)]
        [TestCase("PeaShooter", false)]
        [TestCase("Fixture0", true)]
        [TestCase("PeaShooter", true)]
        public void CachedPoseMatchesOriginalSampling(string name, bool captured)
        {
            Draw(name, true, false, Vector2.zero, 1f);
            var builds = _renderer.ThumbnailPoseBuildCount;
            for (var variant = 0; variant < 3; variant++)
            {
                var origin = new Vector2(14.25f + variant * 7f, 17.5f);
                var scale = .75f + variant * .5f;
                Draw(name, false, captured, origin, scale);
                var expected = Capture();
                Draw(name, true, captured, origin, scale);
                var actual = Capture();
                var maximum = 0;
                for (var index = 0; index < expected.Length; index++)
                {
                    maximum = Math.Max(maximum, Math.Abs(expected[index].r - actual[index].r));
                    maximum = Math.Max(maximum, Math.Abs(expected[index].g - actual[index].g));
                    maximum = Math.Max(maximum, Math.Abs(expected[index].a - actual[index].a));
                }
                Assert.That(maximum, Is.LessThanOrEqualTo(2), "Cached tracks must retain original alpha, clipping and order.");
            }
            Assert.That(_renderer.ThumbnailPoseBuildCount, Is.EqualTo(builds));
        }

        /// <summary>Bounds arbitrary resource keys and frees every cached pose at renderer disposal.</summary>
        [Test]
        public void PoseCacheIsBoundedAndDisposed()
        {
            for (var index = 0; index < 70; index++)
            {
                Draw("Fixture" + index, true, false, Vector2.zero, 1f);
                Assert.That(_renderer.ThumbnailPoseCount, Is.LessThanOrEqualTo(64));
            }
            _renderer.Dispose();
            Assert.That(_renderer.ThumbnailPoseCount, Is.Zero);
        }

        /// <summary>Checks repeated warm thumbnail submission with Unity's current-thread allocation recorder.</summary>
        [Test]
        public void WarmPoseReplayDoesNotAllocate()
        {
            for (var warm = 0; warm < 32; warm++)
            {
                Draw("PeaShooter", true, false, Vector2.zero, 1f);
            }
            using var allocations = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 1024,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            Assert.That(allocations.Valid, Is.True);
            allocations.Start();
            for (var frame = 0; frame < 64; frame++)
            {
                ObjectSpriteRenderer.BeginSceneFrame();
                _renderer.BeginObjectFrame();
                _renderer.DrawNativeThumbnail("PeaShooter", new Vector2(12f, 8f), .8f);
                _renderer.EndObjectFrame();
            }
            allocations.Stop();
            Assert.That(allocations.Count, Is.Zero);
        }

        /// <summary>Submits one original or cached pose with identical inherited state.</summary>
        /// <param name="name">The native resource.</param>
        /// <param name="cached">Whether to replay the fixed-pose cache.</param>
        /// <param name="captured">Whether to wrap grayscale composition around the pose.</param>
        /// <param name="origin">The logical pose origin.</param>
        /// <param name="scale">The native pose scale.</param>
        private void Draw(string name, bool cached, bool captured, Vector2 origin, float scale)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            SpriteDrawState.Matrix = Matrix4x4.Translate(new Vector3(2f, 3f));
            SpriteDrawState.Color = new Color(.8f, .7f, 1f, .8f);
            SpriteDrawState.Clip = new Rect(4f, 4f, 55f, 50f);
            var matrix = SpriteDrawState.Matrix;
            var color = SpriteDrawState.Color;
            bool Submit()
            {
                return cached ? _renderer.DrawNativeThumbnail(name, origin, scale) : _renderer.DrawNative(name, origin, scale, 0f);
            }
            Assert.That(captured ? _renderer.DrawNativeClipped("thumbnail", new Rect(0f, 0f, 80f, 70f), Submit,
                grayscale: true, topCrop: 8f) : Submit(), Is.True);
            Assert.That(SpriteDrawState.Matrix, Is.EqualTo(matrix));
            Assert.That(SpriteDrawState.Color, Is.EqualTo(color));
            _renderer.EndObjectFrame();
        }

        /// <summary>Captures the completed main renderer through the real graphics device.</summary>
        /// <returns>The topologically identical readback pixel array.</returns>
        private Color32[] Capture()
        {
            var field = typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!;
            ((ObjectSpriteRenderer)field.GetValue(_renderer)!).RenderTo(_target);
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, 128f, 96f), 0, 0);
                _pixels.Apply();
                return _pixels.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Writes one source image without retaining its temporary Unity texture.</summary>
        /// <param name="folder">The isolated native resource directory.</param>
        /// <param name="name">The original image name.</param>
        /// <param name="color">The translucent source pixel color.</param>
        private static void WriteImage(string folder, string name, Color color)
        {
            var image = new Texture2D(12, 10, TextureFormat.RGBA32, false);
            var pixels = new Color[120];
            Array.Fill(pixels, color);
            image.SetPixels(pixels);
            image.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
