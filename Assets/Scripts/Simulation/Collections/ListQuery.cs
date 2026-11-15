#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Provides allocation-free result counting for ordered read-only lists.</summary>
    internal static class ListQuery
    {
        /// <summary>Counts matching entries without allocating a filtered result list.</summary>
        /// <typeparam name="T">The entry type.</typeparam>
        /// <param name="items">The ordered source.</param>
        /// <param name="predicate">The original selection condition.</param>
        /// <returns>The number of matching entries.</returns>
        internal static int CountMatching<T>(this IReadOnlyList<T> items, Predicate<T> predicate)
        {
            var count = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (predicate(items[i]))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
