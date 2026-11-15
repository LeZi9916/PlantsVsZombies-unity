#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Selects plant behavior explicitly from stable native identifiers.</summary>
    internal static class PlantBehaviorFactory
    {
        /// <summary>Creates state ownership for one plant.</summary>
        /// <param name="kind">The original plant kind.</param>
        /// <returns>The matching behavior, including inert support plants.</returns>
        internal static IPlantBehavior Create(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Peashooter:
                case PlantKind.SnowPea:
                case PlantKind.Repeater:
                case PlantKind.PuffShroom:
                case PlantKind.SeaShroom:
                    return new ShooterPlantBehavior();
                case PlantKind.FumeShroom:
                case PlantKind.CabbagePult:
                case PlantKind.KernelPult:
                case PlantKind.MelonPult:
                case PlantKind.WinterMelon:
                    return new TimedPlantBehavior();
                case PlantKind.Sunflower:
                    return new SunflowerPlantBehavior();
                case PlantKind.SunShroom:
                    return new SunShroomPlantBehavior();
                case PlantKind.CoffeeBean:
                    return new CoffeeBeanPlantBehavior();
                case PlantKind.MagnetShroom:
                    return new MagnetShroomPlantBehavior();
                case PlantKind.Torchwood:
                    return new TorchwoodPlantBehavior();
                case PlantKind.Squash:
                    return new SquashPlantBehavior();
                case PlantKind.Blover:
                    return new BloverPlantBehavior();
                case PlantKind.DoomShroom:
                    return new DoomShroomPlantBehavior();
                case PlantKind.TangleKelp:
                    return new TangleKelpPlantBehavior();
                case PlantKind.Chomper:
                    return new ChomperPlantBehavior();
                case PlantKind.CherryBomb:
                    return new CherryBombPlantBehavior();
                case PlantKind.Jalapeno:
                    return new JalapenoPlantBehavior();
                case PlantKind.PotatoMine:
                    return new PotatoMinePlantBehavior();
                case PlantKind.Starfruit:
                    return new StarfruitPlantBehavior();
                case PlantKind.Cactus:
                    return new CactusPlantBehavior();
                case PlantKind.Threepeater:
                    return new ThreepeaterPlantBehavior();
                case PlantKind.SplitPea:
                    return new SplitPeaPlantBehavior();
                case PlantKind.TwinSunflower:
                case PlantKind.GatlingPea:
                case PlantKind.GloomShroom:
                case PlantKind.Cattail:
                case PlantKind.GoldMagnet:
                case PlantKind.Spikerock:
                case PlantKind.CobCannon:
                case PlantKind.Imitater:
                    return new UpgradePlantBehavior();
                case PlantKind.GraveBuster:
                    return new GraveBusterPlantBehavior();
                case PlantKind.Marigold:
                    return new MarigoldPlantBehavior();
                case PlantKind.IceShroom:
                    return new IceShroomPlantBehavior();
                case PlantKind.Spikeweed:
                    return new SpikeweedPlantBehavior();
                case PlantKind.ScaredyShroom:
                    return new ScaredyPlantBehavior();
                default:
                    return new PlantBehavior();
            }
        }
    }
}
