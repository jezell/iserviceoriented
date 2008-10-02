using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IServiceOriented.ServiceBus.UnitTests
{
    /// <summary>
    /// Summary description for RequestResponseTest
    /// </summary>
    [TestClass]
    public class RequestResponseTest
    {
        public RequestResponseTest()
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

        [TestMethod]
        public void TestRequestResponse()
        {
            ServiceBusRuntime runtime = new ServiceBusRuntime(new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue());
            

            CEcho echo = new CEcho();

            SubscriptionEndpoint replyEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", null, null, typeof(void), new MethodDispatcher(echo, false), new IgnoreReplyFilter());
            runtime.Subscribe(replyEndpoint);
            runtime.Start();
            try
            {
                string message = "echo this";

                MessageDelivery[] output = null;
                runtime.Publish(new PublishRequest(typeof(void), "Echo", message), PublishWait.Timeout, TimeSpan.FromSeconds(10), out output);

                Assert.IsNotNull(output);
                Assert.AreEqual(1, output.Length);
                Assert.AreEqual(message, (string)output[0].Message);
            }
            finally
            {
                runtime.Stop();
            }
        }

        public class IgnoreReplyFilter : MessageFilter
        {
            public override bool Include(PublishRequest request)
            {
                if (request.Context.ContainsKey(MessageDelivery.CorrelationId))
                {
                    return false;
                }
                return true;
            }
        }

        public class CEcho
        {
            public string Echo(string echo)
            {
                return echo;
            }
        }
        
    }
}
