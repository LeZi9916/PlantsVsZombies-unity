#nullable enable
using PlantsVsZombies.UI.BitmapFonts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.Game
{
    /// <summary>Runs the original startup warm-up, loading bar and title-screen timeline.</summary>
    internal sealed class OriginalLoadingController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose assets and audio the startup sequence prepares.</param>
        internal OriginalLoadingController(PvZGame owner)
        {
            _owner = owner;
        }

        // Local Chinese GOTY EXE: TitleScreen Draw 0x4A3280, Update 0x4A3940.
        // This executable differs from the reference C# port (529 rather than 523; trigger .54 rather than .52).
        /// <summary>Stores original loading active,  original loading first draw,  original startup warmup ready,  original show loading for capture.</summary>
        internal bool _originalLoadingActive, _originalLoadingFirstDraw, _originalStartupWarmupReady, _originalShowLoadingForCapture;
        /// <summary>Stores original title ticks,  original title screen ticks.</summary>
        internal int _originalTitleTicks, _originalTitleScreenTicks = -1;
        /// <summary>Stores original title remainder.</summary>
        private double _originalTitleRemainder;
        /// <summary>Stores original load width,  original load velocity,  original load start progress,  original previous progress.</summary>
        internal float _originalLoadWidth, _originalLoadVelocity = 0.2f, _originalLoadStartProgress, _originalPreviousProgress;
        /// <summary>Stores original warmup tasks,  original warmup completed.</summary>
        private int _originalWarmupTasks, _originalWarmupCompleted;
        /// <summary>Stores original load triggers.</summary>
        internal readonly float[] _originalLoadTriggers =
        {
            0.11f,
            0.32f,
            0.54f,
            0.72f,
            0.906f
        };
        /// <summary>Stores original load sprout ages.</summary>
        internal readonly float[] _originalLoadSproutAges =
        {
            -1f,
            -1f,
            -1f,
            -1f,
            -1f
        };
        /// <summary>Stores original title textures.</summary>
        internal readonly Dictionary<string, Texture2D> _originalTitleTextures = new Dictionary<string, Texture2D>();
        /// <summary>Stores original loading font.</summary>
        internal CanvasBitmapFont _originalLoadingFont;
        /// <summary>Stores original sprout info,  original head info.</summary>
        internal global::PlantsVsZombies.Reanimation.AnimationInfo _originalSproutInfo, _originalHeadInfo;
        /// <summary>Gets or sets original loading active.</summary>
        public bool OriginalLoadingActive
        {
            get
            {
                return _originalLoadingActive;
            }
        }

        /// <summary>Gets or sets original loading ready.</summary>
        public bool OriginalLoadingReady
        {
            get
            {
                return _originalStartupWarmupReady && _owner.Sounds._audioLoadingFinished && _owner.Music._dynamicAudioReady;
            }
        }

        /// <summary>Gets or sets original loading progress.</summary>
        public float OriginalLoadingProgress
        {
            get
            {
                var warm = _originalStartupWarmupReady ? 1f : _originalWarmupCompleted / (float)Math.Max(1, _originalWarmupTasks);
                return Mathf.Clamp01((warm + _owner.Music.DynamicAudioProgress + (_owner.Sounds._audioLoadingFinished ? 1f : 0f)) / 3f);
            }
        }

        /// <summary>Performs initialize original loading.</summary>
        internal void InitializeOriginalLoading()
        {
            _originalLoadingActive = true;
            _originalLoadingFirstDraw = false;
            _originalShowLoadingForCapture = Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-show-loading") >= 0;
            _originalTitleTicks = 0;
            _originalTitleScreenTicks = -1;
            _originalTitleRemainder = 0;
            _originalLoadWidth = 0;
            _originalLoadVelocity = 0.2f;
            _originalLoadStartProgress = _originalPreviousProgress = 0;
            for (var i = 0; i < _originalLoadSproutAges.Length; i++)
            {
                _originalLoadSproutAges[i] = -1;
            }

            foreach (string key in new[]
            {
                "title",
                "popcap",
                "logo",
                "grass",
                "dirt",
                "sodroll"
            }

            )
            {
                var texture = Resources.Load<Texture2D>("PvZLoading/" + key);
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _originalTitleTextures[key] = texture;
                }
                else
                {
                    Debug.LogWarning("Original title texture is missing: " + key);
                }
            }

            _originalLoadingFont = MenuController.LoadBitmapFont("BrianneTod16");
            _owner.StartCoroutine(PrewarmOriginalStartup());
        }

        /// <summary>Performs prewarm original startup.</summary>
        /// <returns>The resulting prewarm original startup.</returns>
        private IEnumerator PrewarmOriginalStartup()
        {
            // Warm the real selector/first board assets and parsed original animation tracks, yielding between jobs.
            var images = new List<string>();
            var imageRoot = Path.Combine(_owner._root, "images");
            if (Directory.Exists(imageRoot))
            {
                foreach (string path in Directory.GetFiles(imageRoot))
                {
                    var name = Path.GetFileName(path);
                    if (name.StartsWith("SelectorScreen", StringComparison.OrdinalIgnoreCase) || name.StartsWith("SeedChooser", StringComparison.OrdinalIgnoreCase) || name.StartsWith("SeedBank", StringComparison.OrdinalIgnoreCase) || name.StartsWith("background", StringComparison.OrdinalIgnoreCase))
                    {
                        var extension = Path.GetExtension(name);
                        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
                        {
                            images.Add(name);
                        }
                    }
                }
            }

            string[] tracks =
            {
                "LoadBar_sprout",
                "LoadBar_Zombiehead",
                "Peashooter",
                "SunFlower",
                "Wallnut",
                "PotatoMine",
                "SnowPea",
                "Repeater",
                "Zombie",
                "LawnMower"
            };
            _originalWarmupTasks = images.Count + tracks.Length;
            foreach (string image in images)
            {
                _owner.Textures.Load(image);
                _originalWarmupCompleted++;
                yield return null;
            }

            foreach (string track in tracks)
            {
                global::PlantsVsZombies.Reanimation.AnimationInfo info;
                if (_owner._animations.TryGetAnimationInfo(track, null, out info))
                {
                    if (track == "LoadBar_sprout")
                    {
                        _originalSproutInfo = info;
                    }
                    else if (track == "LoadBar_Zombiehead")
                    {
                        _originalHeadInfo = info;
                    }
                }
                else if (track == "LoadBar_sprout" || track == "LoadBar_Zombiehead")
                {
                    Debug.LogError("Missing original startup animation: " + track + ": " + _owner._animations.LastError);
                }

                _originalWarmupCompleted++;
                yield return null;
            }

            _originalStartupWarmupReady = true;
        }

        /// <summary>Performs update original loading.</summary>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting update original loading.</returns>
        internal bool UpdateOriginalLoading(float dt)
        {
            if (!_originalLoadingActive)
            {
                return false;
            }

            if (_owner._smokeMode && !_originalShowLoadingForCapture && OriginalLoadingReady)
            {
                CompleteOriginalLoading(false);
                return false;
            }

            if (!_originalLoadingFirstDraw)
            {
                return true;
            }

            _originalTitleRemainder += Math.Max(0f, dt);
            while (_originalTitleRemainder + 0.00000001 >= 0.01)
            {
                _originalTitleRemainder -= 0.01;
                UpdateOriginalTitleTick();
            }

            return true;
        }

        /// <summary>Performs update original title tick.</summary>
        private void UpdateOriginalTitleTick()
        {
            _originalTitleTicks++;
            // Original PopCap phase lasts 400 ticks in this local executable; the title settles over another 100.
            if (_originalTitleTicks <= 400)
            {
                return;
            }

            if (_originalTitleScreenTicks < 0)
            {
                _originalTitleScreenTicks = 100;
                var progress = OriginalLoadingProgress;
                var totalEstimate = progress > 0.000001f ? _originalTitleTicks / progress : 3000f;
                _originalLoadVelocity = 314f / Mathf.Clamp(totalEstimate * (1f - progress), 100f, 3000f);
                _originalLoadStartProgress = Math.Min(progress, 0.9f);
            }

            if (_originalTitleScreenTicks > 0)
            {
                _originalTitleScreenTicks--;
                return;
            }

            var previousWidth = _originalLoadWidth;
            _originalLoadWidth += _originalLoadVelocity;
            _originalLoadWidth = Math.Min(_originalLoadWidth, 314f * (OriginalLoadingReady ? 1f : 0.99f));
            var p = Mathf.Clamp01((OriginalLoadingProgress - _originalLoadStartProgress) / Math.Max(0.00001f, 1f - _originalLoadStartProgress));
            if (p > _originalPreviousProgress + 0.01f || OriginalLoadingReady)
            {
                // Native spring toward real loading progress, with the reference TodCurve EaseIn target.
                var difference = p * p * 314f - _originalLoadWidth;
                var acceleration = OriginalLoadingReady ? 0.0001f : Mathf.Lerp(0.0001f, 0.00001f, p);
                _originalLoadVelocity = Mathf.Clamp(_originalLoadVelocity + difference * Math.Abs(difference) * acceleration, Mathf.Lerp(0.2f, 0.01f, p), 1f);
                _originalPreviousProgress = p;
            }

            for (var i = 0; i < _originalLoadTriggers.Length; i++)
            {
                if (_originalLoadSproutAges[i] >= 0f)
                {
                    _originalLoadSproutAges[i] += 0.01f;
                }

                var at = _originalLoadTriggers[i] * 314f;
                if (previousWidth < at && _originalLoadWidth >= at && _originalLoadSproutAges[i] < 0f)
                {
                    _originalLoadSproutAges[i] = 0f;
                    _owner.Sounds.PlayPresentationSound("loadingbar_flower");
                    if (i == 4)
                    {
                        _owner.Sounds.PlayPresentationSound("loadingbar_zombie");
                    }
                }
            }
        }

        /// <summary>Performs original title curve.</summary>
        /// <param name = "start">The start.</param>
        /// <param name = "end">The end.</param>
        /// <param name = "value">The value.</param>
        /// <param name = "from">The from.</param>
        /// <param name = "to">The to.</param>
        /// <param name = "bounce">The bounce.</param>
        /// <returns>The resulting original title curve.</returns>
        internal static float OriginalTitleCurve(int start, int end, int value, float from, float to, bool bounce)
        {
            var t = Mathf.Clamp01((value - start) / (float)(end - start));
            var n = bounce ? 1f - Math.Abs(1f - 2f * t) : t * t;
            var result = from + (to - from) * n;
            return (int)(result + (result > 0f ? .5f : -.5f)); // PC TodAnimateCurve integer rounding.
        }

        /// <summary>Performs complete original loading.</summary>
        /// <param name = "clicked">The clicked.</param>
        internal void CompleteOriginalLoading(bool clicked)
        {
            if (!OriginalLoadingReady)
            {
                return;
            }

            _originalLoadingActive = false;
            if (clicked)
            {
                _owner.Sounds.PlayPresentationSound("buttonclick");
            }
        }

        /// <summary>Performs dispose original loading.</summary>
        internal void DisposeOriginalLoading()
        {
            if (_originalLoadingFont != null)
            {
                _originalLoadingFont.Dispose();
                _originalLoadingFont = null;
            }

            _originalTitleTextures.Clear();
        }
    }
}
