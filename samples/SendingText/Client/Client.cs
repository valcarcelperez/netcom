using SendingText.Common;
using System;
using System.Collections.Generic;
using System.CommunicationFramework.Clients;
using System.CommunicationFramework.Common;
using System.CommunicationFramework.Interfaces;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Client
{
    public class Client
    {
        private IPEndPoint remoteEndPoint;
        private System.Timers.Timer timer;
        private IMessageEncoder<string> encoder = new TextMessageEncoder();
        private int timeout = 3000;

        public Client(string clientId, IPAddress iPAddress, int port)
        {
            this.timer = new System.Timers.Timer(1000);
            this.timer.Elapsed += timer_Elapsed;
            this.remoteEndPoint = new IPEndPoint(iPAddress, port);            
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
                await SendTcpMessage();
                await SendUdpMessage();
            }
            catch (Exception ex)
            {
                string errorMesage = string.Format(
                    "Exception while sending message.\nException:\n{0}",
                    ex.Message);
                Logger.Log(errorMesage);
            }
            finally
            {
                this.timer.Start();
            }
        }

        private async Task SendUdpMessage()
        {
            using (UdpMessageProcessor<string> udpMessageProcessor = new UdpMessageProcessor<string>(this.encoder))
            {
                // set socket properties here.
                // udpMessageProcessor.Client.ReceiveBufferSize = ...
                // ...

                string message = "udp-message";
                Logger.Log("Sending {0}...", message);
                await udpMessageProcessor.SendMessageToAsync(message, this.remoteEndPoint);
            }
        }

        private async Task SendTcpMessage()
        {
            IDataFramer framer = new BeginEndFramer(1024, TextMessageEncoder.StartOfText, TextMessageEncoder.EndOfText);
            using (TcpMessageProcessor<string> tcpMessageProcessor = new TcpMessageProcessor<string>(this.encoder, framer))
            {
                // set socket properties here.
                // tcpMessageProcessor.Client.ReceiveBufferSize = ...
                // ...

                Func<Task> process = () => SendTextProcess(tcpMessageProcessor);
                Action timeoutCallback = () => tcpMessageProcessor.Client.Dispose(); // action that executes when the process times out. Disposing the socket will cause asynchronous methods to return.
                await ProcessExecutor.ExecuteProcessAsync(process, timeoutCallback, this.timeout);
            }
        }

        private async Task SendTextProcess(TcpMessageProcessor<string> tcpMessageProcessor)
        {
            Logger.Log("Connecting to {0}...", this.remoteEndPoint);
            await tcpMessageProcessor.ConnectAsync(this.remoteEndPoint);

            string message = "tcp-message";
            Logger.Log("Sending {0}...", message);
            await tcpMessageProcessor.SendMessageAsync(message);

            string receivedMessage = await tcpMessageProcessor.ReceiveMessageAsync();
            Logger.Log("Received {0}...", receivedMessage);
        }
    }
}
