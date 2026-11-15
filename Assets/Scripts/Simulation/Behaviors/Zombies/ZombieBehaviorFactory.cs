#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Maps native species and mode-normalized identifiers to action owners.</summary>
    internal static class ZombieBehaviorFactory
    {
        /// <summary>Creates an actor's capability owner.</summary>
        /// <param name="kind">The native kind.</param>
        /// <returns>The matching special or ordinary walking behavior.</returns>
        internal static IZombieBehavior Create(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Balloon:
                    return new BalloonZombieBehavior();
                case ZombieKind.JackInTheBox:
                    return new JackInTheBoxZombieBehavior();
                case ZombieKind.PoleVaulter:
                    return new PoleVaulterZombieBehavior();
                case ZombieKind.Snorkel:
                    return new SnorkelZombieBehavior();
                case ZombieKind.DolphinRider:
                    return new DolphinZombieBehavior();
                case ZombieKind.Bobsled:
                    return new BobsledZombieBehavior();
                case ZombieKind.Ladder:
                    return new LadderZombieBehavior();
                case ZombieKind.Bungee:
                    return new BungeeZombieBehavior();
                case ZombieKind.BackupDancer:
                case ZombieKind.Dancing:
                    return new DancerZombieBehavior();
                case ZombieKind.Gargantuar:
                    return new GargantuarZombieBehavior();
                case ZombieKind.Imp:
                    return new ThrownImpZombieBehavior();
                case ZombieKind.Digger:
                    return new DiggerZombieBehavior();
                case ZombieKind.Catapult:
                    return new CatapultZombieBehavior();
                case ZombieKind.Zomboni:
                    return new ZomboniZombieBehavior();
                case ZombieKind.Pogo:
                    return new PogoZombieBehavior();
                case ZombieKind.Newspaper:
                    return new NewspaperZombieBehavior();
                default:
                    return new WalkingZombieBehavior();
            }
        }
    }
}
