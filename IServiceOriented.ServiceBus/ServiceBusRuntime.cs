using System;
using System.Runtime.Serialization;
using System.Linq;
using System.Collections.ObjectModel;
using System.Transactions;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;

using Microsoft.Practices.ServiceLocation;
using IServiceOriented.ServiceBus.Threading;
using IServiceOriented.ServiceBus.Collections;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Dispatchers;

namespace IServiceOriented.ServiceBus
{
	public class ServiceBusRuntime : IDisposable
	{
        public ServiceBusRuntime(IServiceLocator serviceLocator) 
        {            
            try
            {
                serviceLocator = Microsoft.Practices.ServiceLocation.ServiceLocator.Current;
            }
            catch(NullReferenceException) // 1.0 throws null ref exception if no provider delegate is set.
            {
            }

            if (serviceLocator == null)
            {                
                serviceLocator = SimpleServiceLocator.With(new Delivery.DirectDeliveryCore()); // default to simple service locator with direct message delivery
                
            }

            _serviceLocator = serviceLocator;

            attachServices();

            _subscriptionEndpoint = new SubscriptionEndpoint(Guid.Empty, "Subscription endpoint", null, null, (Type)null, new SubscriptionDispatcher(() => this.ListSubscriptions(true)), new PredicateMessageFilter(pr => false), true, null);
            _correlatorEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "Correlator", null, null, (Type)null, new ActionDispatcher((se, md) =>
                {
                    _correlator.Reply((string)md.Context[MessageDelivery.CorrelationId], md);
                }), new PredicateMessageFilter(pr => pr.Context.ContainsKey(MessageDelivery.CorrelationId)), true, null);
            
            Subscribe(_subscriptionEndpoint);
            Subscribe(_correlatorEndpoint);            
        }

        public ServiceBusRuntime(params RuntimeService[] runtimeServices) : this(SimpleServiceLocator.With(runtimeServices))
        {            
        }
		public ServiceBusRuntime() : this((IServiceLocator)null)
		{
            
		}
        Correlator _correlator = new Correlator();
        SubscriptionEndpoint _correlatorEndpoint;
        
        void attachServices()
        {
            foreach (RuntimeService rs in ServiceLocator.GetAllInstances<RuntimeService>())
            {
                rs.Attach(this);
            }
        }
		
		object _startLock = new object();
				
        IServiceLocator _serviceLocator;
        public IServiceLocator ServiceLocator
        {
            get
            {
                return _serviceLocator;
            }
        }
			                      
        
		public void Start()
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            try
            {
                System.Diagnostics.Trace.Write("Starting Bus. Service Bus Core = " + ServiceLocator.GetInstance<DeliveryCore>());
            }
            catch(ActivationException)
            {
                throw new InvalidOperationException("An object implementing ServiceBusCore must be registered with the ServiceLocator before starting the bus");
            }

            lock (_startLock)
            {
                if (_started)
                {
                    throw new InvalidOperationException("The service bus is already started.");
                }

                IEnumerable<RuntimeService> runtimeServices = ServiceLocator.GetAllInstances<RuntimeService>();
                foreach (RuntimeService rs in runtimeServices)
                {
                    rs.Validate();
                }
                                               
                _subscriptions.Read(subscriptions =>
                {
                    foreach (SubscriptionEndpoint se in subscriptions)
                    {
                        se.Dispatcher.StartInternal();
                    }
                });                   

                foreach (RuntimeService rs in runtimeServices)
                {
                    // start delivery after other services
                    if (!(rs is DeliveryCore))
                    {
                        rs.StartInternal();
                    }
                }

                foreach (DeliveryCore core in ServiceLocator.GetAllInstances<DeliveryCore>())
                {
                    core.StartInternal();
                }

                lock (_listenerEndpointsLock)
                {
                    foreach (ListenerEndpoint le in _listenerEndpoints)
                    {
                        le.Listener.StartInternal();
                    }
                }

                EventHandler started = Started;
                if (started != null)
                {
                    started(this, EventArgs.Empty);
                }
                _started = true;                 
            }
		}
		    
	    
        const int peekDelay = 5000;

		public bool Stop()
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

			bool clean = true;
			lock(_startLock)
			{
                if (!_started)
                {
                    throw new InvalidOperationException("Service bus is already stopped");
                }
				
                lock(_listenerEndpointsLock)
                {
                    foreach (ListenerEndpoint le in _listenerEndpoints)
                    {
                        le.Listener.StopInternal();
                    }                
                }


                _subscriptions.Read(subscriptions =>
                {
                    foreach (SubscriptionEndpoint se in subscriptions)
                    {
                        se.Dispatcher.StopInternal();
                    }
                });

                // stop delivery first
                foreach (DeliveryCore core in ServiceLocator.GetAllInstances<DeliveryCore>())
                {
                    core.StopInternal();
                }
                // stop rest of services
                foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                {
                    if (!(service is DeliveryCore))
                    {
                        service.StopInternal();
                    }
                }

			
                _started = false;
			}

            EventHandler stopped = Stopped;
            if (stopped != null)
            {
                stopped(this, EventArgs.Empty);
            }
            
            return clean;
		}

        volatile bool _started;
         
        public event EventHandler Started;
        public event EventHandler Stopped;        

        public event EventHandler<EndpointEventArgs> Subscribed;
        public event EventHandler<EndpointEventArgs> Unsubscribed;
        
        public event EventHandler<EndpointEventArgs> ListenerAdded;
        public event EventHandler<EndpointEventArgs> ListenerRemoved;
        
		ListenerEndpointCollection _listenerEndpoints = new ListenerEndpointCollection();
        object _listenerEndpointsLock = new object();

		public IEnumerable<Endpoint> ListeningEndpoints
		{
			get
            {
                lock (_listenerEndpointsLock)
                {
                    return _listenerEndpoints.ToArray();
                }
			}
		}

        ReaderWriterLockedObject<IEnumerable<SubscriptionEndpoint>, SubscriptionEndpointCollection> _subscriptions = new ReaderWriterLockedObject<IEnumerable<SubscriptionEndpoint>, SubscriptionEndpointCollection>(new SubscriptionEndpointCollection(), l => l);
        
		public void Listen(ListenerEndpoint endpoint)
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (endpoint == null) throw new ArgumentNullException("endpoint");

            if (endpoint.Listener.Runtime != null)
            {
                throw new InvalidOperationException("Listener is attached to a bus already");
            }
            
            bool added = false;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    lock (_listenerEndpointsLock)
                    {
                        endpoint.Listener.Runtime = this;
                        _listenerEndpoints.Add(endpoint);                        
                        added = true;
                    }
                    
                    foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                    {
                        service.OnListenerAdded(endpoint);
                    }
            
                    EventHandler<EndpointEventArgs> listenEvent = ListenerAdded;
                    if (listenEvent != null) listenEvent(this, new EndpointEventArgs(endpoint));

                    if (_started)
                    {
                        endpoint.Listener.StartInternal();
                    }

                    ts.Complete();
                }
            }
            catch
            {
                if (added)
                {
                    // remove on failure
                    lock (_listenerEndpointsLock)
                    {
                        _listenerEndpoints.Remove(endpoint);
                    }
                }
                throw;                
            }            
		}

        public void StopListening(Guid endpointId)
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            lock (_listenerEndpointsLock)
            {
                ListenerEndpoint endpoint = _listenerEndpoints.FirstOrDefault(le => le.Id == endpointId);
                if (endpoint != null)
                {
                    if (_started)
                    {
                        endpoint.Listener.StopInternal();
                    }
                    StopListening(endpoint);

                    foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                    {
                        service.OnListenerRemoved(endpoint);
                    }
                
                    EventHandler<EndpointEventArgs> removed = ListenerRemoved;
                    if (removed != null)
                    {
                        removed(this, new EndpointEventArgs(endpoint));
                    }                    
                }
                else
                {
                    throw new ListenerNotFoundException();
                }
            }
        }
		
		public void StopListening(ListenerEndpoint endpoint)
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (endpoint == null) throw new ArgumentNullException("endpoint");


            bool removed = false;
            try
            {
			    using(TransactionScope ts = new TransactionScope())
			    {
				    lock(_listenerEndpointsLock)
				    {
					    _listenerEndpoints.Remove(endpoint);
                        endpoint.Listener.Runtime = null;
				    }

                    
                    foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                    {
                        service.OnListenerRemoved(endpoint);
                    }
                
                    EventHandler<EndpointEventArgs> unlistenEvent = ListenerRemoved;
                    if (unlistenEvent != null) unlistenEvent(this, new EndpointEventArgs(endpoint));

				    ts.Complete();
			    }
            }
            catch
            {
                if (removed)
                {
                    // re-add on failure
                    lock (_listenerEndpointsLock)
                    {
                        _listenerEndpoints.Add(endpoint);
                    }
                }
                throw;
            }

		}

        		
        public SubscriptionEndpoint GetSubscription(Guid subscriptionEndpointId)
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            SubscriptionEndpoint endpoint = null;
            _subscriptions.Read(subscriptions =>
                {
                    endpoint = subscriptions.FirstOrDefault(s => s.Id == subscriptionEndpointId);
                });
            return endpoint;
        }		

        int _maxRetries = 10;
        public int MaxRetries
        {
            get
            {
                return _maxRetries;
            }
            set
            {
                _maxRetries = value;
            }            
        }

        public void PublishOneWay(Type contractType, string action, object message)
        {
            PublishOneWay(new PublishRequest(contractType, action, message, new MessageDeliveryContext()));
        }

        SubscriptionEndpoint _subscriptionEndpoint;

        void deliverToSubscriptionEndpoint(TimeSpan timeout, PublishRequest publishRequest)
        {
            SubscriptionEndpoint se = _subscriptionEndpoint;

            publishRequest = PublishRequest.Copy(publishRequest, new KeyValuePair<MessageDeliveryContextKey, object>(MessageDelivery.PublishRequestId, publishRequest.PublishRequestId));

            using (TransactionScope ts = new TransactionScope())
            {
                MessageDelivery md = new MessageDelivery(Guid.NewGuid().ToString(), se.Id, publishRequest.ContractType, publishRequest.Action, publishRequest.Message, MaxRetries, 0, null, publishRequest.Context, DateTime.Now + timeout);
                DeliveryCore deliveryCore = ServiceLocator.GetInstance<DeliveryCore>();
                deliveryCore.Deliver(md);
                ts.Complete();
            }
        }

        // todo: get rid of this
        TimeSpan _oneWayTimeout = TimeSpan.FromSeconds(120);
        
        public void PublishOneWay(PublishRequest publishRequest)
        {
            deliverToSubscriptionEndpoint(_oneWayTimeout, publishRequest);
        }

        protected string GetResponseCorrelationId(PublishRequest request)
        {
            return request.PublishRequestId.ToString();
        }
     
		public MessageDelivery[] PublishTwoWay(PublishRequest publishRequest, TimeSpan timeout)
		{                                             
            CorrelatorAsyncResult result = null;

            string correlationId = GetResponseCorrelationId(publishRequest);            
            
            using (TransactionScope ts = new TransactionScope())
            {
                result = (CorrelatorAsyncResult)_correlator.BeginWaitForReply(correlationId, null, null);
                deliverToSubscriptionEndpoint(timeout, publishRequest);
                ts.Complete(); 
            }             

            _correlator.EndWaitForReply(result);
            return result.Results.ToArray();
		}

        public Collection<ListenerEndpoint> ListListeners()
        {
            return ListListeners(true);
        }

        public Collection<ListenerEndpoint> ListListeners(bool includeTransient)
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            Collection<ListenerEndpoint> endpoints = new Collection<ListenerEndpoint>();

            lock (_listenerEndpointsLock)
            {
                foreach (ListenerEndpoint endpoint in _listenerEndpoints)
                {
                    if(includeTransient || !endpoint.Transient) endpoints.Add(endpoint);
                }            
            }
            return endpoints;
        }

        public Collection<SubscriptionEndpoint> ListSubscribers()
        {
            return ListSubscriptions(true);
        }
        public Collection<SubscriptionEndpoint> ListSubscriptions(bool includeTransient)
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            Collection<SubscriptionEndpoint> endpoints = new Collection<SubscriptionEndpoint>();

            _subscriptions.Read(subscriptions =>
            {
                foreach (SubscriptionEndpoint endpoint in subscriptions)
                {
                    if (includeTransient || !endpoint.Transient)  endpoints.Add(endpoint);
                }
            });

            return endpoints;
        }
		
		public void Subscribe(SubscriptionEndpoint subscription)
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (subscription == null) throw new ArgumentNullException("subscription");

            if (subscription.Dispatcher.Runtime != null)
            {
                throw new InvalidOperationException("Subscription is attached to a bus already");
            }            

            bool added = false;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {                    
                    _subscriptions.Write(subscriptions =>
                    {
                        subscriptions.Add(subscription);
                        subscription.Dispatcher.Runtime = this;
                        added = true;
                    });

                    if (_started)
                    {
                        subscription.Dispatcher.StartInternal() ;
                    }


                    foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                    {
                        service.OnSubscriptionAdded(subscription);
                    }
                
                    EventHandler<EndpointEventArgs> subscribedEvent = Subscribed;
                    if (subscribedEvent != null) subscribedEvent(this, new EndpointEventArgs(subscription));                   

                    ts.Complete();
                }
            }
            catch
            {
                if (added)
                {                    
                    // Remove subscription on failure
                    _subscriptions.Write(subscriptions =>
                    {
                        subscriptions.Remove(subscription);
                    });
                }
                throw;                
            }
		}

        public void Unsubscribe(Guid subscriptionId)
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            SubscriptionEndpoint endpoint = null;
            _subscriptions.Read(subscriptions =>
                {
                    endpoint = subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
                });
            

            if (endpoint == null)
            {
                throw new SubscriptionNotFoundException();
            }

            Unsubscribe(endpoint);
        }
		
		public void Unsubscribe(SubscriptionEndpoint subscription)
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (subscription == null) throw new ArgumentNullException("subscription");

            bool removed = false;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    subscription.Dispatcher.StopInternal();

                    _subscriptions.Write(subscriptions =>
                    {
                        subscriptions.Remove(subscription);
                        subscription.Dispatcher.Runtime = null;
                        removed = true;
                    });


                    foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                    {
                        service.OnSubscriptionRemoved(subscription);
                    }
                
                    EventHandler<EndpointEventArgs> unsubscribedEvent = Unsubscribed;
                    if (unsubscribedEvent != null) unsubscribedEvent(this, new EndpointEventArgs(subscription));
                    
                    ts.Complete();
                }
            }
            catch
            {
                if (removed)
                {
                    // Re-add subscription on failure
                    _subscriptions.Write(subscriptions =>
                    {
                        subscriptions.Add(subscription);
                    });
                }
                throw;          
            }
		}

        internal void NotifyUnhandledException(Exception ex, bool isTerminating)
        {
            UnhandledExceptionEventHandler ueHandler = _unhandledException;
            if (ueHandler != null) ueHandler(this, new UnhandledExceptionEventArgs(ex, isTerminating));
        }


        internal void NotifyExpired(MessageDelivery delivery)
        {
            foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
            {
                service.OnMessageDeliveryExpired(delivery);
            }
            var handler = _messageDeliveryExpired;
            if (handler != null)
            {
                handler(this, new MessageDeliveryEventArgs() { MessageDelivery = delivery });
            }
        }
        
        internal void NotifyDelivery(MessageDelivery delivery)
        {
            foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
            {
                service.OnMessageDelivered(delivery);
            }
            var handler = _messageDelivered;
            if (handler != null)
            {
                handler(this, new MessageDeliveryEventArgs() { MessageDelivery = delivery });
            }
        }

        internal void NotifyFailure(MessageDelivery delivery, bool permanent)
        {
            foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
            {
                service.OnMessageDeliveryFailed(delivery, false);
            }

            var failed = _messageDeliveryFailed;
            if (failed != null) failed(this, new MessageDeliveryFailedEventArgs() { MessageDelivery = delivery, Permanent = permanent });
        }

        object _eventLock = new Object();

        UnhandledExceptionEventHandler _unhandledException;
        public event UnhandledExceptionEventHandler UnhandledException
        {
            add
            {
                lock (_eventLock)
                {
                    _unhandledException += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _unhandledException -= value;
                }
            }
        }

        EventHandler<MessageDeliveryEventArgs> _messageDelivered;
        public event EventHandler<MessageDeliveryEventArgs> MessageDelivered
        {
            add
            {
                lock (_eventLock)
                {
                    _messageDelivered += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _messageDelivered -= value;
                }
            }
        }


        EventHandler<MessageDeliveryEventArgs> _messageDeliveryExpired;
        public event EventHandler<MessageDeliveryEventArgs> MessageDeliveryExpired
        {
            add
            {
                lock (_eventLock)
                {
                    _messageDeliveryExpired += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _messageDeliveryExpired -= value;
                }
            }
        }


        EventHandler<MessageDeliveryFailedEventArgs> _messageDeliveryFailed;
        public event EventHandler<MessageDeliveryFailedEventArgs> MessageDeliveryFailed
        {
            add
            {
                lock (_eventLock)
                {
                    _messageDeliveryFailed += value;
                }
            }
            remove
            {
                lock (_eventLock)
                {
                    _messageDeliveryFailed -= value;
                }
            }
        }
        volatile bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_started)
                {
                    Stop();
                }                

                if (_subscriptions != null) _subscriptions.Dispose();
                _subscriptions = null;                
                
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        ~ServiceBusRuntime()
        {
            Dispose(false);
        }        
    }
			
	
	public class EndpointEventArgs : EventArgs
	{
		public EndpointEventArgs()
		{
		}
		
		public EndpointEventArgs(Endpoint endpoint)
		{
			Endpoint = endpoint;
		}
		
		public Endpoint Endpoint { get; set; }
	}

    public class MessageDeliveryEventArgs : EventArgs
    {
        public MessageDelivery MessageDelivery
        {
            get;
            set;
        }
    }

    public class MessageDeliveryFailedEventArgs : MessageDeliveryEventArgs
    {
        public bool Permanent
        {
            get;
            set;
        }        
    }

}