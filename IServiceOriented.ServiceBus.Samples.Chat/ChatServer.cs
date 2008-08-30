using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    public class ChatServer
    {
        public ChatServer()
        {            
            _serviceBus = new ServiceBusRuntime(new MsmqMessageDeliveryQueue(".\\private$\\chat_deliver", true), new MsmqMessageDeliveryQueue(".\\private$\\chat_retry", true), new MsmqMessageDeliveryQueue(".\\private$\\chat_fail", true));
            _serviceBus.AddListener(new ListenerEndpoint(Guid.NewGuid(), "Chat Service", "ChatServer", "net.pipe://localhost/chatServer", typeof(IChatService),  new WcfListener<IChatService>()));
            _serviceBus.AddListener(new ListenerEndpoint(Guid.NewGuid(), "Chat Service2", "ChatServer2", "net.pipe://localhost/chatServer2", typeof(IChatService2), new WcfListener<IChatService2>()));
            _serviceBus.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "Chat Service Transformer", null, null, typeof(IChatService), new ChatServiceTransformer(), new TypedMessageFilter(typeof(SendMessageRequest))));
            _serviceBus.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "No subscribers", "ChatClient", "", typeof(IChatService), new MethodDispatcher<IChatService>(new UnhandledReplyHandler(_serviceBus)), new UnhandledMessageFilter(typeof(SendMessageRequest))));
            _serviceBus.RegisterService(new WcfManagementService());
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
                _serviceBus.Publish(new PublishRequest(typeof(IChatService), "SendMessage", new SendMessageRequest("System", request.From, request.To + " is an invalid user")));
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
