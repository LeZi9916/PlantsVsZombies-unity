using System;
using System.IO;
using PvZReplica.Profiles;
using PvZReplica.UI;
using UnityEditor;
using UnityEngine;

public static class ProfileDialogUnityTests
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }

    public static void Run()
    {
        try
        {
            string root = Environment.GetEnvironmentVariable("PVZ_PROFILE_RESOURCE_ROOT");
            string data = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "profile-unity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(data);
            string path = Path.Combine(data, "profiles.dat");
            ProfileCatalog catalog = new ProfileCatalog(path);
            Check(typeof(ProfileDialog).Assembly == typeof(ProfileCatalog).Assembly, "Both production classes compile into one Unity assembly.");
            using (ProfileDialog dialog = new ProfileDialog(root, catalog))
            {
                dialog.Open();
                Check(dialog.IsOpen, "Empty editor opens.");
                Check(!dialog.HandleClick(new Vector2(300, 270)), "Text field receives MouseDown.");
                Check(dialog.HandleBack() && dialog.IsOpen, "Empty editor cancellation stays modal.");
                Check(dialog.HandleBack() && dialog.IsOpen, "Empty list remains open.");
                catalog.Create("Unity 验证");
                Check(dialog.HandleClick(new Vector2(300, 220)), "Click profile row.");
                Check(dialog.HandleClick(new Vector2(300, 478)), "Confirm profile.");
                Check(!dialog.IsOpen, "Confirmed selection closes.");
                ProfileCatalog loaded = new ProfileCatalog(path);
                Check(loaded.Profiles.Count == 1 && loaded.SelectedId == catalog.SelectedId, "Persisted catalog reloads.");
                dialog.Open();
                Check(dialog.HandleBack() && !dialog.IsOpen, "A valid selection allows cancellation.");
            }
            Debug.Log("PROFILE_UNITY_TESTS_PASS checks=" + checks);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogError("PROFILE_UNITY_TESTS_FAIL: " + error);
            EditorApplication.Exit(1);
        }
    }
}
