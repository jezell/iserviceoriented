using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ServiceModel;
using System.ServiceModel.Channels;

namespace IServiceOriented.ServiceBus
{
    [ServiceContract]
    public interface IPassThroughServiceContract
    {
        [OperationContract(Action="*", IsOneWay=true)]
        void Send(Message message);
    }
}
