using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public class MethodDispatcherConfiguration
    {
        Dictionary<Type, object> _targets = new Dictionary<Type, object>();

        public void RegisterTarget(Type type, object target)
        {            
            _targets.Add(type, target);
        }
        public void UnregisterTarget(Type type)
        {
            _targets.Remove(type);
        }

        public object GetTarget(Type type)
        {
            return _targets[type];
        }

        static Dictionary<WeakReference, MethodDispatcherConfiguration> _configs = new Dictionary<WeakReference, MethodDispatcherConfiguration>();


        // Note: this is not optimized
        public static MethodDispatcherConfiguration For(ServiceBusRuntime runtime)
        {            
            lock (_configs)
            {
                List<WeakReference> kill = new List<WeakReference>(); // remove invalid references
                foreach (WeakReference wr in _configs.Keys)
                {
                    if (wr.IsAlive)
                    {
                        if (wr.Target == runtime)
                        {
                            return _configs[wr];
                        }
                    }
                    else
                    {
                        kill.Add(wr);
                    }
                }

                foreach (WeakReference wr in kill) 
                {
                    _configs.Remove(wr);
                }
                
                // Configuration not found
                WeakReference newRef = new WeakReference(runtime);
                MethodDispatcherConfiguration config = new MethodDispatcherConfiguration();
                _configs.Add(newRef, config);
                return config;
            }        
        }
        
    }
    
    public class MethodDispatcher<T> : Dispatcher 
    {
        
        public MethodDispatcher() 
        {
            foreach (MethodInfo method in typeof(T).GetMethods())
            {
                if (_actionLookup.ContainsKey(method.Name))
                {
                    throw new InvalidOperationException("Method overloads are not allowed");
                }
                _actionLookup.Add(method.Name, method);
            }            
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
                methodInfo.Invoke(MethodDispatcherConfiguration.For(DispatchContext.Runtime).GetTarget(typeof(T)), new object[] { message });
            }
            else
            {
                throw new InvalidOperationException("Matching action not found");
            }

        }

        Dictionary<string, MethodInfo> _actionLookup = new Dictionary<string, MethodInfo>();

    }
	
}
