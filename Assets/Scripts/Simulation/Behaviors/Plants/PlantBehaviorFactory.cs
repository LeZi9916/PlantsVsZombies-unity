#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Selects plant behavior explicitly from stable native identifiers.</summary>
    internal static class PlantBehaviorFactory
    {
        /// <summary>Creates state ownership for one plant.</summary>
        /// <param name="kind">The original plant kind.</param>
        /// <param name="pool">The optional match-local capability owner.</param>
        /// <returns>The matching behavior, including inert support plants.</returns>
        internal static IPlantBehavior Create(PlantKind kind, SimulationRuntimePool? pool = null)
        {
            switch (kind)
            {
                case PlantKind.Peashooter:
                case PlantKind.SnowPea:
                case PlantKind.Repeater:
                case PlantKind.PuffShroom:
                case PlantKind.SeaShroom:
                    return pool?.Rent<ShooterPlantBehavior>() ?? new ShooterPlantBehavior();
                case PlantKind.FumeShroom:
                case PlantKind.CabbagePult:
                case PlantKind.KernelPult:
                case PlantKind.MelonPult:
                case PlantKind.WinterMelon:
                    return pool?.Rent<TimedPlantBehavior>() ?? new TimedPlantBehavior();
                case PlantKind.Sunflower:
                    return pool?.Rent<SunflowerPlantBehavior>() ?? new SunflowerPlantBehavior();
                case PlantKind.SunShroom:
                    return pool?.Rent<SunShroomPlantBehavior>() ?? new SunShroomPlantBehavior();
                case PlantKind.CoffeeBean:
                    return pool?.Rent<CoffeeBeanPlantBehavior>() ?? new CoffeeBeanPlantBehavior();
                case PlantKind.MagnetShroom:
                    return pool?.Rent<MagnetShroomPlantBehavior>() ?? new MagnetShroomPlantBehavior();
                case PlantKind.Torchwood:
                    return pool?.Rent<TorchwoodPlantBehavior>() ?? new TorchwoodPlantBehavior();
                case PlantKind.Squash:
                    return pool?.Rent<SquashPlantBehavior>() ?? new SquashPlantBehavior();
                case PlantKind.Blover:
                    return pool?.Rent<BloverPlantBehavior>() ?? new BloverPlantBehavior();
                case PlantKind.DoomShroom:
                    return pool?.Rent<DoomShroomPlantBehavior>() ?? new DoomShroomPlantBehavior();
                case PlantKind.TangleKelp:
                    return pool?.Rent<TangleKelpPlantBehavior>() ?? new TangleKelpPlantBehavior();
                case PlantKind.Chomper:
                    return pool?.Rent<ChomperPlantBehavior>() ?? new ChomperPlantBehavior();
                case PlantKind.CherryBomb:
                    return pool?.Rent<CherryBombPlantBehavior>() ?? new CherryBombPlantBehavior();
                case PlantKind.Jalapeno:
                    return pool?.Rent<JalapenoPlantBehavior>() ?? new JalapenoPlantBehavior();
                case PlantKind.PotatoMine:
                    return pool?.Rent<PotatoMinePlantBehavior>() ?? new PotatoMinePlantBehavior();
                case PlantKind.Starfruit:
                    return pool?.Rent<StarfruitPlantBehavior>() ?? new StarfruitPlantBehavior();
                case PlantKind.Cactus:
                    return pool?.Rent<CactusPlantBehavior>() ?? new CactusPlantBehavior();
                case PlantKind.Threepeater:
                    return pool?.Rent<ThreepeaterPlantBehavior>() ?? new ThreepeaterPlantBehavior();
                case PlantKind.SplitPea:
                    return pool?.Rent<SplitPeaPlantBehavior>() ?? new SplitPeaPlantBehavior();
                case PlantKind.TwinSunflower:
                case PlantKind.GatlingPea:
                case PlantKind.GloomShroom:
                case PlantKind.Cattail:
                case PlantKind.GoldMagnet:
                case PlantKind.Spikerock:
                case PlantKind.CobCannon:
                case PlantKind.Imitater:
                    return pool?.Rent<UpgradePlantBehavior>() ?? new UpgradePlantBehavior();
                case PlantKind.GraveBuster:
                    return pool?.Rent<GraveBusterPlantBehavior>() ?? new GraveBusterPlantBehavior();
                case PlantKind.Marigold:
                    return pool?.Rent<MarigoldPlantBehavior>() ?? new MarigoldPlantBehavior();
                case PlantKind.IceShroom:
                    return pool?.Rent<IceShroomPlantBehavior>() ?? new IceShroomPlantBehavior();
                case PlantKind.Spikeweed:
                    return pool?.Rent<SpikeweedPlantBehavior>() ?? new SpikeweedPlantBehavior();
                case PlantKind.ScaredyShroom:
                    return pool?.Rent<ScaredyPlantBehavior>() ?? new ScaredyPlantBehavior();
                default:
                    return pool?.Rent<PlantBehavior>() ?? new PlantBehavior();
            }
        }
    }
}
