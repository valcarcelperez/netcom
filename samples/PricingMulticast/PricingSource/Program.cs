using PricingMulticastCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PricingSource
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress multicastAddress = IPAddress.Parse("224.0.1.0");
            IPAddress ipAddress = IPAddress.Any; //IPAddress ipAddress = IPAddress.Parse("192.168.1.60"); // use the ip address of the network interface if sending to another computer.
            int multicastTimeToLive = 1; // set a large value to go after the local network segment.

            PricingDataEncoder encoder = new PricingDataEncoder();
            priceSender = new PriceSender(multicastAddress, ipAddress, 30000, encoder, multicastTimeToLive);
            priceSender.Start();

            List<PriceGenerator> proceGeneratorList = new List<PriceGenerator>();
            proceGeneratorList.Add(new PriceGenerator("MSFT", 25, 40, 40));
            proceGeneratorList.Add(new PriceGenerator("IBM", 170, 200, 20));
            proceGeneratorList.Add(new PriceGenerator("DELL", 8, 20, 70));
            proceGeneratorList.Add(new PriceGenerator("HPQ", 15, 25, 50));
            proceGeneratorList.Add(new PriceGenerator("GOOG", 850, 950, 110));

            foreach (var generator in proceGeneratorList)
            {
                generator.PriceNotification += priceGenerator_PriceNotification;
                generator.Start();
            }

            Console.WriteLine("Type 'exit' + Enter to exit");
            string typed;
            do
            {
                typed = Console.ReadLine();
            } while (typed != "exit");

            foreach (var generator in proceGeneratorList)
            {
                generator.Stop();
            }
            priceSender.Stop();
        }

        static PriceSender priceSender;

        static void priceGenerator_PriceNotification(object sender, PriceNotificationEventArgs e)
        {
            priceSender.EnqueuePrice(e.PriceData);
            Logger.Log("Enqueued: {0}", e.PriceData);
        }
    }
}
