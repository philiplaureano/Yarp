using System.Collections.Generic;

namespace Tests
{
    public static class HashSetExtensions
    {
        public static ISet<T> ToHashSet<T>(this IEnumerable<T> items)
        {
            return new HashSet<T>(items);
        }        
    }
}