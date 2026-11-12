#nullable enable
using PlantsVsZombies.UI.BitmapFonts;

using System.Collections.Generic;

using UnityEngine;

namespace PlantsVsZombies.UI
{
    /// <summary>Defines original tooltip layout.</summary>
    public sealed class OriginalTooltipLayout
    {
        /// <summary>Gets or sets bounds.</summary>
        public Rect Bounds { get; internal set; }
        /// <summary>Gets or sets body wrap width.</summary>
        public int BodyWrapWidth { get; internal set; }
        /// <summary>Gets or sets body lines.</summary>
        public IReadOnlyList<string> BodyLines { get; internal set; } = System.Array.Empty<string>();

        /// <summary>Stores title,  warning.</summary>
        internal BitmapTextLayout? _title, _warning;

        /// <summary>Gets the title, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal BitmapTextLayout RequiredTitle => _title ?? throw new System.InvalidOperationException("The member '_title' is not available.");

        /// <summary>Gets the warning, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal BitmapTextLayout RequiredWarning => _warning ?? throw new System.InvalidOperationException("The member '_warning' is not available.");
        /// <summary>Stores body.</summary>
        internal List<BitmapTextLayout> _body = new();
    }
}
