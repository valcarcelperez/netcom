using CommunicationFramework.Servers.UnitTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Communication.UnitTest;
using System.CommunicationFramework.Common.Network;
using System.CommunicationFramework.Interfaces;
using System.CommunicationFramework.Multicast;
using System.CommunicationFramework.Servers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationFramework.Multicast
{
    [TestClass]
    public class MulticastMessageReceiverTest
    {
        private MulticastMessageReceiver<MockMessage> globalTarget;
        private List<ReceivedMessageEventArgs<MockMessage>> receivedMessageArgsList = new List<ReceivedMessageEventArgs<MockMessage>>();
        private List<DecodingErrorEventArgs> decodingErrorEventArgsArgsList = new List<DecodingErrorEventArgs>();
        private object sync = new object();
        private IPAddress multicastAddress = IPAddress.Parse("239.0.0.222");
        private int port = NetworkHelper.GetFreeUdpPort(IPAddress.Loopback);
        private int receiveBufferSize = 2048;
        private UdpMessageServerTestCounters counters;
        private bool decodingErrorHandled = true;
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
            this.globalTarget = GetTarget();
        }

        [TestCleanup()]
        public void Cleanup()
        {
            this.globalTarget.Dispose();
        }

        [TestMethod]
        public void MulticastMessageReceiver_Constructor()
        {
            string name = this.TestContext.TestName;
            IMessageEncoder<MockMessage> encoder = new MockEncoder();
            IPAddress iPAddress = IPAddress.Any;
            int port = NetworkHelper.GetFreeUdpPort(iPAddress);
            MulticastMessageReceiver<MockMessage> target = new MulticastMessageReceiver<MockMessage>(name, this.multicastAddress, iPAddress, port, this.receiveBufferSize, encoder);
            Assert.AreEqual(iPAddress, target.IPAddress);
            Assert.AreEqual(this.multicastAddress, target.MulticastAddress);
            Assert.AreEqual(port, target.Port);
            target.Dispose();
        }

        [TestMethod]
        public void MulticastMessageReceiver_MessageReceived()
        {
            MockMessage message = MockMessage.GetTestMessage01();
            this.globalTarget.Start();           
            Thread.Sleep(250);
            this.SendMulticastMessage(message);
            this.SendMulticastMessage(message);
            Thread.Sleep(250);
            UdpMessageServerTestCounters expected = new UdpMessageServerTestCounters();
            expected.MessageReceivedCount = 2;
            CompareCounters(expected, this.counters);

            ReceivedMessageEventArgs<MockMessage> actual = this.receivedMessageArgsList[0];
            MockMessage.CompareMessages(message, actual.ReceivedMessageInfo.Message);
        }

        [TestMethod]
        public void MulticastMessageReceiver_DecodingError_Is_Raised()
        {
            this.globalTarget.Start();
            Thread.Sleep(250);

            byte[] buffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            this.SendMulticastPackage(buffer, buffer.Length);
            this.SendMulticastPackage(buffer, buffer.Length);

            Thread.Sleep(250);
            UdpMessageServerTestCounters expected = new UdpMessageServerTestCounters();
            expected.DecodingErrorCount = 2;
            CompareCounters(expected, this.counters);
            StringAssert.StartsWith(this.decodingErrorEventArgsArgsList[0].Exception.Message, "Invalid format.");
            StringAssert.StartsWith(this.decodingErrorEventArgsArgsList[1].Exception.Message, "Invalid format.");
        }

        [TestMethod]
        public void MulticastMessageReceiver_DecodingError_GeneralEvent_Is_Not_Raised_When_Handled_Is_False()
        {
            this.globalTarget.Start();
            Thread.Sleep(250);

            byte[] buffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            this.SendMulticastPackage(buffer, buffer.Length);

            Thread.Sleep(250);
            UdpMessageServerTestCounters expected = new UdpMessageServerTestCounters();
            expected.DecodingErrorCount = 1;
            CompareCounters(expected, this.counters);
            StringAssert.StartsWith(this.decodingErrorEventArgsArgsList[0].Exception.Message, "Invalid format.");
        }

        [TestMethod]
        public void MulticastMessageReceiver_DecodingError_GeneralEvent_Is_Raised_When_Handled_Is_False()
        {
            this.globalTarget.Start();
            this.decodingErrorHandled = false;
            Thread.Sleep(250);

            byte[] buffer = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            this.SendMulticastPackage(buffer, buffer.Length);

            Thread.Sleep(250);
            UdpMessageServerTestCounters expected = new UdpMessageServerTestCounters();
            expected.DecodingErrorCount = 1;
            expected.GeneralEventCount = 1;
            CompareCounters(expected, this.counters);
            StringAssert.StartsWith(this.decodingErrorEventArgsArgsList[0].Exception.Message, "Invalid format.");
        }

        private MulticastMessageReceiver<MockMessage> GetTarget()
        {
            string name = this.TestContext.TestName;
            IMessageEncoder<MockMessage> encoder = new MockEncoder();
            MulticastMessageReceiver<MockMessage> target = new MulticastMessageReceiver<MockMessage>(name, this.multicastAddress, IPAddress.Any, this.port, this.receiveBufferSize, encoder);
            target.MessageReceived += globalTarget_MessageReceived;
            target.DecodingError += globalTarget_DecodingError;
            target.GeneralEvent += globalTarget_GeneralEvent;
            return target;
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

        private void SendMulticastMessage(MockMessage message)
        {
            IPEndPoint remotEndPoint = new IPEndPoint(this.multicastAddress, this.port);
            MockEncoder encoder = new MockEncoder();
            byte[] buffer = new byte[1024];
            int size = encoder.EncodeMessage(message, buffer, 0);
            SendMulticastPackage(buffer, size);
        }

        private void SendMulticastPackage(byte[] buffer, int size)
        {
            IPEndPoint remotEndPoint = new IPEndPoint(this.multicastAddress, this.port);
            using (UdpClient client = new UdpClient())
            {
                client.Send(buffer, size, remotEndPoint);
            }
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
