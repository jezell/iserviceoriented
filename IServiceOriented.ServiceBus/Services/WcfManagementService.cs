using System;
using System.Transactions;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.Reflection;
using IServiceOriented.ServiceBus.Dispatchers;
using IServiceOriented.ServiceBus.Listeners;

namespace IServiceOriented.ServiceBus.Services
{
    internal static class WcfManagementServiceActions
    {
        public const string Subscribe = "urn:SubscribeRequest";
        public const string Unsubscribe = "urn:UnsubscribeRequest";
        public const string Listen = "urn:ListenRequest";
        public const string StopListening = "urn:StopListeningRequest";

        public const string ListListeners = "urn:ListListeners";
        public const string ListListenersResponse = "urn:ListListenersResponse";

        public const string ListSubscribers = "urn:ListSubscribers";
        public const string ListSubscribersResponse = "urn:ListSubscribersResponse";        
    }

    public sealed class WcfManagementService : RuntimeService
    {        
        protected override void OnStart()
        {
            base.OnStart();

            _host = new ServiceHost(new ServiceBusManagementService(Runtime));
            _host.Open();
            
        }

        ServiceHost _host;

        protected override void OnStop()
        {
            base.OnStop();
            if(_host != null) _host.Close();
            _host = null;
        }
    }


    [ServiceContract]
    [ServiceKnownType(typeof(WcfDispatcher))]
    [ServiceKnownType(typeof(WcfListener))]
    [ServiceKnownType(typeof(TypedMessageFilter))]
    [ServiceKnownType(typeof(UnhandledMessageFilter))]
    [ServiceKnownType(typeof(WcfProxyDispatcher))]
    [ServiceKnownType(typeof(WcfServiceHostListener))]
    public interface IServiceBusManagementService
    {
        [OperationContract(Action = WcfManagementServiceActions.Subscribe)]
        void Subscribe([MessageParameter(Name = "SubscriptionEndpoint")] SubscriptionEndpoint subscription);

        [OperationContract(Action = WcfManagementServiceActions.Unsubscribe)]
        [FaultContract(typeof(SubscriptionNotFoundFault))]
        void Unsubscribe([MessageParameter(Name = "SubscriptionID")] Guid subscriptionId);

        [OperationContract(Action = WcfManagementServiceActions.Listen)]
        void Listen([MessageParameter(Name = "ListenerEndpoint")] ListenerEndpoint endpoint);

        [OperationContract(Action= WcfManagementServiceActions.StopListening)]
        [FaultContract(typeof(ListenerNotFoundFault))]
        void StopListening([MessageParameter(Name = "ListenerID")] Guid listenerId);

        [OperationContract(Action = WcfManagementServiceActions.ListListeners, ReplyAction = WcfManagementServiceActions.ListListenersResponse)]
        Collection<ListenerEndpoint> ListListeners();

        [OperationContract(Action = WcfManagementServiceActions.ListSubscribers, ReplyAction = WcfManagementServiceActions.ListSubscribersResponse)]        
        Collection<SubscriptionEndpoint> ListSubscribers();
    }

    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public sealed class ServiceBusManagementService : IServiceBusManagementService
    {
        public ServiceBusManagementService(ServiceBusRuntime runtime)
        {
            Runtime = runtime;
        }
        #region IServiceBusManagementService Members

        public ServiceBusRuntime Runtime { get; private set; }

        [OperationBehavior]
        public void Subscribe(SubscriptionEndpoint subscription)
        {
            Runtime.Subscribe(subscription);
        }

        [OperationBehavior]
        [FaultContract(typeof(SubscriptionNotFoundFault))]
        public void Unsubscribe(Guid subscriptionId)
        {
            try
            {
                Runtime.Unsubscribe(subscriptionId);
            }
            catch (SubscriptionNotFoundException)
            {
                throw new FaultException<SubscriptionNotFoundFault>(new SubscriptionNotFoundFault(subscriptionId));
            }
        }

        [OperationBehavior]
        public void Listen([MessageParameter(Name = "Endpoint")] ListenerEndpoint endpoint)
        {
            Runtime.AddListener(endpoint);
        }

        [OperationBehavior]
        public void StopListening(Guid listenerId)
        {
            try
            {
                Runtime.RemoveListener(listenerId);
            }
            catch (ListenerNotFoundException)
            {
                throw new FaultException<ListenerNotFoundFault>(new ListenerNotFoundFault(listenerId));
            }
        }
        
        [OperationBehavior]
        public Collection<SubscriptionEndpoint> ListSubscribers()
        {
            return Runtime.ListSubscribers(false);
        }

        [OperationBehavior]
        public Collection<ListenerEndpoint> ListListeners()
        {
            return Runtime.ListListeners(false);
        }

        #endregion
    }

    [DataContract]
    public sealed class ListenerNotFoundFault
    {
        public ListenerNotFoundFault()
        {
        }

        public ListenerNotFoundFault(Guid listenerId)
        {
            ListenerId = listenerId;
        }

        [DataMember]
        public Guid ListenerId
        {
            get;
            set;
        }
    }


    [DataContract]
    public sealed class SubscriptionNotFoundFault
    {
        public SubscriptionNotFoundFault()
        {
        }

        public SubscriptionNotFoundFault(Guid subscriptionId)
        {
            SubscriptionId = subscriptionId;
        }

        [DataMember]
        public Guid SubscriptionId
        {
            get;
            set;
        }
    }

    [DataContract]
    public sealed class MessageDeliveryNotFoundFault
    {
        public MessageDeliveryNotFoundFault()
        {
        }

        public MessageDeliveryNotFoundFault(string messageId)
        {
            MessageId = messageId;
        }

        [DataMember]
        public string MessageId     
        {
            get;
            set;
        }
    }

}
