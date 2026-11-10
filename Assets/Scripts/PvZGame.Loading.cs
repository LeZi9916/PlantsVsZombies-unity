using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PvZReplica.Reanimation;
using PvZReplica.UI.BitmapFonts;
using UnityEngine;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        // Local Chinese GOTY EXE: TitleScreen Draw 0x4A3280, Update 0x4A3940.
        // This executable differs from the reference C# port (529 rather than 523; trigger .54 rather than .52).
        private bool originalLoadingActive, originalLoadingFirstDraw, originalStartupWarmupReady, originalShowLoadingForCapture;
        private int originalTitleTicks, originalTitleScreenTicks = -1;
        private double originalTitleRemainder;
        private float originalLoadWidth, originalLoadVelocity = 0.2f, originalLoadStartProgress, originalPreviousProgress;
        private int originalWarmupTasks, originalWarmupCompleted;
        private readonly float[] originalLoadTriggers = { 0.11f, 0.32f, 0.54f, 0.72f, 0.906f };
        private readonly float[] originalLoadSproutAges = { -1f, -1f, -1f, -1f, -1f };
        private readonly Dictionary<string, Texture2D> originalTitleTextures = new Dictionary<string, Texture2D>();
        private ImGuiBitmapFont originalLoadingFont;
        private ReanimationRenderer.AnimationInfo originalSproutInfo, originalHeadInfo;
        public bool OriginalLoadingActive { get { return originalLoadingActive; } }
        public bool OriginalLoadingReady { get { return originalStartupWarmupReady && audioLoadingFinished && dynamicAudioReady; } }
        public float OriginalLoadingProgress
        {
            get
            {
                float warm = originalStartupWarmupReady ? 1f : originalWarmupCompleted / (float)Math.Max(1, originalWarmupTasks);
                return Mathf.Clamp01((warm + DynamicAudioProgress + (audioLoadingFinished ? 1f : 0f)) / 3f);
            }
        }
        private void InitializeOriginalLoading()
        {
            originalLoadingActive = true; originalLoadingFirstDraw = false;
            originalShowLoadingForCapture = Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-show-loading") >= 0;
            originalTitleTicks = 0; originalTitleScreenTicks = -1; originalTitleRemainder = 0;
            originalLoadWidth = 0; originalLoadVelocity = 0.2f; originalLoadStartProgress = originalPreviousProgress = 0;
            for (int i = 0; i < originalLoadSproutAges.Length; i++) originalLoadSproutAges[i] = -1;
            foreach (string key in new[] { "title", "popcap", "logo", "grass", "dirt", "sodroll" })
            {
                var texture = Resources.Load<Texture2D>("PvZLoading/" + key);
                if (texture != null) { texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp; originalTitleTextures[key] = texture; }
                else Debug.LogWarning("Original title texture is missing: " + key);
            }
            originalLoadingFont = LoadBitmapFont("BrianneTod16");
            StartCoroutine(PrewarmOriginalStartup());
        }
        private IEnumerator PrewarmOriginalStartup()
        {
            // Warm the real selector/first board assets and parsed original animation tracks, yielding between jobs.
            var images = new List<string>();
            string imageRoot = Path.Combine(root, "images");
            if (Directory.Exists(imageRoot))
                foreach (string path in Directory.GetFiles(imageRoot))
                {
                    string name = Path.GetFileName(path);
                    if (name.StartsWith("SelectorScreen", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("SeedChooser", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("SeedBank", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("background", StringComparison.OrdinalIgnoreCase))
                    {
                        string extension = Path.GetExtension(name);
                        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)) images.Add(name);
                    }
                }
            string[] tracks = { "LoadBar_sprout", "LoadBar_Zombiehead", "Peashooter", "SunFlower", "Wallnut", "PotatoMine", "SnowPea", "Repeater", "Zombie", "LawnMower" };
            originalWarmupTasks = images.Count + tracks.Length;
            foreach (string image in images) { Texture(image); originalWarmupCompleted++; yield return null; }
            foreach (string track in tracks)
            {
                ReanimationRenderer.AnimationInfo info;
                if (animations.TryGetAnimationInfo(track, null, out info))
                {
                    if (track == "LoadBar_sprout") originalSproutInfo = info;
                    else if (track == "LoadBar_Zombiehead") originalHeadInfo = info;
                }
                else if (track == "LoadBar_sprout" || track == "LoadBar_Zombiehead")
                    Debug.LogError("Missing original startup animation: " + track + ": " + animations.LastError);
                originalWarmupCompleted++; yield return null;
            }
            originalStartupWarmupReady = true;
        }
        private bool UpdateOriginalLoading(float dt)
        {
            if (!originalLoadingActive) return false;
            if (smokeMode && !originalShowLoadingForCapture && OriginalLoadingReady)
            { CompleteOriginalLoading(false); return false; }
            if (!originalLoadingFirstDraw) return true;
            originalTitleRemainder += Math.Max(0f, dt);
            while (originalTitleRemainder + 0.00000001 >= 0.01)
            {
                originalTitleRemainder -= 0.01;
                UpdateOriginalTitleTick();
            }
            return true;
        }
        private void UpdateOriginalTitleTick()
        {
            originalTitleTicks++;
            // Original PopCap phase lasts 400 ticks in this local executable; the title settles over another 100.
            if (originalTitleTicks <= 400) return;
            if (originalTitleScreenTicks < 0)
            {
                originalTitleScreenTicks = 100;
                float progress = OriginalLoadingProgress;
                float totalEstimate = progress > 0.000001f ? originalTitleTicks / progress : 3000f;
                originalLoadVelocity = 314f / Mathf.Clamp(totalEstimate * (1f - progress), 100f, 3000f);
                originalLoadStartProgress = Math.Min(progress, 0.9f);
            }
            if (originalTitleScreenTicks > 0) { originalTitleScreenTicks--; return; }
            float previousWidth = originalLoadWidth;
            originalLoadWidth += originalLoadVelocity;
            originalLoadWidth = Math.Min(originalLoadWidth, 314f * (OriginalLoadingReady ? 1f : 0.99f));
            float p = Mathf.Clamp01((OriginalLoadingProgress - originalLoadStartProgress) / Math.Max(0.00001f, 1f - originalLoadStartProgress));
            if (p > originalPreviousProgress + 0.01f || OriginalLoadingReady)
            {
                // Native spring toward real loading progress, with the reference TodCurve EaseIn target.
                float difference = p * p * 314f - originalLoadWidth;
                float acceleration = OriginalLoadingReady ? 0.0001f : Mathf.Lerp(0.0001f, 0.00001f, p);
                originalLoadVelocity = Mathf.Clamp(originalLoadVelocity + difference * Math.Abs(difference) * acceleration,
                    Mathf.Lerp(0.2f, 0.01f, p), 1f);
                originalPreviousProgress = p;
            }
            for (int i = 0; i < originalLoadTriggers.Length; i++)
            {
                if (originalLoadSproutAges[i] >= 0f) originalLoadSproutAges[i] += 0.01f;
                float at = originalLoadTriggers[i] * 314f;
                if (previousWidth < at && originalLoadWidth >= at && originalLoadSproutAges[i] < 0f)
                {
                    originalLoadSproutAges[i] = 0f;
                    PlayPresentationSound("loadingbar_flower");
                    if (i == 4) PlayPresentationSound("loadingbar_zombie");
                }
            }
        }
        private bool DrawOriginalLoading()
        {
            if (!originalLoadingActive) return false;
            Event e = Event.current;
            Vector2 screenPointer = e.mousePosition;
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.identity; GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            float scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            float ox = (Screen.width - 800f * scale) * 0.5f, oy = (Screen.height - 600f * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            if (e.type == EventType.Repaint) originalLoadingFirstDraw = true;
            if (originalTitleTicks <= 400)
            {
                float alpha = originalTitleTicks < 50 ? originalTitleTicks / 50f : originalTitleTicks > 350 ? (400 - originalTitleTicks) / 50f : 1f;
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(alpha)); DrawOriginalTitleTexture("popcap", new Rect(250, 150, 300, 300));
            }
            else
            {
                DrawOriginalTitleTexture("title", new Rect(0, 0, 800, 600));
                int ticks = Math.Max(0, originalTitleScreenTicks);
                float logoY = ticks > 60 ? OriginalTitleCurve(100, 60, ticks, -150, 10, false) : OriginalTitleCurve(60, 50, ticks, 10, 15, true);
                DrawOriginalTitleTexture("logo", new Rect(50, logoY, 700, 116));
                float buttonY = ticks > 10 ? OriginalTitleCurve(60, 10, ticks, 650, 534, false) : OriginalTitleCurve(10, 0, ticks, 534, 529, true);
                float grassY = buttonY - 17f;
                DrawOriginalTitleTexture("dirt", new Rect(244, grassY + 18, 321, 53));
                // PC 4A3777 truncates width before ClipRect. Crop UVs without stretching
                // or changing the IMGUI clip origin, preserving the native 314x33 pixels.
                DrawOriginalLoadingGrass(240f, grassY, originalLoadWidth);
                if (originalLoadWidth < 314f)
                {
                    float travel = originalLoadWidth * .94f;
                    float capScale = Mathf.Lerp(1f, .5f, originalLoadWidth / 314f);
                    float x = 251f + travel, y = grassY - 3f - 35f * capScale + 35f;
                    Texture2D cap;
                    if (originalTitleTextures.TryGetValue("sodroll", out cap) && cap != null)
                    {
                        Matrix4x4 beforeCap = GUI.matrix;
                        // Literal PC TodScaleRotateTransformMatrix, with its native image origin.
                        GUI.matrix = beforeCap * Matrix4x4.TRS(new Vector3(x,y,0),
                            Quaternion.Euler(0,0,-travel * 2f), new Vector3(capScale,capScale,1));
                        GUI.DrawTexture(new Rect(-cap.width * .5f,-cap.height * .5f,cap.width,cap.height), cap, ScaleMode.StretchToFill, true);
                        GUI.matrix = beforeCap;
                    }
                }
                for (int i = 0; i < originalLoadSproutAges.Length; i++) DrawOriginalLoadSprout(i);
                bool ready = OriginalLoadingReady; // PC enables the start label on real completion, independently of bar smoothing.
                Rect button = new Rect(240, buttonY, 314, 50);
                Vector2 pointer = new Vector2((screenPointer.x - ox) / scale, (screenPointer.y - oy) / scale);
                Color label = ready && button.Contains(pointer) ? new Color32(250, 90, 15, 255) : new Color32(218, 184, 33, 255);
                string text = ready ? "点击开始！" : "读取中…";
                float baseline = buttonY + (50f + (originalLoadingFont == null ? 15f : originalLoadingFont.Data.Base)) * 0.5f - 1f;
                if (!DrawBitmap(originalLoadingFont, text, 397, baseline, 314, label, true))
                    Label(button, text, 16, label, TextAnchor.UpperCenter, false);
                // The explicit screenshot scenario owns its start click after the final capture.
                if (ready && !(smokeMode && originalShowLoadingForCapture) && e.type == EventType.MouseUp && e.button == 0 && button.Contains(pointer))
                { CompleteOriginalLoading(true); e.Use(); }
            }
            GUI.color = oldColor; GUI.matrix = oldMatrix;
            return true;
        }
        private static float OriginalTitleCurve(int start, int end, int value, float from, float to, bool bounce)
        {
            float t = Mathf.Clamp01((value - start) / (float)(end - start));
            float n = bounce ? 1f - Math.Abs(1f - 2f * t) : t * t;
            float result = from + (to - from) * n;
            return (int)(result + (result > 0f ? .5f : -.5f)); // PC TodAnimateCurve integer rounding.
        }
        private void DrawOriginalLoadSprout(int index)
        {
            if (originalLoadSproutAges[index] < 0f) return;
            bool head = index == 4;
            var info = head ? originalHeadInfo : originalSproutInfo;
            if (info.FrameCount < 1 || info.FramesPerSecond <= 0f) return;
            float x = 225f + originalLoadTriggers[index] * 314f - (head ? 20f : 0f);
            float y = index == 2 ? 506f : 511f;
            float sx = index == 1 || index == 3 ? -1f : index == 2 ? 1.1f : 1f;
            float sy = index == 2 ? 1.3f : 1f;
            float seconds = originalLoadSproutAges[index];
            Matrix4x4 before = GUI.matrix;
            GUI.matrix = before * Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(sx, sy, 1));
            // PC creates the full 18-frame definition, rate 18, PlayOnceAndHold.
            // Native playback owns frame normalization; do not convert/truncate it a second time.
            animations.DrawNativeDefinition(head ? "LoadBar_Zombiehead" : "LoadBar_sprout", Vector2.zero, 1f, seconds, false, 18f);
            GUI.matrix = before;
        }
        private void DrawOriginalLoadingGrass(float x, float y, float width)
        {
            Texture2D grass;
            if (!originalTitleTextures.TryGetValue("grass",out grass) || grass == null) return;
            int pixels = Math.Max(0, Math.Min(314, (int)width));
            if (pixels == 0) return;
            GUI.DrawTextureWithTexCoords(new Rect(x,y,pixels,grass.height), grass,
                new Rect(0,0,pixels / (float)grass.width,1), true);
        }
        private void DrawOriginalTitleTexture(string key, Rect rect)
        { Texture2D image; if (originalTitleTextures.TryGetValue(key, out image) && image != null) GUI.DrawTexture(rect, image, ScaleMode.StretchToFill, true); }
        private void CompleteOriginalLoading(bool clicked)
        { if (!OriginalLoadingReady) return; originalLoadingActive = false; if (clicked) PlayPresentationSound("buttonclick"); }
        private void DisposeOriginalLoading()
        { if (originalLoadingFont != null) { originalLoadingFont.Dispose(); originalLoadingFont = null; } originalTitleTextures.Clear(); }
    }
}
