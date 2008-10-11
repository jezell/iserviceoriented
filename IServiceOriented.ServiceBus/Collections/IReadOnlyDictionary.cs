using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Collections
{
    /// <summary>
    /// Represents a read only collection of keys and values.
    /// </summary>
    /// <typeparam name="K">Key type</typeparam>
    /// <typeparam name="V">Value type</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public interface IReadOnlyDictionary<TKey,TValue> : IEnumerable<KeyValuePair<TKey,TValue>>
    {
        /// <summary>
        /// Get the keys associated with this dictionary instance.
        /// </summary>
        IEnumerable<TKey> Keys
        {
            get;
        }

        /// <summary>
        /// Get the values contained by this dictionary instance.
        /// </summary>
        IEnumerable<TValue> Values
        {
            get;
        }

        /// <summary>
        /// Get the value associated with a specific key.
        /// </summary>
        TValue this[TKey key]
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
        bool ContainsKey(TKey key);
        /// <summary>
        /// Check to see if the dictionary contains a specific key value pair.
        /// </summary>
        bool Contains(KeyValuePair<TKey, TValue> value);

    }

    /// <summary>
    /// Extension methods for read only dicionaries
    /// </summary>
    public static class ReadOnlyDictionaryExtensions
    {
        /// <summary>
        /// Convert a read only dictionary to a writeable dictionary.
        /// </summary>
        public static Dictionary<TKey, TValue> ToDictionary<TKey,TValue>(this IReadOnlyDictionary<TKey, TValue> readOnlyDictionary)
        {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            foreach (var kv in readOnlyDictionary)
            {
                dict.Add(kv.Key, kv.Value);
            }
            return dict;
        }

        /// <summary>
        /// Convert a dictionary to a read only dictionary.
        /// </summary>
        public static ReadOnlyDictionary<TKey, TValue> MakeReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return dictionary.MakeReadOnly(false);
        }

        /// <summary>
        /// Convert a dictionary to a read only dictionary.
        /// </summary>
        /// <param name="copy">Specifies if the dictionary should be copied (true) or wrapped (false).</param>
        public static ReadOnlyDictionary<TKey, TValue> MakeReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, bool copy)
        {
            return new ReadOnlyDictionary<TKey, TValue>(dictionary, copy);
        }
    }
    
}
