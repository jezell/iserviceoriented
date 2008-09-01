using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{    
    /// <summary>
    /// Provides support for dispatching messages to an object instance.
    /// </summary>
    public sealed class MethodDispatcher : Dispatcher 
    {
        private MethodDispatcher()
        {

        }
        public MethodDispatcher(object target) 
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            Target = target;
            foreach (MethodInfo method in target.GetType().GetMethods())
            {
                if (_actionLookup.ContainsKey(method.Name))
                {
                    throw new InvalidOperationException("Method overloads are not allowed");
                }
                _actionLookup.Add(method.Name, method);
            }            
        }

        public object Target
        {
            get;
            private set;
        }

        protected override void Dispatch(SubscriptionEndpoint endpoint, MessageDelivery messageDelivery)
        {            
            MethodInfo methodInfo;
            if (!_actionLookup.TryGetValue(messageDelivery.Action, out methodInfo))
            {
                foreach (string a in _actionLookup.Keys)
                {
                    if (a == messageDelivery.Action)
                    {
                        methodInfo = _actionLookup[a];
                        break;
                    }
                }
            }

            if (methodInfo != null)
            {
                methodInfo.Invoke(Target, new object[] { messageDelivery.Message });
            }
            else
            {
                throw new InvalidOperationException("Matching action not found");
            }
        }

        Dictionary<string, MethodInfo> _actionLookup = new Dictionary<string, MethodInfo>();

    }
	
}
