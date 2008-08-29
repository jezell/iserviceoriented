using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    public class ChatClient
    {
        public ChatClient(string name)
        {
            _from = name;
            _handler = new IncomingHandler();
            _host = new ServiceHost(_handler, new Uri("net.pipe://localhost/chat/"+_from));
        }

        public void Start()
        {
            _host.Open();

            Service.Use<IServiceBusManagementService>(serviceBus =>
                {
                    serviceBus.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), _from, "ChatClient", "net.pipe://localhost/chat/"+_from+"/send", typeof(IChatService), typeof(WcfDispatcher<IChatService>), new ChatFilter(_from)));
                });
        }

        string _from;

        IncomingHandler _handler;        

        public void Send(string to, string message)
        {
            Service.Use<IChatService>("ChatClient", chatService =>
            {
                chatService.SendMessage(new SendMessageRequest(_from, to, message));
            });
        }

        ServiceHost _host;

        public void Stop()
        {
            _host.Close();
        }
        
        [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConfigurationName="ChatServer")]
        public class IncomingHandler : IChatService
        {
            #region IChatService Members
            [OperationBehavior]
            public void SendMessage(SendMessageRequest request)
            {
                Console.WriteLine(request.From + ": " + request.Message);
            }
            #endregion
        }       

    }

    
    [DataContract]
    public class ChatFilter : MessageFilter
    {
        public ChatFilter()
        {
        }

        public ChatFilter(string to)
        {
            To = to;
        }
        [DataMember]
        public string To;

        protected override string CreateInitString()
        {
            return To;
        }

        protected override void InitFromString(string data)
        {
            To = data;
        }

        public override bool Include(string action, object message)
        {
            SendMessageRequest request = message as SendMessageRequest;
            if (request != null)
            {
                return String.Compare(request.To, To, true) == 0;
            }
            return false;
        }
    }
}
