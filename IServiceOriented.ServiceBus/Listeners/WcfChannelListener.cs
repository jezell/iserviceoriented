using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;

using System.Threading;
using IServiceOriented.ServiceBus.Threading;

namespace IServiceOriented.ServiceBus.Listeners
{
    [Serializable]
    [DataContract]
    public abstract class WcfChannelListener : WcfListener
    {
        protected WcfChannelListener()
        {
            
        }

        protected abstract IChannelListener<IInputChannel> CreateChannelListener();

        protected override void OnStart()
        {
            base.OnStart();
            _workers = new WorkerThreads(TimeSpan.FromSeconds(10), worker);
            _workers.AddWorkers(Environment.ProcessorCount);                                       
         }

        protected override void OnStop()
        {
            _workers.RemoveAll();
            _workers.Dispose();
            _workers = null;
            base.OnStop();
        }

        [NonSerialized]
        WorkerThreads _workers;
        
        void worker(TimeSpan timeout, object state)
        {
            try
            {
                IInputChannel inputChannel = ChannelListener.AcceptChannel(timeout);
                Message message = inputChannel.Receive();
                Runtime.PublishOneWay(new PublishRequest(typeof(IPassThroughServiceContract), message.Headers.Action, message));
            }
            catch (TimeoutException)
            {
            }
        }

        public IChannelListener<IInputChannel> ChannelListener
        {
            get
            {
                return ((IChannelListener<IInputChannel>)CommunicationObject);
            }
        }


        sealed protected override System.ServiceModel.ICommunicationObject CreateCommunicationObject()
        {            
            return CreateChannelListener();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_workers != null)
                {
                    _workers.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
