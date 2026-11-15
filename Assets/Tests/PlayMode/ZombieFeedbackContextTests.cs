#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Protects per-draw zombie feedback reuse without sharing opacity or nested actor state.</summary>
    public sealed class ZombieFeedbackContextTests
    {
        /// <summary>The original-format source tree owned only by this fixture.</summary>
        private string _root = string.Empty;
        /// <summary>The production renderer consuming the fixture source definitions.</summary>
        private ReanimationRenderer _renderer = null!;

        /// <summary>Creates independent source identities with different native track opacities.</summary>
        /// <returns>A frame boundary allowing the graphics device to initialize.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _root = Path.Combine(Application.temporaryCachePath, "PvZFeedbackContext-" + Guid.NewGuid().ToString("N"));
            var folder = Directory.CreateDirectory(Path.Combine(_root, "reanim")).FullName;
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            try
            {
                var colors = new Color[64];
                Array.Fill(colors, Color.white);
                texture.SetPixels(colors);
                texture.Apply();
                var bytes = texture.EncodeToPNG();
                foreach (var name in new[] { "body1", "body2", "shield", "water" })
                {
                    File.WriteAllBytes(Path.Combine(folder, name + ".png"), bytes);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            File.WriteAllText(Path.Combine(folder, "Zombie.reanim"),
                "<fps>12</fps><track><name>anim_walk</name><t><f>0</f></t><t/></track>"
                + Track("Zombie_body1", "body1", 0, ".25")
                + Track("Zombie_body2", "body2", 12, ".75")
                + Track("anim_screendoor", "shield", 24, ".5"));
            File.WriteAllText(Path.Combine(folder, "Zombie_dolphinrider.reanim"),
                "<fps>12</fps><track><name>anim_walk</name><t><f>0</f></t><t/></track>"
                + Track("Zombie_body1", "body1", 0, ".25")
                + Track("Zombie_dolphinrider_whitewater", "water", 12, ".75"));
            _renderer = new ReanimationRenderer(_root);
        }

        /// <summary>Releases the fixture renderer and source directory.</summary>
        /// <returns>A boundary for deferred Unity resource destruction.</returns>
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

        /// <summary>Retains independently sampled track alpha across shared body and shield feedback.</summary>
        /// <param name="state">The normal, chilled, hypnotized, buttered-frozen, or black-corpse feedback state.</param>
        /// <param name="captured">Whether a nonwhite inherited alpha requires native offscreen composition.</param>
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        public void BodyAndShieldFeedbackKeepIndependentTrackOpacity(int state, bool captured)
        {
            var zombie = new Zombie
            {
                Kind = ZombieKind.ScreenDoor, Health = 270f, StartingHealth = 270f,
                ShieldHealth = 1100f, PresentationMaxShield = 1100f,
                PresentationFlash = .05f, PresentationArmorFlash = .07f,
                ChillRemaining = state == 1 ? 1f : 0f,
                Hypnotized = state == 2,
                FrozenRemaining = state == 3 ? 1f : 0f,
                PresentationButter = state == 3 ? 1f : 0f
            };
            var tint = state == 4 ? new Color(0f, 0f, 0f, .6f)
                : captured ? new Color(1f, 1f, 1f, .6f) : Color.white;
            Begin(tint);
            Assert.That(_renderer.DrawNativeZombieCombat(zombie, new Rect(0f, 0f, 80f, 60f),
                () => _renderer.DrawZombiePlayback(new Vector2(4f, 4f), 1f, 0f, ZombieVariant.ScreenDoor,
                    "anim_walk", true, 12f), "feedback"), Is.True);
            _renderer.EndObjectFrame();
            var pool = Pool(captured);
            var expected = state == 1 ? new Color(75f / 255f, 75f / 255f, 1f)
                : state == 2 ? new Color(128f / 255f, 64f / 255f, 192f / 255f) : Color.white;
            var body = Find(pool, "body1");
            AssertColor(body.Colors[0], new Color(expected.r * tint.r, expected.g * tint.g,
                expected.b * tint.b, .25f * tint.a));
            Assert.That(Find(pool, "body2").Colors[0].a, Is.EqualTo(.75f * tint.a).Within(.00001f));
            var shield = Find(pool, "shield");
            AssertColor(shield.Colors[0], new Color(tint.r, tint.g, tint.b, .5f * tint.a));
            var bodyAdd = state == 4 ? Color.clear : state == 1 ? new Color(125f / 255f, 125f / 255f, 1f)
                : state == 2 ? new Color(178f / 255f, 114f / 255f, 242f / 255f) : new Color(50f / 255f, 50f / 255f, 50f / 255f);
            Assert.That(body.AddColor.r, Is.EqualTo(bodyAdd.r).Within(.00001f));
            Assert.That(body.AddColor.g, Is.EqualTo(bodyAdd.g).Within(.00001f));
            Assert.That(body.AddColor.b, Is.EqualTo(bodyAdd.b).Within(.00001f));
            Assert.That(shield.AddColor.r, Is.EqualTo(state == 4 ? 0f : 70f / 255f).Within(.00001f));
        }

        /// <summary>Restores outer actor feedback after a nested synchronous draw changes body tint.</summary>
        [Test]
        public void NestedCombatDrawRestoresOuterFeedback()
        {
            var outer = new Zombie { Kind = ZombieKind.Normal, Health = 270f, StartingHealth = 270f, ChillRemaining = 1f };
            var inner = new Zombie { Kind = ZombieKind.Normal, Health = 270f, StartingHealth = 270f, Hypnotized = true };
            Begin(Color.white);
            Assert.That(_renderer.DrawNativeZombieCombat(outer, new Rect(0f, 0f, 120f, 60f), () =>
            {
                _renderer.DrawZombiePlayback(new Vector2(4f, 4f), 1f, 0f, ZombieVariant.Normal, "anim_walk", true, 12f);
                _renderer.DrawNativeZombieCombat(inner, new Rect(0f, 0f, 120f, 60f),
                    () => _renderer.DrawZombiePlayback(new Vector2(44f, 4f), 1f, 0f, ZombieVariant.Normal, "anim_walk", true, 12f));
                return _renderer.DrawZombiePlayback(new Vector2(84f, 4f), 1f, 0f, ZombieVariant.Normal, "anim_walk", true, 12f);
            }), Is.True);
            _renderer.EndObjectFrame();
            var sprites = Pool(false).SubmittedSprites;
            Assert.That(Pool(false).SubmittedObjectCount, Is.EqualTo(6));
            AssertColor(sprites[0].Colors[0], new Color(75f / 255f, 75f / 255f, 1f, .25f));
            AssertColor(sprites[2].Colors[0], new Color(128f / 255f, 64f / 255f, 192f / 255f, .25f));
            AssertColor(sprites[4].Colors[0], sprites[0].Colors[0]);
            Assert.That(sprites[4].AddColor, Is.EqualTo(sprites[0].AddColor));
        }

        /// <summary>Leaves original dolphin water tracks outside body feedback while preserving source opacity.</summary>
        [Test]
        public void DolphinWaterTracksRetainOriginalColorOverrideExclusions()
        {
            var zombie = new Zombie { Kind = ZombieKind.DolphinRider, Health = 270f, StartingHealth = 270f, ChillRemaining = 1f, PresentationFlash = .05f };
            Begin(Color.white);
            Assert.That(_renderer.DrawNativeZombieCombat(zombie, new Rect(0f, 0f, 80f, 60f),
                () => _renderer.DrawNativePlayback("Zombie_dolphinrider", new Vector2(4f, 4f), 1f,
                    0f, "anim_walk", true, 12f)), Is.True);
            _renderer.EndObjectFrame();
            AssertColor(Find(Pool(false), "water").Colors[0], new Color(1f, 1f, 1f, .75f));
            Assert.That(Find(Pool(false), "water").AddColor, Is.EqualTo(Color.clear));
            Assert.That(Find(Pool(false), "body1").Colors[0].r, Is.EqualTo(75f / 255f).Within(.00001f));
        }

        /// <summary>Begins one complete native presentation with a caller-supplied tint.</summary>
        /// <param name="tint">The inherited final actor tint and opacity.</param>
        private void Begin(Color tint)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            SpriteDrawState.Color = tint;
        }

        /// <summary>Gets the main or offscreen pool retaining original source commands.</summary>
        /// <param name="captured">Whether the tested actor used offscreen composition.</param>
        /// <returns>The pool whose source colors are passed to the production shaders.</returns>
        private ObjectSpriteRenderer Pool(bool captured)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            if (captured)
            {
                return ((List<ObjectSpriteRenderer>)typeof(ReanimationRenderer).GetField("_nativeCaptureRenderers", flags)!.GetValue(_renderer)!)[0];
            }
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(_renderer)!;
        }

        /// <summary>Finds a source command by its independent image identity.</summary>
        /// <param name="pool">The source command pool.</param>
        /// <param name="name">The fixture image basename.</param>
        /// <returns>The first matching visible source command.</returns>
        private static ObjectSprite Find(ObjectSpriteRenderer pool, string name)
        {
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = pool.SubmittedSprites[index];
                if (sprite.SourceImage!.SourceIdentity.EndsWith("/" + name + ".png", StringComparison.Ordinal))
                {
                    return sprite;
                }
            }
            throw new InvalidOperationException("The fixture source was not submitted: " + name);
        }

        /// <summary>Checks exact source-channel values before GPU blending and quantization.</summary>
        /// <param name="actual">The submitted source color.</param>
        /// <param name="expected">The expected native feedback color.</param>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.00001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.00001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.00001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.00001f));
        }

        /// <summary>Builds one original-format track with a source-specific opacity.</summary>
        /// <param name="name">The original track role.</param>
        /// <param name="image">The independent fixture source identity.</param>
        /// <param name="x">The native horizontal offset.</param>
        /// <param name="alpha">The native source opacity.</param>
        /// <returns>The two-frame authored track fragment.</returns>
        private static string Track(string name, string image, int x, string alpha)
        {
            return "<track><name>" + name + "</name><t><i>IMAGE_REANIM_" + image + "</i><x>" + x
                + "</x><f>0</f><a>" + alpha + "</a></t><t/></track>";
        }
    }
}
