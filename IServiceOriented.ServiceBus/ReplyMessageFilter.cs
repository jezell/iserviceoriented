using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public class ReplyMessageFilter : MessageFilter
    {
        public ReplyMessageFilter(string replyToMessageId) : this(new string[] { replyToMessageId })
        {
            
        }

        public ReplyMessageFilter(string[] replyToMessageIds)
        {
            ReplyToMessageIds = replyToMessageIds;
        }


        public string[] ReplyToMessageIds
        {
            get;
            private set;
        }


        public override bool Include(PublishRequest request)
        {
            if (request.Context.ContainsKey(MessageDelivery.ReplyToMessageId))
            {
                foreach (string replyToMessageId in ReplyToMessageIds)
                {
                    return ((string)request.Context[MessageDelivery.ReplyToMessageId] == replyToMessageId);
                }
            }
            return false;
        }
        
    }
}
