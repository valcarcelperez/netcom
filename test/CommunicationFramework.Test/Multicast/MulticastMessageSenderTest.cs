using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Communication.UnitTest;
using System.CommunicationFramework.Common.Network;
using System.CommunicationFramework.Multicast;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationFramework.Multicast
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable"), TestClass]
    public class MulticastMessageSenderTest
    {
        private MockEncoder encoder = new MockEncoder();
        private MulticastMessageSender<MockMessage> globalTarget;
        private IPAddress multicastAddress = IPAddress.Parse("239.0.0.222");
        private IPAddress iPAddress = IPAddress.Any;

        private int port = NetworkHelper.GetFreeUdpPort(IPAddress.Loopback);

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
            this.globalTarget = new MulticastMessageSender<MockMessage>(this.multicastAddress, this.iPAddress, this.port, this.encoder);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            this.globalTarget.Dispose();
        }

        [TestMethod]
        public void MulticastMessageSender_Constructor()
        {
            int port = NetworkHelper.GetFreeUdpPort(IPAddress.Loopback);
            MulticastMessageSender<MockMessage> target = new MulticastMessageSender<MockMessage>(multicastAddress, this.iPAddress, port, this.encoder);
            target.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MulticastMessageSender_Constructor_MulticastAddress_Can_Not_Be_Null()
        {
            IPAddress multicastAddress = null;
            int port = NetworkHelper.GetFreeUdpPort(IPAddress.Loopback);
            MulticastMessageSender<MockMessage> target = new MulticastMessageSender<MockMessage>(multicastAddress, this.iPAddress, port, this.encoder);
            target.Dispose();
        }

        [TestMethod]
        [Timeout(2000)]
        public void MulticastMessageSender_SendMessage()
        {
            Task<MockMessage>[] taskList = new Task<MockMessage>[3];
            for (int i = 0; i < taskList.Length; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => ReceiveMulticastMessage());
            }

            Thread.Sleep(1000);
            MockMessage message = MockMessage.GetTestMessage01();
            this.globalTarget.SendMessage(message);

            Task.WaitAll(taskList);
            foreach (var t in taskList)
            {
                CompareMessages(message, t.Result);
            }
        }

        [TestMethod]
        [Timeout(2000)]
        public async Task MulticastMessageSender_SendMessageAsync()
        {
            Task<MockMessage>[] taskList = new Task<MockMessage>[3];
            for (int i = 0; i < taskList.Length; i++)
            {
                taskList[i] = Task.Factory.StartNew(() => ReceiveMulticastMessage());
            }

            Thread.Sleep(1000);
            MockMessage message = MockMessage.GetTestMessage01();
            await this.globalTarget.SendMessageAsync(message);

            Task.WaitAll(taskList);
            foreach (var t in taskList)
            {
                CompareMessages(message, t.Result);
            }
        }

        private void CompareMessages(MockMessage expected, MockMessage actual)
        {
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Field1, actual.Field1);
            Assert.AreEqual(expected.Field2, actual.Field2);
        }

        private MockMessage ReceiveMulticastMessage()
        {
            EndPoint localEndPoint = new IPEndPoint(IPAddress.Any, this.port);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            using (UdpClient client = new UdpClient())
            {
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.ExclusiveAddressUse = false;
                client.JoinMulticastGroup(this.multicastAddress);
                client.Client.Bind(localEndPoint);

                byte[] data = client.Receive(ref remoteEndPoint);
                return this.encoder.DecodeMessage(data, 0, data.Length);
            }
        }
    }
}
