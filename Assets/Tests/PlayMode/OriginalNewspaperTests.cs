#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original newspaper artwork and sound resources through the production presentation path.</summary>
    public sealed class OriginalNewspaperTests
    {
        /// <summary>Resolves the explicitly selected read-only original runtime resource directory.</summary>
        /// <returns>The existing original runtime directory.</returns>
        private static string ResourceRoot()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            return root;
        }

        /// <summary>Checks that the presenter submits the original question-mark attachment and angry head at their native transition.</summary>
        /// <returns>A frame boundary allowing the real graphics device to initialize.</returns>
        [UnityTest]
        public IEnumerator OriginalQuestionMarkAndAngryHeadAreDrawn()
        {
            var root = ResourceRoot();
            yield return null;
            var host = new GameObject("Inactive original newspaper fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
                Assert.That(owner._game.ChooseDefaultAdventureSeeds(), Is.True);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var load = typeof(ReanimationRenderer).GetMethod("LoadTexture", flags)!;
                var zombie = new Zombie
                {
                    Id = 81,
                    Kind = ZombieKind.Newspaper,
                    Row = 2,
                    X = (200f + 32f) / 80f,
                    Health = 270f,
                    StartingHealth = 270f,
                    PresentationNativePhase = 30,
                    PresentationNewspaperGaspRemaining = 1f,
                    PresentationNewspaperSurpriseAge = .5f,
                    PresentationAnimation = "anim_gasp",
                    PresentationAnimationAge = .5f
                };
                owner._game.Zombies.Add(zombie);
                Submit(animations, presenter, draw, zombie);
                var question = (Texture2D)load.Invoke(animations, new object[] { "IMAGE_REANIM_ZOMBIE_QUESTIONMARK" })!;
                var angryHead = (Texture2D)load.Invoke(animations, new object[] { "IMAGE_REANIM_ZOMBIE_PAPER_MADHEAD" })!;
                var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
                var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", flags)!.GetValue(pool)!;
                var questionSprite = Find(pool, sprites, question);
                Assert.That(questionSprite, Is.Not.Null, "The gasp must display the original attached question-mark artwork.");
                // PC PlayOnce samples (frameCount-1)*normalizedTime, not seconds*fps directly.
                // At 0.5 seconds: normalizedTime=0.5*12/15=0.4, so source frame=5.6.
                // Original frames five/six are (35.9,-32) and (37,-30.3); attachment origin is (189,250).
                var expectedQuestionX = 189f + 35.9f + (37f - 35.9f) * .6f;
                var expectedQuestionY = 250f - 32f + (-30.3f + 32f) * .6f;
                Assert.That(questionSprite!.Vertices[0].x, Is.EqualTo(expectedQuestionX).Within(.01f), "The attachment uses the original normalized-time X interpolation.");
                Assert.That(-questionSprite.Vertices[0].y, Is.EqualTo(expectedQuestionY).Within(.01f), "The attachment uses the original normalized-time Y interpolation.");
                Assert.That(Find(pool, sprites, angryHead), Is.Null, "The mad head appears only after the gasp completes.");
                pool.RenderTo(target);
                Read(target, pixels);
                Assert.That(VisiblePixels(pixels, new Rect(220, 200, 60, 50)), Is.GreaterThan(15), "The question mark must reach the actual camera output.");
                Save(pixels, "newspaper-surprise.png");

                zombie.PresentationNativePhase = 31;
                zombie.PresentationNewspaperGaspRemaining = 0f;
                zombie.PresentationNewspaperSurpriseAge = -1f;
                Submit(animations, presenter, draw, zombie);
                Assert.That(Find(pool, sprites, question), Is.Null, "Completed surprise playback leaves no question mark on the angry zombie.");
                Assert.That(Find(pool, sprites, angryHead), Is.Not.Null, "Angry walking must replace anim_head1 with the original mad-head texture.");
                pool.RenderTo(target);
                Read(target, pixels);
                Save(pixels, "newspaper-angry.png");
            }
            finally
            {
                presenter.Dispose();
                animations.Dispose();
                foreach (var texture in owner._textures.Values)
                {
                    if (texture != null)
                    {
                        UnityEngine.Object.Destroy(texture);
                    }
                }

                owner._textures.Clear();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Checks that each original rip and weighted roar sample decodes to non-silent PCM.</summary>
        /// <returns>The original audio decoding requests.</returns>
        [UnityTest]
        public IEnumerator OriginalNewspaperSoundsDecodeNonSilentPcm()
        {
            var root = ResourceRoot();
            foreach (var sample in new[] { "newspaper_rip", "newspaper_rarrgh", "newspaper_rarrgh2" })
            {
                var path = Path.Combine(root, "sounds", sample + ".ogg");
                using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS);
                yield return request.SendWebRequest();
                Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), sample + ": " + request.error);
                var clip = DownloadHandlerAudioClip.GetContent(request);
                try
                {
                    Assert.That(clip.samples, Is.GreaterThan(0), sample);
                    var data = new float[clip.samples * clip.channels];
                    Assert.That(clip.GetData(data, 0), Is.True, sample);
                    var peak = 0f;
                    foreach (var value in data)
                    {
                        peak = Math.Max(peak, Math.Abs(value));
                    }

                    Assert.That(peak, Is.GreaterThan(.001f), sample + " must contain audible source data.");
                }
                finally
                {
                    UnityEngine.Object.Destroy(clip);
                }
            }
        }

        /// <summary>Submits one actual presenter frame over a white background.</summary>
        /// <param name="animations">The original animation renderer.</param>
        /// <param name="presenter">The production game presenter.</param>
        /// <param name="draw">The production zombie drawing method.</param>
        /// <param name="zombie">The explicit isolated fixture zombie.</param>
        private static void Submit(ReanimationRenderer animations, GamePresenter presenter, MethodInfo draw, Zombie zombie)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0, 0, 800, 600), new Rect(0, 0, 1, 1));
            draw.Invoke(presenter, new object[] { zombie });
            animations.EndObjectFrame();
        }

        /// <summary>Finds an actually submitted quad that uses a particular original texture.</summary>
        /// <param name="pool">The active original sprite pool.</param>
        /// <param name="sprites">The pool's retained quad storage.</param>
        /// <param name="texture">The exact original decoded texture.</param>
        /// <returns>The matching live quad, or null when none was submitted.</returns>
        private static ObjectSprite? Find(ObjectSpriteRenderer pool, List<ObjectSprite> sprites, Texture2D texture)
        {
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                if (ReferenceEquals(sprites[index].Properties.GetTexture(Shader.PropertyToID("_MainTex")), texture))
                {
                    return sprites[index];
                }
            }

            return null;
        }

        /// <summary>Copies the completed graphics target into readable pixels.</summary>
        /// <param name="target">The completed URP output.</param>
        /// <param name="pixels">The reusable readback image.</param>
        private static void Read(RenderTexture target, Texture2D pixels)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Counts nonwhite artwork inside an expected logical attachment region.</summary>
        /// <param name="pixels">The original-size camera output.</param>
        /// <param name="region">The top-left logical region containing the attachment.</param>
        /// <returns>The number of visible original artwork pixels.</returns>
        private static int VisiblePixels(Texture2D pixels, Rect region)
        {
            var count = 0;
            for (var y = (int)region.yMin; y < region.yMax; y++)
            {
                for (var x = (int)region.xMin; x < region.xMax; x++)
                {
                    var color = pixels.GetPixel(x, 599 - y);
                    if (color.r + color.g + color.b < 2.8f)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Writes optional diagnostics only into the explicitly selected test output directory.</summary>
        /// <param name="pixels">The completed camera image.</param>
        /// <param name="name">The diagnostic image filename.</param>
        private static void Save(Texture2D pixels, string name)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name), pixels.EncodeToPNG());
            }
        }
    }
}
