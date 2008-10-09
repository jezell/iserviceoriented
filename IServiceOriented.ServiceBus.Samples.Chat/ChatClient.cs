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
            _host = new ServiceHost(new ChatClientService(), new Uri("http://localhost/chat/" + _from));            
        }

        public void Start()
        {
            _host.Open();         
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
        }        
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConfigurationName = "ChatServerOut")]
    [AutoSubscribe("Autosubscribed", "ChatClientOut", typeof(IChatService), DispatcherType = typeof(WcfDispatcherWithUsernameCredentials))]
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
