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

namespace IServiceOriented.ServiceBus.Delivery.Formatters
{    
    public class MessageDeliveryFormatter : IMessageFormatter
    {
        public MessageDeliveryFormatter(params Type[] interfaceType)
        {
            foreach (Type t in interfaceType)
            {
                _converters.Add(t, MessageDeliveryConverter.CreateConverter(t));
            }
        }

        Dictionary<Type, MessageDeliveryConverter> _converters = new Dictionary<Type, MessageDeliveryConverter>();

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
                Type contractType = Type.GetType(msg.Headers.GetHeader<string>(MessageDeliveryConverter.ContractTypeNameHeader, MessageDeliveryConverter.MessagingNamespace));
                return _converters[contractType].CreateMessageDelivery(msg);
            }
        }
       

        public void Write(Message message, object obj)
        {
            MessageDelivery delivery = obj as MessageDelivery;
                        
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                MemoryStream stream = new MemoryStream();

                try
                {
                    System.ServiceModel.Channels.Message msg = _converters[delivery.ContractType].ToMessage(delivery);
                    XmlDictionaryWriter xmlWriter = XmlDictionaryWriter.CreateBinaryWriter(stream);
                    msg.WriteMessage(xmlWriter);
                    xmlWriter.Flush();
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
            return new MessageDeliveryFormatter(_converters.Keys.ToArray());
        }

        #endregion
    }

}
