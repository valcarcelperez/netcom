namespace System.CommunicationFramework.Servers
{
    /// <summary>
    /// Event data for DecodingError event.
    /// </summary>
    public class DecodingErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecodingErrorEventArgs" /> class.
        /// </summary>
        /// <param name="exception">An exception.</param>
        public DecodingErrorEventArgs(Exception exception)
        {
            this.Exception = exception;
        }

        /// <summary>
        /// Gets the Exception.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether a decoding error was handled.
        /// </summary>
        public bool Handled { get; set; }
    }
}
