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
        [OperationContract(IsOneWay=true)]
        void PublishThis(string message);
    }

    [ServiceBehavior(InstanceContextMode=InstanceContextMode.Single)]
    public class ContractImplementation : IContract
    {
        #region IContract Members

        int callCount = 0;
        volatile int failCount = 0;
        int failInterval = 0;

        public void PublishThis(string message)
        {
            int value = Interlocked.Increment(ref callCount);

            if (value <= failCount || (failInterval != 0 && value % failInterval == 0))
            {
                System.Diagnostics.Debug.WriteLine("Fail: " + value);
                throw new Exception("Fail " + value);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PublishThis: " + message);
                lock (PublishedMessages)
                {
                    PublishedMessages.Enqueue(message);
                }
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


        public Queue<string> PublishedMessages = new Queue<string>();

        #endregion
    }
}
