namespace System.CommunicationFramework.Servers
{
    using System.CommunicationFramework.Interfaces;

    /// <summary>
    /// Event arguments used in the event DatagramReceived.
    /// </summary>
    public class DatagramReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatagramReceivedEventArgs" /> class.
        /// </summary>
        /// <param name="receivedDatagramInfo">A ReceivedDatagramInfo.</param>
        public DatagramReceivedEventArgs(ReceivedDatagram receivedDatagramInfo)
        {
            this.ReceivedDatagram = receivedDatagramInfo;
        }

        /// <summary>
        /// Gets or set the ReceivedDatagram.
        /// </summary>
        public ReceivedDatagram ReceivedDatagram { get; private set; }
    }
}
