#nullable enable
using System;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Describes lossless atlas layouts and packaged original animation texts without loading textures.</summary>
    [Serializable]
    public sealed class SpriteAtlasCatalog
    {
        /// <summary>The version understood by the runtime loader.</summary>
        public const int CurrentVersion = 1;
        /// <summary>The serialized catalog format version.</summary>
        public int Version = CurrentVersion;
        /// <summary>The fingerprint of the original sources and baker settings.</summary>
        public string SourceHash = string.Empty;
        /// <summary>The independently packed page-size variants.</summary>
        public SpriteAtlasLayout[] Layouts = Array.Empty<SpriteAtlasLayout>();
        /// <summary>The packaged text files indexed by original relative path.</summary>
        public SpriteAtlasText[] Texts = Array.Empty<SpriteAtlasText>();
    }
}
