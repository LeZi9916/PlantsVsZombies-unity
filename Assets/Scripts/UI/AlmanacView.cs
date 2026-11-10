using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using UnityEngine;
using PvZReplica.Reanimation;
using PvZReplica.VerifiedData;

namespace PvZReplica.UI
{
    public enum AlmanacScreen { Home, PlantIndex, PlantDetail, ZombieIndex, ZombieDetail }

    /// <summary>
    /// An independently hosted IMGUI almanac. Coordinates and clicks are in an 800x600
    /// logical canvas; the host owns the GUI matrix, input dispatch and renderer lifetime.
    /// </summary>
    public sealed class AlmanacView : IDisposable
    {
        private const int PlantCount = 49;
        private const int ZombieCount = 26;
        private static readonly Rect Canvas = new Rect(0, 0, 800, 600);
        private static readonly Rect PlantHome = new Rect(130, 345, 156, 42);
        private static readonly Rect ZombieHome = new Rect(487, 345, 210, 48);
        private static readonly Rect BackButton = new Rect(32, 567, 164, 26);
        private static readonly Rect CloseButton = new Rect(676, 567, 89, 26);
        private static readonly Color DarkText = new Color(40f / 255f, 50f / 255f, 90f / 255f, 1f);

        private readonly string root;
        private readonly ReanimationRenderer renderer;
        private readonly Dictionary<string, string> strings;
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> failedAnimations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> diagnosticSet = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> diagnostics = new List<string>();
        private readonly ReadOnlyCollection<string> readOnlyDiagnostics;
        private HashSet<int> unlockedPlants;
        private HashSet<int> unlockedZombies;
        private string formattedDescription;
        private float descriptionHeight, scrollOffset;
        private Rect descriptionRect;
        private Font font;
        private GUIStyle headingStyle, titleStyle, labelStyle, bodyStyle, smallStyle, iconStyle;
        private bool disposed;
        private int selectedPlantId, selectedZombieId;
        private float animationEpoch = float.NaN;
        private Vector2 pointer = new Vector2(-1000f, -1000f);
        private PointerCapture capture;
        private float scrollGrabOffset;

        private enum PointerCapture { None, Plants, Zombies, Index, Close, ScrollBar }

        public AlmanacScreen Screen { get; private set; }
        public bool IsClosed { get; private set; }
        public int SelectedId { get; private set; }
        public int DetailPage
        {
            get
            {
                if (descriptionRect.height <= 0f) return 0;
                float maxOffset = descriptionHeight - descriptionRect.height;
                if (maxOffset > 0f && scrollOffset >= maxOffset - 0.01f) return DetailPageCount - 1;
                return (int)(scrollOffset / descriptionRect.height);
            }
        }
        public int DetailPageCount { get { return descriptionRect.height <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(descriptionHeight / descriptionRect.height)); } }
        public IReadOnlyList<string> Diagnostics { get { return readOnlyDiagnostics; } }

        public AlmanacView(string resourceRoot, ReanimationRenderer reanimationRenderer)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            if (reanimationRenderer == null) throw new ArgumentNullException(nameof(reanimationRenderer));
            root = Path.GetFullPath(resourceRoot);
            renderer = reanimationRenderer;
            string path = Path.Combine(root, "properties", "LawnStrings.txt");
            if (!File.Exists(path)) throw new FileNotFoundException("The original LawnStrings.txt is required.", path);
            strings = ReadStrings(path);
            readOnlyDiagnostics = diagnostics.AsReadOnly();
            Open();
        }

        /// <summary>Reopens the home page. Previously supplied unlock sets are retained.</summary>
        public void Open()
        {
            ThrowIfDisposed();
            selectedPlantId = selectedZombieId = 0;
            IsClosed = false;
            ShowHome();
        }

        /// <summary>Null means all unlocked; otherwise IDs are original plant IDs 0..48.</summary>
        public void SetUnlockedPlants(IEnumerable<int> ids)
        {
            ThrowIfDisposed();
            unlockedPlants = CopyUnlocks(ids, PlantCount);
            ClearLockedSelection(true);
        }

        /// <summary>Null means all unlocked; otherwise IDs are original zombie IDs 0..25.</summary>
        public void SetUnlockedZombies(IEnumerable<int> ids)
        {
            ThrowIfDisposed();
            unlockedZombies = CopyUnlocks(ids, ZombieCount);
            ClearLockedSelection(false);
        }

        /// <summary>Draw from OnGUI after the host sets its logical 800x600 GUI matrix.</summary>
        public void Draw(float seconds)
        {
            ThrowIfDisposed();
            if (IsClosed) return;
            EnsureStyles();
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f) seconds = 0f;
            if (float.IsNaN(animationEpoch) || seconds < animationEpoch) animationEpoch = seconds;
            float animationSeconds = seconds - animationEpoch;
            Color oldColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                if (Screen == AlmanacScreen.Home) DrawHome(animationSeconds);
                else DrawCategory(animationSeconds);
                DrawButton(CloseButton, "Almanac_CloseButton.png", "Almanac_CloseButtonHighlight.png", Text("CLOSE_BUTTON"), labelStyle, -8);
            }
            finally { GUI.color = oldColor; }
        }

        /// <summary>
        /// Convenience for a complete primary click; live hosts should forward separate
        /// pointer events. Entries select on press; navigation commits on release.
        /// </summary>
        public bool HandleClick(Vector2 point)
        {
            ThrowIfDisposed();
            if (IsClosed || !Canvas.Contains(point)) return false;
            bool control = NavigationAt(point) != PointerCapture.None || SlotAt(point) >= 0 ||
                (CanScroll && ScrollBarRect().Contains(point));
            HandlePointer(point, EventType.MouseDown);
            HandlePointer(point, EventType.MouseUp);
            return control;
        }

        /// <summary>
        /// Owns primary down/drag/up and movement while the modal is open. Navigation
        /// requires a press and release over the same button; dragging out cancels it.
        /// </summary>
        public bool HandlePointer(Vector2 point, EventType type)
        {
            ThrowIfDisposed();
            if (IsClosed || (type != EventType.MouseDown && type != EventType.MouseUp &&
                type != EventType.MouseMove && type != EventType.MouseDrag)) return false;
            pointer = point;
            if (type == EventType.MouseDown)
            {
                capture = NavigationAt(point);
                if (capture != PointerCapture.None) return true;
                if (CanScroll && ScrollBarRect().Contains(point))
                {
                    Rect thumb = ScrollThumbRect();
                    capture = PointerCapture.ScrollBar;
                    scrollGrabOffset = thumb.Contains(point) ? point.y - thumb.y : thumb.height * 0.5f;
                    DragScrollBar(point.y);
                    return true;
                }
                int id = SlotAt(point);
                if (id >= 0 && SelectedId != id)
                {
                    SelectedId = id;
                    if (IsPlantScreen) selectedPlantId = id;
                    else selectedZombieId = id;
                    Screen = IsPlantScreen ? AlmanacScreen.PlantDetail : AlmanacScreen.ZombieDetail;
                    ResetDescription();
                    animationEpoch = float.NaN;
                }
            }
            else if (type == EventType.MouseDrag && capture == PointerCapture.ScrollBar)
                DragScrollBar(point.y);
            else if (type == EventType.MouseUp)
            {
                PointerCapture pressed = capture;
                capture = PointerCapture.None;
                if (pressed == PointerCapture.ScrollBar) DragScrollBar(point.y);
                else if (pressed != PointerCapture.None && pressed == NavigationAt(point))
                {
                    if (pressed == PointerCapture.Close) IsClosed = true;
                    else if (pressed == PointerCapture.Index) ShowHome();
                    else SetCategory(pressed == PointerCapture.Plants);
                }
            }
            return true;
        }

        private PointerCapture NavigationAt(Vector2 point)
        {
            if (!Canvas.Contains(point)) return PointerCapture.None;
            if (CloseButton.Contains(point)) return PointerCapture.Close;
            if (Screen == AlmanacScreen.Home)
            {
                if (PlantHome.Contains(point)) return PointerCapture.Plants;
                if (ZombieHome.Contains(point)) return PointerCapture.Zombies;
            }
            else if (BackButton.Contains(point)) return PointerCapture.Index;
            return PointerCapture.None;
        }

        private int SlotAt(Vector2 point)
        {
            if (Screen == AlmanacScreen.Home || !Canvas.Contains(point)) return -1;
            bool plants = IsPlantScreen;
            int count = plants ? PlantCount : ZombieCount;
            for (int id = 0; id < count; id++)
                if (IsUnlocked(plants, id) && SlotRect(plants, id).Contains(point)) return id;
            return -1;
        }

        /// <summary>Returns to the home index, retaining category selections, or closes home.</summary>
        public bool HandleBack()
        {
            ThrowIfDisposed();
            if (IsClosed) return false;
            if (Screen == AlmanacScreen.Home) { IsClosed = true; capture = PointerCapture.None; }
            else ShowHome();
            return true;
        }

        private void ShowHome()
        {
            Screen = AlmanacScreen.Home;
            SelectedId = -1;
            capture = PointerCapture.None;
            animationEpoch = float.NaN;
            ResetDescription();
        }

        private bool CanScroll
        {
            get { return Screen != AlmanacScreen.Home && formattedDescription != null &&
                descriptionRect.height > 0f && descriptionHeight > descriptionRect.height; }
        }

        /// <summary>Native wheel behavior advances five units per nonzero wheel event.</summary>
        public bool HandleScroll(Vector2 point, float amount)
        {
            ThrowIfDisposed();
            if (IsClosed || !CanScroll || !descriptionRect.Contains(point) ||
                float.IsNaN(amount) || float.IsInfinity(amount) || amount == 0f) return false;
            scrollOffset = Math.Max(0f, Math.Min(descriptionHeight - descriptionRect.height,
                scrollOffset + Math.Sign(amount) * 5f));
            return true;
        }

        private void DragScrollBar(float y)
        {
            if (!CanScroll || float.IsNaN(y) || float.IsInfinity(y)) return;
            Rect bar = ScrollBarRect(), thumb = ScrollThumbRect();
            float travel = bar.height - thumb.height;
            if (travel <= 0f) return;
            float fraction = Math.Max(0f, Math.Min(1f, (y - bar.y - scrollGrabOffset) / travel));
            scrollOffset = fraction * (descriptionHeight - descriptionRect.height);
        }

        private bool IsPlantScreen { get { return Screen == AlmanacScreen.PlantIndex || Screen == AlmanacScreen.PlantDetail; } }

        private void SetCategory(bool plants)
        {
            int rememberedId = plants ? selectedPlantId : selectedZombieId;
            SelectedId = rememberedId >= 0 && IsUnlocked(plants, rememberedId) ? rememberedId : -1;
            Screen = plants ? (SelectedId < 0 ? AlmanacScreen.PlantIndex : AlmanacScreen.PlantDetail) :
                (SelectedId < 0 ? AlmanacScreen.ZombieIndex : AlmanacScreen.ZombieDetail);
            capture = PointerCapture.None;
            animationEpoch = float.NaN;
            ResetDescription();
        }

        private static HashSet<int> CopyUnlocks(IEnumerable<int> ids, int count)
        {
            if (ids == null) return null;
            HashSet<int> result = new HashSet<int>();
            foreach (int id in ids)
            {
                if (id < 0 || id >= count) throw new ArgumentOutOfRangeException(nameof(ids), "Unknown almanac ID: " + id);
                result.Add(id);
            }
            return result;
        }

        private bool IsUnlocked(bool plants, int id)
        {
            HashSet<int> set = plants ? unlockedPlants : unlockedZombies;
            return set == null || set.Contains(id);
        }

        private void ClearLockedSelection(bool plants)
        {
            if (SelectedId < 0 || Screen == AlmanacScreen.Home || IsPlantScreen != plants || IsUnlocked(plants, SelectedId)) return;
            Screen = plants ? AlmanacScreen.PlantIndex : AlmanacScreen.ZombieIndex;
            SelectedId = -1;
            ResetDescription();
        }

        private void ResetDescription()
        {
            formattedDescription = null;
            descriptionHeight = 0f;
            descriptionRect = default(Rect);
            scrollOffset = 0f;
        }

        private void DrawHome(float seconds)
        {
            DrawBackground("Almanac_IndexBack.jpg");
            headingStyle.normal.textColor = new Color(220f / 255f, 220f / 255f, 220f / 255f, 1);
            GUI.Label(new Rect(100, 32, 600, 40), Text("SUBURBAN_ALMANAC_INDEX"), headingStyle);
            DrawNativePortrait(true, 1, new Vector2(167, 225), 1f, seconds, false);
            DrawNativePortrait(false, 0, new Vector2(535, 215), 1f, seconds, false);
            DrawButton(PlantHome, "SeedChooser_Button.png", "SeedChooser_Button_Glow.png", Text("VIEW_PLANTS"), labelStyle, 0);
            DrawButton(ZombieHome, "button_middle.png", "button_middle.png", Text("VIEW_ZOMBIES"), labelStyle, 0);
        }

        private void DrawCategory(float seconds)
        {
            bool plants = IsPlantScreen;
            DrawBackground(plants ? "Almanac_PlantBack.jpg" : "Almanac_ZombieBack.jpg");
            headingStyle.normal.textColor = plants ? new Color(213f / 255f, 159f / 255f, 43f / 255f, 1) : new Color(0, 196f / 255f, 0, 1);
            GUI.Label(new Rect(70, plants ? 20 : 26, 660, 42), Text(plants ? "SUBURBAN_ALMANAC_PLANTS" : "SUBURBAN_ALMANAC_ZOMBIES"), headingStyle);
            int count = plants ? PlantCount : ZombieCount;
            for (int id = 0; id < count; id++) DrawSlot(plants, id, SlotRect(plants, id), seconds);
            if (SelectedId >= 0) DrawDetail(plants, SelectedId, seconds);
            DrawButton(BackButton, "Almanac_IndexButton.png", "Almanac_IndexButtonHighlight.png", Text("ALMANAC_INDEX"), labelStyle, 8);
        }

        internal string PlantDisplayName(int id)
        {
            if (id < 0 || id >= PlantCount) throw new ArgumentOutOfRangeException(nameof(id));
            return Text(EntryKey(true, id));
        }

        private static Rect SlotRect(bool plants, int id)
        {
            if (plants) return id == 48 ? new Rect(20, 23, 36, 46) : new Rect(26 + (id % 8) * 52, 92 + (id / 8) * 78, 50, 70);
            return id == 25 ? new Rect(192, 486, 76, 76) : new Rect(22 + (id % 5) * 85, 86 + (id / 5) * 80, 76, 76);
        }

        private void DrawSlot(bool plants, int id, Rect rect, float seconds)
        {
            if (!IsUnlocked(plants, id))
            {
                if (!plants) DrawImage("Almanac_ZombieBlank.png", rect);
                return;
            }
            if (plants && id == 48)
            {
                DrawImage("Almanac_Imitater.png", new Rect(rect.x, rect.y, 34, 43));
                return;
            }
            if (plants)
            {
                Texture2D seeds = Texture("seeds.png");
                int backgroundCell = id >= 40 && id <= 47 ? 1 : 2;
                if (seeds != null) GUI.DrawTextureWithTexCoords(rect, seeds, new Rect(backgroundCell / 9f, 0, 1f / 9f, 1));
                else Report("Original seed packet atlas is missing: seeds.png");
            }
            else DrawImage("Almanac_ZombieWindow.png", rect);
            if (plants)
            {
                int cell = AlmanacPortraitLayout.PlantAtlasCell(id);
                if (cell >= 0)
                {
                    Texture2D atlas = Texture("packet_plants.png");
                    if (atlas != null) GUI.DrawTextureWithTexCoords(rect, atlas, new Rect(cell / 13f, 0, 1f / 13f, 1));
                }
                else
                {
                    Vector2 origin;
                    float scale;
                    AlmanacPortraitLayout.PlantThumbnailTransform(id, rect, out origin, out scale);
                    DrawNativePortrait(true, id, origin, scale, 0f, true);
                }
                GUI.Label(new Rect(rect.x + 2f, rect.y + 53f, rect.width - 7f, 15f),
                    VerifiedDataCatalog.Plants[id].SunCost.ToString(), iconStyle);
            }
            else
            {
                Rect clip = new Rect(rect.x + 2, rect.y + 2, 72, 72);
                Vector2 origin = id == 25 ? new Vector2(rect.x + 1, rect.y - 6) :
                    AlmanacPortraitLayout.ZombieThumbnailOrigin(id, rect);
                if (!renderer.DrawNativeClipped("almanac-zombie-thumb-" + id, clip,
                    () => DrawNativePortrait(false, id, origin, 0.5f, 0f, true), true))
                    Report("zombie thumbnail " + id + ": " + renderer.LastError);
                DrawImage("Almanac_ZombieWindow2.png", rect);
            }
            if (rect.Contains(pointer))
            {
                Outline(rect, new Color(1f, 1f, 1f, 0.35f), 2f);
            }
        }

        private void DrawDetail(bool plants, int id, float seconds)
        {
            Rect ground = plants ? new Rect(521, 107, 200, 200) : new Rect(518, 110, 200, 200);
            DrawImage(GroundFor(plants, id), ground);
            if (plants)
                DrawNativePortrait(true, id, AlmanacPortraitLayout.PlantPreviewOrigin(id), 1f, seconds, false);
            else
            {
                Rect clip = AlmanacPortraitLayout.ZombiePreviewClip;
                Vector2 origin = AlmanacPortraitLayout.ZombiePreviewOrigin(id);
                if (!renderer.DrawNativeClipped("almanac-zombie-preview", clip,
                    () => DrawNativePortrait(false, id, origin, 1f, seconds, false)))
                    Report("zombie preview " + id + ": " + renderer.LastError);
            }
            DrawImage(plants ? "Almanac_PlantCard.png" : "Almanac_ZombieCard.png",
                plants ? new Rect(459, 86, 324, 484) : new Rect(455, 78, 324, 497));
            string key = EntryKey(plants, id);
            GUI.Label(plants ? new Rect(480, 270, 274, 35) : new Rect(480, 344, 268, 35), Text(key), titleStyle);
            Rect header = plants ? new Rect(485, 309, 258, 100) : new Rect(485, 377, 258, 100);
            string headerText = Text(key + "_DESCRIPTION_HEADER");
            float headerHeight = Math.Min(80f, bodyStyle.CalcHeight(new GUIContent(headerText), header.width));
            header.height = headerHeight;
            GUI.Label(header, headerText, bodyStyle);
            float bottom = plants ? (id == 48 ? 539f : 505f) : 537f;
            descriptionRect = new Rect(485, header.yMax + 3f, 258, Math.Max(20f, bottom - header.yMax - 3f));
            if (formattedDescription == null)
                formattedDescription = FormatDescription(Text(key + "_DESCRIPTION"));
            descriptionHeight = bodyStyle.CalcHeight(new GUIContent(formattedDescription), descriptionRect.width - 8f);
            scrollOffset = Math.Max(0f, Math.Min(scrollOffset, Math.Max(0f, descriptionHeight - descriptionRect.height)));
            GUI.BeginGroup(descriptionRect);
            GUI.Label(new Rect(0, -scrollOffset, descriptionRect.width - 8f, descriptionHeight + 2f), formattedDescription, bodyStyle);
            GUI.EndGroup();
            if (descriptionHeight > descriptionRect.height) DrawScrollBar();
            if (plants && id != 48)
            {
                PlantDefinition plant = VerifiedDataCatalog.Plants[id];
                string recharge = plant.RechargeThresholdRaw == 750 ? "WAIT_TIME_SHORT" :
                    plant.RechargeThresholdRaw == 3000 ? "WAIT_TIME_LONG" : "WAIT_TIME_VERY_LONG";
                GUI.Label(new Rect(485, 520, 130, 30), Text("COST") + ": " + plant.SunCost, smallStyle);
                GUI.Label(new Rect(610, 520, 132, 30), Text("WAIT_TIME") + ": " + Text(recharge), smallStyle);
            }
        }

        private Rect ScrollBarRect()
        {
            return new Rect(descriptionRect.xMax - 8f, descriptionRect.y, 8f, descriptionRect.height);
        }

        private Rect ScrollThumbRect()
        {
            Rect bar = ScrollBarRect();
            float height = Math.Min(bar.height, Math.Max(12f, bar.height * bar.height / descriptionHeight));
            float position = scrollOffset / (descriptionHeight - descriptionRect.height);
            return new Rect(bar.x, bar.y + position * (bar.height - height), bar.width, height);
        }

        private void DrawScrollBar()
        {
            Fill(ScrollBarRect(), new Color(0.32f, 0.27f, 0.34f, 0.35f));
            Fill(ScrollThumbRect(), new Color(0.42f, 0.36f, 0.43f, 0.9f));
        }

        private static string GroundFor(bool plants, int id)
        {
            if (!plants) return id == 12 || id == 13 ? "Almanac_GroundIce.jpg" : "Almanac_GroundDay.jpg";
            if (id == 16 || id == 19 || id == 43) return "Almanac_GroundPool.jpg";
            if (id == 24) return "Almanac_GroundNightPool.jpg";
            if ((id >= 8 && id <= 15) || id == 25 || id == 31 || id == 42)
                return "Almanac_GroundNight.jpg";
            return id == 33 ? "Almanac_GroundRoof.jpg" : "Almanac_GroundDay.jpg";
        }

        private bool DrawNativePortrait(bool plants, int id, Vector2 origin, float scale, float seconds, bool thumbnail)
        {
            string failureKey = (plants ? "plant:" : "zombie:") + id;
            bool drawn = false;
            if (!failedAnimations.Contains(failureKey))
            {
                ReanimationDefinition animation;
                int animationId = plants ? VerifiedDataCatalog.Plants[id].ReanimationId : VerifiedDataCatalog.Zombies[id].ReanimationId;
                if (VerifiedDataCatalog.TryGetReanimation(animationId, out animation))
                {
                    string name = Path.GetFileNameWithoutExtension(animation.Path.Replace('\\', '/'));
                    if (!plants && id == 25)
                        drawn = renderer.DrawAlmanacBoss(origin, scale, seconds, thumbnail);
                    else if (!plants && (id == 0 || id == 1 || id == 2 || id == 4 || id == 6))
                    {
                        ZombieVariant variant = id == 1 ? ZombieVariant.Flag : id == 2 ? ZombieVariant.Conehead :
                            id == 4 ? ZombieVariant.Buckethead : id == 6 ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
                        drawn = renderer.DrawZombieNative(origin, scale, seconds, variant, "anim_idle", thumbnail);
                    }
                    else if (!plants && id == 10)
                        drawn = renderer.DrawNative(name, origin, scale, seconds, "anim_idle", DuckyTrackVisible);
                    else
                    {
                        string clip = plants && id == 4 ? "anim_armed" : plants && id == 9 && !thumbnail ? "anim_bigidle" :
                            !plants && id == 18 ? "anim_pogo" : !plants && id == 24 ? "anim_walk" :
                            !plants && id == 9 && !thumbnail ? "anim_armraise" :
                            !plants && id == 3 && !thumbnail ? "anim_walk" : null;
                        Func<string, bool> filter = plants && id == 34 ? (Func<string, bool>)KernelTrackVisible : null;
                        drawn = renderer.DrawNative(name, origin, scale, seconds, clip, filter);
                    }
                    if (!drawn)
                    {
                        string error = renderer.LastError ?? "No image was drawn.";
                        Report(failureKey + ": " + error);
                        if (!error.StartsWith("No visible image tracks", StringComparison.Ordinal)) failedAnimations.Add(failureKey);
                    }
                }
                else { Report(failureKey + ": missing reanimation catalog ID " + animationId); failedAnimations.Add(failureKey); }
            }
            return drawn;
        }

        private static bool KernelTrackVisible(string track)
        {
            return !string.Equals(track, "Cornpult_butter", StringComparison.OrdinalIgnoreCase);
        }

        private static bool DuckyTrackVisible(string track)
        {
            return string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(track, "Zombie_whitewater", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(track, "Zombie_whitewater2", StringComparison.OrdinalIgnoreCase) ||
                ReanimationRenderer.IsZombieTrackVisible(track, ZombieVariant.Normal);
        }

        private static string EntryKey(bool plants, int id)
        {
            return plants ? VerifiedDataCatalog.Plants[id].InternalName : VerifiedDataCatalog.Zombies[id].InternalName;
        }

        private void EnsureStyles()
        {
            if (bodyStyle != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" }, 16);
            headingStyle = Style(25, TextAnchor.MiddleCenter, Color.white, true);
            titleStyle = Style(19, TextAnchor.MiddleCenter, DarkText, true);
            labelStyle = Style(15, TextAnchor.MiddleCenter, DarkText, true);
            smallStyle = Style(14, TextAnchor.MiddleLeft, DarkText, false);
            iconStyle = Style(12, TextAnchor.MiddleRight, Color.black, true);
            bodyStyle = Style(14, TextAnchor.UpperLeft, DarkText, false);
            bodyStyle.wordWrap = true;
            bodyStyle.richText = true;
            bodyStyle.clipping = TextClipping.Clip;
        }

        private GUIStyle Style(int size, TextAnchor anchor, Color color, bool bold)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.font = font;
            style.fontSize = size;
            style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            style.alignment = anchor;
            style.normal.textColor = color;
            style.padding = new RectOffset(0, 0, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            return style;
        }

        private static string FormatDescription(string value)
        {
            StringBuilder result = new StringBuilder();
            bool colored = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '{')
                {
                    int end = value.IndexOf('}', i + 1);
                    if (end > i)
                    {
                        string token = value.Substring(i + 1, end - i - 1);
                        string color = token == "KEYWORD" ? "#873222" : token == "FLAVOR" ? "#624C42" :
                            token == "NOCTURNAL" || token == "AQUATIC" ? "#9A351F" : token == "STAT" ? "#28325A" : null;
                        if (color != null)
                        {
                            if (colored) result.Append("</color>");
                            result.Append("<color=").Append(color).Append('>');
                            colored = true;
                        }
                        else if (token != "SHORTLINE" && token != "METAL")
                        {
                            int number;
                            if (int.TryParse(token, out number)) result.Append('(').Append(token).Append(')');
                            else result.Append('{').Append(token).Append('}');
                        }
                        i = end;
                        continue;
                    }
                }
                if (value[i] != '\r') result.Append(value[i]);
            }
            if (colored) result.Append("</color>");
            return result.ToString();
        }

        private string Text(string key)
        {
            string value;
            if (strings.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
            Report("Missing LawnStrings entry: " + key);
            return "[" + key + "]";
        }

        private static Dictionary<string, string> ReadStrings(string path)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string key = null;
            StringBuilder value = new StringBuilder();
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (IsStringKey(trimmed))
                    {
                        if (key != null) result[key] = value.ToString().TrimEnd('\r', '\n');
                        key = trimmed.Substring(1, trimmed.Length - 2);
                        value.Length = 0;
                    }
                    else if (key != null)
                    {
                        if (value.Length > 0) value.Append('\n');
                        value.Append(line);
                    }
                }
            }
            if (key != null) result[key] = value.ToString().TrimEnd('\r', '\n');
            return result;
        }

        private static bool IsStringKey(string text)
        {
            if (text.Length < 3 || text[0] != '[' || text[text.Length - 1] != ']') return false;
            for (int i = 1; i < text.Length - 1; i++)
            {
                char c = text[i];
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')) return false;
            }
            return true;
        }

        private Texture2D Texture(string name)
        {
            Texture2D texture;
            if (textures.TryGetValue(name, out texture)) return texture;
            string path = Path.Combine(root, "images", name);
            texture = null;
            try
            {
                if (File.Exists(path))
                {
                    byte[] data = File.ReadAllBytes(path);
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(texture, data, true)) { Destroy(texture); texture = null; }
                    else { texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp; }
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Destroy(texture);
                texture = null;
                Report("Could not read " + path + ": " + error.Message);
            }
            if (texture == null) Report("Missing or invalid image: " + path);
            textures[name] = texture;
            return texture;
        }

        private void DrawBackground(string name)
        {
            DrawImage(name, Canvas);
        }

        private bool DrawImage(string name, Rect rect)
        {
            Texture2D texture = Texture(name);
            if (texture == null) return false;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            return true;
        }

        private void DrawButton(Rect rect, string normal, string highlight, string text, GUIStyle style, int textOffset)
        {
            bool hovered=rect.Contains(pointer),down=hovered&&Input.GetMouseButton(0);
            if(normal=="button_middle.png")
            {
                OriginalUiWidgets.StoneButton(Texture,rect,hovered,down);
                if(!OriginalUiWidgets.StoneLabel(rect,text,hovered,down)&&!string.IsNullOrEmpty(text))
                    GUI.Label(new Rect(rect.x+textOffset,rect.y,rect.width,rect.height),text,style);
            }
            else
            {
                OriginalUiWidgets.ImageButton(Texture,rect,normal,highlight,null,hovered,down);
                if(!string.IsNullOrEmpty(text))GUI.Label(new Rect(rect.x+textOffset,rect.y,rect.width,rect.height),text,style);
            }
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void Outline(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void Report(string message)
        {
            if (diagnosticSet.Add(message)) diagnostics.Add(message);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(AlmanacView));
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        /// <summary>Releases this view's textures and font, but not the supplied renderer.</summary>
        public void Dispose()
        {
            if (disposed) return;
            foreach (Texture2D texture in textures.Values) Destroy(texture);
            textures.Clear();
            Destroy(font);
            disposed = true;
        }
    }
}
