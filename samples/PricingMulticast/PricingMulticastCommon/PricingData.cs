using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PricingMulticastCommon
{
    public class PricingData
    {
        public PricingData(string symbol, decimal price)
        {
            this.Symbol = symbol;
            this.Price = price;
        }

        public string Symbol { get; set; }
        public decimal Price { get; set; }

        public override string ToString()
        {
            return string.Format("{0}:{1}", this.Symbol, this.Price.ToString("C"));
        }
    }
}
