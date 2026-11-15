#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Protects combat-body ownership and ordinary-zombie equipment fast-path equivalence.</summary>
    public sealed class ReanimationCombatAssociationTests
    {
        /// <summary>The original-format source tree owned only by this fixture.</summary>
        private string _root = string.Empty;
        /// <summary>The production sampler whose per-draw source association is under test.</summary>
        private ReanimationRenderer _renderer = null!;

        /// <summary>Creates bodies and unrelated attachments that deliberately reuse body-only track names.</summary>
        /// <returns>A frame boundary initializing the graphics device.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
            _root = Path.Combine(Application.temporaryCachePath, "PvZCombatOwner-" + Guid.NewGuid().ToString("N"));
            var folder = Directory.CreateDirectory(Path.Combine(_root, "reanim")).FullName;
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                image.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                image.Apply();
                var bytes = image.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(folder, "source.png"), bytes);
                foreach (var name in new[] { "wallnut_cracked1", "wallnut_cracked2", "tallnut_cracked1", "tallnut_cracked2",
                    "zombie_outerarm_upper2", "zombie_flag3" })
                {
                    File.WriteAllBytes(Path.Combine(folder, name + ".png"), bytes);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
            var marker = "<fps>12</fps><track><name>anim_walk</name><t><f>0</f></t><t/></track>";
            File.WriteAllText(Path.Combine(folder, "zOmBiE.reanim"), marker + Track("body", 0f) + Track("anim_head", 4f));
            File.WriteAllText(Path.Combine(folder, "Attachment.reanim"), marker + Track("anim_head", 0f));
            File.WriteAllText(Path.Combine(folder, "Zombie_dolphinrider.reanim"), marker + Track("Zombie_dolphinrider_whitewater", 0f));
            File.WriteAllText(Path.Combine(folder, "WaterAttachment.reanim"), marker + Track("Zombie_dolphinrider_whitewater", 0f));
            File.WriteAllText(Path.Combine(folder, "Wallnut.reanim"), marker + Track("anim_face", 0f));
            File.WriteAllText(Path.Combine(folder, "Tallnut.reanim"), marker + Track("anim_idle", 0f));
            _renderer = new ReanimationRenderer(_root);
        }

        /// <summary>Releases fixture-owned renderer resources and original source files.</summary>
        /// <returns>A boundary for deferred Unity destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _renderer.Dispose();
            Directory.Delete(_root, true);
            yield return null;
        }

        /// <summary>Applies live head visibility to the case-insensitive body only, not an unrelated same-named attachment track.</summary>
        /// <param name="headless">Whether the body's live source head should be suppressed.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void HeadVisibilityBelongsToBodyAndDoesNotLeakToAttachment(bool headless)
        {
            var zombie = new Zombie { Kind = ZombieKind.Normal, Health = 270f, StartingHealth = 270f, Headless = headless };
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            Assert.That(_renderer.DrawNativeZombieCombat(zombie, new Rect(0f, 0f, 64f, 64f), () =>
            {
                var body = _renderer.DrawNative("ZOMBIE", new Vector2(4f, 4f), 1f, 0f, "anim_walk");
                var attachment = _renderer.DrawNative("Attachment", new Vector2(24f, 4f), 1f, 0f, "anim_walk");
                return body && attachment;
            }), Is.True);
            _renderer.EndObjectFrame();
            var sprites = Pool().SubmittedSprites;
            Assert.That(sprites.Count, Is.EqualTo(headless ? 2 : 3));
            Assert.That(sprites[sprites.Count - 1].Vertices[0].x, Is.EqualTo(24f), "The attachment's head track must remain visible.");
        }

        /// <summary>Excludes the dolphin body's water track from feedback while preserving the same-named attachment's inherited tint.</summary>
        [Test]
        public void DolphinWaterExclusionDoesNotLeakToAnotherAsset()
        {
            var zombie = new Zombie { Kind = ZombieKind.DolphinRider, Health = 500f, StartingHealth = 500f, ChillRemaining = 1f };
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            Assert.That(_renderer.DrawNativeZombieCombat(zombie, new Rect(0f, 0f, 64f, 64f), () =>
            {
                var body = _renderer.DrawNative("Zombie_dolphinrider", new Vector2(4f, 4f), 1f, 0f, "anim_walk");
                var attachment = _renderer.DrawNative("WaterAttachment", new Vector2(24f, 4f), 1f, 0f, "anim_walk");
                return body && attachment;
            }), Is.True);
            _renderer.EndObjectFrame();
            var sprites = Pool().SubmittedSprites;
            Assert.That(sprites.Count, Is.EqualTo(2));
            Assert.That(sprites[0].Colors[0], Is.EqualTo(Color.white));
            Assert.That(sprites[0].AddColor, Is.EqualTo(Color.clear));
            Assert.That(sprites[1].Colors[0].r, Is.EqualTo(75f / 255f).Within(.00001f));
            Assert.That(sprites[1].Colors[0].b, Is.EqualTo(1f));
            Assert.That(sprites[1].AddColor.r, Is.EqualTo(75f / 255f).Within(.00001f));
        }

        /// <summary>Compares ordinary-zombie visibility and image selection with the unchanged pure rules across live mode-head and damage combinations.</summary>
        [Test]
        public void OrdinaryZombieFastPathsMatchOriginalRulesAcrossLiveStateCombinations()
        {
            var tracks = new[] { "body", "anim_head", "anim_hair", "Zombie_outerarm_upper", "Zombie_outerarm_lower",
                "Zombie_outerarm_hand", "anim_cone", "anim_bucket", "anim_screendoor", "anim_face", "anim_idle", "Zombie_flag", "Zombie_flaghand" };
            var definition = "<fps>12</fps><track><name>anim_walk</name><t><f>0</f></t><t/></track>";
            for (var index = 0; index < tracks.Length; index++)
            {
                definition += Track(tracks[index], index * 4f);
            }
            // Replace before this fixture's renderer has loaded or indexed any animation.
            File.WriteAllText(Path.Combine(_root, "reanim", "zOmBiE.reanim"), definition);
            var samples = new[]
            {
                (Health: 270f, Armor: 0f, Shield: 0f),
                (Health: 30f, Armor: 75f, Shield: 30f),
                (Health: 150f, Armor: 150f, Shield: 400f),
                (Health: 400f, Armor: 300f, Shield: 1100f),
                (Health: -1f, Armor: 1f, Shield: 1f)
            };
            foreach (var modeHead in new[] { -1, 0, 26, 27, 29, 30, 31, 32, 33, 255 })
            {
                foreach (var sample in samples)
                {
                    for (var state = 0; state < 4; state++)
                    {
                        var zombie = new Zombie
                        {
                            Kind = ZombieKind.Normal, ModeHead = modeHead, Health = sample.Health, StartingHealth = 270f,
                            ArmorHealth = sample.Armor, PresentationMaxArmor = 300f,
                            ShieldHealth = sample.Shield, PresentationMaxShield = 1100f,
                            Headless = (state & 1) != 0, PresentationArmLost = (state & 2) != 0
                        };
                        var expected = new List<string>();
                        var effectiveKind = modeHead >= 26 ? modeHead : 0;
                        foreach (var track in tracks)
                        {
                            var visible = AdventureZombieEquipmentRules.TrackVisible(zombie, track)
                                && (!zombie.PresentationArmLost || AdventureZombieFeedbackRules.ArmTrackVisible(effectiveKind, track))
                                && (!zombie.Headless || AdventureZombiePartsRules.HeadTrackVisible(effectiveKind, track));
                            if (visible)
                            {
                                expected.Add(OriginalImageIdentity(zombie, "zOmBiE", track));
                            }
                        }
                        expected.Add(OriginalImageIdentity(zombie, "Wallnut", "anim_face"));
                        expected.Add(OriginalImageIdentity(zombie, "Tallnut", "anim_idle"));
                        ObjectSpriteRenderer.BeginSceneFrame();
                        _renderer.BeginObjectFrame();
                        Assert.That(_renderer.DrawNativeZombieCombat(zombie, new Rect(0f, 0f, 128f, 64f), () =>
                        {
                            var drawn = _renderer.DrawNative("ZOMBIE", new Vector2(4f, 4f), 1f, 0f, "anim_walk", IncludeNamedTrack);
                            drawn |= _renderer.DrawNative("Wallnut", new Vector2(64f, 4f), 1f, 0f, "anim_walk");
                            drawn |= _renderer.DrawNative("Tallnut", new Vector2(96f, 4f), 1f, 0f, "anim_walk");
                            return drawn;
                        }), Is.True);
                        _renderer.EndObjectFrame();
                        var pool = Pool();
                        var sprites = pool.SubmittedSprites;
                        Assert.That(pool.SubmittedObjectCount, Is.EqualTo(expected.Count),
                            "Body visibility must match the original rules for mode head " + modeHead + ", state " + state);
                        for (var index = 0; index < expected.Count; index++)
                        {
                            Assert.That(sprites[index].SourceImage!.SourceIdentity, Is.EqualTo(expected[index]).IgnoreCase,
                                "Image selection must match original rules for mode head " + modeHead + ", state " + state);
                        }
                    }
                }
            }
        }

        /// <summary>Retains the two special plant-head helm substitutions even when the zombie kind is Normal.</summary>
        /// <param name="modeHead">The native plant-head mode selecting the helm.</param>
        /// <param name="asset">The attached plant-head asset.</param>
        /// <param name="armor">The remaining helm health selecting the original crack stage.</param>
        /// <param name="expected">The exact fixture source identity for the selected damage image.</param>
        [TestCase(27, "Wallnut", 150f, "reanim/wallnut_cracked1.png")]
        [TestCase(27, "Wallnut", 75f, "reanim/wallnut_cracked2.png")]
        [TestCase(31, "Tallnut", 150f, "reanim/tallnut_cracked1.png")]
        [TestCase(31, "Tallnut", 75f, "reanim/tallnut_cracked2.png")]
        public void NormalKindPlantHeadHelmsKeepTheirCrackedArtwork(int modeHead, string asset, float armor, string expected)
        {
            var zombie = new Zombie
            {
                Kind = ZombieKind.Normal, ModeHead = modeHead, Health = 270f, StartingHealth = 270f,
                ArmorHealth = armor, PresentationMaxArmor = 300f
            };
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            Assert.That(_renderer.DrawNativeZombieCombat(zombie, new Rect(0f, 0f, 64f, 64f),
                () => _renderer.DrawNative(asset, new Vector2(4f, 4f), 1f, 0f, "anim_walk")), Is.True);
            _renderer.EndObjectFrame();
            Assert.That(Pool().SubmittedSprites.Count, Is.EqualTo(1));
            Assert.That(Pool().SubmittedSprites[0].SourceImage!.SourceIdentity, Is.EqualTo(expected).IgnoreCase);
        }

        /// <summary>Evaluates the original equipment-first, then missing-arm image precedence independently of the renderer fast path.</summary>
        /// <param name="zombie">The live synthetic state evaluated by the original pure rules.</param>
        /// <param name="asset">The body or child asset being drawn.</param>
        /// <param name="track">The original source track.</param>
        /// <returns>The source fixture identity selected by the unchanged rules.</returns>
        private static string OriginalImageIdentity(Zombie zombie, string asset, string track)
        {
            var image = AdventureZombieEquipmentRules.Image(zombie, asset, "Zombie", track);
            if (image == null && zombie.PresentationArmLost)
            {
                var kind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
                image = AdventureZombieFeedbackRules.Same(asset, "Zombie") ? AdventureZombieFeedbackRules.ArmReplacement(kind, track) : null;
                if ((kind == 1 || kind == 33) && AdventureZombieFeedbackRules.Same(track, "Zombie_flag"))
                {
                    image = "IMAGE_REANIM_ZOMBIE_FLAG3";
                }
            }
            return image == null ? "reanim/source.png" : "reanim/" + image.Substring("IMAGE_REANIM_".Length).ToLowerInvariant() + ".png";
        }

        /// <summary>Allows every named fixture track so the test isolates combat equipment, arm and head rules.</summary>
        /// <param name="track">The original source-track name.</param>
        /// <returns>True for every nonempty fixture track.</returns>
        private static bool IncludeNamedTrack(string track)
        {
            return !string.IsNullOrEmpty(track);
        }

        /// <summary>Reads completed logical source commands without adding a production diagnostic interface.</summary>
        /// <returns>The existing screen sprite pool.</returns>
        private ObjectSpriteRenderer Pool()
        {
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_renderer)!;
        }

        /// <summary>Creates a visible two-frame native image track.</summary>
        /// <param name="name">The original source-track name.</param>
        /// <param name="x">The local source translation distinguishing body and attachment artwork.</param>
        /// <returns>An original-format track fragment.</returns>
        private static string Track(string name, float x)
        {
            return "<track><name>" + name + "</name><t><i>IMAGE_REANIM_source</i><x>" + x + "</x><f>0</f></t><t/></track>";
        }
    }
}
