using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Reflection;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;
using IServiceOriented.ServiceBus.Listeners;


namespace IServiceOriented.ServiceBus.Dispatchers
{
    [Serializable]
    [DataContract]
    public abstract class WcfDispatcher : Dispatcher
    {
        protected WcfDispatcher()
        {
            
        }

        protected WcfDispatcher(SubscriptionEndpoint endpoint)
            : base(endpoint)
        {
     
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnStop()
        {
            base.OnStop();
        }
     
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
