using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PvZReplica.UI;
using UnityEngine;

public sealed class SelectorSmoke : MonoBehaviour
{
    private SelectorView view;
    private string root;
    private string output;
    private int resourceWarnings;

    private void Awake()
    {
        Application.runInBackground = true;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] == "-selector-root") root = args[++i];
            else if (args[i] == "-selector-output") output = args[++i];
        }
        if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(output))
            throw new Exception("Missing smoke test root/output.");
        Application.logMessageReceived += Log;
        view = new SelectorView(root);
        StartCoroutine(CheckScreenshot());
    }

    private void Log(string condition, string stack, LogType type)
    {
        if (condition.StartsWith("SelectorView:", StringComparison.Ordinal)) resourceWarnings++;
    }

    private void OnGUI()
    {
        if (view != null) view.Draw(Time.time);
    }

    private IEnumerator CheckScreenshot()
    {
        yield return new WaitForSeconds(1f);
        yield return new WaitForEndOfFrame();
        Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
        if (image == null || image.width != 800 || image.height != 600 || resourceWarnings != 0)
        {
            Debug.LogError("SELECTOR_SMOKE_FAIL: size or resource warnings " + resourceWarnings);
            Application.Quit(1);
            yield break;
        }
        HashSet<int> colors = new HashSet<int>();
        for (int y = 20; y < 600; y += 40)
            for (int x = 20; x < 800; x += 40)
            {
                Color32 p = image.GetPixel(x, y);
                colors.Add((p.r << 16) | (p.g << 8) | p.b);
            }
        File.WriteAllBytes(output, image.EncodeToPNG());
        Destroy(image);
        if (colors.Count < 50)
        {
            Debug.LogError("SELECTOR_SMOKE_FAIL: screenshot appears blank, colors=" + colors.Count);
            Application.Quit(1);
            yield break;
        }
        Debug.Log("SELECTOR_SMOKE_PASS colors=" + colors.Count);
        Application.Quit(0);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= Log;
        if (view != null) view.Dispose();
    }
}
