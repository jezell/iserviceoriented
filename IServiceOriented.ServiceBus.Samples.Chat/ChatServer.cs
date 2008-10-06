using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IServiceOriented.ServiceBus.Collections;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    public class ChatServer
    {
        public ChatServer()
        {            
            _serviceBus = new ServiceBusRuntime(SimpleServiceLocator.With(new MsmqMessageDeliveryQueue(".\\private$\\chat_deliver", true), new MsmqMessageDeliveryQueue(".\\private$\\chat_retry", true), new MsmqMessageDeliveryQueue(".\\private$\\chat_fail", true), SimpleServiceLocator.With(new WcfManagementService())));
            _serviceBus.AddListener(new ListenerEndpoint(Guid.NewGuid(), "Chat Service", "ChatServer", "http://localhost/chatServer", typeof(IChatService), new WcfListener()));
            _serviceBus.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "No subscribers", "ChatClient", "", typeof(IChatService), new MethodDispatcher(new UnhandledReplyHandler(_serviceBus)), new UnhandledMessageFilter(typeof(SendMessageRequest))));
            _serviceBus.UnhandledException+= (o, ex) =>
                {
                    Console.WriteLine("Unhandled Exception: "+ex.ExceptionObject);
                };
                        
        }

        class UnhandledReplyHandler : IChatService
        {
            public UnhandledReplyHandler(ServiceBusRuntime serviceBus)
            {
                _serviceBus = serviceBus;
            }

            ServiceBusRuntime _serviceBus;
            public void SendMessage(SendMessageRequest request)
            {
                if (request.From != "System")
                {
                    _serviceBus.Publish(new PublishRequest(typeof(IChatService), "SendMessage", new SendMessageRequest("System", request.From, request.To + " is an invalid user"), 
                        new MessageDeliveryContext(new KeyValuePair<string,object>[] { new KeyValuePair<string,object>(MessageDelivery.PrimaryIdentityNameKey, "SYSTEM" ) })));
                }
            }
        }
        
        ServiceBusRuntime _serviceBus;

        public void Start()
        {
            _serviceBus.Start();
        }

        public void Stop()
        {
            _serviceBus.Stop();
        }
        
        
    }

}
