using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [Serializable]
    public class BooleanMessageFilter : MessageFilter
    {
        public BooleanMessageFilter(bool messageFilter)
        {
            _messageFilter = messageFilter;
        }

        bool _messageFilter;

        public override bool Include(string action, object message)
        {
            return _messageFilter;
        }
        
    }
}
