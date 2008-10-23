using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Dispatchers;
using IServiceOriented.ServiceBus.Delivery.Formatters;
using System.ServiceModel.Description;
using System.ServiceModel;
using System.Transactions;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestRequestResponse
    {
        public TestRequestResponse()
        {
        }

        public void TEst()
        {
            ContractDescription description = ContractDescription.GetContract(typeof(ISendMessageContract));
            Console.WriteLine(description);
        }


        [Test]
        public void MethodDispatcher_Publishes_Response_Messages()
        {
            System.Messaging.IMessageFormatter binaryFormatter = new System.Messaging.BinaryMessageFormatter();
            using (ServiceBusRuntime runtime = Create.BinaryMsmqRuntime())
            {
                CEcho echo = new CEcho();

                SubscriptionEndpoint replyEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", null, null, typeof(void), new MethodDispatcher(echo, false), new PredicateMessageFilter( m => m.Action == "Echo"));
                runtime.Subscribe(replyEndpoint);
                runtime.Start();
                try
                {
                    string message = "echo this";

                    MessageDelivery[] output = runtime.PublishTwoWay(new PublishRequest(typeof(void), "Echo", message), TimeSpan.FromSeconds(100));

                    Assert.IsNotNull(output);
                    Assert.AreEqual(1, output.Length);
                    Assert.AreEqual(message, (string)output[0].Message);
                }
                finally
                {
                    runtime.Stop();
                }
            }
        }

        [Test]
        public void MethodDispatcher_Publishes_Fault_Messages()
        {
            System.Messaging.IMessageFormatter binaryFormatter = new System.Messaging.BinaryMessageFormatter();
            using (ServiceBusRuntime runtime = Create.BinaryMsmqRuntime())
            {
                CEcho echo = new CEcho();

                SubscriptionEndpoint replyEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "test", null, null, typeof(void), new MethodDispatcher(echo, false), new PredicateMessageFilter(m =>
                    {                    
                        bool result = m.Action == "ThrowInvalidOperationException";
                        return result;                
                    }));
                runtime.Subscribe(replyEndpoint);
                runtime.Start();
                try
                {
                    string message = null;

                    MessageDelivery[] output = runtime.PublishTwoWay(new PublishRequest(typeof(void), "ThrowInvalidOperationException", message), TimeSpan.FromSeconds(100));

                    Assert.IsNotNull(output);
                    Assert.AreEqual(1, output.Length);
                    Assert.IsInstanceOfType(typeof(InvalidOperationException), output[0].Message);
                }
                finally
                {
                    runtime.Stop();
                }
            }
        }

        [Test]
        public void WcfDispatcher_Publishes_Fault_Messages()
        {
            //Assert.Ignore("Bug in .NET framework prevents this from working properly");

            // Code in
            /*                     
             
             private void ValidateScopeRequiredAndAutoComplete(OperationDescription operation, bool singleThreaded, string contractName)
             {
                OperationBehaviorAttribute attribute = operation.Behaviors.Find<OperationBehaviorAttribute>();
                if (attribute != null)
                {
                    if (!attribute.TransactionScopeRequired && !attribute.TransactionAutoComplete)
                    {
                        string name = "SFxTransactionAutoEnlistOrAutoComplete2";
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.GetString(name, new object[] { contractName, operation.Name })));
                    }
                    if (!singleThreaded && !attribute.TransactionAutoComplete)
                    {
                        string str2 = "SFxTransactionNonConcurrentOrAutoComplete2";
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.GetString(str2, new object[] { contractName, operation.Name })));
                    }
                }
             }
             
            throws: 
   
                System.InvalidOperationException : The operation 'ThrowFaultException' on contract 'IEcho' is configured with TransactionAutoComplete set to true and with TransactionScopeRequired set to false. TransactionAutoComplete requires that TransactionScopeRequired is set to true.
	            at System.ServiceModel.Dispatcher.TransactionValidationBehavior.ValidateScopeRequiredAndAutoComplete(OperationDescription operation, Boolean singleThreaded, String contractName)
	            at System.ServiceModel.Dispatcher.TransactionValidationBehavior.System.ServiceModel.Description.IServiceBehavior.Validate(ServiceDescription service, ServiceHostBase serviceHostBase)
	            at System.ServiceModel.Description.DispatcherBuilder.ValidateDescription(ServiceDescription description, ServiceHostBase serviceHost)
	            at System.ServiceModel.Description.DispatcherBuilder.InitializeServiceHost(ServiceDescription description, ServiceHostBase serviceHost)
	            at System.ServiceModel.ServiceHostBase.InitializeRuntime()
	            at System.ServiceModel.ServiceHostBase.OnBeginOpen()
	            at System.ServiceModel.ServiceHostBase.OnOpen(TimeSpan timeout)
	            at System.ServiceModel.Channels.CommunicationObject.Open(TimeSpan timeout)
	            at System.ServiceModel.Channels.CommunicationObject.Open()
            	
             Check that throws this exception appears to be coded incorrectly, since it checks !attribute.TransactionAutoComplete instead of attribute.TransactionAutoComplete before throwing the message, so we
             can't tell WCF not to cancel the transaction.
             
            */


            using (ServiceBusRuntime runtime = Create.MsmqRuntime<IEcho>())
            {
                CEcho echo = new CEcho();

                ServiceHost echoHost = new ServiceHost(typeof(CEcho));
                 
                try
                {
                    echoHost.Open();

                    SubscriptionEndpoint replyEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "EchoClient", "EchoHostClient", "net.pipe://localhost/echo", typeof(IEcho), new WcfProxyDispatcher(), new PredicateMessageFilter( m => m.Action == "ThrowFaultException"));
                    runtime.Subscribe(replyEndpoint);
                    runtime.Start();

                    string message = "fault reason";

                    MessageDelivery[] output = runtime.PublishTwoWay(new PublishRequest(typeof(IEcho), "ThrowFaultException", message), TimeSpan.FromSeconds(10));

                    Assert.IsNotNull(output);
                    Assert.IsInstanceOfType(typeof(FaultException<SendFault>), output[0].Message);
                    Assert.AreEqual(message, ((FaultException<SendFault>)output[0].Message).Reason.ToString());
                    echoHost.Close();
                }
                finally
                {
                    echoHost.Abort();
                }

            }
        }

        [Test]
        public void WcfDispatcher_Publishes_Response_Messages()
        {

            using (ServiceBusRuntime runtime = Create.MsmqRuntime<IEcho>())
            {
                CEcho echo = new CEcho();

                ServiceHost echoHost = new ServiceHost(typeof(CEcho));

                try
                {
                    echoHost.Open();

                    SubscriptionEndpoint replyEndpoint = new SubscriptionEndpoint(Guid.NewGuid(), "EchoClient", "EchoHostClient", "net.pipe://localhost/echo", typeof(IEcho), new WcfProxyDispatcher(), new PredicateMessageFilter( m => m.Action == "Echo"));
                    runtime.Subscribe(replyEndpoint);
                    runtime.Start();

                    string message = "this is a message";

                    MessageDelivery[] output = runtime.PublishTwoWay(new PublishRequest(typeof(IEcho), "Echo", message), TimeSpan.FromSeconds(10));

                    Assert.IsNotNull(output);
                    Assert.IsInstanceOfType(typeof(string), output[0].Message);
                    Assert.AreEqual(message, output[0].Message);
                    echoHost.Close();
                }
                finally
                {
                    echoHost.Abort();
                }

            }
        }
       

        
    }
    [ServiceContract]
    public interface IEcho
    {
        [OperationContract(Action="Echo")]
        string Echo(string echo);

        [FaultContract(typeof(SendFault))]
        [OperationContract(Action="ThrowFaultException")]
        string ThrowFaultException(string echo);
    }

    [ServiceBehavior(ConfigurationName = "EchoHost")]
    public class CEcho : IEcho
    {
        public string Echo(string echo)
        {
            if (echo == null)
            {
                throw new InvalidOperationException();
            }
            return echo;
        }

        public string ThrowInvalidOperationException(string echo)
        {
            throw new InvalidOperationException();
        }

        [TransactionFlow(TransactionFlowOption.NotAllowed)]
        public string ThrowFaultException(string echo)
        {            
            throw new FaultException<SendFault>(new SendFault(), echo);                            
        }

    }
        
}
