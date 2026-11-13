#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks changed native motion using the original animation artwork, real graphics, and sound files.</summary>
    public sealed class OriginalNativeMotionTests
    {
        /// <summary>Checks reverse-dive pixels, water-death clipping, and the previously missing original water splash.</summary>
        /// <returns>The graphics initialization frame.</returns>
        [UnityTest]
        public IEnumerator SnorkelReverseDiveMatchesRiseAndDeathClipsAtWaterline()
        {
            var root = ResourceRoot();
            yield return null;
            var host = new GameObject("Inactive native motion fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 21 }, 7529);
                owner._game.ChooseDefaultAdventureSeeds();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var zombie = new Zombie
                {
                    Id = 81, Kind = ZombieKind.Snorkel, Row = 2, X = 4f,
                    Health = 270f, StartingHealth = 270f, PresentationInPool = true,
                    PresentationAltitude = 10f, Eating = true, PresentationNativePhase = 60
                };
                owner._game.Zombies.Add(zombie);
                owner._game.AdventurePresentationZombieAnimation(zombie);
                zombie.PresentationAnimationAge = .2f;
                Submit(animations, presenter, draw, zombie);
                Read(animations, target, pixels);
                var rise = pixels.GetPixels32();
                Assert.That(VisiblePixels(rise, 0, 600), Is.GreaterThan(100));
                Save(pixels, "snorkel-rise.png");
                zombie.PresentationNativePhase = 62;
                zombie.Eating = false;
                zombie.PresentationAnimationAge = .3f;
                Submit(animations, presenter, draw, zombie);
                Read(animations, target, pixels);
                Assert.That(pixels.GetPixels32(), Is.EqualTo(rise), "Down-from-eating must reverse the original uptoeat clip.");
                Save(pixels, "snorkel-dive.png");

                zombie.PresentationNativePhase = 61;
                zombie.Eating = true;
                owner._game.AdventurePresentationZombieAnimation(zombie);
                foreach (var age in new[] { 0f, .3f, .6f, 1.1f })
                {
                    zombie.PresentationAnimationAge = age;
                    Submit(animations, presenter, draw, zombie);
                    AssertBodyClipped(animations, 338f);
                    AssertSnorkelSourceOrder(animations);
                }

                Read(animations, target, pixels);
                Save(pixels, "snorkel-eating-hands.png");
                zombie.Eating = false;
                zombie.PresentationNativePhase = 58;
                zombie.PresentationInPool = false;
                owner._game.AdventurePresentationZombieAnimation(zombie);
                zombie.PresentationAnimationAge = 0f;
                Submit(animations, presenter, draw, zombie);
                zombie.PresentationAnimationAge = .7f;
                Submit(animations, presenter, draw, zombie);
                Read(animations, target, pixels);
                Assert.That(VisiblePixels(pixels.GetPixels32(), 0, 600), Is.GreaterThan(100));
                Save(pixels, "snorkel-jump.png");

                zombie.PresentationNativePhase = 61;
                zombie.PresentationInPool = true;
                typeof(GameSimulation).GetMethod("AdventurePresentationZombieDeath", flags)!.Invoke(owner._game, new object[] { zombie, 0U });
                owner._game.Presentation.Corpses[0].Age = .8f;
                var corpse = typeof(GamePresenter).GetMethod("DrawAdventureCorpses", flags)!;
                ObjectSpriteRenderer.BeginSceneFrame();
                SpriteDrawState.Clip = new Rect(0, 0, 800, 600);
                animations.BeginObjectFrame();
                animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0, 0, 800, 600), new Rect(0, 0, 1, 1));
                corpse.Invoke(presenter, new object[] { 2 });
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                var death = pixels.GetPixels32();
                Assert.That(VisiblePixels(death, 0, 339), Is.GreaterThan(100));
                AssertBodyClipped(animations, 338f);
                Save(pixels, "snorkel-death.png");

                ObjectSpriteRenderer.BeginSceneFrame();
                SpriteDrawState.Clip = new Rect(0, 0, 800, 600);
                animations.BeginObjectFrame();
                animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0, 0, 800, 600), new Rect(0, 0, 1, 1));
                Assert.That(animations.DrawNativePlayback("splash", new Vector2(350, 280), 1.2f, .2f, null, false, 18f,
                    verticalScale: .8f), Is.True, "The production resource root must contain the exact original splash reanimation and artwork.");
                animations.EndObjectFrame();
                Read(animations, target, pixels);
                Assert.That(VisiblePixels(pixels.GetPixels32(), 0, 600), Is.GreaterThan(50));
                Save(pixels, "native-water-splash.png");
            }
            finally
            {
                presenter.Dispose();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                owner.Textures.ReleaseAll();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Checks the first landed production pose exactly matches a fresh walk pose instead of blending the jump displacement twice.</summary>
        /// <returns>A frame boundary for the original artwork and graphics device.</returns>
        [UnityTest]
        public IEnumerator PoleVaultLandingUsesWalkPoseWithoutSlidingFromJumpDisplacement()
        {
            var root = ResourceRoot();
            yield return null;
            var host = new GameObject("Inactive original pole vault landing fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            using var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            try
            {
                owner._root = root;
                owner._animations = animations;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 1 }, 7529);
                Assert.That(owner._game.Adventure.Phase, Is.EqualTo(AdventurePhase.Playing), "The opening level begins directly without a seed chooser.");
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GamePresenter).GetField("_renderingBoardObjects", flags)!.SetValue(presenter, true);
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureZombie", flags)!;
                var begin = typeof(GameSimulation).GetMethod("BeginAdventurePoleVault", flags)!;
                var clock = typeof(GameSimulation).GetMethod("AdvanceAdventureZombieClock", flags)!;
                var jump = typeof(GameSimulation).GetMethod("AdvanceAdventurePoleVault", flags)!;
                var zombie = new Zombie
                {
                    Id = 81, Kind = ZombieKind.PoleVaulter, Row = 2, X = 6.5f,
                    Health = 500f, StartingHealth = 500f, PresentationInPool = false
                };
                owner._game.Zombies.Add(zombie);
                begin.Invoke(owner._game, new object[] { zombie, new Plant { Kind = PlantKind.Wallnut, Row = 2, Column = 5 } });
                for (var tick = 0; tick < 179; tick++)
                {
                    clock.Invoke(owner._game, new object[] { zombie, .01f });
                    jump.Invoke(owner._game, new object[] { zombie, .01f });
                }

                Submit(animations, presenter, draw, zombie);
                zombie.PresentationPreviousX = zombie.X;
                clock.Invoke(owner._game, new object[] { zombie, .01f });
                jump.Invoke(owner._game, new object[] { zombie, .01f });
                Assert.That(zombie.PresentationNativePhase, Is.EqualTo(13));
                Submit(animations, presenter, draw, zombie);
                Read(animations, target, pixels);
                var landed = pixels.GetPixels32();
                Assert.That(VisiblePixels(landed, 0, 600), Is.GreaterThan(100));
                Save(pixels, "pole-vault-first-landed-pose.png");

                // A new actor identity has no retained jump pose and provides an independent unblended walk reference.
                zombie.Id = 82;
                Submit(animations, presenter, draw, zombie);
                Read(animations, target, pixels);
                Assert.That(pixels.GetPixels32(), Is.EqualTo(landed), "PC StartWalkAnim(0) must use the authored walk pose immediately at the landing origin.");
            }
            finally
            {
                presenter.Dispose();
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                owner.Textures.ReleaseAll();
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Checks real motion Foley samples decode and reach the production shared voice pool.</summary>
        /// <returns>The audio decoding and playback frame boundaries.</returns>
        [UnityTest]
        public IEnumerator OriginalVaultAndWaterFoleyReachProductionVoices()
        {
            var root = ResourceRoot();
            var host = new GameObject("Inactive motion Foley fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audioHost = new GameObject("Motion Foley voices");
            var source = audioHost.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            using var voices = new AudioVoicePool(audioHost, source, 8);
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audioHost.AddComponent<AudioListener>();
            }

            var previousPause = AudioListener.pause;
            AudioListener.pause = false;
            var effects = owner.Sounds;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            owner._root = root;
            effects._audioSource = source;
            effects._soundVolume = .7f;
            effects._soundVoices = voices;
            typeof(GameSoundEffects).GetField("_zombiePartAudioLoading", flags)!.SetValue(effects, true);
            var play = typeof(GameSoundEffects).GetMethod("PlayAdventureZombieFoley", flags)!;
            try
            {
                foreach (var foley in new[] { 74, 78, 48, 52, 53, 16, 22, 23, 46, 50, 51 })
                {
                    var clips = new List<AudioClip>();
                    foreach (var sample in AdventureZombieFoleyData.Definitions[foley].Samples)
                    {
                        using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.Combine(root, "sounds", sample + ".ogg")).AbsoluteUri, AudioType.OGGVORBIS);
                        yield return request.SendWebRequest();
                        Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), sample + ": " + request.error);
                        var clip = DownloadHandlerAudioClip.GetContent(request);
                        var data = new float[clip.samples * clip.channels];
                        Assert.That(clip.GetData(data, 0), Is.True);
                        Assert.That(Array.Exists(data, value => Math.Abs(value) > .001f), Is.True, sample);
                        effects._presentationSounds[sample] = clip;
                        clips.Add(clip);
                    }

                    voices.StopAll();
                    play.Invoke(effects, new object[] { new AdventureZombieSoundEvent { Foley = foley, Tick = 100 } });
                    yield return null;
                    Assert.That(Array.Exists(audioHost.GetComponents<AudioSource>(), voice => voice.isPlaying && clips.Contains(voice.clip)),
                        Is.True, "Foley " + foley + " must reach a production audio voice.");
                }
            }
            finally
            {
                voices.StopAll();
                AudioListener.pause = previousPause;
                foreach (var clip in effects._presentationSounds.Values)
                {
                    UnityEngine.Object.Destroy(clip);
                }

                effects._presentationSounds.Clear();
                UnityEngine.Object.Destroy(audioHost);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Gets an explicitly supplied read-only original runtime asset directory.</summary>
        /// <returns>The existing resource root.</returns>
        private static string ResourceRoot()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the original runtime resource directory.");
            }

            return root;
        }

        /// <summary>Submits the production zombie presenter over a white background.</summary>
        /// <param name="animations">The production native animation renderer.</param>
        /// <param name="presenter">The production game presenter.</param>
        /// <param name="draw">The private presenter entry point.</param>
        /// <param name="zombie">The initialized native pose.</param>
        private static void Submit(ReanimationRenderer animations, GamePresenter presenter, MethodInfo draw, Zombie zombie)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = new Rect(0, 0, 800, 600);
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0, 0, 800, 600), new Rect(0, 0, 1, 1));
            draw.Invoke(presenter, new object[] { zombie });
            animations.EndObjectFrame();
        }

        /// <summary>Renders the submitted objects and reads their real graphics output.</summary>
        /// <param name="animations">The renderer owning the submitted sprite pool.</param>
        /// <param name="target">The real graphics render target.</param>
        /// <param name="pixels">The reusable readable output texture.</param>
        private static void Read(ReanimationRenderer animations, RenderTexture target, Texture2D pixels)
        {
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            pool.RenderTo(target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 800, 600), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
        }

        /// <summary>Checks actual submitted body clipping bounds while preserving native unclipped whitewater tracks.</summary>
        /// <param name="animations">The renderer that submitted the retained death pose.</param>
        /// <param name="bottom">The original DrawReanim body clipping edge.</param>
        private static void AssertBodyClipped(ReanimationRenderer animations, float bottom)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var load = typeof(ReanimationRenderer).GetMethod("LoadTexture", flags)!;
            var whitewater1 = load.Invoke(animations, new object[] { "IMAGE_REANIM_ZOMBIE_SNORKLE_WHITEWATER1" });
            var whitewater2 = load.Invoke(animations, new object[] { "IMAGE_REANIM_ZOMBIE_SNORKLE_WHITEWATER2" });
            var whitewater3 = load.Invoke(animations, new object[] { "IMAGE_REANIM_ZOMBIE_SNORKLE_WHITEWATER3" });
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(animations)!;
            var sprites = pool.SubmittedSprites;
            var bodyQuads = 0;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var sprite = sprites[index];
                var texture = sprite.SourceImage;
                if (texture?.SourceTexture == Texture2D.whiteTexture || ReferenceEquals(texture, whitewater1) || ReferenceEquals(texture, whitewater2) || ReferenceEquals(texture, whitewater3))
                {
                    continue;
                }

                bodyQuads++;
                Assert.That(sprite.ClipEnabled, Is.True);
                Assert.That(sprite.Clip.yMax,
                    Is.LessThanOrEqualTo(bottom + .01f), "Native death body artwork must stop at the original waterline.");
            }

            Assert.That(bodyQuads, Is.GreaterThan(0));
        }

        /// <summary>Checks the original source ordering while the waterline independently clips the hands.</summary>
        /// <param name="animations">The renderer that submitted the eating pose.</param>
        private static void AssertSnorkelSourceOrder(ReanimationRenderer animations)
        {
            var names = new[]
            {
                "Zombie_snorkle_innerarm_hand", "Zombie_snorkle_innerarm_lower", "Zombie_snorkle_innerarm_upper",
                "Zombie_snorkle_body1", "anim_head1", "anim_head_jaw", "anim_head_snorkle",
                "Zombie_outerarm_hand", "Zombie_snorkle_outerarm_upper", "Zombie_outerarm_lower",
                "Zombie_snorkle_whitewater2", "Zombie_snorkle_whitewater"
            };
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer)
                .GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            var previous = -1;
            var checkedTracks = 0;
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                var node = pool.SubmittedSprites[index].GameObject;
                if (node == null)
                {
                    continue;
                }

                var order = Array.IndexOf(names, node.name);
                if (order >= 0)
                {
                    Assert.That(order, Is.GreaterThan(previous), "Snorkel tracks must retain the original reanimation's painter order.");
                    previous = order;
                    checkedTracks++;
                }
            }

            Assert.That(checkedTracks, Is.GreaterThan(3));
        }

        /// <summary>Counts visible original artwork in an inclusive/exclusive band of logical screen rows.</summary>
        /// <param name="pixels">The complete bottom-left-origin camera readback.</param>
        /// <param name="top">The first logical screen row to include.</param>
        /// <param name="bottom">The first logical screen row to exclude.</param>
        /// <returns>The nonwhite pixel count in that band.</returns>
        private static int VisiblePixels(Color32[] pixels, int top, int bottom)
        {
            var count = 0;
            for (var y = top; y < bottom; y++)
            {
                for (var x = 0; x < 800; x++)
                {
                    var color = pixels[(599 - y) * 800 + x];
                    if (color.r + color.g + color.b < 714)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Writes optional graphics diagnostics only into the caller's selected test output directory.</summary>
        /// <param name="pixels">The completed real graphics output.</param>
        /// <param name="name">The diagnostic filename.</param>
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
