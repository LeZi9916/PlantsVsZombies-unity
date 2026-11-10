#nullable enable

namespace PlantsVsZombies.Timing
{
    // These are snapshots of external game conditions, not modeled game systems.
    /// <summary>Defines plant timing input.</summary>
    public readonly struct PlantTimingInput
    {
        /// <summary>Gets or sets plant update enabled.</summary>
        public bool PlantUpdateEnabled { get; }
        /// <summary>Gets or sets shooting countdown enabled.</summary>
        public bool ShootingCountdownEnabled { get; }
        /// <summary>Gets or sets main action enabled.</summary>
        public bool MainActionEnabled { get; }
        /// <summary>Gets or sets sun production enabled.</summary>
        public bool SunProductionEnabled { get; }
        /// <summary>Gets or sets has target.</summary>
        public bool HasTarget { get; }
        /// <summary>Gets or sets track.</summary>
        public ShootingTrack Track { get; }
        /// <summary>Gets or sets animation reset ready.</summary>
        public bool AnimationResetReady { get; }
        /// <summary>Gets or sets random draw.</summary>
        public int RandomDraw { get; }

        /// <summary>Initializes plant timing input.</summary>
        /// <param name = "plantUpdateEnabled">The plant update enabled.</param>
        /// <param name = "shootingCountdownEnabled">The shooting countdown enabled.</param>
        /// <param name = "mainActionEnabled">The main action enabled.</param>
        /// <param name = "sunProductionEnabled">The sun production enabled.</param>
        /// <param name = "hasTarget">The has target.</param>
        /// <param name = "track">The track.</param>
        /// <param name = "animationResetReady">The animation reset ready.</param>
        /// <param name = "randomDraw">The random draw.</param>
        public PlantTimingInput(bool plantUpdateEnabled, bool shootingCountdownEnabled, bool mainActionEnabled, bool sunProductionEnabled, bool hasTarget, ShootingTrack track, bool animationResetReady, int randomDraw)
        {
            PlantUpdateEnabled = plantUpdateEnabled;
            ShootingCountdownEnabled = shootingCountdownEnabled;
            MainActionEnabled = mainActionEnabled;
            SunProductionEnabled = sunProductionEnabled;
            HasTarget = hasTarget;
            Track = track;
            AnimationResetReady = animationResetReady;
            RandomDraw = randomDraw;
        }
    }
}
