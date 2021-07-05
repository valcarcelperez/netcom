namespace System.CommunicationFramework.Common.Network
{
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;

    /// <summary>
    /// Helper class with methods related to networking.
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>
        /// Gets a free TCP port for a specific IP address.
        /// </summary>
        /// <param name="address">An IP address.</param>
        /// <returns>The port number.</returns>
        public static int GetFreeTcpPort(IPAddress address)
        {
            TcpListener tcpListener = new TcpListener(address, 0);
            tcpListener.Start();
            int port = (tcpListener.LocalEndpoint as IPEndPoint).Port;
            tcpListener.Stop();
            return port;
        }

        /// <summary>
        /// Gets a free UDP port for a specific IP address.
        /// </summary>
        /// <param name="address">An IP address.</param>
        /// <returns>The port number.</returns>
        public static int GetFreeUdpPort(IPAddress address)
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                IPEndPoint endPoint = new IPEndPoint(address, 0);
                socket.Bind(endPoint);
                return (socket.LocalEndPoint as IPEndPoint).Port;
            }
        }

        /// <summary>
        /// Verifies if a TCP port is being used by a listener.
        /// </summary>
        /// <param name="address">IP address to verify.</param>
        /// <param name="port">A port number to verify.</param>
        /// <returns>True if the port is being used by a listener.</returns>
        public static bool IsTcpPortInUseByListener(IPAddress address, int port)
        {
            IPGlobalProperties globalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var list = globalProperties.GetActiveTcpListeners();
            return list.Any(a => a.Port == port && a.Address.Equals(address));
        }

        /// <summary>
        /// Verifies if a UDP port is being used by a listener.
        /// </summary>
        /// <param name="address">IP address to verify.</param>
        /// <param name="port">A port number to verify.</param>
        /// <returns>True if the port is being used by a listener.</returns>
        public static bool IsUdpPortInUseByListener(IPAddress address, int port)
        {
            IPGlobalProperties globalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var list = globalProperties.GetActiveUdpListeners();
            return list.Any(a => a.Port == port && a.Address.Equals(address));
        }
    }
}
