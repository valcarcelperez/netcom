namespace System.CommunicationFramework.Multicast
{
    using System;
    using System.CommunicationFramework.Clients;
    using System.CommunicationFramework.Common;
    using System.CommunicationFramework.Interfaces;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Generic multicast sender.
    /// </summary>
    /// <typeparam name="T">The type of the message to be sent.</typeparam>
    public class MulticastMessageSender<T> : IDisposable
    {
        /// <summary>
        /// Value used to set the buffer size.
        /// </summary>
        private const int DefaulBufferSize = 2048;

        /// <summary>
        /// Track whether Dispose has been called. 
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Message encoder used when sending messages.
        /// </summary>
        private IMessageEncoder<T> encoder;

        /// <summary>
        /// The multicast address.
        /// </summary>
        private IPAddress multicastAddress; 

        /// <summary>
        /// The remote end point where the message is sent.
        /// </summary>
        private IPEndPoint remoteEndPoint;
        
        /// <summary>
        /// A <c>UdpClient</c> used to send the multicast package.
        /// </summary>
        private UdpClient client;

        /// <summary>
        /// A buffer used to store the encoded message.
        /// </summary>
        private byte[] buffer = new byte[DefaulBufferSize];

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastMessageSender{T}" /> class.
        /// </summary>
        /// <param name="multicastAddress">A multicast address.</param>
        /// <param name="multicastAddress">Local address.</param>
        /// <param name="port">A port.</param>
        /// <param name="encoder">An encoder.</param>
        /// <param name="multicastTimeToLive">An IP multicast Time to Live.</param>
        public MulticastMessageSender(IPAddress multicastAddress, IPAddress interfaceAddress, int port, IMessageEncoder<T> encoder, int multicastTimeToLive = 1)
        {
            Throw.ThrowIfNull(multicastAddress, "multicastAddress");
            Throw.ThrowIfNull(encoder, "encoder");
            this.encoder = encoder;
            this.multicastAddress = multicastAddress;

            this.client = new UdpClient();
            this.client.JoinMulticastGroup(this.multicastAddress, interfaceAddress);
            this.client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, multicastTimeToLive);
            this.remoteEndPoint = new IPEndPoint(multicastAddress, port);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MulticastMessageSender{T}" /> class.
        /// </summary>
        ~MulticastMessageSender()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the size of the buffer used when sending messages.
        /// </summary>
        public int SendBufferSize
        {
            get
            {
                return this.buffer.Length;
            }

            set
            {
                if (this.buffer.Length != value)
                {
                    this.buffer = new byte[value];
                }
            }
        }

        /// <summary>
        /// Sends a multicast message synchronously.
        /// </summary>
        /// <param name="message">A message.</param>
        public void SendMessage(T message)
        {
            int size = this.encoder.EncodeMessage(message, this.buffer, 0);
            this.client.Send(this.buffer, size, this.remoteEndPoint);
        }

        /// <summary>
        /// Sends a multicast message synchronously.
        /// </summary>
        /// <param name="message">A message.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task SendMessageAsync(T message)
        {
            int size = this.encoder.EncodeMessage(message, this.buffer, 0);
            await this.client.SendAsync(this.buffer, size, this.remoteEndPoint);
        }

        /// <summary>
        /// Dispose this object. Part of implementing the IDisposable interface.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Part of the dispose mechanism.
        /// </summary>
        /// <param name="disposing">Indicates that the method has been called from the Dispose() method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.client.Close();
                }

                this.disposed = true;
            }
        }
    }
}
