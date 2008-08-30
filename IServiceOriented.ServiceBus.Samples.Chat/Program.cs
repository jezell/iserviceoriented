using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IServiceOriented.ServiceBus.Samples.Chat
{
    class Program
    {
        static void Main(string[] args)
        {
            MessageDelivery.RegisterKnownType(typeof(ChatFilter));
            MessageDelivery.RegisterKnownType(typeof(ChatFilter2));
            MessageDelivery.RegisterKnownType(typeof(SendMessageRequest));
            MessageDelivery.RegisterKnownType(typeof(SendMessageRequest2));
            
            if(args.Length == 0)
            {
                Console.WriteLine("usage: Chat.exe [server | client]");
            }

            if (args[0] == "server")
            {
                Console.WriteLine("Starting server..");

                ChatServer server = new ChatServer();
                server.Start();

                Console.ReadLine();

                server.Stop();
            }
            else if (args[0] == "client" || args[0] == "client2")
            {
                Console.WriteLine("Starting client...");

                Console.Write("Enter name: ");

                ChatClient client = new ChatClient(Console.ReadLine(), args[0] == "client2");
                client.Start();

                while (true)
                {
                    string line = Console.ReadLine();

                    if (line.StartsWith("@"))
                    {
                        string to = line.Substring(1, line.IndexOf(' ')-1);
                        client.Send(to, line.Substring(to.Length +2));
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
