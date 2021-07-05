using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TemperatureClientServerCommon;
using System.Net.Sockets;
using System.Net;

namespace TemperatureGenerator
{
    public class Client
    {
        private System.Timers.Timer timer;
        private TemperatureSenderClientSideProtocol protocol;
        private Random random = new Random();

        public Client(string clientId, IPAddress iPAddress, int port)
        {
            this.timer = new System.Timers.Timer(1000);
            this.timer.Elapsed += timer_Elapsed;
            TemperatureNotifierMessageEncoder encoder = new TemperatureNotifierMessageEncoder();
            this.protocol = new TemperatureSenderClientSideProtocol(encoder, clientId, iPAddress, port, 3000);
        }

        public void Start()
        {
            this.timer.Start();
        }

        async void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.timer.Stop();

            try
            {
                decimal temp = 50 + random.Next(10);
                await this.protocol.SendTemperatureUdpAsync(temp);
                await this.protocol.SendTemperatureTcpAsync(temp);                
            }
            catch (Exception ex)
            {
                string mesage = string.Format(
                    "Exception while sending temperature. Step: {0}\nException:\n{1}", 
                    this.protocol.ProtocolStep, 
                    ex.Message);
                Logger.Log(mesage);
            }
            finally
            {
                this.timer.Start();
            }
        }
    }
}
