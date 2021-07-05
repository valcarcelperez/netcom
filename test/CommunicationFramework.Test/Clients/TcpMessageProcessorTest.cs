using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Communication.UnitTest;
using System.CommunicationFramework.Clients;
using System.CommunicationFramework.Servers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationFramework.Clients.UnitTest
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class TcpServerMock
    {
        private TcpServer tcpServer;
        public IList<Socket> ClientList = new List<Socket>();

        public TcpServerMock(string serverIdentifier, IPAddress iPAddress, int port)
        {
            this.tcpServer = new TcpServer(serverIdentifier, iPAddress, port);
            this.tcpServer.ClientConnected += tcpServer_ClientConnected;            
        }

        public TcpListener TcpListener
        {
            get
            {
                return this.tcpServer.TcpListener;
            }
        }

        public void Start()
        {
            this.tcpServer.Start();
        }

        public void Stop()
        {
            this.tcpServer.Stop();
        }

        void tcpServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            this.ClientList.Add(e.Client);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable"), TestClass]
    public class TcpMessageProcessorTest
    {
        private MockEncoder encoder = new MockEncoder();
        private MockMessageFramer framer = new MockMessageFramer(1024);
        private TcpMessageProcessor<MockMessage> globalTarget;

        private TcpServerMock tcpServer;
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
            this.tcpServer = new TcpServerMock(this.testContextInstance.TestName, IPAddress.Loopback, 0);
            this.tcpServer.Start();
            int port = (this.tcpServer.TcpListener.LocalEndpoint as IPEndPoint).Port;
            this.remoteEndPoint = new IPEndPoint((this.tcpServer.TcpListener.LocalEndpoint as IPEndPoint).Address, port);
            this.globalTarget = new TcpMessageProcessor<MockMessage>(this.encoder, this.framer);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            this.globalTarget.Dispose();
            tcpServer.Stop();
        }


        [TestMethod]
        public void TcpMessageProcessor_Constructor()
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            TcpMessageProcessor<MockMessage> target = new TcpMessageProcessor<MockMessage>(this.encoder, client, this.framer);
            Assert.IsFalse(target.ReuseSocket);
            target.Dispose();
        }

        [TestMethod]
        public void TcpMessageProcessor_Constructor_Passing_One_Parameter()
        {
            TcpMessageProcessor<MockMessage> target = new TcpMessageProcessor<MockMessage>(this.encoder);
            Assert.IsFalse(target.ReuseSocket);
            target.Dispose();
        }

        [TestMethod]
        public void TcpMessageProcessor_Connect()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);

            Assert.AreEqual(1, this.tcpServer.ClientList.Count);
            IPEndPoint clientEndPoint = this.globalTarget.Client.RemoteEndPoint as IPEndPoint;
            IPEndPoint clientInServerEndPoint = this.tcpServer.ClientList[0].RemoteEndPoint as IPEndPoint;

            Assert.IsTrue(clientEndPoint.Address.Equals(clientEndPoint.Address));
            Assert.AreEqual(clientEndPoint.Port, clientEndPoint.Port);

            this.tcpServer.ClientList[0].Send(new byte[] { 1, 2 });
        }

#if NET40
        [TestMethod]
        [Timeout(5000)]
        public void TcpMessageProcessor_ConnectAsync()
        {
            Task task = this.globalTarget.ConnectAsync(this.remoteEndPoint);
            task.Wait();
            Thread.Sleep(250);

            Assert.AreEqual(1, this.tcpServer.ClientList.Count);
            IPEndPoint clientEndPoint = this.globalTarget.Client.RemoteEndPoint as IPEndPoint;
            IPEndPoint clientInServerEndPoint = this.tcpServer.ClientList[0].RemoteEndPoint as IPEndPoint;

            Assert.IsTrue(clientEndPoint.Address.Equals(clientEndPoint.Address));
            Assert.AreEqual(clientEndPoint.Port, clientEndPoint.Port);

            this.tcpServer.ClientList[0].Send(new byte[] { 1, 2 });
        }
#else
        [TestMethod]
        [Timeout(5000)]
        public async Task TcpMessageProcessor_ConnectAsync()
        {
            await this.globalTarget.ConnectAsync(this.remoteEndPoint);
            Thread.Sleep(250);

            Assert.AreEqual(1, this.tcpServer.ClientList.Count);
            IPEndPoint clientEndPoint = this.globalTarget.Client.RemoteEndPoint as IPEndPoint;
            IPEndPoint clientInServerEndPoint = this.tcpServer.ClientList[0].RemoteEndPoint as IPEndPoint;

            Assert.IsTrue(clientEndPoint.Address.Equals(clientEndPoint.Address));
            Assert.AreEqual(clientEndPoint.Port, clientEndPoint.Port);

            this.tcpServer.ClientList[0].Send(new byte[] { 1, 2 });
        }

#endif

        [TestMethod]
        public void TcpMessageProcessor_Disconnect()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            this.globalTarget.Disconnect();
        }

#if NET40
        [TestMethod]
        [Timeout(5000)]
        public void TcpMessageProcessor_DisconnectAsync()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            Task task = this.globalTarget.DisconnectAsync();
            task.Wait();
        }
#else
        [TestMethod]
        [Timeout(5000)]
        public async Task TcpMessageProcessor_DisconnectAsync()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            await this.globalTarget.DisconnectAsync();
        }
#endif

        [TestMethod]
        public void TcpMessageProcessor_SendMessage()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            MockMessage message = MockMessage.GetTestMessage01();
            this.globalTarget.SendMessage(message);
            ValidateAfterSendMessage(message);
        }

#if NET40
        [TestMethod]
        [Timeout(5000)]
        public void TcpMessageProcessor_SendMessageAsync()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            MockMessage message = MockMessage.GetTestMessage01();
            Task task = this.globalTarget.SendMessageAsync(message);
            task.Wait();
            ValidateAfterSendMessage(message);
        }
#else
        [TestMethod]
        [Timeout(5000)]
        public async Task TcpMessageProcessor_SendMessageAsync()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            MockMessage message = MockMessage.GetTestMessage01();
            await this.globalTarget.SendMessageAsync(message);
            ValidateAfterSendMessage(message);
        }
#endif

        [TestMethod]
        public void TcpMessageProcessor_ReceiveMessage()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            MockMessage message = MockMessage.GetTestMessage01();

            byte[] buffer = new byte[1024];
            int size = this.encoder.EncodeMessage(message, buffer, 0);
            Socket client = this.tcpServer.ClientList[0];
            client.Send(buffer, size, SocketFlags.None);

            MockMessage receivedMessage = this.globalTarget.ReceiveMessage();
            MockMessage.CompareMessages(message, receivedMessage);
        }

#if NET40
        [TestMethod]
        [Timeout(5000)]
        public void TcpMessageProcessor_ReceiveMessageAsync()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            MockMessage message = MockMessage.GetTestMessage01();

            byte[] buffer = new byte[1024];
            int size = this.encoder.EncodeMessage(message, buffer, 0);
            Socket client = this.tcpServer.ClientList[0];

            Task.Factory.StartNew(() =>
            {
                client.Send(buffer, 10, SocketFlags.None);
                Thread.Sleep(400);
                client.Send(buffer, 10, size - 10, SocketFlags.None);
            });            

            Task<MockMessage> task = this.globalTarget.ReceiveMessageAsync();
            task.Wait();
            MockMessage.CompareMessages(message, task.Result);
        }
#else
#pragma warning disable 4014
        [TestMethod]
        [Timeout(5000)]
        public async Task TcpMessageProcessor_ReceiveMessageAsync()
        {
            this.globalTarget.Connect(this.remoteEndPoint);
            Thread.Sleep(250);
            MockMessage message = MockMessage.GetTestMessage01();

            byte[] buffer = new byte[1024];
            int size = this.encoder.EncodeMessage(message, buffer, 0);
            Socket client = this.tcpServer.ClientList[0];

            Task.Factory.StartNew(() =>
            {
                client.Send(buffer, 10, SocketFlags.None);
                Thread.Sleep(400);
                client.Send(buffer, 10, size - 10, SocketFlags.None);
            });            

            MockMessage receivedMessage = await this.globalTarget.ReceiveMessageAsync();
            MockMessage.CompareMessages(message, receivedMessage);
        }
#pragma warning restore 4014
#endif

        private void ValidateAfterSendMessage(MockMessage message)
        {
            Socket client = this.tcpServer.ClientList[0];
            byte[] buffer = new byte[1024];
            int size = client.Receive(buffer);
            MockMessage receivedMessage = this.encoder.DecodeMessage(buffer, 0, size);
            MockMessage.CompareMessages(message, receivedMessage);
        }
    }
}
