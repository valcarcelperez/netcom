namespace System.CommunicationFramework.Clients
{
    using System.CommunicationFramework.Interfaces;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Uses a Socket to send and receive messages.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    public class TcpMessageProcessor<T> : MessageProcessor<T>
    {
        /// <summary>
        /// Gets or sets the Framer.
        /// </summary>
        private IDataFramer framer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpMessageProcessor{T}" /> class.
        /// </summary>
        /// <param name="encoder">An encoder.</param>
        /// <param name="client">A socket.</param>
        /// <param name="framer">A framer.</param>
        public TcpMessageProcessor(IMessageEncoder<T> encoder, Socket client, IDataFramer framer = null)
            : base(encoder, client)
        {
            this.framer = framer;
            this.ReuseSocket = false;

            if (client != null)
            {
                this.Client = client;
            }
            else
            {
                this.Client = this.GetClient();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpMessageProcessor{T}" /> class.
        /// </summary>
        /// <param name="encoder">An encoder.</param>
        /// <param name="framer">A framer.</param>
        public TcpMessageProcessor(IMessageEncoder<T> encoder, IDataFramer framer = null)
            : this(encoder, null, framer)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the socket will be reused.
        /// </summary>
        public bool ReuseSocket { get; set; }

        /// <summary>
        /// Connects to a remote end point.
        /// </summary>
        /// <param name="remoteEndPoint">A remote end point.</param>
        public void Connect(EndPoint remoteEndPoint)
        {
            this.Client.Connect(remoteEndPoint);
        }

        /// <summary>
        /// Connects asynchronously to a remote end point.
        /// </summary>
        /// <param name="remoteEndPoint">A remote end point.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task ConnectAsync(EndPoint remoteEndPoint)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(this.Client);
            this.Client.BeginConnect(
                remoteEndPoint,
                iar =>
                {
                    TaskCompletionSource<object> t = (TaskCompletionSource<object>)iar.AsyncState;
                    Socket s = (Socket)t.Task.AsyncState;

                    try
                    {
                        s.EndConnect(iar);
                        t.TrySetResult(null);
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
        /// Disconnects from a remote endpoint.
        /// </summary>
        public void Disconnect()
        {
            this.Client.Disconnect(this.ReuseSocket);
        }

        /// <summary>
        /// Disconnects asynchronously from a remote endpoint.
        /// </summary>
        /// <returns>A Task.</returns>
        public Task DisconnectAsync()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(this.Client);
            this.Client.BeginDisconnect(
                this.ReuseSocket,
                iar =>
                {
                    TaskCompletionSource<object> t = (TaskCompletionSource<object>)iar.AsyncState;
                    Socket s = (Socket)t.Task.AsyncState;

                    try
                    {
                        s.EndDisconnect(iar);
                        t.TrySetResult(null);
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
        /// Sends a message to a remote endpoint.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public void SendMessage(T message)
        {
            int size = this.Encoder.EncodeMessage(message, this.SendBuffer, 0);
            this.Client.Send(this.SendBuffer, 0, size, this.SocketFlags);
        }

        /// <summary>
        /// Sends a message asynchronously to a remote endpoint.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task SendMessageAsync(T message)
        {
            int size = this.Encoder.EncodeMessage(message, this.SendBuffer, 0);

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(this.Client);
            this.Client.BeginSend(
                this.SendBuffer,
                0,
                size,
                this.SocketFlags,
                iar =>
                {
                    TaskCompletionSource<object> t = (TaskCompletionSource<object>)iar.AsyncState;
                    Socket s = (Socket)t.Task.AsyncState;

                    try
                    {
                        s.EndSend(iar);
                        t.TrySetResult(null);
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
        /// Receives a message from a remote endpoint.
        /// </summary>
        /// <returns>A message of type <see cref="{T}" /></returns>
        public T ReceiveMessage()
        {
            this.VerifyFramer();
            bool frameCompleted = false;

            while (!frameCompleted)
            {
                int size = this.Client.Receive(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length, this.SocketFlags);
                int index = 0;
                frameCompleted = this.framer.FrameReceivedData(this.ReceiveBuffer, ref index, size);
            }

            return this.DecodeReceivedMessage();
        }

        /// <summary>
        /// Receives a message asynchronously from a remote endpoint.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
#if NET40
        public Task<T> ReceiveMessageAsync()
        {
            this.VerifyFramer();
            bool frameCompleted = false;

            Task<T> task = new Task<T>(() =>
            {
                while (!frameCompleted)
                {
                    int size = this.ReceiveAsync(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length).Result;
                    int index = 0;
                    frameCompleted = this.framer.FrameReceivedData(this.ReceiveBuffer, ref index, size);
                }

                return this.DecodeReceivedMessage();
            });
            task.Start();
            return task;
        }
#else
        public async Task<T> ReceiveMessageAsync()
        {
            this.VerifyFramer();
            bool frameCompleted = false;

            while (!frameCompleted)
            {
                int size = await this.ReceiveAsync(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length);
                int index = 0;
                frameCompleted = this.framer.FrameReceivedData(this.ReceiveBuffer, ref index, size);
            }

            return this.DecodeReceivedMessage();
        }
#endif

        /// <summary>
        /// Returns a socket that uses TCP.
        /// </summary>
        /// <returns>A new socket.</returns>
        private Socket GetClient()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Receives data asynchronously from a remote endpoint.
        /// </summary>
        /// <param name="buffer">Array containing the data to be received.</param>
        /// <param name="index">Beginning of the data in the buffer.</param>
        /// <param name="size">Size of the data in the buffer.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private Task<int> ReceiveAsync(byte[] buffer, int index, int size)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>(this.Client);
            this.Client.BeginReceive(
                buffer,
                index,
                size,
                this.SocketFlags,
                iar =>
                {
                    TaskCompletionSource<int> t = (TaskCompletionSource<int>)iar.AsyncState;
                    Socket s = (Socket)t.Task.AsyncState;

                    try
                    {
                        int receivedCount = s.EndReceive(iar);
                        t.TrySetResult(receivedCount);
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
        /// Raises an exception if the framer is null.
        /// </summary>
        private void VerifyFramer()
        {
            if (this.framer == null)
            {
                throw new InvalidOperationException("A framer must be passed in the constructor to be able to receive messages.");
            }
        }

        /// <summary>
        /// Decodes a received message.
        /// </summary>
        /// <returns>A message of type <see cref="{T}"/></returns>
        private T DecodeReceivedMessage()
        {
            T message = this.Encoder.DecodeMessage(this.framer.Buffer, 0, this.framer.ReceivedDataSize);
            this.framer.Reset();
            return message;
        }
    }
}
