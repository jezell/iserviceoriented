using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Threading;

namespace IServiceOriented.ServiceBus.Collections
{
    /// <summary>
    /// Represents a read only collection of keys and values.
    /// </summary>
    /// <typeparam name="K">Key type</typeparam>
    /// <typeparam name="V">Value type</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), Serializable]
    [CollectionDataContract]
    public class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        IDictionary<TKey, TValue> _dictionary;

        public ReadOnlyDictionary()
            : this(new Dictionary<TKey,TValue>(), false)
        {
            
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ReadOnlyDictionary(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            _dictionary = new Dictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> p in pairs)
            {
                _dictionary.Add(p);
            }
        }

        public ReadOnlyDictionary(IDictionary<TKey, TValue> from)
            : this(from, false)
        {
        }


        private void Add(KeyValuePair<TKey,TValue> pair)
        {
            _dictionary.Add(pair.Key, pair.Value);
        }

        public ReadOnlyDictionary(IDictionary<TKey, TValue> from, bool copy)
        {
            if (copy)
            {
                _dictionary = new Dictionary<TKey, TValue>(from);
            }
            else
            {
                _dictionary = from;
            }
        }

        #region IReadOnlyDictionary<K,V> Members

        public IEnumerable<TKey> Keys
        {
            get { return _dictionary.Keys; }
        }

        public IEnumerable<TValue> Values
        {
            get { return _dictionary.Values; }
        }

        public TValue this[TKey key]
        {
            get { return _dictionary[key]; }
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> value)
        {
            return _dictionary.Contains(value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
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
