#nullable enable
using System;
using System.Globalization;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Selects the original adventure opening dialogue and advances its numbered entries.</summary>
    public static class CrazyDaveDialogue
    {
        /// <summary>Original CutScene::StartLevelIntro and LawnApp::AdvanceCrazyDaveText.</summary>
        public const string Evidence = "PC 0x43A900 and 0x453D20";

        /// <summary>Formats a displayed money amount using the original dialogue convention.</summary>
        /// <param name="amount">The displayed amount, after converting coins to dollars.</param>
        /// <returns>The amount with the original currency symbol and grouping.</returns>
        public static string FormatMoney(long amount)
        {
            return "$" + amount.ToString(amount > 9999 ? "N0" : "0", CultureInfo.InvariantCulture);
        }

        /// <summary>Selects the original idle mouth override requested by a dialogue entry.</summary>
        /// <param name="message">The original message including its control markers.</param>
        /// <returns>The original reanimation image identifier, or null when no mouth is specified.</returns>
        /// <exception cref="ArgumentNullException">The message is null.</exception>
        public static string? IdleMouthImage(string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.IndexOf("{MOUTH_BIG_SMILE}", StringComparison.Ordinal) >= 0)
            {
                return "IMAGE_REANIM_CRAZYDAVE_MOUTH1";
            }

            if (message.IndexOf("{MOUTH_SMALL_SMILE}", StringComparison.Ordinal) >= 0)
            {
                return "IMAGE_REANIM_CRAZYDAVE_MOUTH5";
            }

            if (message.IndexOf("{MOUTH_BIG_OH}", StringComparison.Ordinal) >= 0)
            {
                return "IMAGE_REANIM_CRAZYDAVE_MOUTH4";
            }

            if (message.IndexOf("{MOUTH_SMALL_OH}", StringComparison.Ordinal) >= 0)
            {
                return "IMAGE_REANIM_CRAZYDAVE_MOUTH6";
            }

            return null;
        }

        /// <summary>Selects the original first dialogue entry for an adventure opening.</summary>
        /// <param name="level">The one-based adventure level.</param>
        /// <param name="firstAdventure">Whether the player has not finished adventure.</param>
        /// <param name="mode">The adventure level's special mode.</param>
        /// <param name="firstPacketUpgradeAvailable">Whether the original first slot upgrade conditions are met.</param>
        /// <param name="secondPacketUpgradeAvailable">Whether the original second slot upgrade conditions are met.</param>
        /// <param name="isRestart">Whether the previous board was lost or restarted.</param>
        /// <returns>The first original text index, or -1 if there is no opening dialogue.</returns>
        public static int SelectAdventureStart(int level, bool firstAdventure, AdventureMode mode, bool firstPacketUpgradeAvailable, bool secondPacketUpgradeAvailable, bool isRestart)
        {
            if (firstAdventure && level == 11)
            {
                return 201;
            }

            if (firstAdventure && level == 12)
            {
                return 1401;
            }

            if (firstAdventure && level >= 13 && level <= 24 && level != 15 && level != 20 && level != 21 && firstPacketUpgradeAvailable)
            {
                return 1501;
            }

            if (firstAdventure && level >= 16 && level <= 24 && level != 20 && level != 21 && secondPacketUpgradeAvailable)
            {
                return 1551;
            }

            if (mode == AdventureMode.Bowling)
            {
                return firstAdventure ? 2400 : 2411;
            }

            if (firstAdventure && level == 21)
            {
                return 501;
            }

            if (mode == AdventureMode.WhackAZombie)
            {
                return 401;
            }

            if (mode == AdventureMode.LittleTrouble)
            {
                return 701;
            }

            if (firstAdventure && level == 31)
            {
                return 801;
            }

            if (mode == AdventureMode.Vasebreaker)
            {
                return 2500;
            }

            if (mode == AdventureMode.Storm)
            {
                return 1101;
            }

            if (firstAdventure && level == 41)
            {
                return 1201;
            }

            if (mode == AdventureMode.Bungee)
            {
                return firstAdventure ? 1301 : 1304;
            }

            if (!firstAdventure && level == 1)
            {
                return 1601;
            }

            if (mode == AdventureMode.Boss && !isRestart)
            {
                return 2300;
            }

            return -1;
        }

        /// <summary>Determines whether an opening has implemented its required interactions.</summary>
        /// <param name="start">The selected original starting message index.</param>
        /// <returns>Whether the opening is supported by the current coordinator.</returns>
        public static bool IsSupportedAdventureOpening(int start)
        {
            return start == 201 || start == 1401 || start == 1501 || start == 1551 || start == 401 || start == 501 || start == 701 || start == 801
                || start == 1101 || start == 1201 || start == 1301 || start == 1304 || start == 1601 || start == 2300 || start == 2400 || start == 2411 || start == 2500;
        }

        /// <summary>Advances to the next numbered entry only if it exists in the original string table.</summary>
        /// <param name="current">The index of the dialogue currently displayed.</param>
        /// <param name="exists">Tests whether a numbered original entry exists.</param>
        /// <returns>The next entry index, or -1 when the dialogue has ended.</returns>
        /// <exception cref="ArgumentNullException">The entry lookup is null.</exception>
        public static int NextMessageIndex(int current, Func<int, bool> exists)
        {
            if (exists == null)
            {
                throw new ArgumentNullException(nameof(exists));
            }

            if (current < 0 || current == int.MaxValue)
            {
                return -1;
            }

            var next = current + 1;
            return exists(next) ? next : -1;
        }
    }
}
