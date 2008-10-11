using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Globalization;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// A collection of listener endpoints.
    /// </summary>
    public class ListenerEndpointCollection : Collection<ListenerEndpoint>
    {
        void checkItem(ListenerEndpoint item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            foreach (ListenerEndpoint endpoint in this)
            {
                if(endpoint == item)
                {
                    throw new InvalidOperationException("This listener endpoint has already been added.");
                }
                if (item.Id == endpoint.Id)
                {
                    throw new DuplicateIdentifierException(String.Format(CultureInfo.InvariantCulture, "The id {0} is associated with another endpoint.", endpoint.Id));
                }
            }
        }   
     
        protected override void InsertItem(int index, ListenerEndpoint item)
        {
            checkItem(item);
            base.InsertItem(index, item);
            _fastLookup.Add(item.Id, item);
        }

        protected override void SetItem(int index, ListenerEndpoint item)
        {
            checkItem(item);
            ListenerEndpoint oldItem = this[index];            
            base.SetItem(index, item);
            _fastLookup.Remove(oldItem.Id);
            _fastLookup.Add(item.Id, item);
        }

        protected override void RemoveItem(int index)
        {
            ListenerEndpoint item = this[index];
            base.RemoveItem(index);
            _fastLookup.Remove(item.Id);
        }

        Dictionary<Guid, ListenerEndpoint> _fastLookup = new Dictionary<Guid, ListenerEndpoint>();

        /// <summary>
        /// Gets the listener with the specified ID.
        /// </summary>
        /// <param name="value">ID of the listener to get</param>
        public ListenerEndpoint Find(Guid value)
        {
            ListenerEndpoint endpoint;

            _fastLookup.TryGetValue(value, out endpoint);

            return endpoint;
        }
    }
}
