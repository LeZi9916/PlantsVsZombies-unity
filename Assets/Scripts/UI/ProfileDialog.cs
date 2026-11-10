#nullable enable
using PlantsVsZombies.Profiles;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>A modal, 800 by 600 logical-coordinate profile picker.</summary>
    public sealed class ProfileDialog : IDisposable
    {
        /// <summary>Defines page.</summary>
        private enum Page
        {
            /// <summary>Identifies list.</summary>
            List,
            /// <summary>Identifies create.</summary>
            Create,
            /// <summary>Identifies rename.</summary>
            Rename,
            /// <summary>Identifies delete.</summary>
            Delete
        }

        /// <summary>Stores rows per page.</summary>
        private const int RowsPerPage = 8;
        /// <summary>Stores name control.</summary>
        private const string NameControl = "PvZ.ProfileDialog.Name";
        /// <summary>Stores panel.</summary>
        private static readonly Rect s_panel = new Rect(193, 46, 413, 508);
        /// <summary>Stores list.</summary>
        private static readonly Rect s_list = new Rect(283, 208, 223, 200);
        /// <summary>Stores previous.</summary>
        private static readonly Rect s_previous = new Rect(512, 213, 32, 32);
        /// <summary>Stores next.</summary>
        private static readonly Rect s_next = new Rect(512, 366, 32, 32);
        /// <summary>Stores rename button.</summary>
        private static readonly Rect s_renameButton = new Rect(248, 409, 117, 46);
        /// <summary>Stores delete button.</summary>
        private static readonly Rect s_deleteButton = new Rect(422, 409, 117, 46);
        /// <summary>Stores ok button.</summary>
        private static readonly Rect s_okButton = new Rect(248, 455, 117, 46);
        /// <summary>Stores cancel button.</summary>
        private static readonly Rect s_cancelButton = new Rect(422, 455, 117, 46);
        /// <summary>Stores name field.</summary>
        private static readonly Rect s_nameField = new Rect(266, 251, 267, 38);
        /// <summary>Stores profiles.</summary>
        private readonly ProfileCatalog _profiles;
        /// <summary>Stores images.</summary>
        private readonly Dictionary<string, Texture2D> _images = new Dictionary<string, Texture2D>();
        /// <summary>Stores font.</summary>
        private Font _font;
        /// <summary>Stores label style.</summary>
        private GUIStyle _labelStyle;
        /// <summary>Stores field style.</summary>
        private GUIStyle _fieldStyle;
        /// <summary>Stores page.</summary>
        private Page _page;
        /// <summary>Stores page index.</summary>
        private int _pageIndex;
        /// <summary>Stores highlighted id.</summary>
        private Guid? _highlightedId;
        /// <summary>Stores target id.</summary>
        private Guid? _targetId;
        /// <summary>Stores target name.</summary>
        private string _targetName;
        /// <summary>Stores edit name.</summary>
        private string _editName = "";
        /// <summary>Stores error.</summary>
        private string _error;
        /// <summary>Stores request focus.</summary>
        private bool _requestFocus;
        /// <summary>Stores disposed.</summary>
        private bool _disposed;
        /// <summary>Gets or sets is open.</summary>
        public bool IsOpen { get; private set; }

        /// <param name = "resourceRoot">Filesystem directory containing the original images directory.</param>
        public ProfileDialog(string resourceRoot, ProfileCatalog profiles)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
            {
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            }

            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }

            this._profiles = profiles;
            var directory = Path.Combine(Path.GetFullPath(resourceRoot), "images");
            string[] names =
            {
                "dialog_topleft.png",
                "dialog_topmiddle.png",
                "dialog_topright.png",
                "dialog_centerleft.png",
                "dialog_centermiddle.png",
                "dialog_centerright.png",
                "dialog_bigbottomleft.png",
                "dialog_bigbottommiddle.png",
                "dialog_bigbottomright.png",
                "dialog_header.png",
                "button_left.png",
                "button_middle.png",
                "button_right.png",
                "button_down_left.png",
                "button_down_middle.png",
                "button_down_right.png",
                "editbox.png"
            };
            try
            {
                foreach (string name in names)
                {
                    _images.Add(name, LoadTexture(Path.Combine(directory, name)));
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>Starts a new session; an empty catalog opens the new-user editor.</summary>
        public void Open()
        {
            ThrowIfDisposed();
            if (IsOpen)
            {
                throw new InvalidOperationException("The profile dialog is already open.");
            }

            IsOpen = true;
            _page = Page.List;
            _pageIndex = 0;
            _highlightedId = null;
            _targetId = null;
            _targetName = null;
            _editName = "";
            _error = null;
            _requestFocus = false;
            try
            {
                var items = Snapshot();
                var selected = _profiles.SelectedId;
                var current = Find(items, selected);
                if (current != null)
                {
                    _highlightedId = current.Id;
                }
                else if (items.Count > 0)
                {
                    _highlightedId = items[0].Id;
                }

                if (items.Count == 0)
                {
                    StartCreate();
                }
                else
                {
                    ShowHighlightedPage(items);
                }
            }
            catch (Exception exception)
            {
                SetError("无法读取用户目录", exception);
            }
        }

        /// <summary>Submits the retained Canvas elements in the host's logical viewport.</summary>
        public void Draw()
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return;
            }

            EnsureStyles();
            var previousColor = CanvasUI.Color;
            var previousContent = CanvasUI.ContentColor;
            var previousEnabled = CanvasUI.Enabled;
            try
            {
                CanvasUI.Enabled = true;
                CanvasUI.Color = Color.white;
                CanvasUI.ContentColor = Color.white;
                Fill(new Rect(0, 0, 800, 600), new Color(0, 0, 0, 0.55f));
                DrawPanel();
                Label(new Rect(235, 105, 330, 33), Heading(), 21, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.49f), false);
                if (_page == Page.List)
                {
                    DrawList();
                }
                else if (_page == Page.Delete)
                {
                    DrawDelete();
                }
                else
                {
                    DrawEditor();
                }

                if (!string.IsNullOrEmpty(_error))
                {
                    Label(new Rect(249, 147, 302, 58), _error, 14, TextAnchor.MiddleCenter, new Color(1f, 0.70f, 0.65f), true);
                }
            }
            finally
            {
                CanvasUI.Color = previousColor;
                CanvasUI.ContentColor = previousContent;
                CanvasUI.Enabled = previousEnabled;
            }
        }

        /// <summary>
        /// Handles a primary MouseDown in logical coordinates. Consume the event when true;
        /// false while open means the IMGUI name field must receive the same MouseDown.
        /// The host must still suppress input to the underlying screen.
        /// </summary>
        public bool HandleClick(Vector2 point)
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return false;
            }

            if ((_page == Page.Create || _page == Page.Rename) && s_nameField.Contains(point))
            {
                return false;
            }

            if (s_cancelButton.Contains(point))
            {
                HandleBack();
                return true;
            }

            if (_page == Page.List)
            {
                HandleListClick(point);
                return true;
            }

            if (s_okButton.Contains(point))
            {
                if (_page == Page.Create)
                {
                    Create();
                }
                else if (_page == Page.Rename)
                {
                    Rename();
                }
                else
                {
                    Delete();
                }
            }

            return true;
        }

        /// <summary>Cancel a subdialog first; close only with a readable, valid selected profile.</summary>
        public bool HandleBack()
        {
            ThrowIfDisposed();
            if (!IsOpen)
            {
                return false;
            }

            if (_page != Page.List)
            {
                _page = Page.List;
                _requestFocus = false;
                _error = null;
                try
                {
                    var items = Snapshot();
                    if (items.Count == 0)
                    {
                        _error = "请先创建一个用户。";
                    }
                    else
                    {
                        ShowHighlightedPage(items);
                    }
                }
                catch (Exception exception)
                {
                    SetError("无法读取用户目录", exception);
                }

                return true;
            }

            if (!string.IsNullOrEmpty(_error))
            {
                _error = null;
                return true;
            }

            try
            {
                var items = Snapshot();
                if (Find(items, _profiles.SelectedId) == null)
                {
                    _error = items.Count == 0 ? "请先创建一个用户。" : "请先选择一个用户。";
                }
                else
                {
                    IsOpen = false;
                }
            }
            catch (Exception exception)
            {
                SetError("无法读取用户目录", exception);
            }

            return true;
        }

        /// <summary>Performs dispose.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            IsOpen = false;
            foreach (Texture2D image in _images.Values)
            {
                Release(image);
            }

            _images.Clear();
            Release(_font);
            _font = null;
        }

        /// <summary>Performs handle list click.</summary>
        /// <param name = "point">The point.</param>
        private void HandleListClick(Vector2 point)
        {
            try
            {
                var items = Snapshot();
                var pageCount = PageCount(items.Count);
                _pageIndex = Math.Max(0, Math.Min(_pageIndex, pageCount - 1));
                if (pageCount > 1 && s_previous.Contains(point))
                {
                    _pageIndex = Math.Max(0, _pageIndex - 1);
                    _error = null;
                }
                else if (pageCount > 1 && s_next.Contains(point))
                {
                    _pageIndex = Math.Min(pageCount - 1, _pageIndex + 1);
                    _error = null;
                }
                else if (s_list.Contains(point))
                {
                    var row = (int)((point.y - s_list.y) / 24f);
                    var index = _pageIndex * RowsPerPage + row;
                    if (row >= RowsPerPage)
                    {
                        return;
                    }

                    if (index == items.Count)
                    {
                        StartCreate();
                    }
                    else if (index >= 0 && index < items.Count)
                    {
                        _highlightedId = items[index].Id;
                        _error = null;
                    }
                }
                else if (s_renameButton.Contains(point))
                {
                    StartEdit(items, Page.Rename);
                }
                else if (s_deleteButton.Contains(point))
                {
                    StartEdit(items, Page.Delete);
                }
                else if (s_okButton.Contains(point))
                {
                    Select(items);
                }
            }
            catch (Exception exception)
            {
                SetError("无法读取用户目录", exception);
            }
        }

        /// <summary>Performs start create.</summary>
        private void StartCreate()
        {
            _page = Page.Create;
            _targetId = null;
            _targetName = null;
            _editName = "";
            _error = null;
            _requestFocus = true;
        }

        /// <summary>Performs start edit.</summary>
        /// <param name = "items">The items.</param>
        /// <param name = "next">The next.</param>
        private void StartEdit(List<UserProfile> items, Page next)
        {
            var item = Find(items, _highlightedId);
            if (item == null)
            {
                _error = "请先选择一个用户。";
                return;
            }

            _targetId = item.Id;
            _targetName = item.Name;
            _editName = item.Name;
            _error = null;
            _page = next;
            _requestFocus = next == Page.Rename;
        }

        /// <summary>Performs create.</summary>
        private void Create()
        {
            var name = _editName.Trim();
            if (name.Length == 0)
            {
                _error = "请输入你的用户名。";
                return;
            }

            List<UserProfile> before = null;
            try
            {
                before = Snapshot();
                _profiles.Create(name);
                var after = Snapshot();
                var created = FindCreated(before, after);
                if (created == null || !string.Equals(created.Name, name, StringComparison.Ordinal))
                {
                    _page = Page.List;
                    _error = "无法确认新用户是否创建成功，请检查列表。";
                    return;
                }

                _highlightedId = created.Id;
                _page = Page.List;
                ShowHighlightedPage(after);
                Select(after);
            }
            catch (Exception exception)
            {
                // A catalog may report an uncertain write after the file changed.
                if (before != null)
                {
                    try
                    {
                        var after = Snapshot();
                        var created = FindCreated(before, after);
                        if (created != null)
                        {
                            _page = Page.List;
                            _highlightedId = created.Id;
                            ShowHighlightedPage(after);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                SetError("创建用户失败，请检查列表后重试", exception);
            }
        }

        /// <summary>Performs select.</summary>
        /// <param name = "items">The items.</param>
        private void Select(List<UserProfile> items)
        {
            var item = Find(items, _highlightedId);
            if (item == null)
            {
                _error = "请先选择一个用户。";
                return;
            }

            try
            {
                _profiles.Select(item.Id);
                var after = Snapshot();
                if (Find(after, item.Id) == null || _profiles.SelectedId != item.Id)
                {
                    _error = "未能确认当前用户，请重新选择。";
                    return;
                }

                _error = null;
                IsOpen = false;
            }
            catch (Exception exception)
            {
                SetError("选择用户失败", exception);
            }
        }

        /// <summary>Performs rename.</summary>
        private void Rename()
        {
            var name = _editName.Trim();
            if (name.Length == 0)
            {
                _error = "请输入你的用户名。";
                return;
            }

            try
            {
                if (!_targetId.HasValue || Find(Snapshot(), _targetId) == null)
                {
                    _error = "用户已不存在，请取消并重新选择。";
                    return;
                }

                _profiles.Rename(_targetId.Value, name);
                var after = Snapshot();
                var renamed = Find(after, _targetId);
                if (renamed == null || !string.Equals(renamed.Name, name, StringComparison.Ordinal))
                {
                    _error = "无法确认重命名结果，请检查用户目录。";
                    return;
                }

                _highlightedId = _targetId;
                _page = Page.List;
                _error = null;
                ShowHighlightedPage(after);
            }
            catch (Exception exception)
            {
                SetError("重命名失败", exception);
            }
        }

        /// <summary>Performs delete.</summary>
        private void Delete()
        {
            try
            {
                var item = Find(Snapshot(), _targetId);
                if (!_targetId.HasValue || item == null || !string.Equals(item.Name, _targetName, StringComparison.Ordinal))
                {
                    _error = "用户已发生变化，请取消后重新确认。";
                    return;
                }

                _profiles.Delete(_targetId.Value);
                var after = Snapshot();
                if (Find(after, _targetId) != null)
                {
                    _error = "未能确认删除结果，请检查用户目录。";
                    return;
                }

                var selected = Find(after, _profiles.SelectedId);
                _highlightedId = selected != null ? selected.Id : (after.Count > 0 ? (Guid? )after[0].Id : null);
                _page = Page.List;
                _error = after.Count == 0 ? "请先创建一个用户。" : null;
                ShowHighlightedPage(after);
            }
            catch (Exception exception)
            {
                SetError("删除用户失败", exception);
            }
        }

        /// <summary>Performs snapshot.</summary>
        /// <returns>The resulting snapshot.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private List<UserProfile> Snapshot()
        {
            var source = _profiles.Profiles;
            if (source == null)
            {
                throw new InvalidDataException("用户目录为空引用。");
            }

            var result = new List<UserProfile>(source.Count);
            var ids = new HashSet<Guid>();
            foreach (UserProfile item in source)
            {
                if (item == null || item.Id == Guid.Empty || item.Name == null || !ids.Add(item.Id))
                {
                    throw new InvalidDataException("用户目录包含无效或重复的标识。");
                }

                result.Add(item);
            }

            return result;
        }

        /// <summary>Performs find.</summary>
        /// <param name = "items">The items.</param>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting find.</returns>
        private static UserProfile Find(List<UserProfile> items, Guid? id)
        {
            if (!id.HasValue || id.Value == Guid.Empty)
            {
                return null;
            }

            foreach (UserProfile item in items)
            {
                if (item.Id == id.Value)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>Performs find created.</summary>
        /// <param name = "before">The before.</param>
        /// <param name = "after">The after.</param>
        /// <returns>The resulting find created.</returns>
        private static UserProfile FindCreated(List<UserProfile> before, List<UserProfile> after)
        {
            UserProfile created = null;
            foreach (UserProfile item in after)
            {
                if (Find(before, item.Id) != null)
                {
                    continue;
                }

                if (created != null)
                {
                    return null;
                }

                created = item;
            }

            return created;
        }

        /// <summary>Performs page count.</summary>
        /// <param name = "count">The count.</param>
        /// <returns>The resulting page count.</returns>
        private static int PageCount(int count)
        {
            return count / RowsPerPage + 1;
        }

        /// <summary>Performs show highlighted page.</summary>
        /// <param name = "items">The items.</param>
        private void ShowHighlightedPage(List<UserProfile> items)
        {
            var index = items.FindIndex(item => item.Id == _highlightedId);
            _pageIndex = index < 0 ? Math.Min(_pageIndex, PageCount(items.Count) - 1) : index / RowsPerPage;
        }

        /// <summary>Performs set error.</summary>
        /// <param name = "description">The description.</param>
        /// <param name = "exception">The exception.</param>
        private void SetError(string description, Exception exception)
        {
            _error = description + "：" + exception.Message;
        }

        /// <summary>Performs heading.</summary>
        /// <returns>The resulting heading.</returns>
        private string Heading()
        {
            if (_page == Page.Create)
            {
                return "新用户";
            }

            if (_page == Page.Rename)
            {
                return "重设用户名";
            }

            if (_page == Page.Delete)
            {
                return "你确定吗？";
            }

            return "你是谁？";
        }

        /// <summary>Performs draw list.</summary>
        private void DrawList()
        {
            List<UserProfile> items;
            try
            {
                items = Snapshot();
            }
            catch (Exception exception)
            {
                SetError("无法读取用户目录", exception);
                items = new List<UserProfile>();
            }

            var pageCount = PageCount(items.Count);
            _pageIndex = Math.Max(0, Math.Min(_pageIndex, pageCount - 1));
            for (var row = 0; row < RowsPerPage; row++)
            {
                var index = _pageIndex * RowsPerPage + row;
                if (index > items.Count)
                {
                    break;
                }

                var rowRect = new Rect(s_list.x, s_list.y + row * 24, s_list.width, 24);
                if (index == items.Count)
                {
                    Label(rowRect, "（创建一个新的玩家）", 14, TextAnchor.MiddleLeft, new Color(0.85f, 0.81f, 0.72f), false);
                }
                else
                {
                    var selected = items[index].Id == _highlightedId;
                    if (selected)
                    {
                        Fill(rowRect, new Color(20f / 255f, 180f / 255f, 15f / 255f, 1f));
                    }

                    Label(new Rect(rowRect.x + 5, rowRect.y, rowRect.width - 10, rowRect.height), items[index].Name, 16, TextAnchor.MiddleLeft, Color.white, false);
                }
            }

            if (pageCount > 1)
            {
                Label(s_previous, "▲", 19, TextAnchor.MiddleCenter, Color.white, false);
                Label(new Rect(509, 291, 50, 22), (_pageIndex + 1) + "/" + pageCount, 12, TextAnchor.MiddleCenter, Color.white, false);
                Label(s_next, "▼", 19, TextAnchor.MiddleCenter, Color.white, false);
            }

            var available = Find(items, _highlightedId) != null;
            Button(s_renameButton, "重命名", available);
            Button(s_deleteButton, "删除", available);
            Button(s_okButton, "确定", available);
            Button(s_cancelButton, "取消", true);
        }

        /// <summary>Performs draw editor.</summary>
        private void DrawEditor()
        {
            Label(new Rect(252, 212, 296, 28), "请输入你的名字：", 17, TextAnchor.MiddleLeft, Color.white, false);
            CanvasUI.SetNextControlName(NameControl);
            OriginalUiWidgets.EditBox(UiTexture, s_nameField);
            _editName = CanvasUI.TextField(s_nameField, _editName, _fieldStyle);
            if (_requestFocus)
            {
                CanvasUI.FocusControl(NameControl);
                _requestFocus = false;
            }

            Button(s_okButton, "确定", true);
            Button(s_cancelButton, "取消", true);
        }

        /// <summary>Performs draw delete.</summary>
        private void DrawDelete()
        {
            Label(new Rect(257, 222, 286, 110), "这样将从用户名录中永久删除\n'" + _targetName + "'。", 17, TextAnchor.MiddleCenter, Color.white, true);
            Button(s_okButton, "是", true);
            Button(s_cancelButton, "否", true);
        }

        /// <summary>Performs ui texture.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting ui texture.</returns>
        private Texture2D UiTexture(string name)
        {
            Texture2D result;
            return _images.TryGetValue(name, out result) ? result : null;
        }

        /// <summary>Performs draw panel.</summary>
        private void DrawPanel()
        {
            OriginalUiWidgets.DialogFrame(UiTexture, s_panel, true, true);
        }

        /// <summary>Performs button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "enabled">The enabled.</param>
        private void Button(Rect rect, string text, bool enabled)
        {
            bool hover = enabled && OriginalUiWidgets.Hovered(rect), down = hover && OriginalUiWidgets.Pressed(rect);
            OriginalUiWidgets.StoneButton(UiTexture, rect, hover, down, enabled);
            if (!OriginalUiWidgets.StoneLabel(rect, text, hover, down, enabled))
            {
                Label(rect, text, 16, TextAnchor.MiddleCenter, enabled ? OriginalUiWidgets.StoneText : Color.gray, false);
            }
        }

        /// <summary>Performs image.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "rect">The rect.</param>
        private void Image(string name, Rect rect)
        {
            CanvasUI.DrawTexture(rect, _images[name], ScaleMode.StretchToFill, true);
        }

        /// <summary>Performs fill.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "color">The color.</param>
        private static void Fill(Rect rect, Color color)
        {
            var before = CanvasUI.Color;
            CanvasUI.Color = color;
            CanvasUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            CanvasUI.Color = before;
        }

        /// <summary>Performs label.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "size">The size.</param>
        /// <param name = "alignment">The alignment.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "wrap">The wrap.</param>
        private void Label(Rect rect, string text, int size, TextAnchor alignment, Color color, bool wrap)
        {
            _labelStyle.fontSize = size;
            _labelStyle.alignment = alignment;
            _labelStyle.wordWrap = wrap;
            _labelStyle.normal.textColor = color;
            CanvasUI.Label(rect, text, _labelStyle);
        }

        /// <summary>Performs ensure styles.</summary>
        private void EnsureStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            _font = Font.CreateDynamicFontFromOSFont(new[] {
                "Microsoft YaHei",
                "SimHei",
                "Noto Sans CJK SC",
                "Arial Unicode MS"
            }, 18);
            _labelStyle = new GUIStyle();
            _fieldStyle = OriginalUiWidgets.TransparentTextField(new GUIStyle());
            if (_font != null)
            {
                _labelStyle.font = _font;
                _fieldStyle.font = _font;
            }

            _labelStyle.richText = false;
            _labelStyle.clipping = TextClipping.Clip;
            _labelStyle.padding = new RectOffset(0, 0, 0, 0);
            _fieldStyle.fontSize = 18;
            _fieldStyle.richText = false;
            _fieldStyle.alignment = TextAnchor.MiddleLeft;
            _fieldStyle.padding = new RectOffset(8, 8, 7, 7);
        }

        /// <summary>Performs load texture.</summary>
        /// <param name = "path">The path.</param>
        /// <returns>The resulting load texture.</returns>
        /// <exception cref = "InvalidDataException">The operation cannot satisfy its preconditions.</exception>
        private static Texture2D LoadTexture(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(image, bytes, false))
                {
                    throw new InvalidDataException("Cannot decode image: " + path);
                }

                image.hideFlags = HideFlags.HideAndDontSave;
                image.filterMode = FilterMode.Bilinear;
                image.wrapMode = TextureWrapMode.Clamp;
                return image;
            }
            catch
            {
                Release(image);
                throw;
            }
        }

        /// <summary>Performs release.</summary>
        /// <param name = "resource">The resource.</param>
        private static void Release(UnityEngine.Object resource)
        {
            if (resource == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(resource);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(resource);
            }
        }

        /// <summary>Performs throw if disposed.</summary>
        /// <exception cref = "ObjectDisposedException">The operation cannot satisfy its preconditions.</exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ProfileDialog));
            }
        }
    }
}
