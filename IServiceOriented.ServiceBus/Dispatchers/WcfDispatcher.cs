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

namespace IServiceOriented.ServiceBus.Dispatchers
{    
    /// <summary>
    /// Provides support for dispatching messages to WCF endpoints.
    /// </summary>
    [Serializable]
    [DataContract]
    public class WcfDispatcher : Dispatcher
    {
        public WcfDispatcher()
        {
            
        }

        public WcfDispatcher(SubscriptionEndpoint endpoint) : base(endpoint)
        {

        }

        ActionLookup initActionLookup(SubscriptionEndpoint endpoint)
        {
            ActionLookup lookup = null;

            if (_lookup != null) return _lookup;

            lookup = new ActionLookup();

            Dictionary<string, MethodInfo> actionLookup = new Dictionary<string, MethodInfo>();
            Dictionary<string, string> replyActionLookup = new Dictionary<string, string>();
            foreach (MethodInfo method in endpoint.ContractType.GetMethods())
            {
                object[] attributes = method.GetCustomAttributes(typeof(OperationContractAttribute), false);
                if (attributes.Length > 0)
                {
                    OperationContractAttribute oca = (OperationContractAttribute)attributes[0];
                    string action = oca.Action;
                    if (action == null)
                    {
                        action = method.Name;
                    }
                    actionLookup.Add(action, method);

                    if (!oca.IsOneWay)
                    {
                        string replyAction = oca.ReplyAction;
                        if (replyAction == null)
                        {
                            replyAction = action + "Reply";
                        }

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
        
        public override void Dispatch(MessageDelivery messageDelivery)
        {
            // TODO: Clean this up. Creating channel factory for each call is expensive
            Type channelType = typeof(ChannelFactory<>).MakeGenericType(Endpoint.ContractType);
            object factory = Activator.CreateInstance(channelType, Endpoint.ConfigurationName);
            ((ChannelFactory)factory).Endpoint.Address = new EndpointAddress(Endpoint.Address);

            ApplySecurityContext(messageDelivery, (ChannelFactory)factory);

            IClientChannel proxy = (IClientChannel)channelType.GetMethod("CreateChannel", new Type[] { }).Invoke(factory, new object[] { });
            bool success = false;
            try
            {
                var lookup = initActionLookup(Endpoint);

                MethodInfo methodInfo = lookup.MethodLookup[messageDelivery.Action];
                
                if (methodInfo != null)
                {
                    try
                    {
                        object result = methodInfo.Invoke(proxy, new object[] { messageDelivery.Message });

                        if (lookup.ReplyActionLookup.ContainsKey(messageDelivery.Action)) // if two way message, publish reply
                        {
                            KeyValuePair<string, object>[] replyData = new KeyValuePair<string, object>[1];
                            replyData[0] = new KeyValuePair<string, object>(MessageDelivery.CorrelationId, messageDelivery.MessageId);
                            Runtime.Publish(new PublishRequest(Endpoint.ContractType, lookup.ReplyActionLookup[messageDelivery.Action], result, new MessageDeliveryContext(replyData))); 
                        }
                    }
                    catch(System.Reflection.TargetInvocationException ex)
                    {
                        if (lookup.ReplyActionLookup.ContainsKey(messageDelivery.Action)) // if two way message, publish reply
                        {
                            KeyValuePair<string, object>[] replyData = new KeyValuePair<string, object>[1];
                            replyData[0] = new KeyValuePair<string, object>(MessageDelivery.CorrelationId, messageDelivery.MessageId);
                        
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

        class ActionLookup
        {
            public Dictionary<string, string> ReplyActionLookup;            
            public Dictionary<string, MethodInfo> MethodLookup;
        }

        ActionLookup _lookup;

        public static MessageFilter CreateMessageFilter(Type interfaceType)
        {               
            return new TypedMessageFilter(WcfServiceHostFactory.GetMessageTypes(interfaceType));
        }
    }

		
}
