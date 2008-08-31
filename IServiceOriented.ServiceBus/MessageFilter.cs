using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    [Serializable]
    [DataContract]
    public abstract class MessageFilter
    {
        public abstract bool Include(string action, object message);        
    }
	
		
}
