#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines adventure planting failure.</summary>
    public enum AdventurePlantingFailure
    {
        /// <summary>Identifies none.</summary>
        None,
        /// <summary>Identifies not playing.</summary>
        NotPlaying,
        /// <summary>Identifies invalid cell.</summary>
        InvalidCell,
        /// <summary>Identifies seed unavailable.</summary>
        SeedUnavailable,
        /// <summary>Identifies cooldown.</summary>
        Cooldown,
        /// <summary>Identifies sun.</summary>
        Sun,
        /// <summary>Identifies occupied.</summary>
        Occupied,
        /// <summary>Identifies needs lily pad.</summary>
        NeedsLilyPad,
        /// <summary>Identifies needs flower pot.</summary>
        NeedsFlowerPot,
        /// <summary>Identifies needs water.</summary>
        NeedsWater,
        /// <summary>Identifies roof restriction.</summary>
        RoofRestriction,
        /// <summary>Identifies grave.</summary>
        Grave,
        /// <summary>Identifies crater.</summary>
        Crater,
        /// <summary>Identifies ice.</summary>
        Ice,
        /// <summary>Identifies needs sleeping mushroom.</summary>
        NeedsSleepingMushroom,
        /// <summary>Identifies tutorial.</summary>
        Tutorial,
        /// <summary>Identifies bowling line.</summary>
        BowlingLine,
        /// <summary>Identifies needs upgrade.</summary>
        NeedsUpgrade,
        /// <summary>Identifies cannon footprint.</summary>
        CannonFootprint,
        /// <summary>Identifies imitater target.</summary>
        ImitaterTarget,
        /// <summary>Identifies a Potato Mine rejected on a Lily Pad by the original ground-only rule.</summary>
        PotatoMineOnLilyPad
    }
}
