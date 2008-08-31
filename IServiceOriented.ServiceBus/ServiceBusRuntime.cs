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

namespace IServiceOriented.ServiceBus
{
	public class ServiceBusRuntime : IDisposable
	{
		public ServiceBusRuntime(IMessageDeliveryQueue deliveryQueue, IMessageDeliveryQueue retryQueue, IMessageDeliveryQueue failureQueue)
		{
            if (deliveryQueue == null) throw new ArgumentNullException("deliveryQueue");
            if (retryQueue == null) throw new ArgumentNullException("retryQueue");
            if (failureQueue == null) throw new ArgumentNullException("failureQueue");
            
			_messageDeliveryQueue = deliveryQueue;
			_retryQueue = retryQueue;			
            _failureQueue = failureQueue;
		}
		
		object _startLock = new object();
		
		List<Thread> _workerThreads = new List<Thread>();
        object _workerThreadsLock = new object();
		
		public void RegisterService(RuntimeService service)
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (service == null) throw new ArgumentNullException("service");

			lock(_startLock)
			{
                if (_started)
                {
                    throw new InvalidOperationException("Services cannot be registered or unregistered while the bus is running.");
                }
                _runtimeServices.Write(runtimeServices =>
                {
                    runtimeServices.Add(service);                    
                });
			}
		}
		
		public void UnregisterService(RuntimeService service)
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (service == null) throw new ArgumentNullException("service");

			lock(_startLock)
			{
                if (_started)
                {
                    throw new InvalidOperationException("Services cannot be registered or unregistered while the bus is running.");
                }

                _runtimeServices.Write(runtimeServices =>
                {
                    runtimeServices.Remove(service);
				});   
            }
		}

        public RuntimeService GetRuntimeService(Type type)
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (type == null) throw new ArgumentNullException("type");
            
            RuntimeService retVal = null;
            _runtimeServices.Read(runtimeServices =>
            {
                foreach (RuntimeService service in runtimeServices)
                {
                    if (type.IsAssignableFrom(service.GetType()))
                    {
                        retVal = (RuntimeService)service;
                        break;
                    }
                }
            });
            return retVal;
        }

        public IEnumerable<RuntimeService> GetRuntimeServices(Type type)
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (type == null) throw new ArgumentNullException("type");

            List<RuntimeService> matching = new List<RuntimeService>();
            _runtimeServices.Read(runtimeServices =>
            {
                foreach (RuntimeService service in runtimeServices)
                {
                    if (type.IsAssignableFrom(service.GetType()))
                    {
                        matching.Add(service);
                    }
                }

            });
            return matching;
        }
		
        ReaderWriterLockedObject<ReadOnlyCollection<RuntimeService>, IList<RuntimeService>> _runtimeServices = new ReaderWriterLockedObject<ReadOnlyCollection<RuntimeService>, IList<RuntimeService>>(new List<RuntimeService>(), l => new ReadOnlyCollection<RuntimeService>(l));

        
        
		public void Start()
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            lock (_startLock)
            {
                if (_started)
                {
                    throw new InvalidOperationException("The service bus is already started.");
                }                
                                
                _runtimeServices.Read(runtimeServices =>
                {

                    int i = 0;
                    for (; i < runtimeServices.Count; i++)
                    {
                        runtimeServices[i].StartInternal(this);
                    }
                                        
                });

                _subscriptions.Read(subscriptions =>
                {
                    foreach (SubscriptionEndpoint se in subscriptions)
                    {
                        se.Dispatcher.StartInternal();
                    }
                });                   

                lock (_listenerEndpointsLock)
                {
                    foreach(ListenerEndpoint le in _listenerEndpoints)
                    {
                        le.Listener.StartInternal();
                    }
                }
                
                addWorker(deliveryWorker, "Delivery worker {0}");
                addWorker(retryWorker, "Retry worker {0}");

                EventHandler started = Started;
                if (started != null)
                {
                    started(this, EventArgs.Empty);
                }
                _started = true;                 
            }
		}
		
		void addWorker(ParameterizedThreadStart start, string name)
		{
			lock(_workerThreadsLock)
			{
				Thread thread = new Thread(start);			
				thread.IsBackground = true;
                int threadIndex = _workerThreads.Count;
                if (name != null)
                {
                    thread.Name = String.Format(CultureInfo.CurrentCulture, name, threadIndex);
                }
				_workerThreads.Add(thread);
				_stopWaitHandles.Add(new AutoResetEvent(false));
                thread.Start(threadIndex);
			}
		}
        const int DEQUEUE_TIMEOUT_SECONDS = 5;

		void deliveryWorker(object param)
		{
			int threadIndex = (int)param;

            while(true)
			{                
                DoSafely(() =>
                {
                    CommittableTransaction ct = new CommittableTransaction();
                    Transaction.Current = ct;
                    try
                    {
                        Transaction.Current = ct;
                        MessageDelivery md = _messageDeliveryQueue.Dequeue(TimeSpan.FromSeconds(DEQUEUE_TIMEOUT_SECONDS));

                        if (md != null)
                        {
                            DeliveryWork work = new DeliveryWork(ct, md);

                            ThreadPool.QueueUserWorkItem((deliverWork) =>
                            {
                                DeliverOne((DeliveryWork)deliverWork, _retryQueue);
                            }, work);
                        }
                    }
                    finally
                    {
                        Transaction.Current = null;
                    }
                });

                if (_stopping)
                {
                    _stopWaitHandles[threadIndex].Set();
                    break;
                }

			}
		}

        protected class DeliveryWork
        {
            public DeliveryWork(CommittableTransaction transaction, MessageDelivery delivery)
            {
                Transaction = transaction;
                Delivery = delivery;
            }
            public CommittableTransaction Transaction
            {
                get;
                set;
            }
            public MessageDelivery Delivery
            {
                get;
                set;
            }
        }
        
		void retryWorker(object param)
		{            
            int threadIndex = (int)param;
            while(true)
			{
                CommittableTransaction ct = new CommittableTransaction();

                try
                {
                    Transaction.Current = ct;
                    MessageDelivery md = _retryQueue.Dequeue(TimeSpan.FromSeconds(DEQUEUE_TIMEOUT_SECONDS));

                    if (md != null)
                    {
                        DeliveryWork work = new DeliveryWork(ct, md);

                        ThreadPool.QueueUserWorkItem((deliveryWork) =>
                        {
                            DeliverOne((DeliveryWork)deliveryWork, _retryQueue);
                        }, work);
                    }

                    if (_stopping)
                    {
                        _stopWaitHandles[threadIndex].Set();
                        break;
                    }                 
                }
                finally
                {
                    Transaction.Current = null;
                }
                Thread.Sleep(RETRY_SLEEP_MS);
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
				_stopping = true;

                lock (_workerThreadsLock)
                {
                    WaitHandle.WaitAll(_stopWaitHandles.ToArray());

                    _workerThreads.Clear();
                    _stopWaitHandles.Clear();
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

                _runtimeServices.Read(runtimeServices =>
                {
                    foreach (RuntimeService service in runtimeServices)
                    {
                        service.StopInternal();
                    }
                });

			
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
        volatile bool _stopping = false;

        List<AutoResetEvent> _stopWaitHandles = new List<AutoResetEvent>();		
         
        public event EventHandler Started;
        public event EventHandler Stopped;        

        public event EventHandler<EndpointEventArgs> Subscribed;
        public event EventHandler<EndpointEventArgs> Unsubscribed;
        
        public event EventHandler<EndpointEventArgs> ListenerAdded;
        public event EventHandler<EndpointEventArgs> ListenerRemoved;
        
		List<ListenerEndpoint> _listenerEndpoints = new List<ListenerEndpoint>();
        object _listenerEndpointsLock = new object();

		public IEnumerable<Endpoint> ListeningEndpoints
		{
			get
			{
				return _listenerEndpoints.ToArray();
			}
		}

        ReaderWriterLockedObject<IEnumerable<SubscriptionEndpoint>, IList<SubscriptionEndpoint>> _subscriptions = new ReaderWriterLockedObject<IEnumerable<SubscriptionEndpoint>, IList<SubscriptionEndpoint>>(new List<SubscriptionEndpoint>(), l => l);

        public static void VerifyContract(Type contractType)
        {
            if (contractType == null) throw new ArgumentNullException("contractType");

            MethodInfo[] methods = contractType.GetMethods();
            HashSet<string> set = new HashSet<string>();

            foreach (MethodInfo method in methods)
            {
                if (set.Contains(method.Name))
                {
                    throw new InvalidContractException("Method overloads are not allowed. The method "+method.Name+" is overloaded.");
                }
                else
                {
                    set.Add(method.Name);
                }

                if(method.ReturnType != typeof(void))
                {
                    throw new InvalidContractException(method.Name + " must have no return value instead of "+method.ReturnType);
                }

                if (method.GetParameters().Length != 1)
                {
                    throw new InvalidContractException("Methods must have one parameter");
                }
            }
        }

		public void AddListener(ListenerEndpoint endpoint)
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            if (endpoint == null) throw new ArgumentNullException("endpoint");

            if (endpoint.Listener.Runtime != null)
            {
                throw new InvalidOperationException("Listener is attached to a bus already");
            }

            VerifyContract(endpoint.ContractType);

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
                    
                    _runtimeServices.Read(runtimeServices =>
                    {
                        foreach (RuntimeService service in runtimeServices)
                        {
                            service.OnListenerAdded(endpoint);
                        }
                    });                    

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

                    _runtimeServices.Read(runtimeServices =>
                    {
                        foreach (RuntimeService service in runtimeServices)
                        {
                            service.OnListenerRemoved(endpoint);
                        }
                    });                    

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

                    _runtimeServices.Read(runtimeServices =>
                    {
                        foreach (RuntimeService service in runtimeServices)
                        {
                            service.OnListenerRemoved(endpoint);
                        }
                    });

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

        public event UnhandledExceptionEventHandler UnhandledException;
		
		readonly IMessageDeliveryQueue _messageDeliveryQueue;
        public IMessageDeliveryQueue MessageDeliveryQueue
        {
            get
            {
                return _messageDeliveryQueue;
            }
        }

        readonly IMessageDeliveryQueue _retryQueue;
        public IMessageDeliveryQueue RetryQueue
        {
            get
            {
                return _retryQueue;
            }
        }

        readonly IMessageDeliveryQueue _failureQueue;
        public IMessageDeliveryQueue FailureQueue
        {
            get
            {
                return _failureQueue;
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
				
		protected void QueueDelivery(Guid subscriptionEndpointId, string action, object message, ReadOnlyDictionary<string, object> context)
		{
            if (message == null) throw new ArgumentNullException("message");

			MessageDelivery delivery = new MessageDelivery(subscriptionEndpointId, action, message, _maxRetries, context);
			_messageDeliveryQueue.Enqueue(delivery);
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

        bool _exponentialBackOff;
        public bool ExponentialBackOff
        {
            get
            {
                return _exponentialBackOff;
            }
            set
            {
                _exponentialBackOff = value;
            }
        }

        int _retryDelayMS = 1000;
        public int RetryDelay
        {
            get
            {
                return _retryDelayMS;
            }
            set
            {
                _retryDelayMS = value;
            }
        }

		const int RETRY_SLEEP_MS = 100;
        const int FUTURE_SLEEP_MS = 100;

        
		protected void DeliverOne(DeliveryWork work, IMessageDeliveryQueue retryQueue)
		{
            using (System.Transactions.Transaction.Current = work.Transaction)
            {
                try
                {
                    MessageDelivery delivery = work.Delivery;
                    if (delivery != null)
                    {
                        if (delivery.TimeToProcess != null)
                        {
                            int mDelay = (int)(delivery.TimeToProcess.Value - DateTime.Now).TotalMilliseconds;
                            if (mDelay > 0)
                            {
                                System.Diagnostics.Debug.WriteLine("Time to process is " + mDelay + " milliseconds away. Requeuing in " + FUTURE_SLEEP_MS);
                                Thread.Sleep(FUTURE_SLEEP_MS); // Sleep briefly in case we are in a loop of future messages, should be a little smarter
                                retryQueue.Enqueue(delivery);
                                return;
                            }
                        }

                        try
                        {
                            SubscriptionEndpoint endpoint = GetSubscription(delivery.SubscriptionEndpointId);
                            if (endpoint != null)
                            {
                                Dispatcher dispatcher = endpoint.Dispatcher;

                                if (endpoint != null) // subscriber might have been removed since enqueue
                                {


                                    if (dispatcher != null)
                                    {
                                        dispatcher.DispatchInternal(delivery);

                                        _runtimeServices.FastRead(runtimeServices =>
                                        {
                                            ForEachSafely(runtimeServices, service => service.OnMessageDelivered(delivery));
                                        });
                                        InvokeSafely(_messageDelivered, this, new MessageDeliveryEventArgs() { MessageDelivery = delivery });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageDelivery retryDelivery = delivery.CreateRetry(false, DateTime.Now.AddMilliseconds((_exponentialBackOff ? (_retryDelayMS * delivery.RetryCount * delivery.RetryCount) : _retryDelayMS)));
                            if (!delivery.RetriesMaxed)
                            {
                                System.Diagnostics.Trace.TraceError("Sending to retry queue due to unhandled exception: " + ex.Message);
                                notifyUnhandledException(ex);
                                retryQueue.Enqueue(retryDelivery);

                                _runtimeServices.FastRead(runtimeServices =>
                                {
                                    ForEachSafely(runtimeServices, service => service.OnMessageDeliveryFailed(delivery, false));
                                });
                                InvokeSafely(_messageDeliveryFailed, this, new MessageDeliveryFailedEventArgs() { MessageDelivery = delivery, Permanent = false });
                            }
                            else
                            {
                                System.Diagnostics.Trace.TraceError("Sending to failure queue due to unhandled exception: " + ex.Message);
                                notifyUnhandledException(ex);
                                _failureQueue.Enqueue(retryDelivery);

                                _runtimeServices.FastRead(runtimeServices =>
                                {
                                    ForEachSafely(runtimeServices, service => service.OnMessageDeliveryFailed(delivery, true));
                                });
                                InvokeSafely(_messageDeliveryFailed, this, new MessageDeliveryFailedEventArgs() { MessageDelivery = delivery, Permanent = true });
                            }
                        }
                    }
                    work.Transaction.Commit();
                }
                catch
                {
                    work.Transaction.Rollback();
                    throw;
                }
            }
		}
				
        /// <summary>
        /// Execute a block of code, passing any unhandled exceptions to the unhandled exception handler
        /// </summary>
        /// <param name="action"></param>
        protected void DoSafely(Action action)
        {
            try
            {
                action();
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.TraceError("UNHANDLED EXCEPTION: "+ex);
                notifyUnhandledException(ex);                
            }
        }


        private void notifyUnhandledException(Exception ex)
        {
            notifyUnhandledException(ex, false);
        }
        private void notifyUnhandledException(Exception ex, bool isTerminating)
        {       
            // Warning: unhandled exception inside unhandled exception event handlers could cause bad things to happen
            UnhandledExceptionEventHandler ueHandler = UnhandledException;
            if (ueHandler != null) ueHandler(this, new UnhandledExceptionEventArgs(ex, isTerminating));            
        }

        protected bool DoSafely<T>(Action<T> action, T value)
        {
            bool clean = false;
            try
            {
                action(value);
                clean = true;
            }
            catch (Exception ex)
            {
                UnhandledExceptionEventHandler ueHandler = UnhandledException;

                if (ueHandler != null)
                {
                    ueHandler(this, new UnhandledExceptionEventArgs(ex, false));
                }
            }
            return clean;
        }
        protected bool ForEachSafely<T>(IEnumerable<T> collection, Action<T> action)
        {
            bool clean = true;
            foreach (T value in collection)
            {
                clean = clean && DoSafely(action, value);
            }
            return clean;
        }

        protected bool InvokeSafely(Delegate handler, params object[] parameters)
        {
            if(handler == null) return true;
            Delegate[] list = handler.GetInvocationList();
            return ForEachSafely(list, d => d.DynamicInvoke(parameters));
        }

        public void Publish(Type contractType, string action, object message)
        {
            Publish(new PublishRequest(contractType, action, message, new ReadOnlyDictionary<string,object>()));
        }
		public void Publish(PublishRequest publishRequest)
		{
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            VerifyContract(publishRequest.Contract);

            _subscriptions.Read(subscriptions =>
            {
                using (TransactionScope ts = new TransactionScope())
                {
                    List<SubscriptionEndpoint> unhandledFilters = new List<SubscriptionEndpoint>();

                    bool handled = false;
                    foreach (SubscriptionEndpoint subscription in subscriptions)
                    {
                        bool include;
                        if (subscription.Filter != null)
                        {
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
                                include = subscription.Filter.Include(publishRequest);
                            }
                        }
                        else
                        {
                            include = true;
                        }

                        if (include)
                        {
                            QueueDelivery(subscription.Id, publishRequest.Action, publishRequest.Message, publishRequest.Context);
                            handled = true;
                        }
                    }

                    // if unhandled, send to subscribers of unhandled messages
                    if (!handled)
                    {
                        foreach (SubscriptionEndpoint subscription in unhandledFilters)
                        {
                            QueueDelivery(subscription.Id, publishRequest.Action, publishRequest.Message, publishRequest.Context);
                        }
                    }


                    ts.Complete();
                }
            });
		}

        public Collection<ListenerEndpoint> ListListeners()
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            Collection<ListenerEndpoint> endpoints = new Collection<ListenerEndpoint>();

            lock (_listenerEndpointsLock)
            {
                foreach (ListenerEndpoint endpoint in _listenerEndpoints)
                {
                    endpoints.Add(endpoint);
                }            
            }
            return endpoints;
        }

        public Collection<SubscriptionEndpoint> ListSubscribers()
        {
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            Collection<SubscriptionEndpoint> endpoints = new Collection<SubscriptionEndpoint>();

            _subscriptions.Read(subscriptions =>
            {
                foreach (SubscriptionEndpoint endpoint in subscriptions)
                {
                    endpoints.Add(endpoint);
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

            VerifyContract(subscription.ContractType);

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
                        subscription.Dispatcher.StopInternal() ;
                    }

                    _runtimeServices.Read(runtimeServices =>
                    {
                        foreach (RuntimeService service in runtimeServices)
                        {
                            service.OnSubscriptionAdded(subscription);
                        }
                    });

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
                    
                    _runtimeServices.Read(runtimeServices =>
                    {
                        foreach (RuntimeService service in runtimeServices)
                        {
                            service.OnSubscriptionRemoved(subscription);
                        }
                    });

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

        object _messageDeliveredEventLock = new object();
        event EventHandler<MessageDeliveryEventArgs> _messageDelivered;
        public event EventHandler<MessageDeliveryEventArgs> MessageDelivered
        {
            add
            {
                lock (_messageDeliveredEventLock)
                {
                    _messageDelivered += value;
                }
            }
            remove
            {
                lock (_messageDeliveredEventLock)
                {
                    _messageDelivered -= value;
                }
            }
        }

        object _messageDeliveryFailedEventLock = new object();
        event EventHandler<MessageDeliveryFailedEventArgs> _messageDeliveryFailed;
        public event EventHandler<MessageDeliveryFailedEventArgs> MessageDeliveryFailed
        {
            add
            {
                lock (_messageDeliveryFailedEventLock)
                {
                    _messageDeliveryFailed += value;
                }
            }
            remove
            {
                lock (_messageDeliveryFailedEventLock)
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

                if(_runtimeServices != null) _runtimeServices.Dispose();
                _runtimeServices = null;

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