using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Reflection;

using System.Runtime.Serialization;

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

        public WcfDispatcher(WcfDispatchStyle dispatchStyle)
        {
            DispatchStyle = dispatchStyle;
        }

        // TODO: if the same dispatcher instance is reused with another type this will be invalid
        void initActionLookup()
        {
            Dictionary<string, MethodInfo> actionLookup = new Dictionary<string, MethodInfo>();
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
                }
            }
            _actionLookup = actionLookup;
            
        }

        // TODO: if the same dispatcher instance is reused with another type this will be invalid
        void initTypeLookup()
        {
            Dictionary<Type, MethodInfo> typeLookup = new Dictionary<Type, MethodInfo>();
            foreach (MethodInfo method in Endpoint.ContractType.GetMethods())
            {
                object[] attributes = method.GetCustomAttributes(typeof(OperationContractAttribute), false);
                if (attributes.Length > 0)
                {
                    OperationContractAttribute oca = (OperationContractAttribute)attributes[0];
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 1)
                    {
                        typeLookup.Add(parameters[0].ParameterType, method);
                    }
                }
            }
            _typeLookup = typeLookup;            
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

                if (DispatchStyle == WcfDispatchStyle.Action)
                {
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
                }
                else if (DispatchStyle == WcfDispatchStyle.Type)
                {
                    if (_typeLookup == null)
                    {
                        initTypeLookup();
                    }

                    if (_typeLookup.TryGetValue(messageDelivery.Message.GetType(), out methodInfo))
                    {
                        foreach (Type t in _typeLookup.Keys)
                        {
                            if (t == typeof(object))
                            {
                                methodInfo = _typeLookup[t];
                                break;
                            }
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }

                if (methodInfo != null)
                {                    
                    methodInfo.Invoke(proxy, new object[] { messageDelivery.Message });                    
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
        Dictionary<string, MethodInfo> _actionLookup;

        [NonSerialized]
        Dictionary<Type, MethodInfo> _typeLookup;


        /// <summary>
        /// Gets or sets the way that this dispatcher will determine which operation to invoke.
        /// </summary>        
        public WcfDispatchStyle DispatchStyle
        {
            get;
            set;
        }
    }

    public enum WcfDispatchStyle
    {
        /// <summary>
        /// Dispatch by finding an operation that matches the requested action (or "*" if no direct match)
        /// </summary>
        Action, 
        /// <summary>
        /// Dispatch by finding an operation that accepts the requested message type (or object if no direct match)
        /// </summary>
        Type
    }
		
}
