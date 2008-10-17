using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.Serialization;
using IServiceOriented.ServiceBus.Services;
using IServiceOriented.ServiceBus.Listeners;
using IServiceOriented.ServiceBus.Dispatchers;
using System.ServiceModel.Description;

namespace IServiceOriented.ServiceBus.Samples.Chat
{    
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("usage: Chat.exe [server | client]");
                return;
            }

            if (args[0] == "server")
            {
                Console.WriteLine("Starting server..");

                ChatServer server = new ChatServer();
                server.Start();

                Console.ReadLine();

                server.Stop();
            }
            else if (args[0] == "client")
            {
                Console.WriteLine("Starting client...");

                Console.Write("Enter name: ");

                ChatClient client = new ChatClient(Console.ReadLine());
                client.Start();

                while (true)
                {
                    string line = Console.ReadLine();

                    if (line.StartsWith("@") && line.Length > 1)
                    {
                        string to = line.Substring(1, line.IndexOf(' ')-1);
                        if (to.Length > 0)
                        {
                            client.Send(to, line.Substring(to.Length + 2));
                        }
                    }
                    else if (line == "quit")
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Unrecognized command. Send format:\r\n@To message");
                    }
                }                
                
                client.Stop();
            }
            
        }
    }
}
