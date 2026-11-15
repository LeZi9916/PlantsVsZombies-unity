#nullable enable
using PlantsVsZombies.Progression;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace PlantsVsZombies.Modes
{
    /// <summary>Combines persistent mode access overrides with the game's normal category and trophy unlock rules.</summary>
    public static class ModeAccessRules
    {
        /// <summary>Caches one receipt key per supported mode so access checks do not allocate key strings.</summary>
        private static readonly Dictionary<int, string> s_overrideKeys = BuildOverrideKeys();

        /// <summary>Identifies the bounded set of persistent access receipts excluded from transient reward trimming.</summary>
        private static readonly HashSet<string> s_overrideReceipts = new HashSet<string>(s_overrideKeys.Values, StringComparer.Ordinal);

        /// <summary>Reads a mode's explicit access override without changing scores or progression.</summary>
        /// <param name="account">The player account containing persisted mode access receipts.</param>
        /// <param name="gameModeId">The original identifier of a supported mode.</param>
        /// <returns>True for explicitly unlocked, false for explicitly locked, or null to use normal unlock rules.</returns>
        /// <exception cref="ArgumentNullException">The account is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The mode identifier is not supported by the local mode catalog.</exception>
        public static bool? GetOverride(PlayerAccount account, int gameModeId)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            var key = GetOverrideKey(gameModeId);
            if (account._receipts.TryGetValue(key, out var value))
            {
                if (value == 1)
                {
                    return true;
                }

                if (value == 2)
                {
                    return false;
                }
            }

            return null;
        }

        /// <summary>Checks an explicit override before applying the unchanged category gifts and trophy requirements.</summary>
        /// <param name="gameModeId">The original identifier of a supported mode.</param>
        /// <param name="account">The player's category gifts, mode records and explicit access overrides.</param>
        /// <param name="nextAdventureLevel">The player's next adventure level.</param>
        /// <param name="finishedAdventure">Whether the player has completed at least one adventure run.</param>
        /// <returns>The reason the mode is locked, or null when the mode can be opened.</returns>
        /// <exception cref="ArgumentNullException">The account is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The mode identifier is not supported by the local mode catalog.</exception>
        public static string? LockReason(int gameModeId, PlayerAccount account, int nextAdventureLevel, bool finishedAdventure)
        {
            var access = GetOverride(account, gameModeId);
            if (access.HasValue)
            {
                return access.Value ? null : "此挑战已锁定。";
            }

            var giftAccess = gameModeId <= 15 ? account.SurvivalUnlocked : gameModeId <= 35 ? account.MinigamesUnlocked : account.PuzzleUnlocked;
            if (!giftAccess && !ModeUnlockRules.CategoryUnlocked(gameModeId, nextAdventureLevel, finishedAdventure))
            {
                return gameModeId <= 15 ? "通关冒险模式后解锁生存模式。" : gameModeId <= 35 ? "通过冒险模式3-2，领取小游戏礼物后解锁。" : "通过冒险模式4-6，领取解谜模式礼物后解锁。";
            }

            return ModeUnlockRules.LockReason(gameModeId, finishedAdventure, account.ModeRecord);
        }

#if UNITY_EDITOR
        /// <summary>Edits one detached mode access override and opens its category when explicitly unlocking it.</summary>
        /// <param name="account">The detached account draft to edit; no file is written.</param>
        /// <param name="gameModeId">The original identifier of a supported mode.</param>
        /// <param name="unlocked">True to force access, false to force a lock, or null to restore normal rules.</param>
        /// <exception cref="ArgumentNullException">The account is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The mode identifier is not supported by the local mode catalog.</exception>
        internal static void SetOverride(PlayerAccount account, int gameModeId, bool? unlocked)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            var key = GetOverrideKey(gameModeId);
            if (!unlocked.HasValue)
            {
                account._receipts.Remove(key);
                return;
            }

            account._receipts[key] = unlocked.Value ? 1 : 2;
            if (unlocked.Value)
            {
                if (gameModeId <= 15)
                {
                    account.SurvivalUnlocked = true;
                }
                else if (gameModeId <= 35)
                {
                    account.MinigamesUnlocked = true;
                }
                else
                {
                    account.PuzzleUnlocked = true;
                }
            }

            PlayerAccountStore.TrimReceipts(account);
        }

        /// <summary>Unlocks every supported mode and category without modifying scores, coins or adventure progress.</summary>
        /// <param name="account">The detached account draft to edit; no file is written.</param>
        /// <exception cref="ArgumentNullException">The account is null.</exception>
        internal static void UnlockAll(PlayerAccount account)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            foreach (var key in s_overrideKeys.Values)
            {
                account._receipts[key] = 1;
            }

            account.MinigamesUnlocked = true;
            account.PuzzleUnlocked = true;
            account.SurvivalUnlocked = true;
            PlayerAccountStore.TrimReceipts(account);
        }
#endif

        /// <summary>Identifies supported persistent mode overrides that normal reward bookkeeping must preserve.</summary>
        /// <param name="key">The receipt key to classify.</param>
        /// <returns>True only for the cached access receipt of a mode in the local catalog.</returns>
        internal static bool IsOverrideReceipt(string key)
        {
            return s_overrideReceipts.Contains(key);
        }

        /// <summary>Finds a supported mode's cached access receipt key.</summary>
        /// <param name="gameModeId">The original mode identifier to resolve.</param>
        /// <returns>The already allocated receipt key for this mode.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The mode identifier is absent from the local catalog.</exception>
        private static string GetOverrideKey(int gameModeId)
        {
            if (!s_overrideKeys.TryGetValue(gameModeId, out var key))
            {
                throw new ArgumentOutOfRangeException(nameof(gameModeId), gameModeId, "The mode identifier must belong to the local mode catalog.");
            }

            return key;
        }

        /// <summary>Allocates stable receipt keys once for every currently supported mode.</summary>
        /// <returns>The catalog mode identifiers and their invariant receipt keys.</returns>
        private static Dictionary<int, string> BuildOverrideKeys()
        {
            var keys = new Dictionary<int, string>();
            foreach (var mode in ModeCatalog.All)
            {
                keys.Add(mode.Id, "mode:unlock:" + mode.Id.ToString(CultureInfo.InvariantCulture));
            }

            return keys;
        }
    }
}
