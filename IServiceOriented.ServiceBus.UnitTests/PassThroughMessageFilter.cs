using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.UnitTests
{

    [Serializable]
    public class PassThroughMessageFilter : MessageFilter
    {
        public override bool Include(PublishRequest request)
        {
            return true;
        }

    }

}
