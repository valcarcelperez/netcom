using System;
using System.CommunicationFramework.Clients;
using System.CommunicationFramework.Common;
using System.CommunicationFramework.Interfaces;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TemperatureClientServerCommon
{
    public class TemperatureSenderServerSideProtocol : TemperatureSenderProtocolBase
    {
        private TemperatureNotifierMessage receivedMessage;
        private IMessageEncoder<TemperatureNotifierMessage> encoder;
        private IDataFramer framer = new BeginLengthFramer(1024, TemperatureNotifierMessageEncoder.FrameBegin);

        public TemperatureSenderServerSideProtocol(IMessageEncoder<TemperatureNotifierMessage> encoder, int timeout = 1000)
        {
            this.encoder = encoder;
            this.timeout = timeout;
        }

        public async Task<TemperatureNotifierMessage> ReceiveTemperatureTcpAsync(Socket client)
        {
            // set socket properties here.
            // client.ReceiveBufferSize = ...
            // ...

            using (TcpMessageProcessor<TemperatureNotifierMessage> tcpMessageProcessor = new TcpMessageProcessor<TemperatureNotifierMessage>(this.encoder, client, this.framer))
            {
                Func<Task> process = () => ReceiveTcp(tcpMessageProcessor); // process that receives the temperature using TCP.
                Action timeoutCallback = () => tcpMessageProcessor.Client.Dispose(); // action that executes when the process times out. Disposing the socket will cause asynchronous methods to return.
                await ProcessExecutor.ExecuteProcessAsync(process, timeoutCallback, this.timeout);
            }

            return this.receivedMessage;
        }

        private async Task ReceiveTcp(TcpMessageProcessor<TemperatureNotifierMessage> tcpMessageProcessor)
        {
            string clientInfo = tcpMessageProcessor.Client.RemoteEndPoint.ToString();
            //Logger.Log("Receiving messages from : {0}.", clientInfo);
            this.ProtocolStep = TemperatureClientServerCommon.ProtocolStep.ReceivingFirstMessage;
            this.receivedMessage = await tcpMessageProcessor.ReceiveMessageAsync();
            if (this.receivedMessage.MessageType != TemperatureNotifierMessageType.Temperature)
            {
                throw new Exception(
                    string.Format("Unexpected message type received. Expected: {0}. Received: {1}", 
                        TemperatureNotifierMessageType.Temperature, 
                        this.receivedMessage.MessageType));
            }

            TemperatureNotifierMessage responseMessage = this.BuildResponseMessage(this.receivedMessage);
            //Logger.Log("Sending Ack to : {0}.", clientInfo);
            this.ProtocolStep = TemperatureClientServerCommon.ProtocolStep.SendingAckMessage;
            await tcpMessageProcessor.SendMessageAsync(responseMessage);
            //Logger.Log("Completed communication with : {0}.", clientInfo);

            this.ProtocolStep = TemperatureClientServerCommon.ProtocolStep.Completed;
        }

        private TemperatureNotifierMessage BuildResponseMessage(TemperatureNotifierMessage receivedMessage)
        {
            TemperatureNotifierMessage responseMessage = new TemperatureNotifierMessage();
            responseMessage.ClientId = receivedMessage.ClientId;
            responseMessage.MessageId = receivedMessage.MessageId;
            responseMessage.MessageDateTime = DateTime.Now;
            responseMessage.MessageType = TemperatureNotifierMessageType.Ack;
            return responseMessage;
        }
    }
}
