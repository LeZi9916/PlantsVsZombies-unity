#nullable enable
using PlantsVsZombies.Simulation;
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure plant hit rules.</summary>
    public static class AdventurePlantHitRules
    {
        /// <summary>Performs brightness.</summary>
        /// <param name = "ticks">The ticks.</param>
        /// <param name = "imitated">The imitated.</param>
        /// <returns>The resulting brightness.</returns>
        public static int Brightness(int ticks, bool imitated)
        {
            return Math.Min(imitated ? 128 : 255, Math.Max(0, ticks) * 3);
        }

        /// <summary>Performs combined.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting combined.</returns>
        public static AdventureProducerGlowVisual Combined(Plant p)
        {
            var production = AdventureProducerGlow.Visual(p);
            var ticks = Math.Max(p.PresentationEatenFlashTicks, production.FlashTicks);
            return new AdventureProducerGlowVisual(ticks > 0, ticks, (byte)Brightness(ticks, p.IsImitated));
        }
    }
}
