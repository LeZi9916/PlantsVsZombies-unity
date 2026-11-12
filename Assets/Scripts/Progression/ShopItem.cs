#nullable enable

namespace PlantsVsZombies.Progression
{
    /// <summary>Defines shop item.</summary>
    public sealed class ShopItem
    {
        /// <summary>Gets or sets id.</summary>
        public int Id { get; internal set; }
        /// <summary>Gets or sets name.</summary>
        public string Name { get; internal set; } = string.Empty;
        /// <summary>Gets or sets price units.</summary>
        public int PriceUnits { get; internal set; }
        /// <summary>Gets or sets unlock level.</summary>
        public int UnlockLevel { get; internal set; }
        /// <summary>Gets or sets after adventure.</summary>
        public bool AfterAdventure { get; internal set; }
        /// <summary>Gets or sets garden item.</summary>
        public bool GardenItem { get; internal set; }
        /// <summary>Gets or sets consumable.</summary>
        public bool Consumable { get; internal set; }
        /// <summary>Gets or sets icon.</summary>
        public string? Icon { get; internal set; }
    }
}
