#nullable enable

namespace PlantsVsZombies.Adventure
{
    // PC Coin::CoinInitialize 0x43B610, UpdateFall 0x43C1F0,
    // UpdateCollected 0x43C570; Board::FadeOutLevel 0x418140.
    /// <summary>Defines adventure award stage.</summary>
    public enum AdventureAwardStage
    {
        /// <summary>Identifies falling.</summary>
        Falling,
        /// <summary>Identifies waiting.</summary>
        Waiting,
        /// <summary>Identifies collecting.</summary>
        Collecting,
        /// <summary>Identifies introduction.</summary>
        Introduction
    }
}
