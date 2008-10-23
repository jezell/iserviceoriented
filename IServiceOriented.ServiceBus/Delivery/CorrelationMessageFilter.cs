using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace IServiceOriented.ServiceBus.Delivery
{
    public class CorrelationMessageFilter : MessageFilter
    {
        public CorrelationMessageFilter(string correlationId) : this(new string[] { correlationId })
        {
            
        }

        public CorrelationMessageFilter(IEnumerable<string> correlationIds)
        {
            CorrelationIds = new ReadOnlyCollection<string>(correlationIds.ToList());
        }


        public ReadOnlyCollection<string> CorrelationIds
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
                    if ((string)request.Context[MessageDelivery.CorrelationId] == replyToMessageId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
    }
}
