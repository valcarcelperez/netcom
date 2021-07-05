namespace System.CommunicationFramework.Servers
{
    using System.Net;

    /// <summary>
    /// Defines the information about a received message.
    /// </summary>
    /// <typeparam name="T">The type of message.</typeparam>
    public class UdpReceivedMessageInfo<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UdpReceivedMessageInfo{T}" /> class.
        /// </summary>
        /// <param name="message">A message.</param>
        /// <param name="remoteEndPoint">A remote end point from where the message was sent.</param>
        public UdpReceivedMessageInfo(T message, IPEndPoint remoteEndPoint)
        {
            this.Message = message;
            this.RemoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        /// Gets the Message.
        /// </summary>
        public T Message { get; private set; }

        /// <summary>
        /// Gets the RemoteEndPoint.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }
    }
}
