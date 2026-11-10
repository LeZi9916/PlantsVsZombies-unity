#nullable enable

namespace PlantsVsZombies.Progression
{
    /// <summary>Runtime inputs for one selector refresh, including separate query results.</summary>
    public sealed class SelectorRuntimeFields
    {
        /// <summary>Uses one effective platform result at all three native call sites.</summary>
        public SelectorRuntimeFields(bool appFlag9E8, int appValue9E4, bool platformQueryPassed, bool auxiliaryVisibilityOverride) : this(appFlag9E8, appValue9E4, platformQueryPassed, platformQueryPassed, platformQueryPassed, auxiliaryVisibilityOverride)
        {
        }

        /// <summary>Supplies the effective platform results at the three call sites separately.</summary>
        public SelectorRuntimeFields(bool appFlag9E8, int appValue9E4, bool modeQueryPassed, bool completionQueryPassed, bool gardenQueryPassed, bool auxiliaryVisibilityOverride)
        {
            AppFlag9E8 = appFlag9E8;
            AppValue9E4 = appValue9E4;
            ModeQueryPassed = modeQueryPassed;
            CompletionQueryPassed = completionQueryPassed;
            GardenQueryPassed = gardenQueryPassed;
            AuxiliaryVisibilityOverride = auxiliaryVisibilityOverride;
        }

        /// <summary>Gets or sets app flag9 e8.</summary>
        public bool AppFlag9E8 { get; }
        /// <summary>Gets or sets app value9 e4.</summary>
        public int AppValue9E4 { get; }
        /// <summary>Effective app + 0x99c / 0x651f30 result for the mode-lock check.</summary>
        public bool ModeQueryPassed { get; }
        /// <summary>Effective result for the completion-indicator check.</summary>
        public bool CompletionQueryPassed { get; }
        /// <summary>Effective result for the garden-visibility check.</summary>
        public bool GardenQueryPassed { get; }
        /// <summary>Selector byte + 0x159; affects auxiliary visibility, not mode locks.</summary>
        public bool AuxiliaryVisibilityOverride { get; }
    }
}
