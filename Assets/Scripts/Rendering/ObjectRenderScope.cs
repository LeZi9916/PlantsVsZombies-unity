#nullable enable
using System;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Completes one nested batch of scene-rendered sprite commands.</summary>
    public readonly struct ObjectRenderScope : IDisposable
    {
        /// <summary>The pool that owns this rendering scope.</summary>
        private readonly ObjectSpriteRenderer? _renderer;
        /// <summary>Creates a scope for a pool that has already begun collecting commands.</summary>
        /// <param name = "renderer">The pool to finish when the scope is disposed.</param>
        internal ObjectRenderScope(ObjectSpriteRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>Publishes the scene objects when the outermost scope ends.</summary>
        public void Dispose()
        {
            _renderer?.EndFrame();
        }
    }
}
