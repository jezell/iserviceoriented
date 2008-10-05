using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Globalization;

namespace IServiceOriented.ServiceBus
{
    public abstract class DeliveryCore : RuntimeService
    {
        protected void NotifyUnhandledException(Exception ex, bool isTerminating)
        {
            Runtime.NotifyUnhandledException(ex, isTerminating);
        }

        protected void NotifyDelivery(MessageDelivery delivery)
        {                        
            Runtime.NotifyDelivery(delivery);
        }

        protected void NotifyFailure(MessageDelivery delivery, bool permanent)
        {
            Runtime.NotifyFailure(delivery, permanent);
        }

        public abstract void QueueDelivery(MessageDelivery delivery);
        protected abstract void QueueRetry(MessageDelivery delivery);
        protected abstract void QueueFail(MessageDelivery delivery);       
    }

    public class TripleQueueDeliveryCore : DeliveryCore
    {
        public TripleQueueDeliveryCore(IMessageDeliveryQueue deliveryQueue, IMessageDeliveryQueue retryQueue, IMessageDeliveryQueue failureQueue)
        {
            _messageDeliveryQueue = deliveryQueue;
            _retryQueue = retryQueue;
            _failureQueue = failureQueue;
        }

        public class DeliveryWork
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

        protected override void OnStart()
        {
            base.OnStart();

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                addWorker(deliveryWorker, "Delivery worker {0}");
                addWorker(retryWorker, "Retry worker {0}");
            }

        }

        void addWorker(ParameterizedThreadStart start, string name)
        {
            lock (_workerThreadsLock)
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

        void deliveryWorker(object param)
        {
            int deliveryMax = 5;
            int threadIndex = (int)param;

            using (Semaphore deliverySemaphore = new Semaphore(deliveryMax, deliveryMax))
            {
                while (true)
                {
                    deliveryWork(deliverySemaphore, _messageDeliveryQueue);

                    if (_stopping)
                    {
                        deliverySemaphore.WaitOne(deliveryMax); // wait for worker threads
                        _stopWaitHandles[threadIndex].Set();
                        break;
                    }

                }
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
                    deliveryWork(retrySemaphore, _retryQueue);

                    Thread.Sleep(RETRY_SLEEP_MS);

                    if (_stopping)
                    {
                        retrySemaphore.WaitOne(retryMax); // wait for worker threads
                        _stopWaitHandles[threadIndex].Set();
                        break;
                    }
                }
            }
        }
        
        void deliveryWork(Semaphore deliverySemaphore, IMessageDeliveryQueue queue)
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
                        MessageDelivery md = queue.Dequeue(TimeSpan.FromSeconds(DEQUEUE_TIMEOUT_SECONDS));

                        if (md != null)
                        {                            
                            DeliveryWork work = new DeliveryWork(ct, md);

                            release = false;
                            ThreadPool.QueueUserWorkItem((deliverWork) =>
                            {
                                Thread.BeginCriticalRegion();
                                try
                                {
                                    DoSafely(() => DeliverOne((DeliveryWork)deliverWork, _retryQueue, _failureQueue));
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
        }
        const int FUTURE_SLEEP_MS = 100;        				
        const int RETRY_SLEEP_MS = 100;
        const int DEQUEUE_TIMEOUT_SECONDS = 5;

        List<Thread> _workerThreads = new List<Thread>();
        object _workerThreadsLock = new object();

        List<AutoResetEvent> _stopWaitHandles = new List<AutoResetEvent>();

        volatile bool _stopping = false;

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

        protected override void OnStop()
        {
            base.OnStop();

            _stopping = true;

            try
            {
                lock (_workerThreadsLock)
                {
                    WaitHandle.WaitAll(_stopWaitHandles.ToArray());

                    _workerThreads.Clear();
                    _stopWaitHandles.Clear();
                }
            }
            finally
            {
                _stopping = false;
            }
        }

        protected void DeliverOne(DeliveryWork work, IMessageDeliveryQueue retryQueue, IMessageDeliveryQueue failQueue)
        {
            System.Transactions.Transaction.Current = work.Transaction;
            bool sent = false;
            try
            {
                using (work.Transaction)
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
                                    QueueRetry(delivery);
                                    return;
                                }
                            }

                            SubscriptionEndpoint endpoint = Runtime.GetSubscription(delivery.SubscriptionEndpointId);
                            if (endpoint != null)
                            {
                                Dispatcher dispatcher = endpoint.Dispatcher;

                                if (dispatcher != null)
                                {
                                    dispatcher.DispatchInternal(delivery);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Dispatcher is not set");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine(String.Format("Subscription {0} no longer exists. Skipping delivery.", delivery.SubscriptionEndpointId));
                            }
                        }

                        work.Transaction.Commit();
                        sent = true;
                        NotifyDelivery(delivery);
                    }
                    catch (Exception ex)
                    {
                        bool retry = !work.Delivery.RetriesMaxed;                           
                        if (!sent)
                        {
                            
                            if (retry)
                            {
                                QueueRetry(work.Delivery);
                            }
                            else
                            {
                                QueueFail(work.Delivery);
                            }

                            work.Transaction.Commit();                            
                        }
                        NotifyUnhandledException(ex, false);
                        if (!sent)
                        {
                            NotifyFailure(work.Delivery, !retry);
                        }
                    }
                }
            }
            finally
            {
                System.Transactions.Transaction.Current = null;
            }
        }

        public override void QueueDelivery(MessageDelivery delivery)
        {
            _messageDeliveryQueue.Enqueue(delivery);
        }
  
        protected override void QueueRetry(MessageDelivery delivery)
        {
            MessageDelivery retryDelivery = delivery.CreateRetry(false, DateTime.Now.AddMilliseconds((_exponentialBackOff ? (_retryDelayMS * delivery.RetryCount * delivery.RetryCount) : _retryDelayMS)));
            _retryQueue.Enqueue(retryDelivery);
        }

        protected override void QueueFail(MessageDelivery delivery)
        {
            MessageDelivery retryDelivery = delivery.CreateRetry(false, DateTime.Now.AddMilliseconds((_exponentialBackOff ? (_retryDelayMS * delivery.RetryCount * delivery.RetryCount) : _retryDelayMS)));                    
            _failureQueue.Enqueue(retryDelivery);
        }


        protected void DoSafely(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("UNHANDLED EXCEPTION: " + ex);
                NotifyUnhandledException(ex, false);
            }
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
                NotifyUnhandledException(ex, false);
            }
            return clean;
        }
    }
           
}
