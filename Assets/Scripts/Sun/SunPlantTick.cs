#nullable enable

namespace PlantsVsZombies.Sun
{
    /// <summary>Defines sun plant tick.</summary>
    public readonly struct SunPlantTick
    {
        /// <summary>Gets or sets state.</summary>
        public SunPlantState State { get; }
        /// <summary>Gets or sets spawn count.</summary>
        public int SpawnCount { get; }
        /// <summary>Gets or sets spawn kind.</summary>
        public SunKind SpawnKind { get; }
        /// <summary>Gets or sets motion type.</summary>
        public int MotionType { get; }

        /// <summary>Initializes sun plant tick.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "spawnCount">The spawn count.</param>
        /// <param name = "spawnKind">The spawn kind.</param>
        internal SunPlantTick(SunPlantState state, int spawnCount, SunKind spawnKind)
        {
            State = state;
            SpawnCount = spawnCount;
            SpawnKind = spawnKind;
            MotionType = 2;
        }
    }
}
