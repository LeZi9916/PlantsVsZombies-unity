#nullable enable
using System;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Maps an original text path to a platform-independent Resources text asset.</summary>
    [Serializable]
    public sealed class SpriteAtlasText
    {
        /// <summary>The original relative animation, particle, or properties path.</summary>
        public string SourceIdentity = string.Empty;
        /// <summary>The generated Resources path without a file extension.</summary>
        public string ResourcePath = string.Empty;
    }
}
