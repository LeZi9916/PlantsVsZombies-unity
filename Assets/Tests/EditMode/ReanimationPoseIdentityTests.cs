#nullable enable
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects exact-name pose identity across source resources, duplicate tracks, hidden poses, and replay resets.</summary>
    public sealed class ReanimationPoseIdentityTests
    {
        /// <summary>The isolated original-format source directory.</summary>
        private string _root = string.Empty;
        /// <summary>The production sampler whose retained blend poses are tested.</summary>
        private ReanimationRenderer _renderer = null!;

        /// <summary>Creates two original resources sharing one exact track name and several deliberate identity variants.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "PvZPoseIdentity-" + Guid.NewGuid().ToString("N"));
            var folder = Directory.CreateDirectory(Path.Combine(_root, "reanim")).FullName;
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                image.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                image.Apply();
                File.WriteAllBytes(Path.Combine(folder, "sprite.png"), image.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
            Write(folder, "First", Markers() + ImageTrack("body", 10, 30));
            Write(folder, "Second", Markers() + ImageTrack("body", 50, 70));
            Write(folder, "DifferentCase", Markers() + ImageTrack("Body", 50, 70));
            Write(folder, "Duplicate", Markers() + ImageTrack("body", 0, 20) + ImageTrack("body", 30, 40));
            Write(folder, "Hidden", "<track><name>anim_idle</name><t><f>0</f></t><t/><t><f>-1</f></t><t/><t/><t/></track>"
                + "<track><name>anim_hidden</name><t><f>-1</f></t><t/><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_alt</name><t><f>-1</f></t><t/><t/><t/><t><f>0</f></t><t/></track>"
                + "<track><name>body</name><t><i>IMAGE_REANIM_sprite</i><x>10</x><f>0</f></t><t/>"
                + "<t><x>70</x><f>-1</f></t><t/><t><x>100</x><f>0</f></t><t/></track>"
                + "<track><name>filler</name><t><i>IMAGE_REANIM_sprite</i><x>200</x><f>0</f></t><t/><t/><t/><t/><t/></track>");
            _renderer = new ReanimationRenderer(_root);
        }

        /// <summary>Releases this fixture's sources and native rendering resources.</summary>
        [TearDown]
        public void TearDown()
        {
            _renderer?.Dispose();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        /// <summary>Blends the same exact track name across independently parsed resource assets.</summary>
        [Test]
        public void MatchingTrackNamesBlendAcrossSourceAssets()
        {
            Draw("First", "anim_idle", 0f);
            Draw("Second", "anim_alt", 0f);
            Assert.That(Draw("Second", "anim_alt", .1f), Is.EqualTo(40f).Within(.0001f));
        }

        /// <summary>Keeps ordinal pose matching independent of case-insensitive animation and resource aliases.</summary>
        [Test]
        public void DifferentlyCasedTrackNamesDoNotShareBlendPoses()
        {
            Draw("First", "anim_idle", 0f);
            Assert.That(Draw("DifferentCase", "anim_alt", 0f), Is.EqualTo(70f).Within(.0001f));
            Assert.That(Draw("DifferentCase", "anim_alt", .1f), Is.EqualTo(70f).Within(.0001f));
        }

        /// <summary>Uses the last submitted duplicate-name pose when a later clip begins its transition.</summary>
        [Test]
        public void DuplicateTrackNamesRetainLastWriteWinsBehavior()
        {
            Draw("Duplicate", "anim_idle", 0f);
            Assert.That(Draw("Second", "anim_alt", 0f), Is.EqualTo(30f).Within(.0001f));
            Assert.That(Draw("Second", "anim_alt", .1f), Is.EqualTo(50f).Within(.0001f));
        }

        /// <summary>Restarts a blend from the latest sampled pose when the same clip's unwrapped clock moves backward.</summary>
        [Test]
        public void SameClipReplayStartsFromTheMostRecentPose()
        {
            Assert.That(Draw("First", "anim_idle", .2f), Is.EqualTo(30f).Within(.0001f));
            Assert.That(Draw("First", "anim_idle", 0f), Is.EqualTo(30f).Within(.0001f));
            Assert.That(Draw("First", "anim_idle", .1f), Is.EqualTo(26f).Within(.0001f));
        }

        /// <summary>Does not restore a stale visible source pose after an intervening invisible clip.</summary>
        [Test]
        public void HiddenPosePreventsBlendingFromItsPreviouslyVisiblePosition()
        {
            Draw("Hidden", "anim_idle", 0f);
            Draw("Hidden", "anim_hidden", 0f);
            Assert.That(Draw("Hidden", "anim_alt", 0f), Is.EqualTo(100f).Within(.0001f));
        }

        /// <summary>Adopts the simulation-owned zero-blend pose even when the intermediate resource was not drawn.</summary>
        [Test]
        public void ExplicitPoseResetReplacesThePreviousResourcePose()
        {
            Draw("First", "anim_idle", 0f);
            Assert.That(_renderer.EnsureNativePlaybackPose("Second", "actor", "anim_alt", 1), Is.True);
            Assert.That(Draw("First", "anim_idle", 0f), Is.EqualTo(70f).Within(.0001f));
        }

        /// <summary>Draws one retained actor and reads the first source quad's native x coordinate.</summary>
        /// <param name="resource">The original animation resource.</param>
        /// <param name="animation">The named clip to play.</param>
        /// <param name="seconds">The unwrapped simulation-owned playback clock.</param>
        /// <returns>The source quad's horizontal position after pose blending.</returns>
        private float Draw(string resource, string animation, float seconds)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            _renderer.BeginObjectFrame();
            try
            {
                Assert.That(_renderer.DrawNativePlayback(resource, Vector2.zero, 1f, seconds,
                    animation, false, 12f, instanceKey: "actor", blendTicks: 20), Is.True, _renderer.LastError);
            }
            finally
            {
                _renderer.EndObjectFrame();
            }
            var field = typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var pool = (ObjectSpriteRenderer)field.GetValue(_renderer)!;
            Assert.That(pool.SubmittedObjectCount, Is.GreaterThan(0));
            return pool.SubmittedSprites[0].Vertices[0].x;
        }

        /// <summary>Writes one parsed resource without changing any repository asset.</summary>
        /// <param name="folder">The isolated native animation directory.</param>
        /// <param name="name">The resource basename.</param>
        /// <param name="tracks">The authored marker and image tracks.</param>
        private static void Write(string folder, string name, string tracks)
        {
            File.WriteAllText(Path.Combine(folder, name + ".reanim"), "<fps>12</fps>" + tracks);
        }

        /// <summary>Creates independent two-frame idle and alternative marker ranges.</summary>
        /// <returns>The original-format marker track fragments.</returns>
        private static string Markers()
        {
            return "<track><name>anim_idle</name><t><f>0</f></t><t/><t><f>-1</f></t><t/></track>"
                + "<track><name>anim_alt</name><t><f>-1</f></t><t/><t><f>0</f></t><t/></track>";
        }

        /// <summary>Creates a source track whose idle pose moves and whose alternative pose stays fixed.</summary>
        /// <param name="name">The exact track name used to retain blend state.</param>
        /// <param name="first">The idle clip's first x coordinate.</param>
        /// <param name="second">The idle clip's last and alternative clip's fixed x coordinate.</param>
        /// <returns>The original-format source track fragment.</returns>
        private static string ImageTrack(string name, int first, int second)
        {
            return "<track><name>" + name + "</name><t><i>IMAGE_REANIM_sprite</i><x>" + first
                + "</x><f>0</f></t><t><x>" + second + "</x></t><t/><t/></track>";
        }
    }
}
