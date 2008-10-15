using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IServiceOriented.ServiceBus.Collections;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Services;
using IServiceOriented.ServiceBus.Listeners;
using IServiceOriented.ServiceBus.Dispatchers;
using IServiceOriented.ServiceBus.Delivery.Formatters;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    public class ChatServer
    {
        public ChatServer()
        {
            MessageDeliveryFormatter formatter = new MessageDeliveryFormatter(typeof(IChatService));            
            _serviceBus = new ServiceBusRuntime(new QueuedDeliveryCore(new MsmqMessageDeliveryQueue(".\\private$\\chat_deliver", true, formatter), new MsmqMessageDeliveryQueue(".\\private$\\chat_retry", true, formatter), new MsmqMessageDeliveryQueue(".\\private$\\chat_fail", true, formatter)), new WcfManagementService());
            _serviceBus.AddListener(new ListenerEndpoint(Guid.NewGuid(), "Chat Service", "ChatServer", "http://localhost/chatServer", typeof(IChatService), new WcfServiceHostListener()));
            _serviceBus.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "No subscribers", "ChatClient", "", typeof(IChatService), new MethodDispatcher(new UnhandledReplyHandler(_serviceBus)), new UnhandledMessageFilter(typeof(SendMessageRequest)), true));
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
                    _serviceBus.PublishOneWay(new PublishRequest(typeof(IChatService), "SendMessage", new SendMessageRequest("System", request.From, request.To + " is an invalid user"),
                        new MessageDeliveryContext(new KeyValuePair<MessageDeliveryContextKey, object>[] { new KeyValuePair<MessageDeliveryContextKey, object>(MessageDelivery.PrimaryIdentityNameKey, "SYSTEM") })));
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
