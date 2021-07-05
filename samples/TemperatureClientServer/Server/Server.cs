using System;
using System.Collections.Generic;
using System.CommunicationFramework.Common;
using System.CommunicationFramework.Interfaces;
using System.CommunicationFramework.Servers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TemperatureClientServerCommon;

namespace Server
{
    public class Server
    {
        private IMessageEncoder<TemperatureNotifierMessage> encoder = new TemperatureNotifierMessageEncoder();
        private TcpServer tcpServer;
        private UdpMessageServer<TemperatureNotifierMessage> udpMessageServer;

        public Server(int port)
        {
            this.tcpServer = new TcpServer("Demo Server TCP", IPAddress.Any, port);
            this.tcpServer.Started += ServerStarted;
            this.tcpServer.Stopped += ServerStopped;
            this.tcpServer.ClientConnected += ClientConnected;
            this.tcpServer.GeneralEvent += ServerGeneralEvent;

            this.udpMessageServer = new UdpMessageServer<TemperatureNotifierMessage>("Demo Server UDP", IPAddress.Any, port, 1024, 1024 * 5, this.encoder);
            this.udpMessageServer.Started += ServerStarted;
            this.udpMessageServer.Stopped += ServerStopped;
            this.udpMessageServer.GeneralEvent += ServerGeneralEvent;
            this.udpMessageServer.MessageReceived += UdpMessageReceived;
            this.udpMessageServer.DecodingError += DecodingError;
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

        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Task.Run(() =>
                {
                    ServiceConnectedClient(e.Client);
                }).ConfigureAwait(false);
        }

        private void ServerStopped(object sender, EventArgs e)
        {
            Logger.Log("{0} stopped.", (sender as CfServer).ServerIdentifier);
        }

        private void ServerStarted(object sender, EventArgs e)
        {
            Logger.Log("{0} started.", (sender as CfServer).ServerIdentifier);
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

        private async void ServiceConnectedClient(Socket socket)
        {
            TemperatureSenderServerSideProtocol protocol = new TemperatureSenderServerSideProtocol(this.encoder, 3000);
            try
            {
                string clientInfo = socket.RemoteEndPoint.ToString(); // we need to get the client info here because after the communication is completed the socket is disposed.
                TemperatureNotifierMessage temperatureInfo = await protocol.ReceiveTemperatureTcpAsync(socket);
                Logger.Log("Temperature received. TCP:{0}. ClientId : {1}, Value : {2}", clientInfo, temperatureInfo.ClientId, temperatureInfo.Temperature);
            }
            catch (Exception ex)
            {
                string mesage = string.Format(
                    "Exception while servicing connected client. Step: {0}.\nException:\n{1}",
                    protocol.ProtocolStep,
                    ex.Message);
                Logger.Log(mesage);
            }
        }

        private void UdpMessageReceived(object sender, ReceivedMessageEventArgs<TemperatureNotifierMessage> e)
        {
            TemperatureNotifierMessage message = e.ReceivedMessageInfo.Message;
            Logger.Log("Temperature received. UDP:{0}. ClientId : {1}, Value : {2}", e.ReceivedMessageInfo.RemoteEndPoint, message.ClientId, message.Temperature);
        }

        private void DecodingError(object sender, DecodingErrorEventArgs e)
        {
            string mesage = string.Format(
                "Exception while decoding received a datagram.\nException:\n{0}",
                e.Exception.Message);
            Logger.Log(mesage);
            e.Handled = true;
        }
    }
}
