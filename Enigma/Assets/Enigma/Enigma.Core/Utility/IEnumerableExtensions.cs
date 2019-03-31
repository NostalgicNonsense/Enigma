using System.Collections.Generic;

namespace Assets.Enigma.Enigma.Core.Utility
{
    public static class IEnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            var hashset = new HashSet<T>();
            foreach (var var in source)
            {
                hashset.Add(var);
            }

            return hashset;
        }
    }
}
