using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;
using System.ServiceModel;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    [ServiceContract]
    public interface IChatService
    {
        [OperationContract(IsOneWay=true)]
        void SendMessage(SendMessageRequest request);
    }

    [DataContract]
    public class SendMessageRequest
    {
        public SendMessageRequest()
        {
        }

        public SendMessageRequest(string from, string to, string message)
        {
            From = from;
            To = to;
            Message = message;
        }

        [DataMember]
        public string From
        {
            get;
            set;
        }

        [DataMember]
        public string To
        {
            get;
            set;
        }

        [DataMember]
        public string Message
        {
            get;
            set;
        }
    }
}
