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

        
        // TODO: if the same dispatcher instance is reused with another type this will be invalid
        void initActionLookup()
        {
            Dictionary<string, MethodInfo> actionLookup = new Dictionary<string, MethodInfo>();
            Dictionary<string, string> replyActionLookup = new Dictionary<string, string>();
            foreach (MethodInfo method in Endpoint.ContractType.GetMethods())
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

                    if(!oca.IsOneWay)
                    {
                        string replyAction = oca.ReplyAction;
                        if(replyAction == null)
                        {
                            replyAction = action+"Reply";
                        }

                        replyActionLookup.Add(action, replyAction);
                    }
                }
            }
            _actionLookup = actionLookup;
            _replyActionLookup = replyActionLookup;
            
        }

        /// <summary>
        /// Specifies whether to apply the credentials of the user that published the message.
        /// </summary>
        /// <remarks>Credentials will be passed as UserName credentials, not using Windows impersonation or delegation.</remarks>
        [DataMember]
        public bool ApplyCredentials
        {
            get;
            set;
        }

        protected virtual void ApplySecurityContext(ChannelFactory factory)
        {
            if (ApplyCredentials)
            {
                if (DispatchContext.MessageDelivery.Context.ContainsKey(MessageDelivery.PrimaryIdentityNameKey))
                {
                    factory.Credentials.UserName.UserName = (string)DispatchContext.MessageDelivery.Context[MessageDelivery.PrimaryIdentityNameKey];
                    factory.Credentials.UserName.Password = "";
                }
            }
        }
        
        protected override void Dispatch(SubscriptionEndpoint endpoint, MessageDelivery messageDelivery)
        {
            // TODO: Clean this up. Creating channel factory for each call is expensive
            Type channelType = typeof(ChannelFactory<>).MakeGenericType(endpoint.ContractType);
            object factory = Activator.CreateInstance(channelType, endpoint.ConfigurationName);
            ((ChannelFactory)factory).Endpoint.Address = new EndpointAddress(endpoint.Address);

            ApplySecurityContext((ChannelFactory)factory);

            IClientChannel proxy = (IClientChannel)channelType.GetMethod("CreateChannel", new Type[] { }).Invoke(factory, new object[] { });
            bool success = false;
            try
            {
                MethodInfo methodInfo;

                if (_actionLookup == null)
                {
                    initActionLookup();
                }

                if (!_actionLookup.TryGetValue(messageDelivery.Action, out methodInfo))
                {
                    foreach (string a in _actionLookup.Keys)
                    {
                        if (a == "*")
                        {
                            methodInfo = _actionLookup[a];
                            break;
                        }
                    }
                }

                if (methodInfo != null)
                {
                    try
                    {
                        object result = methodInfo.Invoke(proxy, new object[] { messageDelivery.Message });

                        if (_replyActionLookup.ContainsKey(messageDelivery.Action)) // if two way message, publish reply
                        {
                            KeyValuePair<string, object>[] replyData = new KeyValuePair<string, object>[1];
                            replyData[0] = new KeyValuePair<string, object>(MessageDelivery.CorrelationId, messageDelivery.MessageId);
                            Runtime.Publish(new PublishRequest(endpoint.ContractType, _replyActionLookup[messageDelivery.Action], result, new MessageDeliveryContext( replyData ))); 
                        }
                    }
                    catch(System.Reflection.TargetInvocationException ex)
                    {
                        if (_replyActionLookup.ContainsKey(messageDelivery.Action)) // if two way message, publish reply
                        {
                            KeyValuePair<string, object>[] replyData = new KeyValuePair<string, object>[1];
                            replyData[0] = new KeyValuePair<string, object>(MessageDelivery.CorrelationId, messageDelivery.MessageId);
                        
                            if (ex.InnerException is FaultException)
                            {
                                Runtime.Publish(new PublishRequest(endpoint.ContractType, _replyActionLookup[messageDelivery.Action], ex.InnerException, new MessageDeliveryContext(replyData)));
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

        [NonSerialized]
        Dictionary<string, string> _replyActionLookup;


        [NonSerialized]
        Dictionary<string, MethodInfo> _actionLookup;

    }

		
}
