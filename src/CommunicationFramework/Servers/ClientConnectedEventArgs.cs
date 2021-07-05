namespace System.CommunicationFramework.Servers
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Sockets;

    /// <summary>
    /// Event arguments used in the event ClientConnected in the class <c>TcpServer</c>.
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientConnectedEventArgs" /> class.
        /// </summary>
        /// <param name="client">A Socket that represents the connected client.</param>
        public ClientConnectedEventArgs(Socket client)
        {
            this.Client = client;
        }

        /// <summary>
        /// Gets the Client.
        /// </summary>
        public Socket Client { get; private set; }
    }
}
