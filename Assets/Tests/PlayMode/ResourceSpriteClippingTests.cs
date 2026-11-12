#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks particle clipping and atlas rows against literal synthetic source rectangles and scene geometry.</summary>
    public sealed class ResourceSpriteClippingTests
    {
        /// <summary>Owns the uniquely generated synthetic runtime asset directory.</summary>
        private string _root = "";
        /// <summary>Submits synthetic atlas sprites through the production animation renderer.</summary>
        private ReanimationRenderer _renderer = null!;

        /// <summary>Creates a two-column, two-row atlas and an original-format clipped particle definition.</summary>
        /// <returns>A frame boundary allowing the actual render pipeline to initialize.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _root = Path.Combine(Application.temporaryCachePath, "PvZSpriteClipping-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "reanim"));
            Directory.CreateDirectory(Path.Combine(_root, "images"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            Directory.CreateDirectory(Path.Combine(_root, "particles"));
            var atlas = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            try
            {
                for (var y = 0; y < 64; y++)
                {
                    for (var x = 0; x < 64; x++)
                    {
                        atlas.SetPixel(x, y, y >= 32 ? x < 32 ? Color.red : Color.green
                            : x < 32 ? Color.blue : Color.yellow);
                    }
                }

                atlas.Apply();
                File.WriteAllBytes(Path.Combine(_root, "images", "atlas.png"), atlas.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.Destroy(atlas);
            }

            File.WriteAllText(Path.Combine(_root, "properties", "Resources.xml"),
                "<ResourceManifest><Resources id=\"Fixture\"><SetDefaults path=\"images\" idprefix=\"IMAGE_\"/>"
                + "<Image id=\"CLIPATLAS\" path=\"atlas\" cols=\"2\" rows=\"2\"/></Resources></ResourceManifest>");
            File.WriteAllText(Path.Combine(_root, "particles", "Clipped.xml"),
                "<Emitter><Image>IMAGE_CLIPATLAS</Image><ImageRow>1</ImageRow><ImageFrames>1</ImageFrames>"
                + "<SpawnMinActive>1</SpawnMinActive><ParticleDuration>100</ParticleDuration><SystemDuration>100</SystemDuration>"
                + "<ClipTop>0.25</ClipTop><ClipBottom>0.125</ClipBottom></Emitter>");
            _renderer = new ReanimationRenderer(_root);
        }

        /// <summary>Releases production rendering resources and only the unique synthetic fixture directory.</summary>
        /// <returns>A frame boundary allowing deferred Unity texture destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer?.Dispose();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }

            yield return null;
        }

        /// <summary>Checks omitted optional arguments retain the original full first-row quad and UV coordinates.</summary>
        [Test]
        public void DefaultArgumentsPreserveFullAtlasCell()
        {
            BeginFrame();
            Assert.That(_renderer.DrawResourceSprite("IMAGE_CLIPATLAS", new Vector2(40f, 40f), 2f, frame: 1), Is.True);
            _renderer.EndObjectFrame();
            AssertQuad(new Rect(8f, 8f, 64f, 64f), new Rect(.5f, .5f, .5f, .5f));
        }

        /// <summary>Checks independent top and bottom crops change both geometry and UVs while selecting the second atlas row.</summary>
        [Test]
        public void ClipsBothEdgesAndSelectsSecondAtlasRow()
        {
            BeginFrame();
            Assert.That(_renderer.DrawResourceSprite("IMAGE_CLIPATLAS", new Vector2(40f, 40f), 2f, frame: 1,
                clipTop: .25f, clipBottom: .125f, imageRow: 1), Is.True);
            _renderer.EndObjectFrame();
            // The literal 32-pixel cell is scaled to 64 pixels: top removes16, bottom removes8.
            // Row one spans V0..0.5; the independent cropped boundaries are V0.0625..0.375.
            AssertQuad(new Rect(8f, 24f, 64f, 40f), new Rect(.5f, .0625f, .5f, .3125f));
        }

        /// <summary>Checks a fully clipped source contributes no scene geometry.</summary>
        [Test]
        public void CompletelyClippedSpriteSubmitsNoQuad()
        {
            BeginFrame();
            Assert.That(_renderer.DrawResourceSprite("IMAGE_CLIPATLAS", new Vector2(40f, 40f), 1f,
                clipTop: .75f, clipBottom: .25f, imageRow: 1), Is.True);
            _renderer.EndObjectFrame();
            Assert.That(Pool().SubmittedObjectCount, Is.Zero);
        }

        /// <summary>Checks the original particle evaluator forwards its XML row and crop tracks to the actual sprite draw.</summary>
        [Test]
        public void ParticleDefinitionForwardsClippingAndAtlasRow()
        {
            var particles = new OriginalParticleRenderer(_root, _renderer);
            BeginFrame();
            particles.BeginFrame();
            Assert.That(particles.Draw("synthetic-clipped-particle", "Clipped", 0f, new Vector2(40f, 40f), 711), Is.True);
            particles.EndFrame();
            _renderer.EndObjectFrame();
            Assert.That(particles.LastError, Is.Null.Or.Empty);
            AssertQuad(new Rect(24f, 32f, 32f, 20f), new Rect(0f, .0625f, .5f, .3125f));
        }

        /// <summary>Begins one actual production scene submission with an identity pixel transform.</summary>
        private void BeginFrame()
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
        }

        /// <summary>Gets the production scene pool containing the submitted synthetic quads.</summary>
        /// <returns>The actual animation renderer's scene pool.</returns>
        private ObjectSpriteRenderer Pool()
        {
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer)
                .GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_renderer)!;
        }

        /// <summary>Checks the actual scene mesh against independently specified screen bounds and source UVs.</summary>
        /// <param name="bounds">The literal expected top-left screen pixel rectangle.</param>
        /// <param name="uv">The literal expected bottom-left Unity texture rectangle.</param>
        private void AssertQuad(Rect bounds, Rect uv)
        {
            var pool = Pool();
            Assert.That(pool.SubmittedObjectCount, Is.EqualTo(1));
            var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer)
                .GetField("_sprites", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pool)!;
            var sprite = sprites[0];
            var expectedPositions = new[]
            {
                new Vector2(bounds.xMin, bounds.yMin), new Vector2(bounds.xMax, bounds.yMin),
                new Vector2(bounds.xMax, bounds.yMax), new Vector2(bounds.xMin, bounds.yMax)
            };
            var expectedCoordinates = new[]
            {
                new Vector2(uv.xMin, uv.yMax), new Vector2(uv.xMax, uv.yMax),
                new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMin, uv.yMin)
            };
            for (var index = 0; index < 4; index++)
            {
                Assert.That(sprite.Vertices[index].x, Is.EqualTo(expectedPositions[index].x).Within(.0001f));
                Assert.That(-sprite.Vertices[index].y, Is.EqualTo(expectedPositions[index].y).Within(.0001f));
                Assert.That(sprite.TextureCoordinates[index].x, Is.EqualTo(expectedCoordinates[index].x).Within(.0001f));
                Assert.That(sprite.TextureCoordinates[index].y, Is.EqualTo(expectedCoordinates[index].y).Within(.0001f));
            }
        }
    }
}
