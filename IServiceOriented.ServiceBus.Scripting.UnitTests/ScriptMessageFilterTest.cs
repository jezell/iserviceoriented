using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IServiceOriented.ServiceBus.Scripting.UnitTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class ScriptMessageFilterTest
    {
        public ScriptMessageFilterTest()
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
