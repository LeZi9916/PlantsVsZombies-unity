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
        public IReadOnlyList<string> BodyLines { get; internal set; }

        /// <summary>Stores title,  warning.</summary>
        internal BitmapTextLayout _title, _warning;
        /// <summary>Stores body.</summary>
        internal List<BitmapTextLayout> _body;
    }
}
