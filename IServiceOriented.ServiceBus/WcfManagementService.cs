using System;
using System.Transactions;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.Reflection;

namespace IServiceOriented.ServiceBus
{
    internal static class WcfManagementServiceActions
    {
        public const string Subscribe = "urn:SubscribeRequest";
        public const string Unsubscribe = "urn:UnsubscribeRequest";
        public const string AddListener = "urn:AddListenerRequest";
        public const string RemoveListener = "urn:RemoveListenerRequest";

        public const string ListListeners = "urn:ListListeners";
        public const string ListListenersResponse = "urn:ListListenersResponse";

        public const string ListSubscribers = "urn:ListSubscribers";
        public const string ListSubscribersResponse = "urn:ListSubscribersResponse";

        public const string ListMessagesInFailureQueue = "urn:ListMessagesInFailureQueueRequest";
        public const string ListMessagesInFailureQueueResponse = "urn:ListMessagesInFailureQueueRequest";

        public const string RetryFailedMessageDelivery = "urn:RetryFailedMessageDelivery";
    }

    public class WcfManagementService : RuntimeService
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

    static class ServiceBusManagementServiceTypeProvider
    {
        public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider)
        {
            List<Type> knownTypes = new List<Type>();
            foreach (KnownTypeAttribute a in provider.GetCustomAttributes(typeof(KnownTypeAttribute), true).Cast<KnownTypeAttribute>())
            {
                if(a.Type != null)
                {
                    knownTypes.Add(a.Type);
                }
            }
            knownTypes.AddRange(MessageDelivery.GetKnownTypes());
            return knownTypes.ToArray();
        }
    }

    [ServiceContract]
    [ServiceKnownType("GetKnownTypes", typeof(ServiceBusManagementServiceTypeProvider))]
    public interface IServiceBusManagementService
    {
        [OperationContract(Action = WcfManagementServiceActions.Subscribe)]
        void Subscribe([MessageParameter(Name = "SubscriptionEndpoint")] SubscriptionEndpoint subscription);

        [OperationContract(Action = WcfManagementServiceActions.Unsubscribe)]
        [FaultContract(typeof(SubscriptionNotFoundFault))]
        void Unsubscribe([MessageParameter(Name = "SubscriptionID")] Guid subscriptionId);

        [OperationContract(Action = WcfManagementServiceActions.AddListener)]
        void AddListener([MessageParameter(Name = "ListenerEndpoint")] ListenerEndpoint endpoint);

        [OperationContract(Action= WcfManagementServiceActions.RemoveListener)]
        [FaultContract(typeof(ListenerNotFoundFault))]
        void RemoveListener([MessageParameter(Name = "ListenerID")] Guid listenerId);

        [OperationContract(Action = WcfManagementServiceActions.ListListeners, ReplyAction = WcfManagementServiceActions.ListListenersResponse)]
        Collection<ListenerEndpoint> ListListeners();

        [OperationContract(Action = WcfManagementServiceActions.ListSubscribers, ReplyAction = WcfManagementServiceActions.ListSubscribersResponse)]
        Collection<SubscriptionEndpoint> ListSubscribers();

        [OperationContract(Action = WcfManagementServiceActions.ListMessagesInFailureQueue, ReplyAction = WcfManagementServiceActions.ListMessagesInFailureQueueResponse)]
        [return: MessageParameter(Name="Messages")]
        Collection<MessageDelivery> ListMessagesInFailureQueue([MessageParameter(Name = "MaxResults")] int? maxResults, [MessageParameter(Name = "LastMessageID")]string lastMessageId);

        [OperationContract(Action = WcfManagementServiceActions.RetryFailedMessageDelivery)]
        [FaultContract(typeof(MessageDeliveryNotFoundFault))]
        void RetryFailedMessageDelivery(string messageId);
    }

    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public class ServiceBusManagementService : IServiceBusManagementService
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
        public void AddListener([MessageParameter(Name = "Endpoint")] ListenerEndpoint endpoint)
        {
            Runtime.AddListener(endpoint);
        }

        [OperationBehavior]
        public void RemoveListener(Guid listenerId)
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
        public void RetryFailedMessageDelivery(string messageId)
        {
            using(TransactionScope ts = new TransactionScope())
            {
                MessageDelivery delivery = Runtime.FailureQueue.Dequeue(messageId, TimeSpan.FromSeconds(30));
                if (delivery != null)
                {
                    Runtime.RetryQueue.Enqueue(delivery.CreateRetry(true, DateTime.Now));
                }
                else
                {
                    throw new FaultException<MessageDeliveryNotFoundFault>(new MessageDeliveryNotFoundFault(messageId));
                }
                ts.Complete();
            }
        }

        [OperationBehavior]
        public Collection<MessageDelivery> ListMessagesInFailureQueue(int? maxResults, string lastMessageId)
        {
            Collection<MessageDelivery> messages = new Collection<MessageDelivery>();
            bool found = lastMessageId == null;
            foreach (MessageDelivery md in Runtime.FailureQueue.ListMessages())
            {
                if (maxResults != null && maxResults.Value >= messages.Count)
                {
                    break;
                }

                if (found)
                {
                    messages.Add(md);
                }

                if (lastMessageId != null)
                {
                    if (md.MessageId == lastMessageId)
                    {
                        found = true;
                    }
                }
            }

            return messages;        
        }

        [OperationBehavior]
        public Collection<SubscriptionEndpoint> ListSubscribers()
        {
            return Runtime.ListSubscribers();
        }

        [OperationBehavior]
        public Collection<ListenerEndpoint> ListListeners()
        {
            return Runtime.ListListeners();
        }

        #endregion
    }

    [DataContract]
    public class ListenerNotFoundFault
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
    public class SubscriptionNotFoundFault
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
    public class MessageDeliveryNotFoundFault
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
