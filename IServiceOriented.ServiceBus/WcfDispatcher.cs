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
        
        protected override void Dispatch(SubscriptionEndpoint endpoint, string action, object message)
        {
            
            MethodInfo methodInfo;

            if (DispatchStyle == WcfDispatchStyle.Action)
            {
                if (_actionLookup == null)
                {
                    initActionLookup();
                }

                if (!_actionLookup.TryGetValue(action, out methodInfo))
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

                if (_typeLookup.TryGetValue(message.GetType(), out methodInfo))
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
                Service.Use(endpoint.ContractType, endpoint.ConfigurationName, endpoint.Address, contract =>
                {
                    methodInfo.Invoke(contract, new object[] { message });
                });
            }
            else
            {
                throw new InvalidOperationException("Matching method not found");
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
