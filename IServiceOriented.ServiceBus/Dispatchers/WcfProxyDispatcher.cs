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
            foreach (MethodInfo method in endpoint.ContractType.GetMethods())
            {
                if (WcfUtils.IsServiceMethod(method))
                {
                    string action = WcfUtils.GetAction(endpoint.ContractType, method);
                    if (action == "*")
                    {
                        _passThrough = true;
                    }
                    actionLookup.Add(action, method);
                    
                    string replyAction = WcfUtils.GetReplyAction(endpoint.ContractType, method);
                    
                    if(replyAction != null)
                    {
                        replyActionLookup.Add(action, replyAction);
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

        protected override ICommunicationObject CreateCommunicationObject()
        {
            Type channelType = typeof(ChannelFactory<>).MakeGenericType(Endpoint.ContractType);
            ChannelFactory factory = (ChannelFactory)Activator.CreateInstance(channelType, Endpoint.ConfigurationName);
            return factory;
        }

        protected ChannelFactory Factory
        {
            get
            {
                return (ChannelFactory)CommunicationObject;
            }
        }

        protected IContextChannel CreateProxy()
        {
            Type channelType = typeof(ChannelFactory<>).MakeGenericType(Endpoint.ContractType);            
            return (IContextChannel)channelType.GetMethod("CreateChannel", new Type[] { }).Invoke(Factory, new object[] { });
        }

        protected virtual void ApplySecurityContext(MessageDelivery messageDelivery)
        {
        }
        
        public override void Dispatch(MessageDelivery messageDelivery)
        {
            Factory.Endpoint.Address = new EndpointAddress(Endpoint.Address);

            IContextChannel proxy = CreateProxy();            
            using (OperationContextScope scope = new OperationContextScope(proxy))
            {
                ApplySecurityContext(messageDelivery);

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
                                replyData[0] = new KeyValuePair<MessageDeliveryContextKey, object>(MessageDelivery.CorrelationId, messageDelivery.MessageDeliveryId);
                                Runtime.Publish(new PublishRequest(Endpoint.ContractType, lookup.ReplyActionLookup[messageDelivery.Action], result, new MessageDeliveryContext(replyData)));
                            }
                        }
                        catch (System.Reflection.TargetInvocationException ex)
                        {
                            if (lookup.ReplyActionLookup.ContainsKey(messageDelivery.Action)) // if two way message, publish reply
                            {
                                KeyValuePair<MessageDeliveryContextKey, object>[] replyData = new KeyValuePair<MessageDeliveryContextKey, object>[1];
                                replyData[0] = new KeyValuePair<MessageDeliveryContextKey, object>(MessageDelivery.CorrelationId, messageDelivery.MessageDeliveryId);

                                if (ex.InnerException is FaultException)
                                {
                                    Runtime.Publish(new PublishRequest(Endpoint.ContractType, lookup.ReplyActionLookup[messageDelivery.Action], ex.InnerException, new MessageDeliveryContext(replyData)));
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

        public static MessageFilter CreateMessageFilter(Type interfaceType)
        {               
            return new TypedMessageFilter(WcfUtils.GetMessageInformation(interfaceType).Select(mi => mi.MessageType).ToArray());
        }
    }

		
}
