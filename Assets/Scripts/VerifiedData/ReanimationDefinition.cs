#nullable enable

namespace PlantsVsZombies.VerifiedData
{
    /// <summary>A static reanimation ID-to-path mapping and its unidentified raw field.</summary>
    public sealed class ReanimationDefinition
    {
        /// <summary>Creates a verified original animation identifier-to-path mapping.</summary>
        /// <param name = "id">The zero-based original reanimation identifier.</param>
        /// <param name = "path">The resource-relative original reanimation path.</param>
        /// <param name = "unknown0x08">The uninterpreted value at original row offset 0x08.</param>
        internal ReanimationDefinition(int id, string path, int unknown0x08)
        {
            Id = id;
            Path = path;
            Unknown0x08 = unknown0x08;
        }

        /// <summary>Gets the original zero-based reanimation identifier.</summary>
        public int Id { get; }
        /// <summary>Gets the original resource-relative reanimation path with its stored casing and separators.</summary>
        public string Path { get; }
        /// <summary>Gets the uninterpreted integer stored at offset 0x08 in the original row.</summary>
        public int Unknown0x08 { get; }
    }
}
