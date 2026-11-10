using System;
using System.IO;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.UI;
using UnityEngine;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
    private static void Click(ProfileDialog dialog, float x, float y)
    { Check(dialog.HandleClick(new Vector2(x, y)), "Expected modal click."); }
    private static void Draw(ProfileDialog dialog, string text = null)
    {
        CanvasUI.Labels.Clear();
        CanvasUI.NextText = text;
        dialog.Draw();
    }
    private static bool Has(string text) { return CanvasUI.Labels.Exists(label => label.Contains(text)); }
    private static void Ok(ProfileDialog dialog) { Click(dialog, 300, 478); }
    private static void Cancel(ProfileDialog dialog) { Click(dialog, 470, 478); }

    private static void Main(string[] args)
    {
        string root = args.Length == 0
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../../extracted_assets/raw"))
            : Path.GetFullPath(args[0]);
        string output = Path.Combine(AppContext.BaseDirectory, "integration-data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        string file = Path.Combine(output, "profiles.dat");
        ProfileCatalog catalog = new ProfileCatalog(file);
        using (ProfileDialog dialog = new ProfileDialog(root, catalog))
        {
            dialog.Open();
            Draw(dialog, "真实用户一");
            Ok(dialog);
            Check(!dialog.IsOpen, "Real create should close after selection.");
            ProfileCatalog reloaded = new ProfileCatalog(file);
            Check(reloaded.Profiles.Count == 1 && reloaded.Profiles[0].Name == "真实用户一" &&
                reloaded.SelectedId == reloaded.Profiles[0].Id, "Create persists to disk.");
            Guid first = catalog.Profiles[0].Id;
            dialog.Open();
            Click(dialog, 300, 244);
            Draw(dialog, "真实用户二");
            Ok(dialog);
            Check(!dialog.IsOpen && new ProfileCatalog(file).Profiles.Count == 2, "Second user persists.");
            Guid second = catalog.Profiles[1].Id;

            dialog.Open();
            Click(dialog, 300, 220);
            Check(catalog.SelectedId == second, "Highlight does not change catalog selection.");
            Ok(dialog);
            Check(!dialog.IsOpen && new ProfileCatalog(file).SelectedId == first, "Selection persists.");

            dialog.Open();
            Click(dialog, 300, 430);
            Draw(dialog, "真实用户二");
            Ok(dialog);
            Draw(dialog);
            Check(dialog.IsOpen && Has("重命名失败") && catalog.Profiles[0].Name == "真实用户一",
                "Real duplicate-name error remains visible.");
            Draw(dialog, "真实用户三");
            Ok(dialog);
            Check(dialog.IsOpen && new ProfileCatalog(file).Profiles[0].Name == "真实用户三", "Rename persists.");
            Click(dialog, 470, 430);
            Cancel(dialog);
            Check(new ProfileCatalog(file).Profiles.Count == 2, "Cancel deletion leaves disk unchanged.");
            Click(dialog, 470, 430);
            Ok(dialog);
            reloaded = new ProfileCatalog(file);
            Check(dialog.IsOpen && reloaded.Profiles.Count == 1 && reloaded.SelectedId == second,
                "Confirmed delete persists and retains a valid selection.");
            Click(dialog, 470, 430);
            Ok(dialog);
            Check(dialog.IsOpen && new ProfileCatalog(file).Profiles.Count == 0, "Last deletion stays modal.");
            dialog.HandleBack();
            dialog.HandleBack();
            Check(dialog.IsOpen, "Empty persisted catalog cannot be dismissed.");
        }

        string lockedFile = Path.Combine(output, "locked.dat");
        ProfileCatalog locked = new ProfileCatalog(lockedFile);
        using (ProfileDialog dialog = new ProfileDialog(root, locked))
        using (FileStream held = new FileStream(lockedFile + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            dialog.Open();
            Draw(dialog, "无法写入");
            Ok(dialog);
            Draw(dialog);
            Check(dialog.IsOpen && Has("创建用户失败") && !File.Exists(lockedFile), "Real lock error remains open and visible.");
            dialog.HandleBack();
            dialog.HandleBack();
            Check(dialog.IsOpen, "An unusable real catalog cannot accidentally close.");
        }
        Console.WriteLine("PASS: " + checks + " real-catalog integration checks; data " + output);
    }
}
