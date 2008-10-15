using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ServiceModel.Channels;
using IServiceOriented.ServiceBus.Delivery.Formatters;
using System.Xml;
using IServiceOriented.ServiceBus.IO;

namespace IServiceOriented.ServiceBus.Dispatchers
{
    public class FileSystemDispatcher : Dispatcher
    {
        public FileSystemDispatcher(MessageDeliveryWriterFactory writerFactory)
        {
            WriterFactory = writerFactory;
        }

        public FileSystemDispatcher(MessageDeliveryWriterFactory writerFactory, string outgoingFolder)
            : this(writerFactory)
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

        public MessageDeliveryWriterFactory WriterFactory
        {
            get;
            private set;
        }

        public override void Dispatch(MessageDelivery messageDelivery)
        {
            try
            {
                FileStream fs = File.Open(Path.Combine(OutgoingFolder, messageDelivery.MessageDeliveryId), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using (var writer = WriterFactory.CreateWriter(fs))
                {
                    writer.Write(messageDelivery);
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
