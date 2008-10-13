using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ServiceModel.Channels;
using IServiceOriented.ServiceBus.Delivery.Formatters;
using System.Xml;

namespace IServiceOriented.ServiceBus.Dispatchers
{
    public class FileSystemDispatcher : Dispatcher
    {
        public FileSystemDispatcher()
        {
        }

        public FileSystemDispatcher(string outgoingFolder)
        {
            OutgoingFolder = outgoingFolder;
        }

        protected override void OnStart()
        {
            if (!Directory.Exists(OutgoingFolder))
            {
                Directory.CreateDirectory(OutgoingFolder);
            }
            base.OnStart();
        }

        MessageDeliveryConverter _converter;
        public MessageDeliveryConverter Converter
        {
            get
            {
                if (_converter == null)
                {
                    _converter = MessageDeliveryConverter.CreateConverter(Endpoint.ContractType);
                }
                return _converter;
            }
        }
        
        public override void Dispatch(MessageDelivery messageDelivery)
        {
            try
            {
                FileStream fs = File.Open(Path.Combine(OutgoingFolder, messageDelivery.MessageId), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                try
                {
                    Message message = Converter.ToMessage(messageDelivery);

                    using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(fs))
                    {
                        message.WriteMessage(writer);
                    }
                }
                finally
                {
                    fs.Close();
                }
            }
            catch (IOException ex)
            {
                throw new DeliveryException("Error writing file", ex);
            }
        }

        public string OutgoingFolder
        {
            get;
            set;
        }
    }
}
