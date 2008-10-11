using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;
using System.Collections.ObjectModel;
using System.ServiceModel.Channels;
using IServiceOriented.ServiceBus.Dispatchers;


namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Represents a message to be delivered.
    /// </summary>
    [Serializable]
    [DataContract]
    [KnownType("GetKnownTypes")]
    public class MessageDelivery 
    {
        public MessageDelivery(Guid subscriptionEndpointId, Type contractType, string action, object message, int maxRetries, MessageDeliveryContext context)
        {
            MessageId = Guid.NewGuid().ToString();
            SubscriptionEndpointId = subscriptionEndpointId;
            Action = action;
            Message = message;
            MaxRetries = maxRetries;
            Context = context;
            ContractType = contractType;
        }

        public MessageDelivery(string messageId, Guid subscriptionEndpointId, Type contractType, string action, object message, int maxRetries, int retryCount, DateTime? timeToProcess, MessageDeliveryContext context)
        {
            MessageId = messageId;
            SubscriptionEndpointId = subscriptionEndpointId;
            Action = action;
            Message = message;
            RetryCount = retryCount;
            TimeToProcess = timeToProcess;
            MaxRetries = maxRetries;
            Context = context;
            ContractType = contractType;
        }

        /// <summary>
        /// Gets a list of types that have been registered for message delivery.
        /// </summary>        
        public static Type[] GetKnownTypes()
        {
            lock (_knownTypes)
            {
                return _knownTypes.ToArray();
            }
        }

        static List<Type> _knownTypes = new List<Type>(new Type[] { typeof(UnhandledMessageFilter), typeof(Listeners.WcfListener), typeof(WcfDispatcher), typeof(TypedMessageFilter), typeof(ReadOnlyCollection<string>), typeof(ReadOnlyCollection<short>), typeof(ReadOnlyCollection<int>), typeof(ReadOnlyCollection<bool>), typeof(ReadOnlyCollection<decimal>), typeof(ReadOnlyCollection<float>), typeof(ReadOnlyCollection<double>), typeof(ReadOnlyCollection<long>), typeof(ReadOnlyCollection<Guid>), typeof(MessageDeliveryContext) });
        /// <summary>
        /// Clears the list of types registered for message delivery.
        /// </summary>
        public static void ClearKnownTypes()
        {
            lock (_knownTypes)
            {
                _knownTypes.Clear();
            }
        }

        /// <summary>
        /// Register a type for message delivery.
        /// </summary>
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

        /// <summary>
        /// Unregister a type for message delivery.
        /// </summary>
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

        
        private string _messageId;
        /// <summary>
        /// Gets the unique identifier of this message
        /// </summary>
        [DataMember]
        public string MessageId
        {
            get { return _messageId; }
            private set { _messageId = value; }
        }

        private Guid _subscriptionEndpointId;
        /// <summary>
        /// Gets the identifier of the subscription that this message is associated with.
        /// </summary>
        [DataMember]
        public Guid SubscriptionEndpointId
        {
            get { return _subscriptionEndpointId; }
            private set { _subscriptionEndpointId = value; }
        }

        private Type _contractType;
        /// <summary>
        /// Gets the type of contract associated with this message delivery.
        /// </summary>
        public Type ContractType
        {
            get
            {
                return _contractType;
            }
            set
            {
                _contractType = value;
            }
        }

        /// <summary>
        /// Gets the name of the contract type associated with this message delivery.
        /// </summary>
        [DataMember]
        public string ContractTypeName
        {
            get
            {
                if (_contractType == null) return null;
                return _contractType.AssemblyQualifiedName;
            }
            set
            {
                if (value == null)
                {
                    ContractType = null;
                }
                else
                {
                    Type type = Type.GetType(value);
                    if (type == null)
                    {
                        throw new InvalidOperationException("The type specified does not exist");
                    }
                    ContractType = type;
                }
            }
        }

        private string _action;
        /// <summary>
        /// Gets the action associated with this message.
        /// </summary>
        [DataMember]
        public string Action
        {
            get { return _action; }
            private set { _action = value; }
        }

        private  object _message;
        /// <summary>
        /// Gets the message contents.
        /// </summary>
        [DataMember]
        public object Message
        {
            get { return _message; }
            private set { _message = value; }
        } 

        private int _retryCount;
        /// <summary>
        /// Gets the number of times this message has been retried.
        /// </summary>
        [DataMember]
        public int RetryCount
        {
            get { return _retryCount; }
            private set { _retryCount = value; }
        } 

        private DateTime? _timeToProcess;
        /// <summary>
        /// Gets the time that this message should be processed (or null if immediately).
        /// </summary>
        [DataMember]
        public DateTime? TimeToProcess
        {
            get { return _timeToProcess; }
            private set { _timeToProcess = value; }
        }


        /// <summary>
        /// Gets the maximum number of times this message will be retried.
        /// </summary>
        [DataMember]
        public int MaxRetries
        {
            get { return _maxRetries; }
            private set { _maxRetries = value; }
        }


        /// <summary>
        /// Gets a boolean value indicating whether the maximum number of retries has been met.
        /// </summary>
        public bool RetriesMaxed
        {
            get
            {
                return _maxRetries < _retryCount;
            }
        }

        MessageDeliveryContext _context = new MessageDeliveryContext();
        
        /// <summary>
        /// Gets the context associated with this message.
        /// </summary>
        [DataMember]
        public MessageDeliveryContext Context
        {
            get
            {
                return _context;
            }
            private set
            {
                _context = value;
            }
        }

        private int _maxRetries;
        
        /// <summary>
        /// Create a retry message based off of this message.
        /// </summary>
        /// <param name="resetRetryCount">Whether or not to reset the retry count.</param>
        /// <param name="timeToDeliver">Time to deliver the retry message.</param>
        /// <returns>A new MessageDelivery.</returns>
        public MessageDelivery CreateRetry(bool resetRetryCount, DateTime timeToDeliver)
        {            
            int retryCount = resetRetryCount ? 0 : (_retryCount + 1);            
            return new MessageDelivery(_messageId, _subscriptionEndpointId, _contractType, _action, _message, _maxRetries, retryCount, timeToDeliver, _context);             
        }


        /// <summary>
        /// Create a retry message based off of this message.
        /// </summary>
        /// <param name="resetRetryCount">Whether or not to reset the retry count.</param>
        /// <param name="timeToDeliver">Time to deliver the retry message.</param>
        /// <param name="exception">The exception that caused the retry.</param>
        /// <remarks>The exception will be attached to the message context.</remarks>
        /// <returns>A new MessageDelivery.</returns>
        public MessageDelivery CreateRetry(bool resetRetryCount, DateTime timeToDeliver, Exception exception)
        {
            int retryCount = resetRetryCount ? 0 : (_retryCount + 1);
            
            var context = _context.ToDictionary();

            if(!context.ContainsKey(Exceptions))
            {
                List<string> exceptions = new List<string>();
                exceptions.Add(exception.ToString());
                context.Add(Exceptions, new ReadOnlyCollection<string>(exceptions));
            }
            else
            {
                List<string> exceptions = new List<string>((IEnumerable<string>)context[Exceptions]);
                exceptions.Add(exception.ToString());
                context[Exceptions] = new ReadOnlyCollection<string>(exceptions);
            }            
                        
            return new MessageDelivery(_messageId, _subscriptionEndpointId, _contractType, _action, _message, _maxRetries, retryCount, timeToDeliver, new MessageDeliveryContext(context));
        }

        public const string PrimaryIdentityNameKey = "PrimaryIdentityName";        
        public const string WindowsIdentityNameKey = "WindowsIdentityName";
        public const string WindowsIdentityImpersonationLevelKey = "WindowsImpersonationLevel";
        public const string CorrelationId = "CorrelationId";
        public const string Exceptions = "Exceptions";

        
    }
}
