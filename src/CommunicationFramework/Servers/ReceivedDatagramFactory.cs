namespace System.CommunicationFramework.Servers
{
    using System.CommunicationFramework.Interfaces;
    using System.Net;

    /// <summary>
    /// Implementation of IReceivedDatagramFactory that returns a new object every time.
    /// </summary>
    public class ReceivedDatagramFactory : IReceivedDatagramFactory
    {
        /// <summary>
        /// Size of the datagram.
        /// </summary>
        private int datagramSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivedDatagramFactory" /> class.
        /// </summary>
        /// <param name="datagramSize">Size of the datagram.</param>
        public ReceivedDatagramFactory(int datagramSize)
        {
            this.datagramSize = datagramSize;
        }

        /// <summary>
        /// Returns a ReceivedDatagram.
        /// </summary>
        /// <returns>A ReceivedDatagramInfo.</returns>
        public ReceivedDatagram GetReceivedDatagram()
        {
            ReceivedDatagram result = new ReceivedDatagram();
            result.Buffer = new byte[this.datagramSize];
            result.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            return result;
        }
    }
}
