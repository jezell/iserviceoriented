using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public class CorrelationMessageFilter : MessageFilter
    {
        public CorrelationMessageFilter(string correlationId) : this(new string[] { correlationId })
        {
            
        }

        public CorrelationMessageFilter(string[] correlationIds)
        {
            CorrelationIds = correlationIds;
        }


        public string[] CorrelationIds
        {
            get;
            private set;
        }


        public override bool Include(PublishRequest request)
        {
            if (request.Context.ContainsKey(MessageDelivery.CorrelationId))
            {
                foreach (string replyToMessageId in CorrelationIds)
                {
                    return ((string)request.Context[MessageDelivery.CorrelationId] == replyToMessageId);
                }
            }
            return false;
        }
        
    }
}
