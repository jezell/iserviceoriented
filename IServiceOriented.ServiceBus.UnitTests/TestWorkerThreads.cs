using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using IServiceOriented.ServiceBus.Threading;
using System.Threading;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestWorkerThreads
    {
        [Test]
        public void Can_Start_And_Stop_Workers()
        {
            long count = 0;

            using(WorkerThreads threads = new WorkerThreads(TimeSpan.FromSeconds(5), (ts, obj) => { Interlocked.Increment(ref count); Thread.Sleep(100); }))
            {
                int index = threads.AddWorker();

                // pause a bit and make sure thread is working
                Thread.Sleep(1000);

                Assert.AreEqual(1, threads.Count);

                long curCount = Interlocked.Read(ref count);

                Assert.AreNotEqual(0, curCount); // has value been incremented

                threads.RemoveWorker(index);

                Assert.AreEqual(0, threads.Count);

                // pause a bit and make sure thread is actually dead
                long countAfterStop = Interlocked.Read(ref count);

                Thread.Sleep(1000);

                curCount = Interlocked.Read(ref count);

                Assert.AreEqual(curCount, countAfterStop);
            }

        }

        [Test]
        public void Worker_Threads_Stop_On_Dispose()
        {
            long count = 0;

            WorkerThreads threads = new WorkerThreads(TimeSpan.FromSeconds(5), (ts, obj) => { Interlocked.Increment(ref count); Thread.Sleep(100); });
            try
            {
                int index = threads.AddWorker();

                // pause a bit and make sure thread is working
                Thread.Sleep(1000);

                Assert.AreEqual(1, threads.Count);

                long curCount = Interlocked.Read(ref count);

                Assert.Greater(count, 0);
                
                threads.Dispose();                

                // pause a bit and make sure thread is actually dead
                long countAfterStop = Interlocked.Read(ref count);

                Thread.Sleep(1000);

                curCount = Interlocked.Read(ref count);

                Assert.AreEqual(curCount, countAfterStop);
            }
            finally
            {
                threads.Dispose();
            }
        }

        [Test]
        public void RemoveWorker_Aborts_Thread_After_Timeout()
        {
            long count = 0;

            using (WorkerThreads threads = new WorkerThreads(TimeSpan.FromSeconds(5), (ts, obj) => { Interlocked.Increment(ref count); Thread.Sleep(1000 * 30); Assert.Fail(); }))
            {
                int index = threads.AddWorker();

                // pause a bit and make sure thread is working
                Thread.Sleep(1000);

                Assert.AreEqual(1, threads.Count);

                long curCount = Interlocked.Read(ref count);

                Assert.AreNotEqual(0, curCount); // has value been incremented

                threads.RemoveWorker(index);

                Assert.AreEqual(0, threads.Count);

                // pause a bit and make sure thread is actually dead
                long countAfterStop = Interlocked.Read(ref count);

                Thread.Sleep(1000);

                curCount = Interlocked.Read(ref count);

                Assert.AreEqual(curCount, countAfterStop);
            }

        }


        [Test]
        public void RemoveAll_Removes_All_Threads()
        {
            using (WorkerThreads threads = new WorkerThreads(TimeSpan.FromSeconds(5), (ts, obj) => {  }))
            {
                for (int i = 0; i < 10; i++)
                {
                    threads.AddWorker();
                }

                Assert.AreEqual(10, threads.Count);

                threads.RemoveAll();

                Assert.AreEqual(0, threads.Count);
            }
        }

        [Test]
        public void State_Is_Passed_To_Workers()
        {
            string state = "this is some state";

            using (WorkerThreads threads = new WorkerThreads(TimeSpan.FromSeconds(5), (ts, obj) => { Assert.AreEqual(state, obj); }))
            {
                threads.AddWorker(state);

                Thread.Sleep(1000);
            }
        }

    }
}
