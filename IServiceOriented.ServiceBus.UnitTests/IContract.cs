using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.ServiceModel;

namespace IServiceOriented.ServiceBus.UnitTests
{
    [ServiceContract]
    public interface IContract
    {
        [OperationContract(IsOneWay=true, Action="PublishThis")]
        void PublishThis(string message);
    }

    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public class ContractImplementation : IContract
    {
        #region IContract Members

        int callCount = 0;
        int publishedCount = 0;
        volatile int failCount = 0;
        int failInterval = 0;


        public void PublishThis(string message)
        {
            int value = Interlocked.Increment(ref callCount);

            if (value <= failCount || (failInterval != 0 && value % failInterval == 0))
            {
                throw new Exception("Fail " + value);
            }
            else
            {
                PublishedMessages[Interlocked.Increment(ref publishedCount) - 1] = message;                
            }
        }

        public void SetFailCount(int value)
        {
            failCount = value;
            callCount = 0;
        }

        public void SetFailInterval(int value)
        {
            failInterval = value;
        }

        public int PublishedCount
        {
            get
            {
                return publishedCount;
            }
        }

        public const int MAX_MESSAGES = 10000;
        public string[] PublishedMessages = new string[MAX_MESSAGES];

        #endregion
    }
}
