using System;
using System.Collections.Generic;
using System.IO;
using PvZReplica.Profiles;
using UnityEngine;

namespace PvZReplica.UI
{
    /// <summary>A modal, 800 by 600 logical-coordinate profile picker.</summary>
    public sealed class ProfileDialog : IDisposable
    {
        private enum Page { List, Create, Rename, Delete }

        private const int RowsPerPage = 8;
        private const string NameControl = "PvZ.ProfileDialog.Name";
        private static readonly Rect Panel = new Rect(193, 46, 413, 508);
        private static readonly Rect List = new Rect(283, 208, 223, 200);
        private static readonly Rect Previous = new Rect(512, 213, 32, 32);
        private static readonly Rect Next = new Rect(512, 366, 32, 32);
        private static readonly Rect RenameButton = new Rect(248, 409, 117, 46);
        private static readonly Rect DeleteButton = new Rect(422, 409, 117, 46);
        private static readonly Rect OkButton = new Rect(248, 455, 117, 46);
        private static readonly Rect CancelButton = new Rect(422, 455, 117, 46);
        private static readonly Rect NameField = new Rect(266, 251, 267, 38);

        private readonly ProfileCatalog profiles;
        private readonly Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
        private Font font;
        private GUIStyle labelStyle;
        private GUIStyle fieldStyle;
        private Page page;
        private int pageIndex;
        private Guid? highlightedId;
        private Guid? targetId;
        private string targetName;
        private string editName = "";
        private string error;
        private bool requestFocus;
        private bool disposed;

        public bool IsOpen { get; private set; }

        /// <param name="resourceRoot">Filesystem directory containing the original images directory.</param>
        public ProfileDialog(string resourceRoot, ProfileCatalog profiles)
        {
            if (string.IsNullOrWhiteSpace(resourceRoot))
                throw new ArgumentException("A resource root is required.", nameof(resourceRoot));
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));
            this.profiles = profiles;

            string directory = Path.Combine(Path.GetFullPath(resourceRoot), "images");
            string[] names = {
                "dialog_topleft.png", "dialog_topmiddle.png", "dialog_topright.png",
                "dialog_centerleft.png", "dialog_centermiddle.png", "dialog_centerright.png",
                "dialog_bigbottomleft.png", "dialog_bigbottommiddle.png", "dialog_bigbottomright.png",
                "dialog_header.png", "button_left.png", "button_middle.png", "button_right.png",
                "button_down_left.png", "button_down_middle.png", "button_down_right.png", "editbox.png"
            };
            try
            {
                foreach (string name in names) images.Add(name, LoadTexture(Path.Combine(directory, name)));
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
            if (IsOpen) throw new InvalidOperationException("The profile dialog is already open.");
            IsOpen = true;
            page = Page.List;
            pageIndex = 0;
            highlightedId = null;
            targetId = null;
            targetName = null;
            editName = "";
            error = null;
            requestFocus = false;
            try
            {
                List<UserProfile> items = Snapshot();
                Guid? selected = profiles.SelectedId;
                UserProfile current = Find(items, selected);
                if (current != null) highlightedId = current.Id;
                else if (items.Count > 0) highlightedId = items[0].Id;
                if (items.Count == 0) StartCreate();
                else ShowHighlightedPage(items);
            }
            catch (Exception exception) { SetError("无法读取用户目录", exception); }
        }

        /// <summary>Call from OnGUI with the host's 800 by 600 GUI matrix already set.</summary>
        public void Draw()
        {
            ThrowIfDisposed();
            if (!IsOpen) return;
            EnsureStyles();
            Color previousColor = GUI.color;
            Color previousContent = GUI.contentColor;
            bool previousEnabled = GUI.enabled;
            try
            {
                GUI.enabled = true;
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                Fill(new Rect(0, 0, 800, 600), new Color(0, 0, 0, 0.55f));
                DrawPanel();
                Label(new Rect(235, 105, 330, 33), Heading(), 21, TextAnchor.MiddleCenter,
                    new Color(1f, 0.86f, 0.49f), false);

                if (page == Page.List) DrawList();
                else if (page == Page.Delete) DrawDelete();
                else DrawEditor();

                if (!string.IsNullOrEmpty(error))
                    Label(new Rect(249, 147, 302, 58), error, 14, TextAnchor.MiddleCenter,
                        new Color(1f, 0.70f, 0.65f), true);
            }
            finally
            {
                GUI.color = previousColor;
                GUI.contentColor = previousContent;
                GUI.enabled = previousEnabled;
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
            if (!IsOpen) return false;
            if ((page == Page.Create || page == Page.Rename) && NameField.Contains(point)) return false;
            if (CancelButton.Contains(point)) { HandleBack(); return true; }
            if (page == Page.List)
            {
                HandleListClick(point);
                return true;
            }
            if (OkButton.Contains(point))
            {
                if (page == Page.Create) Create();
                else if (page == Page.Rename) Rename();
                else Delete();
            }
            return true;
        }

        /// <summary>Cancel a subdialog first; close only with a readable, valid selected profile.</summary>
        public bool HandleBack()
        {
            ThrowIfDisposed();
            if (!IsOpen) return false;
            if (page != Page.List)
            {
                page = Page.List;
                requestFocus = false;
                error = null;
                try
                {
                    List<UserProfile> items = Snapshot();
                    if (items.Count == 0) error = "请先创建一个用户。";
                    else ShowHighlightedPage(items);
                }
                catch (Exception exception) { SetError("无法读取用户目录", exception); }
                return true;
            }
            if (!string.IsNullOrEmpty(error))
            {
                error = null;
                return true;
            }
            try
            {
                List<UserProfile> items = Snapshot();
                if (Find(items, profiles.SelectedId) == null)
                    error = items.Count == 0 ? "请先创建一个用户。" : "请先选择一个用户。";
                else IsOpen = false;
            }
            catch (Exception exception) { SetError("无法读取用户目录", exception); }
            return true;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            IsOpen = false;
            foreach (Texture2D image in images.Values) Release(image);
            images.Clear();
            Release(font);
            font = null;
        }

        private void HandleListClick(Vector2 point)
        {
            try
            {
                List<UserProfile> items = Snapshot();
                int pageCount = PageCount(items.Count);
                pageIndex = Math.Max(0, Math.Min(pageIndex, pageCount - 1));
                if (pageCount > 1 && Previous.Contains(point))
                {
                    pageIndex = Math.Max(0, pageIndex - 1);
                    error = null;
                }
                else if (pageCount > 1 && Next.Contains(point))
                {
                    pageIndex = Math.Min(pageCount - 1, pageIndex + 1);
                    error = null;
                }
                else if (List.Contains(point))
                {
                    int row = (int)((point.y - List.y) / 24f);
                    int index = pageIndex * RowsPerPage + row;
                    if (row >= RowsPerPage) return;
                    if (index == items.Count) StartCreate();
                    else if (index >= 0 && index < items.Count)
                    {
                        highlightedId = items[index].Id;
                        error = null;
                    }
                }
                else if (RenameButton.Contains(point)) StartEdit(items, Page.Rename);
                else if (DeleteButton.Contains(point)) StartEdit(items, Page.Delete);
                else if (OkButton.Contains(point)) Select(items);
            }
            catch (Exception exception) { SetError("无法读取用户目录", exception); }
        }

        private void StartCreate()
        {
            page = Page.Create;
            targetId = null;
            targetName = null;
            editName = "";
            error = null;
            requestFocus = true;
        }

        private void StartEdit(List<UserProfile> items, Page next)
        {
            UserProfile item = Find(items, highlightedId);
            if (item == null) { error = "请先选择一个用户。"; return; }
            targetId = item.Id;
            targetName = item.Name;
            editName = item.Name;
            error = null;
            page = next;
            requestFocus = next == Page.Rename;
        }

        private void Create()
        {
            string name = editName.Trim();
            if (name.Length == 0) { error = "请输入你的用户名。"; return; }
            List<UserProfile> before = null;
            try
            {
                before = Snapshot();
                profiles.Create(name);
                List<UserProfile> after = Snapshot();
                UserProfile created = FindCreated(before, after);
                if (created == null || !string.Equals(created.Name, name, StringComparison.Ordinal))
                {
                    page = Page.List;
                    error = "无法确认新用户是否创建成功，请检查列表。";
                    return;
                }
                highlightedId = created.Id;
                page = Page.List;
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
                        List<UserProfile> after = Snapshot();
                        UserProfile created = FindCreated(before, after);
                        if (created != null)
                        {
                            page = Page.List;
                            highlightedId = created.Id;
                            ShowHighlightedPage(after);
                        }
                    }
                    catch (Exception) { }
                }
                SetError("创建用户失败，请检查列表后重试", exception);
            }
        }

        private void Select(List<UserProfile> items)
        {
            UserProfile item = Find(items, highlightedId);
            if (item == null) { error = "请先选择一个用户。"; return; }
            try
            {
                profiles.Select(item.Id);
                List<UserProfile> after = Snapshot();
                if (Find(after, item.Id) == null || profiles.SelectedId != item.Id)
                {
                    error = "未能确认当前用户，请重新选择。";
                    return;
                }
                error = null;
                IsOpen = false;
            }
            catch (Exception exception) { SetError("选择用户失败", exception); }
        }

        private void Rename()
        {
            string name = editName.Trim();
            if (name.Length == 0) { error = "请输入你的用户名。"; return; }
            try
            {
                if (!targetId.HasValue || Find(Snapshot(), targetId) == null) { error = "用户已不存在，请取消并重新选择。"; return; }
                profiles.Rename(targetId.Value, name);
                List<UserProfile> after = Snapshot();
                UserProfile renamed = Find(after, targetId);
                if (renamed == null || !string.Equals(renamed.Name, name, StringComparison.Ordinal))
                {
                    error = "无法确认重命名结果，请检查用户目录。";
                    return;
                }
                highlightedId = targetId;
                page = Page.List;
                error = null;
                ShowHighlightedPage(after);
            }
            catch (Exception exception) { SetError("重命名失败", exception); }
        }

        private void Delete()
        {
            try
            {
                UserProfile item = Find(Snapshot(), targetId);
                if (!targetId.HasValue || item == null || !string.Equals(item.Name, targetName, StringComparison.Ordinal))
                {
                    error = "用户已发生变化，请取消后重新确认。";
                    return;
                }
                profiles.Delete(targetId.Value);
                List<UserProfile> after = Snapshot();
                if (Find(after, targetId) != null)
                {
                    error = "未能确认删除结果，请检查用户目录。";
                    return;
                }
                UserProfile selected = Find(after, profiles.SelectedId);
                highlightedId = selected != null ? selected.Id : (after.Count > 0 ? (Guid?)after[0].Id : null);
                page = Page.List;
                error = after.Count == 0 ? "请先创建一个用户。" : null;
                ShowHighlightedPage(after);
            }
            catch (Exception exception) { SetError("删除用户失败", exception); }
        }

        private List<UserProfile> Snapshot()
        {
            IReadOnlyList<UserProfile> source = profiles.Profiles;
            if (source == null) throw new InvalidDataException("用户目录为空引用。");
            List<UserProfile> result = new List<UserProfile>(source.Count);
            HashSet<Guid> ids = new HashSet<Guid>();
            foreach (UserProfile item in source)
            {
                if (item == null || item.Id == Guid.Empty || item.Name == null || !ids.Add(item.Id))
                    throw new InvalidDataException("用户目录包含无效或重复的标识。");
                result.Add(item);
            }
            return result;
        }

        private static UserProfile Find(List<UserProfile> items, Guid? id)
        {
            if (!id.HasValue || id.Value == Guid.Empty) return null;
            foreach (UserProfile item in items)
                if (item.Id == id.Value) return item;
            return null;
        }

        private static UserProfile FindCreated(List<UserProfile> before, List<UserProfile> after)
        {
            UserProfile created = null;
            foreach (UserProfile item in after)
            {
                if (Find(before, item.Id) != null) continue;
                if (created != null) return null;
                created = item;
            }
            return created;
        }

        private static int PageCount(int count) { return count / RowsPerPage + 1; }

        private void ShowHighlightedPage(List<UserProfile> items)
        {
            int index = items.FindIndex(item => item.Id == highlightedId);
            pageIndex = index < 0 ? Math.Min(pageIndex, PageCount(items.Count) - 1) : index / RowsPerPage;
        }

        private void SetError(string description, Exception exception)
        {
            error = description + "：" + exception.Message;
        }

        private string Heading()
        {
            if (page == Page.Create) return "新用户";
            if (page == Page.Rename) return "重设用户名";
            if (page == Page.Delete) return "你确定吗？";
            return "你是谁？";
        }

        private void DrawList()
        {
            List<UserProfile> items;
            try { items = Snapshot(); }
            catch (Exception exception)
            {
                SetError("无法读取用户目录", exception);
                items = new List<UserProfile>();
            }
            int pageCount = PageCount(items.Count);
            pageIndex = Math.Max(0, Math.Min(pageIndex, pageCount - 1));
            for (int row = 0; row < RowsPerPage; row++)
            {
                int index = pageIndex * RowsPerPage + row;
                if (index > items.Count) break;
                Rect rowRect = new Rect(List.x, List.y + row * 24, List.width, 24);
                if (index == items.Count)
                    Label(rowRect, "（创建一个新的玩家）", 14, TextAnchor.MiddleLeft,
                        new Color(0.85f, 0.81f, 0.72f), false);
                else
                {
                    bool selected = items[index].Id == highlightedId;
                    if (selected) Fill(rowRect, new Color(20f/255f,180f/255f,15f/255f,1f));
                    Label(new Rect(rowRect.x + 5, rowRect.y, rowRect.width - 10, rowRect.height),
                        items[index].Name, 16, TextAnchor.MiddleLeft, Color.white, false);
                }
            }
            if (pageCount > 1)
            {
                Label(Previous, "▲", 19, TextAnchor.MiddleCenter, Color.white, false);
                Label(new Rect(509, 291, 50, 22), (pageIndex + 1) + "/" + pageCount,
                    12, TextAnchor.MiddleCenter, Color.white, false);
                Label(Next, "▼", 19, TextAnchor.MiddleCenter, Color.white, false);
            }
            bool available = Find(items, highlightedId) != null;
            Button(RenameButton, "重命名", available);
            Button(DeleteButton, "删除", available);
            Button(OkButton, "确定", available);
            Button(CancelButton, "取消", true);
        }

        private void DrawEditor()
        {
            Label(new Rect(252, 212, 296, 28), "请输入你的名字：", 17,
                TextAnchor.MiddleLeft, Color.white, false);
            GUI.SetNextControlName(NameControl);
            OriginalUiWidgets.EditBox(UiTexture,NameField);
            editName = GUI.TextField(NameField, editName, fieldStyle);
            if (requestFocus && Event.current != null && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl(NameControl);
                requestFocus = false;
            }
            Button(OkButton, "确定", true);
            Button(CancelButton, "取消", true);
        }

        private void DrawDelete()
        {
            Label(new Rect(257, 222, 286, 110), "这样将从用户名录中永久删除\n'" + targetName + "'。",
                17, TextAnchor.MiddleCenter, Color.white, true);
            Button(OkButton, "是", true);
            Button(CancelButton, "否", true);
        }

        private Texture2D UiTexture(string name)
        { Texture2D result; return images.TryGetValue(name,out result) ? result : null; }
        private void DrawPanel()
        { OriginalUiWidgets.DialogFrame(UiTexture,Panel,true,true); }
        private void Button(Rect rect, string text, bool enabled)
        {
            bool hover=enabled&&OriginalUiWidgets.Hovered(rect),down=hover&&OriginalUiWidgets.Pressed(rect);
            OriginalUiWidgets.StoneButton(UiTexture,rect,hover,down,enabled);
            if(!OriginalUiWidgets.StoneLabel(rect,text,hover,down,enabled))
                Label(rect,text,16,TextAnchor.MiddleCenter,enabled?OriginalUiWidgets.StoneText:Color.gray,false);
        }

        private void Image(string name, Rect rect)
        {
            GUI.DrawTexture(rect, images[name], ScaleMode.StretchToFill, true);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = before;
        }

        private void Label(Rect rect, string text, int size, TextAnchor alignment, Color color, bool wrap)
        {
            labelStyle.fontSize = size;
            labelStyle.alignment = alignment;
            labelStyle.wordWrap = wrap;
            labelStyle.normal.textColor = color;
            GUI.Label(rect, text, labelStyle);
        }

        private void EnsureStyles()
        {
            if (labelStyle != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] {
                "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Arial Unicode MS"
            }, 18);
            labelStyle = new GUIStyle(GUI.skin.label);
            fieldStyle = OriginalUiWidgets.TransparentTextField(new GUIStyle(GUI.skin.textField));
            if (font != null) { labelStyle.font = font; fieldStyle.font = font; }
            labelStyle.richText = false;
            labelStyle.clipping = TextClipping.Clip;
            labelStyle.padding = new RectOffset(0, 0, 0, 0);
            fieldStyle.fontSize = 18;
            fieldStyle.richText = false;
            fieldStyle.alignment = TextAnchor.MiddleLeft;
            fieldStyle.padding = new RectOffset(8, 8, 7, 7);
        }

        private static Texture2D LoadTexture(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(image, bytes, false))
                    throw new InvalidDataException("Cannot decode image: " + path);
                image.hideFlags = HideFlags.HideAndDontSave;
                image.filterMode = FilterMode.Bilinear;
                image.wrapMode = TextureWrapMode.Clamp;
                return image;
            }
            catch { Release(image); throw; }
        }

        private static void Release(UnityEngine.Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(resource);
            else UnityEngine.Object.DestroyImmediate(resource);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(ProfileDialog));
        }
    }
}
