#nullable enable

namespace PlantsVsZombies.Rendering
{
    /// <summary>Selects a deterministic rendering backend for compatibility and paired validation.</summary>
    internal enum SpriteRenderingBackend
    {
        /// <summary>Uses instancing when supported and otherwise retains the portable mesh renderer.</summary>
        Auto,
        /// <summary>Uses the independent combined-mesh implementation.</summary>
        Mesh,
        /// <summary>Requests instancing while retaining the same safe capability and capacity fallbacks.</summary>
        Instanced
    }
}
