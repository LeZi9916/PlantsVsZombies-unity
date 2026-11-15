#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns a temporary list until the surrounding using scope ends.</summary>
    /// <typeparam name="T">The temporary entry type.</typeparam>
    internal readonly struct ScratchBufferLease<T> : IDisposable
    {
        /// <summary>Receives the buffer after the operation.</summary>
        private readonly ScratchBufferPool<T> _owner;
        /// <summary>Gets the independently rented mutable list.</summary>
        internal List<T> Items { get; }
        /// <summary>Creates a lease owned by one using scope.</summary>
        /// <param name="owner">The session-local pool.</param>
        /// <param name="items">The empty rented list.</param>
        internal ScratchBufferLease(ScratchBufferPool<T> owner, List<T> items)
        {
            _owner = owner;
            Items = items;
        }
        /// <summary>Returns the temporary list to its session owner exactly once.</summary>
        public void Dispose()
        {
            _owner.Return(Items);
        }
    }
}
