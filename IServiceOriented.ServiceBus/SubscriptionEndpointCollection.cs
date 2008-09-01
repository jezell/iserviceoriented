using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// A collection of subscription endpoints.
    /// </summary>
    public class SubscriptionEndpointCollection : Collection<SubscriptionEndpoint>
    {
        void checkItem(SubscriptionEndpoint item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            foreach (SubscriptionEndpoint endpoint in this)
            {
                if (endpoint == item)
                {
                    throw new InvalidOperationException("This subscription endpoint has already been added.");
                }
                if (item.Id == endpoint.Id)
                {
                    throw new DuplicateIdentifierException(String.Format("The id {0} is associated with another endpoint.", endpoint.Id));
                }
            }
        }

        protected override void InsertItem(int index, SubscriptionEndpoint item)
        {
            checkItem(item);
            base.InsertItem(index, item);
            _fastLookup.Add(item.Id, item);
        }

        protected override void SetItem(int index, SubscriptionEndpoint item)
        {
            checkItem(item);
            SubscriptionEndpoint oldItem = this[index];
            base.SetItem(index, item);
            _fastLookup.Remove(oldItem.Id);
            _fastLookup.Add(item.Id, item);
        }

        protected override void RemoveItem(int index)
        {
            SubscriptionEndpoint item = this[index];
            base.RemoveItem(index);
            _fastLookup.Remove(item.Id);
        }

        Dictionary<Guid, SubscriptionEndpoint> _fastLookup = new Dictionary<Guid, SubscriptionEndpoint>();

        /// <summary>
        /// Gets the subscription with the specified ID.
        /// </summary>
        /// <param name="value">ID of the subscription to get</param>        
        public SubscriptionEndpoint this[Guid value]
        {
            get
            {
                return _fastLookup[value];
            }
        }
    }
}
