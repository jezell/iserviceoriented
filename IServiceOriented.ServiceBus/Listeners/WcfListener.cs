using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

using System.Runtime.Serialization;
using System.ServiceModel.Channels;

namespace IServiceOriented.ServiceBus.Listeners
{    
    [Serializable]
    [DataContract]
    public abstract class WcfListener : Listener
    {
        protected WcfListener()
        {            
        }

        protected override void OnStart()
        {
            base.OnStart();
            CommunicationObject = CreateCommunicationObject();
            CommunicationObject.Open();
        }

        protected override void OnStop()
        {
            CommunicationObject.Close();
            CommunicationObject = null;
            base.OnStop();
        }
        
        protected abstract ICommunicationObject CreateCommunicationObject();

        protected ICommunicationObject CommunicationObject
        {
            get;
            private set;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (CommunicationObject != null)
                {
                    if (CommunicationObject.State != CommunicationState.Closed) CommunicationObject.Close();
                }
            }
            base.Dispose(disposing);
        }
        
    }
	
}
