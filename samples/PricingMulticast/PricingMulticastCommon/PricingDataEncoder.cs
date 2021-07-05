using System;
using System.Collections.Generic;
using System.CommunicationFramework.Interfaces;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PricingMulticastCommon
{
    public class PricingDataEncoder : IMessageEncoder<PricingData>
    {
        public int EncodeMessage(PricingData message, byte[] buffer, int index)
        {
            XElement xml = new XElement("pricingData", new XAttribute("price", message.Price), new XAttribute("symbol", message.Symbol));
            byte[] data = Encoding.ASCII.GetBytes(xml.ToString());
            Array.Copy(data, 0, buffer, index, data.Length);
            return data.Length;
        }

        public PricingData DecodeMessage(byte[] buffer, int index, int size)
        {
            XElement xml = XElement.Parse(Encoding.ASCII.GetString(buffer, index, size));
            string symbol = xml.Attribute("symbol").Value;
            decimal price = decimal.Parse(xml.Attribute("price").Value);
            return new PricingData(symbol, price);
        }
    }
}
