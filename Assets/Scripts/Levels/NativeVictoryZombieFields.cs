#nullable enable

namespace PlantsVsZombies.Levels
{
    /// <summary>
    /// Raw fields read by the native victory-blocker iterator. They deliberately
    /// retain offsets because not all field semantics have been established.
    /// </summary>
    public struct NativeVictoryZombieFields
    {
        /// <summary>Stores byte ba.</summary>
        public readonly bool ByteBA;
        /// <summary>Stores byte ec.</summary>
        public readonly bool ByteEC;
        /// <summary>Stores phase28.</summary>
        public readonly int Phase28;
        /// <summary>Stores byte b8.</summary>
        public readonly bool ByteB8;
        /// <summary>Initializes native victory zombie fields.</summary>
        /// <param name = "byteBA">The byte ba.</param>
        /// <param name = "byteEC">The byte ec.</param>
        /// <param name = "phase28">The phase28.</param>
        /// <param name = "byteB8">The byte b8.</param>
        public NativeVictoryZombieFields(bool byteBA, bool byteEC, int phase28, bool byteB8)
        {
            ByteBA = byteBA;
            ByteEC = byteEC;
            Phase28 = phase28;
            ByteB8 = byteB8;
        }
    }
}
