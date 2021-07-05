using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(1000);
            server.Start();

            Console.WriteLine("Type 'exit' + Enter to exit");
            string typed;
            do
            {
                typed = Console.ReadLine();
            } while (typed != "exit");

            server.Stop();
        }
    }
}
