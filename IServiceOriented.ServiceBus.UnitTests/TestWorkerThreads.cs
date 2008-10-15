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
    }
}
