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

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{    
    public class MessageDeliveryFormatter : IMessageFormatter
    {
        #region IMessageFormatter Members

        public bool CanRead(Message message)
        {
            return message.Body is MessageDelivery;
        }

        const int MAX_HEADER_SIZE = 1024 * 1024;

        public object Read(Message message)
        {
            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            quotas.MaxArrayLength = Int32.MaxValue;
            quotas.MaxBytesPerRead = Int32.MaxValue;
            quotas.MaxDepth = Int32.MaxValue;
            quotas.MaxNameTableCharCount = Int32.MaxValue;
            quotas.MaxStringContentLength = Int32.MaxValue;
    
            using (XmlDictionaryReader xmlReader = XmlDictionaryReader.CreateBinaryReader(message.BodyStream, quotas))
            {
                var msg = System.ServiceModel.Channels.Message.CreateMessage(xmlReader, MAX_HEADER_SIZE, System.ServiceModel.Channels.MessageVersion.Default);
                return MessageDeliveryConverter.CreateMessageDelivery(msg);
            }
        }
       

        public void Write(Message message, object obj)
        {
            MessageDelivery delivery = obj as MessageDelivery;
                        
            using (StringWriter writer = new StringWriter())
            {
                System.ServiceModel.Channels.Message msg = MessageDeliveryConverter.ToMessage(delivery);

                MemoryStream stream = new MemoryStream();
                XmlDictionaryWriter xmlWriter = XmlDictionaryWriter.CreateBinaryWriter(stream);
                msg.WriteMessage(xmlWriter);
                xmlWriter.Flush();
                stream.Position = 0;
                message.BodyStream = stream;
            }

        }



        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return new MessageDeliveryFormatter();
        }

        #endregion
    }

}
