using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace IServiceOriented.ServiceBus.UnitTests
{
    public static class Config
    {
        public static string SqlServer
        {
            get
            {
                return ConfigurationSettings.AppSettings["sqlServer"];
            }
        }

        public static string PersistenceDb
        {
            get
            {
                return ConfigurationSettings.AppSettings["persistenceDb"];
            }
        }

        public static string TestQueuePath
        {
            get
            {
                return ConfigurationSettings.AppSettings["testQueuePath"];
            }
        }

        public static string RetryQueuePath
        {
            get
            {
                return ConfigurationSettings.AppSettings["retryQueuePath"];
            }
        }

        public static string FailQueuePath
        {
            get
            {
                return ConfigurationSettings.AppSettings["failQueuePath"];
            }
        }

        public static string ToMailAddress
        {
            get
            {
                return ConfigurationSettings.AppSettings["toMailAddress"];
            }
        }

        public static string FromMailAddress
        {
            get
            {
                return ConfigurationSettings.AppSettings["fromMailAddress"];
            }
        }

        public static string IncomingFilePath
        {
            get
            {
                return ConfigurationSettings.AppSettings["incomingFilePath"];
            }

        }

        public static string ProcessedFilePath
        {
            get
            {
                return ConfigurationSettings.AppSettings["processedFilePath"];
            }
        }
    }
}
