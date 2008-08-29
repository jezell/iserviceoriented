using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Reflection;

namespace IServiceOriented.ServiceBus
{    
    public class WcfDispatcher<T> : Dispatcher
    {
        public WcfDispatcher()
        {
            foreach (MethodInfo method in typeof(T).GetMethods())
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
                    _actionLookup.Add(action, method);
                }
            }
        }

        protected override void Dispatch(SubscriptionEndpoint endpoint, string action, object message)
        {
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
                Service.Use<T>(endpoint.ConfigurationName, endpoint.Address, contract =>
                {
                    methodInfo.Invoke(contract, new object[] { message });
                });
            }
            else
            {
                throw new InvalidOperationException("Matching action not found");
            }
        }

        Dictionary<string, MethodInfo> _actionLookup = new Dictionary<string, MethodInfo>();

    }
		
}
