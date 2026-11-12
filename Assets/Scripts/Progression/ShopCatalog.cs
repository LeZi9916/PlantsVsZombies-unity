#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Progression
{
    /// <summary>Defines shop catalog.</summary>
    public static class ShopCatalog
    {
        /// <summary>Performs i.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "name">The name.</param>
        /// <param name = "price">The price.</param>
        /// <param name = "level">The level.</param>
        /// <param name = "after">The after.</param>
        /// <param name = "garden">The garden.</param>
        /// <param name = "consumable">The consumable.</param>
        /// <param name = "icon">The icon.</param>
        /// <returns>The resulting i.</returns>
        private static ShopItem I(int id, string name, int price, int level = 25, bool after = false, bool garden = false, bool consumable = false, string? icon = null)
        {
            return new ShopItem
            {
                Id = id,
                Name = name,
                PriceUnits = price,
                UnlockLevel = level,
                AfterAdventure = after,
                GardenItem = garden,
                Consumable = consumable,
                Icon = icon
            };
        }

        /// <summary>Stores items.</summary>
        public static readonly IReadOnlyList<ShopItem> Items = Array.AsReadOnly(new[] {
            I(0, "机枪射手", 500),
            I(1, "双子向日葵", 500),
            I(2, "忧郁菇", 750, 35),
            I(3, "香蒲", 1000, 35),
            I(4, "冰瓜", 1000, 51, true),
            I(5, "吸金磁", 300, 41),
            I(6, "地刺王", 750, 41),
            I(7, "玉米加农炮", 2000, 51, true),
            I(8, "模仿者", 3000, 51, true),
            I(21, "种子槽升级", 75, 25, false, false, false, "Store_PacketUpgrade.png"),
            I(22, "池塘清扫车", 100, 25, false, false, false, "icon_poolcleaner.png"),
            I(23, "屋顶清扫车", 300, 42, false, false, false, "icon_roofcleaner.png"),
            I(24, "草耙", 20, 25, false, false, true, "icon_rake.png"),
            I(29, "坚果包扎术", 200, 51, true, false, false, "Store_FirstAidWallnutIcon.png"),
            I(10, "金盏花幼苗", 250, 51, true, true, true),
            I(11, "金盏花幼苗", 250, 51, true, true, true),
            I(12, "金盏花幼苗", 250, 51, true, true, true),
            I(13, "黄金浇水壶", 1000, 45, false, true, false, "WateringCanGold.png"),
            I(14, "肥料（5份）", 75, 45, false, true, true, "Fertilizer.png"),
            I(15, "杀虫剂（5份）", 100, 45, false, true, true, "BugSpray.png"),
            I(16, "留声机", 1500, 45, false, true, false, "Phonograph.png"),
            I(17, "园艺手套", 100, 45, false, true, false, "Zen_GardenGlove.png"),
            I(18, "蘑菇园", 3000, 51, true, true, false, "Store_MushroomGardenIcon.png"),
            I(19, "手推车", 20, 51, true, true, false, "Zen_WheelBarrow.png"),
            I(20, "蜗牛", 300, 51, true, true, false, "Stinky.png"),
            I(25, "水族馆", 3000, 51, true, true, false, "Store_AquariumGardenIcon.png"),
            I(27, "智慧树", 1000, 51, true, true, false, "Store_TreeOfWisdomIcon.png"),
            I(28, "树肥（1份）", 250, 51, true, true, true, "TreeFood.png")
        });
        /// <summary>Performs get.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting get.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public static ShopItem Get(int id)
        {
            foreach (var item in Items)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            throw new ArgumentOutOfRangeException("id");
        }

        /// <summary>Performs price.</summary>
        /// <param name = "item">The item.</param>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting price.</returns>
        public static int Price(ShopItem item, PlayerAccount p)
        {
            if (item.Id != 21)
            {
                return item.PriceUnits;
            }

            int[] prices =
            {
                75,
                500,
                2000,
                8000
            };
            return prices[Math.Min(3, p.PurchaseCount(21))];
        }

        /// <summary>Performs is unlocked.</summary>
        /// <param name = "item">The item.</param>
        /// <param name = "nextLevel">The next level.</param>
        /// <param name = "completedRuns">The completed runs.</param>
        /// <returns>The resulting is unlocked.</returns>
        public static bool IsUnlocked(ShopItem item, int nextLevel, int completedRuns)
        {
            return completedRuns > 0 || !item.AfterAdventure && nextLevel >= item.UnlockLevel;
        }

        /// <summary>Performs sold out.</summary>
        /// <param name = "item">The item.</param>
        /// <param name = "p">The p.</param>
        /// <param name = "utc">The utc.</param>
        /// <returns>The resulting sold out.</returns>
        public static bool SoldOut(ShopItem item, PlayerAccount? p, long utc)
        {
            if (item.Id == 21)
            {
                if (p == null)
                {
                    throw new System.ArgumentNullException(nameof(p));
                }

                return p.PurchaseCount(21) >= 4;
            }

            if (item.Id >= 10 && item.Id <= 12)
            {
                if (p == null)
                {
                    throw new System.ArgumentNullException(nameof(p));
                }

                return p.PurchaseCount(item.Id) == (int)(utc / 86400) || p.Plants.Count >= 32;
            }

            if (item.Id == 14 || item.Id == 15)
            {
                if (p == null)
                {
                    throw new System.ArgumentNullException(nameof(p));
                }

                return p.PurchaseCount(item.Id) > 15;
            }

            if (item.Id == 24)
            {
                if (p == null)
                {
                    throw new System.ArgumentNullException(nameof(p));
                }

                return p.PurchaseCount(24) > 0;
            }

            if (item.Id == 28)
            {
                if (p == null)
                {
                    throw new System.ArgumentNullException(nameof(p));
                }

                return p.PurchaseCount(28) >= 10;
            }

            if (item.Consumable)
            {
                return false;
            }

            if (p == null)
            {
                throw new System.ArgumentNullException(nameof(p));
            }

            return p.PurchaseCount(item.Id) > 0;
        }
    }
}
