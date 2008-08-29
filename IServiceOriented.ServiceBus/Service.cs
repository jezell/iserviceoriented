using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace IServiceOriented.ServiceBus
{    
    public static class Service
    {        
        public static void Use<T>(Action<T> codeBlock)
        {
            ChannelFactory<T> channelFactory = new ChannelFactory<T>("");
            
            IClientChannel proxy = (IClientChannel)channelFactory.CreateChannel();
            bool success = false;
            try
            {
                codeBlock((T)proxy);
                proxy.Close();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    proxy.Abort();
                }
            }
        }

        public static void Use<T>(string config, Action<T> codeBlock)
        {
            ChannelFactory<T> channelFactory = new ChannelFactory<T>(config);
            IClientChannel proxy = (IClientChannel)channelFactory.CreateChannel();
            bool success = false;
            try
            {
                codeBlock((T)proxy);
                proxy.Close();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    proxy.Abort();
                }
            }
        }

        public static void Use<T>(string config, string address, Action<T> codeBlock)
        {
            ChannelFactory<T> channelFactory = new ChannelFactory<T>(config);
            channelFactory.Endpoint.Address = new EndpointAddress(address);
            IClientChannel proxy = (IClientChannel)channelFactory.CreateChannel();
            bool success = false;
            try
            {
                codeBlock((T)proxy);
                proxy.Close();
                success = true;
            }
            finally
            {
                if (!success)
                {
                    proxy.Abort();
                }
            }
        }
    }
}
