using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Globalization;
using System.Messaging;

namespace IServiceOriented.ServiceBus.Delivery
{
    /// <summary>
    /// Uses message queues to deliver messages
    /// </summary>
    public class QueuedDeliveryCore : DeliveryCore
    {
        public QueuedDeliveryCore(IMessageDeliveryQueue deliveryQueue, IMessageDeliveryQueue retryQueue, IMessageDeliveryQueue failureQueue)
        {
            _messageDeliveryQueue = deliveryQueue;
            _retryQueue = retryQueue;
            _failureQueue = failureQueue;
        }

        public QueuedDeliveryCore(IMessageDeliveryQueue deliveryQueue, IMessageDeliveryQueue failureQueue)
        {
            _messageDeliveryQueue = deliveryQueue;
            _failureQueue = failureQueue;
        }


        public class DeliveryWork
        {
            public DeliveryWork(CommittableTransaction transaction, MessageDelivery delivery, Semaphore deliverySemaphore)
            {
                Transaction = transaction;
                Delivery = delivery;
                DeliverySemaphore = deliverySemaphore;
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

            public Semaphore DeliverySemaphore
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
                if(_retryQueue != null) addWorker(retryWorker, "Retry worker {0}");
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
                        for (int i = 0; i < deliveryMax; i++) deliverySemaphore.WaitOne(); // wait for worker threads
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
                        for(int i = 0; i < retryMax; i++) retrySemaphore.WaitOne(); // wait for worker threads
                        _stopWaitHandles[threadIndex].Set();
                        break;
                    }
                }
            }
        }

        void deliveryWork(Semaphore deliverySemaphore, IMessageDeliveryQueue queue)
        {
            if (deliverySemaphore.WaitOne(TimeSpan.FromSeconds(1), true))
            {
                bool release = true;
                try
                {
                    CommittableTransaction ct = new CommittableTransaction();

                    Transaction.Current = ct;
                    MessageDelivery md = queue.Dequeue(TimeSpan.FromSeconds(DEQUEUE_TIMEOUT_SECONDS));

                    if (md != null)
                    {
                        DeliveryWork work = new DeliveryWork(ct, md, deliverySemaphore);

                        release = false;
                        ThreadPool.QueueUserWorkItem((deliverWork) =>
                        {
                            try
                            {
                                DoSafely(() => DeliverOne((DeliveryWork)deliverWork, _retryQueue ?? _messageDeliveryQueue, _failureQueue));
                            }
                            finally
                            {
                                work.DeliverySemaphore.Release();                                    
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
                }
            }
        
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
                                    Requeue(delivery);
                                    return;
                                }
                            }

                            SubscriptionEndpoint endpoint = Runtime.GetSubscription(delivery.SubscriptionEndpointId);
                            if (endpoint != null)
                            {
                                Dispatcher dispatcher = endpoint.Dispatcher;

                                if (dispatcher != null)
                                {
                                    dispatcher.Dispatch(delivery);
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

                        NotifyDelivery(delivery);                                  

                        work.Transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        bool retry = !work.Delivery.RetriesMaxed;
                        if (retry)
                        {
                            QueueRetry(work.Delivery, ex);
                        }
                        else
                        {
                            QueueFail(work.Delivery, ex);
                        }

                        NotifyUnhandledException(ex, false);
                        NotifyFailure(work.Delivery, !retry);
                        
                        work.Transaction.Commit();
                    }
                }
            }
            finally
            {
                work.Transaction.Dispose();
                System.Transactions.Transaction.Current = null;
            }
        }

        public override void Deliver(MessageDelivery delivery)
        {
            _messageDeliveryQueue.Enqueue(delivery);
        }

        protected void Requeue(MessageDelivery delivery)
        {
            MessageDelivery retryDelivery = delivery.CreateRetry(false, DateTime.Now.AddMilliseconds((_exponentialBackOff ? (_retryDelayMS * delivery.RetryCount * delivery.RetryCount) : _retryDelayMS)));
            (_retryQueue ?? _messageDeliveryQueue).Enqueue(retryDelivery);
        }

        protected void QueueRetry(MessageDelivery delivery, Exception exception)
        {
            MessageDelivery retryDelivery = delivery.CreateRetry(false, DateTime.Now.AddMilliseconds((_exponentialBackOff ? (_retryDelayMS * delivery.RetryCount * delivery.RetryCount) : _retryDelayMS)), exception);
            (_retryQueue ?? _messageDeliveryQueue).Enqueue(retryDelivery);
        }

        protected void QueueFail(MessageDelivery delivery, Exception exception)
        {
            MessageDelivery retryDelivery = delivery.CreateRetry(false, DateTime.Now.AddMilliseconds((_exponentialBackOff ? (_retryDelayMS * delivery.RetryCount * delivery.RetryCount) : _retryDelayMS)), exception);
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
                System.Diagnostics.Trace.TraceError("UNHANDLED EXCEPTION: " + ex);
                NotifyUnhandledException(ex, false);
            }
            return clean;
        }
    }

}
