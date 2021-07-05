using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
    public struct UdpServerTestCounters
    {
        public int StartedCount;
        public int StoppedCount;
        public int DatagramReceivedCount;
        public int GeneralEventCount;
    }

    [TestClass]
    public class UdpServerTest
    {
        private UdpServerTestCounters counters;
        private object sync = new object();
        private List<DatagramReceivedEventArgs> argsList = new List<DatagramReceivedEventArgs>();
        private UdpServer globalTarget;
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

        private UdpServer GetTarget()
        {
            string name = this.TestContext.TestName;
            int maxDatagramSize = 1024;
            UdpServer target = new UdpServer(name, IPAddress.Any, 0, maxDatagramSize * 10, maxDatagramSize);
            return target;
        }

        [TestInitialize()]
        public void Initialize()
        {
            this.globalTarget = GetTarget();
            SubscribeToEvents(this.globalTarget);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            this.globalTarget.Dispose();
        }

        [TestMethod]
        public void UdpServer_Constructor()
        {
            string name = this.TestContext.TestName;
            IPAddress iPAddress = IPAddress.Loopback;
            int port = NetworkHelper.GetFreeUdpPort(iPAddress);
            int maxDatagramSize = 1024;
            UdpServer target = new UdpServer(name, iPAddress, port, maxDatagramSize * 10, maxDatagramSize);
            try
            {
                Assert.AreEqual(name, target.ServerIdentifier);
                Assert.AreEqual(iPAddress, target.IPAddress);
                Assert.AreEqual(port, target.Port);
                Assert.IsNotNull(target.UdpReceivedDatagramInfoProvider);
                Assert.IsNull(target.Socket);
                Assert.AreEqual(typeof(ReceivedDatagramFactory), target.UdpReceivedDatagramInfoProvider.GetType());
            }
            finally
            {
                target.Dispose();
            }
        }

        [TestMethod]
        public void UdpServer_Port_Not_Assigned()
        {
            int port = 0;
            IPAddress iPAddress = IPAddress.Loopback;
            int maxDatagramSize = 1024;

            using (UdpServer target = new UdpServer(this.TestContext.TestName, iPAddress, port, maxDatagramSize * 10, maxDatagramSize))
            {
                target.Start();
                Assert.IsTrue((target.Socket.LocalEndPoint as IPEndPoint).Port > 0);
                Assert.AreEqual(port, target.Port);
            }
        }

        [TestMethod]
        public void UdpServer_Port_Assigned()
        {
            IPAddress iPAddress = IPAddress.Loopback;
            int port = NetworkHelper.GetFreeUdpPort(iPAddress);
            int maxDatagramSize = 1024;
            using (UdpServer target = new UdpServer(this.TestContext.TestName, iPAddress, port, maxDatagramSize * 10, maxDatagramSize))
            {
                target.Start();
                Assert.AreEqual(port, (target.Socket.LocalEndPoint as IPEndPoint).Port);
                Assert.AreEqual(port, target.Port);
            }
        }

        [TestMethod]
        public void UdpServer_Started_Must_Be_Called()
        {
            this.globalTarget.Start();
            UdpServerTestCounters expected = new UdpServerTestCounters();
            expected.StartedCount = 1;
            this.CompareCounters(expected, counters);
        }

        [TestMethod]
        public void UdpServer_Stopped_Must_Be_Called()
        {
            this.globalTarget.Start();
            this.ResetCounters();
            this.globalTarget.Stop();
            UdpServerTestCounters expected = new UdpServerTestCounters();
            expected.StoppedCount = 1;
            this.CompareCounters(expected, counters);
        }

        [TestMethod]
        public void UdpServer_Running()
        {
            bool actual = this.globalTarget.Running;
            Assert.IsFalse(actual);
            this.globalTarget.Start();
            actual = this.globalTarget.Running;
            Assert.IsTrue(actual);
            this.globalTarget.Stop();
            actual = this.globalTarget.Running;
            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void UdpServer_DatagramReceived_Sync()
        {
            this.globalTarget.ReceiveAsynchronously = false;
            this.UdpServer_DatagramReceived();
        }

        [TestMethod]
        public void UdpServer_DatagramReceived_Async()
        {
            this.globalTarget.ReceiveAsynchronously = true;
            this.UdpServer_DatagramReceived();
        }

        private void UdpServer_DatagramReceived()
        {
            this.globalTarget.Start();
            this.ResetCounters();

            byte[] data0 = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] data1 = new byte[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Port);

            Thread.Sleep(250);
            Socket client0 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            int sentSize0 = client0.SendTo(data0, remoteEndPoint);

            Socket client1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            int sentSize1 = client1.SendTo(data1, remoteEndPoint);
            Thread.Sleep(250);

            UdpServerTestCounters expected = new UdpServerTestCounters();
            expected.DatagramReceivedCount = 2;
            this.CompareCounters(expected, counters);

            Assert.AreEqual(2, argsList.Count);
            ReceivedDatagram receivedDatagram;

            receivedDatagram = argsList[0].ReceivedDatagram;
            Assert.AreEqual((client0.LocalEndPoint as IPEndPoint).Port, (receivedDatagram.RemoteEndPoint as IPEndPoint).Port);
            CompareBuffer(receivedDatagram.Size, receivedDatagram.Buffer, data0);

            receivedDatagram = argsList[1].ReceivedDatagram;
            Assert.AreEqual((client1.LocalEndPoint as IPEndPoint).Port, (receivedDatagram.RemoteEndPoint as IPEndPoint).Port);
            this.CompareBuffer(receivedDatagram.Size, receivedDatagram.Buffer, data1);
        }

        [TestMethod]
        public void UdpServer_Listener_Closed_When_Disposed_Sync()
        {
            this.globalTarget.ReceiveAsynchronously = false;
            this.UdpServer_Listener_Closed_When_Disposed();
        }

        [TestMethod]
        public void UdpServer_Listener_Closed_When_Disposed_Async()
        {
            this.globalTarget.ReceiveAsynchronously = true;
            this.UdpServer_Listener_Closed_When_Disposed();
        }

        private void UdpServer_Listener_Closed_When_Disposed()
        {
            int maxDatagramSize = 1024;
            UdpServer target = new UdpServer(this.TestContext.TestName, IPAddress.Loopback, 0, maxDatagramSize * 10, maxDatagramSize);
            target.Start();
            IPAddress iPAddress = (target.Socket.LocalEndPoint as IPEndPoint).Address;
            int port = (target.Socket.LocalEndPoint as IPEndPoint).Port;
            bool portOpen;

            portOpen = NetworkHelper.IsUdpPortInUseByListener(iPAddress, port);
            Assert.IsTrue(portOpen);
            target.Dispose();

            portOpen = NetworkHelper.IsUdpPortInUseByListener(iPAddress, port);
            Assert.IsFalse(portOpen);
        }

        [TestMethod]
        public void UdpServer_Listener_Closed_When_Stopped_Sync()
        {
            this.globalTarget.ReceiveAsynchronously = false;
            this.UdpServer_Listener_Closed_When_Stopped();
        }

        [TestMethod]
        public void UdpServer_Listener_Closed_When_Stopped_Async()
        {
            this.globalTarget.ReceiveAsynchronously = true;
            this.UdpServer_Listener_Closed_When_Stopped();
        }

        private void UdpServer_Listener_Closed_When_Stopped()
        {
            int maxDatagramSize = 1024;
            UdpServer target = new UdpServer(this.TestContext.TestName, IPAddress.Loopback, 0, maxDatagramSize * 10, maxDatagramSize);
            target.Start();
            IPAddress iPAddress = (target.Socket.LocalEndPoint as IPEndPoint).Address;
            int port = (target.Socket.LocalEndPoint as IPEndPoint).Port;
            bool portOpen;

            portOpen = NetworkHelper.IsUdpPortInUseByListener(iPAddress, port);
            Assert.IsTrue(portOpen);
            target.Stop();

            portOpen = NetworkHelper.IsUdpPortInUseByListener(iPAddress, port);
            Assert.IsFalse(portOpen);
            target.Dispose();
        }

        [TestMethod]
        public void UdpServer_Listener_Restars_Sync()
        {
            this.globalTarget.ReceiveAsynchronously = false;
            this.UdpServer_Listener_Restars();
        }

        [TestMethod]
        public void UdpServer_Listener_Restars_Async()
        {
            this.globalTarget.ReceiveAsynchronously = true;
            this.UdpServer_Listener_Restars();
        }

        private void UdpServer_Listener_Restars()
        {
            this.globalTarget.Start();
            IPAddress iPAddress = (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Address;
            int port = (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Port;
            bool portOpen;

            portOpen = NetworkHelper.IsUdpPortInUseByListener(iPAddress, port);
            Assert.IsTrue(portOpen);
            this.globalTarget.Stop();

            portOpen = NetworkHelper.IsUdpPortInUseByListener(iPAddress, port);
            Assert.IsFalse(portOpen);

            this.globalTarget.Start();
            iPAddress = (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Address;
            port = (this.globalTarget.Socket.LocalEndPoint as IPEndPoint).Port;
            portOpen = NetworkHelper.IsUdpPortInUseByListener(iPAddress, port);
            Assert.IsTrue(portOpen);
        }

        [TestMethod]
        public void UdpServer_GeneralEvent_Is_Not_Raised_When_Disposing_Sync()
        {
            this.globalTarget.ReceiveAsynchronously = false;
            this.UdpServer_GeneralEvent_Is_Not_Raised_When_Disposing();
        }

        [TestMethod]
        public void UdpServer_GeneralEvent_Is_Not_Raised_When_Disposing_Async()
        {
            this.globalTarget.ReceiveAsynchronously = true;
            this.UdpServer_GeneralEvent_Is_Not_Raised_When_Disposing();
        }

        private void UdpServer_GeneralEvent_Is_Not_Raised_When_Disposing()
        {
            int maxDatagramSize = 1024;
            UdpServer target = new UdpServer(this.TestContext.TestName, IPAddress.Loopback, 0, maxDatagramSize * 10, maxDatagramSize);
            this.SubscribeToEvents(target);
            
            target.Start();
            Thread.Sleep(250);
            this.ResetCounters();

            target.Dispose();
            Thread.Sleep(250);

            UdpServerTestCounters expected = new UdpServerTestCounters();
            expected.StoppedCount = 1;
            this.CompareCounters(expected, counters);            
        }

        private void CompareBuffer(int expectedSize, byte[] expected, byte[] actual)
        {
            for (int i = 0; i < expectedSize; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        void target_DatagramReceived(object sender, DatagramReceivedEventArgs e)
        {
            lock (sync)
            {
                argsList.Add(e);
                counters.DatagramReceivedCount++;
            }
        }

        void target_Started(object sender, EventArgs e)
        {
            lock (sync)
            {
                counters.StartedCount++;
            }
        }

        void target_Stopped(object sender, EventArgs e)
        {
            lock (sync)
            {
                counters.StoppedCount++;
            }
        }

        void target_GeneralEvent(object sender, System.CommunicationFramework.Common.CancellableMethodManagerEventArgs e)
        {
            lock (sync)
            {
                counters.GeneralEventCount++;
            }
        }

        private void CompareCounters(UdpServerTestCounters expected, UdpServerTestCounters actual)
        {
            lock (sync)
            {
                Assert.AreEqual(expected.StartedCount, actual.StartedCount);
                Assert.AreEqual(expected.StoppedCount, actual.StoppedCount);
                Assert.AreEqual(expected.DatagramReceivedCount, actual.DatagramReceivedCount);
                Assert.AreEqual(expected.GeneralEventCount, actual.GeneralEventCount);
            }
        }

        private void ResetCounters()
        {
            lock (sync)
            {
                counters = new UdpServerTestCounters();
            }
        }

        private void SubscribeToEvents(UdpServer target)
        {
            target.DatagramReceived += target_DatagramReceived;
            target.Stopped += target_Stopped;
            target.Started += target_Started;
            target.GeneralEvent += target_GeneralEvent;
        }
    }
}
