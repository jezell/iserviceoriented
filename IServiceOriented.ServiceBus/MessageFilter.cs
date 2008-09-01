using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;

namespace IServiceOriented.ServiceBus
{
    /// <summary>
    /// Base class for message filters.
    /// </summary>
    [Serializable]
    [DataContract]
    public abstract class MessageFilter
    {
        /// <summary>
        /// Determine whether a message should be included or skipped.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>true if the message should be included, false if the message should be skipped.</returns>
        public abstract bool Include(PublishRequest request);        
    }
	
		
}
