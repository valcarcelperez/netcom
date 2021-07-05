using PricingMulticastCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommunicationFramework.Common;
using System.CommunicationFramework.Interfaces;
using System.CommunicationFramework.Multicast;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PricingSource
{
    public class PriceSender
    {
        private ConcurrentQueue<PricingData> queue;
        private CancellableMethodManager cancellableMethodManager;
        private AutoResetEvent newPriceSignal = new AutoResetEvent(false);
        private MulticastMessageSender<PricingData> multicastSender;

        public PriceSender(IPAddress multicastAddress, IPAddress interfaceAddress, int port, IMessageEncoder<PricingData> encoder, int multicastTimeToLive)
        {
            this.queue = new ConcurrentQueue<PricingData>();
            this.cancellableMethodManager = new CancellableMethodManager(QueueProcessor, "PriceSender");
            this.multicastSender = new MulticastMessageSender<PricingData>(multicastAddress, interfaceAddress, port, encoder, multicastTimeToLive);
        }

        public void Start()
        {
            this.cancellableMethodManager.Start();
        }

        public void Stop()
        {
            this.cancellableMethodManager.Stop();
        }

        public void EnqueuePrice(PricingData priceData)
        {
            this.queue.Enqueue(priceData);
            newPriceSignal.Set();
        }

        private void QueueProcessor(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    newPriceSignal.WaitOne(10);
                    QueueProcessorSender(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString());
                }
            }
        }

        private void QueueProcessorSender(CancellationToken cancellationToken)
        {
            PricingData priceData;
            while (this.queue.TryDequeue(out priceData) && !cancellationToken.IsCancellationRequested)
            {
                this.multicastSender.SendMessage(priceData);
            }
        }
    }
}
