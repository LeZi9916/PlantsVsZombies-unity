#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using PlantsVsZombies.Simulation;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Draws bounded pages of the checkpoint's actual saved model fields.</summary>
    internal sealed class MatchCheckpointEditorFields
    {
        /// <summary>Limits expanded collection rows per GUI event.</summary>
        private const int PageSize = 40;
        /// <summary>Retains expansion choices without mutating the draft.</summary>
        private readonly HashSet<string> _expanded = new HashSet<string>();
        /// <summary>Distinguishes explicitly closed roots from initially expanded roots.</summary>
        private readonly HashSet<string> _collapsedRoots = new HashSet<string>();
        /// <summary>Retains the selected page for each expanded collection or model.</summary>
        private readonly Dictionary<string, int> _pages = new Dictionary<string, int>();
        /// <summary>Detects reference cycles along the current expanded branch.</summary>
        private readonly List<object> _ancestors = new List<object>();
        /// <summary>Gets the latest rejected field edit, or empty after a successful edit.</summary>
        internal string Error { get; private set; } = string.Empty;
        /// <summary>Maps common native state names to concise editor labels.</summary>
        private static readonly Dictionary<string, string> s_labels = new Dictionary<string, string>
        {
            { "SunAmount", "阳光" }, { "State", "对局状态" }, { "Elapsed", "对局时间（秒）" },
            { "CurrentWave", "当前波次（从 0 开始）" }, { "WaveCountdown", "波次倒计时（秒）" },
            { "Phase", "阶段" }, { "Health", "当前生命值" }, { "Row", "行（从 0 开始）" },
            { "Column", "列（从 0 开始）" }, { "X", "横坐标" }, { "Kind", "对象类型" },
            { "Id", "对象 ID" }, { "Plants", "植物" }, { "Zombies", "僵尸" },
            { "Peas", "投射物" }, { "Suns", "阳光拾取物" }, { "Mowers", "小推车" },
            { "Presentation", "动画与表现状态" }, { "ModeSession", "模式状态" }
        };

        /// <summary>Draws one category of detached state without advancing gameplay or changing collection topology.</summary>
        /// <param name="game">The saved board draft.</param>
        /// <param name="tab">The selected editor category.</param>
        /// <param name="search">An optional leaf field-name filter.</param>
        /// <returns>Whether any actual model value changed.</returns>
        internal bool Draw(GameSimulation game, int tab, string search)
        {
            var world = game.CheckpointState;
            var changed = false;
            if (tab == 0)
            {
                var node = Root("对局", "World", world);
                foreach (var child in node.Children(0, node.ChildCount))
                {
                    if (child.Name == "SunAmount" || child.Name == "State" || child.Name == "Elapsed" || child.Name == "SpawnedZombies")
                    {
                        changed |= DrawNode(child, 0, search);
                    }
                }
                var board = Root("关卡进度", "Board", game.Adventure);
                foreach (var child in board.Children(0, board.ChildCount))
                {
                    if (child.Name == "CurrentWave" || child.Name == "WaveCountdown" || child.Name == "Phase"
                        || child.Name == "FlagsSpawned" || child.Name == "TotalWavesSpawned" || child.Name == "MoneyCollectedUnits")
                    {
                        changed |= DrawNode(child, 0, search);
                    }
                }
            }
            else if (tab == 1)
            {
                changed |= DrawNode(Root("植物", "Plants", world.Plants), 0, search);
            }
            else if (tab == 2)
            {
                changed |= DrawNode(Root("僵尸", "Zombies", world.Zombies), 0, search);
            }
            else if (tab == 3)
            {
                EditorGUILayout.HelpBox("单位为原版 tick（0.01 秒）；0 表示已就绪。此处保留种子槽和所选卡片。", MessageType.Info);
                changed |= DrawNode(Root("种子剩余冷却", "_seedCooldownTicks", world._seedCooldownTicks), 0, search);
                changed |= DrawNode(Root("种子初始冷却阈值", "_seedInitialCooldownThresholds", world._seedInitialCooldownThresholds), 0, search);
            }
            else if (tab == 4)
            {
                changed |= DrawNode(Root("投射物", "Peas", world.Peas), 0, search);
                changed |= DrawNode(Root("阳光拾取物", "Suns", world.Suns), 0, search);
                changed |= DrawNode(Root("金币与掉落物", "Coins", game.Adventure._coins), 0, search);
                changed |= DrawNode(Root("玉米炮弹", "CobShells", game.Adventure._cobShells), 0, search);
                changed |= DrawNode(Root("火焰", "Flames", world.Flames), 0, search);
            }
            else if (tab == 5)
            {
                changed |= DrawNode(Root("小推车", "Mowers", world.Mowers), 0, search);
                changed |= DrawNode(Root("场景状态", "Board", game.Adventure), 0, search);
                changed |= DrawNode(Root("模式状态", "ModeSession", game.ModeSession), 0, search);
                changed |= DrawNode(Root("动画与特效", "Presentation", world.Presentation), 0, search);
            }
            else
            {
                changed |= DrawNode(Root("全部已保存字段", "World", world), 0, search);
            }
            if (Error.Length > 0)
            {
                EditorGUILayout.HelpBox(Error, MessageType.Error);
            }
            return changed;
        }

        /// <summary>Creates a nonreplaceable root whose existing model children remain editable.</summary>
        /// <param name="name">The category label.</param>
        /// <param name="path">The stable root path.</param>
        /// <param name="value">The detached model or logical collection.</param>
        /// <returns>The root field adapter.</returns>
        private static MatchCheckpointFieldNode Root(string name, string path, object? value)
        {
            return new MatchCheckpointFieldNode(name, path, value?.GetType() ?? typeof(object), () => value);
        }

        /// <summary>Draws one scalar or lazily expanded model, with cycle and depth guards.</summary>
        /// <param name="node">The value adapter.</param>
        /// <param name="depth">The current bounded expansion depth.</param>
        /// <param name="search">The leaf-name filter.</param>
        /// <returns>Whether a descendant was actually edited.</returns>
        private bool DrawNode(MatchCheckpointFieldNode node, int depth, string search)
        {
            var value = node.Value;
            var label = s_labels.TryGetValue(node.Name, out var translated) ? translated : node.Name;
            if (value == null || MatchCheckpointFieldNode.IsScalar(value.GetType()))
            {
                if (search.Length > 0 && label.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0
                    && node.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
                return DrawScalar(node, label, value);
            }
            if (value is Plant plant)
            {
                label += $" #{plant.Id} {plant.Kind}  行 {plant.Row + 1} / 列 {plant.Column + 1}";
            }
            else if (value is Zombie zombie)
            {
                label += $" #{zombie.Id} {zombie.Kind}  行 {zombie.Row + 1}";
            }
            var cycle = false;
            foreach (var ancestor in _ancestors)
            {
                cycle |= ReferenceEquals(ancestor, value);
            }
            if (cycle || depth >= 10)
            {
                EditorGUILayout.LabelField(label, cycle ? "已在上级显示的引用" : "达到显示深度限制");
                return false;
            }
            var count = node.ChildCount;
            var expanded = depth == 0 ? !_collapsedRoots.Contains(node.Path) : _expanded.Contains(node.Path);
            expanded = EditorGUILayout.Foldout(expanded, label + $"  ({count})" + (node.ReadOnly ? " [只读]" : string.Empty), true);
            if (!expanded)
            {
                _expanded.Remove(node.Path);
                if (depth == 0)
                {
                    _collapsedRoots.Add(node.Path);
                }
                return false;
            }
            _collapsedRoots.Remove(node.Path);
            _expanded.Add(node.Path);
            _ancestors.Add(value);
            EditorGUI.indentLevel++;
            var changed = false;
            try
            {
                var page = _pages.TryGetValue(node.Path, out var prior) ? prior : 0;
                var pages = Math.Max(1, (count + PageSize - 1) / PageSize);
                page = Math.Min(page, pages - 1);
                if (pages > 1)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(page == 0))
                        {
                            if (GUILayout.Button("上一页", GUILayout.Width(75)))
                            {
                                page--;
                            }
                        }
                        EditorGUILayout.LabelField($"{page + 1} / {pages}", GUILayout.Width(80));
                        using (new EditorGUI.DisabledScope(page == pages - 1))
                        {
                            if (GUILayout.Button("下一页", GUILayout.Width(75)))
                            {
                                page++;
                            }
                        }
                    }
                }
                _pages[node.Path] = page;
                foreach (var child in node.Children(page * PageSize, PageSize))
                {
                    changed |= DrawNode(child, depth + 1, search);
                }
            }
            finally
            {
                EditorGUI.indentLevel--;
                _ancestors.RemoveAt(_ancestors.Count - 1);
            }
            return changed;
        }

        /// <summary>Draws a typed editor control and applies only finite, representable changes.</summary>
        /// <param name="node">The scalar adapter.</param>
        /// <param name="label">The visible localized label.</param>
        /// <param name="value">The current scalar value.</param>
        /// <returns>Whether a value was successfully written.</returns>
        private bool DrawScalar(MatchCheckpointFieldNode node, string label, object? value)
        {
            if (value == null)
            {
                EditorGUILayout.LabelField(label, "null");
                return false;
            }
            using (new EditorGUI.DisabledScope(!node.CanWrite))
            {
                EditorGUI.BeginChangeCheck();
                object edited = value;
                var type = value.GetType();
                if (type.IsEnum)
                {
                    edited = EditorGUILayout.EnumPopup(label, (Enum)value);
                }
                else if (type == typeof(bool))
                {
                    edited = EditorGUILayout.Toggle(label, (bool)value);
                }
                else if (type == typeof(float))
                {
                    edited = EditorGUILayout.FloatField(label, (float)value);
                }
                else if (type == typeof(double))
                {
                    edited = EditorGUILayout.DoubleField(label, (double)value);
                }
                else if (type == typeof(string) || type == typeof(char))
                {
                    var text = EditorGUILayout.TextField(label, value.ToString());
                    edited = type == typeof(char) ? (object)(text.Length == 0 ? '\0' : text[0]) : text;
                }
                else if (type == typeof(int) || type == typeof(short) || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte))
                {
                    edited = EditorGUILayout.IntField(label, Convert.ToInt32(value));
                }
                else if (type == typeof(long) || type == typeof(uint))
                {
                    edited = EditorGUILayout.LongField(label, Convert.ToInt64(value));
                }
                else if (type == typeof(ulong) || type == typeof(decimal))
                {
                    edited = EditorGUILayout.TextField(label, Convert.ToString(value, CultureInfo.InvariantCulture));
                }
                else
                {
                    EditorGUILayout.LabelField(label, value.ToString());
                }
                var changed = EditorGUI.EndChangeCheck();
                if (changed && node.CanWrite)
                {
                    try
                    {
                        node.SetValue(type.IsEnum ? edited : Convert.ChangeType(edited, type, CultureInfo.InvariantCulture));
                        Error = string.Empty;
                        return true;
                    }
                    catch (Exception exception) when (exception is ArgumentException || exception is FormatException || exception is OverflowException)
                    {
                        Error = label + "：" + exception.Message;
                    }
                }
                return false;
            }
        }
    }
}
