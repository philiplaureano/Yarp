using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests
{
    public static class CollectionTestingExtensions
    {
        public static IEnumerable<TOutput> CastAs<TOutput>(this IEnumerable<object> items)
        {
            return items.Where(item => item is TOutput).Cast<TOutput>();
        }

        public static void BlockUntilAll<T>(this IEnumerable<T> items, Func<T, bool> condition)
        {
            var currentItems = items.ToArray();
            while (!currentItems.All(condition))
            {
                /* Block the thread */
            }
        }

        public static void BlockUntilAny<T>(this IEnumerable<T> items, Func<T, bool> condition)
        {
            var currentItems = items.ToArray();
            while (!currentItems.Any(condition))
            {
                /* Block the thread */
            }
        }

        public static void ShouldHaveAtLeastOne<T>(this IEnumerable<T> items, Func<T, bool> condition)
        {
            Assert.True(items.Count(condition) > 0);
        }
    }
}