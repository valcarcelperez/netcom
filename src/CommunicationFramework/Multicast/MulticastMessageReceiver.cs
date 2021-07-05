namespace System.CommunicationFramework.Multicast
{
    using System.CommunicationFramework.Common;
    using System.CommunicationFramework.Interfaces;
    using System.CommunicationFramework.Servers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Generic multicast received.
    /// </summary>
    /// <typeparam name="T">The type of the message to be sent.</typeparam>
    public class MulticastMessageReceiver<T> : CfServer
    {
        /// <summary>
        /// Track whether Dispose has been called. 
        /// </summary>
        private bool disposed = false;
        
        /// <summary>
        /// A <c>UdpClient</c> used to receive multicast packages.
        /// </summary>
        private UdpClient client;

        /// <summary>
        /// The remote end point where the server is listening.
        /// </summary>
        private EndPoint localEndPoint;

        /// <summary>
        /// A message encoder use to decode received messages.
        /// </summary>
        private IMessageEncoder<T> encoder;

        /// <summary>
        /// Indicates that the server is being stopped.
        /// </summary>
        private bool stopping = false;

        /// <summary>
        /// Synchronization object.
        /// </summary>
        private object sync = new object();

        /// <summary>
        /// Size of the buffer used by the socket when receiving data.
        /// </summary>
        private int receiveBufferSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastMessageReceiver{T}" /> class.
        /// </summary>
        /// <param name="serverIdentifier">A server identifier.</param>
        /// <param name="multicastAddress">A multicast address.</param>
        /// <param name="iPAddress">The IP address of the network interface where the server is listening.</param>
        /// <param name="port">A port.</param>
        /// <param name="receiveBufferSize">size of the receive buffer of the Socket.</param>
        /// <param name="encoder">An encoder.</param>
        public MulticastMessageReceiver(string serverIdentifier, IPAddress multicastAddress, IPAddress iPAddress, int port, int receiveBufferSize, IMessageEncoder<T> encoder)
            : base(serverIdentifier, iPAddress, port)
        {
            this.MulticastAddress = multicastAddress;
            this.receiveBufferSize = receiveBufferSize;
            this.ListenerProcessManager.LongRunning = false;
            this.encoder = encoder;
            this.localEndPoint = new IPEndPoint(this.IPAddress, this.Port);
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
        /// Gets the multicast address.
        /// </summary>
        public IPAddress MulticastAddress { get; private set; }

        /// <summary>
        /// Part of the dispose mechanism.
        /// </summary>
        /// <param name="disposing">Indicates that the method has been called from the Dispose() method.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.ListenerProcessManager.Dispose();
                }

                this.disposed = true;
            }
        }

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
        /// Handles the event BeforeStart from listenerProcessManager.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A EventArgs.</param>
        protected override void BeforeStart(object sender, EventArgs e)
        {
            this.client = new UdpClient();
            this.client.Client.ReceiveBufferSize = this.receiveBufferSize;
            this.client.ExclusiveAddressUse = false;
            this.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.client.Client.Bind(this.localEndPoint);
            this.client.JoinMulticastGroup(this.MulticastAddress, this.IPAddress);
        }

        /// <summary>
        /// Handles the BeforeStop from listenerProcessManager.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A EventArgs.</param>
        protected override void BeforeStop(object sender, EventArgs e)
        {
            if (!this.Running)
            {
                return;
            }

            lock (this.sync)
            {
                this.stopping = true;
            }

            this.client.Close();
        }

        /// <summary>
        /// Process that listens for new packages.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken to stop the process.</param>
        protected override async void ListeningProcess(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult udpReceiveResult = await this.client.ReceiveAsync();
                    this.ProcessReceivedDatagram(udpReceiveResult);
                }
                catch (ObjectDisposedException ex)
                {
                    lock (this.sync)
                    {
                        if (this.stopping)
                        {
                            return;
                        }
                    }

                    this.OnGeneralEvent(new CancellableMethodManagerEventArgs("Error in ListeningProcess", ex));
                    Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    this.OnGeneralEvent(new CancellableMethodManagerEventArgs("Error in ListeningProcess", ex));
                    Thread.Sleep(200);
                }
            }
        }

        /// <summary>
        /// Process a received datagram.
        /// </summary>
        /// <param name="udpReceiveResult">A <c>UdpReceiveResult</c>.</param>
        private void ProcessReceivedDatagram(UdpReceiveResult udpReceiveResult)
        {
            UdpReceivedMessageInfo<T> receivedMessageInfo = null;
            try
            {
                T receivedMessage = this.encoder.DecodeMessage(udpReceiveResult.Buffer, 0, udpReceiveResult.Buffer.Length);
                receivedMessageInfo = new UdpReceivedMessageInfo<T>(receivedMessage, udpReceiveResult.RemoteEndPoint);
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

        /// <summary>
        /// Handler of the event Process Started.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A EventArgs</param>
        private void ListenerProcessManagerProcessStarted(object sender, EventArgs e)
        {
            this.OnProcessStarted(e);
        }

        /// <summary>
        /// Handler of the event Process Stopped.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A EventArgs</param>
        private void ListenerProcessManagerProcessStopped(object sender, EventArgs e)
        {
            this.OnProcessStopped(e);
        }

        /// <summary>
        /// Handles the CancellableMethodManagerEvent from listenerProcessManager.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A CancellableMethodManagerEventArgs.</param>
        private void CancellableMethodManagerEvent(object sender, CancellableMethodManagerEventArgs e)
        {
            this.OnGeneralEvent(e);
        }
    }
}
