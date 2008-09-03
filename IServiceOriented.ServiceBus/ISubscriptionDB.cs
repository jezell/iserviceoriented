using System;
namespace IServiceOriented.ServiceBus
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
