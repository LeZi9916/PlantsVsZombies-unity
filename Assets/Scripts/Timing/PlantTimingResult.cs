#nullable enable

namespace PlantsVsZombies.Timing
{
    /// <summary>Defines plant timing result.</summary>
    public readonly struct PlantTimingResult
    {
        /// <summary>Gets or sets state.</summary>
        public PlantTimingState State { get; }
        /// <summary>Gets or sets attempt.</summary>
        public AttackAttempt Attempt { get; }
        /// <summary>Gets or sets animation started.</summary>
        public bool AnimationStarted { get; }
        /// <summary>Gets or sets projectiles created.</summary>
        public int ProjectilesCreated { get; }
        /// <summary>Gets or sets sun created.</summary>
        public bool SunCreated { get; }

        /// <summary>Initializes plant timing result.</summary>
        /// <param name = "state">The state.</param>
        /// <param name = "attempt">The attempt.</param>
        /// <param name = "animationStarted">The animation started.</param>
        /// <param name = "projectilesCreated">The projectiles created.</param>
        /// <param name = "sunCreated">The sun created.</param>
        internal PlantTimingResult(PlantTimingState state, AttackAttempt attempt, bool animationStarted, int projectilesCreated, bool sunCreated)
        {
            State = state;
            Attempt = attempt;
            AnimationStarted = animationStarted;
            ProjectilesCreated = projectilesCreated;
            SunCreated = sunCreated;
        }
    }
}
