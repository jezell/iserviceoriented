using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Instructs the service bus to send unhandled messages of a certain type to this endpoint.
    /// </summary>
    [DataContract]
    public class UnhandledMessageFilter : TypedMessageFilter
    {
        public UnhandledMessageFilter(Type messageType)
            : base(messageType)
        {
        }

        public UnhandledMessageFilter(params Type[] messageTypes) : base(messageTypes)
        {
        }

        public UnhandledMessageFilter(bool inherit, Type messageType)
            : base(inherit, messageType)
        {
        }

        public UnhandledMessageFilter(bool inherit, params Type[] messageTypes)
            : base(inherit, messageTypes)
        {
        }
        
        public override bool Include(PublishRequest request)
        {
            return base.Include(request);
        }
    }
}
