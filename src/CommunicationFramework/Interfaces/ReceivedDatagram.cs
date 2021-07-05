namespace System.CommunicationFramework.Interfaces
{
    using System.Net;

    /// <summary>
    /// Represents one datagram received by the UDP Server.
    /// </summary>
    public class ReceivedDatagram
    {
        /// <summary>
        /// Gets or sets the Buffer.
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Gets or sets the RemoteEndPoint.
        /// </summary>
        public EndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the Size.
        /// </summary>
        public int Size { get; set; }
    }
}
