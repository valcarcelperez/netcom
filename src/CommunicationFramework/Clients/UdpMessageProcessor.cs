namespace System.CommunicationFramework.Clients
{
    using System;
    using System.CommunicationFramework.Interfaces;
    using System.CommunicationFramework.Servers;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements a generic UDP messages processor. 
    /// Allows sending and receiving messages using UDP.
    /// </summary>
    /// <typeparam name="T">The type of message than can be sent and received.</typeparam>
    public class UdpMessageProcessor<T> : MessageProcessor<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UdpMessageProcessor{T}"/> class.
        /// </summary>
        /// <param name="encoder">An encoder.</param>
        /// <param name="client">A socket used during the communication. If this parameter is null an socket is created internally. The socket is always disposed when this object is disposed.</param>
        public UdpMessageProcessor(IMessageEncoder<T> encoder, Socket client)
            : base(encoder, client)
        {
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
        /// Initializes a new instance of the <see cref="UdpMessageProcessor{T}"/> class.
        /// </summary>
        /// <param name="encoder">An encoder.</param>
        public UdpMessageProcessor(IMessageEncoder<T> encoder)
            : this(encoder, null)
        {
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">A message.</param>
        /// <param name="remoteEndPoint">A remote end point.</param>
        public void SendMessageTo(T message, EndPoint remoteEndPoint)
        {
            int size = this.Encoder.EncodeMessage(message, this.SendBuffer, 0);
            this.Client.SendTo(this.SendBuffer, 0, size, this.SocketFlags, remoteEndPoint);
        }

        /// <summary>
        /// Sends a message asynchronously.
        /// </summary>
        /// <param name="message">A message.</param>
        /// <param name="remoteEndPoint">A remote end point.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task SendMessageToAsync(T message, EndPoint remoteEndPoint)
        {
            int size = this.Encoder.EncodeMessage(message, this.SendBuffer, 0);

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(this.Client);
            this.Client.BeginSendTo(
                this.SendBuffer,
                0,
                size,
                this.SocketFlags,
                remoteEndPoint,
                iar =>
                {
                    TaskCompletionSource<object> t = (TaskCompletionSource<object>)iar.AsyncState;
                    Socket s = (Socket)t.Task.AsyncState;

                    try
                    {
                        s.EndSendTo(iar);
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
        /// Receives a message.
        /// </summary>
        /// <param name="localEndPoint">A local end point.</param>
        /// <returns>Information about received message.</returns>
        public UdpReceivedMessageInfo<T> ReceiveMessageFrom(EndPoint localEndPoint)
        {
            this.Client.Bind(localEndPoint);
            EndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, 0);
            int size = this.Client.ReceiveFrom(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length, this.SocketFlags, ref remoteEP);
            T message = this.Encoder.DecodeMessage(this.ReceiveBuffer, 0, size);
            return new UdpReceivedMessageInfo<T>(message, remoteEP as IPEndPoint);
        }

        /// <summary>
        /// Received a message asynchronously.
        /// </summary>
        /// <param name="localEndPoint">A local end point.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<UdpReceivedMessageInfo<T>> ReceiveMessageFromAsync(EndPoint localEndPoint)
        {
            this.Client.Bind(localEndPoint);
            UdpReceivedDataInfo receivedDataInfo = await this.ReceiveFromAsync(this.ReceiveBuffer, 0, this.ReceiveBuffer.Length);
            T message = this.Encoder.DecodeMessage(this.ReceiveBuffer, 0, receivedDataInfo.Size);
            return new UdpReceivedMessageInfo<T>(message, receivedDataInfo.RemoteEndPoint as IPEndPoint);
        }

        /// <summary>
        /// Returns a socket that uses UDP.
        /// </summary>
        /// <returns>A new socket.</returns>
        private Socket GetClient()
        {
            return new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// Received a message asynchronously.
        /// </summary>
        /// <param name="buffer">An array of type System.Byte that is the storage location for the received data.</param>
        /// <param name="index">The zero-based position in the buffer parameter at which to store the data.</param>
        /// <param name="size">The number of bytes to receive.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private Task<UdpReceivedDataInfo> ReceiveFromAsync(byte[] buffer, int index, int size)
        {
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            TaskCompletionSource<UdpReceivedDataInfo> tcs = new TaskCompletionSource<UdpReceivedDataInfo>(this.Client);
            this.Client.BeginReceiveFrom(
                buffer,
                index,
                size,
                this.SocketFlags,
                ref remoteEndPoint,
                iar =>
                {
                    TaskCompletionSource<UdpReceivedDataInfo> t = (TaskCompletionSource<UdpReceivedDataInfo>)iar.AsyncState;
                    Socket s = (Socket)t.Task.AsyncState;
                    try
                    {
                        int receivedCount = s.EndReceiveFrom(iar, ref remoteEndPoint);
                        t.TrySetResult(new UdpReceivedDataInfo(receivedCount, remoteEndPoint));
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
        /// Defines the information about a received datagram.
        /// </summary>
        private class UdpReceivedDataInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="UdpReceivedDataInfo"/> class.
            /// </summary>
            /// <param name="size">The size of the datagram.</param>
            /// <param name="remoteEndPoint">The remote end point from where the datagram was sent.</param>
            public UdpReceivedDataInfo(int size, EndPoint remoteEndPoint)
            {
                this.Size = size;
                this.RemoteEndPoint = remoteEndPoint;
            }

            /// <summary>
            /// Gets the Size of the datagram.
            /// </summary>
            public int Size { get; private set; }

            /// <summary>
            /// Gets the remote end point.
            /// </summary>
            public EndPoint RemoteEndPoint { get; private set; }
        }
    }
}
