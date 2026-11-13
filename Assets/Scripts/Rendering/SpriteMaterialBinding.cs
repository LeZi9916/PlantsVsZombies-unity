#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains blend materials while refreshing native texture bindings after asset unloading and reloading.</summary>
    internal sealed class SpriteMaterialBinding
    {
        /// <summary>Gets the reusable materials for normal, additive, and opaque hardware blending.</summary>
        internal Material[] Materials { get; }
        /// <summary>Gets or sets the submission batch that last rebound the currently sampled texture.</summary>
        internal ulong LastBatchVersion { get; set; }

        /// <summary>Retains newly created bindings already initialized for the current submission batch.</summary>
        /// <param name="materials">The materials sharing this logical source texture.</param>
        /// <param name="batchVersion">The submission batch that initialized their native texture properties.</param>
        internal SpriteMaterialBinding(Material[] materials, ulong batchVersion)
        {
            Materials = materials;
            LastBatchVersion = batchVersion;
        }
    }
}
