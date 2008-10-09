using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus.Listeners
{
    /// <summary>
    /// Provides support for hosting WCF service contracts with the service bus.
    /// </summary>
    /// <remarks>
    /// Currently, contracts are restricted to one way, single parameter, methods.
    /// </remarks>
    [Serializable]
    [DataContract]
    public class WcfListener : Listener
    {
        public WcfListener()
        {            
        }
        protected override void OnStart()
        {
            _host = CreateServiceHost();
            _host.Open();
        }

        /// <summary>
        /// Gets the type that should be hosted by the ServiceHost
        /// </summary>
        /// <returns></returns>
        protected virtual Type GetServiceImplementationType()
        {
            Type hostType = WcfServiceHostFactory.CreateImplementationType(Endpoint.ContractType);
            return hostType;
        }

        /// <summary>
        /// Creates and initializes a service host for use with this listener
        /// </summary>
        /// <returns></returns>
        protected virtual ServiceHost CreateServiceHost()
        {
            return WcfServiceHostFactory.CreateHost(Runtime, Endpoint.ContractType, GetServiceImplementationType(), Endpoint.ConfigurationName, Endpoint.Address);
        }


        protected override void OnStop()
        {
            if(_host != null) _host.Close();
            _host = null;            
        }

        [NonSerialized]
        ServiceHost _host;
        protected ServiceHost Host
        {
            get
            {
                return _host;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_host != null)
                {
                    _host.Close();
                }
            }
        }
    }
	
}
