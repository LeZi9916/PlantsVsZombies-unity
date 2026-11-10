#nullable enable
using PlantsVsZombies.Simulation;
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>
    /// Local PC EXE 0x46DE18..0x46DE4A raises +0xB8 before emitting the item; 0x473264..0x473275
    /// then decrements it, and 0x471C3A..0x471C8F maps the surviving count to an additive source-image color.
    /// This helper neither advances production nor creates items, particles, animations or random draws.
    /// </summary>
    public static class AdventureProducerGlow
    {
        /// <summary>Stores lead ticks.</summary>
        public const int LeadTicks = 100;
        /// <summary>Stores brightness per tick.</summary>
        public const int BrightnessPerTick = 3;
        /// <summary>Stores normal brightness limit.</summary>
        public const int NormalBrightnessLimit = 255;
        /// <summary>Stores imitater brightness limit.</summary>
        public const int ImitaterBrightnessLimit = 128;
        /// <summary>Performs has native production hint.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting has native production hint.</returns>
        public static bool HasNativeProductionHint(PlantKind kind)
        {
            return kind == PlantKind.Sunflower || kind == PlantKind.TwinSunflower || kind == PlantKind.SunShroom || kind == PlantKind.Marigold;
        }

        /// <summary>
        /// Run once at the end of a real 100 Hz plant update, including early-continue paths.
        /// Pass the exact counter after the production decrement. On an actual emitted-item tick,
        /// produced=true recovers the zero crossing even when the producer already reset that counter.
        /// productionAdvanced must reflect the real production gates; sleeping/growing/mode-blocked plants
        /// pass false. animationAdvanced is independent: the native animation pass decays a residual hint
        /// even while production is suspended, whereas a paused board does not run this method at all.
        /// </summary>
        public static void Tick(Plant plant, bool productionAdvanced, int nativeLaunchCounterAfterTick, bool produced, bool animationAdvanced = true)
        {
            if (plant == null)
            {
                throw new ArgumentNullException(nameof(plant));
            }

            // A sleeping plant cannot enter the production function, but its existing counter still decays.
            plant.ProducerGlow = Step(plant.Kind, plant.ProducerGlow, productionAdvanced && !plant.Asleep, nativeLaunchCounterAfterTick, produced, animationAdvanced);
        }

        /// <summary>Performs step.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "previous">The previous.</param>
        /// <param name = "productionAdvanced">The production advanced.</param>
        /// <param name = "nativeLaunchCounterAfterTick">The native launch counter after tick.</param>
        /// <param name = "produced">The produced.</param>
        /// <param name = "animationAdvanced">The animation advanced.</param>
        /// <returns>The resulting step.</returns>
        public static AdventureProducerGlowState Step(PlantKind kind, AdventureProducerGlowState previous, bool productionAdvanced, int nativeLaunchCounterAfterTick, bool produced, bool animationAdvanced = true)
        {
            if (!previous.Initialized || previous.Kind != kind)
            {
                previous = new AdventureProducerGlowState
                {
                    Kind = kind,
                    Initialized = true
                };
            }

            if (!HasNativeProductionHint(kind))
            {
                return new AdventureProducerGlowState
                {
                    Kind = kind,
                    Initialized = true
                };
            }

            previous.ProductionAdvanced = productionAdvanced;
            previous.ProducedThisTick = productionAdvanced && produced;
            previous.LaunchCounterAfterTick = nativeLaunchCounterAfterTick;
            if (productionAdvanced)
            {
                // +0x58 is reset before the caller observes a successful production tick. Its glow test
                // preceded that reset, at the zero crossing: both Twin suns share this one pulse.
                var counter = produced ? 0 : nativeLaunchCounterAfterTick;
                if (counter <= LeadTicks)
                {
                    var pulse = counter <= 0 ? LeadTicks : LeadTicks - counter;
                    previous.FlashTicks = Math.Max(previous.FlashTicks, pulse);
                }
            }

            if (animationAdvanced && previous.FlashTicks > 0)
            {
                previous.FlashTicks--;
            }

            return previous;
        }

        /// <summary>Performs visual.</summary>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting visual.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static AdventureProducerGlowVisual Visual(Plant plant)
        {
            if (plant == null)
            {
                throw new ArgumentNullException(nameof(plant));
            }

            var state = plant.ProducerGlow;
            if (!state.Initialized || state.Kind != plant.Kind || !HasNativeProductionHint(plant.Kind))
            {
                return new AdventureProducerGlowVisual(false, 0, 0);
            }

            return Visual(state, plant.IsImitated);
        }

        /// <summary>Performs visual.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "imitated">The imitated.</param>
        /// <returns>The resulting visual.</returns>
        public static AdventureProducerGlowVisual Visual(AdventureProducerGlowState state, bool imitated)
        {
            var ticks = Math.Max(0, state.FlashTicks);
            if (!state.Initialized || !HasNativeProductionHint(state.Kind) || ticks == 0)
            {
                return new AdventureProducerGlowVisual(false, 0, 0);
            }

            var limit = imitated ? ImitaterBrightnessLimit : NormalBrightnessLimit;
            var brightness = (int)Math.Min(limit, (long)ticks * BrightnessPerTick);
            return new AdventureProducerGlowVisual(true, ticks, (byte)brightness);
        }

        /// <summary>Only for existing producers whose authoritative countdown is stored in ActionTimer seconds.</summary>
        public static int CounterFromSeconds(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            var ticks = Math.Round(seconds * 100.0, MidpointRounding.AwayFromZero);
            return ticks <= int.MinValue ? int.MinValue : ticks >= int.MaxValue ? int.MaxValue : (int)ticks;
        }

        /// <summary>Performs reset.</summary>
        /// <param name = "plant">The plant.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static void Reset(Plant plant)
        {
            if (plant == null)
            {
                throw new ArgumentNullException(nameof(plant));
            }

            plant.ProducerGlow = new AdventureProducerGlowState
            {
                Kind = plant.Kind,
                Initialized = true
            };
        }
    }
}
