namespace System.CommunicationFramework.Servers
{
    using System;
    using System.CommunicationFramework.Common;
    using System.CommunicationFramework.Interfaces;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// A UDP Server that executes in a separated thread and raises an event per each received message.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    public class UdpMessageServer<T> : UdpServer
    {
        /// <summary>
        /// A message encoder.
        /// </summary>
        private IMessageEncoder<T> encoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpMessageServer{T}" /> class.
        /// </summary>
        /// <param name="serverIdentifier">Name or identifier for this server.</param>
        /// <param name="iPAddress">The IP address where the server is listening.</param>
        /// <param name="port">The Port where the server is listening.</param>
        /// <param name="receiveBufferSize">Size of the buffer used by the socket when receiving datagrams.</param>
        /// <param name="maxDatagramSize">Max size of one datagram.</param>
        /// <param name="encoder">An message encoder.</param>
        public UdpMessageServer(string serverIdentifier, IPAddress iPAddress, int port, int receiveBufferSize, int maxDatagramSize, IMessageEncoder<T> encoder)
            : base(serverIdentifier, iPAddress, port, receiveBufferSize, maxDatagramSize)
        {
            Throw.ThrowIfNull(encoder, "encoder");
            this.encoder = encoder;
            this.DatagramReceived += this.LocalDatagramReceived;
        }

        /// <summary>
        /// Event raised when a message is received.
        /// </summary>
        public event EventHandler<ReceivedMessageEventArgs<T>> MessageReceived;

        /// <summary>
        /// Event raise when an exception occurs while decoding messages.
        /// </summary>
        public event EventHandler<DecodingErrorEventArgs> DecodingError;

        /// <summary>
        /// Raises the MessageReceived event.
        /// </summary>
        /// <param name="e">A ReceivedMessageEventArgs.</param>
        protected virtual void OnMessageReceived(ReceivedMessageEventArgs<T> e)
        {
            EventHandler<ReceivedMessageEventArgs<T>> handle = this.MessageReceived;
            if (handle != null)
            {
                Task.Run(() => { handle(this, e); }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Raises the OnDecodingError event.
        /// </summary>
        /// <param name="e">A DecodingErrorEventArgs.</param>
        protected virtual void OnDecodingError(DecodingErrorEventArgs e)
        {
            EventHandler<DecodingErrorEventArgs> handle = this.DecodingError;
            if (handle != null)
            {
                handle(this, e);
            }
        }

        /// <summary>
        /// Processes a received datagram.
        /// </summary>
        /// <param name="sender">The object that raised a DatagramReceived event.</param>
        /// <param name="e">A DatagramReceivedEventArgs.</param>
        private void LocalDatagramReceived(object sender, DatagramReceivedEventArgs e)
        {
            UdpReceivedMessageInfo<T> receivedMessageInfo = null;
            try
            {
                T receivedMessage = this.encoder.DecodeMessage(e.ReceivedDatagram.Buffer, 0, e.ReceivedDatagram.Size);
                receivedMessageInfo = new UdpReceivedMessageInfo<T>(receivedMessage, e.ReceivedDatagram.RemoteEndPoint as IPEndPoint);
            }
            catch (Exception ex)
            {
                DecodingErrorEventArgs decodingErrorEventArgs = new DecodingErrorEventArgs(ex);
                this.OnDecodingError(decodingErrorEventArgs);
                if (!decodingErrorEventArgs.Handled)
                {
                    throw;
                }
            }

            if (receivedMessageInfo != null)
            {
                this.OnMessageReceived(new ReceivedMessageEventArgs<T>(receivedMessageInfo));
            }
        }
    }
}
