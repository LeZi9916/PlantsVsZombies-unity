#nullable enable

using System;
using System.Collections.Generic;
using PlantsVsZombies.Simulation;
using UnityEngine;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Preserves PC Plant::DrawShadow (0x473AD0) species exclusions, offsets, and scales.</summary>
    internal static class AdventurePlantShadowRules
    {
        /// <summary>Checks whether the original plant draws a separate ground shadow.</summary>
        /// <param name="plant">The plant whose native shadow is being submitted.</param>
        /// <param name="plants">The board plants used to find a plant or pumpkin below flying coffee.</param>
        /// <returns>Whether the original separate shadow image should be drawn.</returns>
        internal static bool HasShadow(Plant plant, IReadOnlyList<Plant> plants)
        {
            switch (plant.Kind)
            {
                case PlantKind.LilyPad:
                case PlantKind.Starfruit:
                case PlantKind.TangleKelp:
                case PlantKind.SeaShroom:
                case PlantKind.CobCannon:
                case PlantKind.Spikeweed:
                case PlantKind.Spikerock:
                case PlantKind.GraveBuster:
                case PlantKind.Cattail:
                    return false;
            }

            if (plant.Kind == PlantKind.CoffeeBean)
            {
                for (var index = 0; index < plants.Count; index++)
                {
                    var other = plants[index];
                    if (other != plant && other.Row == plant.Row && other.Column == plant.Column
                        && (other.Layer == PlantLayer.Main || other.Layer == PlantLayer.Armor))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Gets PC PlantDrawHeightOffset (0x473490) for species with a separate shadow.</summary>
        /// <param name="plant">The plant whose shadow is being positioned.</param>
        /// <param name="plants">The board plants used to find a supporting flower pot.</param>
        /// <param name="inPool">Whether the plant occupies an original water cell.</param>
        /// <param name="ticks">The board's completed native updates, used for water bobbing.</param>
        /// <returns>The PC height offset, without the mobile reference's additional roof displacement.</returns>
        internal static float DrawHeight(Plant plant, IReadOnlyList<Plant> plants, bool inPool, int ticks)
        {
            var height = inPool && plant.Kind != PlantKind.CoffeeBean
                ? (float)Math.Sin(plant.Row * Math.PI + plant.Column * Math.PI * .25f + ticks * Math.PI * 2f / 200f) * 2f
                : 0f;
            for (var index = 0; index < plants.Count; index++)
            {
                var support = plants[index];
                if (plant.Kind != PlantKind.FlowerPot && support.Kind == PlantKind.FlowerPot
                    && support.Row == plant.Row && support.Column == plant.Column)
                {
                    // PC 0x473340 is called with scale 1: the extra scale corrections vanish.
                    height += plant.Kind == PlantKind.ScaredyShroom ? 0f
                        : plant.Kind == PlantKind.Chomper || plant.Kind == PlantKind.Plantern ? -10f : -5f;
                    break;
                }
            }

            switch (plant.Kind)
            {
                case PlantKind.PuffShroom:
                    return height + 5f;
                case PlantKind.ScaredyShroom:
                    return height - 14f;
                case PlantKind.FlowerPot:
                    return height + 26f;
                case PlantKind.Pumpkin:
                    return height + 15f;
                case PlantKind.CoffeeBean:
                    return height - 20f;
                default:
                    return height;
            }
        }

        /// <summary>Places the original image using its unscaled center before applying the species scale.</summary>
        /// <param name="plant">The species and native Sun-shroom growth state being drawn.</param>
        /// <param name="cellOrigin">The plant's board cell origin including its roof slope.</param>
        /// <param name="drawHeight">The original plant draw-height offset, including a supporting flower pot.</param>
        /// <param name="imageSize">The unscaled dimensions of the terrain's shadow image.</param>
        /// <param name="growthProgress">The clamped Sun-shroom grow animation phase, from zero to one.</param>
        /// <returns>The original shadow image bounds in board pixels.</returns>
        internal static Rect Bounds(Plant plant, Vector2 cellOrigin, float drawHeight, Vector2 imageSize, float growthProgress)
        {
            var offset = new Vector2(-3f, 51f);
            var scale = 1f;
            switch (plant.Kind)
            {
                case PlantKind.Squash:
                    offset.y = 56f;
                    break;
                case PlantKind.PuffShroom:
                    offset.y = 42f;
                    scale = .5f;
                    break;
                case PlantKind.SunShroom:
                    offset.y = 42f;
                    scale = .5f + .5f * Mathf.Clamp01(growthProgress);
                    break;
                case PlantKind.UmbrellaLeaf:
                    offset = new Vector2(-7f, 52f);
                    scale = .5f;
                    break;
                case PlantKind.FumeShroom:
                case PlantKind.GloomShroom:
                    offset.y = 47f;
                    scale = 1.3f;
                    break;
                case PlantKind.CabbagePult:
                case PlantKind.MelonPult:
                case PlantKind.WinterMelon:
                    offset.y = 47f;
                    break;
                case PlantKind.KernelPult:
                    offset = new Vector2(0f, 47f);
                    break;
                case PlantKind.ScaredyShroom:
                    offset = new Vector2(-9f, 55f);
                    break;
                case PlantKind.Chomper:
                    offset = new Vector2(-21f, 57f);
                    break;
                case PlantKind.FlowerPot:
                    offset = new Vector2(-4f, 46f);
                    break;
                case PlantKind.TallNut:
                    offset.y = 54f;
                    scale = 1.3f;
                    break;
                case PlantKind.Pumpkin:
                    offset.y = 46f;
                    scale = 1.4f;
                    break;
                case PlantKind.Cactus:
                    offset = new Vector2(-8f, 50f);
                    break;
                case PlantKind.Plantern:
                    offset.y = 57f;
                    break;
                case PlantKind.CoffeeBean:
                    offset.y = 81f;
                    break;
            }

            // PC 0x527D20 adds the unscaled half-image before scaling. Plant::DrawShadow
            // does not apply the small-plant body scale to this separate ground image.
            var size = imageSize * scale;
            var center = cellOrigin + new Vector2(0f, drawHeight) + offset + imageSize * .5f;
            return new Rect(center - size * .5f, size);
        }
    }
}
