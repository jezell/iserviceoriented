using System;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using Microsoft.Scripting.Hosting;

namespace IServiceOriented.ServiceBus.Scripting.UnitTests
{
    [TestFixture]
    public class ScriptMessageFilterTest
    {        

        [Test]
        public void TestPython()
        {
            ScriptMessageFilter filterTrue = new ScriptMessageFilter("py", 
                @"request.Message == True");

            Assert.IsTrue(filterTrue.Include(new PublishRequest(null, null, true)));

            ScriptMessageFilter filterFalse = new ScriptMessageFilter("py",
                @"request.Message == True");

            Assert.IsFalse(filterFalse.Include(new PublishRequest(null, null, false)));

            ScriptMessageFilter filterTrueC1 = new ScriptMessageFilter("py",
                @"request.Message.IsTrue == True");

            Assert.IsTrue(filterTrueC1.Include(new PublishRequest(null, null, new C1() { IsTrue = true })));

            ScriptMessageFilter filterFalseC1 = new ScriptMessageFilter("py",
                @"request.Message.IsTrue == True");

            Assert.IsFalse(filterFalseC1.Include(new PublishRequest(null, null, new C1() { IsTrue = false })));            
        }


        public class C1
        {
            public bool IsTrue;
        }
    }
}
