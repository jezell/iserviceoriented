using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    [DataContract]
    public class ChatServiceTransformer : TransformationDispatcher
    {
        protected override PublishRequest Transform(PublishRequest information)
        {            
            SendMessageRequest original = information.Message as SendMessageRequest;
            if (original != null)
            {
                return new PublishRequest(typeof(IChatService2), information.Action, new SendMessageRequest2()
                {
                    Title = "Untitled Message",
                    From = original.From,
                    To = original.To,
                    Message = original.Message
                });
            }
            else
            {
                SendMessageRequest2 original2 = information.Message as SendMessageRequest2;
                if (original2 != null)
                {
                    return new PublishRequest(typeof(IChatService), information.Action, new SendMessageRequest()
                    {
                        From = original2.From,
                        Message = original2.Message,
                        To = original2.To
                    });
                }
            }
            throw new NotSupportedException();
        }    
    }
}
