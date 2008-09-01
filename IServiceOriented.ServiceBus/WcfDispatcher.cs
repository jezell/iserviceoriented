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
        
        protected override void Dispatch(SubscriptionEndpoint endpoint, string action, object message)
        {
            if (_actionLookup == null)
            {
                initActionLookup();
            }
            
            MethodInfo methodInfo;

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

            if (methodInfo != null)
            {
                Service.Use(endpoint.ContractType, endpoint.ConfigurationName, endpoint.Address, contract =>
                {
                    methodInfo.Invoke(contract, new object[] { message });
                });
            }
            else
            {
                throw new InvalidOperationException("Matching action not found");
            }
        }

        [NonSerialized]
        Dictionary<string, MethodInfo> _actionLookup;

    }
		
}
