using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus
{
    public class PredicateMessageFilter : MessageFilter
    {
        public PredicateMessageFilter(Predicate<PublishRequest> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate");

            _pred = predicate;
        }
        Predicate<PublishRequest> _pred;

        public override bool Include(PublishRequest request)
        {
            return _pred(request);
        }
    }
}
