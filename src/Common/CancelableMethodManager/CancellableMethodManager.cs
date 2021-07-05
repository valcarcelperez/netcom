namespace System.CommunicationFramework.Common
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Executes a cancellable method in a separated thread.
    /// </summary>
    public class CancellableMethodManager : IDisposable
    {
        /// <summary>
        /// Track whether Dispose has been called. 
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// CancellationTokenSource use to cancel the CancellableMethod.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource = null;

        /// <summary>
        /// AutoResetEvent that is part of the mechanism for stopping the process.
        /// </summary>
        private AutoResetEvent cancellableMethodDoneEvent = new AutoResetEvent(false);

        /// <summary>
        /// CancellableMethod that is executed.
        /// </summary>
        private CancellableMethod cancellableMethod;

        /// <summary>
        /// Synchronization object.
        /// </summary>
        private object sync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="CancellableMethodManager" /> class.
        /// </summary>
        /// <param name="cancellableMethod">A CancellableMethod.</param>
        /// <param name="processName">A process name.</param>
        public CancellableMethodManager(CancellableMethod cancellableMethod, string processName)
        {
            this.cancellableMethod = cancellableMethod;
            this.ProcessName = processName;
            this.LongRunning = true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CancellableMethodManager" /> class.
        /// </summary>
        ~CancellableMethodManager()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// A general event
        /// </summary>
        public event EventHandler<CancellableMethodManagerEventArgs> CancellableMethodMenagerEvent;

        /// <summary>
        /// Event raised before the CancellableMethod starts. 
        /// Do not update the UI from this event since the UI thread is going to be waiting for the method Start() to return.
        /// </summary>
        public event EventHandler BeforeStart;

        /// <summary>
        /// Event raised before the CancellableMethod stops.
        /// Do not update the UI from this event since the UI thread is going to be waiting for the method Stop() to return.
        /// </summary>
        public event EventHandler BeforeStop;

        /// <summary>
        /// Event raised after the CancellableMethod starts.
        /// </summary>
        public event EventHandler ProcessStarted;

        /// <summary>
        /// Event raised after the CancellableMethod stops.
        /// </summary>
        public event EventHandler ProcessStopped;

        /// <summary>
        /// Gets the name of the cancellable process.
        /// </summary>
        public string ProcessName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the CancellableMethod is running.
        /// </summary>
        public bool Running
        {
            get
            {
                lock (this.sync)
                {
                    return this.cancellationTokenSource != null;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter TaskCreationOptions.LongRunning must be used when creating the Task object that will execute the method.
        /// </summary>
        public bool LongRunning { get; set; }

        /// <summary>
        /// Starts the CancellableMethod in a dedicated thread.
        /// </summary>
        public void Start()
        {
            this.OnBeforeStart(EventArgs.Empty);
            bool started = false;
            string message = string.Empty;
            lock (this.sync)
            {
                if (this.cancellationTokenSource == null)
                {
                    this.cancellationTokenSource = new CancellationTokenSource();
                    CancellationToken token = this.cancellationTokenSource.Token;
                    Action action = () => this.ExecuteCancellableMethod(token);
                    if (this.LongRunning)
                    {
                        Task.Factory.StartNew(action, TaskCreationOptions.LongRunning);
                    }
                    else
                    {
                        Task.Factory.StartNew(action);
                    }

                    started = true;
                }
                else
                {
                    message = string.Format("{0} is already running.", this.ProcessName);
                }
            }

            if (started)
            {
                this.OnProcessStarted(EventArgs.Empty);
            }
            else
            {
                this.OnCancellableProcessEvent(new CancellableMethodManagerEventArgs(message));
            }
        }

        /// <summary>
        /// Stops the CancellableMethod.
        /// </summary>
        /// <param name="timeout">The timeout in milliseconds.</param>
        public void Stop(int timeout = 1000)
        {
            this.OnBeforeStop(EventArgs.Empty);
            bool stopped = false;
            string message = string.Empty;
            lock (this.sync)
            {
                if (this.cancellationTokenSource != null)
                {
                    this.cancellationTokenSource.Cancel();
                    this.cancellationTokenSource.Dispose();
                    this.cancellationTokenSource = null;

                    stopped = this.cancellableMethodDoneEvent.WaitOne(timeout);
                    if (!stopped)
                    {
                        message = string.Format("{0} did not stop on time.", this.ProcessName);                        
                    }
                }
                else
                {
                    message = string.Format("{0} is not running.", this.ProcessName);
                }
            }

            if (stopped)
            {
                this.OnProcessStopped(EventArgs.Empty);
            }
            else
            {
                this.OnCancellableProcessEvent(new CancellableMethodManagerEventArgs(message));
            }
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
        /// Raises a general event when assigned.
        /// </summary>
        /// <param name="e">A CancellableMethodManagerEventArgs.</param>
        protected virtual void OnCancellableProcessEvent(CancellableMethodManagerEventArgs e)
        {
            EventHandler<CancellableMethodManagerEventArgs> handler = this.CancellableMethodMenagerEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises a before start event when assigned.
        /// </summary>
        /// <param name="e">A EventArgs.</param>
        protected virtual void OnBeforeStart(EventArgs e)
        {
            EventHandler handler = this.BeforeStart;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises a before stop event when assigned.
        /// </summary>
        /// <param name="e">A EventArgs.</param>
        protected virtual void OnBeforeStop(EventArgs e)
        {
            EventHandler handler = this.BeforeStop;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises a process started event when assigned.
        /// </summary>
        /// <param name="e">A EventArgs.</param>
        protected virtual void OnProcessStarted(EventArgs e)
        {
            EventHandler handler = this.ProcessStarted;
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
            EventHandler handler = this.ProcessStopped;
            if (handler != null)
            {
                handler(this, e);
            }
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
                    this.StopIfRunning();
                    this.cancellableMethodDoneEvent.Dispose();
                }
                
                this.disposed = true;
            }
        }

        /// <summary>
        /// Executes the CancellableMethod.
        /// </summary>
        /// <param name="token">A CancellationToken.</param>
        private void ExecuteCancellableMethod(CancellationToken token)
        {
            try
            {
                this.cancellableMethod(token);
            }
            catch (Exception ex)
            {
                this.OnCancellableProcessEvent(new CancellableMethodManagerEventArgs("An unhandled exception has occurred in the Cancellable Method.", ex));
            }
            finally
            {
                this.cancellableMethodDoneEvent.Set();
            }
        }

        /// <summary>
        /// Stops the process if it is running.
        /// </summary>
        private void StopIfRunning()
        {
            lock (this.sync)
            {
                if (this.Running)
                {
                    this.Stop();
                }
            }
        }
    }
}
