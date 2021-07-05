using PricingMulticastCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PricingSource
{
    public class PriceNotificationEventArgs : EventArgs
    {
        public PriceNotificationEventArgs(PricingData priceData)
        {
            this.PriceData = priceData;
        }

        public PricingData PriceData { get; private set; }             
    }

    public class PriceGenerator
    {
        private decimal minPrice;
        private decimal maxPrice;
        private decimal deltaPrice;
        private Timer timer;
        private Random random;

        public PriceGenerator(string symbol, decimal minPrice, decimal maxPrice, int interval)
        {
            this.Symbol = symbol;
            this.minPrice = minPrice;
            this.maxPrice = maxPrice;
            this.deltaPrice = maxPrice - minPrice;
           

            this.timer = new Timer(interval);
            this.timer.Elapsed += timer_Elapsed;

            this.random = new Random();
        }

        public event EventHandler<PriceNotificationEventArgs> PriceNotification;

        public string Symbol { get; private set; }

        public void Start()
        {
            this.timer.Start();
        }

        public void Stop()
        {
            this.timer.Stop();
        }

        protected virtual void OnPriceNotification(PriceNotificationEventArgs e)
        {
            EventHandler<PriceNotificationEventArgs> handle = this.PriceNotification;
            if (handle != null)
            {
                handle(this, e);
            }
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            decimal price = (decimal)random.NextDouble() * this.deltaPrice + this.minPrice;
            PricingData priceData = new PricingData(this.Symbol, price);
            OnPriceNotification(new PriceNotificationEventArgs(priceData));
        }
    }
}
