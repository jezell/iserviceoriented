using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.ServiceModel;


namespace IServiceOriented.ServiceBus.Samples.Chat
{
    public class NullUserNamePasswordValidator : UserNamePasswordValidator
    {
        public override void Validate(string userName, string password)
        {
            // Allow everything through
            System.Diagnostics.Trace.WriteLine("Ignoring username and password");
        }
    }
}
