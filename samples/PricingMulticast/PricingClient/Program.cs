using PricingMulticastCommon;
using System;
using System.Collections.Generic;
using System.CommunicationFramework.Common;
using System.CommunicationFramework.Multicast;
using System.CommunicationFramework.Servers;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PricingClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress multicastAddress = IPAddress.Parse("224.0.1.0");
            IPAddress ipAddress = IPAddress.Any; //IPAddress ipAddress = IPAddress.Parse("192.168.1.60"); // use the ip address of the network interface if sending to another computer.

            PricingDataEncoder encoder = new PricingDataEncoder();
            MulticastMessageReceiver<PricingData> priceReceiver = new MulticastMessageReceiver<PricingData>("PriceClient", multicastAddress, ipAddress, 30000, 1024, encoder);
            priceReceiver.DecodingError += priceReceiver_DecodingError;
            priceReceiver.GeneralEvent += priceReceiver_GeneralEvent;
            priceReceiver.MessageReceived += priceReceiver_MessageReceived;
            priceReceiver.Started += priceReceiver_Started;
            priceReceiver.Stopped += priceReceiver_Stopped;
            priceReceiver.Start();

            Console.WriteLine("Type 'exit' + Enter to exit");
            string typed;
            do
            {
                typed = Console.ReadLine();
            } while (typed != "exit");

            priceReceiver.Stop();
        }

        static void priceReceiver_Stopped(object sender, EventArgs e)
        {
            Logger.Log("Stopped");
        }

        private static void priceReceiver_Started(object sender, EventArgs e)
        {
            Logger.Log("Started");
        }

        static void priceReceiver_MessageReceived(object sender, ReceivedMessageEventArgs<PricingData> e)
        {
            Logger.Log("Price received: {0}", e.ReceivedMessageInfo.Message);
        }

        static void priceReceiver_GeneralEvent(object sender, CancellableMethodManagerEventArgs e)
        {
            Logger.Log(e.Message);
        }

        static void priceReceiver_DecodingError(object sender, DecodingErrorEventArgs e)
        {
            Logger.Log("Decoding Error: {0}", e.Exception.Message);
        }
    }
}
