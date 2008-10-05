using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;

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

        public MethodDispatcher(object target) : this(target, true)
        {
        }
        public MethodDispatcher(object target, bool isOneWay) 
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            IsOneWay = isOneWay;

            Target = target;
            foreach (MethodInfo method in target.GetType().GetMethods())
            {
                if (_actionLookup.ContainsKey(method.Name))
                {
                    throw new InvalidOperationException("Method overloads are not allowed");
                }
                _actionLookup.Add(method.Name, method);

                if (!IsOneWay)
                {
                    _replyLookup.Add(method.Name, method.Name + "Reply");
                }
            }            
        }

        public bool IsOneWay
        {
            get;
            private set;
        }

        public object Target
        {
            get;
            private set;
        }

        protected override void Dispatch(SubscriptionEndpoint endpoint, MessageDelivery messageDelivery)
        {            
            MethodInfo methodInfo;
            string replyAction = null;

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

            if(!IsOneWay) replyAction = _replyLookup[messageDelivery.Action];

            if (methodInfo != null)
            {
                object result = methodInfo.Invoke(Target, new object[] { messageDelivery.Message });

                if (!IsOneWay)
                {
                    KeyValuePair<string, object>[] replyData = new KeyValuePair<string, object>[1];
                    replyData[0] = new KeyValuePair<string, object>(MessageDelivery.CorrelationId, messageDelivery.MessageId);                         
                    Runtime.Publish(new PublishRequest(endpoint.ContractType, replyAction, result, new ReadOnlyDictionary<string, object>(replyData)));
                }
            }
            else
            {
                throw new InvalidOperationException("Matching action not found");
            }
        }

        Dictionary<string, MethodInfo> _actionLookup = new Dictionary<string, MethodInfo>();
        Dictionary<string, string> _replyLookup = new Dictionary<string, string>();

    }
	
}
