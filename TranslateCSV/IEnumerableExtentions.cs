using System;
using System.Collections.Generic;
using System.Text;

namespace TranslateCSV
{
    public static class IEnumerableExtentions
    {
        public static IDictionary<TKey, TValue> ToDictionaryUnique<TElement, TKey, TValue>(
                this IEnumerable<TElement> source,
                Func<TElement, TKey> keyGetter,
                Func<TElement, TValue> valueGetter)
        {
            IDictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            foreach (var e in source)
            {
                var key = keyGetter(e);
                if (dict.ContainsKey(key)) dict[key] = valueGetter(e);
                else                       dict.Add(key, valueGetter(e));
            }
            return dict;
        }
    }
}
