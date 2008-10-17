using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using IServiceOriented.ServiceBus.Delivery.Formatters;

namespace IServiceOriented.ServiceBus.Dispatchers
{
    [Serializable]
    [DataContract]
    public abstract class WcfChannelDispatcher : WcfDispatcher
    {
        protected abstract ChannelFactory<IOutputChannel> CreateChannelFactory();

        
        protected override void OnStart()
        {
            _converter = MessageDeliveryConverter.CreateConverter(Endpoint.ContractType);
            base.OnStart();
        }

        [NonSerialized]
        MessageDeliveryConverter _converter;

        public override void Dispatch(MessageDelivery messageDelivery)
        {
            IOutputChannel channel = CreateChannelFactory().CreateChannel();
            bool closed = false;            
            try
            {
                channel.Open();
                channel.Send(_converter.ToMessage(messageDelivery));
            }
            finally
            {
                channel.Close();
                closed = true;
            }
            if (!closed)
            {
                channel.Abort();
            }
        }


    }
}
