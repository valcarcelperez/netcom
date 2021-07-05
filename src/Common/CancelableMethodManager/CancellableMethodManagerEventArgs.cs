namespace System.CommunicationFramework.Common
{
    /// <summary>
    /// Event arguments used for events in CancellableMethodManager.
    /// </summary>
    public class CancellableMethodManagerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CancellableMethodManagerEventArgs" /> class.
        /// </summary>
        /// <param name="message">A Message.</param>
        /// <param name="exception">An Exception.</param>
        public CancellableMethodManagerEventArgs(string message, Exception exception = null)
        {
            this.Message = message;
            this.Exception = exception;
        }

        /// <summary>
        /// Gets the description of the event.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the exception that is part of the event information.
        /// </summary>
        public Exception Exception { get; private set; }
    }
}
