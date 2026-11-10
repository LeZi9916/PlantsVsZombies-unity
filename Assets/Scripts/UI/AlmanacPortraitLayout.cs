#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>Source-pixel coordinates from the local EXE's almanac and seed-packet draw paths.</summary>
    public static class AlmanacPortraitLayout
    {
        /// <summary>Stores zombie preview clip.</summary>
        public static readonly Rect ZombiePreviewClip = new Rect(517, 124, 197, 187);
        /// <summary>Performs plant preview origin.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting plant preview origin.</returns>
        public static Vector2 PlantPreviewOrigin(int id)
        {
            Validate(id, 49);
            var x = id == 47 ? 538f : 578f;
            var y = id == 23 ? 158f : id == 33 ? 120f : id == 35 ? 160f : id == 11 ? 195f : 140f;
            return new Vector2(x, y + PlantHeightOffset(id));
        }

        // Board-null, row/column zero, stable pose. Aquatic bobbing is handled by the host.
        /// <summary>Performs plant height offset.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting plant height offset.</returns>
        public static float PlantHeightOffset(int id)
        {
            Validate(id, 49);
            switch (id)
            {
                case 33:
                    return 26;
                case 16:
                    return 25;
                case 29:
                    return 10;
                case 19:
                    return 24;
                case 24:
                    return 28;
                case 35:
                    return -20;
                case 30:
                    return 15;
                case 8:
                    return 5;
                case 13:
                    return -14;
                case 11:
                    return -40;
                case 21:
                    return 15;
                case 46:
                    return 21;
                default:
                    return 0;
            }
        }

        /// <summary>Performs zombie preview origin.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting zombie preview origin.</returns>
        public static Vector2 ZombiePreviewOrigin(int id)
        {
            Validate(id, 26);
            float x = 559, y = 175;
            switch (id)
            {
                case 12:
                    x -= 30;
                    y += 5;
                    break;
                case 23:
                    y += 30;
                    break;
                case 7:
                    x -= 10;
                    break;
                case 16:
                    y -= 20;
                    break;
                case 20:
                    x += 15;
                    break;
                case 22:
                    x -= 10;
                    break;
                case 25:
                    x -= 540;
                    y -= 175;
                    break;
            }

            return new Vector2(x, y);
        }

        /// <summary>Performs zombie thumbnail origin.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "slot">The slot.</param>
        /// <returns>The resulting zombie thumbnail origin.</returns>
        public static Vector2 ZombieThumbnailOrigin(int id, Rect slot)
        {
            Validate(id, 26);
            // Cache image draws a reanim at (40,40), then the index displays it at
            // half size against (slot.x+1, slot.y-6), with these native adjustments.
            float x = slot.x + 21, y = slot.y + 14;
            switch (id)
            {
                case 3:
                    x += 2;
                    y -= 3;
                    break;
                case 1:
                    x += 2;
                    y += 10;
                    break;
                case 2:
                    y += 12;
                    break;
                case 4:
                    y += 9;
                    break;
                case 7:
                    x -= 15;
                    y -= 1;
                    break;
                case 12:
                    y += 3;
                    break;
                case 14:
                    x -= 2;
                    y -= 10;
                    break;
                case 18:
                    y -= 3;
                    break;
                case 23:
                    x += 15;
                    y += 17;
                    break;
                case 24:
                    x -= 8;
                    y -= 7;
                    break;
                case 20:
                    x -= 4;
                    y += 3;
                    break;
                case 11:
                    x -= 10;
                    break;
                case 22:
                    x -= 24;
                    y -= 1;
                    break;
                case 8:
                    y += 15;
                    break;
                case 9:
                    x -= 5;
                    y += 20;
                    break;
                case 19:
                    y += 4;
                    break;
                case 13:
                    y -= 8;
                    break;
                case 21:
                    y -= 3;
                    break;
            }

            return new Vector2(x, y);
        }

        /// <summary>Performs plant atlas cell.</summary>
        /// <param name = "id">The id.</param>
        /// <returns>The resulting plant atlas cell.</returns>
        public static int PlantAtlasCell(int id)
        {
            Validate(id, 49);
            switch (id)
            {
                case 4:
                    return 0;
                case 6:
                    return 1;
                case 12:
                    return 2;
                case 23:
                    return 3;
                case 27:
                    return 4;
                case 30:
                    return 5;
                case 41:
                    return 6;
                case 47:
                    return 7;
                case 32:
                    return 8;
                case 34:
                    return 9;
                case 39:
                    return 10;
                case 44:
                    return 11;
                case 46:
                    return 12;
                default:
                    return -1;
            }
        }

        /// <summary>Performs plant thumbnail transform.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "slot">The slot.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        public static void PlantThumbnailTransform(int id, Rect slot, out Vector2 origin, out float scale)
        {
            Validate(id, 49);
            float x = 5, y = 8;
            scale = 0.5f;
            switch (id)
            {
                case 23:
                    x = 12;
                    y = 22;
                    scale = 0.3f;
                    break;
                case 35:
                    x = 0;
                    y = 9;
                    scale = 0.55f;
                    break;
                case 47:
                    x = 6;
                    y = 22;
                    scale = 0.26f;
                    break;
                case 26:
                    x = 9;
                    y = 13;
                    break;
                case 31:
                    y = 12;
                    break;
                case 10:
                case 30:
                case 6:
                case 15:
                case 17:
                case 12:
                case 21:
                case 46:
                case 25:
                case 22:
                case 19:
                case 4:
                    x = 8;
                    y = 12;
                    scale = 0.4f;
                    break;
                case 41:
                case 42:
                    x = 7;
                    y = 14;
                    scale = 0.45f;
                    break;
                case 43:
                    x = 8;
                    y = 13;
                    scale = 0.45f;
                    break;
                case 34:
                    x = 13;
                    y = 14;
                    scale = 0.4f;
                    break;
                case 32:
                    x = 15;
                    y = 14;
                    scale = 0.4f;
                    break;
                case 39:
                case 44:
                    x = 18;
                    y = 19;
                    scale = 0.35f;
                    break;
                case 11:
                    x = 10;
                    y = 15;
                    scale = 0.4f;
                    break;
                case 28:
                    x = 12;
                    y = 12;
                    scale = 0.45f;
                    break;
                case 27:
                    x = 8;
                    y = 17;
                    scale = 0.4f;
                    break;
                case 29:
                    x = 6;
                    y = 8;
                    break;
                case 18:
                case 37:
                    x = 5;
                    y = 10;
                    break;
                case 40:
                    x = 2;
                    y = 8;
                    break;
            }

            origin = new Vector2(slot.x + x, slot.y + y + 1f);
        }

        /// <summary>Performs validate.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "count">The count.</param>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        private static void Validate(int id, int count)
        {
            if (id < 0 || id >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
    }
}
