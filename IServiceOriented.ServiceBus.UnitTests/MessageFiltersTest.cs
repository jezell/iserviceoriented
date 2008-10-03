using System;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IServiceOriented.ServiceBus.UnitTests
{
    /// <summary>
    /// Summary description for MessageFiltersTest
    /// </summary>
    [TestClass]
    public class MessageFiltersTest
    {
        public MessageFiltersTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        class C1
        {
        }

        class C1IsMyBase : C1
        {
        }
        [TestMethod]
        public void TypedMessageFilterIncludeWithoutInherit()
        {
            TypedMessageFilter tmf = new TypedMessageFilter(false, typeof(int), typeof(string), typeof(C1));
            Assert.IsTrue(tmf.Include(new PublishRequest(null, null, 1)));
            Assert.IsTrue(tmf.Include(new PublishRequest(null, null, "test")));
            Assert.IsFalse(tmf.Include(new PublishRequest(null, null, 1M)));
            Assert.IsTrue(tmf.Include(new PublishRequest(null, null, new C1())));
            Assert.IsFalse(tmf.Include(new PublishRequest(null, null, new C1IsMyBase())));
        }

        [TestMethod]
        public void TypedMessageFilterIncludeWithInherit()
        {
            TypedMessageFilter tmf = new TypedMessageFilter(true, typeof(int), typeof(string), typeof(C1));
            Assert.IsTrue(tmf.Include(new PublishRequest(null, null, 1)));
            Assert.IsTrue(tmf.Include(new PublishRequest(null, null, "test")));
            Assert.IsFalse(tmf.Include(new PublishRequest(null, null, 1M)));
            Assert.IsTrue(tmf.Include(new PublishRequest(null, null, new C1())));
            Assert.IsTrue(tmf.Include(new PublishRequest(null, null, new C1IsMyBase())));
        }
        
        [TestMethod]
        public void TestUnhandledMessageFilter()
        {
            using (ServiceBusRuntime runtime = new ServiceBusRuntime(new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue()))
            {

                int handledCount = 0;
                int unhandledCount = 0;

                AutoResetEvent reset = new AutoResetEvent(false);

                SubscriptionEndpoint handled = new SubscriptionEndpoint(Guid.NewGuid(), "Handled", null, null, typeof(void), new ActionDispatcher((e, d) => { handledCount++; System.Diagnostics.Trace.WriteLine("Handled Message = " + d.Message); reset.Set(); }), null);
                SubscriptionEndpoint unhandled = new SubscriptionEndpoint(Guid.NewGuid(), "Unhandled", null, null, typeof(void), new ActionDispatcher((e, d) => { unhandledCount++; System.Diagnostics.Trace.WriteLine("Unhandled Message = " + d.Message); reset.Set(); }), new UnhandledMessageFilter(true, typeof(object)));

                runtime.Subscribe(handled);
                runtime.Subscribe(unhandled);

                runtime.Start();

                runtime.Publish(null, null, "Handled");

                // Make sure that unhandled doesn't get the message if it is handled
                if (reset.WaitOne(1000 * 10, true))
                {
                    Assert.AreEqual(1, handledCount);
                    Assert.AreEqual(0, unhandledCount);
                }
                else
                {
                    Assert.Fail("Waited too long");
                }

                runtime.Unsubscribe(handled);

                handledCount = 0;
                unhandledCount = 0;


                runtime.Publish(null, null, "Unhandled");

                // Make sure that unhandled gets the message if it is handled
                if (reset.WaitOne(1000 * 10, true))
                {
                    Assert.AreEqual(0, handledCount);
                    Assert.AreEqual(1, unhandledCount);
                }
                else
                {
                    Assert.Fail("Waited too long");
                }

                runtime.Stop();
            }
        }
    }
}
