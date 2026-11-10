using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PvZReplica.UI;
using UnityEngine;

public sealed class HelpSmoke : MonoBehaviour
{
    private HelpView view;
    private string output;
    private bool guiStatePreserved = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartSmoke()
    {
        new GameObject("HelpSmoke").AddComponent<HelpSmoke>();
    }

    private void Awake()
    {
        Application.runInBackground = true;
        string root = null;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] == "-help-root") root = args[++i];
            else if (args[i] == "-help-output") output = args[++i];
        }
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(output))
            throw new Exception("Missing smoke root/output");
        view = new HelpView(root);
        view.Open();
        StartCoroutine(Capture());
    }

    private void OnGUI()
    {
        if (view == null) return;
        Color old = GUI.color;
        Color sentinel = new Color(.7f, .8f, .9f, .6f);
        GUI.color = sentinel;
        view.Draw();
        if (GUI.color != sentinel) guiStatePreserved = false;
        GUI.color = old;
    }

    private IEnumerator Capture()
    {
        yield return new WaitForSecondsRealtime(1f);
        yield return new WaitForEndOfFrame();
        Texture2D normal = ScreenCapture.CaptureScreenshotAsTexture();
        if (normal == null || normal.width != 800 || normal.height != 600)
        {
            Debug.LogError("HELP_SMOKE_FAIL screenshot unavailable or not 800x600");
            Application.Quit(1);
            yield break;
        }
        File.WriteAllBytes(Path.Combine(output, "help-normal.png"), normal.EncodeToPNG());
        Color32[] normalPixels = normal.GetPixels32();
        HashSet<int> colors = new HashSet<int>();
        for (int i = 0; i < normalPixels.Length; i += 19)
        {
            Color32 p = normalPixels[i];
            colors.Add((p.r << 16) | (p.g << 8) | p.b);
        }
        view.HandlePointer(new Vector2(400, 520), EventType.MouseMove);
        yield return null;
        yield return new WaitForEndOfFrame();
        Texture2D hover = ScreenCapture.CaptureScreenshotAsTexture();
        if (hover == null || hover.width != normal.width || hover.height != normal.height)
        {
            Debug.LogError("HELP_SMOKE_FAIL hover screenshot unavailable");
            Application.Quit(1);
            yield break;
        }
        File.WriteAllBytes(Path.Combine(output, "help-hover.png"), hover.EncodeToPNG());
        Color32[] hoverPixels = hover.GetPixels32();
        int changed = 0;
        for (int y = 500; y < 542; y++)
            for (int x = 324; x < 480; x++)
            {
                int index = (599 - y) * 800 + x;
                Color32 a = normalPixels[index], b = hoverPixels[index];
                if (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) > 10) changed++;
            }
        Destroy(normal);
        Destroy(hover);
        if (colors.Count < 100 || changed < 100 || !guiStatePreserved)
        {
            Debug.LogError("HELP_SMOKE_FAIL colors=" + colors.Count + " hoverPixels=" + changed +
                " guiState=" + guiStatePreserved);
            Application.Quit(1);
            yield break;
        }
        Debug.Log("HELP_SMOKE_PASS colors=" + colors.Count + " hoverPixels=" + changed +
            " guiState=" + guiStatePreserved);
        Application.Quit(0);
    }

    private void OnDestroy()
    {
        if (view != null) view.Dispose();
    }
}
