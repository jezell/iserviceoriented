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
using System.ServiceModel;
using IServiceOriented.ServiceBus.Delivery.Formatters;
using System.Globalization;
using IServiceOriented.ServiceBus.IO;

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{    
    public class MessageDeliveryFormatter : IMessageFormatter
    {
        public MessageDeliveryFormatter()
        {
            
        }

        public MessageDeliveryFormatter(MessageDeliveryReaderFactory readerFactory, MessageDeliveryWriterFactory writerFactory) 
        {
            ReaderFactory = readerFactory;
            WriterFactory = writerFactory;
        }

        public MessageDeliveryReaderFactory ReaderFactory
        {
            get;
            private set;
        }

        public MessageDeliveryWriterFactory WriterFactory
        {
            get;
            private set;
        }

        
        #region IMessageFormatter Members

        public bool CanRead(Message message)
        {
            return message.Body is MessageDelivery;
        }
        
        public object Read(Message message)
        {
            return ReaderFactory.CreateReader(message.BodyStream, false).Read();
        }
       

        public void Write(Message message, object obj)
        {
            MessageDelivery delivery = obj as MessageDelivery;
                        
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                MemoryStream stream = new MemoryStream();
                try
                {
                    WriterFactory.CreateWriter(stream, false).Write(delivery);
                    stream.Position = 0;
                    message.BodyStream = stream;
                }
                catch (KeyNotFoundException)
                {
                    throw new InvalidOperationException("Unregistered contract type");
                }
            }

        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return new MessageDeliveryFormatter(ReaderFactory, WriterFactory);
        }

        #endregion
    }

}
