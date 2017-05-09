﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Extension & utility methods for the <see cref="IEnumerable{T}" /> interface.
    /// </summary>
    [PublicAPI]
    public static class EnumerableUtility
    {
        private const string NULL = "null";
        private const string DEFAULT_JOINER = ", ";

        /// <summary>
        ///     Converts an enumerable of objects to a string using the <see cref="object.ToString()" /> method.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="separator">The separator between values.</param>
        /// <param name="enumerable">The enumerable to convert.</param>
        /// <returns>The joined string.</returns>
        /// <seealso cref="JoinToString{T}(IEnumerable{T}, string,Func{T,string})" />
        [DebuggerStepThrough]
        public static string JoinToString<T>(this IEnumerable<T> enumerable, string separator = DEFAULT_JOINER)
        {
            StringBuilder builder = new StringBuilder();
            foreach (T element in enumerable) {
                if (builder.Length != 0) {
                    builder.Append(separator);
                }
                builder.Append(element != null ? element.ToString() : NULL);
            }
            return builder.ToString();
        }

        /// <summary>
        ///     Converts an enumerable of objects to a string using a conversion delegate.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="separator">The separator between values.</param>
        /// <param name="enumerable">The enumerable to convert.</param>
        /// <param name="obtainer">The delegate used to convert.</param>
        /// <returns>The joined string.</returns>
        /// <seealso cref="JoinToString{T}(IEnumerable{T}, string)" />
        [DebuggerStepThrough]
        public static string JoinToString<T>(this IEnumerable<T> enumerable, string separator, Func<T, string> obtainer)
        {
            StringBuilder builder = new StringBuilder();
            foreach (T element in enumerable) {
                if (builder.Length != 0) {
                    builder.Append(separator);
                }
                builder.Append(obtainer(element));
            }
            return builder.ToString();
        }

        /// <summary>
        ///     Converts an enumerable of objects to a string using a conversion delegate.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="enumerable">The enumerable to convert.</param>
        /// <param name="obtainer">The delegate used to convert.</param>
        /// <returns>The joined string.</returns>
        /// <seealso cref="JoinToString{T}(IEnumerable{T}, string)" />
        [DebuggerStepThrough]
        public static string JoinToString<T>(this IEnumerable<T> enumerable, Func<T, string> obtainer)
        {
            return JoinToString(enumerable, DEFAULT_JOINER, obtainer);
        }

        /// <summary>
        ///     Removes all elements matching the predicate from this collection.
        /// </summary>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <param name="collection">The collection to remove from.</param>
        /// <param name="predicate">The removal predicate.</param>
        /// <returns>The amount of elements removed.</returns>
        public static int RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            if (collection.Count == 0) {
                return 0;
            }
            List<T> remove = collection.Where(predicate).ToList();
            int removed = 0;
            foreach (T toRemove in remove) {
                if (collection.Remove(toRemove)) {
                    removed++;
                }
            }
            return removed;
        }
    }
}