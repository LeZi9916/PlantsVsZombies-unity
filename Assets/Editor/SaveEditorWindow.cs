#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Edits local profile, account and campaign saves through their existing persistence services.</summary>
    internal sealed class SaveEditorWindow : EditorWindow
    {
        /// <summary>The tabs that group the persisted account and campaign fields.</summary>
        private static readonly string[] s_tabs = { "基本数据", "冒险进度", "商店物品", "模式解锁", "禅境花园", "高级数据" };

        /// <summary>The directory entered by the user, retained across editor domain reloads.</summary>
        [SerializeField]
        private string _directory = "";

        /// <summary>The visible field category, retained across editor domain reloads.</summary>
        [SerializeField]
        private int _tab;

        /// <summary>Preserves complete detached drafts across script compilation and editor domain reloads.</summary>
        [SerializeField]
        private SaveEditorDraft? _draft;

        /// <summary>The independent loaded save session, or null before opening a valid catalog.</summary>
        private SaveEditorSession? _session;

        /// <summary>The field scroll position.</summary>
        private Vector2 _scroll;

        /// <summary>The name currently entered in the profile rename control.</summary>
        private string _profileName = "";

        /// <summary>The last committed profile name, available even when the catalog needs reopening.</summary>
        private string _savedProfileName = "";

        /// <summary>The optional purchase or record field filter.</summary>
        private string _search = "";

        /// <summary>The current garden plant index.</summary>
        private int _gardenSelection;

        /// <summary>Whether the detached account has edits that have not been saved.</summary>
        private bool _accountDirty;

        /// <summary>Whether the detached adventure checkpoint has edits that have not been saved.</summary>
        private bool _adventureDirty;

        /// <summary>Whether entering Play Mode invalidated the loaded disk snapshots.</summary>
        [SerializeField]
        private bool _requiresReload;

        /// <summary>The latest load or save result displayed in the window.</summary>
        private string _status = "";

        /// <summary>The severity of the latest result displayed in the window.</summary>
        private MessageType _statusType = MessageType.Info;

        /// <summary>Gets whether the profile name contains an uncommitted edit.</summary>
        private bool NameDirty => _session == null ? _draft != null : !string.Equals(_profileName, _savedProfileName, StringComparison.Ordinal);

        /// <summary>Opens the save editor from the project's editor menu.</summary>
        [MenuItem("PlantsVsZombies/Save Editor")]
        internal static void Open()
        {
            var window = GetWindow<SaveEditorWindow>();
            window.titleContent = new GUIContent("存档编辑器");
            window.minSize = new Vector2(660, 480);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("存档编辑器");
            minSize = new Vector2(660, 480);
            saveChangesMessage = "存档编辑器中有尚未保存的数据。账号和冒险进度分别写入各自的存档文件。";
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (string.IsNullOrEmpty(_directory))
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
        }

        private void OnGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawDirectory();
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("请退出 Play Mode 后重新读取存档，再进行编辑。", MessageType.Info);
                return;
            }

            if (_requiresReload)
            {
                EditorGUILayout.HelpBox("存档可能发生变化。请点击“读取 / 重载”刷新；未保存的草稿会在重载前提示处理。", MessageType.Warning);
                if (_draft != null && GUILayout.Button("尝试恢复原目录草稿"))
                {
                    RestoreSession();
                }
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.HelpBox(_status, _statusType);
            }

            if (_session == null)
            {
                EditorGUILayout.HelpBox("选择包含 profiles.dat 的游戏存档目录。默认目录为当前项目的 Application.persistentDataPath；首次使用时请先在游戏中创建用户。", MessageType.Info);
                return;
            }

            if (_requiresReload)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(_requiresReload))
            {
                DrawProfile();
                _tab = GUILayout.Toolbar(_tab, s_tabs);
                if (_tab == 2 || _tab == 3)
                {
                    _search = EditorGUILayout.TextField("筛选名称 / 索引", _search);
                }

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                var previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Mathf.Min(300, position.width * 0.48f);
                try
                {
                    DrawFields();
                }
                finally
                {
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                    EditorGUILayout.EndScrollView();
                }

                DrawSaveButtons();
            }

            CaptureDraft();
        }

        /// <summary>Saves each edited sidecar before the editor closes this window.</summary>
        public override void SaveChanges()
        {
            if (!SavePendingChanges())
            {
                throw new InvalidOperationException("存档未能全部保存；请保留窗口并查看错误信息。");
            }

            base.SaveChanges();
        }

        /// <summary>Discards detached edits when the editor's close dialog requests it.</summary>
        public override void DiscardChanges()
        {
            _accountDirty = false;
            _adventureDirty = false;
            _draft = null;
            base.DiscardChanges();
        }

        /// <summary>Draws the save-directory controls and explicit read action.</summary>
        private void DrawDirectory()
        {
            EditorGUILayout.LabelField("存档目录", EditorStyles.boldLabel);
            _directory = EditorGUILayout.TextField(_directory);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("默认目录"))
                {
                    TryLoadDirectory(Application.persistentDataPath);
                }

                if (GUILayout.Button("浏览…"))
                {
                    var chosen = EditorUtility.OpenFolderPanel("选择游戏存档目录", _directory, "");
                    if (!string.IsNullOrEmpty(chosen))
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

            if (_session != null)
            {
                EditorGUILayout.LabelField("已读取", _session.DirectoryPath, EditorStyles.wordWrappedMiniLabel);
            }
        }

        /// <summary>Draws an editor-only user selection and an explicit persisted rename action.</summary>
        private void DrawProfile()
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
                RunAction(() =>
                {
                    session.SelectProfile(session.Profiles[next].Id);
                    ResetDraftState();
                }, "已切换编辑用户。游戏中的当前用户选择保持不变。");
            }

            EditorGUILayout.LabelField("用户标识", session.SelectedProfileId.ToString());
            using (new EditorGUILayout.HorizontalScope())
            {
                _profileName = EditorGUILayout.TextField("用户名", _profileName);
                using (new EditorGUI.DisabledScope(!NameDirty))
                {
                    if (GUILayout.Button("保存名称", GUILayout.Width(90)))
                    {
                        SaveName();
                    }
                }
            }
        }

        /// <summary>Draws the selected group and tracks edits separately for each persistent sidecar.</summary>
        private void DrawFields()
        {
            var session = _session!;
            EditorGUI.BeginChangeCheck();
            switch (_tab)
            {
                case 0:
                    SaveEditorFields.DrawAccount(session.Account);
                    break;
                case 1:
                    SaveEditorFields.DrawAdventure(session.Adventure);
                    break;
                case 2:
                    SaveEditorFields.DrawPurchases(session.Account, _search);
                    break;
                case 3:
                    SaveEditorFields.DrawRecords(session.Account, session.Adventure, _search);
                    break;
                case 4:
                    SaveEditorFields.DrawGarden(session.Account, ref _gardenSelection);
                    break;
                case 5:
                    SaveEditorFields.DrawAdvanced(session.Account);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (_tab == 1)
                {
                    _adventureDirty = true;
                }
                else
                {
                    _accountDirty = true;
                }

                UpdateUnsavedChanges();
            }
        }

        /// <summary>Draws explicit save actions for the account and campaign files.</summary>
        private void DrawSaveButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_accountDirty))
                {
                    if (GUILayout.Button(_accountDirty ? "保存账号数据 *" : "账号数据已保存"))
                    {
                        SaveAccount();
                    }
                }

                using (new EditorGUI.DisabledScope(!_adventureDirty))
                {
                    if (GUILayout.Button(_adventureDirty ? "保存冒险进度 *" : "冒险进度已保存"))
                    {
                        SaveAdventure();
                    }
                }
            }

            EditorGUILayout.LabelField("写入时保留原文件备份；外部修改冲突需要重载。", EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>Loads a requested directory after resolving any detached edits.</summary>
        /// <param name="directory">The requested save directory.</param>
        private void TryLoadDirectory(string directory)
        {
            if (ResolvePendingChanges())
            {
                _directory = directory;
                LoadDirectory();
            }
        }

        /// <summary>Loads all three save services without creating or modifying user data.</summary>
        /// <returns>True when a complete disk session was loaded.</returns>
        private bool LoadDirectory()
        {
            return RunAction(() =>
            {
                var loaded = new SaveEditorSession(_directory);
                _session = loaded;
                _directory = loaded.DirectoryPath;
                _requiresReload = false;
                ResetDraftState();
            }, "存档已读取。修改后请保存对应的数据文件。");
        }

        /// <summary>Reopens the disk session and restores any serialized draft after a domain reload.</summary>
        private void RestoreSession()
        {
            var draft = _draft;
            if (draft != null)
            {
                _directory = draft.DirectoryPath;
            }

            var loaded = LoadDirectory();
            if (draft != null)
            {
                _draft = draft;
                _profileName = draft.ProfileName;
                _accountDirty = draft.AccountDirty;
                _adventureDirty = draft.AdventureDirty;
                _requiresReload = true;
                UpdateUnsavedChanges();
            }

            if (draft != null && loaded && _session != null)
            {
                RunAction(() =>
                {
                    _requiresReload = !draft.Restore(_session);
                    _profileName = draft.ProfileName;
                    _accountDirty = draft.AccountDirty;
                    _adventureDirty = draft.AdventureDirty;
                    _draft = draft;
                    UpdateUnsavedChanges();
                }, "已恢复脚本重新加载前的编辑草稿。");
            }
        }

        /// <summary>Clears detached edit markers after successfully loading or switching users.</summary>
        private void ResetDraftState()
        {
            _accountDirty = false;
            _adventureDirty = false;
            _savedProfileName = _session!.ProfileName;
            _profileName = _savedProfileName;
            _gardenSelection = 0;
            _scroll = Vector2.zero;
            UpdateUnsavedChanges();
        }

        /// <summary>Offers to save, discard or retain edits before navigation changes the loaded draft.</summary>
        /// <returns>True when navigation may continue.</returns>
        private bool ResolvePendingChanges()
        {
            if (!_accountDirty && !_adventureDirty && !NameDirty)
            {
                return true;
            }

            var choice = EditorUtility.DisplayDialogComplex("尚未保存", "当前用户有未保存的数据或名称。账号、冒险进度和名称分别保存。", "保存并继续", "取消", "放弃并继续");
            if (choice == 0)
            {
                return SavePendingChanges();
            }

            return choice == 2;
        }

        /// <summary>Saves dirty sidecars in order and retains any draft whose save failed.</summary>
        /// <returns>True when every dirty sidecar was saved.</returns>
        private bool SavePendingChanges()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || _requiresReload || _session == null)
            {
                _status = "请退出 Play Mode 并重新读取后保存。也可在重载提示中选择放弃草稿。";
                _statusType = MessageType.Warning;
                return false;
            }

            if (_accountDirty && !SaveAccount())
            {
                return false;
            }

            if (_adventureDirty && !SaveAdventure())
            {
                return false;
            }

            return !NameDirty || SaveName();
        }

        /// <summary>Persists the edited profile name without changing either progression draft.</summary>
        /// <returns>True when persistence succeeded.</returns>
        private bool SaveName()
        {
            return RunAction(() =>
            {
                _session!.RenameProfile(_profileName);
                _savedProfileName = _session.ProfileName;
                _profileName = _savedProfileName;
                UpdateUnsavedChanges();
            }, "用户名已保存。");
        }

        /// <summary>Validates and persists the account draft, keeping the campaign draft independent.</summary>
        /// <returns>True when persistence succeeded.</returns>
        private bool SaveAccount()
        {
            return RunAction(() =>
            {
                _session!.SaveAccount();
                _accountDirty = false;
                UpdateUnsavedChanges();
            }, "账号数据已保存：player.account（包含商店、模式、花园和智慧树）。");
        }

        /// <summary>Validates and persists the campaign draft, keeping the account draft independent.</summary>
        /// <returns>True when persistence succeeded.</returns>
        private bool SaveAdventure()
        {
            return RunAction(() =>
            {
                _session!.SaveAdventure();
                _adventureDirty = false;
                UpdateUnsavedChanges();
            }, "冒险进度已保存：campaign.adventure。");
        }

        /// <summary>Runs a user action and displays expected filesystem or validation failures in the window.</summary>
        /// <param name="action">The load, save or profile action.</param>
        /// <param name="success">The status text shown after success.</param>
        /// <returns>True when the action completed.</returns>
        private bool RunAction(Action action, string success)
        {
            try
            {
                action();
                _status = success;
                _statusType = MessageType.Info;
                CaptureDraft();
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is InvalidOperationException)
            {
                _status = exception.Message;
                _statusType = MessageType.Error;
                if (exception is IOException && exception is not InvalidDataException || exception is InvalidOperationException)
                {
                    _requiresReload = _session != null;
                }

                Debug.LogWarning("[" + nameof(SaveEditorWindow) + "] 存档操作失败（" + exception.GetType().Name + "），请查看存档编辑器。", this);
                return false;
            }
        }

        /// <summary>Updates Unity's native close-window warning after the draft state changes.</summary>
        private void UpdateUnsavedChanges()
        {
            hasUnsavedChanges = _accountDirty || _adventureDirty || NameDirty;
        }

        /// <summary>Captures current detached state for Unity's window serialization without writing save files.</summary>
        private void CaptureDraft()
        {
            if (_session != null && !_requiresReload)
            {
                UpdateUnsavedChanges();
                _draft = hasUnsavedChanges ? SaveEditorDraft.Capture(_session, _profileName, _accountDirty, _adventureDirty) : null;
            }
        }

        /// <summary>Invalidates loaded disk snapshots as the game enters or leaves Play Mode.</summary>
        /// <param name="state">The editor's new Play Mode transition state.</param>
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && _session != null)
            {
                CaptureDraft();
                _requiresReload = true;
            }

            if (state == PlayModeStateChange.EnteredEditMode && _session == null)
            {
                RestoreSession();
            }

            Repaint();
        }
    }
}
