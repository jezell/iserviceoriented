using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Collections;

using System.Runtime.Serialization;
using System.Collections.ObjectModel;

namespace IServiceOriented.ServiceBus
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), CollectionDataContract]
    [Serializable]
    [KnownType(typeof(int))]
    [KnownType(typeof(string))] 
    [KnownType(typeof(Int64))] 
    [KnownType(typeof(Int16))]
    [KnownType(typeof(Guid))]
    [KnownType(typeof(decimal))] 
    [KnownType(typeof(float))]
    [KnownType(typeof(double))]
    [KnownType(typeof(ReadOnlyCollection<int>))] 
    [KnownType(typeof(ReadOnlyCollection<string>))] 
    [KnownType(typeof(ReadOnlyCollection<Int64>))] 
    [KnownType(typeof(ReadOnlyCollection<Int16>))]
    [KnownType(typeof(ReadOnlyCollection<Guid>))]
    [KnownType(typeof(ReadOnlyCollection<decimal>))]
    [KnownType(typeof(ReadOnlyCollection<float>))]
    [KnownType(typeof(ReadOnlyCollection<double>))]
    public class MessageDeliveryContext : IReadOnlyDictionary<MessageDeliveryContextKey, object>
    {
        public MessageDeliveryContext()
        {
        }

        public MessageDeliveryContext(IDictionary<MessageDeliveryContextKey, object> dictionary)
        {
            foreach (KeyValuePair<MessageDeliveryContextKey, object> pair in dictionary)
            {
                Add(pair);
            }
        }

        public MessageDeliveryContext(IReadOnlyDictionary<MessageDeliveryContextKey, object> dictionary)            
        {
            foreach (KeyValuePair<MessageDeliveryContextKey, object> pair in dictionary)
            {
                Add(pair);
            }
        }

        public MessageDeliveryContext(KeyValuePair<MessageDeliveryContextKey, object>[] pairs)            
        {
            foreach (KeyValuePair<MessageDeliveryContextKey, object> pair in pairs)
            {
                Add(pair);
            }
        }

        private void Add(KeyValuePair<MessageDeliveryContextKey, object> pair)
        {
            _dictionary.Add(pair);
        }

        IDictionary<MessageDeliveryContextKey, object> _dictionary = new Dictionary<MessageDeliveryContextKey, object>();

        #region IReadOnlyDictionary<string,object> Members

        public IEnumerable<MessageDeliveryContextKey> Keys
        {
            get { return _dictionary.Keys; }
        }

        public IEnumerable<object> Values
        {
            get { return _dictionary.Values; }
        }

        public object this[MessageDeliveryContextKey key]
        {
            get { return _dictionary[key]; }
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool ContainsKey(MessageDeliveryContextKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<MessageDeliveryContextKey, object> value)
        {
            return _dictionary.Contains(value);
        }

        #endregion

        #region IEnumerable<KeyValuePair<MessageDeliveryContextKey,object>> Members

        public IEnumerator<KeyValuePair<MessageDeliveryContextKey, object>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        #endregion
    }

    [Serializable]
    [DataContract]
    public class MessageDeliveryContextKey
    {        
        public MessageDeliveryContextKey(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty");            
              
            _name = name;
            _namespace = String.Empty;
        }

        public MessageDeliveryContextKey(string name, string ns) : this(name)
        {
            if (ns == null) throw new ArgumentException("Namespace cannot be null");

            _namespace = ns ?? String.Empty;
        }

        [DataMember(Name="Name")]
        string _name;        
        
        public string Name
        {
            get
            {
                return _name;
            }
        }

        [DataMember(Name = "Namespace")]
        string _namespace;

        
        public string Namespace
        {
            get
            {
                return _namespace;
            }
        }

        public string FullName
        {
            get
            {
                return Namespace + Name;
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + Namespace.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            MessageDeliveryContextKey key = obj as MessageDeliveryContextKey;
            if (key != null)
            {
                return Name.Equals(key.Name) && Namespace.Equals(key.Namespace);
            }
            return false;
        }

        public static bool operator==(MessageDeliveryContextKey key1, MessageDeliveryContextKey key2)
        {
            
            if (Object.ReferenceEquals(key1, null))
            {
                return Object.ReferenceEquals(key2, null);
            }
            else
            {
                return key1.Equals(key2);
            }
        }

        public static bool operator!=(MessageDeliveryContextKey key1, MessageDeliveryContextKey key2)
        {
            if (Object.ReferenceEquals(key1 , null))
            {
                return !Object.ReferenceEquals(key2, null);
            }
            else
            {
                return !key1.Equals(key2);
            }
        }
    }
}
