using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DistributedServiceProvider.Base.Extensions
{
    /// <summary>
    /// Extensions to the IEnumerable interface
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Appends an enumeration after another
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="before">The before.</param>
        /// <param name="after">The after.</param>
        /// <returns></returns>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> before, IEnumerable<T> after)
        {
            foreach (var item in before)
                yield return item;
            foreach (var item in after)
                yield return item;
        }

        /// <summary>
        /// Appends a single item to an enumeration
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="before">The before.</param>
        /// <param name="after">The after.</param>
        /// <returns></returns>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> before, T after)
        {
            foreach (var item in before)
                yield return item;
            yield return after;
        }

        public static IEnumerable<T> OrderWithComparer<T>(this IEnumerable<T> enumerable, Comparer<T> comparison)
        {
            List<T> list = new List<T>(enumerable);

            list.Sort(comparison);

            foreach (var item in list)
                yield return item;
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> func)
        {
            foreach (var item in collection)
                func(item);
        }
    }
}
