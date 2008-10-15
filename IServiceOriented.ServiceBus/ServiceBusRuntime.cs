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
        }

        public ServiceBusRuntime(params RuntimeService[] runtimeServices) : this(SimpleServiceLocator.With(runtimeServices))
        {            
        }
		public ServiceBusRuntime() : this((IServiceLocator)null)
		{
            
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
                                               
                _subscriptions.Read(subscriptions =>
                {
                    foreach (SubscriptionEndpoint se in subscriptions)
                    {
                        se.Dispatcher.StartInternal();
                    }
                });                   

                IEnumerable<RuntimeService> runtimeServices = ServiceLocator.GetAllInstances<RuntimeService>();
                foreach (RuntimeService rs in runtimeServices)
                {
                    // start delivery after other services
                    if (!(rs is DeliveryCore))
                    {
                        rs.StartInternal(this);
                    }
                }

                foreach (DeliveryCore core in ServiceLocator.GetAllInstances<DeliveryCore>())
                {
                    core.StartInternal(this);
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
        
		public void AddListener(ListenerEndpoint endpoint)
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

        public void RemoveListener(Guid endpointId)
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
                    RemoveListener(endpoint);

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
		
		public void RemoveListener(ListenerEndpoint endpoint)
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

        List<MessageDelivery> determineDeliveries(PublishRequest publishRequest)
        {
            List<MessageDelivery> messageDeliveries = new List<MessageDelivery>();
            
            SubscriptionEndpoint[] subscriptions = null;
            _subscriptions.Read(endpoints =>
            {
                subscriptions = endpoints.ToArray();
            });
            
            List<SubscriptionEndpoint> unhandledFilters = new List<SubscriptionEndpoint>();

            bool handled = false;
            foreach (SubscriptionEndpoint subscription in subscriptions)
            {
                bool include;

                if (subscription.Filter is UnhandledMessageFilter)
                {
                    include = false;
                    if (subscription.Filter.Include(publishRequest))
                    {
                        unhandledFilters.Add(subscription);
                    }
                }
                else
                {
                    include = subscription.Filter == null || subscription.Filter.Include(publishRequest);
                }

                if (include)
                {
                    MessageDelivery delivery = new MessageDelivery(subscription.Id, publishRequest.ContractType, publishRequest.Action, publishRequest.Message, MaxRetries, publishRequest.Context);
                    messageDeliveries.Add(delivery);
                    handled = true;
                }
            }

            // if unhandled, send to subscribers of unhandled messages
            if (!handled)
            {
                foreach (SubscriptionEndpoint subscription in unhandledFilters)
                {
                    MessageDelivery delivery = new MessageDelivery(subscription.Id, publishRequest.ContractType, publishRequest.Action, publishRequest.Message, MaxRetries, publishRequest.Context);
                    messageDeliveries.Add(delivery);
                }
            }

            return messageDeliveries;
        }

        public void PublishOneWay(PublishRequest publishRequest)
        {
            using (TransactionScope ts = new TransactionScope())
            {                
                foreach (MessageDelivery md in determineDeliveries(publishRequest))
                {
                    DeliveryCore deliveryCore = SelectDeliveryCore(md);
                    deliveryCore.Deliver(md);
                }
                ts.Complete();
            }
        }

        public virtual DeliveryCore SelectDeliveryCore(MessageDelivery delivery)
        {
            DeliveryCore deliveryCore = ServiceLocator.GetInstance<DeliveryCore>();
            return deliveryCore;
        }
        
		public MessageDelivery[] PublishTwoWay(PublishRequest publishRequest, TimeSpan timeout)
		{                     
            MessageDelivery[] results = null;

            CorrelationMessageFilter filter;
            ActionDispatcher dispatcher;
            SubscriptionEndpoint temporarySubscription = null;

            CountdownLatch latch = null;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    var messageDeliveries = determineDeliveries(publishRequest);
                    
                    latch = new CountdownLatch(messageDeliveries.Count);
                    filter = new CorrelationMessageFilter(messageDeliveries.Select(md => md.MessageDeliveryId).ToArray());
                    results = new MessageDelivery[messageDeliveries.Count];
                    dispatcher = new ActionDispatcher((se, md) =>
                    {
                        for (int j = 0; j < messageDeliveries.Count; j++)
                        {
                            if (messageDeliveries[j].MessageDeliveryId == (string)md.Context[MessageDelivery.CorrelationId]) // is reply
                            {
                                results[j] = md;
                                latch.Tick();
                            }
                        }
                    });

                    temporarySubscription = new SubscriptionEndpoint(Guid.NewGuid(), "Temporary subscription", null, null, typeof(void), dispatcher, filter, true);
                    Subscribe(temporarySubscription);
            
                    Thread.MemoryBarrier(); // make sure variable assignment doesn't move after enqueue

                    foreach (MessageDelivery md in messageDeliveries)
                    {
                        DeliveryCore deliveryCore = SelectDeliveryCore(md);
                        deliveryCore.Deliver(md);                        
                    }

                    ts.Complete();
                }

                latch.Handle.WaitOne(timeout, true); // wait for responses
                
            }
            finally
            {
                if (temporarySubscription != null)
                {
                    Unsubscribe(temporarySubscription);
                }
                if (latch != null)
                {
                    latch.Dispose();
                }
            }

            List<MessageDelivery> trimmedResults = new List<MessageDelivery>();
            foreach (MessageDelivery delivery in results)
            {
                if (delivery != null)
                {
                    trimmedResults.Add(delivery);
                }
            }
            return trimmedResults.ToArray();
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
            return ListSubscribers(true);
        }
        public Collection<SubscriptionEndpoint> ListSubscribers(bool includeTransient)
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