namespace System.CommunicationFramework.Common
{
    using System;

    /// <summary>
    /// Defines the result of executing a process.
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionResult" /> class.
        /// </summary>
        /// <param name="exception">Exception occurred in the process.</param>
        /// <param name="timedOut">Value indicating whether the process timed out.</param>
        public ExecutionResult(Exception exception, bool timedOut)
        {
            this.Exception = exception;
            this.TimedOut = timedOut;
        }

        /// <summary>
        /// Gets the exception occurred in the process.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the process timed out.
        /// </summary>
        public bool TimedOut { get; private set; }
    }
}
