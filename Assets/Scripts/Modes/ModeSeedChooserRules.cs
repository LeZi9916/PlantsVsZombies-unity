#nullable enable
using System.Collections.Generic;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Modes
{
    /// <summary>Preserves PC 49A300/49B120/49AF50 independent-mode seed restrictions and confirmations.</summary>
    internal static class ModeSeedChooserRules
    {
        /// <summary>Tests the five original Last Stand exclusions, including zero-cost mushrooms.</summary>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="seed">The ordinary seed or Imitater template.</param>
        /// <returns>Whether the original chooser prevents picking this seed.</returns>
        internal static bool IsDisabled(int mode, PlantKind seed)
        {
            return mode == 31 && (seed == PlantKind.Sunflower || seed == PlantKind.SunShroom || seed == PlantKind.TwinSunflower
                || seed == PlantKind.PuffShroom || seed == PlantKind.SeaShroom);
        }

        /// <summary>Builds PC OnStartButton's ordered original confirmations from the settled bank.</summary>
        /// <param name="session">The actual challenge and retained lawn.</param>
        /// <param name="selection">The settled original seed chooser state.</param>
        /// <returns>The warnings to confirm before handing the bank to the board.</returns>
        internal static List<ModeChooserWarning> Warnings(ModeSession session, AdventureSeedChooserState selection)
        {
            var warnings = new List<ModeChooserWarning>();
            var board = session.Game.Adventure;
            var repick = session.Definition.Survival && session.Stage > 1;
            bool Picked(PlantKind kind)
            {
                return selection.PickedPlantType(kind, board.ImitaterTarget);
            }
            if (session.Definition.Id == 22 && !Picked(PlantKind.Starfruit))
            {
                warnings.Add(new ModeChooserWarning("SEED_CHOOSER_SEEING_STARS_WARNING"));
            }
            if (!repick && session.Definition.Id != 31 && !Picked(PlantKind.Sunflower) && !Picked(PlantKind.TwinSunflower) && !Picked(PlantKind.SunShroom))
            {
                warnings.Add(new ModeChooserWarning("SEED_CHOOSER_SUN_WARNING"));
            }
            if (board.Level.HasPool && !repick && !Picked(PlantKind.LilyPad) && !Picked(PlantKind.SeaShroom) && !Picked(PlantKind.TangleKelp))
            {
                warnings.Add(new ModeChooserWarning("SEED_CHOOSER_POOL_WARNING"));
            }
            if (board.Level.HasRoof && !Picked(PlantKind.FlowerPot) && session.UnlockedBasicSeeds > (int)PlantKind.FlowerPot)
            {
                warnings.Add(new ModeChooserWarning("SEED_CHOOSER_ROOF_WARNING"));
            }
            var flyers = false;
            foreach (var wave in board.Waves)
            {
                foreach (var zombie in wave.Zombies)
                {
                    flyers |= zombie == ZombieKind.Balloon;
                }
            }
            if (flyers && !session.Game.Plants.Exists(plant => plant.Kind == PlantKind.Cactus || plant.Kind == PlantKind.Cattail)
                && !Picked(PlantKind.Cattail) && !Picked(PlantKind.Cactus) && !Picked(PlantKind.Blover))
            {
                warnings.Add(new ModeChooserWarning("SEED_CHOOSER_FLYER_WARNING"));
            }
            if (!session.Definition.Survival)
            {
                Upgrade(PlantKind.GatlingPea, PlantKind.Repeater);
                Upgrade(PlantKind.WinterMelon, PlantKind.MelonPult);
                Upgrade(PlantKind.TwinSunflower, PlantKind.Sunflower);
                Upgrade(PlantKind.Spikerock, PlantKind.Spikeweed);
                Upgrade(PlantKind.CobCannon, PlantKind.KernelPult);
                Upgrade(PlantKind.GoldMagnet, PlantKind.MagnetShroom);
                Upgrade(PlantKind.GloomShroom, PlantKind.FumeShroom);
                Upgrade(PlantKind.Cattail, PlantKind.LilyPad);
            }
            return warnings;

            void Upgrade(PlantKind kind, PlantKind basis)
            {
                if (Picked(kind) && !Picked(basis))
                {
                    warnings.Add(new ModeChooserWarning("SEED_CHOOSER_UPGRADE_WARNING", kind, basis));
                }
            }
        }
    }
}
