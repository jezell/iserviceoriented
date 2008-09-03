using System;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using IServiceOriented.ServiceBus.UnitTests;

namespace IServiceOriented.ServiceBus.Scripting.UnitTests
{
    /// <summary>
    /// Summary description for ScriptTransformationDispatcherTest
    /// </summary>
    [TestClass]
    public class ScriptTransformationDispatcherTest
    {
        public ScriptTransformationDispatcherTest()
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
        public void TestPythonTransformation()
        {
            ScriptTransformationDispatcher dispatcher = new ScriptTransformationDispatcher("py",

@"import clr

clr.AddReference(""IServiceOriented.ServiceBus"")
clr.AddReference(""IServiceOriented.ServiceBus.Scripting.UnitTests"")

from IServiceOriented.ServiceBus import PublishRequest
from IServiceOriented.ServiceBus.Scripting.UnitTests import AfterTransformation

def Execute():
    outgoing = AfterTransformation(int(request.Message.Value));
    return PublishRequest(request.ContractType, request.Action, outgoing)
"


, System.Scripting.SourceCodeKind.Statements);

            dispatcher.Script.Check();

            dispatcher.Script.ExecuteWithVariables(new Dictionary<string,object>() {{ "request", new PublishRequest(null, null, new BeforeTransformation() { Value = "1000" }) }});

            AutoResetEvent reset = new AutoResetEvent(false);

            bool success = false;

            ServiceBusRuntime runtime = new ServiceBusRuntime(new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue());
            runtime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "Tranformation", null, null, typeof(void), dispatcher, new TypedMessageFilter(typeof(BeforeTransformation))));
            runtime.Subscribe(new SubscriptionEndpoint(Guid.NewGuid(), "AfterTransformation", null, null, typeof(void), new ActionDispatcher( (subscription, md) =>
            {
                try
                {
                    success = ((AfterTransformation)md.Message).Value == 1000;
                }
                finally
                {
                    reset.Set();
                }
            }), new TypedMessageFilter(typeof(AfterTransformation))));
            runtime.Start();

            runtime.Publish(new PublishRequest(null, null, new BeforeTransformation() { Value = "1000" }));

            if (!reset.WaitOne(1000 * 10, true))
            {
                Assert.Fail("Waited too long");
            }

            runtime.Stop();

        }        
    }

    public class BeforeTransformation
    {
        public string Value { get; set; }
    }

    public class AfterTransformation
    {
        public AfterTransformation(int value)
        {
            Value = value;
        }
        public int Value { get; set; }
    }
}
