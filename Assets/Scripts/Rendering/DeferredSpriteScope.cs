#nullable enable
using System;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Bounds synchronous geometry deferral to an explicitly authorized presentation call.</summary>
    internal struct DeferredSpriteScope : IDisposable
    {
        /// <summary>Whether this instance still owns its explicit nested scope.</summary>
        private bool _active;

        /// <summary>Creates a scope for an already incremented deferral depth.</summary>
        /// <param name="active">Whether this scope owns an explicit begin operation.</param>
        internal DeferredSpriteScope(bool active)
        {
            _active = active;
        }

        /// <summary>Flushes the pending segment before ending this nested presentation scope.</summary>
        public void Dispose()
        {
            if (!_active)
            {
                return;
            }
            _active = false;
            ObjectSpriteRenderer.EndDeferred();
        }
    }
}
