namespace System.CommunicationFramework.Common
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Class that executes a process and calls a delegate if the process times out.
    /// </summary>
    public class ProcessExecutor
    {
        /// <summary>
        /// Timeout in milliseconds.
        /// </summary>
        private int timeout;

        /// <summary>
        /// Gets or sets the delegate to notify the caller that the process timeout.
        /// </summary>
        private Action processTimedoutCallBack;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExecutor" /> class.
        /// </summary>
        /// <param name="processTimedoutCallBack">A delegate to notify the caller if the process timeout.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        public ProcessExecutor(Action processTimedoutCallBack, int timeout)
        {
            Throw.ThrowIfNull(processTimedoutCallBack, "processTimedoutCallBack");
            this.timeout = timeout;
            this.processTimedoutCallBack = processTimedoutCallBack;
        }

        public static async Task ExecuteProcessAsync(Func<Task> process, Action timeoutCallBack, int timeout)
        {
            ProcessExecutor executor = new ProcessExecutor(timeoutCallBack, timeout);
            await executor.ExecuteAsync(process);
        }

        public static async Task<TResult> ExecuteProcessAsync<TResult>(Func<Task<TResult>> process, Action timeoutCallBack, int timeout)
        {
            ProcessExecutor executor = new ProcessExecutor(timeoutCallBack, timeout);
            return await executor.ExecuteAsync(process);
        }

        public async Task ExecuteAsync(Func<Task> process)
        {
            Func<Task<object>> newFunc = async () =>
            {
                await process();
                return null;
            };

            await this.ExecuteAsync<object>(newFunc);
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> process)
        {
            bool timeout = false;
            object syncObject = new object();

            using (var cancellationTokenSource = new CancellationTokenSource(this.timeout))
            {
                Action action = () =>
                {
                    lock (syncObject)
                    {
                        timeout = true;
                    }

                    this.processTimedoutCallBack();
                };

                using (cancellationTokenSource.Token.Register(() => action()))
                {
                    Exception exception = null;
                    TResult result = default(TResult);

                    try
                    {
                        result = await process();
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    lock (syncObject)
                    {
                        if (timeout)
                        {
                            throw new TimeoutException("Operation timeout.", exception);
                        }
                    }

                    if (exception != null)
                    {
                        throw exception;
                    }

                    return result;
                }
            }
        }
    }
}
