#nullable enable
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Invalidates inspection visibility synchronously without a per-frame object update.</summary>
    [ExecuteAlways]
    [AddComponentMenu("")]
    internal sealed class SpriteHierarchyVisibility : MonoBehaviour
    {
        /// <summary>The registry currently owning this grouping object.</summary>
        private SpriteHierarchy? _owner;
        /// <summary>The binding generation rejecting callbacks after a match reset.</summary>
        private uint _generation;

        /// <summary>Changes the owner before an inactive object is rebound.</summary>
        /// <param name="owner">The live registry, or null for an inactive pool object.</param>
        /// <param name="generation">The owner's current generation.</param>
        internal void Bind(SpriteHierarchy? owner, uint generation)
        {
            _owner = owner;
            _generation = generation;
        }

        private void OnEnable()
        {
            _owner?.NotifyVisibilityChanged(_generation);
        }

        private void OnDisable()
        {
            _owner?.NotifyVisibilityChanged(_generation);
        }
    }
}
