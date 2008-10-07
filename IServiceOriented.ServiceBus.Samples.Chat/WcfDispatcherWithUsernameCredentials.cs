using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    public class WcfDispatcherWithUsernameCredentials : WcfDispatcher
    {
        protected override void ApplySecurityContext(System.ServiceModel.ChannelFactory factory)
        {
            if (DispatchContext.MessageDelivery.Context.ContainsKey(MessageDelivery.PrimaryIdentityNameKey))
            {
                factory.Credentials.UserName.UserName = (string)DispatchContext.MessageDelivery.Context[MessageDelivery.PrimaryIdentityNameKey];
                factory.Credentials.UserName.Password = "";
            }
        }
    }
}
