#nullable enable


namespace PlantsVsZombies.Adventure
{
    /// <summary>Supplies external target, lane, animation, and random observations for one tick.</summary>
    public readonly struct AdventureMultiHeadTimingInput
    {
        /// <summary>Gets whether the plant receives this update.</summary>
        public bool PlantUpdateEnabled { get; }

        /// <summary>Gets whether the old shooting countdown advances.</summary>
        public bool ShootingCountdownEnabled { get; }

        /// <summary>Gets whether the primary attack countdown advances.</summary>
        public bool MainActionEnabled { get; }

        /// <summary>Gets the heads whose corresponding target search finds a zombie.</summary>
        public AdventureShootingHeads TargetHeads { get; }

        /// <summary>Gets the heads whose Threepeater lanes exist on this board.</summary>
        public AdventureShootingHeads AvailableHeads { get; }

        /// <summary>Gets whether the shooting animations can return to idle at zero.</summary>
        public bool AnimationResetReady { get; }

        /// <summary>Gets the injected result of the native R(15) draw when due.</summary>
        public int RandomDraw { get; }

        /// <summary>Initializes the observations for one native update.</summary>
        /// <param name="plantUpdateEnabled">Whether the plant receives this update.</param>
        /// <param name="shootingCountdownEnabled">Whether the old shooting countdown advances.</param>
        /// <param name="mainActionEnabled">Whether the primary attack countdown advances.</param>
        /// <param name="targetHeads">The heads with a target in their respective lane or direction.</param>
        /// <param name="availableHeads">The Threepeater heads with a valid lane.</param>
        /// <param name="animationResetReady">Whether a finished shooting animation can return to idle.</param>
        /// <param name="randomDraw">The injected R(15) result when the primary countdown expires.</param>
        public AdventureMultiHeadTimingInput(bool plantUpdateEnabled, bool shootingCountdownEnabled, bool mainActionEnabled,
            AdventureShootingHeads targetHeads, AdventureShootingHeads availableHeads, bool animationResetReady, int randomDraw)
        {
            PlantUpdateEnabled = plantUpdateEnabled;
            ShootingCountdownEnabled = shootingCountdownEnabled;
            MainActionEnabled = mainActionEnabled;
            TargetHeads = targetHeads;
            AvailableHeads = availableHeads;
            AnimationResetReady = animationResetReady;
            RandomDraw = randomDraw;
        }
    }
}
