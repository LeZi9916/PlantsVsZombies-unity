#nullable enable

using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Reuses bounded session-local temporary lists with independent nested leases.</summary>
    /// <typeparam name="T">The temporary entry type.</typeparam>
    internal sealed class ScratchBufferPool<T>
    {
        /// <summary>Retains inactive scratch buffers only.</summary>
        private readonly Stack<List<T>> _available = new Stack<List<T>>(8);
        /// <summary>Rents a distinct empty list for this operation.</summary>
        /// <returns>A disposable lease that returns its list on early exits and exceptions.</returns>
        internal ScratchBufferLease<T> Rent()
        {
            return new ScratchBufferLease<T>(this, _available.Count == 0 ? new List<T>() : _available.Pop());
        }
        /// <summary>Releases references and bounds both retained count and exceptional capacities.</summary>
        /// <param name="items">The completed operation's temporary list.</param>
        internal void Return(List<T> items)
        {
            items.Clear();
            if (_available.Count < 8 && items.Capacity <= 4096)
            {
                _available.Push(items);
            }
        }
    }
}
