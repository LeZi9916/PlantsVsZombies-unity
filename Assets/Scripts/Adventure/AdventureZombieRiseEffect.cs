#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the original independent ZombieRise particles after their zombie leaves the grave.</summary>
    public sealed class AdventureZombieRiseEffect
    {
        /// <summary>Identifies this particle instance and its lawn row.</summary>
        public int Id, Row;

        /// <summary>Stores the original world-space particle origin and its elapsed lifetime.</summary>
        public float X, Y, Age;

        /// <summary>Gets whether this is the Whack-a-Zombie rise particle.</summary>
        public bool Whack;
    }
}
