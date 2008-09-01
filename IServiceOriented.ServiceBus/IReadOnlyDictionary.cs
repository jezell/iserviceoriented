using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Represents a read only collection of keys and values.
    /// </summary>
    /// <typeparam name="K">Key type</typeparam>
    /// <typeparam name="V">Value type</typeparam>
    public interface IReadOnlyDictionary<K,V> : IEnumerable<KeyValuePair<K,V>>
    {
        /// <summary>
        /// Get the keys associated with this dictionary instance.
        /// </summary>
        IEnumerable<K> Keys
        {
            get;
        }

        /// <summary>
        /// Get the values contained by this dictionary instance.
        /// </summary>
        IEnumerable<V> Values
        {
            get;
        }

        /// <summary>
        /// Get the value associated with a specific key.
        /// </summary>
        V this[K key]
        {
            get;
        }

        /// <summary>
        /// Get the number of items contained by this dictionary.
        /// </summary>
        int Count
        {
            get;
        }

        /// <summary>
        /// Check to see if the dictionary contains a specific key.
        /// </summary>
        bool ContainsKey(K key);
        /// <summary>
        /// Check to see if the dictionary contains a specific key value pair.
        /// </summary>
        bool Contains(KeyValuePair<K, V> value);

    }

    /// <summary>
    /// Extension methods for read only dicionaries
    /// </summary>
    public static class ReadOnlyDictionaryExtensions
    {
        /// <summary>
        /// Convert a read only dictionary to a writeable dictionary.
        /// </summary>
        public static Dictionary<K, V> ToDictionary<K,V>(this IReadOnlyDictionary<K, V> readOnlyDictionary)
        {
            Dictionary<K, V> dict = new Dictionary<K, V>();
            foreach (var kv in readOnlyDictionary)
            {
                dict.Add(kv.Key, kv.Value);
            }
            return dict;
        }

        /// <summary>
        /// Convert a dictionary to a read only dictionary.
        /// </summary>
        public static ReadOnlyDictionary<K, V> MakeReadOnly<K, V>(this IDictionary<K, V> dictionary)
        {
            return dictionary.MakeReadOnly(false);
        }

        /// <summary>
        /// Convert a dictionary to a read only dictionary.
        /// </summary>
        /// <param name="copy">Specifies if the dictionary should be copied (true) or wrapped (false).</param>
        public static ReadOnlyDictionary<K, V> MakeReadOnly<K, V>(this IDictionary<K, V> dictionary, bool copy)
        {
            return new ReadOnlyDictionary<K, V>(dictionary, copy);
        }
    }
    
}
