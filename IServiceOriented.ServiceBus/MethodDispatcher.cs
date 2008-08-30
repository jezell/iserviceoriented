using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{    
    public sealed class MethodDispatcher<T> : Dispatcher 
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
            foreach (MethodInfo method in typeof(T).GetMethods())
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

        protected override void Dispatch(SubscriptionEndpoint endpoint, string action, object message)
        {            
            MethodInfo methodInfo;
            if (!_actionLookup.TryGetValue(action, out methodInfo))
            {
                foreach (string a in _actionLookup.Keys)
                {
                    if (a == action)
                    {
                        methodInfo = _actionLookup[a];
                        break;
                    }
                }
            }

            if (methodInfo != null)
            {
                methodInfo.Invoke(Target, new object[] { message });
            }
            else
            {
                throw new InvalidOperationException("Matching action not found");
            }
        }

        Dictionary<string, MethodInfo> _actionLookup = new Dictionary<string, MethodInfo>();

    }
	
}
