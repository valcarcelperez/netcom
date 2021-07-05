namespace System.CommunicationFramework.Servers
{
    using System.CommunicationFramework.Common;
    using System.Net;
    using System.Threading;

    /// <summary>
    /// Base class for implementing TCP/IP servers.
    /// </summary>
    public abstract class CfServer : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CfServer" /> class.
        /// </summary>
        /// <param name="serverIdentifier">Name or identifier for this server.</param>
        /// <param name="iPAddress">The IP address where the server is listening.</param>
        /// <param name="port">The Port where the server is listening.</param>
        public CfServer(string serverIdentifier, IPAddress iPAddress, int port)
        {
            this.ListenerProcessManager = new CancellableMethodManager(this.ListeningProcess, serverIdentifier);
            this.ListenerProcessManager.BeforeStart += this.BeforeStart;
            this.ListenerProcessManager.BeforeStop += this.BeforeStop;
            this.ListenerProcessManager.CancellableMethodMenagerEvent += this.CancellableMethodManagerEvent;
            this.ListenerProcessManager.ProcessStarted += this.ListenerProcessManagerProcessStarted;
            this.ListenerProcessManager.ProcessStopped += this.ListenerProcessManagerProcessStopped;
            this.IPAddress = iPAddress;
            this.Port = port;
            this.IsPortDefined = port != 0;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CfServer" /> class.
        /// </summary>
        ~CfServer()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Event raised when the server starts.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Event raised when the server stops.
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Event raised when an internal error occurred.
        /// </summary>
        public event EventHandler<CancellableMethodManagerEventArgs> GeneralEvent;

        /// <summary>
        /// Gets a value indicating whether the server is listening.
        /// </summary>
        public bool Running
        {
            get
            {
                return this.ListenerProcessManager.Running;
            }
        }

        /// <summary>
        /// Gets the ServerIdentifier.
        /// </summary>
        public string ServerIdentifier
        {
            get
            {
                return this.ListenerProcessManager.ProcessName;
            }
        }

        /// <summary>
        /// Gets or sets the IP address where the server is listening.
        /// </summary>
        public IPAddress IPAddress { get; protected set; }

        /// <summary>
        /// Gets or sets the port where the server is listening.
        /// </summary>
        public int Port { get; protected set; }

        /// <summary>
        /// Gets or sets the ListenerProcessManager that executes the listening process.
        /// </summary>
        protected CancellableMethodManager ListenerProcessManager { get; set; }

        /// <summary>
        /// Gets a value indicating whether the port has been defined,
        /// </summary>
        protected bool IsPortDefined { get; private set; }

        /// <summary>
        /// Dispose this object. Part of implementing the IDisposable interface.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            this.ListenerProcessManager.Start();
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        /// <param name="timeout">The timeout in milliseconds.</param>
        public void Stop(int timeout = 1000)
        {
            this.ListenerProcessManager.Stop(timeout);
        }

        /// <summary>
        /// Part of the dispose mechanism.
        /// </summary>
        /// <param name="disposing">Indicates that the method has been called from the Dispose() method.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Raises the GeneralEvent event.
        /// </summary>
        /// <param name="e">A CancellableMethodManagerEventArgs</param>
        protected virtual void OnGeneralEvent(CancellableMethodManagerEventArgs e)
        {
            EventHandler<CancellableMethodManagerEventArgs> handler = this.GeneralEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Process that listens and accepts new connections.
        /// </summary>
        /// <param name="cancellationToken">A CancellationToken to signal cancelation.</param>
        protected abstract void ListeningProcess(CancellationToken cancellationToken);

        /// <summary>
        /// Handles the BeforeStart from listenerProcessManager.
        /// </summary>
        /// <param name="sender">The object raising the event.</param>
        /// <param name="e">An EventArgs.</param>
        protected virtual void BeforeStart(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Handles the BeforeStop from listenerProcessManager.
        /// </summary>
        /// <param name="sender">The object raising the event.</param>
        /// <param name="e">An EventArgs.</param>
        protected virtual void BeforeStop(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Raises a process started event when assigned.
        /// </summary>
        /// <param name="e">A EventArgs.</param>
        protected virtual void OnProcessStarted(EventArgs e)
        {
            EventHandler handler = this.Started;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises a process stopped event when assigned.
        /// </summary>
        /// <param name="e">A EventArgs.</param>
        protected virtual void OnProcessStopped(EventArgs e)
        {
            EventHandler handler = this.Stopped;
            if (handler != null)
            {
                handler(this, e);
            }
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
        /// Handler of the event Process Started.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">A EventArgs</param>
        private void ListenerProcessManagerProcessStarted(object sender, EventArgs e)
        {
            this.OnProcessStarted(e);
        }
    }
}
