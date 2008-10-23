using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Reflection;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;
using IServiceOriented.ServiceBus.Listeners;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace IServiceOriented.ServiceBus.Dispatchers
{    
    /// <summary>
    /// Provides support for dispatching messages to WCF endpoints.
    /// </summary>
    [Serializable]
    [DataContract]
    public class WcfProxyDispatcher : WcfDispatcher
    {
        public WcfProxyDispatcher()
        {
            
        }

        public WcfProxyDispatcher(SubscriptionEndpoint endpoint) : base(endpoint)
        {

        }

        bool _passThrough;

        ActionLookup initActionLookup(SubscriptionEndpoint endpoint)
        {
            ActionLookup lookup = null;

            if (_lookup != null) return _lookup;

            lookup = new ActionLookup();

            Dictionary<string, MethodInfo> actionLookup = new Dictionary<string, MethodInfo>();
            Dictionary<string, string> replyActionLookup = new Dictionary<string, string>();
            foreach (OperationDescription operation in ContractDescription.GetContract(endpoint.ContractType).Operations)
            {
                MessageDescription requestMessage = operation.Messages.Where(md => md.Direction == MessageDirection.Input).First();
                MessageDescription responseMessage = operation.Messages.Where(md => md.Direction == MessageDirection.Output).FirstOrDefault();
                
                if (requestMessage.Action == "*")
                {
                    _passThrough = true;
                }

                actionLookup.Add(requestMessage.Action, operation.SyncMethod);

                if (responseMessage != null)
                {
                    string replyAction = responseMessage.Action;

                    if (replyAction != null)
                    {
                        replyActionLookup.Add(requestMessage.Action, replyAction);
                    }
                }
            
            }
            lookup.MethodLookup = actionLookup;
            lookup.ReplyActionLookup = replyActionLookup;

            _lookup = lookup;

            return lookup;
        }

        protected virtual void ApplySecurityContext(MessageDelivery delivery, ChannelFactory factory)
        {         
        }

        protected ChannelFactory CreateFactory()
        {
            Type channelType = typeof(ChannelFactory<>).MakeGenericType(Endpoint.ContractType);
            ChannelFactory factory = (ChannelFactory)Activator.CreateInstance(channelType, Endpoint.ConfigurationName);            
            return factory;
        }

        protected IContextChannel CreateProxy(IChannelFactory factory)
        {
            Type channelType = typeof(ChannelFactory<>).MakeGenericType(Endpoint.ContractType);            
            return (IContextChannel)channelType.GetMethod("CreateChannel", new Type[] { }).Invoke(factory, new object[] { });
        }

        protected string GetResponseCorrelationId(MessageDelivery delivery)
        {
            return (string)delivery.Context[MessageDelivery.PublishRequestId];
        }
        
        public override void Dispatch(MessageDelivery messageDelivery)
        {
            if (!Started) throw new InvalidOperationException("Dispatcher is not started yet");
            ChannelFactory factory = CreateFactory();
            factory.Endpoint.Address = new EndpointAddress(Endpoint.Address);
            ApplySecurityContext(messageDelivery, factory);
            IContextChannel proxy = CreateProxy(factory);            
            using (OperationContextScope scope = new OperationContextScope(proxy))
            {
                
                bool success = false;
                try
                {
                    var lookup = initActionLookup(Endpoint);

                    MethodInfo methodInfo = lookup.MethodLookup[_passThrough ? "*" : messageDelivery.Action];

                    if (methodInfo != null)
                    {
                        try
                        {
                            object result = methodInfo.Invoke(proxy, new object[] { messageDelivery.Message });

                            if (lookup.ReplyActionLookup.ContainsKey(messageDelivery.Action)) // if two way message, publish reply
                            {
                                KeyValuePair<MessageDeliveryContextKey, object>[] replyData = new KeyValuePair<MessageDeliveryContextKey, object>[1];
                                replyData[0] = new KeyValuePair<MessageDeliveryContextKey, object>(MessageDelivery.CorrelationId, GetResponseCorrelationId(messageDelivery));

                                Runtime.PublishOneWay(new PublishRequest(Endpoint.ContractType, lookup.ReplyActionLookup[messageDelivery.Action], result, new MessageDeliveryContext(replyData)));
                            }
                        }
                        catch (System.Reflection.TargetInvocationException ex)
                        {
                            if (lookup.ReplyActionLookup.ContainsKey(messageDelivery.Action)) // if two way message, publish reply
                            {
                                KeyValuePair<MessageDeliveryContextKey, object>[] replyData = new KeyValuePair<MessageDeliveryContextKey, object>[1];
                                replyData[0] = new KeyValuePair<MessageDeliveryContextKey, object>(MessageDelivery.CorrelationId, GetResponseCorrelationId(messageDelivery));

                                FaultException fex = ex.InnerException as FaultException;
                                if (fex != null)
                                {
                                    Runtime.PublishOneWay(new PublishRequest(Endpoint.ContractType, fex.Action, ((FaultException)ex.InnerException), new MessageDeliveryContext(replyData)));
                                }
                                else
                                {
                                    throw;
                                }
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Matching method not found");
                    }
                    proxy.Close();
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        proxy.Abort();
                    }
                }
            }
        }

        class ActionLookup
        {
            public Dictionary<string, string> ReplyActionLookup;            
            public Dictionary<string, MethodInfo> MethodLookup;
        }

        [NonSerialized]
        ActionLookup _lookup;

        public static MessageFilter CreateMessageFilter(Type contractType)
        {
            ContractDescription description = ContractDescription.GetContract(contractType);
            List<Type> messageTypes = new List<Type>();
            foreach (OperationDescription operation in description.Operations)
            {
                foreach (MessageDescription message in operation.Messages)
                {
                    messageTypes.Add(message.Body.Parts[0].Type);
                }
            }
            return new TypedMessageFilter(messageTypes.ToArray());
        }
    }

		
}
