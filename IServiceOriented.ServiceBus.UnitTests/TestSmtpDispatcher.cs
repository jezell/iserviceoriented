using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using IServiceOriented.ServiceBus.Delivery;
using IServiceOriented.ServiceBus.Dispatchers;
using System.Net.Mail;
using IServiceOriented.ServiceBus.Delivery.Formatters;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [TestFixture]
    public class TestSmtpDispatcher
    {
        // Todo: Find IMAP or Pop3 components for recieve test
        [Test]
        public void SmtpDispatcher_Can_Send_Messages()
        {
            if (Config.FromMailAddress != null && Config.ToMailAddress != null)
            {
                ServiceBusRuntime dispatchRuntime = new ServiceBusRuntime(new DirectDeliveryCore());
                var subscription = new SubscriptionEndpoint(Guid.NewGuid(), "Smtp Dispatcher", null, null, typeof(IContract), new SmtpDispatcher("this is a test", new MailAddress(Config.FromMailAddress), new MailAddress[] { new MailAddress(Config.ToMailAddress) }), new PassThroughMessageFilter());

                ServiceBusTest tester = new ServiceBusTest(dispatchRuntime);
                tester.StartAndStop(() =>
                {
                    dispatchRuntime.Subscribe(subscription);
                    dispatchRuntime.PublishOneWay(typeof(IContract), "PublishThis", "this is a test message");
                });
            }
            else
            {
                NUnit.Framework.Assert.Ignore("From and to email addresses must be configured to run smtp tests");
            }

        }
    }
}
