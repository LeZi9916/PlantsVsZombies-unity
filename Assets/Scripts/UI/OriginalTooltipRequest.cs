#nullable enable

namespace PlantsVsZombies.UI
{
    /// <summary>Native ToolTipWidget inputs. X/Y are the host's native desired position, not a plant grid.</summary>
    public sealed class OriginalTooltipRequest
    {
        /// <summary>Stores title, body, warning.</summary>
        public string Title = "", Body = "", Warning = "";
        /// <summary>Stores x, y, warning flash counter.</summary>
        public int X, Y, WarningFlashCounter;
        /// <summary>Stores visible, centered.</summary>
        public bool Visible = true, Centered;
        /// <summary>Stores min screen x, max screen y, canvas width.</summary>
        public int MinScreenX, MaxScreenY = 600, CanvasWidth = 800;
        /// <summary>Stores graphics translation x, graphics translation y.</summary>
        public float GraphicsTranslationX, GraphicsTranslationY;
        // PC +0x78 changes the minimum body line width through this original property (default100).
        /// <summary>Stores use seed chooser line width.</summary>
        public bool UseSeedChooserLineWidth;
        /// <summary>Stores seed chooser max line width.</summary>
        public int SeedChooserMaxLineWidth = 100;
    }
}
