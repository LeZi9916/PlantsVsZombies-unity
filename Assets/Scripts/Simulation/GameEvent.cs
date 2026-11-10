#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines game event.</summary>
    public enum GameEvent
    {
        /// <summary>Identifies planted.</summary>
        Planted,
        /// <summary>Identifies shoot.</summary>
        Shoot,
        /// <summary>Identifies sun collected.</summary>
        SunCollected,
        /// <summary>Identifies zombie hit.</summary>
        ZombieHit,
        /// <summary>Identifies zombie died.</summary>
        ZombieDied,
        /// <summary>Identifies mower.</summary>
        Mower,
        /// <summary>Identifies explosion.</summary>
        Explosion,
        /// <summary>Identifies mine explosion.</summary>
        MineExplosion,
        /// <summary>Identifies jalapeno explosion.</summary>
        JalapenoExplosion,
        /// <summary>Identifies won.</summary>
        Won,
        /// <summary>Identifies lost.</summary>
        Lost,
        /// <summary>Identifies sun pickup started.</summary>
        SunPickupStarted
    }
}
