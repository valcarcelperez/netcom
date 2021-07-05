namespace System.CommunicationFramework.Servers
{
    using System.CommunicationFramework.Common;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    /// A TCP Server that executes in a separated thread and raises an event with the socket created for the client connected.
    /// </summary>
    public class TcpServer : CfServer
    {
        /// <summary>
        /// Track whether Dispose has been called. 
        /// </summary>
        private bool disposed = false;

#if !NET40
        /// <summary>
        /// Indicates that the server is being stopped.
        /// </summary>
        private bool stopping = false;
#endif

        /// <summary>
        /// Synchronization object.
        /// </summary>
        private object sync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServer" /> class.
        /// </summary>
        /// <param name="serverIdentifier">Name or identifier for this server.</param>
        /// <param name="iPAddress">The IP address where the server is listening.</param>
        /// <param name="port">The Port where the server is listening.</param>
        public TcpServer(string serverIdentifier, IPAddress iPAddress, int port)
            : base(serverIdentifier, iPAddress, port)
        {
            this.TcpListener = new TcpListener(this.IPAddress, port);
#if !NET40
            this.AcceptConnectionsAsynchronously = false;
#endif
        }

        /// <summary>
        /// Event raised when a client is connected.
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>
        /// Gets or sets the maximum length of the pending connections queue. 
        /// When the value is greater than zero it is passed to the internal <c>TcpListener</c> when the Start method is called.
        /// </summary>
        public int BackLog { get; set; }

        /// <summary>
        /// Gets the <c>TcpListener</c>>.
        /// </summary>
        public TcpListener TcpListener { get; private set; }

#if !NET40
        /// <summary>
        /// Gets or sets a value indicating whether the connections will be accepted asynchronously.
        /// <remarks>Set to false (default) if a large number of connections are expected. Set to true if a small number of connections are expected.</remarks>
        /// </summary>
        public bool AcceptConnectionsAsynchronously { get; set; }
#endif

        /// <summary>
        /// Raises the ClientConnected event.
        /// </summary>
        /// <param name="e">A ClientConnectedEventArgs.</param>
        protected virtual void OnClientConnected(ClientConnectedEventArgs e)
        {
            EventHandler<ClientConnectedEventArgs> handler = this.ClientConnected;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Process that listens and accepts new connections.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken to stop the process.</param>
#if NET40
        protected override void ListeningProcess(CancellationToken cancellationToken)
        {
            this.ListeningProcessSync(cancellationToken);
        }
#else
        protected override void ListeningProcess(CancellationToken cancellationToken)
        {
            if (this.AcceptConnectionsAsynchronously)
            {
                this.ListeningProcessAsync(cancellationToken);
            }
            else
            {
                this.ListeningProcessSync(cancellationToken);
            }
        }
#endif

        /// <summary>
        /// Handles the BeforeStart from listenerProcessManager.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A EventArgs.</param>
        protected override void BeforeStart(object sender, EventArgs e)
        {
#if !NET40
            lock (this.sync)
            {
                this.stopping = false;
            }
#endif

            int port;
            if (this.IsPortDefined)
            {
                port = this.Port;
            }
            else
            {
                port = 0;
            }

            if (this.BackLog == 0)
            {
                this.TcpListener.Start();
            }
            else
            {
                this.TcpListener.Start(this.BackLog);
            }

#if !NET40
            this.ListenerProcessManager.LongRunning = !this.AcceptConnectionsAsynchronously;
#endif
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

#if !NET40
            lock (this.sync)
            {
                this.stopping = true;
            }

            if (this.AcceptConnectionsAsynchronously)
            {
                this.TcpListener.Stop();
            }
#endif
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
        /// Listens for new connections synchronously.
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
                        if (!this.TcpListener.Pending())
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        Socket socket = this.TcpListener.AcceptSocket();
                        this.OnClientConnected(new ClientConnectedEventArgs(socket));
                    }
                    catch (Exception ex)
                    {
                        this.OnGeneralEvent(new CancellableMethodManagerEventArgs("Error in ListeningProcess", ex));
                        Thread.Sleep(500);
                    }
                }
            }
            finally
            {
                this.TcpListener.Stop();
            }
        }

#if !NET40
        /// <summary>
        /// Listens for new connections asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A CancellationToken.</param>
        private async void ListeningProcessAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Socket socket = await this.TcpListener.AcceptSocketAsync();
                        this.OnClientConnected(new ClientConnectedEventArgs(socket));
                    }
                    catch (InvalidOperationException ex)
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
            finally
            {
                this.TcpListener.Stop();
            }
        }
#endif
    }
}
