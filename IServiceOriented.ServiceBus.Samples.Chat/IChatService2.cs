using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;
using System.ServiceModel;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    [ServiceContract]
    public interface IChatService2
    {
        [OperationContract(IsOneWay = true)]
        void SendMessage(SendMessageRequest2 request);
    }
    
    [DataContract]
    public class SendMessageRequest2
    {
        public SendMessageRequest2()
        {
        }

        public SendMessageRequest2(string title, string from, string to, string message)
        {
            From = from;
            To = to;
            Message = message;
            Title = title;
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

        [DataMember]
        public string Title
        {
            get;
            set;
        }
    }
}
