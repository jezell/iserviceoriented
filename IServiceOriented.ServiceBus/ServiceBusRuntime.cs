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

namespace IServiceOriented.ServiceBus
{
	public class ServiceBusRuntime : IDisposable
	{
        public ServiceBusRuntime(IMessageDeliveryQueue deliveryQueue, IMessageDeliveryQueue retryQueue, IMessageDeliveryQueue failureQueue, IServiceLocator serviceLocator)
        {
            if (deliveryQueue == null) throw new ArgumentNullException("deliveryQueue");
            if (retryQueue == null) throw new ArgumentNullException("retryQueue");
            if (failureQueue == null) throw new ArgumentNullException("failureQueue");

            try
            {
                serviceLocator = Microsoft.Practices.ServiceLocation.ServiceLocator.Current;
            }
            catch(NullReferenceException) // 1.0 throws null ref exception if no provider delegate is set.
            {
            }

            if (serviceLocator == null)
            {                
                serviceLocator = new SimpleServiceLocator(); // default to simple service locator
            }

            _messageDeliveryQueue = deliveryQueue;
            _retryQueue = retryQueue;
            _failureQueue = failureQueue;

            _serviceLocator = serviceLocator;
        }
		public ServiceBusRuntime(IMessageDeliveryQueue deliveryQueue, IMessageDeliveryQueue retryQueue, IMessageDeliveryQueue failureQueue) : this(deliveryQueue, retryQueue, failureQueue, null)
		{
            
		}
		
		object _startLock = new object();
		
		List<Thread> _workerThreads = new List<Thread>();
        object _workerThreadsLock = new object();

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

            lock (_startLock)
            {
                if (_started)
                {
                    throw new InvalidOperationException("The service bus is already started.");
                }                
                                
                IEnumerable<RuntimeService> runtimeServices = ServiceLocator.GetAllInstances<RuntimeService>();
                
                foreach(RuntimeService rs in runtimeServices) rs.StartInternal(this);                    

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
                    thread.Name = String.Format(CultureInfo.CurrentCulture, name, Guid.NewGuid());
                }
				_workerThreads.Add(thread);
				_stopWaitHandles.Add(new AutoResetEvent(false));
                thread.Start(threadIndex);
			}
		}
        const int DEQUEUE_TIMEOUT_SECONDS = 5;
        
        

		void deliveryWorker(object param)
		{
            int deliveryMax = 5;
			int threadIndex = (int)param;

            using (Semaphore deliverySemaphore = new Semaphore(deliveryMax, deliveryMax))
            {
                while (true)
                {
                    DoSafely(() =>
                    {
                        if (deliverySemaphore.WaitOne(TimeSpan.FromSeconds(1), true))
                        {
                            bool release = true;
                            Thread.BeginCriticalRegion();
                            try
                            {
                                CommittableTransaction ct = new CommittableTransaction();

                                Transaction.Current = ct;
                                MessageDelivery md = _messageDeliveryQueue.Dequeue(TimeSpan.FromSeconds(DEQUEUE_TIMEOUT_SECONDS));

                                if (md != null)
                                {
                                    DeliveryWork work = new DeliveryWork(ct, md);

                                    release = false;
                                    ThreadPool.QueueUserWorkItem((deliverWork) =>
                                    {
                                        Thread.BeginCriticalRegion();
                                        try
                                        {
                                            DoSafely(() => DeliverOne((DeliveryWork)deliverWork, _retryQueue));
                                        }
                                        finally
                                        {
                                            deliverySemaphore.Release();
                                            Thread.EndCriticalRegion();
                                        }
                                    }, work);
                                }
                                else
                                {
                                    ct.Dispose();
                                }
                            }
                            finally
                            {
                                if (release)
                                {
                                    deliverySemaphore.Release();
                                }
                                Transaction.Current = null;
                                Thread.EndCriticalRegion();
                            }
                        }
                    });

                    if (_stopping)
                    {
                        deliverySemaphore.WaitOne(deliveryMax); // wait for worker threads
                        _stopWaitHandles[threadIndex].Set();
                        break;
                    }

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
            int retryMax = 5;
            using (Semaphore retrySemaphore = new Semaphore(retryMax, retryMax))
            {
                int threadIndex = (int)param;
                while (true)
                {

                    DoSafely(() =>
                    {

                        try
                        {
                            if (retrySemaphore.WaitOne(TimeSpan.FromSeconds(1), true))
                            {
                                bool release = true;
                                Thread.BeginCriticalRegion();
                                try
                                {
                                    CommittableTransaction ct = new CommittableTransaction();
                                    Transaction.Current = ct;

                                    MessageDelivery md = _retryQueue.Dequeue(TimeSpan.FromSeconds(DEQUEUE_TIMEOUT_SECONDS));

                                    if (md != null)
                                    {
                                        DeliveryWork work = new DeliveryWork(ct, md);
                                        release = false;
                                        ThreadPool.QueueUserWorkItem((deliveryWork) =>
                                        {
                                            Thread.BeginCriticalRegion();
                                            try
                                            {
                                                DoSafely(() => DeliverOne((DeliveryWork)deliveryWork, _retryQueue));
                                            }
                                            finally
                                            {
                                                retrySemaphore.Release();
                                                Thread.EndCriticalRegion();
                                            }
                                        }, work);
                                    }
                                    else
                                    {
                                        ct.Dispose();
                                    }
                                }
                                finally
                                {
                                    if (release)
                                    {
                                        retrySemaphore.Release();
                                    }
                                    Thread.EndCriticalRegion();
                                }
                            }

                        }
                        finally
                        {
                            Transaction.Current = null;
                        }
                    });

                    if (_stopping)
                    {
                        retrySemaphore.WaitOne(retryMax); // wait for worker threads
                        _stopWaitHandles[threadIndex].Set();
                        break;
                    }


                    Thread.Sleep(RETRY_SLEEP_MS);
                }
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

                foreach (RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                {
                    service.StopInternal();
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
        volatile bool _stopping = false;

        List<AutoResetEvent> _stopWaitHandles = new List<AutoResetEvent>();		
         
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
				return _listenerEndpoints.ToArray();
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

                                        foreach(RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                                        {
                                            service.OnMessageDelivered(delivery);
                                        }
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

                                foreach(RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                                {
                                    service.OnMessageDeliveryFailed(delivery, false);
                                }
                           
                                InvokeSafely(_messageDeliveryFailed, this, new MessageDeliveryFailedEventArgs() { MessageDelivery = delivery, Permanent = false });
                            }
                            else
                            {
                                System.Diagnostics.Trace.TraceError("Sending to failure queue due to unhandled exception: " + ex.Message);
                                notifyUnhandledException(ex);
                                _failureQueue.Enqueue(retryDelivery);

                                foreach(RuntimeService service in ServiceLocator.GetAllInstances<RuntimeService>())
                                {
                                    service.OnMessageDeliveryFailed(delivery, true);
                                }
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
            MessageDelivery[] results;
            Publish(new PublishRequest(contractType, action, message, new ReadOnlyDictionary<string,object>()), PublishWait.None, TimeSpan.MinValue, out results);
        }

        public void Publish(PublishRequest publishRequest)
        {
            MessageDelivery[] results;
            Publish(publishRequest, PublishWait.None, TimeSpan.MinValue, out results);
        }

		public void Publish(PublishRequest publishRequest, PublishWait wait, TimeSpan timeout, out MessageDelivery[] res)
		{
            bool waitForReply = wait != PublishWait.None;
 
            if (_disposed) throw new ObjectDisposedException("ServiceBusRuntime");

            res = null;

            MessageDelivery[] results = null;

            SubscriptionEndpoint[] subscriptions = null;
            _subscriptions.Read(endpoints =>
            {
                subscriptions = endpoints.ToArray();
            });

            int subscriptionCount = subscriptions.Length;

            List<MessageDelivery> messageDeliveries = new List<MessageDelivery>();
            CorrelationMessageFilter filter;
            ActionDispatcher dispatcher;
            SubscriptionEndpoint temporarySubscription = null;

            CountdownLatch latch = null;
            try
            {
                using (TransactionScope ts = new TransactionScope())
                {
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
                            MessageDelivery delivery = new MessageDelivery(subscription.Id, publishRequest.ContractType, publishRequest.Action, publishRequest.Message, _maxRetries, publishRequest.Context);
                            messageDeliveries.Add(delivery);
                            handled = true;
                        }
                    }

                    // if unhandled, send to subscribers of unhandled messages
                    if (!handled)
                    {
                        foreach (SubscriptionEndpoint subscription in unhandledFilters)
                        {
                            MessageDelivery delivery = new MessageDelivery(subscription.Id, publishRequest.ContractType, publishRequest.Action, publishRequest.Message, _maxRetries, publishRequest.Context);
                            messageDeliveries.Add(delivery);
                        }
                    }

                    if(waitForReply)
                    {
                        latch = new CountdownLatch(messageDeliveries.Count);
                        filter = new CorrelationMessageFilter(messageDeliveries.Select(md => md.MessageId).ToArray());
                        results = new MessageDelivery[messageDeliveries.Count];
                        dispatcher = new ActionDispatcher((se, md) =>
                                {
                                    for (int j = 0; j < messageDeliveries.Count; j++)
                                    {
                                        if (messageDeliveries[j].MessageId == (string)md.Context[MessageDelivery.CorrelationId]) // is reply
                                        {
                                            results[j] = md;
                                            latch.Tick();
                                        }
                                    }
                                });

                        temporarySubscription = new SubscriptionEndpoint(Guid.NewGuid(), "Temporary subscription", null, null, typeof(void), dispatcher, filter, true);                            
                        Subscribe(temporarySubscription);
                    }                        
                                   
                    Thread.MemoryBarrier(); // make sure variable assignment doesn't move after enqueue

                    foreach (MessageDelivery md in messageDeliveries)
                    {
                        _messageDeliveryQueue.Enqueue(md);
                    }
                    
                    ts.Complete();
                }

                if (waitForReply)
                {
                    latch.Handle.WaitOne(timeout, true); // wait for responses
                }
            }
            finally
            {
                if(temporarySubscription != null)
                {
                    Unsubscribe(temporarySubscription);
                }
                if (latch != null)
                {
                    latch.Dispose();
                }
            }
        

            res = results;
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

    public enum PublishWait
    {
        None,
        Timeout,
        FirstWins
    }

}