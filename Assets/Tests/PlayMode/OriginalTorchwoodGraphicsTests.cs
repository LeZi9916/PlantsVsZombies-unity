#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
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
    /// <summary>Checks real original FirePea attachment coordinates, new animation clocks and ignition audio.</summary>
    public sealed class OriginalTorchwoodGraphicsTests
    {
        /// <summary>Checks forward and backward fire attachments remain on the pea path and use original animation and sound.</summary>
        /// <returns>Original audio decoding and graphics frame boundaries.</returns>
        [UnityTest]
        public IEnumerator FireAttachmentsRetainBothOriginalPathsAndStartTheirOwnAnimation()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            var audioPath = Path.Combine(root, "sounds", "firepea.ogg");
            if (!File.Exists(audioPath) || !Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resources.");
            }

            using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(audioPath).AbsoluteUri, AudioType.OGGVORBIS);
            yield return request.SendWebRequest();
            Assert.That(request.result, Is.EqualTo(UnityWebRequest.Result.Success), request.error);
            var clip = DownloadHandlerAudioClip.GetContent(request);
            var host = new GameObject("Inactive original Torchwood graphics fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var audioHost = new GameObject("Original ignition audio fixture");
            var source = audioHost.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true;
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 0)
            {
                audioHost.AddComponent<AudioListener>();
            }

            var effects = owner.Sounds;
            effects._audioSource = source;
            effects._soundVolume = .7f;
            effects._soundVoices = new AudioVoicePool(audioHost, source, 4);
            effects._presentationSounds["firepea"] = clip;
            using var animations = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            var target = new RenderTexture(800, 600, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var pixels = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            var previousMatrix = SpriteDrawState.Matrix;
            var previousColor = SpriteDrawState.Color;
            var previousClip = SpriteDrawState.Clip;
            var previousPause = AudioListener.pause;
            AudioListener.pause = false;
            try
            {
                owner._root = root;
                owner._animations = animations;
                SpriteDrawState.Matrix = Matrix4x4.identity;
                SpriteDrawState.Color = Color.white;
                SpriteDrawState.Clip = new Rect(0f, 0f, 800f, 600f);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var fire = typeof(GameSimulation).GetMethod("FireAdventurePea", flags)!;
                var plants = typeof(GameSimulation).GetMethod("AdvanceAdventurePlants", flags)!;
                var move = typeof(GameSimulation).GetMethod("AdvanceAdventureProjectiles", flags)!;
                var draw = typeof(GamePresenter).GetMethod("DrawAdventureProjectile", flags)!;
                foreach (var backward in new[] { false, true })
                {
                    owner._game = new GameSimulation(new GameRules { AdventureLevel = 41 }, 7529);
                    owner._game.ChooseDefaultAdventureSeeds();
                    typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                        .Invoke(owner._game, new object[] { PlantKind.Torchwood, 2, 4 });
                    var plant = new Plant { Kind = backward ? PlantKind.SplitPea : PlantKind.Peashooter, Row = 2, Column = 2 };
                    var shot = (Pea)fire.Invoke(owner._game, new object[] { plant, plant.Row, backward, 20f })!;
                    shot.X = 4.45f;
                    shot.PresentationPreviousX = shot.X;
                    shot.PresentationAge = 2f;
                    shot.PresentationNativeAge = 2f;
                    var y = shot.PresentationLaunchY;
                    BeginFrame(animations);
                    draw.Invoke(presenter, new object[] { shot });
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    var ordinary = pixels.GetPixels32();
                    BeginFrame(animations);
                    Assert.That(animations.DrawResourceSprite("IMAGE_PROJECTILEPEA", new Vector2(396f, y), 1f), Is.True);
                    animations.EndObjectFrame();
                    Read(animations, target, pixels);
                    Assert.That(ordinary, Is.EqualTo(pixels.GetPixels32()), "The ordinary pea retains its existing muzzle height while crossing the roof.");
                    owner._game.Presentation.ZombieSounds.Clear();
                    plants.Invoke(owner._game, new object[] { .01f });
                    Assert.That(shot.Fire, Is.True);
                    Assert.That(shot.PresentationFireAge, Is.Zero);
                    Assert.That(shot.PresentationFireRate, Is.InRange(50f, 80f));
                    var ignition = owner._game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 39);
                    Assert.That(ignition, Has.Count.EqualTo(1));
                    if (!backward)
                    {
                        typeof(GameSoundEffects).GetMethod("PlayAdventureZombieFoley", flags)!.Invoke(effects, new object[] { ignition[0] });
                        yield return null;
                        var playing = 0;
                        foreach (var voice in audioHost.GetComponents<AudioSource>())
                        {
                            if (voice.clip == clip && voice.isPlaying)
                            {
                                playing++;
                                Assert.That(voice.pitch, Is.InRange(1f, Mathf.Pow(2f, 10f / 12f)));
                            }
                        }

                        Assert.That(playing, Is.EqualTo(1), "One real conversion reaches the original firepea recording once.");
                    }

                    for (var sample = 0; sample < 2; sample++)
                    {
                        BeginFrame(animations);
                        draw.Invoke(presenter, new object[] { shot });
                        animations.EndObjectFrame();
                        Read(animations, target, pixels);
                        var actual = pixels.GetPixels32();
                        Assert.That(Array.Exists(actual, pixel => pixel.r > pixel.g + 20 && pixel.r > pixel.b + 20), Is.True,
                            "The real FirePea sheet must submit original flame pixels.");
                        BeginFrame(animations);
                        var origin = new Vector2(40f + shot.X * 80f - 39f, y - 39f);
                        Assert.That(animations.DrawNativePlayback("FirePea", origin, 1f, sample * .05f, null, true,
                            shot.PresentationFireRate, facingLeft: backward, mirrorWidth: 80f), Is.True);
                        animations.EndObjectFrame();
                        Read(animations, target, pixels);
                        Assert.That(actual, Is.EqualTo(pixels.GetPixels32()),
                            "PC 47E0E0 attaches at native mPos+(-25,-25), or +(55,-25) with negative X scale, and starts age zero at 50..80 FPS.");
                        if (sample == 0)
                        {
                            move.Invoke(owner._game, new object[] { .05f, false });
                            shot.PresentationPreviousX = shot.X;
                        }
                    }
                }
            }
            finally
            {
                SpriteDrawState.Matrix = previousMatrix;
                SpriteDrawState.Color = previousColor;
                SpriteDrawState.Clip = previousClip;
                AudioListener.pause = previousPause;
                effects.DisposeAdventureCherryBombAudio();
                effects._presentationSounds.Clear();
                presenter.Dispose();
                owner.Textures.ReleaseAll();
                target.Release();
                UnityEngine.Object.Destroy(clip);
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(audioHost);
                UnityEngine.Object.Destroy(host);
            }
        }

        /// <summary>Begins an original-artwork frame on an opaque white comparison background.</summary>
        /// <param name="animations">The real renderer receiving the sprite frame.</param>
        private static void BeginFrame(ReanimationRenderer animations)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            animations.BeginObjectFrame();
            animations.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>Renders the real sprite batch and reads its comparison pixels.</summary>
        /// <param name="animations">The renderer owning the submitted frame.</param>
        /// <param name="target">The independent GPU render target.</param>
        /// <param name="pixels">The reusable readable comparison image.</param>
        private static void Read(ReanimationRenderer animations, RenderTexture target, Texture2D pixels)
        {
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animations)!;
            pool.RenderTo(target);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0f, 0f, 800f, 600f), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
        }
    }
}
