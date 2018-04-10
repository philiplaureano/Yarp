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

        public static void BlockUntilAny<T>(this IEnumerable<T> items, Func<T, bool> condition, TimeSpan timeout)
        {
            items.BlockUntilAny(condition, delegate { }, timeout);
        }

        public static void BlockUntilAny<T>(this IEnumerable<T> items, Func<T, bool> condition, Action timeoutHandler,
            TimeSpan timeout)
        {
            var timeStarted = DateTime.UtcNow;

            // Add a timeout to the existing condition
            bool ModifiedCondition(T item)
            {
                var timeElapsed = DateTime.UtcNow - timeStarted;
                var hasTimeExpired = timeElapsed > timeout;
                if (hasTimeExpired)
                {
                    // Call the handler if the timeout occurs
                    timeoutHandler();
                    return false;
                }

                return condition(item);
            }

            items.BlockUntilAny(ModifiedCondition);
        }

        public static void BlockUntilAny<T>(this IEnumerable<T> items, Func<T, bool> condition)
        {
            var blockUntilAny = CreateBlocker<T>(Enumerable.Any);
            blockUntilAny(items, condition);
        }

        public static void BlockUntilAll<T>(this IEnumerable<T> items, Func<T, bool> condition)
        {
            var blockUntilAll = CreateBlocker<T>(Enumerable.All);
            blockUntilAll(items, condition);
        }

        private static Action<IEnumerable<T>, Func<T, bool>> CreateBlocker<T>(
            Func<IEnumerable<T>, Func<T, bool>, bool> conditionalTest)
        {
            return (items, condition) =>
            {
                var currentItems = items.ToArray();
                while (!conditionalTest(currentItems, condition))
                {
                    /* Block the thread */
                }
            };
        }

        public static void ShouldHaveAtLeastOne<T>(this IEnumerable<T> items, Func<T, bool> condition)
        {
            Assert.True(items.Count(condition) > 0);
        }
    }
}