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
            _serviceBus.AddListener(new ListenerEndpoint(Guid.NewGuid(), "Chat Service", "ChatServer", "net.pipe://localhost/chatServer", typeof(IChatService), typeof(WcfListener<IChatService>)));
            _serviceBus.RegisterService(new WcfManagementService());
            _serviceBus.UnhandledException+= (o, ex) =>
                {
                    Console.WriteLine("Unhandled Exception: "+ex.ExceptionObject);
                };
            
            IChatService noListenerReply = new UnhandledReplyHandler(_serviceBus);
            MethodDispatcherConfiguration.For(_serviceBus).RegisterTarget(typeof(IChatService), noListenerReply);

            _serviceBus.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "No subscribers", "ChatClient", "", typeof(IChatService), typeof(MethodDispatcher<IChatService>), new UnhandledMessageFilter(typeof(SendMessageRequest))));
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
                _serviceBus.Publish(typeof(IChatService), "SendMessage", new SendMessageRequest("System", request.From, request.To + " is an invalid user"));
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
