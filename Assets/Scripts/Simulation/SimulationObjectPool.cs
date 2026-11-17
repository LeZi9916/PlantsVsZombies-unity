#nullable enable

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Reuses short-lived simulation entries while bounding retained inactive objects.</summary>
    /// <typeparam name="T">The simulation entry with a parameterless constructor.</typeparam>
    internal sealed class SimulationObjectPool<T> where T : class, new()
    {
        /// <summary>The inactive objects available for reuse.</summary>
        private readonly List<T> _available;
        /// <summary>The maximum number of inactive objects retained after peak demand.</summary>
        private int _capacity;
        /// <summary>Gets the number of objects constructed by this pool.</summary>
        internal int CreatedCount { get; private set; }
        /// <summary>Gets rent requests that exceeded explicitly prepared inactive storage.</summary>
        internal long MissCount { get; private set; }
        /// <summary>Gets the number of retained inactive objects.</summary>
        internal int AvailableCount => _available.Count;
        /// <summary>Gets the maximum retained inactive count.</summary>
        internal int RetainedCapacity => _capacity;
        /// <summary>Gets the number of rents served by an already constructed object.</summary>
        internal long ReuseCount { get; private set; }
        /// <summary>Gets returns discarded because the bounded pool was full.</summary>
        internal long DiscardedCount { get; private set; }

        /// <summary>Prepares the common demand before simulation updates begin.</summary>
        /// <param name="initialCount">The number of entries created during initialization.</param>
        /// <param name="capacity">The maximum number of inactive entries to retain.</param>
        /// <exception cref="ArgumentOutOfRangeException">The initial count is negative or exceeds retained capacity.</exception>
        public SimulationObjectPool(int initialCount, int capacity)
        {
            if (initialCount < 0 || capacity < initialCount)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCount));
            }
            _capacity = capacity;
            _available = new List<T>(capacity);
            for (var i = 0; i < initialCount; i++)
            {
                _available.Add(new T());
                CreatedCount++;
            }
        }

        /// <summary>Takes an inactive entry, growing only when concurrent demand exceeds availability.</summary>
        /// <returns>An entry that its caller must reset before publishing it to the active list.</returns>
        public T Rent()
        {
            if (_available.Count > 0)
            {
                ReuseCount++;
                var last = _available.Count - 1;
                var item = _available[last];
                _available.RemoveAt(last);
                return item;
            }
            CreatedCount++;
            MissCount++;
            return new T();
        }

        /// <summary>Retains a retired entry when the inactive pool has room.</summary>
        /// <param name="item">The entry that has already been removed from its active list.</param>
        public void Return(T item)
        {
            TryReturn(item);
        }

        /// <summary>Returns a retired entry without retaining more than the declared capacity.</summary>
        /// <param name="item">The retired entry, already disconnected from every active owner.</param>
        /// <returns>Whether this pool retained the object.</returns>
        internal bool TryReturn(T item)
        {
            if (_available.Count < _capacity)
            {
                _available.Add(item);
                return true;
            }
            DiscardedCount++;
            return false;
        }

        /// <summary>Creates only a bounded number of inactive objects during loading preparation.</summary>
        /// <param name="targetAvailable">The desired inactive count, clamped to this pool's capacity.</param>
        /// <param name="maxObjects">The maximum number constructed by this call.</param>
        /// <param name="prepared">An optional loading-time observer registering the new record's ownership.</param>
        /// <returns>The number of objects constructed.</returns>
        internal int PrepareCapacityStep(int targetAvailable, int maxObjects, Action<T>? prepared = null)
        {
            if (maxObjects > 0 && targetAvailable > _capacity)
            {
                _available.Capacity = targetAvailable;
                _capacity = targetAvailable;
            }
            var count = Math.Min(Math.Max(0, maxObjects), Math.Max(0, Math.Min(targetAvailable, _capacity) - _available.Count));
            for (var index = 0; index < count; index++)
            {
                var item = new T();
                prepared?.Invoke(item);
                _available.Add(item);
                CreatedCount++;
            }
            return count;
        }
    }
}
