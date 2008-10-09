using System;
using System.Threading;
using System.Text;
using System.Collections.Generic;

using NUnit.Framework;

using IServiceOriented.ServiceBus.UnitTests;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Dispatchers;

namespace IServiceOriented.ServiceBus.Scripting.UnitTests
{
    [TestFixture]
    public class ScriptTransformationDispatcherTest
    {        
        [Test]
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
                , Microsoft.Scripting.SourceCodeKind.Statements);

            dispatcher.Script.Check();

            dispatcher.Script.ExecuteWithVariables(new Dictionary<string,object>() {{ "request", new PublishRequest(null, null, new BeforeTransformation() { Value = "1000" }) }});

            AutoResetEvent reset = new AutoResetEvent(false);

            bool success = false;

            ServiceBusRuntime runtime = new ServiceBusRuntime(new QueuedDeliveryCore( new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue(), new NonTransactionalMemoryQueue() ));
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
