using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Represents a read only collection of keys and values.
    /// </summary>
    /// <typeparam name="K">Key type</typeparam>
    /// <typeparam name="V">Value type</typeparam>
    [Serializable]
    [CollectionDataContract]
    public class ReadOnlyDictionary<K, V> : IReadOnlyDictionary<K, V>
    {
        IDictionary<K, V> _dictionary;

        public ReadOnlyDictionary()
            : this(new Dictionary<K,V>(), false)
        {
            
        }

        public ReadOnlyDictionary(IEnumerable<KeyValuePair<K, V>> pairs)
        {
            _dictionary = new Dictionary<K, V>();
            foreach (KeyValuePair<K, V> p in pairs)
            {
                _dictionary.Add(p);
            }
        }

        public ReadOnlyDictionary(IDictionary<K, V> from)
            : this(from, false)
        {
        }

        private void Add(KeyValuePair<K,V> pair)
        {
            _dictionary.Add(pair.Key, pair.Value);
        }

        public ReadOnlyDictionary(IDictionary<K, V> from, bool copy)
        {
            if (copy)
            {
                _dictionary = new Dictionary<K, V>(from);
            }
            else
            {
                _dictionary = from;
            }
        }

        #region IReadOnlyDictionary<K,V> Members

        public IEnumerable<K> Keys
        {
            get { return _dictionary.Keys; }
        }

        public IEnumerable<V> Values
        {
            get { return _dictionary.Values; }
        }

        public V this[K key]
        {
            get { return _dictionary[key]; }
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool ContainsKey(K key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<K, V> value)
        {
            return _dictionary.Contains(value);
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        #endregion
    }
}
