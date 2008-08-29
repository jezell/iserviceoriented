using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    [Serializable]
    public abstract class MessageFilter
    {
        public abstract bool Include(string action, object message);

        protected internal abstract void InitFromString(string data);
        protected internal abstract string CreateInitString();

    }
	
		
}
