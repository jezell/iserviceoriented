using System;
namespace IServiceOriented.ServiceBus.Data
{
    public interface ISubscriptionDB
    {
        void CreateListener(ListenerEndpoint endpoint);
        void CreateSubscription(SubscriptionEndpoint subscription);
        void DeleteListener(Guid endpointId);
        void DeleteSubscription(Guid id);
        System.Collections.Generic.IEnumerable<ListenerEndpoint> LoadListenerEndpoints();
        System.Collections.Generic.IEnumerable<SubscriptionEndpoint> LoadSubscriptionEndpoints();
    }
}
