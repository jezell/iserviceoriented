using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ServiceModel;

namespace IServiceOriented.ServiceBus.Test
{
	public class TestClass
	{
        public class ReadOnlyList<T> : IEnumerable<T>
        {
            IList<T> _list;
            public ReadOnlyList(IList<T> list)
            {
                _list = list;
            }

            public T this[int index]
            {
                get
                {
                    return _list[index];
                }
            }

            public int Count
            {
                get
                {
                    return _list.Count;
                }
            }
            
            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            #endregion
        }
		public static void Main()
		{            
            List<object> l = new List<object>();
            l.Add(1);
            l.Add(2);
            l.Add(3);            
            ReaderWriterLockedObject<ReadOnlyList<object>, List<object>> lockedObject = new ReaderWriterLockedObject<ReadOnlyList<object>, List<object>>(l, c =>  new ReadOnlyList<object> (c));
            
            lockedObject.Read(list =>
                {
                    foreach (object o in list)
                    {
                        Console.WriteLine(o);
                    }
                });

            Console.ReadLine();

			ServiceHost remoteEndpointHost = new ServiceHost(typeof(Hello));
			remoteEndpointHost.Open();

            ServiceBusRuntime serviceBus = new ServiceBusRuntime(new MsmqMessageDeliveryQueue(".\\private$\\esb_delivery_queue"), new MsmqMessageDeliveryQueue(".\\private$\\esb_retry_queue"), new MsmqMessageDeliveryQueue(".\\private$\\esb_failure_queue"));

            ListenerEndpoint endpoint = new ListenerEndpoint(Guid.NewGuid(), "HelloListener", "IServiceOriented.ServiceBus.HelloListener", "net.pipe://localhost/hello", typeof(IHello));
			serviceBus.ListenerAdded += (o, lea) => { System.Diagnostics.Debug.WriteLine("Listening started on "+lea.Endpoint.Address); };
			serviceBus.ListenerRemoved += (o, lea) => { System.Diagnostics.Debug.WriteLine("Listening stopped on "+lea.Endpoint.Address); };
			serviceBus.AddListener(endpoint);
            serviceBus.RegisterService(new LogFailedMessagesService());
            serviceBus.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "", "IServiceOriented.ServiceBus.HelloListener", "net.pipe://localhost/remotehello", typeof(IHello), typeof(WcfDispatcher<IHello>), new StringMessageFilter() ));
			WcfListenerEndpointHostingService endpointHostingService = new WcfListenerEndpointHostingService();
			endpointHostingService.HostStarted += (o, lea) => { System.Diagnostics.Debug.WriteLine("ServiceHost started on "+lea.Endpoint.Address); };
			endpointHostingService.HostStopped += (o, lea) => { System.Diagnostics.Debug.WriteLine("ServiceHost stopped on "+lea.Endpoint.Address); };
			endpointHostingService.RegisterHost(typeof(IHello), typeof(HelloListener));
			serviceBus.RegisterService(endpointHostingService);
            serviceBus.UnhandledException += new UnhandledExceptionEventHandler(serviceBus_UnhandledException);
            serviceBus.Start();

            Service<IHello>.Use("listener", hello =>
            {
                hello.Print("This should not be delivered" + DateTime.Now);
            });

			
			Service<IHello>.Use("listener", hello =>
			{
                hello.Print("Hello via hosted endpoint *" + DateTime.Now);
			});

            Service<IHello>.Use("listener", hello =>
            {
                hello.Print("Hello via hosted endpoint *" + DateTime.Now);
            });

            Service<IHello>.Use("listener", hello =>
            {
                hello.Print("Hello via hosted endpoint *" + DateTime.Now);
            });

            Service<IHello>.Use("listener", hello =>
            {
                hello.Print("Hello via hosted endpoint * " + DateTime.Now);
            });
		
			
            Console.WriteLine("Service started. Press enter to stop...");
            Console.ReadLine();

            serviceBus.Stop();

            remoteEndpointHost.Close();

			
		}

        static void serviceBus_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("UNHANDLED EXCEPTION: " + e.ExceptionObject);
        }
	}
	
    [Serializable]
	public class StringMessageFilter : MessageFilter
	{
		public override bool Include(string action, object message)
		{
			return Convert.ToString(message).Contains("*");
		}

        protected override void InitFromData(string data)
        {
            
        }

        protected override string GetInitData()
        {
            return null;
        }
	}

    public class LogFailedMessagesService : RuntimeService
    {
        protected override void OnMessageDelivered(MessageDelivery delivery)
        {
            Console.WriteLine("DELIVERED: " + delivery.Message);            
            base.OnMessageDelivered(delivery);
        }
        protected override void OnMessageDeliveryFailed(MessageDelivery delivery, bool permanent)
        {
            Console.WriteLine("FAILED: "+delivery.Message);
            Console.WriteLine("IS PERMANENT=" + permanent);
            base.OnMessageDeliveryFailed(delivery, permanent);
        }
    }
	
	[ServiceContract]
	public interface IHello
	{
		[OperationContract(Action="Print")]
		void Print(string message);
	}
		
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
	public class HelloListener : ListenerBase, IHello
	{
		
		[OperationBehavior]
		public void Print(string message)
		{
			System.Diagnostics.Debug.WriteLine("Forwarding to bus"+message);
			Runtime.Publish(typeof(HelloListener), "Print", message);
		}
		
	}
	
	[ServiceBehavior]
	public class Hello : IHello
	{
		[OperationBehavior]
		public void Print(string message)
		{
			count++;
			if(count > 5)
			{
				System.Diagnostics.Debug.WriteLine(message);
			}
			else
			{
				throw new Exception("Retry this");
			}
		}
		
		static int count = 0;
	}
}