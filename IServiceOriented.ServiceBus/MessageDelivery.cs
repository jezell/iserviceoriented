using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    [Serializable]
    [DataContract]
    [KnownType("GetKnownTypes")]
    public class MessageDelivery 
    {
        public MessageDelivery(Guid subscriptionEndpointId, string action, object message, int maxRetries)
        {
            _messageId = Guid.NewGuid().ToString();
            _subscriptionEndpointId = subscriptionEndpointId;
            _action = action;
            _message = message;
            _maxRetries = maxRetries;
        }

        public MessageDelivery(string messageId, Guid subscriptionEndpointId, string action, object message, int maxRetries, int retryCount, DateTime? timeToProcess, int queueCount)
        {
            _messageId = messageId;
            _subscriptionEndpointId = subscriptionEndpointId;
            _action = action;
            _message = message;
            _retryCount = retryCount;
            _timeToProcess = timeToProcess;
            _queueCount = queueCount;
            _maxRetries = maxRetries;
        }

        public static Type[] GetKnownTypes()
        {
            lock (_knownTypes)
            {
                return _knownTypes.ToArray();
            }
        }
        
        static List<Type> _knownTypes = new List<Type>(new Type[] { typeof(UnhandledMessageFilter) } );
        public static void ClearKnownTypes()
        {
            lock (_knownTypes)
            {
                _knownTypes.Clear();
            }
        }

        public static void RegisterKnownType(Type type)
        {
            lock (_knownTypes)
            {
                if (!_knownTypes.Contains(type))
                {                    
                    _knownTypes.Add(type);
                }
            }
        }

        public static void UnregisterKnownType(Type type)
        {
            lock (_knownTypes)
            {
                if (_knownTypes.Contains(type))
                {
                    _knownTypes.Remove(type);
                }
            }
        }


        int _queueCount;
        [DataMember]
        public int QueueCount
        {
            get
            {
                return _queueCount;
            }
            private set
            {
                _queueCount = value;
            }
        }
        
        public int IncrementQueueCount()
        {
            return _queueCount++; 
        }


        private string _messageId;
        [DataMember]
        public string MessageId
        {
            get { return _messageId; }
            private set { _messageId = value; }
        }

        private Guid _subscriptionEndpointId;
        [DataMember]
        public Guid SubscriptionEndpointId
        {
            get { return _subscriptionEndpointId; }
            private set { _subscriptionEndpointId = value; }
        } 

        private string _action;
        [DataMember]
        public string Action
        {
            get { return _action; }
            private set { _action = value; }
        }

        private  object _message;

        [DataMember]
        public object Message
        {
            get { return _message; }
            private set { _message = value; }
        } 

        private int _retryCount;
        [DataMember]
        public int RetryCount
        {
            get { return _retryCount; }
            private set { _retryCount = value; }
        } 

        private DateTime? _timeToProcess;
        [DataMember]
        public DateTime? TimeToProcess
        {
            get { return _timeToProcess; }
            private set { _timeToProcess = value; }
        }


        [DataMember]
        public int MaxRetries
        {
            get { return _maxRetries; }
            private set { _maxRetries = value; }
        } 


        public bool RetriesMaxed
        {
            get
            {
                return _maxRetries < _retryCount;
            }
        }

        private int _maxRetries;
        
        public MessageDelivery CreateRetry(bool resetRetryCount, DateTime timeToDeliver)
        {            
            int retryCount = resetRetryCount ? 0 : (_retryCount + 1);            
            return new MessageDelivery(_messageId, _subscriptionEndpointId, _action, _message, _maxRetries, retryCount, timeToDeliver, QueueCount+1);             
        }        
    }
}
