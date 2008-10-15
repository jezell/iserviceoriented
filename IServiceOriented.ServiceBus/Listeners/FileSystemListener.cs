using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.ServiceModel.Channels;
using IServiceOriented.ServiceBus.Delivery.Formatters;
using System.Threading;

namespace IServiceOriented.ServiceBus.Listeners
{
    public class FileSystemListener : Listener
    {
        public FileSystemListener()
        {
            OpenTimeout = TimeSpan.FromSeconds(10);
        }

        public FileSystemListener(string incomingFolder, string processedFolder) : this()
        {
            IncomingFolder = incomingFolder;
            ProcessedFolder = processedFolder;
        }

        protected override void OnStart()
        {            
            if (!Directory.Exists(IncomingFolder))
            {
                Directory.CreateDirectory(IncomingFolder);
            }

            if (!Directory.Exists(ProcessedFolder))
            {
                Directory.CreateDirectory(ProcessedFolder);
            }

            _watcher = new FileSystemWatcher(IncomingFolder);
            _watcher.Created += onFileCreated;
            _watcher.EnableRaisingEvents = true;            

            foreach (string file in Directory.GetFiles(IncomingFolder))
            {
                publishMessage(file);
            }

            base.OnStart();
        }


        protected override void OnStop()
        {
            base.OnStop();
        }

        public string IncomingFolder
        {
            get;
            set;
        }

        public string ProcessedFolder
        {
            get;
            set;
        }

        int _maxHeaderSize = Int32.MaxValue;

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

        object _publishLock = new object();
        bool publishMessage(string path)
        {
            lock (_publishLock) // note, this will only allow a single thread to publish at a time
            {
                try
                {                    
                    FileStream fileStream = null;
                    DateTime startTime = DateTime.Now;
                    while (fileStream == null)
                    {
                        try
                        {
                            fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                        }
                        catch(FileNotFoundException)
                        {
                            throw;
                        }
                        catch (IOException ex)
                        {
                            System.Diagnostics.Trace.WriteLine(ex);

                            Thread.Sleep(1000);
                            if (DateTime.Now - startTime > OpenTimeout)
                            {
                                throw new TimeoutException("Timed out while trying to open file");
                            }
                        }
                    }

                    try
                    {
                        XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
                        using (XmlDictionaryReader xmlReader = XmlDictionaryReader.CreateTextReader(fileStream, quotas))
                        {
                            Message message = Message.CreateMessage(xmlReader, _maxHeaderSize, MessageVersion.Default);
                            MessageDelivery delivery = Converter.CreateMessageDelivery(message);
                            Runtime.PublishOneWay(new PublishRequest(delivery.ContractType, delivery.Action, delivery.Message, delivery.Context));
                        }

                    }
                    finally
                    {
                        fileStream.Close();
                    }
                    File.Move(path, Path.Combine(ProcessedFolder, Path.GetFileName(path)));
                    return true;
                }
                catch (FileNotFoundException) // file no longer exists
                {
                    return false;
                }
            }
        }

        public TimeSpan OpenTimeout
        {
            get;
            private set;
        }

        void onFileCreated(object sender, FileSystemEventArgs e)
        {
            publishMessage(e.FullPath);
        }

        FileSystemWatcher _watcher;
    }
}
