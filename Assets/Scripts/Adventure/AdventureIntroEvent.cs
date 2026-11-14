#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro event.</summary>
    public enum AdventureIntroEvent
    {
        /// <summary>Identifies seed chooser opened.</summary>
        SeedChooserOpened,
        /// <summary>Identifies place lawn items.</summary>
        PlaceLawnItems,
        /// <summary>Identifies sod started.</summary>
        SodStarted,
        /// <summary>Identifies sod finished.</summary>
        SodFinished,
        /// <summary>Identifies grave stones enabled.</summary>
        GraveStonesEnabled,
        /// <summary>Identifies ready set plant started.</summary>
        ReadySetPlantStarted,
        /// <summary>Identifies intro finished.</summary>
        IntroFinished,
        /// <summary>Identifies the original forced storm flash while panning to the street or returning to the lawn.</summary>
        StormFlashStarted
    }
}
