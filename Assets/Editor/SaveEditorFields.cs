#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Draws typed controls for detached save snapshots without performing disk writes.</summary>
    internal static class SaveEditorFields
    {
        /// <summary>The mask containing every valid adventure reward bit.</summary>
        private const long AllRewards = (1L << 50) - 1;

        /// <summary>Names the plants accepted by the persistent account and adventure formats.</summary>
        private static readonly string[] s_plantNames = BuildPlantNames();

        /// <summary>Names the automatic, explicitly unlocked and explicitly locked access policies.</summary>
        private static readonly string[] s_modeAccessNames = { "跟随游戏进度", "解锁", "锁定" };

        /// <summary>Draws account balances, unlocks and introduction markers.</summary>
        /// <param name="account">The detached account to edit.</param>
        internal static void DrawAccount(PlayerAccount account)
        {
            account.Coins = Integer("金币（存档单位）", account.Coins, 0, 99999);
            EditorGUILayout.LabelField("游戏显示金币", account.DisplayCoins.ToString("N0"));
            EditorGUILayout.HelpBox("1 存档单位 = 10 游戏金币。", MessageType.Info);
            account.TreeHeight = Integer("智慧树高度", account.TreeHeight, 0, 10000000);
            account.PacketUpgradeDeclines = Integer("拒绝卡槽升级次数", account.PacketUpgradeDeclines);
            account.MinigamesUnlocked = EditorGUILayout.Toggle("解锁迷你游戏", account.MinigamesUnlocked);
            account.PuzzleUnlocked = EditorGUILayout.Toggle("解锁解谜模式", account.PuzzleUnlocked);
            account.SurvivalUnlocked = EditorGUILayout.Toggle("解锁生存模式", account.SurvivalUnlocked);
            EditorGUILayout.LabelField("具体关卡的解锁状态可在“模式解锁”页修改。", EditorStyles.wordWrappedMiniLabel);
            account.TreeFoodGiftClaimed = EditorGUILayout.Toggle("已领取首次树肥", account.TreeFoodGiftClaimed);
            account.GardenGiftClaimed = EditorGUILayout.Toggle("已领取花园入门奖励", account.GardenGiftClaimed);
            using (new EditorGUI.DisabledScope(account.PurchaseCount(14) > 0))
            {
                var initialized = EditorGUILayout.Toggle("花园肥料已初始化", account.GardenFertilizerInitialized);
                if (initialized != account.GardenFertilizerInitialized)
                {
                    account._receipts["zen:fertilizer"] = initialized ? 1 : 0;
                }
            }

            EditorGUILayout.LabelField("肥料库存大于 0 时始终视为已初始化。", EditorStyles.wordWrappedMiniLabel);

            account.SnailAwakeUntilUtc = Timestamp("蜗牛清醒截止时间", account.SnailAwakeUntilUtc);
        }

        /// <summary>Draws a consistent campaign checkpoint and its saved starting loadout.</summary>
        /// <param name="progress">The detached campaign checkpoint to edit.</param>
        internal static void DrawAdventure(AdventureProgress progress)
        {
            var completed = progress.Completed;
            var level = Integer("下一关（1–50）", progress.NextLevel, 1, 50);
            completed = EditorGUILayout.Toggle("本轮全部通关", completed);
            if (level != progress.NextLevel && level != 50)
            {
                completed = false;
            }

            progress.NextLevel = completed ? 50 : level;
            progress.CollectedRewards = completed ? (ulong)AllRewards : (1UL << (progress.NextLevel - 1)) - 1;
            progress.Cycle = Integer("当前周目", progress.Cycle, 1, 1000000);
            EditorGUILayout.LabelField("已通关次数", progress.CompletedRuns.ToString());
            EditorGUILayout.LabelField("已领取关卡奖励", completed ? "1–50" : "前 " + (progress.NextLevel - 1) + " 关");
            progress.AttemptSeed = EditorGUILayout.IntField("本次尝试随机种子", progress.AttemptSeed);
            var previousSlots = progress.PacketSlots;
            progress.PacketSlots = Integer("本次尝试卡槽数", progress.PacketSlots, 6, 10);
            var hasTarget = EditorGUILayout.Toggle("保存模仿者目标", progress.ImitaterTarget.HasValue);
            progress.ImitaterTarget = hasTarget ? Plant("模仿者目标", progress.ImitaterTarget ?? PlantKind.Peashooter, 39) : (PlantKind?)null;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("修改关卡会同步已领取奖励范围。选卡可留空，由游戏重新选卡；卡槽升级次数在商店物品中设置。", MessageType.Info);
            var seeds = new List<PlantKind>(progress.SelectedSeeds);
            var count = Integer("已选卡片数量", seeds.Count, 0, progress.PacketSlots);
            if (previousSlots != progress.PacketSlots)
            {
                count = Math.Min(count, progress.PacketSlots);
            }
            while (seeds.Count > count)
            {
                seeds.RemoveAt(seeds.Count - 1);
            }

            while (seeds.Count < count)
            {
                var next = 0;
                while (seeds.Contains((PlantKind)next))
                {
                    next++;
                }

                seeds.Add((PlantKind)next);
            }

            for (var index = 0; index < seeds.Count; index++)
            {
                var chosen = Plant("卡片 " + (index + 1), seeds[index]);
                var duplicate = seeds.IndexOf(chosen);
                if (duplicate >= 0 && duplicate != index)
                {
                    seeds[duplicate] = seeds[index];
                }

                seeds[index] = chosen;
            }

            progress.SelectedSeeds = seeds.AsReadOnly();
        }

        /// <summary>Draws every purchase counter, including unknown slots retained by the file format.</summary>
        /// <param name="account">The detached account whose counters are edited.</param>
        /// <param name="search">A case-insensitive name or identifier filter.</param>
        internal static void DrawPurchases(PlayerAccount account, string search)
        {
            EditorGUILayout.HelpBox("0 通常表示未拥有。ID 10–12 保存最后购买日的 UTC 天号；ID 21 为卡槽升级次数（0–4）。其余值保留存档原有含义。", MessageType.Info);
            for (var index = 0; index < account._purchases.Length; index++)
            {
                var name = index == 26 ? "巧克力" : "保留项目";
                foreach (var item in ShopCatalog.Items)
                {
                    if (item.Id == index)
                    {
                        name = item.Name;
                        break;
                    }
                }

                var label = "[" + index + "] " + name;
                if (Matches(label, search))
                {
                    account._purchases[index] = Integer(label, account._purchases[index], 0, index == 21 ? 4 : int.MaxValue);
                }
            }
        }

        /// <summary>Draws individual mode access policies and records using persistent zero-based indices.</summary>
        /// <param name="account">The detached account containing the records.</param>
        /// <param name="progress">The detached checkpoint used to preview automatic unlock requirements.</param>
        /// <param name="search">A case-insensitive name or identifier filter.</param>
        internal static void DrawRecords(PlayerAccount account, AdventureProgress progress, string search)
        {
            if (GUILayout.Button("解锁所有模式"))
            {
                ModeAccessRules.UnlockAll(account);
                GUI.changed = true;
            }

            EditorGUILayout.HelpBox("可逐个设置“跟随游戏进度 / 解锁 / 锁定”。解锁会同时开启对应模式入口，修改后点击“保存账号数据”生效。当前状态按窗口中的冒险草稿预览；冒险修改需单独保存。", MessageType.Info);
            EditorGUILayout.LabelField("记录索引 = 模式 ID − 1；成绩与通关记录可在下方独立编辑。", EditorStyles.wordWrappedMiniLabel);
            for (var index = 0; index < account._modeRecords.Length; index++)
            {
                var knownMode = ModeCatalog.TryGet(index + 1, out var mode);
                var name = knownMode ? mode!.Name : "保留记录";
                if (index == 49)
                {
                    name = "原版智慧树记录";
                }

                var label = knownMode ? "模式 " + (index + 1) + " / 记录 [" + index + "] " + name : "记录 [" + index + "] " + name;
                if (Matches(label, search))
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                        if (knownMode)
                        {
                            var modeId = index + 1;
                            var access = ModeAccessRules.GetOverride(account, modeId);
                            var policy = access.HasValue ? access.Value ? 1 : 2 : 0;
                            var edited = EditorGUILayout.Popup("解锁状态", policy, s_modeAccessNames);
                            if (edited != policy)
                            {
                                ModeAccessRules.SetOverride(account, modeId, edited == 0 ? (bool?)null : edited == 1);
                            }

                            var reason = ModeAccessRules.LockReason(modeId, account, progress.NextLevel, progress.CompletedRuns > 0);
                            EditorGUILayout.LabelField("当前状态", reason == null ? "已解锁，可进入" : "已锁定：" + reason, EditorStyles.wordWrappedMiniLabel);
                        }

                        account._modeRecords[index] = Integer(index == 49 ? "原版智慧树记录" : "成绩 / 通关记录", account._modeRecords[index]);
                    }
                }
            }
        }

        /// <summary>Draws a plant selector followed by the selected plant's complete persisted data.</summary>
        /// <param name="account">The detached account containing mutable plant copies.</param>
        /// <param name="selection">The selected plant index, updated after adding or removing a plant.</param>
        internal static void DrawGarden(PlayerAccount account, ref int selection)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新增植物"))
                {
                    AddPlant(account, ref selection);
                }

                using (new EditorGUI.DisabledScope(account._garden.Count == 0))
                {
                    if (GUILayout.Button("移除当前植物"))
                    {
                        selection = Mathf.Clamp(selection, 0, account._garden.Count - 1);
                        account._garden.RemoveAt(selection);
                        GUI.changed = true;
                    }
                }
            }

            if (account._garden.Count == 0)
            {
                EditorGUILayout.HelpBox("当前用户没有花园植物。新增和移除将在保存账号后生效。", MessageType.Info);
                return;
            }

            var labels = new string[account._garden.Count];
            for (var index = 0; index < labels.Length; index++)
            {
                var item = account._garden[index];
                labels[index] = (index + 1) + ". " + item.Kind + " / " + item.Garden + " / " + item.Slot;
            }

            var changedBeforeSelection = GUI.changed;
            selection = EditorGUILayout.Popup("当前植物", Mathf.Clamp(selection, 0, labels.Length - 1), labels);
            GUI.changed = changedBeforeSelection;
            var plant = account._garden[selection];
            EditorGUILayout.LabelField("植物标识", plant.Id.ToString());
            plant.Kind = Plant("植物类型", plant.Kind);
            var garden = (GardenKind)EditorGUILayout.EnumPopup("所在花园", plant.Garden);
            if (garden != plant.Garden)
            {
                plant.Garden = garden;
                plant.Slot = Math.Min(plant.Slot, garden == GardenKind.Greenhouse ? 31 : 7);
            }
            plant.Slot = Integer("位置（从 0 开始）", plant.Slot, 0, plant.Garden == GardenKind.Greenhouse ? 31 : 7);
            plant.Growth = Integer("生长阶段", plant.Growth, 0, 3);
            plant.Waterings = Integer("已浇水次数", plant.Waterings, 0, 5);
            plant.FeedingsPerGrow = Integer("每阶段浇水需求", plant.FeedingsPerGrow, 3, 5);
            plant.MatureNeed = EditorGUILayout.Popup("成熟后需求", plant.MatureNeed == GardenNeed.Music ? 1 : 0, new[] { "杀虫剂", "音乐" }) == 1 ? GardenNeed.Music : GardenNeed.BugSpray;
            plant.PendingCoins = Integer("待收集金币（存档单位）", plant.PendingCoins, 0, 99999);
            plant.CareCycle = Integer("照料周期", plant.CareCycle);
            plant.OriginalIndex = Integer("原版来源索引（-1 为新增）", plant.OriginalIndex, -1, 199);
            plant.NextCareUtc = Timestamp("下次照料时间", plant.NextCareUtc);
            plant.HappyUntilUtc = Timestamp("心满意足截止时间", plant.HappyUntilUtc);
            plant.LastWaterUtc = Timestamp("上次浇水时间", plant.LastWaterUtc);
            plant.LastNeedFulfilledUtc = Timestamp("上次满足需求时间", plant.LastNeedFulfilledUtc);
            plant.LastFertilizedUtc = Timestamp("上次施肥时间", plant.LastFertilizedUtc);
            plant.ChocolateUntilUtc = Timestamp("巧克力效果截止时间", plant.ChocolateUntilUtc);
            plant.LastCoinUtc = Timestamp("金币计时时间", plant.LastCoinUtc);
        }

        /// <summary>Draws reward bookkeeping and existing idempotency receipts without changing their keys.</summary>
        /// <param name="account">The detached account whose bookkeeping is edited.</param>
        internal static void DrawAdvanced(PlayerAccount account)
        {
            EditorGUILayout.HelpBox("奖励标记和回执控制重复领奖与教程状态。调整前请核对目标关卡和周目；普通编辑会保留这些数据。时间字段使用 UTC Unix 秒，0 表示未设置。", MessageType.Info);
            account._rewardCycle = Integer("账号奖励周目", account._rewardCycle, 0, 1000000);
            account._rewardMask = (ulong)Math.Clamp(EditorGUILayout.LongField("账号奖励位掩码（50 位）", (long)account._rewardMask), 0, AllRewards);
            EditorGUILayout.LabelField("已保存回执", account._receipts.Count.ToString());
            var keys = new List<string>(account._receipts.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (var key in keys)
            {
                account._receipts[key] = Integer(key, account._receipts[key]);
            }
        }

        /// <summary>Draws an integer constrained to a persistent field's supported range.</summary>
        /// <param name="label">The field's display label.</param>
        /// <param name="value">The current field value.</param>
        /// <param name="minimum">The inclusive minimum.</param>
        /// <param name="maximum">The inclusive maximum.</param>
        /// <returns>The edited value within the supported range.</returns>
        private static int Integer(string label, int value, int minimum = 0, int maximum = int.MaxValue)
        {
            EditorGUI.BeginChangeCheck();
            var edited = EditorGUILayout.IntField(label, value);
            return EditorGUI.EndChangeCheck() ? Math.Clamp(edited, minimum, maximum) : value;
        }

        /// <summary>Draws a Unix timestamp and a local-time preview for easier inspection.</summary>
        /// <param name="label">The field's display label.</param>
        /// <param name="value">The current Unix timestamp in seconds.</param>
        /// <returns>A nonnegative timestamp supported by DateTimeOffset.</returns>
        private static long Timestamp(string label, long value)
        {
            EditorGUI.BeginChangeCheck();
            var edited = EditorGUILayout.LongField(label + "（UTC 秒）", value);
            edited = EditorGUI.EndChangeCheck() ? Math.Clamp(edited, 0, 253402300799L) : value;
            if (edited > 0 && edited <= 253402300799L)
            {
                EditorGUILayout.LabelField("本地时间", DateTimeOffset.FromUnixTimeSeconds(edited).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz"));
            }

            return edited;
        }

        /// <summary>Draws a plant popup restricted to the persistent format's plant identifiers.</summary>
        /// <param name="label">The field's display label.</param>
        /// <param name="value">The current plant identifier.</param>
        /// <param name="maximum">The maximum supported identifier for this field.</param>
        /// <returns>The selected supported plant identifier.</returns>
        private static PlantKind Plant(string label, PlantKind value, int maximum = 48)
        {
            var names = s_plantNames;
            if (maximum != names.Length - 1)
            {
                names = new string[maximum + 1];
                Array.Copy(s_plantNames, names, names.Length);
            }

            return (PlantKind)EditorGUILayout.Popup(label, Mathf.Clamp((int)value, 0, maximum), names);
        }

        /// <summary>Finds a free garden position and creates a new plant with its own stable identifier.</summary>
        /// <param name="account">The detached account that receives the plant.</param>
        /// <param name="selection">The plant index to select after insertion.</param>
        private static void AddPlant(PlayerAccount account, ref int selection)
        {
            for (var garden = 0; garden < 3; garden++)
            {
                var capacity = garden == 0 ? 32 : 8;
                for (var slot = 0; slot < capacity; slot++)
                {
                    var occupied = false;
                    foreach (var plant in account._garden)
                    {
                        occupied |= (int)plant.Garden == garden && plant.Slot == slot;
                    }

                    if (!occupied)
                    {
                        account._garden.Add(new GardenPlant
                        {
                            Id = Guid.NewGuid(),
                            Kind = garden == 1 ? PlantKind.PuffShroom : garden == 2 ? PlantKind.LilyPad : PlantKind.Marigold,
                            Garden = (GardenKind)garden,
                            Slot = slot
                        });
                        selection = account._garden.Count - 1;
                        GUI.changed = true;
                        return;
                    }
                }
            }

            EditorUtility.DisplayDialog("禅境花园", "全部花园位置均已占用。", "确定");
        }

        /// <summary>Checks whether a field label contains the optional filter.</summary>
        /// <param name="label">The candidate field label.</param>
        /// <param name="search">The optional case-insensitive filter.</param>
        /// <returns>True when the label should be shown.</returns>
        private static bool Matches(string label, string search)
        {
            return label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Creates stable popup labels for all serialized plant identifiers.</summary>
        /// <returns>Labels indexed by original plant identifier.</returns>
        private static string[] BuildPlantNames()
        {
            var names = new string[49];
            for (var index = 0; index < names.Length; index++)
            {
                names[index] = "[" + index + "] " + (PlantKind)index;
            }

            return names;
        }
    }
}
