#nullable enable

namespace PlantsVsZombies.Adventure
{
    // Isolated PC cutscene presentation. No game objects, Unity or music dependency.
    /// <summary>Defines adventure intro stage.</summary>
    public enum AdventureIntroStage
    {
        /// <summary>Identifies home.</summary>
        Home,
        /// <summary>Identifies pan to street.</summary>
        PanToStreet,
        /// <summary>Identifies street.</summary>
        Street,
        /// <summary>Identifies choosing seeds.</summary>
        ChoosingSeeds,
        /// <summary>Identifies return to lawn.</summary>
        ReturnToLawn,
        /// <summary>Identifies lawn setup.</summary>
        LawnSetup,
        /// <summary>Identifies ready set plant.</summary>
        ReadySetPlant,
        /// <summary>Identifies complete.</summary>
        Complete
    }
}
