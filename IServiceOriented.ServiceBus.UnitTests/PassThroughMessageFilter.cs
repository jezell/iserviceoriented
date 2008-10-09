using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus.UnitTests
{

    [Serializable]
    [DataContract]
    public class PassThroughMessageFilter : MessageFilter
    {
        public override bool Include(PublishRequest request)
        {
            return true;
        }

    }

}
