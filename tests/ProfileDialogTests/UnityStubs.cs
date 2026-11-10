using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
        public float xMax { get { return x + width; } }
        public float yMax { get { return y + height; } }
        public bool Contains(Vector2 p)
        { return p.x >= x && p.x < xMax && p.y >= y && p.y < yMax; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
    }
    public enum TextureFormat { RGBA32 }
    public enum HideFlags { HideAndDontSave }
    public enum FilterMode { Bilinear }
    public enum TextureWrapMode { Clamp }
    public enum ScaleMode { StretchToFill }
    public enum EventType { Repaint, Layout, MouseDown, KeyDown }
    public enum TextClipping { Clip }
    public enum TextAnchor { MiddleCenter, MiddleLeft }
    public sealed class Event { public static Event current; public EventType type; }
    public static class Application { public static bool isPlaying; }
    public class Object
    {
        public bool Destroyed;
        public static void Destroy(Object value) { value.Destroyed = true; }
        public static void DestroyImmediate(Object value) { value.Destroyed = true; }
    }
    public sealed class Texture2D : Object
    {
        public static readonly List<Texture2D> Created = new List<Texture2D>();
        public static readonly Texture2D whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        public int width, height;
        public HideFlags hideFlags;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Texture2D(int width, int height, TextureFormat format, bool mipmaps)
        { this.width = width; this.height = height; Created.Add(this); }
    }
    public static class ImageConversion
    {
        public static bool LoadImage(Texture2D image, byte[] bytes, bool nonReadable)
        {
            if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4e || bytes[3] != 0x47)
                return false;
            image.width = ReadInt(bytes, 16);
            image.height = ReadInt(bytes, 20);
            return true;
        }
        private static int ReadInt(byte[] b, int i) { return b[i] << 24 | b[i + 1] << 16 | b[i + 2] << 8 | b[i + 3]; }
    }
    public sealed class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string[] names, int size) { return new Font(); }
    }
    public sealed class RectOffset { public RectOffset(int l, int r, int t, int b) { } }
    public sealed class GUIStyleState { public Color textColor; }
    public sealed class GUIStyle
    {
        public Font font;
        public int fontSize;
        public bool wordWrap, richText;
        public TextClipping clipping;
        public TextAnchor alignment;
        public RectOffset padding;
        public GUIStyleState normal = new GUIStyleState();
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
    }
    public sealed class GUISkin
    {
        public GUIStyle label = new GUIStyle();
        public GUIStyle textField = new GUIStyle();
    }
    public static class GUI
    {
        public static Color color = Color.white, contentColor = Color.white;
        public static bool enabled = true;
        public static GUISkin skin = new GUISkin();
        public static string NextText;
        public static int TextFieldCalls;
        public static string Focused;
        public static readonly List<string> Labels = new List<string>();
        public static void DrawTexture(Rect rect, Texture2D image, ScaleMode mode, bool alpha) { }
        public static void Label(Rect rect, string text, GUIStyle style) { Labels.Add(text); }
        public static void SetNextControlName(string name) { }
        public static void FocusControl(string name) { Focused = name; }
        public static string TextField(Rect rect, string value, GUIStyle style)
        {
            TextFieldCalls++;
            if (NextText == null) return value;
            string next = NextText;
            NextText = null;
            return next;
        }
    }
}
