using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;

using System.Messaging;
using System.ServiceModel.Description;
using System.Xml;
using IServiceOriented.ServiceBus.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using IServiceOriented.ServiceBus.Delivery.Formatters;

namespace IServiceOriented.ServiceBus.Delivery
{    
    /// <summary>
    /// Provides support for queuing messages using MSMQ
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msmq")]
    public class MsmqMessageDeliveryQueue : IMessageDeliveryQueue, IDisposable
    {
        public MsmqMessageDeliveryQueue(string path, IMessageFormatter formatter)
        {
            _queue = new MessageQueue(path);
            _formatter = formatter;
        }

        public MsmqMessageDeliveryQueue(string path, MessageQueueTransactionType transactionType, IMessageFormatter formatter)
        {
            _queue = new MessageQueue(path);
            _formatter = new MessageDeliveryFormatter();
            _transactionType = transactionType;
            _formatter = formatter;
        }


        public MsmqMessageDeliveryQueue(string path, bool createIfNotExists, IMessageFormatter formatter)
        {
            if (createIfNotExists)
            {
                if (!MessageQueue.Exists(path))
                {
                    MessageQueue.Create(path, true);
                }
            }
            _queue = new MessageQueue(path);
            _formatter = formatter;
        }


        public MsmqMessageDeliveryQueue(MessageQueue queue, IMessageFormatter formatter)
        {
            _queue = queue;
            _formatter = formatter;
        }

        /// <summary>
        /// Create a message queue
        /// </summary>
        /// <param name="path"></param>
        public static void Create(string path)
        {
            MessageQueue.Create(path, true);
        }

        /// <summary>
        /// Delete a message queue
        /// </summary>
        /// <param name="path"></param>
        public static void Delete(string path)
        {
            MessageQueue.Delete(path);
        }

        /// <summary>
        /// Determines if a message queue with the specified path exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool Exists(string path)
        {
            return MessageQueue.Exists(path);
        }

        MessageQueueTransactionType _transactionType = MessageQueueTransactionType.Automatic;
        /// <summary>
        /// Gets or sets the TransactionType that will be used when communicating with the message queue
        /// </summary>
        public MessageQueueTransactionType TransactionType
        {
            get
            {
                return _transactionType;
            }
            set
            {
                _transactionType = value;
            }
        }
        MessageQueue _queue;
        IMessageFormatter _formatter;

        public IMessageFormatter Formatter
        {
            get
            {
                return _formatter;
            }
            set
            {
                _formatter = value;
            }
        }


        private System.Messaging.Message createMessage(MessageDelivery value)
        {
            System.Messaging.Message message = new System.Messaging.Message();
            message.Label = value.MessageId;
            message.Formatter = _formatter;
            message.Body = value;
            return message;
        }

        private MessageDelivery createObject(System.Messaging.Message message)
        {
            message.Formatter = _formatter;
            try
            {
                return (MessageDelivery)message.Body;
            }
            catch (Exception ex)
            {
                throw new PoisonMessageException("The message body could not be deserialized", ex);
            }
        }

        public void Enqueue(MessageDelivery value)
        {
            if (_disposed) throw new ObjectDisposedException("MsmqMessageDeliveryQueue");

            System.Messaging.Message message = createMessage(value);
            _queue.Send(message, _transactionType);
        }

        public MessageDelivery Peek(TimeSpan timeout)
        {
            if (_disposed) throw new ObjectDisposedException("MsmqMessageDeliveryQueue");
            
            try
            {
                System.Messaging.Message message = _queue.Peek(timeout);
                return createObject(message);
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    return default(MessageDelivery);
                }
                throw;
            }
        }

        public MessageDelivery Dequeue(TimeSpan timeout)
        {
            if (_disposed) throw new ObjectDisposedException("MsmqMessageDeliveryQueue");
            
            try
            {
                System.Messaging.Message message = _queue.Receive(timeout, _transactionType);
                return createObject(message);
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    return default(MessageDelivery);
                }
                throw;
            }
        }

        public MessageDelivery Dequeue(string id, TimeSpan timeout)
        {
            if (_disposed) throw new ObjectDisposedException("MsmqMessageDeliveryQueue");

            try
            {
                using(System.Messaging.MessageEnumerator msgEnum = _queue.GetMessageEnumerator2())
                {
                    if(msgEnum.Current.Label == id)
                    {
                        Message message = _queue.ReceiveById(msgEnum.Current.Id, _transactionType);
                        return createObject(message);        
                    }
                }

                return null;
            }
            catch (MessageQueueException ex)
            {
                if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    throw new TimeoutException("The timeout was reached before the specified message could be returned.");
                }
                else if (ex.MessageQueueErrorCode == MessageQueueErrorCode.MessageNotFound)
                {
                    return default(MessageDelivery);
                }
                throw;
            }
        }

        public IEnumerable<MessageDelivery> ListMessages()
        {
            if (_disposed) throw new ObjectDisposedException("MsmqMessageDeliveryQueue");

            MessageEnumerator enumerator = _queue.GetMessageEnumerator2();
            while (enumerator.MoveNext())
            {
                yield return createObject(enumerator.Current);
            }
        }

        volatile bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if(_queue != null) _queue.Dispose();
                _queue = null;

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        ~MsmqMessageDeliveryQueue()
        {
            Dispose(false);
        }
    }
	
	

}
