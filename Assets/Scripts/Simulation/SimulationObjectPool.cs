#nullable enable

using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Reuses short-lived simulation entries while bounding retained inactive objects.</summary>
    /// <typeparam name="T">The simulation entry with a parameterless constructor.</typeparam>
    internal sealed class SimulationObjectPool<T> where T : class, new()
    {
        /// <summary>The inactive objects available for reuse.</summary>
        private readonly Stack<T> _available;
        /// <summary>The maximum number of inactive objects retained after peak demand.</summary>
        private readonly int _capacity;

        /// <summary>Prepares the common demand before simulation updates begin.</summary>
        /// <param name="initialCount">The number of entries created during initialization.</param>
        /// <param name="capacity">The maximum number of inactive entries to retain.</param>
        public SimulationObjectPool(int initialCount, int capacity)
        {
            _capacity = capacity;
            _available = new Stack<T>(capacity);
            for (var i = 0; i < initialCount; i++)
            {
                _available.Push(new T());
            }
        }

        /// <summary>Takes an inactive entry, growing only when concurrent demand exceeds availability.</summary>
        /// <returns>An entry that its caller must reset before publishing it to the active list.</returns>
        public T Rent()
        {
            return _available.Count > 0 ? _available.Pop() : new T();
        }

        /// <summary>Retains a retired entry when the inactive pool has room.</summary>
        /// <param name="item">The entry that has already been removed from its active list.</param>
        public void Return(T item)
        {
            if (_available.Count < _capacity)
            {
                _available.Push(item);
            }
        }
    }
}
