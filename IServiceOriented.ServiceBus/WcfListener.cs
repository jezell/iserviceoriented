using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    [Serializable]
    [DataContract]
    public class WcfListener : Listener
    {
        public WcfListener()
        {            
        }
        protected override void OnStart()
        {
            _host = WcfServiceHostFactory.CreateHost(Runtime, Endpoint.ContractType, Endpoint.ConfigurationName, Endpoint.Address);
            _host.Open();
        }

        protected override void OnStop()
        {
            if(_host != null) _host.Close();
            _host = null;            
        }

        [NonSerialized]
        ServiceHost _host;

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
