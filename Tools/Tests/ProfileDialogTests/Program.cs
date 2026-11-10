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
    { Check(dialog.HandleClick(new Vector2(x, y)), "Expected modal click to be consumed."); }
    private static void Draw(ProfileDialog dialog, string text = null)
    {
        GUI.Labels.Clear();
        GUI.NextText = text;
        Event.current = new Event { type = EventType.Repaint };
        dialog.Draw();
    }
    private static bool HasLabel(string fragment)
    { return GUI.Labels.Exists(text => text.Contains(fragment)); }
    private static void Ok(ProfileDialog dialog) { Click(dialog, 300, 478); }
    private static void Cancel(ProfileDialog dialog) { Click(dialog, 470, 478); }
    private static void Create(ProfileDialog dialog, string name, int row)
    {
        Click(dialog, 300, 220 + row * 24);
        Draw(dialog, name);
        Ok(dialog);
    }

    private static void Main(string[] args)
    {
        string root = args.Length == 0
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../extracted_assets/raw"))
            : Path.GetFullPath(args[0]);
        ProfileCatalog catalog = new ProfileCatalog("fixture");
        ProfileDialog dialog = new ProfileDialog(root, catalog);
        Check(!dialog.IsOpen, "Dialog starts closed.");
        dialog.Open();
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("新用户"), "Empty catalog starts in create editor.");
        Check(GUI.Focused == "PvZ.ProfileDialog.Name", "Name field receives focus.");
        Check(!dialog.HandleClick(new Vector2(300, 270)), "Text field MouseDown must reach IMGUI.");
        Check(dialog.HandleClick(new Vector2(1, 1)), "Clicks outside the panel remain modal.");
        Ok(dialog);
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("请输入"), "Empty name is rejected visibly.");
        Cancel(dialog);
        Check(dialog.IsOpen, "Canceling initial creation does not close an empty catalog.");
        dialog.HandleBack();
        dialog.HandleBack();
        Check(dialog.IsOpen, "Back cannot close with no selected user.");

        Create(dialog, "  张三  ", 0);
        Check(!dialog.IsOpen && catalog.Profiles.Count == 1, "Create persists and closes after selection.");
        Check(catalog.Profiles[0].Name == "张三" && catalog.SelectedId == catalog.Profiles[0].Id, "Trimmed name and selection.");
        dialog.Open();
        Create(dialog, "李四", 1);
        Check(!dialog.IsOpen && catalog.Profiles.Count == 2, "A second user can be created.");
        Guid first = catalog.Profiles[0].Id;
        Guid second = catalog.Profiles[1].Id;
        dialog.Open();
        Click(dialog, 300, 220);
        Check(catalog.SelectedId == second, "Highlighting is not selection.");
        catalog.FailOperation = "select";
        Ok(dialog);
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("选择用户失败") && catalog.SelectedId == second, "Failed selection stays open.");
        catalog.FailOperation = null;
        catalog.NoopOperation = "select";
        Ok(dialog);
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("未能确认"), "A silent selection failure does not close.");
        catalog.NoopOperation = null;
        Ok(dialog);
        Check(!dialog.IsOpen && catalog.SelectedId == first, "Selection succeeds on retry.");

        dialog.Open();
        Click(dialog, 300, 430);
        Draw(dialog, "李四");
        Ok(dialog);
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("重命名失败") && catalog.Profiles[0].Name == "张三", "Duplicate rename reports error.");
        Draw(dialog, "王五");
        catalog.NoopOperation = "rename";
        Ok(dialog);
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("无法确认重命名"), "Silent rename failure is visible.");
        catalog.NoopOperation = null;
        Ok(dialog);
        Check(dialog.IsOpen && catalog.Profiles[0].Name == "王五", "Rename returns to the list.");
        Click(dialog, 470, 430);
        Draw(dialog);
        Check(HasLabel("永久删除") && HasLabel("王五"), "Deletion names the target.");
        Cancel(dialog);
        Check(catalog.Profiles.Count == 2 && catalog.DeleteCalls == 0, "Cancel never deletes.");
        Click(dialog, 470, 430);
        catalog.FailOperation = "delete";
        Ok(dialog);
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("删除用户失败") && catalog.Profiles.Count == 2, "Failed deletion stays open.");
        catalog.FailOperation = null;
        catalog.NoopOperation = "delete";
        Ok(dialog);
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("未能确认删除"), "Silent deletion failure stays open.");
        catalog.NoopOperation = null;
        Ok(dialog);
        Check(catalog.Profiles.Count == 1 && catalog.SelectedId == second && dialog.IsOpen, "Delete leaves the list open.");
        Click(dialog, 470, 430);
        Ok(dialog);
        Check(catalog.Profiles.Count == 0 && dialog.IsOpen, "Deleting the final profile never closes the dialog.");
        dialog.HandleBack();
        dialog.HandleBack();
        Check(dialog.IsOpen, "Back still cannot close after final deletion.");

        Click(dialog, 300, 220);
        Draw(dialog, "恢复");
        catalog.CommitThenThrow = "create";
        Ok(dialog);
        Draw(dialog);
        Check(dialog.IsOpen && catalog.Profiles.Count == 1 && HasLabel("检查列表"), "Uncertain create returns to list.");
        int calls = catalog.CreateCalls;
        catalog.CommitThenThrow = null;
        Ok(dialog);
        Check(!dialog.IsOpen && catalog.CreateCalls == calls, "Retry selects the created entry without creating twice.");

        for (int i = 0; i < 17; i++) catalog.Create("玩家" + i);
        dialog.Open();
        Draw(dialog);
        Check(HasLabel("3/3"), "Opening locates the selected profile's page.");
        Click(dialog, 520, 220);
        Draw(dialog);
        Check(HasLabel("2/3"), "Previous page works.");
        Click(dialog, 520, 375);
        Draw(dialog);
        Check(HasLabel("3/3") && HasLabel("创建一个新的玩家"), "Final page exposes creation.");
        dialog.HandleBack();
        Check(!dialog.IsOpen, "Cancel with a valid prior selection closes.");

        catalog.FailRead = true;
        dialog.Open();
        Draw(dialog);
        Check(dialog.IsOpen && HasLabel("无法读取用户目录"), "Read failure is visible.");
        dialog.HandleBack();
        dialog.HandleBack();
        Check(dialog.IsOpen, "Unreadable catalog never closes via Back.");
        catalog.FailRead = false;
        GUI.color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
        GUI.enabled = false;
        Draw(dialog);
        Check(GUI.color.r == 0.1f && !GUI.enabled, "Draw restores host GUI state.");
        dialog.Dispose();
        dialog.Dispose();
        foreach (Texture2D image in Texture2D.Created)
            if (!ReferenceEquals(image, Texture2D.whiteTexture)) Check(image.Destroyed, "Owned image was not released.");
        bool disposed = false;
        try { dialog.Open(); } catch (ObjectDisposedException) { disposed = true; }
        Check(disposed, "Disposed view rejects use.");
        bool missing = false;
        try { new ProfileDialog(Path.Combine(root, "does-not-exist"), catalog); }
        catch (IOException) { missing = true; }
        Check(missing, "Missing required assets throw explicitly.");
        Console.WriteLine("PASS: " + checks + " controlled UI checks");
    }
}
