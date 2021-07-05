namespace System.CommunicationFramework.Servers
{
    /// <summary>
    /// Event data for ReceivedMessage event.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    public class ReceivedMessageEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivedMessageEventArgs{T}" /> class.
        /// </summary>
        /// <param name="receivedMessageInfo">An <c>UdpReceivedMessageInfo</c>.</param>
        public ReceivedMessageEventArgs(UdpReceivedMessageInfo<T> receivedMessageInfo)
        {
            this.ReceivedMessageInfo = receivedMessageInfo;
        }

        /// <summary>
        /// Gets the ReceivedMessageInfo.
        /// </summary>
        public UdpReceivedMessageInfo<T> ReceivedMessageInfo { get; private set; }
    }
}
