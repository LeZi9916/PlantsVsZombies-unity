using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;
using PvZReplica.Reanimation;
using PvZReplica.UI;
using UnityEngine;

public sealed class AlmanacSmoke : MonoBehaviour
{
    private AlmanacView view;
    private ReanimationRenderer animationRenderer;
    private string output;
    private readonly StringBuilder errors = new StringBuilder();
    private int repaintCount;
    private float renderClock;

    [RuntimeInitializeOnLoadMethod]
    private static void Bootstrap()
    {
        new GameObject("Smoke Camera").AddComponent<Camera>();
        new GameObject("Almanac Smoke").AddComponent<AlmanacSmoke>();
    }

    private void Awake()
    {
        output = Environment.GetEnvironmentVariable("PVZ_ALMANAC_OUTPUT");
        Directory.CreateDirectory(output);
        Application.runInBackground = true;
        Application.logMessageReceived += RecordLog;
        animationRenderer = new ReanimationRenderer(Environment.GetEnvironmentVariable("PVZ_ALMANAC_ROOT"));
        view = new AlmanacView(Environment.GetEnvironmentVariable("PVZ_ALMANAC_ROOT"), animationRenderer);
    }

    private void RecordLog(string text, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            errors.AppendLine(text + "\n" + stack);
    }

    private void OnGUI()
    {
        if (view == null) return;
        if (Event.current.type == EventType.Repaint) repaintCount++;
        view.Draw(renderClock);
    }

    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        Texture2D screen = ScreenCapture.CaptureScreenshotAsTexture();
        Color32[] pixels = screen.GetPixels32();
        int nonBlack = 0;
        foreach (Color32 pixel in pixels) if (pixel.r > 10 || pixel.g > 10 || pixel.b > 10) nonBlack++;
        if (repaintCount == 0 || nonBlack < pixels.Length / 4) errors.AppendLine("Blank capture or no repaint: " + name);
        File.WriteAllBytes(Path.Combine(output, name + ".png"), screen.EncodeToPNG());
        Destroy(screen);
        if (name.StartsWith("zombie-", StringComparison.Ordinal)) SavePortraitSurfaces(name);
    }

    private void SavePortraitSurfaces(string name)
    {
        string directory = Path.Combine(output, "portraits");
        Directory.CreateDirectory(directory);
        var surfaces = (System.Collections.IDictionary)typeof(ReanimationRenderer)
            .GetField("nativeSurfaces", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animationRenderer);
        string[] keys = { "almanac-zombie-preview", "almanac-zombie-thumb-" + view.SelectedId };
        foreach (string key in keys)
        {
            object surface = surfaces[key];
            if (surface == null) { errors.AppendLine("Missing native portrait surface: " + key); continue; }
            RenderTexture texture = (RenderTexture)surface.GetType().GetField("Texture").GetValue(surface);
            RenderTexture previous = RenderTexture.active;
            Texture2D copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = texture;
                copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                copy.Apply();
                string kind = key.EndsWith("preview", StringComparison.Ordinal) ? "preview" : "thumb";
                File.WriteAllBytes(Path.Combine(directory, kind + "-" + name.Substring(7) + ".png"), copy.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Destroy(copy); }
        }
    }

    private IEnumerator Start()
    {
        yield return Capture("home");
        view.HandleClick(new Vector2(131, 346));
        yield return Capture("plant");
        for (int id = 0; id < 49; id++)
        {
            float x = id == 48 ? 21 : 27 + (id % 8) * 52;
            float y = id == 48 ? 24 : 93 + (id / 8) * 78;
            if (!view.HandleClick(new Vector2(x, y)) || view.SelectedId != id) errors.AppendLine("Plant selection failed: " + id);
            yield return new WaitForEndOfFrame();
            if (id == 18) yield return Capture("threepeater");
            if (id == 23) yield return Capture("tallnut");
            if (id == 35) yield return Capture("coffee");
            if (id == 40) yield return Capture("gatling");
            if (id == 48) yield return Capture("imitater");
        }
        view.HandleClick(new Vector2(33, 568));
        view.HandleClick(new Vector2(488, 346));
        yield return Capture("zombie");
        for (int id = 0; id < 26; id++)
        {
            float x = id == 25 ? 193 : 23 + (id % 5) * 85;
            float y = id == 25 ? 487 : 87 + (id / 5) * 80;
            renderClock = 0f;
            if (!view.HandleClick(new Vector2(x, y)) || view.SelectedId != id) errors.AppendLine("Zombie selection failed: " + id);
            yield return new WaitForEndOfFrame();
            yield return Capture("zombie-" + id.ToString("00") + "-0");
            renderClock = 0.75f;
            yield return Capture("zombie-" + id.ToString("00") + "-1");
            if (id == 10) yield return Capture("ducky");
            if (id == 25) yield return Capture("boss");
        }
        var loaded = (Dictionary<string, Texture2D>)typeof(ReanimationRenderer)
            .GetField("textures", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animationRenderer);
        int maskedBossLayers = 0;
        foreach (KeyValuePair<string, Texture2D> pair in loaded)
        {
            string name = Path.GetFileName(pair.Key);
            if (!string.Equals(name, "Zombie_boss_upperbody.jpg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, "Zombie_boss_neck.jpg", StringComparison.OrdinalIgnoreCase)) continue;
            maskedBossLayers++;
            if (pair.Value.format != TextureFormat.RGBA32) errors.AppendLine("JPG mask lost its alpha channel: " + name);
            RenderTexture previous = RenderTexture.active;
            RenderTexture copy = RenderTexture.GetTemporary(pair.Value.width, pair.Value.height, 0, RenderTextureFormat.ARGB32);
            Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(pair.Value, copy);
                RenderTexture.active = copy;
                pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                pixel.Apply();
                if (pixel.GetPixel(0, 0).a > 0.01f) errors.AppendLine("Boss JPG mask has an opaque corner: " + name);
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(copy); Destroy(pixel); }
        }
        if (maskedBossLayers != 2) errors.AppendLine("Boss alpha layers were not loaded and checked.");
        File.WriteAllLines(Path.Combine(output, "diagnostics.txt"), view.Diagnostics);
        if (view.Diagnostics.Count > 0) errors.AppendLine(string.Join("\n", view.Diagnostics));
        view.Dispose();
        animationRenderer.Dispose();
        try { view.Open(); errors.AppendLine("Disposed view accepted Open"); }
        catch (ObjectDisposedException) { }
        File.WriteAllText(Path.Combine(output, "result.txt"), errors.Length == 0 ? "PASS" : errors.ToString());
        Application.Quit(errors.Length == 0 ? 0 : 1);
    }
}
