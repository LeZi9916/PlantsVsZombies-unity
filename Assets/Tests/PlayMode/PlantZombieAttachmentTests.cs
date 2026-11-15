#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks native 527B50 attachment orientation, the ninety-pixel zombie mirror and original track painter order.</summary>
    public sealed class PlantZombieAttachmentTests
    {
        /// <summary>Owns only this fixture's original-format definitions.</summary>
        private string _root = null!;
        /// <summary>Owns the production renderer under test.</summary>
        private ReanimationRenderer _renderer = null!;

        /// <summary>Creates labeled two-pixel source images and minimal original-format parent and child tracks.</summary>
        /// <returns>A graphics initialization boundary.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _root = Path.Combine(Application.temporaryCachePath, "PvZPlantZombie-" + Guid.NewGuid().ToString("N"));
            var directory = Directory.CreateDirectory(Path.Combine(_root, "reanim")).FullName;
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                image.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                image.Apply();
                foreach (var name in new[] { "body", "head", "front" })
                {
                    File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
            var markers = "<fps>12</fps><track><name>anim_walk</name><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_eat</name><t><f>-1</f></t><t/><t><f>0</f></t><t/></track>";
            File.WriteAllText(Path.Combine(directory, "Zombie.reanim"), markers
                + Track("Zombie_body", "body", false) + Track("anim_head1", "body", true) + Track("Zombie_outerarm_hand", "front", false));
            var child = "<fps>12</fps><track><name>anim_idle</name><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_head_idle</name><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_shooting</name><t><f>-1</f></t><t/><t><f>0</f></t><t/></track>"
                + "<track><name>picture</name><t><i>IMAGE_REANIM_head</i><x>0</x><f>0</f></t><t/><t><x>20</x></t><t/></track>";
            foreach (var name in new[] { "PeaShooterSingle", "Wallnut", "Jalapeno", "GatlingPea", "Squash", "Tallnut" })
            {
                File.WriteAllText(Path.Combine(directory, name + ".reanim"), child);
            }
            _renderer = new ReanimationRenderer(_root);
        }

        /// <summary>Releases owned source definitions and renderer resources.</summary>
        /// <returns>A deferred destruction boundary.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer.Dispose();
            Directory.Delete(_root, true);
            yield return null;
        }

        /// <summary>Checks literal matrix channels produced by executing the original PC 527B50 instructions.</summary>
        /// <param name="kind">The original plant-headed species.</param>
        /// <param name="x">The original attachment x offset.</param>
        /// <param name="y">The original attachment y offset.</param>
        /// <param name="size">The original child scale magnitude.</param>
        [TestCase(26, 65, -5, 1f)]
        [TestCase(27, 50, 0, .8f)]
        [TestCase(28, 55, -5, 1f)]
        [TestCase(29, 65, -5, 1f)]
        [TestCase(30, 55, -15, .75f)]
        [TestCase(31, 37, 0, .8f)]
        public void NativeOffsetMatrixAndMirrorMatchPcInstructions(int kind, float x, float y, float size)
        {
            var ordinary = DrawHead(kind, false);
            var mirrored = DrawHead(kind, true);
            // PC 527B50 returns [-cos(.2),sin(.2);sin(.2),cos(.2)] for scale (-1,1).
            var edge = ordinary[1] - ordinary[0];
            Assert.That(edge.x, Is.EqualTo(-.980066597f * size * 2).Within(.00001f));
            Assert.That(-edge.y, Is.EqualTo(.198669329f * size * 2).Within(.00001f));
            Assert.That(ordinary[0].x, Is.EqualTo(100 + x).Within(.00001f));
            Assert.That(-ordinary[0].y, Is.EqualTo(100 + y).Within(.00001f));
            Assert.That(mirrored[0].x + ordinary[0].x, Is.EqualTo(290).Within(.00001f));
            Assert.That(mirrored[0].y, Is.EqualTo(ordinary[0].y).Within(.00001f));
        }

        /// <summary>Checks the hidden attachment follows the displayed body blend and precedes the original foreground arm.</summary>
        [Test]
        public void HeadUsesTheSameBlendedParentAndOriginalPainterPosition()
        {
            var zombie = new Zombie { Kind = ZombieKind.Normal, ModeHead = 26, Health = 270, StartingHealth = 270, PresentationBodyBlendTicks = 20 };
            var actor = new AdventureActorRenderer(_renderer);
            DrawActor(actor, zombie, "anim_walk", .05f);
            var before = Head().Vertices[0];
            DrawActor(actor, zombie, "anim_eat", 0);
            var after = Head().Vertices[0];
            Assert.That(Vector3.Distance(before, after), Is.LessThan(.0001f));
            var sprites = Pool.SubmittedSprites.Take(Pool.SubmittedObjectCount).ToArray();
            var names = sprites.Select(sprite => Path.GetFileName(sprite.SourceImage!.SourceIdentity)).ToArray();
            Assert.That(Array.FindIndex(names, name => name == "head.png"), Is.GreaterThan(Array.FindIndex(names, name => name == "body.png")));
            Assert.That(Array.FindIndex(names, name => name == "head.png"), Is.LessThan(Array.FindIndex(names, name => name == "front.png")));
            Assert.That(SpriteDrawState.Color, Is.EqualTo(Color.white));
            Assert.That(SpriteDrawState.Matrix, Is.EqualTo(Matrix4x4.identity));
        }

        /// <summary>Checks the independently controlled original shooting clip is rendered rather than a perpetual idle head.</summary>
        [Test]
        public void ActiveChildClipAndOriginalOnceAndHoldTimeReachRendering()
        {
            var zombie = new Zombie { Kind = ZombieKind.Normal, ModeHead = 26, Health = 270, StartingHealth = 270,
                PlantZombieState = new AdventurePlantZombieState(26) };
            var actor = new AdventureActorRenderer(_renderer);
            DrawActor(actor, zombie, "anim_walk", 0);
            var idle = Head().Vertices[0];
            zombie.PlantZombieState.Play("anim_shooting", 35, false, 0);
            zombie.PlantZombieState.HeadSeconds = 10;
            DrawActor(actor, zombie, "anim_walk", 0);
            var shot = Head().Vertices[0];
            Assert.That(shot.x - idle.x, Is.EqualTo(-.980066597f * 20).Within(.0001f));
            Assert.That(-shot.y + idle.y, Is.EqualTo(.198669329f * 20).Within(.0001f));
        }

        /// <summary>Gets the renderer's actual submitted scene source commands.</summary>
        private ObjectSpriteRenderer Pool => (ObjectSpriteRenderer)typeof(ReanimationRenderer)
            .GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_renderer)!;

        /// <summary>Finds the attached child's submitted original source quad.</summary>
        /// <returns>The untrimmed logical source command.</returns>
        private ObjectSprite Head()
        {
            return Pool.SubmittedSprites.Take(Pool.SubmittedObjectCount)
                .Single(sprite => sprite.SourceImage!.SourceIdentity.EndsWith("head.png", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Submits one unscaled native attachment at the original frame-zero base pose.</summary>
        /// <param name="kind">The original species.</param>
        /// <param name="mirror">Whether the complete zombie is mind-controlled.</param>
        /// <returns>The source vertices copied before the next frame.</returns>
        private Vector3[] DrawHead(int kind, bool mirror)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            SpriteDrawState.Matrix = Matrix4x4.identity;
            SpriteDrawState.Color = Color.white;
            Assert.That(_renderer.DrawNativePlantHeadAttachment(new Vector2(100, 100), 1, "anim_walk", 0, 12, kind, 0, mirror), Is.True, _renderer.LastError);
            _renderer.EndObjectFrame();
            return (Vector3[])Head().Vertices.Clone();
        }

        /// <summary>Submits the same production actor across an original body clip transition.</summary>
        /// <param name="actor">The production body and attachment presenter.</param>
        /// <param name="zombie">The unchanged zombie state.</param>
        /// <param name="clip">The active native body clip.</param>
        /// <param name="seconds">The current body clip age.</param>
        private void DrawActor(AdventureActorRenderer actor, Zombie zombie, string clip, float seconds)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            SpriteDrawState.Matrix = Matrix4x4.identity;
            SpriteDrawState.Color = Color.white;
            actor.DrawZombie(zombie, "Zombie", clip, new Vector2(100, 100), 1, seconds, 12, true, "plant-zombie-body", "plant-zombie-surface");
            _renderer.EndObjectFrame();
        }

        /// <summary>Creates one labeled native parent track with a distinct eating pose.</summary>
        /// <param name="name">The original parent track name.</param>
        /// <param name="image">The labeled test image.</param>
        /// <param name="moving">Whether the hidden head bone changes between clips.</param>
        /// <returns>The original-format source track.</returns>
        private static string Track(string name, string image, bool moving)
        {
            return "<track><name>" + name + "</name><t><i>IMAGE_REANIM_" + image + "</i><f>0</f><x>0</x></t>"
                + (moving ? "<t><x>20</x></t><t><x>100</x></t><t/>" : "<t/><t/><t/>") + "</track>";
        }
    }
}
