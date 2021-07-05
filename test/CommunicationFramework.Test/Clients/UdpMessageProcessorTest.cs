using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Communication.UnitTest;
using System.CommunicationFramework.Clients;
using System.CommunicationFramework.Common.Network;
using System.CommunicationFramework.Interfaces;
using System.CommunicationFramework.Servers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationFramework.Clients.UnitTest
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class UdpServerMock
    {
        private UdpServer udpServer;
        public IList<ReceivedDatagram> DatagramList = new List<ReceivedDatagram>();

        public UdpServerMock(string serverIdentifier, IPAddress iPAddress, int port)
        {
            this.udpServer = new UdpServer(serverIdentifier, iPAddress, port, 1024 * 10, 1024);
            this.udpServer.DatagramReceived += udpServer_DatagramReceived;
        }

        public Socket Socket
        {
            get
            {
                return this.udpServer.Socket;
            }
        }

        public void Start()
        {
            this.udpServer.Start();
        }

        public void Stop()
        {
            this.udpServer.Stop();
        }

        void udpServer_DatagramReceived(object sender, DatagramReceivedEventArgs e)
        {
            this.DatagramList.Add(e.ReceivedDatagram);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable"), TestClass]
    public class UdpMessageProcessorTest
    {
        private MockEncoder encoder = new MockEncoder();
        private UdpMessageProcessor<MockMessage> globalTarget;

        private UdpServerMock udpServer;
        private EndPoint remoteEndPoint;
        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestInitialize()]
        public void Initialize()
        {
            this.udpServer = new UdpServerMock(this.testContextInstance.TestName, IPAddress.Loopback, 0);
            this.udpServer.Start();
            int port = (this.udpServer.Socket.LocalEndPoint as IPEndPoint).Port;
            this.remoteEndPoint = new IPEndPoint((this.udpServer.Socket.LocalEndPoint as IPEndPoint).Address, port);
            this.globalTarget = new UdpMessageProcessor<MockMessage>(this.encoder);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            this.globalTarget.Dispose();
            udpServer.Stop();
        }

        [TestMethod]
        public void UdpMessageProcessor_Constructor()
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            UdpMessageProcessor<MockMessage> target = new UdpMessageProcessor<MockMessage>(this.encoder, client);
            Assert.AreEqual(client, target.Client);
            target.Dispose();
        }

        [TestMethod]
        public void UdpMessageProcessor_Constructor_Passing_One_Parameter()
        {
            UdpMessageProcessor<MockMessage> target = new UdpMessageProcessor<MockMessage>(this.encoder);
            Assert.IsNotNull(target.Client);
            target.Dispose();
        }

        [TestMethod]
        public void UdpMessageProcessor_SendMessageTo()
        {
            MockMessage message = MockMessage.GetTestMessage01();
            this.globalTarget.SendMessageTo(message, remoteEndPoint);
            ValidateAfterSendToMessage(message);
        }

        [TestMethod]
        public async Task UdpMessageProcessor_SendMessageToAsync()
        {
            MockMessage message = MockMessage.GetTestMessage01();
            await this.globalTarget.SendMessageToAsync(message, remoteEndPoint);
            ValidateAfterSendToMessage(message);
        }

        [TestMethod]
        public void UdpMessageProcessor_ReceiveMessageFrom()
        {
            int port = NetworkHelper.GetFreeUdpPort(IPAddress.Loopback);
            MockMessage message = MockMessage.GetTestMessage01();
            EndPoint senderEndPoint = null;
            Task.Run(() =>
                {
                    Thread.Sleep(250);
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        byte[] buffer = new byte[1024];
                        int size = this.encoder.EncodeMessage(message, buffer, 0);
                        socket.SendTo(buffer, 0, size, SocketFlags.None, remoteEndPoint);
                        senderEndPoint = socket.LocalEndPoint;
                    }
                });

            EndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, port);
            UdpReceivedMessageInfo<MockMessage> receivedMessageInfo = this.globalTarget.ReceiveMessageFrom(remoteEP);
            ValidateAfterReceiceFromMessage(message, port, senderEndPoint, receivedMessageInfo);
        }

        [TestMethod]
        public async Task UdpMessageProcessor_ReceiveMessageAsyncFrom()
        {
            int port = NetworkHelper.GetFreeUdpPort(IPAddress.Loopback);
            MockMessage message = MockMessage.GetTestMessage01();
            EndPoint senderEndPoint = null;
            var t = Task.Run(() =>
            {
                Thread.Sleep(250);
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    byte[] buffer = new byte[1024];
                    int size = this.encoder.EncodeMessage(message, buffer, 0);
                    socket.SendTo(buffer, 0, size, SocketFlags.None, remoteEndPoint);
                    senderEndPoint = socket.LocalEndPoint;
                }
            });

            EndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, port);
            UdpReceivedMessageInfo<MockMessage> receivedMessageInfo = await this.globalTarget.ReceiveMessageFromAsync(remoteEP);
            ValidateAfterReceiceFromMessage(message, port, senderEndPoint, receivedMessageInfo);
        }

        private void ValidateAfterSendToMessage(MockMessage message)
        {
            Thread.Sleep(250);

            Assert.AreEqual(1, this.udpServer.DatagramList.Count);
            ReceivedDatagram receivedDatagram = this.udpServer.DatagramList[0];
            MockMessage receivedMessage = this.encoder.DecodeMessage(receivedDatagram.Buffer, 0, receivedDatagram.Size);
            MockMessage.CompareMessages(message, receivedMessage);

            IPEndPoint clientEndPoint = this.globalTarget.Client.LocalEndPoint as IPEndPoint;
            IPEndPoint clientEndPointInServer = receivedDatagram.RemoteEndPoint as IPEndPoint;
            Assert.AreEqual(clientEndPoint.Port, clientEndPointInServer.Port);
            Assert.IsTrue(clientEndPointInServer.Address.Equals(IPAddress.Loopback));
        }

        private void ValidateAfterReceiceFromMessage(MockMessage message, int port, EndPoint senderEndPoint, UdpReceivedMessageInfo<MockMessage> receivedMessageInfo)
        {
            MockMessage.CompareMessages(message, receivedMessageInfo.Message);
            IPEndPoint clientEP = receivedMessageInfo.RemoteEndPoint as IPEndPoint;
            IPEndPoint senderEP = senderEndPoint as IPEndPoint;
            Assert.AreEqual(senderEP.Port, clientEP.Port);
            Assert.IsTrue(clientEP.Address.Equals(IPAddress.Loopback));
        }
    }
}
