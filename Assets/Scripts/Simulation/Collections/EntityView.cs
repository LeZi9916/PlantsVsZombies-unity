#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Provides an allocation-free, read-only live view of ordered entities.</summary>
    /// <typeparam name="T">The observed entity type.</typeparam>
    public sealed class EntityView<T> : IReadOnlyList<T>
    {
        /// <summary>Stores the owner's ordered entries.</summary>
        private readonly List<T> _items;
        /// <summary>Tracks live membership supplied by the owner.</summary>
        private readonly HashSet<T>? _members;
        /// <summary>Creates a view without copying the owner's list.</summary>
        /// <param name="items">The privately owned list.</param>
        /// <param name="members">The owner's membership index.</param>
        internal EntityView(List<T> items, HashSet<T>? members = null)
        {
            _items = items;
            _members = members;
        }
        /// <summary>Checks whether an entity remains active.</summary>
        /// <param name="item">The entity reference.</param>
        /// <returns>Whether the entity remains active.</returns>
        public bool Contains(T item)
        {
            return _members != null ? _members.Contains(item) : _items.Contains(item);
        }
        /// <summary>Checks whether a live entry satisfies a read-only predicate.</summary>
        /// <param name="predicate">The selection condition.</param>
        /// <returns>Whether any entry matches.</returns>
        public bool Exists(Predicate<T> predicate)
        {
            return _items.Exists(predicate);
        }
        /// <summary>Copies current entries for an explicitly requested independent snapshot.</summary>
        /// <returns>A new ordered array.</returns>
        public T[] ToArray()
        {
            return _items.ToArray();
        }
        /// <summary>Checks whether all live entries satisfy a read-only condition.</summary>
        /// <param name="predicate">The condition evaluated in update order.</param>
        /// <returns>Whether every active entry matches.</returns>
        public bool TrueForAll(Predicate<T> predicate)
        {
            return _items.TrueForAll(predicate);
        }
        /// <summary>Gets the active entry count.</summary>
        public int Count => _items.Count;
        /// <summary>Gets the entry at an update-order index.</summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The active entry.</returns>
        public T this[int index] => _items[index];
        /// <summary>Enumerates entries without boxing the list enumerator.</summary>
        /// <returns>The ordered value-type enumerator.</returns>
        public List<T>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }
        /// <summary>Enumerates entries through the collection interface.</summary>
        /// <returns>The ordered enumerator.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }
        /// <summary>Enumerates entries through the nongeneric interface.</summary>
        /// <returns>The ordered enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
