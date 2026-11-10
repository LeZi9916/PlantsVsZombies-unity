using System;
using System.Collections.Generic;
using System.IO;
using PvZReplica.Waves;
using PvZReplica.UI.BitmapFonts;
using UnityEngine;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private GameSimulation waveNoticeGame;
        private float hugeWaveNoticeStart = -1f, finalWaveNoticeStart = -1f;
        private AdventureWaveTextAnimation hugeWaveTextAnimation;
        private string hugeWaveText;
        private readonly List<BitmapTextLayout> hugeWaveGlyphs = new List<BitmapTextLayout>();
        private float hugeWaveTextWidth;
        private Reanimation.ReanimationRenderer bundledWaveAnimations;
        private string bundledWaveRoot;
        private void InitializeAdventureWaveNotices()
        {
            if (waveNoticeGame != null) waveNoticeGame.WaveScheduleEventRaised -= HandleAdventureWaveNotice;
            waveNoticeGame = game; hugeWaveNoticeStart = finalWaveNoticeStart = -1f;
            if (waveNoticeGame != null) waveNoticeGame.WaveScheduleEventRaised += HandleAdventureWaveNotice;
        }
        private void HandleAdventureWaveNotice(WaveScheduleEvent e)
        {
            if (waveNoticeGame != game) return;
            if (e.Kind == WaveScheduleEventKind.HugeWaveWarning) hugeWaveNoticeStart = game.Elapsed;
            else if (e.Kind == WaveScheduleEventKind.HugeWaveCleared) hugeWaveNoticeStart = -1f;
            else if (e.Kind == WaveScheduleEventKind.FinalWaveApproaching) finalWaveNoticeStart = game.Elapsed;
        }
        private void DrawAdventureWaveNotices()
        {
            if (game == null || !game.IsAdventure || choosingSeeds || AdventureIntroActive) return;
            if (waveNoticeGame != game) InitializeAdventureWaveNotices();
            if (hugeWaveNoticeStart >= 0f)
            {
                int age = Math.Max(0, (int)Math.Round((game.Elapsed - hugeWaveNoticeStart) * 100d));
                if (age < AdventureWaveNoticeRules.HugeDuration) DrawOriginalHugeWaveText(age);
            }
            if (finalWaveNoticeStart >= 0f)
            {
                float age = Math.Max(0, game.Elapsed - finalWaveNoticeStart);
                var renderer = OriginalFinalWaveRenderer();
                Reanimation.ReanimationRenderer.NativeAnimationInfo info;
                if (renderer != null && renderer.TryGetNativeAnimationInfo("FinalWave", null, out info) && age < info.Count / info.SourceRate)
                    renderer.DrawNativeDefinition("FinalWave", new Vector2(0, 30), 1f, age, false);
            }
        }
        private string EnsureOriginalWaveFiles()
        {
            if (bundledWaveRoot != null) return bundledWaveRoot;
            var text = Resources.Load<TextAsset>("PvZWaveNotices/TextFadeOn");
            var final = Resources.Load<TextAsset>("PvZWaveNotices/FinalWave");
            var image = Resources.Load<TextAsset>("PvZWaveNotices/FinalWaveImage");
            if (text == null || final == null || image == null) return null;
            // Existing renderer consumes original disk definitions. Materialize ONLY these byte-identical
            // bundled originals in our own cache; never alter StreamingAssets or another renderer's index.
            string cache = Path.Combine(Application.temporaryCachePath, "pvz-original-wave-notices-20261110");
            string reanim = Path.Combine(cache, "reanim"); Directory.CreateDirectory(reanim);
            File.WriteAllBytes(Path.Combine(reanim, "TextFadeOn.reanim"), text.bytes);
            File.WriteAllBytes(Path.Combine(reanim, "FinalWave.reanim"), final.bytes);
            File.WriteAllBytes(Path.Combine(reanim, "FinalWave.png"), image.bytes);
            bundledWaveRoot = cache;
            return cache;
        }
        private Reanimation.ReanimationRenderer OriginalFinalWaveRenderer()
        {
            if (File.Exists(Path.Combine(root, "reanim", "FinalWave.reanim")) && File.Exists(Path.Combine(root, "reanim", "FinalWave.png"))) return animations;
            if (bundledWaveAnimations == null)
            {
                string bundled = EnsureOriginalWaveFiles();
                if (bundled == null) return null;
                bundledWaveAnimations = new Reanimation.ReanimationRenderer(bundled);
            }
            return bundledWaveAnimations;
        }
        private void DisposeAdventureWaveNotices()
        {
            if (waveNoticeGame != null) waveNoticeGame.WaveScheduleEventRaised -= HandleAdventureWaveNotice;
            waveNoticeGame = null;
            if (bundledWaveAnimations != null) { bundledWaveAnimations.Dispose(); bundledWaveAnimations = null; }
        }
        private void DrawOriginalHugeWaveText(int age)
        {
            if (modeTitleBitmap == null) return; // existing HouseofTerror28 atlas; no OS-font substitute.
            if (hugeWaveTextAnimation == null)
            {
                string path = Path.Combine(root, "reanim", "TextFadeOn.reanim");
                if (!File.Exists(path))
                {
                    string bundled = EnsureOriginalWaveFiles();
                    if (bundled == null) return;
                    path = Path.Combine(bundled, "reanim", "TextFadeOn.reanim");
                }
                hugeWaveTextAnimation = AdventureWaveTextAnimation.Load(path);
            }
            if (hugeWaveText == null)
            {
                hugeWaveText = OriginalProgressionText("ADVICE_HUGE_WAVE");
                hugeWaveTextWidth = modeTitleBitmap.Layout(hugeWaveText).Width;
                foreach (char c in hugeWaveText) hugeWaveGlyphs.Add(modeTitleBitmap.Layout(c.ToString()));
            }
            float offset = -hugeWaveTextWidth * .5f;
            Matrix4x4 old = GUI.matrix;
            try
            {
                int slide = 100 + hugeWaveText.Length;
                for (int i = 0; i < hugeWaveGlyphs.Count; i++)
                {
                    var pose = hugeWaveTextAnimation.Sample(age, i, hugeWaveText.Length);
                    float x = pose.X + offset;
                    if (age < slide) x += offset * (1f - pose.Phase);
                    float y = pose.Y + AdventureWaveNoticeRules.Baseline - 300;
                    if (pose.Alpha > 0f && pose.ScaleX != 0f && pose.ScaleY != 0f)
                    {
                        GUI.matrix = old * Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(pose.ScaleX, pose.ScaleY, 1));
                        modeTitleBitmap.Draw(hugeWaveGlyphs[i], 0, -modeTitleBitmap.Data.Base, new Color(1, 0, 0, pose.Alpha));
                    }
                    BmFontGlyph glyph; if (modeTitleBitmap.Data.TryGetGlyph(hugeWaveText[i], out glyph)) offset += glyph.XAdvance;
                }
            }
            finally { GUI.matrix = old; }
        }
    }
}
