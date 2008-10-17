using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
using IServiceOriented.ServiceBus.Dispatchers;
using IServiceOriented.ServiceBus.Services;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    public class ChatClient
    {
        public ChatClient(string name)
        {
            _from = name;
            _host = new ServiceHost(new ChatClientService(), new Uri("http://localhost/chat/" + _from));          
        }

        Guid _id = Guid.NewGuid();

        public void Start()
        {
            _host.Open();

            Service.Use<IServiceBusManagementService>(service =>
                {
                    service.Subscribe(new SubscriptionEndpoint(_id, "chat", "ChatClientOut", _host.Description.Endpoints[0].Address.ToString(),
                            typeof(IChatService), new WcfDispatcherWithUsernameCredentials(), new ChatFilter() { To = _from }));
                });
        }

        string _from;   

        
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

            Service.Use<IServiceBusManagementService>(service =>
            {
                service.Unsubscribe(_id);
            });
        }        
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConfigurationName = "ChatServerOut")]
    
    public class ChatClientService : IChatService
    {
        #region IChatService Members
        [OperationBehavior]
        public void SendMessage(SendMessageRequest request)
        {
            if (ServiceSecurityContext.Current != null && ServiceSecurityContext.Current.PrimaryIdentity != null) Console.WriteLine("Identity: " + ServiceSecurityContext.Current.PrimaryIdentity.Name);
            Console.WriteLine(request.From + ": " + request.Message);
        }
        #endregion
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
        
        public override bool Include(PublishRequest request)
        {            
            SendMessageRequest r = request.Message as SendMessageRequest;
            if (r != null)
            {
                return String.Compare(r.To, To, true) == 0;
            }
            return false;
        }
    }

}
