using System;
using System.CommunicationFramework.Clients;
using System.CommunicationFramework.Common;
using System.CommunicationFramework.Interfaces;
using System.Net;
using System.Threading.Tasks;

namespace TemperatureClientServerCommon
{
    /// <summary>
    /// Implements the part of the protocol that executes the client when sending temperature updates.
    /// </summary>
    public class TemperatureSenderClientSideProtocol : TemperatureSenderProtocolBase
    {
        private IPEndPoint remoteEndPoint;
        private TemperatureNotifierMessage message;
        private long messageId = DateTime.UtcNow.Ticks;
        private IDataFramer framer = new BeginLengthFramer(1024, TemperatureNotifierMessageEncoder.FrameBegin);
        private IMessageEncoder<TemperatureNotifierMessage> encoder;

        public TemperatureSenderClientSideProtocol(
            IMessageEncoder<TemperatureNotifierMessage> encoder, 
            string clientId,
            IPAddress iPAddress, 
            int port,
            int timeout)
        {
            this.encoder = encoder;
            this.message = new TemperatureNotifierMessage();
            this.message.ClientId = clientId;
            this.message.MessageType = TemperatureNotifierMessageType.Temperature;
            this.remoteEndPoint = new IPEndPoint(iPAddress, port);
            this.timeout = timeout;
        }

        public async Task SendTemperatureTcpAsync(decimal temperature)
        {
            this.PrepareClientMessage(temperature);
            using (TcpMessageProcessor<TemperatureNotifierMessage> tcpMessageProcessor = new TcpMessageProcessor<TemperatureNotifierMessage>(this.encoder, this.framer))
            {
                // set socket properties here.
                // tcpMessageProcessor.Client.ReceiveBufferSize = ...
                // ...

                Func<Task> process = () => SendTcpAsync(tcpMessageProcessor); // process that sends the temperature using TCP.
                Action timeoutCallback = () => tcpMessageProcessor.Client.Dispose(); // action that executes when the process times out. Disposing the socket will cause asynchronous methods to return.
                await ProcessExecutor.ExecuteProcessAsync(process, timeoutCallback, this.timeout);
            }
        }

        public async Task SendTemperatureUdpAsync(decimal temperature)
        {
            this.PrepareClientMessage(temperature);
            using (UdpMessageProcessor<TemperatureNotifierMessage> udpMessageProcessor = new UdpMessageProcessor<TemperatureNotifierMessage>(this.encoder))
            {
                // set socket properties here.
                // udpMessageProcessor.Client.ReceiveBufferSize = ...
                // ...

                await SendUdpAsync(udpMessageProcessor);
            }
        }

        private void PrepareClientMessage(decimal temperature)
        {
            message.Temperature = temperature;
            message.MessageDateTime = DateTime.Now;
            message.MessageId = this.messageId;
            this.messageId++;
        }

        private async Task SendTcpAsync(TcpMessageProcessor<TemperatureNotifierMessage> tcpMessageProcessor)
        {
            //Logger.Log("SendTcp - Client: {0}. Connecting.", this.message.ClientId);
            this.ProtocolStep = TemperatureClientServerCommon.ProtocolStep.Connecting;
            await tcpMessageProcessor.ConnectAsync(this.remoteEndPoint);

            //Logger.Log("SendTcp - Client: {0}. Sending Message.", this.message.ClientId);
            this.ProtocolStep = TemperatureClientServerCommon.ProtocolStep.SendingTemperatureMessage;
            await tcpMessageProcessor.SendMessageAsync(this.message);

            //Logger.Log("SendTcp - Client: {0}. Receiving Ack.", this.message.ClientId);
            this.ProtocolStep = TemperatureClientServerCommon.ProtocolStep.ReceivingAckMessage;
            TemperatureNotifierMessage ackReceived = await tcpMessageProcessor.ReceiveMessageAsync();
            if (ackReceived.MessageType != TemperatureNotifierMessageType.Ack)
            {
                throw new Exception("Unexpected message type.");
            }

            if (ackReceived.MessageId != message.MessageId)
            {
                throw new Exception("Unexpected message Id.");
            }

            if (ackReceived.ClientId != message.ClientId)
            {
                throw new Exception("Unexpected client Id.");
            }

            //Logger.Log("SendTcp - Client: {0}. Disconnecting.", this.message.ClientId);
            this.ProtocolStep = TemperatureClientServerCommon.ProtocolStep.Disconnecting;
            await tcpMessageProcessor.DisconnectAsync();
            //Logger.Log("SendTcp - Client: {0}. Done.", this.message.ClientId);
        }

        private async Task SendUdpAsync(UdpMessageProcessor<TemperatureNotifierMessage> udpMessageProcessor)
        {
            //Logger.Log("SendUdp - Client: {0}. Sending Message.", this.message.ClientId);
            await udpMessageProcessor.SendMessageToAsync(this.message, this.remoteEndPoint);
            //Logger.Log("SendUdp - Client: {0}. Done.", this.message.ClientId);
        }
    }
}
