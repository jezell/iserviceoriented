using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IServiceOriented.ServiceBus;
using IServiceOriented.ServiceBus.Services;

namespace IServiceOriented.ServiceBus.ConsoleUtility
{
    class Program
    {
        const string CREATE_COUNTERS_SWITCH = "-counters";

        static void printUsage(string command)
        {
            if(command == null)
            {
                Console.WriteLine("Usage: sbutility.exe command [arguments]");
                Console.WriteLine("Commands:");
                Console.WriteLine("         -counters [CATEGORY_NAME] [MACHINE_NAME]");
            }
        }
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                printUsage(null);
                return -1;
            }

            try
            {
                if (args[0] == CREATE_COUNTERS_SWITCH)
                {
                    string categoryName = args.Length > 1 ? args[1] : null;
                    string machineName = args.Length > 2 ? args[2] : null;

                    PerformanceMonitorRuntimeService.CreateCounters(categoryName, machineName);
                    return 0;
                }
                
                printUsage(null);
                return -1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
                return -1;
            }
        }
    }
}
