using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Communication.UnitTest;
using System.CommunicationFramework.Common.Network;
using System.CommunicationFramework.Interfaces;
using System.CommunicationFramework.Servers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationFramework.Servers.UnitTest
{
    public struct UdpMessageServerTestCounters
    {
        public int StartedCount;
        public int StoppedCount;
        public int DatagramReceivedCount;
        public int MessageReceivedCount;
        public int DecodingErrorCount;
        public int GeneralEventCount;
    }

    [TestClass]
    public class UdpMessageServerTest
    {
        private UdpMessageServer<MockMessage> globalTarget;
        private TestContext testContextInstance;
        private List<ReceivedMessageEventArgs<MockMessage>> receivedMessageArgsList = new List<ReceivedMessageEventArgs<MockMessage>>();
        private List<DecodingErrorEventArgs> decodingErrorEventArgsArgsList = new List<DecodingErrorEventArgs>();
        private object sync = new object();
        private UdpMessageServerTestCounters counters;
        private bool decodingErrorHandled = true;

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
            this.globalTarget = GetTarget();
        }

        [TestCleanup()]
        public void Cleanup()
        {
            this.globalTarget.Dispose();
        }

        [TestMethod]
        public void UdpMessageServer_Constructor()
        {
            string name = this.TestContext.TestName;
            IPAddress iPAddress = IPAddress.Loopback;
            int port = NetworkHelper.GetFreeUdpPort(iPAddress);
            int maxDatagramSize = 1024;
            IMessageEncoder<MockMessage> encoder = new MockEncoder();
            UdpMessageServer<MockMessage> target = new UdpMessageServer<MockMessage>(name, iPAddress, port, maxDatagramSize * 10, maxDatagramSize, encoder);
            target.Dispose();
        }

        [TestMethod]
        public void UdpMessageServer_MessageReceived()
        {
            this.globalTarget.Start();
            
            MockMessage message = MockMessage.GetTestMessage01();
            MockEncoder encoder = new MockEncoder();
            byte[] buffer = new byte[1024];
            int size = encoder.EncodeMessage(message, buffer, 0);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Port);
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.SendTo(buffer, 0, size, SocketFlags.None, remoteEndPoint);

            Thread.Sleep(250);
            UdpMessageServerTestCounters expected = new UdpMessageServerTestCounters();
            expected.MessageReceivedCount = 1;
            CompareCounters(expected, this.counters);

            ReceivedMessageEventArgs<MockMessage> actual = this.receivedMessageArgsList[0];
            MockMessage.CompareMessages(message, actual.ReceivedMessageInfo.Message);
        }

        [TestMethod]
        public void UdpMessageServer_DecodingError_Is_Raised()
        {
            this.globalTarget.Start();

            byte[] buffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Port);
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.SendTo(buffer, remoteEndPoint);

            Thread.Sleep(250);
            UdpMessageServerTestCounters expected = new UdpMessageServerTestCounters();
            expected.DecodingErrorCount = 1;
            CompareCounters(expected, this.counters);
            StringAssert.StartsWith(this.decodingErrorEventArgsArgsList[0].Exception.Message, "Invalid format.");            
        }

        [TestMethod]
        public void UdpMessageServer_DecodingError_GeneralEvent_Is_Not_Raised_When_Handled_Is_False()
        {
            this.globalTarget.Start();

            byte[] buffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Port);
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.SendTo(buffer, remoteEndPoint);

            Thread.Sleep(250);
            UdpMessageServerTestCounters expected = new UdpMessageServerTestCounters();
            expected.DecodingErrorCount = 1;
            CompareCounters(expected, this.counters);
            StringAssert.StartsWith(this.decodingErrorEventArgsArgsList[0].Exception.Message, "Invalid format.");
        }

        [TestMethod]
        public void UdpMessageServer_DecodingError_GeneralEvent_Is_Raised_When_Handled_Is_False()
        {
            this.globalTarget.Start();

            this.decodingErrorHandled = false;
            byte[] buffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Port);
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.SendTo(buffer, remoteEndPoint);

            Thread.Sleep(250);
            UdpMessageServerTestCounters expected = new UdpMessageServerTestCounters();
            expected.DecodingErrorCount = 1;
            expected.GeneralEventCount = 1;
            CompareCounters(expected, this.counters);
            StringAssert.StartsWith(this.decodingErrorEventArgsArgsList[0].Exception.Message, "Invalid format.");
        }

        void globalTarget_DecodingError(object sender, DecodingErrorEventArgs e)
        {
            lock (this.sync)
            {
                this.decodingErrorEventArgsArgsList.Add(e);
                this.counters.DecodingErrorCount++;
                e.Handled = this.decodingErrorHandled;
            }
        }

        void globalTarget_MessageReceived(object sender, ReceivedMessageEventArgs<MockMessage> e)
        {
            lock (this.sync)
            {
                receivedMessageArgsList.Add(e);
                this.counters.MessageReceivedCount++;
            }
        }

        void globalTarget_GeneralEvent(object sender, System.CommunicationFramework.Common.CancellableMethodManagerEventArgs e)
        {
            lock (this.sync)
            {
                this.counters.GeneralEventCount++;
            }
        }

        private UdpMessageServer<MockMessage> GetTarget()
        {
            string name = this.TestContext.TestName;
            int maxDatagramSize = 1024;
            IMessageEncoder<MockMessage> encoder = new MockEncoder();
            UdpMessageServer<MockMessage> target = new UdpMessageServer<MockMessage>(name, IPAddress.Any, 0, maxDatagramSize * 10, maxDatagramSize, encoder);
            target.MessageReceived += globalTarget_MessageReceived;
            target.DecodingError += globalTarget_DecodingError;
            target.GeneralEvent += globalTarget_GeneralEvent;
            return target;
        }

        private void CompareCounters(UdpMessageServerTestCounters expected, UdpMessageServerTestCounters actual)
        {
            lock (sync)
            {
                Assert.AreEqual(expected.StartedCount, actual.StartedCount);
                Assert.AreEqual(expected.StoppedCount, actual.StoppedCount);
                Assert.AreEqual(expected.DatagramReceivedCount, actual.DatagramReceivedCount);
                Assert.AreEqual(expected.MessageReceivedCount, actual.MessageReceivedCount);
                Assert.AreEqual(expected.DecodingErrorCount, actual.DecodingErrorCount);
                Assert.AreEqual(expected.GeneralEventCount, actual.GeneralEventCount);                
            }
        }
    }
}
