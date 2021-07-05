namespace System.CommunicationFramework.Clients
{
    using System.CommunicationFramework.Common;
    using System.CommunicationFramework.Interfaces;
    using System.Net.Sockets;

    /// <summary>
    /// Defines a base class for implementing message processors.
    /// </summary>
    /// <typeparam name="T">The type of the message to be process.</typeparam>
    public abstract class MessageProcessor<T> : IDisposable
    {
        /// <summary>
        /// Value used to set the send and receive buffer size.
        /// </summary>
        private const int DefaulBufferSize = 2048;

        /// <summary>
        /// Track whether Dispose has been called. 
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProcessor{T}" /> class.
        /// </summary>
        /// <param name="encoder">An encoder.</param>
        /// <param name="client">A socket.</param>
        public MessageProcessor(IMessageEncoder<T> encoder, Socket client)
        {
            Throw.ThrowIfNull(encoder, "encoder");
            this.SendBuffer = new byte[DefaulBufferSize];
            this.ReceiveBuffer = new byte[DefaulBufferSize];            

            this.Encoder = encoder;
            this.Client = client;
            this.SocketFlags = SocketFlags.None;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MessageProcessor{T}" /> class.
        /// </summary>
        ~MessageProcessor()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the Client.
        /// </summary>
        public Socket Client { get; set; }

        /// <summary>
        /// Gets or sets the Socket Flags used when sending and receiving data.
        /// </summary>
        public SocketFlags SocketFlags { get; set; }

        /// <summary>
        /// Gets or sets the size of the buffer used when sending messages.
        /// </summary>
        public int SendBufferSize
        {
            get
            {
                return this.SendBuffer.Length;
            }

            set
            {
                if (this.SendBuffer.Length != value)
                {
                    this.SendBuffer = new byte[value];
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the buffer used when receiving messages.
        /// </summary>
        public int ReceiveBufferSize
        {
            get
            {
                return this.ReceiveBuffer.Length;
            }

            set
            {
                if (this.ReceiveBuffer.Length != value)
                {
                    this.ReceiveBuffer = new byte[value];
                }
            }
        }

        /// <summary>
        /// Gets or sets the Encoder.
        /// </summary>
        protected IMessageEncoder<T> Encoder { get; set; }

        /// <summary>
        /// Gets or sets the SendBuffer.
        /// </summary>
        protected byte[] SendBuffer { get; set; }

        /// <summary>
        /// Gets or sets the ReceivedBuffer.
        /// </summary>
        protected byte[] ReceiveBuffer { get; set; }

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
                    if (this.Client != null)
                    {
                        this.Client.Dispose();
                    }
                }

                this.disposed = true;
            }
        }
    }
}
