using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Reflection;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;

namespace IServiceOriented.ServiceBus
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
        
        ActionLookup initActionLookup(SubscriptionEndpoint endpoint)
        {
            lock (_endpointLookups)
            {
                ActionLookup lookup = null;
                _endpointLookups.TryGetValue(endpoint, out lookup);

                if (lookup != null) return lookup;

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
                _endpointLookups.Add(endpoint, lookup);

                return lookup;
            }
            
        }
       

        protected virtual void ApplySecurityContext(MessageDelivery delivery, ChannelFactory factory)
        {         
        }
        
        public override void Dispatch(SubscriptionEndpoint endpoint, MessageDelivery messageDelivery)
        {
            // TODO: Clean this up. Creating channel factory for each call is expensive
            Type channelType = typeof(ChannelFactory<>).MakeGenericType(endpoint.ContractType);
            object factory = Activator.CreateInstance(channelType, endpoint.ConfigurationName);
            ((ChannelFactory)factory).Endpoint.Address = new EndpointAddress(endpoint.Address);

            ApplySecurityContext(messageDelivery, (ChannelFactory)factory);

            IClientChannel proxy = (IClientChannel)channelType.GetMethod("CreateChannel", new Type[] { }).Invoke(factory, new object[] { });
            bool success = false;
            try
            {
                MethodInfo methodInfo;

                ActionLookup lookup = null;
                _endpointLookups.TryGetValue(endpoint, out lookup);

                if (lookup == null)
                {
                    lookup = initActionLookup(endpoint);
                }

                if (!lookup.MethodLookup.TryGetValue(messageDelivery.Action, out methodInfo))
                {
                    foreach (string a in lookup.MethodLookup.Keys)
                    {
                        if (a == "*")
                        {
                            methodInfo = lookup.MethodLookup[a];
                            break;
                        }
                    }
                }

                if (methodInfo != null)
                {
                    try
                    {
                        object result = methodInfo.Invoke(proxy, new object[] { messageDelivery.Message });

                        if (lookup.ReplyActionLookup.ContainsKey(messageDelivery.Action)) // if two way message, publish reply
                        {
                            KeyValuePair<string, object>[] replyData = new KeyValuePair<string, object>[1];
                            replyData[0] = new KeyValuePair<string, object>(MessageDelivery.CorrelationId, messageDelivery.MessageId);
                            Runtime.Publish(new PublishRequest(endpoint.ContractType, lookup.ReplyActionLookup[messageDelivery.Action], result, new MessageDeliveryContext(replyData))); 
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
                                Runtime.Publish(new PublishRequest(endpoint.ContractType, lookup.ReplyActionLookup[messageDelivery.Action], ex.InnerException, new MessageDeliveryContext(replyData)));
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

        [NonSerialized]
        Dictionary<SubscriptionEndpoint, ActionLookup> _endpointLookups = new Dictionary<SubscriptionEndpoint, ActionLookup>();
        

        public static MessageFilter CreateMessageFilter(Type interfaceType)
        {               
            return new TypedMessageFilter(WcfServiceHostFactory.GetMessageTypes(interfaceType));
        }
    }

		
}
