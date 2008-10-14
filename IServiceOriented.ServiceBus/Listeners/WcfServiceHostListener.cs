using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

using System.Runtime.Serialization;


namespace IServiceOriented.ServiceBus.Listeners
{
    [Serializable]
    [DataContract]
    public class WcfServiceHostListener : WcfListener
    {
        public WcfServiceHostListener()
        {            
        }

        /// <summary>
        /// Gets the type that should be hosted by the ServiceHost
        /// </summary>
        /// <returns></returns>
        protected virtual Type CreateServiceImplementationType()
        {
            Type hostType = WcfServiceHostFactory.CreateImplementationType(Endpoint.ContractType);
            return hostType;
        }

        /// <summary>
        /// Creates and initializes a service host for use with this listener
        /// </summary>
        /// <returns></returns>
        protected override ICommunicationObject CreateCommunicationObject()
        {
            return WcfServiceHostFactory.CreateHost(Runtime, Endpoint.ContractType, CreateServiceImplementationType(), Endpoint.ConfigurationName, Endpoint.Address);
        }        
    }
}
