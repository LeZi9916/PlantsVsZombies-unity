#nullable enable

namespace PlantsVsZombies.Sun
{
    // The four opaque predicates in the EXE and the outer scene gate are supplied by the caller.
    /// <summary>Defines sky sun context.</summary>
    public readonly struct SkySunContext
    {
        /// <summary>Gets or sets background.</summary>
        public int Background { get; }
        /// <summary>Gets or sets mode.</summary>
        public int Mode { get; }
        /// <summary>Gets or sets update enabled.</summary>
        public bool UpdateEnabled { get; }
        /// <summary>Gets or sets board flag5624.</summary>
        public bool BoardFlag5624 { get; }
        /// <summary>Gets or sets board countdown561 c.</summary>
        public int BoardCountdown561C { get; }
        /// <summary>Gets or sets board countdown5618.</summary>
        public int BoardCountdown5618 { get; }
        /// <summary>Gets or sets tutorial state.</summary>
        public int TutorialState { get; }
        /// <summary>Gets or sets plant count.</summary>
        public int PlantCount { get; }
        /// <summary>Gets or sets external predicate blocked.</summary>
        public bool ExternalPredicateBlocked { get; }

        /// <summary>Initializes sky sun context.</summary>
        /// <param name = "background">The background.</param>
        /// <param name = "mode">The mode.</param>
        /// <param name = "updateEnabled">The update enabled.</param>
        /// <param name = "boardFlag5624">The board flag5624.</param>
        /// <param name = "boardCountdown561C">The board countdown561 c.</param>
        /// <param name = "boardCountdown5618">The board countdown5618.</param>
        /// <param name = "tutorialState">The tutorial state.</param>
        /// <param name = "plantCount">The plant count.</param>
        /// <param name = "externalPredicateBlocked">The external predicate blocked.</param>
        public SkySunContext(int background, int mode, bool updateEnabled, bool boardFlag5624, int boardCountdown561C, int boardCountdown5618, int tutorialState, int plantCount, bool externalPredicateBlocked)
        {
            Background = background;
            Mode = mode;
            UpdateEnabled = updateEnabled;
            BoardFlag5624 = boardFlag5624;
            BoardCountdown561C = boardCountdown561C;
            BoardCountdown5618 = boardCountdown5618;
            TutorialState = tutorialState;
            PlantCount = plantCount;
            ExternalPredicateBlocked = externalPredicateBlocked;
        }
    }
}
