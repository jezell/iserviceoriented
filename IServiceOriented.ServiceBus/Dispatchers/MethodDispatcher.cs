using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;

namespace IServiceOriented.ServiceBus.Dispatchers
{    
    /// <summary>
    /// Provides support for dispatching messages to an object instance.
    /// </summary>
    public class MethodDispatcher : Dispatcher 
    {
        private MethodDispatcher()
        {

        }

        public MethodDispatcher(SubscriptionEndpoint endpoint) : base(endpoint)
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
                    _replyLookup.Add(method.Name, method.Name + "Response");
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

        protected string GetResponseCorrelationId(MessageDelivery delivery)
        {
            return (string)delivery.Context[MessageDelivery.PublishRequestId];
        }

        public override void Dispatch(MessageDelivery messageDelivery)
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

            
            if (methodInfo != null)
            {
                object result;
                try
                {
                    result = methodInfo.Invoke(Target, new object[] { messageDelivery.Message });

                    if (!IsOneWay)
                    {
                        KeyValuePair<MessageDeliveryContextKey, object>[] replyData = new KeyValuePair<MessageDeliveryContextKey, object>[1];
                        replyData[0] = new KeyValuePair<MessageDeliveryContextKey, object>(MessageDelivery.CorrelationId, GetResponseCorrelationId(messageDelivery));
                        replyAction = _replyLookup[messageDelivery.Action];
                        Runtime.PublishOneWay(new PublishRequest(Endpoint.ContractType, replyAction, result, new MessageDeliveryContext(replyData)));
                    }
                }
                catch (TargetInvocationException ex)
                {
                    result = ex.InnerException;
                    if (!IsOneWay)
                    {
                        KeyValuePair<MessageDeliveryContextKey, object>[] replyData = new KeyValuePair<MessageDeliveryContextKey, object>[1];
                        replyData[0] = new KeyValuePair<MessageDeliveryContextKey, object>(MessageDelivery.CorrelationId, GetResponseCorrelationId(messageDelivery));
                        Runtime.PublishOneWay(new PublishRequest(Endpoint.ContractType, replyAction, result, new MessageDeliveryContext(replyData)));
                    }
                    else
                    {
                        throw;
                    }
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
