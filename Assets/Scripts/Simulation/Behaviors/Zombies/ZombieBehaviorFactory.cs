#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Maps native species and mode-normalized identifiers to action owners.</summary>
    internal static class ZombieBehaviorFactory
    {
        /// <summary>Creates an actor's capability owner.</summary>
        /// <param name="kind">The native kind.</param>
        /// <param name="pool">The optional match-local capability owner.</param>
        /// <returns>The matching special or ordinary walking behavior.</returns>
        internal static IZombieBehavior Create(ZombieKind kind, SimulationRuntimePool? pool = null)
        {
            switch (kind)
            {
                case ZombieKind.Balloon:
                    return pool?.Rent<BalloonZombieBehavior>() ?? new BalloonZombieBehavior();
                case ZombieKind.JackInTheBox:
                    return pool?.Rent<JackInTheBoxZombieBehavior>() ?? new JackInTheBoxZombieBehavior();
                case ZombieKind.PoleVaulter:
                    return pool?.Rent<PoleVaulterZombieBehavior>() ?? new PoleVaulterZombieBehavior();
                case ZombieKind.Snorkel:
                    return pool?.Rent<SnorkelZombieBehavior>() ?? new SnorkelZombieBehavior();
                case ZombieKind.DolphinRider:
                    return pool?.Rent<DolphinZombieBehavior>() ?? new DolphinZombieBehavior();
                case ZombieKind.Bobsled:
                    return pool?.Rent<BobsledZombieBehavior>() ?? new BobsledZombieBehavior();
                case ZombieKind.Ladder:
                    return pool?.Rent<LadderZombieBehavior>() ?? new LadderZombieBehavior();
                case ZombieKind.Bungee:
                    return pool?.Rent<BungeeZombieBehavior>() ?? new BungeeZombieBehavior();
                case ZombieKind.BackupDancer:
                case ZombieKind.Dancing:
                    return pool?.Rent<DancerZombieBehavior>() ?? new DancerZombieBehavior();
                case ZombieKind.Gargantuar:
                    return pool?.Rent<GargantuarZombieBehavior>() ?? new GargantuarZombieBehavior();
                case ZombieKind.Imp:
                    return pool?.Rent<ThrownImpZombieBehavior>() ?? new ThrownImpZombieBehavior();
                case ZombieKind.Digger:
                    return pool?.Rent<DiggerZombieBehavior>() ?? new DiggerZombieBehavior();
                case ZombieKind.Catapult:
                    return pool?.Rent<CatapultZombieBehavior>() ?? new CatapultZombieBehavior();
                case ZombieKind.Zomboni:
                    return pool?.Rent<ZomboniZombieBehavior>() ?? new ZomboniZombieBehavior();
                case ZombieKind.Pogo:
                    return pool?.Rent<PogoZombieBehavior>() ?? new PogoZombieBehavior();
                case ZombieKind.Newspaper:
                    return pool?.Rent<NewspaperZombieBehavior>() ?? new NewspaperZombieBehavior();
                default:
                    return pool?.Rent<WalkingZombieBehavior>() ?? new WalkingZombieBehavior();
            }
        }
    }
}
