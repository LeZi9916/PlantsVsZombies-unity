#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Enumerates a mutation-safe list snapshot using a temporary pooled array.</summary>
    /// <typeparam name="T">The reference type stored in the source list.</typeparam>
    internal readonly struct PooledSnapshot<T> where T : class
    {
        /// <summary>The list copied when enumeration begins.</summary>
        private readonly List<T> _source;

        /// <summary>Captures the source without allocating a snapshot until enumeration begins.</summary>
        /// <param name="source">The list whose original ordering and membership must be preserved.</param>
        public PooledSnapshot(List<T> source)
        {
            _source = source;
        }

        /// <summary>Copies the current entries into an independently owned pooled buffer.</summary>
        /// <returns>A value-type enumerator that returns its buffer even when the loop exits early.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_source);
        }

        /// <summary>Owns a single snapshot so nested iterations cannot overwrite each other's entries.</summary>
        internal struct Enumerator : IDisposable
        {
            /// <summary>The rented buffer, or null after disposal.</summary>
            private T[]? _items;
            /// <summary>The number of source entries copied into the buffer.</summary>
            private readonly int _count;
            /// <summary>The current position within the copied entries.</summary>
            private int _index;

            /// <summary>Copies the source entries into a pooled buffer.</summary>
            /// <param name="source">The list whose entries are captured.</param>
            internal Enumerator(List<T> source)
            {
                _count = source.Count;
                _items = _count == 0 ? null : ArrayPool<T>.Shared.Rent(_count);
                _index = -1;
                if (_items != null)
                {
                    source.CopyTo(_items);
                }
            }

            /// <summary>Gets the current entry from the original list snapshot.</summary>
            public T Current => _items![_index];

            /// <summary>Advances within the captured entries without consulting the mutated source.</summary>
            /// <returns>True when another original entry is available.</returns>
            public bool MoveNext()
            {
                return ++_index < _count;
            }

            /// <summary>Clears retained references and returns the rented array exactly once.</summary>
            public void Dispose()
            {
                if (_items == null)
                {
                    return;
                }

                Array.Clear(_items, 0, _count);
                ArrayPool<T>.Shared.Return(_items);
                _items = null;
            }
        }
    }
}
