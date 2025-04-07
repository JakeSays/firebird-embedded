using System;
using System.Collections.Generic;


#pragma warning disable 8714

namespace Std.CommandLine.Utility
{
    internal static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> source,
            TKey key,
            Func<TKey, TValue> create)
        {
            if (source.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                value = create(key);

                source.Add(key, value);

                return value;
            }
        }
    }
}
#pragma warning restore 8714
