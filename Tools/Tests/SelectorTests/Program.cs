using System;
using System.IO;
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

    private static void Hit(SelectorView view, float x, float y, SelectorAction action)
    {
        SelectorAction actual = view.HitTest(new Vector2(x, y));
        Check(actual == action, "Hit " + x + "," + y + ": expected " + action + ", got " + actual);
    }

    private static void Dimensions(string root, string path, int width, int height)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Check(ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(root, path)), false),
            "Invalid image header: " + path);
        Check(texture.width == width && texture.height == height,
            "Unexpected dimensions for " + path + ": " + texture.width + "x" + texture.height);
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void Main(string[] args)
    {
        string root = args.Length == 0
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../extracted_assets/raw"))
            : Path.GetFullPath(args[0]);
        Check(Directory.Exists(root), "Missing raw resource root: " + root);
        SelectorView view = new SelectorView(root);
        Hit(view, 500, 120, SelectorAction.Adventure);
        Hit(view, 500, 215, SelectorAction.MiniGames);
        Hit(view, 500, 290, SelectorAction.Puzzle);
        Hit(view, 500, 360, SelectorAction.Survival);
        Hit(view, 210, 450, SelectorAction.ZenGarden);
        Hit(view, 360, 460, SelectorAction.Almanac);
        Hit(view, 470, 535, SelectorAction.Store);
        Hit(view, 420, 490, SelectorAction.Almanac);
        Hit(view, 327, 428, SelectorAction.Almanac);
        Hit(view, 425.99f, 526.99f, SelectorAction.Almanac);
        Hit(view, 326.99f, 460, SelectorAction.None);
        Hit(view, 426, 450, SelectorAction.None);
        Hit(view, 360, 527, SelectorAction.None);
        Hit(view, 405, 527, SelectorAction.Store);
        Hit(view, 534.99f, 570.99f, SelectorAction.Store);
        Hit(view, 535, 540, SelectorAction.None);
        Hit(view, 470, 571, SelectorAction.None);
        Hit(view, 440, 460, SelectorAction.None);
        Hit(view, 600, 500, SelectorAction.Options);
        Hit(view, 675, 515, SelectorAction.Help);
        Hit(view, 735, 550, SelectorAction.Quit);
        Hit(view, 150, 150, SelectorAction.ChangeUser);
        Hit(view, 150, 210, SelectorAction.Zombatar);
        Hit(view, 10, 300, SelectorAction.None);
        Hit(view, 420, 150, SelectorAction.None);
        Hit(view, 799.99f, 599.99f, SelectorAction.None);
        Hit(view, 800, 100, SelectorAction.None);
        Hit(view, 0, 600, SelectorAction.None);
        Hit(view, -0.01f, 100, SelectorAction.None);
        Hit(view, float.NaN, 100, SelectorAction.None);
        Hit(view, float.PositiveInfinity, 100, SelectorAction.None);
        Hit(view, 405, 65, SelectorAction.None);
        Hit(view, 430, 100, SelectorAction.Adventure);
        Hit(view, 430, 170, SelectorAction.None);

        Event.current = new Event { type = EventType.Layout };
        view.Draw(0);
        Check(ImageConversion.LoadCount == 0, "Layout must not load textures.");
        Event.current.type = EventType.Repaint;
        view.Draw(0);
        Check(Debug.Warnings.Count == 0, "Asset warning: " + string.Join("; ", Debug.Warnings));
        Check(GUI.GroupDepth == 0, "GUI group must be balanced.");
        Check(GUI.Drawn.Count == 32, "Unexpected number of drawn layers: " + GUI.Drawn.Count);
        Check(ImageConversion.LoadCount == 34, "Unexpected image/mask load count: " + ImageConversion.LoadCount);
        // Native anchor evidence fixes both origin and unscaled original image size.
        Rect almanac = GUI.Drawn[28], store = GUI.Drawn[27];
        Check(almanac.x == 327 && almanac.y == 428 && almanac.width == 99 && almanac.height == 99,
            "Almanac must use its native settled anchor and original image size.");
        Check(store.x == 405 && store.y == 482 && store.width == 130 && store.height == 89,
            "Store must use its native settled anchor and original image size.");
        int loaded = ImageConversion.LoadCount;
        view.Draw(float.NaN);
        Check(ImageConversion.LoadCount == loaded, "Second repaint must reuse cached textures.");
        view.HoveredAction = SelectorAction.MiniGames;
        view.SetModeLocked(SelectorAction.MiniGames, true);
        view.Draw(1);
        Check(ImageConversion.LoadCount == loaded + 1, "Highlight should load only once.");
        view.Draw(2);
        Check(ImageConversion.LoadCount == loaded + 1, "Highlight must be cached.");
        Hit(view, 500, 215, SelectorAction.MiniGames);
        bool invalidLock = false;
        try { view.SetModeLocked(SelectorAction.Quit, true); }
        catch (ArgumentOutOfRangeException) { invalidLock = true; }
        Check(invalidLock, "Only mode signs support lock tint.");
        Check(Texture2D.Created.Count == 38, "Three JPEG masks must use separate RGBA textures.");
        view.Dispose();
        view.Dispose();
        foreach (Texture2D texture in Texture2D.Created)
            Check(texture.Destroyed, "Every cached image and temporary mask must be destroyed.");
        bool disposed = false;
        try { view.HitTest(new Vector2(0, 0)); }
        catch (ObjectDisposedException) { disposed = true; }
        Check(disposed, "Disposed views must reject use.");
        Dimensions(root, "reanim/SelectorScreen_BG.jpg", 100, 75);
        Dimensions(root, "reanim/SelectorScreen_BG_Left.jpg", 476, 680);
        Dimensions(root, "reanim/SelectorScreen_BG_Left_.png", 476, 680);
        Dimensions(root, "reanim/SelectorScreen_BG_Center.jpg", 720, 350);
        Dimensions(root, "reanim/SelectorScreen_BG_Center_.png", 720, 350);
        Dimensions(root, "reanim/SelectorScreen_BG_Right.jpg", 730, 560);
        Dimensions(root, "reanim/SelectorScreen_BG_Right_.png", 730, 560);
        Dimensions(root, "reanim/SelectorScreen_StartAdventure_Button1.png", 331, 146);
        Dimensions(root, "reanim/SelectorScreen_Survival_button.png", 313, 133);
        Dimensions(root, "reanim/SelectorScreen_Challenges_button.png", 286, 122);
        Dimensions(root, "reanim/SelectorScreen_Vasebreaker_button.png", 266, 123);
        Dimensions(root, "images/SelectorScreen_ZenGarden.png", 191, 163);
        Dimensions(root, "images/SelectorScreen_Almanac.png", 99, 99);
        Dimensions(root, "images/SelectorScreen_Store.png", 130, 89);
        Dimensions(root, "images/SelectorScreen_Options1.png", 81, 31);
        Dimensions(root, "images/SelectorScreen_Help1.png", 48, 22);
        Dimensions(root, "images/SelectorScreen_Quit1.png", 47, 27);
        Console.WriteLine("PASS: " + checks + " checks, raw root " + root);
    }
}
