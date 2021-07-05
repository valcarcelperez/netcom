using SendingText.Common;
using System;
using System.CommunicationFramework.Clients;
using System.CommunicationFramework.Common;
using System.CommunicationFramework.Interfaces;
using System.CommunicationFramework.Servers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server
{
    public  class Server
    {
        private IMessageEncoder<string> encoder = new TextMessageEncoder();
        private TcpServer tcpServer;
        private UdpMessageServer<string> udpMessageServer;
        private int timeout = 3000;

        public Server(int port)
        {
            this.tcpServer = new TcpServer("Demo Server TCP", IPAddress.Any, port);
            this.tcpServer.Started += ServerStarted;
            this.tcpServer.Stopped += ServerStopped;
            this.tcpServer.ClientConnected += ClientConnected;
            this.tcpServer.GeneralEvent += ServerGeneralEvent;

            this.udpMessageServer = new UdpMessageServer<string>("Demo Server UDP", IPAddress.Any, port, 1024, 1024 * 5, this.encoder);
            this.udpMessageServer.Started += ServerStarted;
            this.udpMessageServer.Stopped += ServerStopped;
            this.udpMessageServer.GeneralEvent += ServerGeneralEvent;
            this.udpMessageServer.MessageReceived += UdpMessageReceived;
            this.udpMessageServer.DecodingError += DecodingError;
        }

        public void Start()
        {
            this.tcpServer.Start();
            this.udpMessageServer.Start();
        }

        public void Stop()
        {
            // this could be performed in parallel.
            this.tcpServer.Stop();
            this.udpMessageServer.Stop();
        }

        private void ServerStarted(object sender, EventArgs e)
        {
            Logger.Log("{0} started.", (sender as CfServer).ServerIdentifier);
        }

        private void ServerStopped(object sender, EventArgs e)
        {
            Logger.Log("{0} stopped.", (sender as CfServer).ServerIdentifier);
        }

        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Task.Run(() =>
            {
                ServiceConnectedClient(e.Client);
            }).ConfigureAwait(false);
        }

        private void ServerGeneralEvent(object sender, CancellableMethodManagerEventArgs e)
        {
            if (e.Exception != null)
            {
                Logger.Log("{0}. {1}. Exception: {2}.", (sender as CfServer).ServerIdentifier, e.Message, e.Exception);
            }
            else
            {
                Logger.Log("{0}. {1}.", (sender as CfServer).ServerIdentifier, e.Message);
            }
        }

        private void UdpMessageReceived(object sender, ReceivedMessageEventArgs<string> e)
        {
            string message = e.ReceivedMessageInfo.Message;
            Logger.Log("Message received. UDP:{0}. Message : {1}", e.ReceivedMessageInfo.RemoteEndPoint, message);
        }

        private void DecodingError(object sender, DecodingErrorEventArgs e)
        {
            string mesage = string.Format(
                "Exception while decoding received a datagram.\nException:\n{0}",
                e.Exception.Message);
            Logger.Log(mesage);
            e.Handled = true;
        }

        private string ReverseText(string text)
        {
            char[] reversed = text.Reverse().ToArray();
            return new string(reversed);
        }

        private async Task ProcessClientRequest(TcpMessageProcessor<string> tcpMessageProcessor)
        {
            string message = await tcpMessageProcessor.ReceiveMessageAsync();
            Logger.Log("Text received. {0}.", message);
            
            string responseMessage = ReverseText(message);            
            await tcpMessageProcessor.SendMessageAsync(responseMessage);
            Logger.Log("Text sent. {0}.", responseMessage);
        }

        private async void ServiceConnectedClient(Socket socket)
        {
            try
            {
                string clientInfo = socket.RemoteEndPoint.ToString(); // we need to get the client info here because after the communication is completed the socket is disposed.
                Logger.Log("Client connected. {0}.", clientInfo);

                IDataFramer framer = new BeginEndFramer(1024, TextMessageEncoder.StartOfText, TextMessageEncoder.EndOfText);
                using (TcpMessageProcessor<string> tcpMessageProcessor = new TcpMessageProcessor<string>(this.encoder, socket, framer))
                {
                    Func<Task> process = () => ProcessClientRequest(tcpMessageProcessor);
                    Action timeoutCallback = () => tcpMessageProcessor.Client.Dispose(); // action that executes when the process times out. Disposing the socket will cause asynchronous methods to return.
                    await ProcessExecutor.ExecuteProcessAsync(process, timeoutCallback, this.timeout);
                }
            }
            catch (Exception ex)
            {
                string errorMesage = string.Format(
                    "Exception while servicing connected client.\nException:\n{0}",
                    ex.Message);
                Logger.Log(errorMesage);
            }
        }
    }
}
