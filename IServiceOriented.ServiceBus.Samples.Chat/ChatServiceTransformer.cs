using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    public class ChatServiceTransformer : TransformationDispatcher
    {
        protected override PublishRequest Transform(PublishRequest information)
        {
            SendMessageRequest original = (SendMessageRequest)information.Message;

            return new PublishRequest(typeof(IChatService2), information.Action, new SendMessageRequest2()
            {
                Title = "Untitled Message",
                From = original.From,
                To = original.To,
                Message = original.Message
            });
        }    
    }
}
