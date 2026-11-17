#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ordered membership and lazily synchronized identity and lane indexes.</summary>
    /// <typeparam name="T">The reference entity type.</typeparam>
    internal sealed class EntityStore<T> : IReadOnlyList<T> where T : class
    {
        /// <summary>Preserves native insertion and removal ordering.</summary>
        private readonly List<T> _items = new List<T>();
        /// <summary>Tracks live reference membership independently of entity identifiers.</summary>
        private readonly HashSet<T> _members = new HashSet<T>();
        /// <summary>Maps native identifiers to the first active entry.</summary>
        private readonly Dictionary<int, T> _ids = new Dictionary<int, T>();
        /// <summary>Retains lane buffers across index rebuilds.</summary>
        private readonly Dictionary<int, List<T>> _rows = new Dictionary<int, List<T>>();
        /// <summary>Reads the native identifier.</summary>
        private readonly Func<T, int> _id;
        /// <summary>Reads the current lane.</summary>
        private readonly Func<T, int> _row;
        /// <summary>Attaches the index invalidation callback to an entry.</summary>
        private readonly Action<T, Action?> _attach;
        /// <summary>Reuses one callback for all active entries.</summary>
        private readonly Action _invalidate;
        /// <summary>Retains an empty candidate buffer for absent lanes.</summary>
        private readonly List<T> _empty = new List<T>();
        /// <summary>Records the index version currently built.</summary>
        private int _indexedVersion = -1;
        /// <summary>Gets the version changed by membership or indexed position updates.</summary>
        internal int Version { get; private set; }
        /// <summary>Gets the cached public read-only view.</summary>
        internal EntityView<T> View { get; }
        /// <summary>Creates the ordered entity owner.</summary>
        /// <param name="id">Reads the native identifier.</param>
        /// <param name="row">Reads the current lane.</param>
        /// <param name="attach">Attaches or detaches invalidation on indexed property changes.</param>
        internal EntityStore(Func<T, int> id, Func<T, int> row, Action<T, Action?> attach)
        {
            _id = id;
            _row = row;
            _attach = attach;
            _invalidate = Invalidate;
            View = new EntityView<T>(_items, _members);
        }
        /// <summary>Gets the number of live entries.</summary>
        public int Count => _items.Count;

        /// <summary>Reserves derived index storage during loading without changing ordered membership.</summary>
        /// <param name="capacity">The expected concurrent entity population.</param>
        /// <param name="rowCount">The number of native board rows.</param>
        internal void PrepareCapacity(int capacity, int rowCount)
        {
            if (_items.Capacity < capacity)
            {
                _items.Capacity = capacity;
            }
            _members.EnsureCapacity(capacity);
            _ids.EnsureCapacity(capacity);
            _rows.EnsureCapacity(rowCount);
            for (var row = 0; row < rowCount; row++)
            {
                if (!_rows.TryGetValue(row, out var candidates))
                {
                    candidates = new List<T>(capacity);
                    _rows.Add(row, candidates);
                }
                else if (candidates.Capacity < capacity)
                {
                    candidates.Capacity = capacity;
                }
            }
        }
        /// <summary>Gets a live entry in update order.</summary>
        /// <param name="index">The ordered index.</param>
        /// <returns>The selected entry.</returns>
        public T this[int index] => _items[index];
        /// <summary>Invalidates derived indexes after an indexed mutation.</summary>
        private void Invalidate()
        {
            Version++;
        }
        /// <summary>Adds an entry once, preserving update order.</summary>
        /// <param name="item">The new live entry.</param>
        /// <exception cref="ArgumentException">The reference is already active.</exception>
        internal void Add(T item)
        {
            if (!_members.Add(item))
            {
                throw new ArgumentException("The entity is already active.", nameof(item));
            }
            _items.Add(item);
            _attach(item, _invalidate);
            Invalidate();
        }
        /// <summary>Removes an entry without introducing gameplay side effects.</summary>
        /// <param name="item">The retiring entry.</param>
        /// <returns>Whether the entry was active.</returns>
        internal bool Remove(T item)
        {
            if (!_members.Remove(item))
            {
                return false;
            }
            _items.Remove(item);
            _attach(item, null);
            Invalidate();
            return true;
        }
        /// <summary>Removes the entry at an ordered index.</summary>
        /// <param name="index">The retiring index.</param>
        internal void RemoveAt(int index)
        {
            var item = _items[index];
            _items.RemoveAt(index);
            _members.Remove(item);
            _attach(item, null);
            Invalidate();
        }
        /// <summary>Retires every entry without death or loot callbacks.</summary>
        internal void Clear()
        {
            foreach (var item in _items)
            {
                _attach(item, null);
            }
            _items.Clear();
            _members.Clear();
            Invalidate();
        }
        /// <summary>Checks live reference membership in constant expected time.</summary>
        /// <param name="item">The entry to inspect.</param>
        /// <returns>Whether it remains active.</returns>
        internal bool Contains(T item)
        {
            return _members.Contains(item);
        }
        /// <summary>Removes matching entries while preserving survivor order.</summary>
        /// <param name="predicate">The removal condition.</param>
        /// <returns>The removed count.</returns>
        internal int RemoveAll(Predicate<T> predicate)
        {
            var removed = 0;
            for (var i = 0; i < _items.Count;)
            {
                if (predicate(_items[i]))
                {
                    RemoveAt(i);
                    removed++;
                }
                else
                {
                    i++;
                }
            }
            return removed;
        }
        /// <summary>Finds the first matching entry in native order.</summary>
        /// <param name="predicate">The selection condition.</param>
        /// <returns>The first match or null.</returns>
        internal T? Find(Predicate<T> predicate)
        {
            return _items.Find(predicate);
        }
        /// <summary>Finds the last matching entry in native order.</summary>
        /// <param name="predicate">The selection condition.</param>
        /// <returns>The last match or null.</returns>
        internal T? FindLast(Predicate<T> predicate)
        {
            return _items.FindLast(predicate);
        }
        /// <summary>Checks whether any active entry matches.</summary>
        /// <param name="predicate">The selection condition.</param>
        /// <returns>Whether a match exists.</returns>
        internal bool Exists(Predicate<T> predicate)
        {
            return _items.Exists(predicate);
        }
        /// <summary>Checks a condition over all active entries.</summary>
        /// <param name="predicate">The condition.</param>
        /// <returns>Whether all entries match.</returns>
        internal bool TrueForAll(Predicate<T> predicate)
        {
            return _items.TrueForAll(predicate);
        }
        /// <summary>Counts matching entries without creating a result list.</summary>
        /// <param name="predicate">The selection condition.</param>
        /// <returns>The matching count.</returns>
        internal int CountMatching(Predicate<T> predicate)
        {
            var count = 0;
            foreach (var item in _items)
            {
                if (predicate(item))
                {
                    count++;
                }
            }
            return count;
        }
        /// <summary>Finds the first active entry for a native identifier.</summary>
        /// <param name="id">The native identifier, including zero for legacy fixtures.</param>
        /// <returns>The active entry or null.</returns>
        internal T? FindById(int id)
        {
            SynchronizeIndexes();
            return _ids.TryGetValue(id, out var item) ? item : null;
        }
        /// <summary>Gets lane candidates in their original global list order.</summary>
        /// <param name="row">The requested lane.</param>
        /// <returns>The retained candidate list; callers must not mutate it.</returns>
        internal List<T> InRow(int row)
        {
            SynchronizeIndexes();
            return _rows.TryGetValue(row, out var items) ? items : _empty;
        }
        /// <summary>Rebuilds invalidated indexes without allocating warmed buffers.</summary>
        private void SynchronizeIndexes()
        {
            if (_indexedVersion == Version)
            {
                return;
            }
            _ids.Clear();
            foreach (var row in _rows.Values)
            {
                row.Clear();
            }
            foreach (var item in _items)
            {
                var id = _id(item);
                if (!_ids.ContainsKey(id))
                {
                    _ids.Add(id, item);
                }
                var lane = _row(item);
                if (!_rows.TryGetValue(lane, out var row))
                {
                    row = new List<T>();
                    _rows.Add(lane, row);
                }
                row.Add(item);
            }
            _indexedVersion = Version;
        }
        /// <summary>Enumerates live entries in update order without boxing.</summary>
        /// <returns>The ordered value-type enumerator.</returns>
        public List<T>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }
        /// <summary>Enumerates live entries through the collection interface.</summary>
        /// <returns>The ordered enumerator.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }
        /// <summary>Enumerates live entries through the nongeneric interface.</summary>
        /// <returns>The ordered enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
