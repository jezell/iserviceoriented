using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public interface IReadOnlyDictionary<K,V> : IEnumerable<KeyValuePair<K,V>>
    {
        IEnumerable<K> Keys
        {
            get;
        }

        IEnumerable<V> Values
        {
            get;
        }

        V this[K key]
        {
            get;
        }

        int Count
        {
            get;
        }

        bool ContainsKey(K key);
        bool Contains(KeyValuePair<K,V> value);

    }

    public static class ReadOnlyDictionaryExtensions
    {
        public static Dictionary<K, V> ToDictionary<K,V>(this IReadOnlyDictionary<K, V> readOnlyDictionary)
        {
            Dictionary<K, V> dict = new Dictionary<K, V>();
            foreach (var kv in readOnlyDictionary)
            {
                dict.Add(kv.Key, kv.Value);
            }
            return dict;
        }

        public static ReadOnlyDictionary<K, V> MakeReadOnly<K, V>(this IDictionary<K, V> dictionary)
        {
            return dictionary.MakeReadOnly(false);
        }
        public static ReadOnlyDictionary<K, V> MakeReadOnly<K,V>(this IDictionary<K, V> dictionary, bool copy)
        {
            return new ReadOnlyDictionary<K, V>(dictionary, copy);
        }
    }
    
}
