#nullable enable

namespace PlantsVsZombies.VerifiedData
{
    /// <summary>
    /// Static mode UI metadata. A row does not establish that a mode is accessible
    /// or provide a playable level script. Adventure mode 0 is not in this table.
    /// </summary>
    public sealed class ChallengeModeDefinition
    {
        /// <summary>Creates one verified challenge-menu metadata row.</summary>
        /// <param name = "id">The original challenge identifier starting at one.</param>
        /// <param name = "localizationKey">The original challenge-title localization key.</param>
        /// <param name = "uiCategoryIndex">The original challenge-menu category index.</param>
        /// <param name = "uiGridRow">The original card row within the category.</param>
        /// <param name = "uiGridColumn">The original card column within the category.</param>
        /// <param name = "unidentified0x04">The uninterpreted value at original row offset 0x04.</param>
        internal ChallengeModeDefinition(int id, string localizationKey, int uiCategoryIndex, int uiGridRow, int uiGridColumn, int unidentified0x04)
        {
            Id = id;
            LocalizationKey = localizationKey;
            UiCategoryIndex = uiCategoryIndex;
            UiGridRow = uiGridRow;
            UiGridColumn = uiGridColumn;
            Unidentified0x04 = unidentified0x04;
        }

        /// <summary>Gets the original one-based challenge identifier; adventure mode zero is excluded.</summary>
        public int Id { get; }
        /// <summary>Gets the original localization key for the challenge title.</summary>
        public string LocalizationKey { get; }
        /// <summary>Gets the original menu category index.</summary>
        public int UiCategoryIndex { get; }
        /// <summary>Gets the original row of the challenge card within its menu category.</summary>
        public int UiGridRow { get; }
        /// <summary>Gets the original column of the challenge card within its menu category.</summary>
        public int UiGridColumn { get; }
        /// <summary>Gets the uninterpreted integer stored at offset 0x04 in the original row.</summary>
        public int Unidentified0x04 { get; }
    }
}
