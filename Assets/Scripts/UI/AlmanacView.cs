#nullable enable
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.VerifiedData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>
    /// An independently hosted IMGUI almanac. Coordinates and clicks are in an 800x600
    /// logical canvas; the host owns the GUI matrix, input dispatch and renderer lifetime.
    /// </summary>
    public sealed class AlmanacView : IDisposable
    {
        /// <summary>Stores plant count.</summary>
        private const int PlantCount = 49;
        /// <summary>Stores zombie count.</summary>
        private const int ZombieCount = 26;
        /// <summary>Stores canvas.</summary>
        private static readonly Rect s_canvas = new Rect(0, 0, 800, 600);
        /// <summary>Stores plant home.</summary>
        private static readonly Rect s_plantHome = new Rect(130, 345, 156, 42);
        /// <summary>Stores zombie home.</summary>
        private static readonly Rect s_zombieHome = new Rect(487, 345, 210, 48);
        /// <summary>Stores back button.</summary>
        private static readonly Rect s_backButton = new Rect(32, 567, 164, 26);
        /// <summary>Stores close button.</summary>
        private static readonly Rect s_closeButton = new Rect(676, 567, 89, 26);
        /// <summary>Stores dark text.</summary>
        private static readonly Color s_darkText = new Color(40f / 255f, 50f / 255f, 90f / 255f, 1f);
        /// <summary>Stores root.</summary>
        private readonly string _root;
        /// <summary>Stores renderer.</summary>
        private readonly ReanimationRenderer _renderer;
        /// <summary>Stores strings.</summary>
        private readonly Dictionary<string, string> _strings;
        /// <summary>Stores textures.</summary>
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Stores failed animations.</summary>
        private readonly HashSet<string> _failedAnimations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Stores diagnostic set.</summary>
        private readonly HashSet<string> _diagnosticSet = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>Stores diagnostics.</summary>
        private readonly List<string> _diagnostics = new List<string>();
        /// <summary>Stores read only diagnostics.</summary>
        private readonly ReadOnlyCollection<string> _readOnlyDiagnostics;
        /// <summary>Stores unlocked plants.</summary>
        private HashSet<int> _unlockedPlants;
        /// <summary>Stores unlocked zombies.</summary>
        private HashSet<int> _unlockedZombies;
        /// <summary>Stores formatted description.</summary>
        private string _formattedDescription;
        /// <summary>Stores description height,  scroll offset.</summary>
        private float _descriptionHeight, _scrollOffset;
        /// <summary>Stores description rect.</summary>
        private Rect _descriptionRect;
        /// <summary>Stores font.</summary>
        private Font _font;
        /// <summary>Stores heading style,  title style,  label style,  body style,  small style,  icon style.</summary>
        private GUIStyle _headingStyle, _titleStyle, _labelStyle, _bodyStyle, _smallStyle, _iconStyle;
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <summary>Stores selected plant id,  selected zombie id.</summary>
        private int _selectedPlantId, _selectedZombieId;
        /// <summary>Stores animation epoch.</summary>
        private float _animationEpoch = float.NaN;
        /// <summary>Stores pointer.</summary>
        private Vector2 _pointer = new Vector2(-1000f, -1000f);
        /// <summary>Stores capture.</summary>
        private PointerCapture _capture;
        /// <summary>Stores scroll grab offset.</summary>
        private float _scrollGrabOffset;
        /// <summary>Defines pointer capture.</summary>
        private enum PointerCapture
        {
            /// <summary>Identifies none.</summary>
            None,
            /// <summary>Identifies plants.</summary>
            Plants,
            /// <summary>Identifies zombies.</summary>
            Zombies,
            /// <summary>Identifies index.</summary>
            Index,
            /// <summary>Identifies close.</summary>
            Close,
            /// <summary>Identifies scroll bar.</summary>
            ScrollBar
        }

        /// <summary>Gets or sets screen.</summary>
        public AlmanacScreen Screen { get; private set; }
        /// <summary>Gets or sets is closed.</summary>
        public bool IsClosed { get; private set; }
        /// <summary>Gets or sets selected id.</summary>
        public int SelectedId { get; private set; }

        /// <summary>Gets or sets detail page.</summary>
        public int DetailPage
        {
            get
            {
                if (_descriptionRect.height <= 0f)
                {
                    return 0;
                }

                var maxOffset = _descriptionHeight - _descriptionRect.height;
                if (maxOffset > 0f && _scrollOffset >= maxOffset - 0.01f)
                {
                    return DetailPageCount - 1;
                }

                return (int)(_scrollOffset / _descriptionRect.height);
            }
        }

        /// <summary>Gets or sets detail page count.</summary>
        public int DetailPageCount
        {
            get
            {
                return _descriptionRect.height <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(_descriptionHeight / _descriptionRect.height));
            }
        }

        /// <summary>Gets or sets diagnostics.</summary>
        public IReadOnlyList<string> Diagnostics
        {
            get
            {
                return _readOnlyDiagnostics;
            }
        }

        /// <summary>Initializes almanac view.</summary>
        /// <param name = "resourceRoot">The resource root.</param>
        /// <param name = "reanimationRenderer">The reanimation renderer.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "FileNotFoundException">The operation cannot satisfy its preconditions.</exception>
        public AlmanacView(string resourceRoot, ReanimationRenderer reanimationRenderer)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
            {
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            }

            if (reanimationRenderer == null)
            {
                throw new ArgumentNullException(nameof(reanimationRenderer));
            }

            _root = Path.GetFullPath(resourceRoot);
            _renderer = reanimationRenderer;
            var path = Path.Combine(_root, "properties", "LawnStrings.txt");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The original LawnStrings.txt is required.", path);
            }

            _strings = ReadStrings(path);
            _readOnlyDiagnostics = _diagnostics.AsReadOnly();
            Open();
        }

        /// <summary>Reopens the home page. Previously supplied unlock sets are retained.</summary>
        public void Open()
        {
            ThrowIfDisposed();
            _selectedPlantId = _selectedZombieId = 0;
            IsClosed = false;
            ShowHome();
        }

        /// <summary>Null means all unlocked; otherwise IDs are original plant IDs 0..48.</summary>
        public void SetUnlockedPlants(IEnumerable<int> ids)
        {
            ThrowIfDisposed();
            _unlockedPlants = CopyUnlocks(ids, PlantCount);
            ClearLockedSelection(true);
        }

        /// <summary>Null means all unlocked; otherwise IDs are original zombie IDs 0..25.</summary>
        public void SetUnlockedZombies(IEnumerable<int> ids)
        {
            ThrowIfDisposed();
            _unlockedZombies = CopyUnlocks(ids, ZombieCount);
            ClearLockedSelection(false);
        }

        /// <summary>Draw from OnGUI after the host sets its logical 800x600 GUI matrix.</summary>
        public void Draw(float seconds)
        {
            ThrowIfDisposed();
            if (IsClosed)
            {
                return;
            }

            EnsureStyles();
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
            {
                seconds = 0f;
            }

            if (float.IsNaN(_animationEpoch) || seconds < _animationEpoch)
            {
                _animationEpoch = seconds;
            }

            var animationSeconds = seconds - _animationEpoch;
            var oldColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                if (Screen == AlmanacScreen.Home)
                {
                    DrawHome(animationSeconds);
                }
                else
                {
                    DrawCategory(animationSeconds);
                }

                DrawButton(s_closeButton, "Almanac_CloseButton.png", "Almanac_CloseButtonHighlight.png", Text("CLOSE_BUTTON"), _labelStyle, -8);
            }
            finally
            {
                GUI.color = oldColor;
            }
        }

        /// <summary>
        /// Convenience for a complete primary click; live hosts should forward separate
        /// pointer events. Entries select on press; navigation commits on release.
        /// </summary>
        public bool HandleClick(Vector2 point)
        {
            ThrowIfDisposed();
            if (IsClosed || !s_canvas.Contains(point))
            {
                return false;
            }

            var control = NavigationAt(point) != PointerCapture.None || SlotAt(point) >= 0 || (CanScroll && ScrollBarRect().Contains(point));
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
            if (IsClosed || (type != EventType.MouseDown && type != EventType.MouseUp && type != EventType.MouseMove && type != EventType.MouseDrag))
            {
                return false;
            }

            _pointer = point;
            if (type == EventType.MouseDown)
            {
                _capture = NavigationAt(point);
                if (_capture != PointerCapture.None)
                {
                    return true;
                }

                if (CanScroll && ScrollBarRect().Contains(point))
                {
                    var thumb = ScrollThumbRect();
                    _capture = PointerCapture.ScrollBar;
                    _scrollGrabOffset = thumb.Contains(point) ? point.y - thumb.y : thumb.height * 0.5f;
                    DragScrollBar(point.y);
                    return true;
                }

                var id = SlotAt(point);
                if (id >= 0 && SelectedId != id)
                {
                    SelectedId = id;
                    if (IsPlantScreen)
                    {
                        _selectedPlantId = id;
                    }
                    else
                    {
                        _selectedZombieId = id;
                    }

                    Screen = IsPlantScreen ? AlmanacScreen.PlantDetail : AlmanacScreen.ZombieDetail;
                    ResetDescription();
                    _animationEpoch = float.NaN;
                }
            }
            else if (type == EventType.MouseDrag && _capture == PointerCapture.ScrollBar)
            {
                DragScrollBar(point.y);
            }
            else if (type == EventType.MouseUp)
            {
                var pressed = _capture;
                _capture = PointerCapture.None;
                if (pressed == PointerCapture.ScrollBar)
                {
                    DragScrollBar(point.y);
                }
                else if (pressed != PointerCapture.None && pressed == NavigationAt(point))
                {
                    if (pressed == PointerCapture.Close)
                    {
                        IsClosed = true;
                    }
                    else if (pressed == PointerCapture.Index)
                    {
                        ShowHome();
                    }
                    else
                    {
                        SetCategory(pressed == PointerCapture.Plants);
                    }
                }
            }

            return true;
        }

        /// <summary>Performs navigation at.</summary>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting navigation at.</returns>
        private PointerCapture NavigationAt(Vector2 point)
        {
            if (!s_canvas.Contains(point))
            {
                return PointerCapture.None;
            }

            if (s_closeButton.Contains(point))
            {
                return PointerCapture.Close;
            }

            if (Screen == AlmanacScreen.Home)
            {
                if (s_plantHome.Contains(point))
                {
                    return PointerCapture.Plants;
                }

                if (s_zombieHome.Contains(point))
                {
                    return PointerCapture.Zombies;
                }
            }
            else if (s_backButton.Contains(point))
            {
                return PointerCapture.Index;
            }

            return PointerCapture.None;
        }

        /// <summary>Performs slot at.</summary>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting slot at.</returns>
        private int SlotAt(Vector2 point)
        {
            if (Screen == AlmanacScreen.Home || !s_canvas.Contains(point))
            {
                return -1;
            }

            var plants = IsPlantScreen;
            var count = plants ? PlantCount : ZombieCount;
            for (var id = 0; id < count; id++)
            {
                if (IsUnlocked(plants, id) && SlotRect(plants, id).Contains(point))
                {
                    return id;
                }
            }

            return -1;
        }

        /// <summary>Returns to the home index, retaining category selections, or closes home.</summary>
        public bool HandleBack()
        {
            ThrowIfDisposed();
            if (IsClosed)
            {
                return false;
            }

            if (Screen == AlmanacScreen.Home)
            {
                IsClosed = true;
                _capture = PointerCapture.None;
            }
            else
            {
                ShowHome();
            }

            return true;
        }

        /// <summary>Performs show home.</summary>
        private void ShowHome()
        {
            Screen = AlmanacScreen.Home;
            SelectedId = -1;
            _capture = PointerCapture.None;
            _animationEpoch = float.NaN;
            ResetDescription();
        }

        /// <summary>Gets or sets can scroll.</summary>
        private bool CanScroll
        {
            get
            {
                return Screen != AlmanacScreen.Home && _formattedDescription != null && _descriptionRect.height > 0f && _descriptionHeight > _descriptionRect.height;
            }
        }

        /// <summary>Native wheel behavior advances five units per nonzero wheel event.</summary>
        public bool HandleScroll(Vector2 point, float amount)
        {
            ThrowIfDisposed();
            if (IsClosed || !CanScroll || !_descriptionRect.Contains(point) || float.IsNaN(amount) || float.IsInfinity(amount) || amount == 0f)
            {
                return false;
            }

            _scrollOffset = Math.Max(0f, Math.Min(_descriptionHeight - _descriptionRect.height, _scrollOffset + Math.Sign(amount) * 5f));
            return true;
        }

        /// <summary>Performs drag scroll bar.</summary>
        /// <param name = "y">The y.</param>
        private void DragScrollBar(float y)
        {
            if (!CanScroll || float.IsNaN(y) || float.IsInfinity(y))
            {
                return;
            }

            Rect bar = ScrollBarRect(), thumb = ScrollThumbRect();
            var travel = bar.height - thumb.height;
            if (travel <= 0f)
            {
                return;
            }

            var fraction = Math.Max(0f, Math.Min(1f, (y - bar.y - _scrollGrabOffset) / travel));
            _scrollOffset = fraction * (_descriptionHeight - _descriptionRect.height);
        }

        /// <summary>Gets or sets is plant screen.</summary>
        private bool IsPlantScreen
        {
            get
            {
                return Screen == AlmanacScreen.PlantIndex || Screen == AlmanacScreen.PlantDetail;
            }
        }

        /// <summary>Performs set category.</summary>
        /// <param name = "plants">The plants.</param>
        private void SetCategory(bool plants)
        {
            var rememberedId = plants ? _selectedPlantId : _selectedZombieId;
            SelectedId = rememberedId >= 0 && IsUnlocked(plants, rememberedId) ? rememberedId : -1;
            Screen = plants ? (SelectedId < 0 ? AlmanacScreen.PlantIndex : AlmanacScreen.PlantDetail) : (SelectedId < 0 ? AlmanacScreen.ZombieIndex : AlmanacScreen.ZombieDetail);
            _capture = PointerCapture.None;
            _animationEpoch = float.NaN;
            ResetDescription();
        }

        /// <summary>Performs copy unlocks.</summary>
        /// <param name = "ids">The ids.</param>
        /// <param name = "count">The count.</param>
        /// <returns>The resulting copy unlocks.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        private static HashSet<int> CopyUnlocks(IEnumerable<int> ids, int count)
        {
            if (ids == null)
            {
                return null;
            }

            var result = new HashSet<int>();
            foreach (int id in ids)
            {
                if (id < 0 || id >= count)
                {
                    throw new ArgumentOutOfRangeException(nameof(ids), "Unknown almanac ID: " + id);
                }

                result.Add(id);
            }

            return result;
        }

        /// <summary>Performs is unlocked.</summary>
        /// <param name = "plants">The plants.</param>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting is unlocked.</returns>
        private bool IsUnlocked(bool plants, int id)
        {
            var set = plants ? _unlockedPlants : _unlockedZombies;
            return set == null || set.Contains(id);
        }

        /// <summary>Performs clear locked selection.</summary>
        /// <param name = "plants">The plants.</param>
        private void ClearLockedSelection(bool plants)
        {
            if (SelectedId < 0 || Screen == AlmanacScreen.Home || IsPlantScreen != plants || IsUnlocked(plants, SelectedId))
            {
                return;
            }

            Screen = plants ? AlmanacScreen.PlantIndex : AlmanacScreen.ZombieIndex;
            SelectedId = -1;
            ResetDescription();
        }

        /// <summary>Performs reset description.</summary>
        private void ResetDescription()
        {
            _formattedDescription = null;
            _descriptionHeight = 0f;
            _descriptionRect = default(Rect);
            _scrollOffset = 0f;
        }

        /// <summary>Performs draw home.</summary>
        /// <param name = "seconds">The seconds.</param>
        private void DrawHome(float seconds)
        {
            DrawBackground("Almanac_IndexBack.jpg");
            _headingStyle.normal.textColor = new Color(220f / 255f, 220f / 255f, 220f / 255f, 1);
            GUI.Label(new Rect(100, 32, 600, 40), Text("SUBURBAN_ALMANAC_INDEX"), _headingStyle);
            DrawNativePortrait(true, 1, new Vector2(167, 225), 1f, seconds, false);
            DrawNativePortrait(false, 0, new Vector2(535, 215), 1f, seconds, false);
            DrawButton(s_plantHome, "SeedChooser_Button.png", "SeedChooser_Button_Glow.png", Text("VIEW_PLANTS"), _labelStyle, 0);
            DrawButton(s_zombieHome, "button_middle.png", "button_middle.png", Text("VIEW_ZOMBIES"), _labelStyle, 0);
        }

        /// <summary>Performs draw category.</summary>
        /// <param name = "seconds">The seconds.</param>
        private void DrawCategory(float seconds)
        {
            var plants = IsPlantScreen;
            DrawBackground(plants ? "Almanac_PlantBack.jpg" : "Almanac_ZombieBack.jpg");
            _headingStyle.normal.textColor = plants ? new Color(213f / 255f, 159f / 255f, 43f / 255f, 1) : new Color(0, 196f / 255f, 0, 1);
            GUI.Label(new Rect(70, plants ? 20 : 26, 660, 42), Text(plants ? "SUBURBAN_ALMANAC_PLANTS" : "SUBURBAN_ALMANAC_ZOMBIES"), _headingStyle);
            var count = plants ? PlantCount : ZombieCount;
            for (var id = 0; id < count; id++)
            {
                DrawSlot(plants, id, SlotRect(plants, id), seconds);
            }

            if (SelectedId >= 0)
            {
                DrawDetail(plants, SelectedId, seconds);
            }

            DrawButton(s_backButton, "Almanac_IndexButton.png", "Almanac_IndexButtonHighlight.png", Text("ALMANAC_INDEX"), _labelStyle, 8);
        }

        /// <summary>Performs plant display name.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting plant display name.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        internal string PlantDisplayName(int id)
        {
            if (id < 0 || id >= PlantCount)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return Text(EntryKey(true, id));
        }

        /// <summary>Performs slot rect.</summary>
        /// <param name = "plants">The plants.</param>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting slot rect.</returns>
        private static Rect SlotRect(bool plants, int id)
        {
            if (plants)
            {
                return id == 48 ? new Rect(20, 23, 36, 46) : new Rect(26 + (id % 8) * 52, 92 + (id / 8) * 78, 50, 70);
            }

            return id == 25 ? new Rect(192, 486, 76, 76) : new Rect(22 + (id % 5) * 85, 86 + (id / 5) * 80, 76, 76);
        }

        /// <summary>Performs draw slot.</summary>
        /// <param name = "plants">The plants.</param>
        /// <param name = "id">The id.</param>
        /// <param name = "rect">The rect.</param>
        /// <param name = "seconds">The seconds.</param>
        private void DrawSlot(bool plants, int id, Rect rect, float seconds)
        {
            if (!IsUnlocked(plants, id))
            {
                if (!plants)
                {
                    DrawImage("Almanac_ZombieBlank.png", rect);
                }

                return;
            }

            if (plants && id == 48)
            {
                DrawImage("Almanac_Imitater.png", new Rect(rect.x, rect.y, 34, 43));
                return;
            }

            if (plants)
            {
                var seeds = Texture("seeds.png");
                var backgroundCell = id >= 40 && id <= 47 ? 1 : 2;
                if (seeds != null)
                {
                    GUI.DrawTextureWithTexCoords(rect, seeds, new Rect(backgroundCell / 9f, 0, 1f / 9f, 1));
                }
                else
                {
                    Report("Original seed packet atlas is missing: seeds.png");
                }
            }
            else
            {
                DrawImage("Almanac_ZombieWindow.png", rect);
            }

            if (plants)
            {
                var cell = AlmanacPortraitLayout.PlantAtlasCell(id);
                if (cell >= 0)
                {
                    var atlas = Texture("packet_plants.png");
                    if (atlas != null)
                    {
                        GUI.DrawTextureWithTexCoords(rect, atlas, new Rect(cell / 13f, 0, 1f / 13f, 1));
                    }
                }
                else
                {
                    Vector2 origin;
                    float scale;
                    AlmanacPortraitLayout.PlantThumbnailTransform(id, rect, out origin, out scale);
                    DrawNativePortrait(true, id, origin, scale, 0f, true);
                }

                GUI.Label(new Rect(rect.x + 2f, rect.y + 53f, rect.width - 7f, 15f), VerifiedDataCatalog.Plants[id].SunCost.ToString(), _iconStyle);
            }
            else
            {
                var clip = new Rect(rect.x + 2, rect.y + 2, 72, 72);
                var origin = id == 25 ? new Vector2(rect.x + 1, rect.y - 6) : AlmanacPortraitLayout.ZombieThumbnailOrigin(id, rect);
                if (!_renderer.DrawNativeClipped("almanac-zombie-thumb-" + id, clip, () => DrawNativePortrait(false, id, origin, 0.5f, 0f, true), true))
                {
                    Report("zombie thumbnail " + id + ": " + _renderer.LastError);
                }

                DrawImage("Almanac_ZombieWindow2.png", rect);
            }

            if (rect.Contains(_pointer))
            {
                Outline(rect, new Color(1f, 1f, 1f, 0.35f), 2f);
            }
        }

        /// <summary>Performs draw detail.</summary>
        /// <param name = "plants">The plants.</param>
        /// <param name = "id">The id.</param>
        /// <param name = "seconds">The seconds.</param>
        private void DrawDetail(bool plants, int id, float seconds)
        {
            var ground = plants ? new Rect(521, 107, 200, 200) : new Rect(518, 110, 200, 200);
            DrawImage(GroundFor(plants, id), ground);
            if (plants)
            {
                DrawNativePortrait(true, id, AlmanacPortraitLayout.PlantPreviewOrigin(id), 1f, seconds, false);
            }
            else
            {
                var clip = AlmanacPortraitLayout.ZombiePreviewClip;
                var origin = AlmanacPortraitLayout.ZombiePreviewOrigin(id);
                if (!_renderer.DrawNativeClipped("almanac-zombie-preview", clip, () => DrawNativePortrait(false, id, origin, 1f, seconds, false)))
                {
                    Report("zombie preview " + id + ": " + _renderer.LastError);
                }
            }

            DrawImage(plants ? "Almanac_PlantCard.png" : "Almanac_ZombieCard.png", plants ? new Rect(459, 86, 324, 484) : new Rect(455, 78, 324, 497));
            var key = EntryKey(plants, id);
            GUI.Label(plants ? new Rect(480, 270, 274, 35) : new Rect(480, 344, 268, 35), Text(key), _titleStyle);
            var header = plants ? new Rect(485, 309, 258, 100) : new Rect(485, 377, 258, 100);
            var headerText = Text(key + "_DESCRIPTION_HEADER");
            var headerHeight = Math.Min(80f, _bodyStyle.CalcHeight(new GUIContent(headerText), header.width));
            header.height = headerHeight;
            GUI.Label(header, headerText, _bodyStyle);
            var bottom = plants ? (id == 48 ? 539f : 505f) : 537f;
            _descriptionRect = new Rect(485, header.yMax + 3f, 258, Math.Max(20f, bottom - header.yMax - 3f));
            if (_formattedDescription == null)
            {
                _formattedDescription = FormatDescription(Text(key + "_DESCRIPTION"));
            }

            _descriptionHeight = _bodyStyle.CalcHeight(new GUIContent(_formattedDescription), _descriptionRect.width - 8f);
            _scrollOffset = Math.Max(0f, Math.Min(_scrollOffset, Math.Max(0f, _descriptionHeight - _descriptionRect.height)));
            GUI.BeginGroup(_descriptionRect);
            GUI.Label(new Rect(0, -_scrollOffset, _descriptionRect.width - 8f, _descriptionHeight + 2f), _formattedDescription, _bodyStyle);
            GUI.EndGroup();
            if (_descriptionHeight > _descriptionRect.height)
            {
                DrawScrollBar();
            }

            if (plants && id != 48)
            {
                var plant = VerifiedDataCatalog.Plants[id];
                var recharge = plant.RechargeThresholdRaw == 750 ? "WAIT_TIME_SHORT" : plant.RechargeThresholdRaw == 3000 ? "WAIT_TIME_LONG" : "WAIT_TIME_VERY_LONG";
                GUI.Label(new Rect(485, 520, 130, 30), Text("COST") + ": " + plant.SunCost, _smallStyle);
                GUI.Label(new Rect(610, 520, 132, 30), Text("WAIT_TIME") + ": " + Text(recharge), _smallStyle);
            }
        }

        /// <summary>Performs scroll bar rect.</summary>
        /// <returns>The resulting scroll bar rect.</returns>
        private Rect ScrollBarRect()
        {
            return new Rect(_descriptionRect.xMax - 8f, _descriptionRect.y, 8f, _descriptionRect.height);
        }

        /// <summary>Performs scroll thumb rect.</summary>
        /// <returns>The resulting scroll thumb rect.</returns>
        private Rect ScrollThumbRect()
        {
            var bar = ScrollBarRect();
            var height = Math.Min(bar.height, Math.Max(12f, bar.height * bar.height / _descriptionHeight));
            var position = _scrollOffset / (_descriptionHeight - _descriptionRect.height);
            return new Rect(bar.x, bar.y + position * (bar.height - height), bar.width, height);
        }

        /// <summary>Performs draw scroll bar.</summary>
        private void DrawScrollBar()
        {
            Fill(ScrollBarRect(), new Color(0.32f, 0.27f, 0.34f, 0.35f));
            Fill(ScrollThumbRect(), new Color(0.42f, 0.36f, 0.43f, 0.9f));
        }

        /// <summary>Performs ground for.</summary>
        /// <param name = "plants">The plants.</param>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting ground for.</returns>
        private static string GroundFor(bool plants, int id)
        {
            if (!plants)
            {
                return id == 12 || id == 13 ? "Almanac_GroundIce.jpg" : "Almanac_GroundDay.jpg";
            }

            if (id == 16 || id == 19 || id == 43)
            {
                return "Almanac_GroundPool.jpg";
            }

            if (id == 24)
            {
                return "Almanac_GroundNightPool.jpg";
            }

            if ((id >= 8 && id <= 15) || id == 25 || id == 31 || id == 42)
            {
                return "Almanac_GroundNight.jpg";
            }

            return id == 33 ? "Almanac_GroundRoof.jpg" : "Almanac_GroundDay.jpg";
        }

        /// <summary>Performs draw native portrait.</summary>
        /// <param name = "plants">The plants.</param>
        /// <param name = "id">The id.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "thumbnail">The thumbnail.</param>
        /// <returns>The resulting draw native portrait.</returns>
        private bool DrawNativePortrait(bool plants, int id, Vector2 origin, float scale, float seconds, bool thumbnail)
        {
            var failureKey = (plants ? "plant:" : "zombie:") + id;
            var drawn = false;
            if (!_failedAnimations.Contains(failureKey))
            {
                ReanimationDefinition animation;
                var animationId = plants ? VerifiedDataCatalog.Plants[id].ReanimationId : VerifiedDataCatalog.Zombies[id].ReanimationId;
                if (VerifiedDataCatalog.TryGetReanimation(animationId, out animation))
                {
                    var name = Path.GetFileNameWithoutExtension(animation.Path.Replace('\\', '/'));
                    if (!plants && id == 25)
                    {
                        drawn = _renderer.DrawAlmanacBoss(origin, scale, seconds, thumbnail);
                    }
                    else if (!plants && (id == 0 || id == 1 || id == 2 || id == 4 || id == 6))
                    {
                        var variant = id == 1 ? ZombieVariant.Flag : id == 2 ? ZombieVariant.Conehead : id == 4 ? ZombieVariant.Buckethead : id == 6 ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
                        drawn = _renderer.DrawZombieNative(origin, scale, seconds, variant, "anim_idle", thumbnail);
                    }
                    else if (!plants && id == 10)
                    {
                        drawn = _renderer.DrawNative(name, origin, scale, seconds, "anim_idle", DuckyTrackVisible);
                    }
                    else
                    {
                        var clip = plants && id == 4 ? "anim_armed" : plants && id == 9 && !thumbnail ? "anim_bigidle" : !plants && id == 18 ? "anim_pogo" : !plants && id == 24 ? "anim_walk" : !plants && id == 9 && !thumbnail ? "anim_armraise" : !plants && id == 3 && !thumbnail ? "anim_walk" : null;
                        var filter = plants && id == 34 ? (Func<string, bool>)KernelTrackVisible : null;
                        drawn = _renderer.DrawNative(name, origin, scale, seconds, clip, filter);
                    }

                    if (!drawn)
                    {
                        var error = _renderer.LastError ?? "No image was drawn.";
                        Report(failureKey + ": " + error);
                        if (!error.StartsWith("No visible image tracks", StringComparison.Ordinal))
                        {
                            _failedAnimations.Add(failureKey);
                        }
                    }
                }
                else
                {
                    Report(failureKey + ": missing reanimation catalog ID " + animationId);
                    _failedAnimations.Add(failureKey);
                }
            }

            return drawn;
        }

        /// <summary>Performs kernel track visible.</summary>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting kernel track visible.</returns>
        private static bool KernelTrackVisible(string track)
        {
            return !string.Equals(track, "Cornpult_butter", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Performs ducky track visible.</summary>
        /// <param name = "track">The track.</param>
        /// <returns>The resulting ducky track visible.</returns>
        private static bool DuckyTrackVisible(string track)
        {
            return string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase) || string.Equals(track, "Zombie_whitewater", StringComparison.OrdinalIgnoreCase) || string.Equals(track, "Zombie_whitewater2", StringComparison.OrdinalIgnoreCase) || ReanimationRenderer.IsZombieTrackVisible(track, ZombieVariant.Normal);
        }

        /// <summary>Performs entry key.</summary>
        /// <param name = "plants">The plants.</param>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting entry key.</returns>
        private static string EntryKey(bool plants, int id)
        {
            return plants ? VerifiedDataCatalog.Plants[id].InternalName : VerifiedDataCatalog.Zombies[id].InternalName;
        }

        /// <summary>Performs ensure styles.</summary>
        private void EnsureStyles()
        {
            if (_bodyStyle != null)
            {
                return;
            }

            _font = Font.CreateDynamicFontFromOSFont(new[] {
                "Microsoft YaHei",
                "SimHei",
                "Noto Sans CJK SC",
                "Arial Unicode MS",
                "Arial"
            }, 16);
            _headingStyle = Style(25, TextAnchor.MiddleCenter, Color.white, true);
            _titleStyle = Style(19, TextAnchor.MiddleCenter, s_darkText, true);
            _labelStyle = Style(15, TextAnchor.MiddleCenter, s_darkText, true);
            _smallStyle = Style(14, TextAnchor.MiddleLeft, s_darkText, false);
            _iconStyle = Style(12, TextAnchor.MiddleRight, Color.black, true);
            _bodyStyle = Style(14, TextAnchor.UpperLeft, s_darkText, false);
            _bodyStyle.wordWrap = true;
            _bodyStyle.richText = true;
            _bodyStyle.clipping = TextClipping.Clip;
        }

        /// <summary>Performs style.</summary>
        /// <param name = "size">The size.</param>
        /// <param name = "anchor">The anchor.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "bold">The bold.</param>
        /// <returns>The resulting style.</returns>
        private GUIStyle Style(int size, TextAnchor anchor, Color color, bool bold)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.font = _font;
            style.fontSize = size;
            style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            style.alignment = anchor;
            style.normal.textColor = color;
            style.padding = new RectOffset(0, 0, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            return style;
        }

        /// <summary>Performs format description.</summary>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting format description.</returns>
        private static string FormatDescription(string value)
        {
            var result = new StringBuilder();
            var colored = false;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '{')
                {
                    var end = value.IndexOf('}', i + 1);
                    if (end > i)
                    {
                        var token = value.Substring(i + 1, end - i - 1);
                        var color = token == "KEYWORD" ? "#873222" : token == "FLAVOR" ? "#624C42" : token == "NOCTURNAL" || token == "AQUATIC" ? "#9A351F" : token == "STAT" ? "#28325A" : null;
                        if (color != null)
                        {
                            if (colored)
                            {
                                result.Append("</color>");
                            }

                            result.Append("<color=").Append(color).Append('>');
                            colored = true;
                        }
                        else if (token != "SHORTLINE" && token != "METAL")
                        {
                            int number;
                            if (int.TryParse(token, out number))
                            {
                                result.Append('(').Append(token).Append(')');
                            }
                            else
                            {
                                result.Append('{').Append(token).Append('}');
                            }
                        }

                        i = end;
                        continue;
                    }
                }

                if (value[i] != '\r')
                {
                    result.Append(value[i]);
                }
            }

            if (colored)
            {
                result.Append("</color>");
            }

            return result.ToString();
        }

        /// <summary>Performs text.</summary>
        /// <param name = "key">The key.</param>
        /// <returns>The resulting text.</returns>
        private string Text(string key)
        {
            string value;
            if (_strings.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            Report("Missing LawnStrings entry: " + key);
            return "[" + key + "]";
        }

        /// <summary>Performs read strings.</summary>
        /// <param name = "path">The path.</param>
        /// <returns>The resulting read strings.</returns>
        private static Dictionary<string, string> ReadStrings(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string key = null;
            var value = new StringBuilder();
            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (IsStringKey(trimmed))
                    {
                        if (key != null)
                        {
                            result[key] = value.ToString().TrimEnd('\r', '\n');
                        }

                        key = trimmed.Substring(1, trimmed.Length - 2);
                        value.Length = 0;
                    }
                    else if (key != null)
                    {
                        if (value.Length > 0)
                        {
                            value.Append('\n');
                        }

                        value.Append(line);
                    }
                }
            }

            if (key != null)
            {
                result[key] = value.ToString().TrimEnd('\r', '\n');
            }

            return result;
        }

        /// <summary>Performs is string key.</summary>
        /// <param name = "text">The text.</param>
        /// <returns>The resulting is string key.</returns>
        private static bool IsStringKey(string text)
        {
            if (text.Length < 3 || text[0] != '[' || text[text.Length - 1] != ']')
            {
                return false;
            }

            for (var i = 1; i < text.Length - 1; i++)
            {
                var c = text[i];
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Performs texture.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting texture.</returns>
        private Texture2D Texture(string name)
        {
            Texture2D texture;
            if (_textures.TryGetValue(name, out texture))
            {
                return texture;
            }

            var path = Path.Combine(_root, "images", name);
            texture = null;
            try
            {
                if (File.Exists(path))
                {
                    var data = File.ReadAllBytes(path);
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!ImageConversion.LoadImage(texture, data, true))
                    {
                        Destroy(texture);
                        texture = null;
                    }
                    else
                    {
                        texture.filterMode = FilterMode.Bilinear;
                        texture.wrapMode = TextureWrapMode.Clamp;
                    }
                }
            }
            catch (Exception error)when (error is IOException || error is UnauthorizedAccessException)
            {
                Destroy(texture);
                texture = null;
                Report("Could not read " + path + ": " + error.Message);
            }

            if (texture == null)
            {
                Report("Missing or invalid image: " + path);
            }

            _textures[name] = texture;
            return texture;
        }

        /// <summary>Performs draw background.</summary>
        /// <param name = "name">The name.</param>
        private void DrawBackground(string name)
        {
            DrawImage(name, s_canvas);
        }

        /// <summary>Performs draw image.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "rect">The rect.</param>
        /// <returns>The resulting draw image.</returns>
        private bool DrawImage(string name, Rect rect)
        {
            var texture = Texture(name);
            if (texture == null)
            {
                return false;
            }

            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            return true;
        }

        /// <summary>Performs draw button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "normal">The normal.</param>
        /// <param name = "highlight">The highlight.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "style">The style.</param>
        /// <param name = "textOffset">The text offset.</param>
        private void DrawButton(Rect rect, string normal, string highlight, string text, GUIStyle style, int textOffset)
        {
            bool hovered = rect.Contains(_pointer), down = hovered && Input.GetMouseButton(0);
            if (normal == "button_middle.png")
            {
                OriginalUiWidgets.StoneButton(Texture, rect, hovered, down);
                if (!OriginalUiWidgets.StoneLabel(rect, text, hovered, down) && !string.IsNullOrEmpty(text))
                {
                    GUI.Label(new Rect(rect.x + textOffset, rect.y, rect.width, rect.height), text, style);
                }
            }
            else
            {
                OriginalUiWidgets.ImageButton(Texture, rect, normal, highlight, null, hovered, down);
                if (!string.IsNullOrEmpty(text))
                {
                    GUI.Label(new Rect(rect.x + textOffset, rect.y, rect.width, rect.height), text, style);
                }
            }
        }

        /// <summary>Performs fill.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "color">The color.</param>
        private static void Fill(Rect rect, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>Performs outline.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "thickness">The thickness.</param>
        private static void Outline(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        /// <summary>Performs report.</summary>
        /// <param name = "message">The message.</param>
        private void Report(string message)
        {
            if (_diagnosticSet.Add(message))
            {
                _diagnostics.Add(message);
            }
        }

        /// <summary>Performs throw if disposed.</summary>
        /// <exception cref = "ObjectDisposedException">The operation cannot satisfy its preconditions.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AlmanacView));
            }
        }

        /// <summary>Performs destroy.</summary>
        /// <param name = "value">The value.</param>
        private static void Destroy(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
        }

        /// <summary>Releases this view's textures and font, but not the supplied renderer.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (Texture2D texture in _textures.Values)
            {
                Destroy(texture);
            }

            _textures.Clear();
            Destroy(_font);
            _disposed = true;
        }
    }
}
