using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IServiceOriented.ServiceBus.Dispatchers;
using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    [DataContract]
    public class WcfDispatcherWithUsernameCredentials : WcfProxyDispatcher
    {
        protected override void ApplySecurityContext(MessageDelivery delivery, System.ServiceModel.ChannelFactory factory)
        {
            if (delivery.Context.ContainsKey(MessageDelivery.PrimaryIdentityNameKey))
            {
                factory.Credentials.UserName.UserName = (string)delivery.Context[MessageDelivery.PrimaryIdentityNameKey];
                factory.Credentials.UserName.Password = "";
            }
        }
    }
}
