namespace System.CommunicationFramework.Servers
{
    using System.CommunicationFramework.Common;
    using System.CommunicationFramework.Interfaces;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A UDP Server that executes in a separated thread and raises an event per each received datagram.
    /// </summary>
    public class UdpServer : CfServer
    {
        /// <summary>
        /// Track whether Dispose has been called. 
        /// </summary>
        private bool disposed = false;

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
        /// Initializes a new instance of the <see cref="UdpServer" /> class.
        /// </summary>
        /// <param name="serverIdentifier">Name or identifier for this server.</param>
        /// <param name="iPAddress">The IP address where the server is listening.</param>
        /// <param name="port">The Port where the server is listening.</param>
        /// <param name="receiveBufferSize">Size of the buffer used by the socket when receiving datagrams.</param>
        /// <param name="maxDatagramSize">Max size of one datagram.</param>
        public UdpServer(string serverIdentifier, IPAddress iPAddress, int port, int receiveBufferSize, int maxDatagramSize)
            : base(serverIdentifier, iPAddress, port)
        {
            this.receiveBufferSize = receiveBufferSize;
            this.UdpReceivedDatagramInfoProvider = new ReceivedDatagramFactory(maxDatagramSize);
            this.ReceiveAsynchronously = false;
        }

        /// <summary>
        /// Event raised when a datagram is received.
        /// </summary>
        public event EventHandler<DatagramReceivedEventArgs> DatagramReceived;

        /// <summary>
        /// Gets or sets the A factory of objects of type <c>UdpReceivedDatagramInfoProvider</c>.
        /// </summary>
        public IReceivedDatagramFactory UdpReceivedDatagramInfoProvider { get; set; }

        /// <summary>
        /// Gets the Socket.
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <c>packages</c> are received asynchronously.
        /// <remarks>Set to false (default) if a large number of packages are expected. Set to true if a small number of packages are expected.</remarks>
        /// </summary>
        public bool ReceiveAsynchronously { get; set; }

        /// <summary>
        /// Handles the event BeforeStart from listenerProcessManager.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A EventArgs.</param>
        protected override void BeforeStart(object sender, EventArgs e)
        {
            lock (this.sync)
            {
                this.stopping = false;
            }

            int port;
            if (this.IsPortDefined)
            {
                port = this.Port;
            }
            else
            {
                port = 0;
            }

            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.Socket.ReceiveBufferSize = this.receiveBufferSize;
            IPEndPoint endPoint = new IPEndPoint(this.IPAddress, port);
            this.Socket.Bind(endPoint);
            this.ListenerProcessManager.LongRunning = !this.ReceiveAsynchronously;
        }

        /// <summary>
        /// Process that listens for new packages.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken to stop the process.</param>
        protected override void ListeningProcess(CancellationToken cancellationToken)
        {
            if (this.ReceiveAsynchronously)
            {
                this.ListeningProcessAsync(cancellationToken);
            }
            else
            {
                this.ListeningProcessSync(cancellationToken);
            }
        }

        /// <summary>
        /// Called when a datagram is received.
        /// </summary>
        /// <param name="e">A DatagramReceivedEventArgs with the data received.</param>
        protected virtual void OnDatagramReceived(DatagramReceivedEventArgs e)
        {
            EventHandler<DatagramReceivedEventArgs> handler = this.DatagramReceived;
            if (handler != null)
            {
                handler(this, e);
            }        
        }

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

            if (this.ReceiveAsynchronously)
            {
                this.Socket.Close();
            }
        }

        /// <summary>
        /// Receives new <c>datagrams</c> asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A CancellationToken.</param>
        private async void ListeningProcessAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    ReceivedDatagram udpReceivedDatagramInfo = await this.ReceiveFromAsyn();
                    this.OnDatagramReceived(new DatagramReceivedEventArgs(udpReceivedDatagramInfo));
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
        /// Encapsulates an asynchronous call to ReceiveFrom.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private Task<ReceivedDatagram> ReceiveFromAsyn()
        {
            ReceivedDatagram udpReceivedDatagramInfo = this.UdpReceivedDatagramInfoProvider.GetReceivedDatagram();
            EndPoint remoteEndPoint = udpReceivedDatagramInfo.RemoteEndPoint;
            TaskCompletionSource<ReceivedDatagram> tcs = new TaskCompletionSource<ReceivedDatagram>(this.Socket);
            this.Socket.BeginReceiveFrom(
                udpReceivedDatagramInfo.Buffer,
                0,
                udpReceivedDatagramInfo.Buffer.Length,
                SocketFlags.None,
                ref remoteEndPoint,
                iar =>
                {
                    TaskCompletionSource<ReceivedDatagram> t = (TaskCompletionSource<ReceivedDatagram>)iar.AsyncState;
                    Socket s = (Socket)t.Task.AsyncState;
                    try 
                    {
                        udpReceivedDatagramInfo.Size = s.EndReceiveFrom(iar, ref remoteEndPoint);
                        udpReceivedDatagramInfo.RemoteEndPoint = remoteEndPoint;
                        t.TrySetResult(udpReceivedDatagramInfo);
                    }
                    catch (Exception ex)
                    {
                        t.TrySetException(ex);
                    }
                },
                tcs);

            return tcs.Task;
        }

        /// <summary>
        /// Receives new <c>datagrams</c> synchronously.
        /// </summary>
        /// <param name="cancellationToken">A CancellationToken.</param>
        private void ListeningProcessSync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (this.Socket.Available == 0)
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        ReceivedDatagram udpReceivedDatagramInfo = this.UdpReceivedDatagramInfoProvider.GetReceivedDatagram();
                        EndPoint endPoint = udpReceivedDatagramInfo.RemoteEndPoint;
                        udpReceivedDatagramInfo.Size = this.Socket.ReceiveFrom(udpReceivedDatagramInfo.Buffer, ref endPoint);
                        udpReceivedDatagramInfo.RemoteEndPoint = endPoint;
                        this.OnDatagramReceived(new DatagramReceivedEventArgs(udpReceivedDatagramInfo));
                    }
                    catch (Exception ex)
                    {
                        this.OnGeneralEvent(new CancellableMethodManagerEventArgs("Error in ListeningProcess", ex));
                        Thread.Sleep(200);
                    }
                }
            }
            finally
            {
                this.Socket.Close();
            }
        }
    }
}
