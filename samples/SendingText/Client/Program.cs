using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            // number of clients to simulate.
            int clientCount = 1;

            int port = 1000;
            IPAddress iPAddress = IPAddress.Loopback;
            Client[] clientCollection = new Client[clientCount];

            int delay = (2000 / clientCount);
            for (int i = 0; i < clientCollection.Length; i++)
            {
                string clientId = string.Format("client_{0}", i);
                Client client = new Client(clientId, iPAddress, port);
                clientCollection[i] = client;
                Thread.Sleep(delay);
                client.Start();
            }

            Console.WriteLine("Type 'exit' + Enter to exit");
            string typed;
            do
            {
                typed = Console.ReadLine();
            } while (typed != "exit");
        }
    }
}
