#nullable enable
namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the current native vase interlude pose and message without restarting Dave's dialogue or RNG.</summary>
    internal sealed class AdventureDialogueCheckpoint
    {
        /// <summary>The fractional native update remaining in the interlude.</summary>
        internal double Remainder;
        /// <summary>The displayed original message number.</summary>
        internal int Message;
        /// <summary>The already resolved original message text.</summary>
        internal string Text = string.Empty;
        /// <summary>The current native body animation marker.</summary>
        internal string? Animation;
        /// <summary>The native body animation age in seconds.</summary>
        internal float Age;
        /// <summary>The body animation frame rate.</summary>
        internal float Rate;
        /// <summary>Whether the native body animation repeats.</summary>
        internal bool Loop;
        /// <summary>Whether Dave retains the native handing pose.</summary>
        internal bool Handing;
        /// <summary>The original held-item image or reanimation.</summary>
        internal string? Item;
        /// <summary>The held-item animation age.</summary>
        internal float ItemAge;
        /// <summary>The native mouth override.</summary>
        internal string? Mouth;
        /// <summary>The native updates until the next blink.</summary>
        internal int BlinkTicks;
        /// <summary>The current blink age, or minus one while inactive.</summary>
        internal float BlinkAge;
    }
}
