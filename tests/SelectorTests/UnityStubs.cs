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
        public bool Contains(Vector2 point)
        { return point.x >= x && point.x < x + width && point.y >= y && point.y < y + height; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public struct Color32 { public byte r, g, b, a; }
    public enum TextureFormat { RGBA32 }
    public enum HideFlags { HideAndDontSave }
    public enum FilterMode { Bilinear }
    public enum TextureWrapMode { Clamp }
    public enum ScaleMode { StretchToFill }
    public enum EventType { Repaint, Layout }
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
        public int width, height;
        public HideFlags hideFlags;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Color32[] Pixels;
        public bool Readable = true;
        public static readonly List<Texture2D> Created = new List<Texture2D>();
        public Texture2D(int width, int height, TextureFormat format, bool mipmaps)
        { this.width = width; this.height = height; Created.Add(this); }
        public Color32[] GetPixels32() { return Pixels ?? (Pixels = new Color32[width * height]); }
        public void SetPixels32(Color32[] pixels) { Pixels = pixels; }
        public void Apply(bool mipmaps, bool noLongerReadable) { Readable = !noLongerReadable; }
    }

    public static class ImageConversion
    {
        public static int LoadCount;
        public static bool LoadImage(Texture2D texture, byte[] bytes, bool noLongerReadable)
        {
            LoadCount++;
            if (bytes.Length >= 24 && bytes[0] == 0x89 && bytes[1] == 0x50)
            {
                texture.width = Int32(bytes, 16);
                texture.height = Int32(bytes, 20);
                return true;
            }
            if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8) return false;
            int position = 2;
            while (position + 9 < bytes.Length)
            {
                if (bytes[position++] != 0xff) continue;
                byte marker = bytes[position++];
                if (marker == 0xff) { position--; continue; }
                if (marker == 0xd8 || marker == 0xd9) continue;
                int length = bytes[position] * 256 + bytes[position + 1];
                if (length < 2 || position + length > bytes.Length) return false;
                if ((marker >= 0xc0 && marker <= 0xc3) || (marker >= 0xc5 && marker <= 0xc7) ||
                    (marker >= 0xc9 && marker <= 0xcb) || (marker >= 0xcd && marker <= 0xcf))
                {
                    texture.height = bytes[position + 3] * 256 + bytes[position + 4];
                    texture.width = bytes[position + 5] * 256 + bytes[position + 6];
                    return true;
                }
                position += length;
            }
            return false;
        }
        private static int Int32(byte[] bytes, int i)
        { return bytes[i] << 24 | bytes[i + 1] << 16 | bytes[i + 2] << 8 | bytes[i + 3]; }
    }

    public static class GUI
    {
        public static Color color = new Color(1, 1, 1, 1);
        public static readonly List<Rect> Drawn = new List<Rect>();
        public static int GroupDepth;
        public static void BeginGroup(Rect rect) { GroupDepth++; }
        public static void EndGroup() { GroupDepth--; }
        public static void DrawTexture(Rect rect, Texture2D texture, ScaleMode mode, bool alpha) { Drawn.Add(rect); }
    }

    public static class Debug
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void LogWarning(object text) { Warnings.Add(text.ToString()); }
    }
}
