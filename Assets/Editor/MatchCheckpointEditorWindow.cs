#nullable enable
using System;
using System.IO;
using PlantsVsZombies.Modes;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Edits existing offline board checkpoints using their native replica persistence backend.</summary>
    internal sealed class MatchCheckpointEditorWindow : EditorWindow
    {
        /// <summary>Groups the authoritative state into useful editing categories.</summary>
        private static readonly string[] s_tabs = { "对局", "植物", "僵尸", "种子冷却", "投射物与拾取物", "场景与模式", "全部字段" };
        /// <summary>The directory entered in the native window.</summary>
        [SerializeField]
        private string _directory = string.Empty;
        /// <summary>The selected field category.</summary>
        [SerializeField]
        private int _tab;
        /// <summary>The full detached draft preserved through script reloads.</summary>
        [SerializeField]
        private MatchCheckpointEditorDraft? _draft;
        /// <summary>Whether current disk baselines must be reloaded before saving.</summary>
        [SerializeField]
        private bool _requiresReload;
        /// <summary>The detached loaded board session.</summary>
        private MatchCheckpointEditorSession? _session;
        /// <summary>The paged state inspector and its UI-only expansion state.</summary>
        private readonly MatchCheckpointEditorFields _fields = new MatchCheckpointEditorFields();
        /// <summary>The field scroll position.</summary>
        private Vector2 _scroll;
        /// <summary>The optional scalar-field name filter.</summary>
        private string _search = string.Empty;
        /// <summary>Whether the loaded board draft has pending edits.</summary>
        private bool _dirty;
        /// <summary>The latest user action result.</summary>
        private string _status = string.Empty;
        /// <summary>The severity of the latest result.</summary>
        private MessageType _statusType;

        /// <summary>Opens the dedicated checkpoint editor from Unity's project menu.</summary>
        [MenuItem("PlantsVsZombies/Match Checkpoint Editor")]
        internal static void Open()
        {
            GetWindow<MatchCheckpointEditorWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("对局存档编辑器");
            minSize = new Vector2(900, 560);
            saveChangesMessage = "当前对局有未保存的修改。保存只写入所选用户的这一个对局文件。";
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += CaptureDraft;
            if (_draft != null)
            {
                _dirty = true;
                hasUnsavedChanges = true;
            }
            if (_directory.Length == 0)
            {
                _directory = Application.persistentDataPath;
            }
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestoreSession();
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= CaptureDraft;
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawDirectory();
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("请退出 Play Mode 后重新读取对局存档。", MessageType.Info);
                return;
            }
            if (_status.Length > 0)
            {
                EditorGUILayout.HelpBox(_status, _statusType);
            }
            if (_session == null)
            {
                EditorGUILayout.HelpBox("选择包含 profiles.dat 和 replica-game-*.dat 的存档目录。先在游戏中退出当前对局以生成对局存档。", MessageType.Info);
                return;
            }
            if (_requiresReload)
            {
                EditorGUILayout.HelpBox("文件已变化或已进入过 Play Mode，请重载后再保存。草稿仍保留在窗口中。", MessageType.Warning);
            }
            DrawSelection();
            var game = _session.Game;
            if (game == null)
            {
                EditorGUILayout.HelpBox("该用户没有已读取的对局。可选择其他用户或已有模式后点击“读取对局”。", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("已读取", _session.CheckpointPath, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("对局", $"{ModeName(_session.SelectedModeId)}  /  关卡 {game.Adventure.Level.Number}  /  植物 {game.Plants.Count}  /  僵尸 {game.Zombies.Count}");
            EditorGUILayout.LabelField("回执标识（只读）", _session.Receipt);
            EditorGUILayout.HelpBox("只编辑已有对象的值；对象类型、ID、引用关系、关卡定义和随机状态只读。账号与冒险通关进度请在 Save Editor 中编辑。", MessageType.None);
            _tab = GUILayout.Toolbar(_tab, s_tabs);
            _search = EditorGUILayout.TextField("筛选字段名", _search);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Min(370f, position.width * .48f);
            try
            {
                using (new EditorGUI.DisabledScope(_requiresReload))
                {
                    if (_fields.Draw(game, _tab, _search))
                    {
                        MarkDirty();
                    }
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUILayout.EndScrollView();
            }
            using (new EditorGUI.DisabledScope(!_dirty || _requiresReload))
            {
                if (GUILayout.Button("保存当前对局" + (_dirty ? " *" : string.Empty)))
                {
                    SaveDraft();
                }
            }
            EditorGUILayout.LabelField("保存保留原文件为 .backup；仅写入当前对局。", EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>Draws directory selection and explicit reload controls.</summary>
        private void DrawDirectory()
        {
            _directory = EditorGUILayout.TextField("存档目录", _directory);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("默认目录"))
                {
                    TryLoadDirectory(Application.persistentDataPath);
                }
                if (GUILayout.Button("浏览…"))
                {
                    var chosen = EditorUtility.OpenFolderPanel("选择对局存档目录", _directory, string.Empty);
                    if (chosen.Length > 0)
                    {
                        TryLoadDirectory(chosen);
                    }
                }
                if (GUILayout.Button("读取 / 重载"))
                {
                    TryLoadDirectory(_directory);
                }
                using (new EditorGUI.DisabledScope(!Directory.Exists(_directory)))
                {
                    if (GUILayout.Button("打开目录"))
                    {
                        EditorUtility.RevealInFinder(_directory);
                    }
                }
            }
        }

        /// <summary>Draws editor-only profile and saved-mode choices without changing the game's selected profile.</summary>
        private void DrawSelection()
        {
            var session = _session!;
            var names = new string[session.Profiles.Count];
            var selected = 0;
            for (var index = 0; index < names.Length; index++)
            {
                names[index] = session.Profiles[index].Name;
                if (session.Profiles[index].Id == session.SelectedProfileId)
                {
                    selected = index;
                }
            }
            var next = EditorGUILayout.Popup("编辑用户", selected, names);
            if (next != selected && ResolvePendingChanges())
            {
                if (RunAction(() => session.SelectProfile(session.Profiles[next].Id), "已切换编辑用户。"))
                {
                    LoadFirstMode();
                }
            }
            var modes = new string[session.AvailableModes.Count];
            var modeIndex = 0;
            for (var index = 0; index < modes.Length; index++)
            {
                modes[index] = ModeName(session.AvailableModes[index]);
                if (session.AvailableModes[index] == session.SelectedModeId)
                {
                    modeIndex = index;
                }
            }
            if (modes.Length > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var mode = EditorGUILayout.Popup("已保存的模式", modeIndex, modes);
                    var read = GUILayout.Button("读取对局", GUILayout.Width(90));
                    if ((mode != modeIndex || read) && ResolvePendingChanges())
                    {
                        LoadMode(session.AvailableModes[mode]);
                    }
                }
            }
        }

        /// <summary>Replaces a directory only after pending edits are saved or explicitly discarded.</summary>
        /// <param name="directory">The requested existing save directory.</param>
        private void TryLoadDirectory(string directory)
        {
            if (ResolvePendingChanges() && RunAction(() =>
            {
                var session = new MatchCheckpointEditorSession(directory);
                _session = session;
                _directory = session.DirectoryPath;
                ClearDirty();
            }, "已读取用户目录。"))
            {
                LoadFirstMode();
            }
        }

        /// <summary>Loads the first existing saved mode when a directory or user has just been selected.</summary>
        private void LoadFirstMode()
        {
            ClearDirty();
            if (_session?.AvailableModes.Count > 0)
            {
                LoadMode(_session.AvailableModes[0]);
            }
        }

        /// <summary>Loads a saved mode while retaining the prior loaded board if the file is invalid.</summary>
        /// <param name="mode">The requested existing native mode ID.</param>
        private void LoadMode(int mode)
        {
            RunAction(() =>
            {
                _session!.SelectMode(mode);
                ClearDirty();
            }, "已读取对局，修改仍保留在草稿中，点击保存后才写入。");
        }

        /// <summary>Restores complete detached edits and their original stale-write baselines after script reload.</summary>
        private void RestoreSession()
        {
            var draft = _draft;
            if (draft != null)
            {
                _dirty = true;
                hasUnsavedChanges = true;
                if (!RunAction(() =>
                {
                    var session = new MatchCheckpointEditorSession(draft.DirectoryPath);
                    session.RestoreDraft(draft);
                    _session = session;
                    _directory = draft.DirectoryPath;
                    _dirty = true;
                    hasUnsavedChanges = true;
                    _requiresReload |= session.HasStaleDraft;
                }, "已恢复对局草稿。"))
                {
                    _requiresReload = true;
                }
            }
            else if (File.Exists(Path.Combine(_directory, "profiles.dat")))
            {
                TryLoadDirectory(_directory);
            }
        }

        /// <summary>Marks an actual model edit for native close warnings and domain-reload recovery.</summary>
        internal void MarkDirty()
        {
            _dirty = true;
            hasUnsavedChanges = true;
            CaptureDraft();
        }

        /// <summary>Captures the full graph only when there are pending edits.</summary>
        private void CaptureDraft()
        {
            if (_dirty && _session?.Game != null)
            {
                _draft = MatchCheckpointEditorDraft.Capture(_session);
            }
        }

        /// <summary>Clears recovery state after an accepted load or successful save.</summary>
        private void ClearDirty()
        {
            _dirty = false;
            _draft = null;
            _requiresReload = false;
            hasUnsavedChanges = false;
            _scroll = Vector2.zero;
        }

        /// <summary>Saves the current board with a backup and conflict checks.</summary>
        /// <returns>Whether the draft was committed successfully.</returns>
        private bool SaveDraft()
        {
            if (_session == null || _requiresReload || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _status = "请退出 Play Mode 并重新读取后保存。";
                _statusType = MessageType.Warning;
                return false;
            }
            return RunAction(() =>
            {
                _session.Save();
                ClearDirty();
            }, "当前对局已保存，原文件已保留为 .backup。");
        }

        /// <summary>Offers the native save/discard/cancel choice before replacing an edited board.</summary>
        /// <returns>Whether navigation can continue.</returns>
        private bool ResolvePendingChanges()
        {
            if (!hasUnsavedChanges)
            {
                return true;
            }
            var choice = EditorUtility.DisplayDialogComplex("尚未保存", "当前对局有未保存的修改。", "保存并继续", "取消", "放弃并继续");
            return choice == 0 ? SaveDraft() : choice == 2;
        }

        /// <summary>Persists the pending board when Unity's native close dialog requests Save.</summary>
        /// <exception cref="InvalidOperationException">The board could not be saved and the window must remain open.</exception>
        public override void SaveChanges()
        {
            if (_dirty && !SaveDraft())
            {
                throw new InvalidOperationException("对局保存失败，请保留窗口并查看错误信息。");
            }
            base.SaveChanges();
        }

        /// <summary>Discards only the editor recovery draft when Unity's close dialog requests Discard.</summary>
        public override void DiscardChanges()
        {
            ClearDirty();
            base.DiscardChanges();
        }

        /// <summary>Reports an expected file or model error without replacing a loaded draft.</summary>
        /// <param name="action">The requested read, select or save operation.</param>
        /// <param name="success">The message shown on success.</param>
        /// <returns>Whether the action completed.</returns>
        private bool RunAction(Action action, string success)
        {
            try
            {
                action();
                _status = success;
                _statusType = MessageType.Info;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException
                || exception is ArgumentException || exception is InvalidOperationException || exception is FormatException)
            {
                _status = exception.Message;
                _statusType = MessageType.Error;
                return false;
            }
        }

        /// <summary>Invalidates loaded snapshots when the actual game can start writing the same files.</summary>
        /// <param name="state">The editor's new Play Mode transition.</param>
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && _session != null)
            {
                CaptureDraft();
                _requiresReload = true;
            }
            if (state == PlayModeStateChange.EnteredEditMode && _session == null && _draft != null)
            {
                RestoreSession();
            }
            Repaint();
        }

        /// <summary>Formats one native saved mode without requiring original game resources.</summary>
        /// <param name="mode">The native mode ID.</param>
        /// <returns>The adventure or challenge name and native ID.</returns>
        private static string ModeName(int mode)
        {
            return mode == 0 ? "0：冒险模式" : mode + "：" + (ModeCatalog.TryGet(mode, out var definition) ? definition.Name : "模式 " + mode);
        }
    }
}
