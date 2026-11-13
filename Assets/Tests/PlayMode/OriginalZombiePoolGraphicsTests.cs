#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original swimmer artwork, per-track water clipping, and pool ambush effects through production presentation.</summary>
    public sealed class OriginalZombiePoolGraphicsTests
    {
        /// <summary>Owns an inactive coordinator without profile or game lifecycle side effects.</summary>
        private GameObject _host = null!;
        /// <summary>Provides the real pool board and original assets to the production presenter.</summary>
        private PvZGame _owner = null!;
        /// <summary>Samples original body, tube, water, seaweed, and splash artwork.</summary>
        private ReanimationRenderer _animations = null!;
        /// <summary>Runs the actual board actor and attached effect presentation.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Retains the original track order and authored transforms used by the independent reference.</summary>
        private XmlDocument _definition = null!;
        /// <summary>Receives the real scene and Canvas camera output.</summary>
        private RenderTexture _target = null!;
        /// <summary>Receives actual camera pixels for source and waterline comparisons.</summary>
        private Texture2D _pixels = null!;
        /// <summary>Restores the scene target owned by the surrounding Unity runner.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>Maps native board coordinates onto the current screen.</summary>
        private float _scale;
        /// <summary>Stores the native viewport's letterbox offset.</summary>
        private Vector2 _offset;

        /// <summary>Creates a real-artwork pool fixture without starting gameplay or reading persistent data.</summary>
        /// <returns>A frame boundary for graphics initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS") ?? "";
            if (!Directory.Exists(Path.Combine(root, "reanim")))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the read-only original runtime resource directory.");
            }

            yield return null;
            _host = new GameObject("Inactive original pool zombie graphics fixture");
            _host.SetActive(false);
            _owner = _host.AddComponent<PvZGame>();
            _owner._root = root;
            _animations = new ReanimationRenderer(root);
            _owner._animations = _animations;
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 29 }, 7529);
            Assert.That(_owner._game.ChooseDefaultAdventureSeeds(), Is.True);
            _owner._game.ResetAdventureWaveSchedule(100000, false, false);
            _owner.Textures._textures["plantshadow.png"] = null!;
            _presenter = new GamePresenter(_owner);
            _definition = new XmlDocument { XmlResolver = null };
            _definition.LoadXml("<root>" + File.ReadAllText(Path.Combine(root, "reanim", "Zombie.reanim")) + "</root>");
            Assert.That(_animations.TryGetNativeAnimationInfo("Zombie", "anim_swim", out _), Is.True, _animations.LastError);
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _pixels = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            _scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            _offset = new Vector2((Screen.width - 800f * _scale) * .5f, (Screen.height - 600f * _scale) * .5f);
        }

        /// <summary>Releases only resources owned by this original-artwork fixture.</summary>
        /// <returns>A frame boundary for deferred object destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_host != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                _presenter.Dispose();
                _animations.Dispose();
                foreach (var texture in _owner.Textures._textures.Values)
                {
                    if (texture != null)
                    {
                        UnityEngine.Object.Destroy(texture);
                    }
                }

                _owner.Textures._textures.Clear();
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_pixels);
                UnityEngine.Object.Destroy(_host);
            }

            yield return null;
        }

        /// <summary>Checks original normal, cone, and bucket swimmers retain their hats, tube, and whitewater at the native waterline.</summary>
        /// <returns>Production and independently clipped original source captures.</returns>
        [UnityTest]
        public IEnumerator PlainSwimmersKeepOriginalArmorTubeAndWaterTracks()
        {
            foreach (var kind in new[] { ZombieKind.Normal, ZombieKind.Conehead, ZombieKind.Buckethead })
            {
                var zombie = Swimmer(kind, false, false, false);
                yield return Capture(() => Draw(zombie));
                var actual = _pixels.GetPixels32();
                Assert.That(FindSprites(Load("IMAGE_REANIM_ZOMBIE_DUCKYTUBE_INWATER")), Has.Count.EqualTo(1));
                Assert.That(FindSprites(Load("IMAGE_REANIM_ZOMBIE_WHITEWATER1")).Count
                    + FindSprites(Load("IMAGE_REANIM_ZOMBIE_WHITEWATER2")).Count
                    + FindSprites(Load("IMAGE_REANIM_ZOMBIE_WHITEWATER3")).Count, Is.GreaterThan(0));
                if (kind != ZombieKind.Normal)
                {
                    Assert.That(FindSprites(Load(kind == ZombieKind.Conehead ? "IMAGE_REANIM_ZOMBIE_CONE1" : "IMAGE_REANIM_ZOMBIE_BUCKET1")), Has.Count.EqualTo(1));
                }

                Save("pool-swimmer-" + kind + ".png");
                yield return Capture(() => DrawReference(zombie, -40f, false));
                Assert.That(DifferentPixels(actual, _pixels.GetPixels32()), Is.LessThan(20),
                    "Original hats and ignored tube/water tracks must retain the authored track order and PC waterline.");
            }
        }

        /// <summary>Checks eating, small bodies, and chilled capture retain clipped legs and unclipped original tube and hand artwork.</summary>
        /// <returns>Actual color captures and independent neutral silhouette references.</returns>
        [UnityTest]
        public IEnumerator EatingSmallAndChilledSwimmersKeepPerTrackWaterClipping()
        {
            foreach (var sample in new[]
            {
                (Eating: true, Small: false, Chilled: false),
                (Eating: false, Small: true, Chilled: false),
                (Eating: true, Small: true, Chilled: true)
            })
            {
                var zombie = Swimmer(ZombieKind.Buckethead, sample.Eating, sample.Small, sample.Chilled);
                yield return Capture(() => Draw(zombie));
                var actual = _pixels.GetPixels32();
                if (sample.Chilled)
                {
                    Assert.That(_animations.NativeSurfaceCount, Is.GreaterThan(0), "This case must exercise actual combat capture clipping.");
                }

                Save("pool-swimmer-eat-" + sample.Eating + "-small-" + sample.Small + "-cold-" + sample.Chilled + ".png");
                yield return Capture(() => DrawReference(zombie, sample.Small ? -20f : -40f, false));
                if (sample.Chilled)
                {
                    AssertMaskMatches(actual, _pixels.GetPixels32());
                }
                else
                {
                    Assert.That(DifferentPixels(actual, _pixels.GetPixels32()), Is.LessThan(20));
                }
            }
        }

        /// <summary>Checks real pool ambush bodies emerge upwards and keep the native phase-specific water clipping through all fifty ticks.</summary>
        /// <returns>Actual captures at the original rise counters fifty, twenty-five, one, and zero.</returns>
        [UnityTest]
        public IEnumerator PoolAmbushRisesUpThroughOriginalWaterline()
        {
            var zombie = Ambush();
            zombie.PoolState!.SeaweedAge = -1f;
            zombie.PoolState.SplashAge = -1f;
            var counters = new[] { 50, 25, 1, 0 };
            var altitudes = new[] { -150f, -95f, -42f, -40f };
            var previousCount = 0;
            for (var phase = 0; phase < counters.Length; phase++)
            {
                while (zombie.GraveRiseTicks > counters[phase])
                {
                    _owner.RequiredGame.Update(.01f);
                }

                zombie.PresentationPreviousX = zombie.X;
                yield return Capture(() => Draw(zombie));
                var actual = _pixels.GetPixels32();
                var count = VisiblePixels(actual);
                if (phase == 0)
                {
                    Assert.That(count, Is.LessThan(5), "The original minus150 body starts below the pool instead of dropping from the sky.");
                }
                else
                {
                    Assert.That(count, Is.GreaterThan(previousCount), "The authored body must rise upwards into view as its native counter falls.");
                }

                previousCount = count;
                Save("pool-ambush-rise-" + counters[phase] + ".png");
                yield return Capture(() => DrawReference(zombie, altitudes[phase], counters[phase] > 0));
                Assert.That(DifferentPixels(actual, _pixels.GetPixels32()), Is.LessThan(20),
                    "Pool emergence clips every original body track at nativeY+113, then restores the native tube exceptions.");
            }
        }

        /// <summary>Checks all three original seaweed attachments follow their real track transforms while the splash keeps its birth origin.</summary>
        /// <returns>Production attachment captures and an independent original splash draw.</returns>
        [UnityTest]
        public IEnumerator PoolAmbushUsesThreeOriginalSeaweedAnchorsAndFixedSplash()
        {
            var zombie = Ambush();
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X);
            var nativeY = 50f + zombie.Row * 85f;
            _owner.RequiredGame.Update(.25f);
            zombie.X += .4f;
            zombie.PresentationPreviousX = zombie.X;
            yield return Capture(() => Draw(zombie));
            var seaweed = FindSprites(Load("IMAGE_ZOMBIE_SEAWEED"));
            Assert.That(seaweed, Has.Count.EqualTo(3), "PC RiseFromGrave attaches three independent original seaweed particles.");
            var head = zombie.Kind == ZombieKind.Conehead ? "anim_cone" : zombie.Kind == ZombieKind.Buckethead ? "anim_bucket" : "anim_head1";
            var tracks = new[] { head, "Zombie_outerarm_upper", "Zombie_duckytube" };
            var offsets = new[] { new Vector2(head == "anim_head1" ? 30f : 37f, 20f), new Vector2(5f, 5f), new Vector2(77f, 20f) };
            var used = new bool[3];
            for (var attachment = 0; attachment < tracks.Length; attachment++)
            {
                var expected = OriginalOrigin(zombie, -95f) + (Vector2)OriginalTrackMatrix(zombie, tracks[attachment]).MultiplyPoint3x4(offsets[attachment]);
                var nearest = float.MaxValue;
                var chosen = -1;
                for (var index = 0; index < seaweed.Count; index++)
                {
                    if (!used[index])
                    {
                        var center = (seaweed[index].Vertices[0] + seaweed[index].Vertices[2]) * .5f;
                        var logical = new Vector2((center.x - _offset.x) / _scale, (-center.y - _offset.y) / _scale);
                        var distance = Vector2.Distance(logical, expected);
                        if (distance < nearest)
                        {
                            nearest = distance;
                            chosen = index;
                        }
                    }
                }

                Assert.That(nearest, Is.LessThan(9f), "The original emitter radius is at most five pixels around " + tracks[attachment] + ".");
                used[chosen] = true;
            }

            var rings = FindSprites(Load("IMAGE_REANIM_SPLASH_RING"));
            Assert.That(rings.Count, Is.GreaterThan(0));
            var first = rings[0].Vertices[0];
            var opposite = rings[0].Vertices[2];
            Save("pool-ambush-seaweed-and-splash.png");
            yield return Capture(() => _animations.DrawNativePlayback("splash", new Vector2(nativeX + 23f, nativeY + 78f),
                .8f, zombie.PoolState!.SplashAge, null, false, 18f));
            var reference = FindSprites(Load("IMAGE_REANIM_SPLASH_RING"))[0];
            Assert.That(Vector2.Distance(first, reference.Vertices[0]), Is.LessThan(.02f));
            Assert.That(Vector2.Distance(opposite, reference.Vertices[2]), Is.LessThan(.02f), "The splash remains at its birth position when the zombie body moves.");
        }

        /// <summary>Checks an independent original Peashooter-head attachment inherits the parent pool waterline in scene and chilled captures.</summary>
        /// <returns>Original body and head captures at fully underground and partially emerged pool phases.</returns>
        [UnityTest]
        public IEnumerator PlantHeadAttachmentKeepsParentWaterlineInSceneAndCapture()
        {
            var game = _owner.RequiredGame;
            var zombie = (Zombie)Invoke(game, "SpawnAdventureZombie", ZombieKind.Normal, 2, 6f, 0)!;
            Invoke(game, "InitializeAdventurePoolZombie", zombie, 6);
            zombie.ModeHead = 26;
            zombie.PoolState!.SeaweedAge = -1f;
            zombie.PoolState.SplashAge = -1f;
            zombie.PresentationAnimationAge = .35f;
            zombie.PresentationPlantHeadAge = .35f;
            foreach (var remaining in new[] { 50, 25 })
            {
                var altitude = remaining == 50 ? -150f : -95f;
                zombie.GraveRiseTicks = remaining;
                zombie.PoolState.Altitude = altitude;
                foreach (var chilled in new[] { false, true })
                {
                    zombie.ChillRemaining = chilled ? 10f : 0f;
                    yield return Capture(() => Draw(zombie));
                    var actual = _pixels.GetPixels32();
                    if (chilled)
                    {
                        Assert.That(_animations.NativeSurfaceCount, Is.GreaterThan(0));
                    }

                    Save("pool-plant-head-rise-" + remaining + "-cold-" + chilled + ".png");
                    yield return Capture(() => DrawReference(zombie, altitude, true));
                    if (chilled)
                    {
                        AssertMaskMatches(actual, _pixels.GetPixels32());
                    }
                    else
                    {
                        Assert.That(DifferentPixels(actual, _pixels.GetPixels32()), Is.LessThan(20),
                            "The original independent plant head must obey the parent's nativeY+113 waterline.");
                    }
                }
            }
        }

        /// <summary>Creates a real swimmer and advances the original water entrance before sampling a stable authored pose.</summary>
        /// <param name="kind">The original normal or armored kind.</param>
        /// <param name="eating">Whether the body uses its original eating pose.</param>
        /// <param name="small">Whether the original half-scale override applies.</param>
        /// <param name="chilled">Whether combat rendering must use its actual chilled capture path.</param>
        /// <returns>The production-initialized actor at a stable original water position.</returns>
        private Zombie Swimmer(ZombieKind kind, bool eating, bool small, bool chilled)
        {
            _owner.RequiredGame.Zombies.Clear();
            var zombie = (Zombie)Invoke(_owner.RequiredGame, "SpawnAdventureZombie", kind, 2, AdventureZombieNativeRules.GridX(400f), 0)!;
            for (var tick = 0; tick < 41; tick++)
            {
                _owner.RequiredGame.Update(.01f);
            }

            zombie.IsSmall = small;
            zombie.X = AdventureZombieNativeRules.GridX(400f, small ? .5f : 1f);
            zombie.PresentationPreviousX = zombie.X;
            zombie.PoolState!.Altitude = small ? -20f : -40f;
            zombie.PoolState.SplashAge = -1f;
            zombie.Eating = eating;
            zombie.ChillRemaining = chilled ? 10f : 0f;
            _owner.RequiredGame.AdventurePresentationZombieAnimation(zombie);
            zombie.PresentationAnimationAge = .35f;
            return zombie;
        }

        /// <summary>Creates a real final-wave pool ambush and selects an actor inside the camera's horizontal bounds.</summary>
        /// <returns>One original normal or armored pool-emergence actor.</returns>
        private Zombie Ambush()
        {
            _owner.RequiredGame.Zombies.Clear();
            Invoke(_owner.RequiredGame, "SpawnScheduledFinalAmbush");
            foreach (var zombie in _owner.RequiredGame.Zombies)
            {
                if (AdventureZombieNativeRules.NativePositionX(zombie.X) < 640f)
                {
                    return zombie;
                }
            }

            throw new InvalidOperationException("Three unique pool ambush cells must include an actor before the final column.");
        }

        /// <summary>Draws literal original tracks in their authored order with independently specified PC clipping exceptions.</summary>
        /// <param name="zombie">The actor whose original costume and authored animation are compared.</param>
        /// <param name="altitude">The independently expected original altitude.</param>
        /// <param name="rising">Whether the original emergence clips all tracks at nativeY+113.</param>
        private void DrawReference(Zombie zombie, float altitude, bool rising)
        {
            var size = zombie.IsSmall ? .5f : 1f;
            var nativeY = 50f + zombie.Row * 85f;
            var bottom = rising ? nativeY + 113f : nativeY + 120f - 10f * (1f - size) - (zombie.Eating ? 7f : 0f);
            var variant = zombie.Kind == ZombieKind.Conehead ? ZombieVariant.Conehead : zombie.Kind == ZombieKind.Buckethead ? ZombieVariant.Buckethead : ZombieVariant.Normal;
            var clip = zombie.Eating ? "anim_eat" : "anim_swim";
            var previous = SpriteDrawState.Clip;
            foreach (XmlNode track in _definition.SelectNodes("/root/track")!)
            {
                var name = track.SelectSingleNode("name")!.InnerText;
                var decoration = name == "Zombie_duckytube" || name == "Zombie_whitewater" || name == "Zombie_whitewater2";
                if (zombie.ModeHead == 26 && (name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("hair", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    continue;
                }

                if (!decoration && !ReanimationRenderer.IsZombieTrackVisible(name, variant))
                {
                    continue;
                }

                // PC SetupWaterTrack 0x552CC0 leaves whitewater2 ignored even during emergence;
                // 0x53FD50 matches the literal Zombie_innerarm3 name, which has no authored track.
                var ignored = name == "Zombie_whitewater2" || !rising && (name == "Zombie_duckytube" || name == "Zombie_whitewater"
                    || name == "Zombie_outerarm_hand" || name == "Zombie_innerarm3");
                SpriteDrawState.Clip = ignored ? previous : new Rect(_offset.x, _offset.y, 800f * _scale, bottom * _scale);
                _animations.DrawNativePlayback("Zombie", OriginalOrigin(zombie, altitude), size, zombie.PresentationAnimationAge,
                    clip, true, _owner.RequiredGame.AdventurePresentationZombieRate(zombie), trackName => trackName == name);
            }

            if (zombie.ModeHead == 26)
            {
                SpriteDrawState.Clip = new Rect(_offset.x, _offset.y, 800f * _scale, bottom * _scale);
                Assert.That(_animations.DrawNativePlantHeadAttachment(OriginalOrigin(zombie, altitude), size, clip,
                    zombie.PresentationAnimationAge, _owner.RequiredGame.AdventurePresentationZombieRate(zombie),
                    26, zombie.PresentationPlantHeadAge, false), Is.True, _animations.LastError);
            }

            SpriteDrawState.Clip = previous;
        }

        /// <summary>Gets the PC body origin directly from native coordinates and its unscaled overlay and body pivots.</summary>
        /// <param name="zombie">The original actor and native row.</param>
        /// <param name="altitude">The independently selected original altitude.</param>
        /// <returns>The literal native body origin before the shared viewport transform.</returns>
        private static Vector2 OriginalOrigin(Zombie zombie, float altitude)
        {
            var size = zombie.IsSmall ? .5f : 1f;
            return new Vector2(AdventureZombieNativeRules.NativePositionX(zombie.X, size) + 15f + 30f * (1f - size),
                50f + zombie.Row * 85f - 8f + 120f * (1f - size) - altitude);
        }

        /// <summary>Samples the original XML's affine track transform independently for the seaweed attachment oracle.</summary>
        /// <param name="zombie">The source actor supplying authored clip time and rate.</param>
        /// <param name="name">The original attachment track name.</param>
        /// <returns>The interpolated track matrix in original body coordinates.</returns>
        private Matrix4x4 OriginalTrackMatrix(Zombie zombie, string name)
        {
            Assert.That(_animations.TryGetNativeAnimationInfo("Zombie", "anim_swim", out var clip), Is.True);
            var phase = zombie.PresentationAnimationAge * _owner.RequiredGame.AdventurePresentationZombieRate(zombie) / clip.Count;
            phase -= Mathf.Floor(phase);
            var frame = clip.Start + (clip.Count - 1) * phase;
            var before = Mathf.FloorToInt(frame);
            var values = new float[] { 0f, 0f, 0f, 0f, 1f, 1f };
            var first = new float[6];
            var second = new float[6];
            var fields = new[] { "x", "y", "kx", "ky", "sx", "sy" };
            var track = _definition.SelectSingleNode("/root/track[name='" + name + "']")!;
            var index = 0;
            foreach (XmlNode sample in track.SelectNodes("t")!)
            {
                for (var field = 0; field < fields.Length; field++)
                {
                    var node = sample.SelectSingleNode(fields[field]);
                    if (node != null)
                    {
                        values[field] = float.Parse(node.InnerText, CultureInfo.InvariantCulture);
                    }
                }

                if (index == before)
                {
                    Array.Copy(values, first, 6);
                }
                else if (index == before + 1)
                {
                    Array.Copy(values, second, 6);
                    break;
                }

                index++;
            }

            for (var field = 0; field < values.Length; field++)
            {
                values[field] = Mathf.Lerp(first[field], second[field], frame - before);
            }

            var matrix = Matrix4x4.identity;
            matrix.m00 = values[4] * Mathf.Cos(values[2] * Mathf.Deg2Rad);
            matrix.m10 = values[4] * Mathf.Sin(values[2] * Mathf.Deg2Rad);
            matrix.m01 = -values[5] * Mathf.Sin(values[3] * Mathf.Deg2Rad);
            matrix.m11 = values[5] * Mathf.Cos(values[3] * Mathf.Deg2Rad);
            matrix.m03 = values[0];
            matrix.m13 = values[1];
            return matrix;
        }

        /// <summary>Draws the actual production board actor with its original body and attached pool effects.</summary>
        /// <param name="zombie">The actor being presented.</param>
        private void Draw(Zombie zombie)
        {
            Invoke(_presenter, "DrawAdventureZombie", zombie);
        }

        /// <summary>Captures one production or literal-source draw over white using the actual scene camera.</summary>
        /// <param name="draw">The draw operation being compared.</param>
        /// <returns>Frame boundaries allowing scene meshes and Canvas geometry to reach the camera.</returns>
        private IEnumerator Capture(Action draw)
        {
            ObjectSpriteRenderer.BeginSceneFrame();
            CanvasUI.BeginFrame();
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(_offset.x, _offset.y, 0f), Quaternion.identity, new Vector3(_scale, _scale, 1f));
            CanvasUI.Color = Color.white;
            CanvasUI.DrawTexture(new Rect(0f, 0f, 800f, 600f), Texture2D.whiteTexture);
            _animations.BeginObjectFrame();
            Invoke(_presenter, "BeginAdventurePresentation");
            try
            {
                draw();
            }
            finally
            {
                Invoke(_presenter, "EndAdventurePresentation");
                _animations.EndObjectFrame();
                CanvasUI.EndFrame();
            }

            yield return null;
            yield return null;
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = _target;
                _pixels.ReadPixels(new Rect(0f, 0f, _target.width, _target.height), 0, 0);
                _pixels.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        /// <summary>Loads an already indexed original image through the production texture cache.</summary>
        /// <param name="image">The exact original image identifier.</param>
        /// <returns>The production-decoded original texture.</returns>
        private Texture2D Load(string image)
        {
            return (Texture2D)Invoke(_animations, "LoadTexture", image)!;
        }

        /// <summary>Finds live scene quads using an exact original texture after the last completed presentation.</summary>
        /// <param name="texture">The original source texture.</param>
        /// <returns>The ordered actual mesh objects using that image.</returns>
        private List<ObjectSprite> FindSprites(Texture2D texture)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", flags)!.GetValue(_animations)!;
            var sprites = (List<ObjectSprite>)typeof(ObjectSpriteRenderer).GetField("_sprites", flags)!.GetValue(pool)!;
            var result = new List<ObjectSprite>();
            for (var index = 0; index < pool.SubmittedObjectCount; index++)
            {
                if (ReferenceEquals(sprites[index].Properties.GetTexture(Shader.PropertyToID("_MainTex")), texture))
                {
                    result.Add(sprites[index]);
                }
            }

            return result;
        }

        /// <summary>Counts actual nonwhite actor pixels in a completed original-artwork frame.</summary>
        /// <param name="pixels">The completed camera frame.</param>
        /// <returns>The number of visible actor pixels.</returns>
        private int VisiblePixels(Color32[] pixels)
        {
            var result = 0;
            for (var y = Mathf.CeilToInt(_offset.y); y < Mathf.FloorToInt(_offset.y + 600f * _scale); y++)
            {
                for (var x = Mathf.CeilToInt(_offset.x); x < Mathf.FloorToInt(_offset.x + 800f * _scale); x++)
                {
                    var pixel = pixels[y * _pixels.width + x];
                    result += 765 - pixel.r - pixel.g - pixel.b > 24 ? 1 : 0;
                }
            }

            return result;
        }

        /// <summary>Counts visibly different RGB pixels in actual source and production frames.</summary>
        /// <param name="first">The production frame.</param>
        /// <param name="second">The original source reference.</param>
        /// <returns>The number of pixels exceeding channel-rounding tolerance.</returns>
        private static int DifferentPixels(Color32[] first, Color32[] second)
        {
            var result = 0;
            for (var index = 0; index < first.Length; index++)
            {
                result += Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                    + Math.Abs(first[index].b - second[index].b) > 12 ? 1 : 0;
            }

            return result;
        }

        /// <summary>Rejects leaked underwater limbs while allowing a one-pixel color-tint silhouette edge around a chilled capture.</summary>
        /// <param name="actual">The chilled production camera pixels.</param>
        /// <param name="expected">The independently clipped neutral original artwork.</param>
        private void AssertMaskMatches(Color32[] actual, Color32[] expected)
        {
            var outside = 0;
            for (var y = 1; y < _pixels.height - 1; y++)
            {
                for (var x = 1; x < _pixels.width - 1; x++)
                {
                    var index = y * _pixels.width + x;
                    if (765 - actual[index].r - actual[index].g - actual[index].b <= 24)
                    {
                        continue;
                    }

                    var near = false;
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            var pixel = expected[index + dy * _pixels.width + dx];
                            near |= 765 - pixel.r - pixel.g - pixel.b > 24;
                        }
                    }

                    outside += near ? 0 : 1;
                }
            }

            Assert.That(VisiblePixels(actual), Is.GreaterThanOrEqualTo(VisiblePixels(expected) * .9f), "Chilled capture must retain the original tube and ignored hand artwork.");
            Assert.That(outside, Is.LessThan(Math.Max(50, VisiblePixels(actual) / 100)), "Chilled capture must not reveal clipped underwater legs.");
        }

        /// <summary>Writes optional original-artwork diagnostics only to the selected ignored output directory.</summary>
        /// <param name="name">The diagnostic PNG filename.</param>
        private void Save(string name)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name), _pixels.EncodeToPNG());
            }
        }

        /// <summary>Invokes a production operation without adding a test-only runtime API.</summary>
        /// <param name="target">The existing presenter, renderer, or simulation.</param>
        /// <param name="name">The private production operation name.</param>
        /// <param name="arguments">The operation's normal arguments.</param>
        /// <returns>The production operation's result, when present.</returns>
        private static object? Invoke(object target, string name, params object[] arguments)
        {
            return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, arguments);
        }
    }
}
