using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.CommunicationFramework.Common.Network;
using System.CommunicationFramework.Servers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommunicationFramework.Servers.UnitTest
{
    public struct TcpServerTestCounters
    {
        public int StartedCount;
        public int StoppedCount;
        public int ClientConnectedCount;
        public int GeneralEventCount;
    }

    [TestClass]
    public class TcpServerTest
    {
        private TcpServerTestCounters counters;
        private object sync = new object();
        private List<ClientConnectedEventArgs> argsList = new List<ClientConnectedEventArgs>();
        private TcpServer globalTarget;
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

        private TcpServer GetTarget()
        {
            return new TcpServer(this.TestContext.TestName, IPAddress.Loopback, 0);
        }

        [TestInitialize()]
        public void Initialize()
        {
            this.globalTarget = GetTarget();
            this.SubscribeToEvents(this.globalTarget);
        }

        [TestCleanup()]
        public void Cleanup()
        {
            this.globalTarget.Dispose();
        }

        private void CompareCounters(TcpServerTestCounters expected, TcpServerTestCounters actual)
        {
            lock (sync)
            {
                Assert.AreEqual(expected.StartedCount, actual.StartedCount);
                Assert.AreEqual(expected.StoppedCount, actual.StoppedCount);
                Assert.AreEqual(expected.ClientConnectedCount, actual.ClientConnectedCount);
                Assert.AreEqual(expected.GeneralEventCount, actual.GeneralEventCount);
            }
        }

        private void ResetCounters()
        {
            lock (sync)
            {
                counters = new TcpServerTestCounters();
            }
        }

        [TestMethod]
        public void TcpServer_Constructor()
        {
            IPAddress iPAddress = IPAddress.Loopback;
            int port = NetworkHelper.GetFreeTcpPort(iPAddress);
            string name = this.TestContext.TestName;
            TcpServer target = new TcpServer(name, iPAddress, port);
            try
            {
                Assert.AreEqual(name, target.ServerIdentifier);
                Assert.AreEqual(port, target.Port);
                Assert.AreEqual(iPAddress, target.IPAddress);
#if !NET40
                Assert.IsFalse(target.AcceptConnectionsAsynchronously);
#endif
            }
            finally
            {
                target.Dispose();
            }
        }

        [TestMethod]
        public void TcpServer_Port_Not_Assigned()
        {
            int port = 0;
            IPAddress iPAddress = IPAddress.Loopback;
            string name = this.TestContext.TestName;
            using (TcpServer target = new TcpServer(name, IPAddress.Loopback, port))
            {
                target.Start();
                Assert.IsTrue((target.TcpListener.LocalEndpoint as IPEndPoint).Port > 0);
                Assert.AreEqual(port, target.Port);
            }
        }

        [TestMethod]
        public void TcpServer_Port_Assigned()
        {
            IPAddress iPAddress = IPAddress.Loopback;
            int port = NetworkHelper.GetFreeTcpPort(iPAddress);
            string name = this.TestContext.TestName;
            using (TcpServer target = new TcpServer(name, IPAddress.Loopback, port))
            {
                target.Start();
                Assert.AreEqual(port, (target.TcpListener.LocalEndpoint as IPEndPoint).Port);
                Assert.AreEqual(port, target.Port);
            }
        }

        [TestMethod]
        public void TcpServer_Started_Must_Be_Called()
        {
            this.globalTarget.Start();
            TcpServerTestCounters expected = new TcpServerTestCounters();
            expected.StartedCount = 1;
            this.CompareCounters(expected, counters);
        }

        [TestMethod]
        public void TcpServer_Stopped_Must_Be_Called()
        {
            this.globalTarget.Start();
            this.ResetCounters();
            this.globalTarget.Stop();
            TcpServerTestCounters expected = new TcpServerTestCounters();
            expected.StoppedCount = 1;
            this.CompareCounters(expected, counters);
        }

        [TestMethod]
        public void TcpServer_Running()
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
        public void TcpServer_Client_Connected_Sync()
        {
#if !NET40
            this.globalTarget.AcceptConnectionsAsynchronously = false;
#endif
            TcpServer_Client_Connected();
        }

#if !NET40
        [TestMethod]
        public void TcpServer_Client_Connected_Async()
        {
            this.globalTarget.AcceptConnectionsAsynchronously = true;
            TcpServer_Client_Connected();
        }
#endif

        private void TcpServer_Client_Connected()
        {
            this.globalTarget.Start();
            this.ResetCounters();
            EndPoint remoteEndPoint = this.globalTarget.TcpListener.LocalEndpoint;

            Thread.Sleep(250);
            Socket client0 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client0.Connect(remoteEndPoint);
            Socket client1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client1.Connect(remoteEndPoint);
            Thread.Sleep(250);

            TcpServerTestCounters expected = new TcpServerTestCounters();
            expected.ClientConnectedCount = 2;
            this.CompareCounters(expected, counters);

            Assert.AreEqual(client0.LocalEndPoint, argsList[0].Client.RemoteEndPoint);
            Assert.AreEqual(client1.LocalEndPoint, argsList[1].Client.RemoteEndPoint);
        }

        [TestMethod]
        public void TcpServer_Listener_Closed_When_Disposed_Sync()
        {
            TcpServer target = new TcpServer(this.TestContext.TestName, IPAddress.Loopback, 0);
#if !NET40
            target.AcceptConnectionsAsynchronously = false;
#endif
            TcpServer_Listener_Closed_When_Disposed(target);
        }

#if !NET40
        [TestMethod]
        public void TcpServer_Listener_Closed_When_Disposed_Async()
        {
            TcpServer target = new TcpServer(this.TestContext.TestName, IPAddress.Loopback, 0);
            target.AcceptConnectionsAsynchronously = true;
            TcpServer_Listener_Closed_When_Disposed(target);
        }
#endif

        public void TcpServer_Listener_Closed_When_Disposed(TcpServer target)
        {
            target.Start();
            IPAddress iPAddress = (target.TcpListener.LocalEndpoint as IPEndPoint).Address;
            int port = (target.TcpListener.LocalEndpoint as IPEndPoint).Port;
            bool portOpen;

            portOpen = NetworkHelper.IsTcpPortInUseByListener(iPAddress, port);
            Assert.IsTrue(portOpen);
            target.Dispose();

            portOpen = NetworkHelper.IsTcpPortInUseByListener(iPAddress, port);
            Assert.IsFalse(portOpen);
        }

        [TestMethod]
        public void TcpServer_GeneralEvent_Is_Not_Raised_When_Disposing_Sync()
        {
            TcpServer target = new TcpServer(this.TestContext.TestName, IPAddress.Loopback, 0);
#if !NET40
            target.AcceptConnectionsAsynchronously = false;
#endif
            TcpServer_GeneralEvent_Is_Not_Raised_When_Disposing(target);
        }

#if !NET40
        [TestMethod]
        public void TcpServer_GeneralEvent_Is_Not_Raised_When_Disposing_Async()
        {
            TcpServer target = new TcpServer(this.TestContext.TestName, IPAddress.Loopback, 0);
            target.AcceptConnectionsAsynchronously = true;
            TcpServer_GeneralEvent_Is_Not_Raised_When_Disposing(target);
        }
#endif

        public void TcpServer_GeneralEvent_Is_Not_Raised_When_Disposing(TcpServer target)
        {
            this.SubscribeToEvents(target);
            target.Start();
            Thread.Sleep(250);
            this.ResetCounters();
            target.Dispose();
            Thread.Sleep(250);
            TcpServerTestCounters expected = new TcpServerTestCounters();
            expected.StoppedCount = 1;
            this.CompareCounters(expected, counters);
        }

        [TestMethod]
        public void TcpServer_Listener_Closed_When_Stopped_Sync()
        {
            TcpServer target = new TcpServer(this.TestContext.TestName, IPAddress.Loopback, 0);
#if !NET40
            target.AcceptConnectionsAsynchronously = false;
#endif
            TcpServer_Listener_Closed_When_Stopped(target);
        }

#if !NET40
        [TestMethod]
        public void TcpServer_Listener_Closed_When_Stopped_Async()
        {
            TcpServer target = new TcpServer(this.TestContext.TestName, IPAddress.Loopback, 0);
            target.AcceptConnectionsAsynchronously = true;
            TcpServer_Listener_Closed_When_Stopped(target);
        }
#endif

        public void TcpServer_Listener_Closed_When_Stopped(TcpServer target)
        {
            target.Start();
            IPAddress iPAddress = (target.TcpListener.LocalEndpoint as IPEndPoint).Address;
            int port = (target.TcpListener.LocalEndpoint as IPEndPoint).Port;
            bool portOpen;

            portOpen = NetworkHelper.IsTcpPortInUseByListener(iPAddress, port);
            Assert.IsTrue(portOpen);
            target.Stop();

            portOpen = NetworkHelper.IsTcpPortInUseByListener(iPAddress, port);
            Assert.IsFalse(portOpen);
            target.Dispose();
        }

        [TestMethod]
        public void TcpServer_Listener_Restarts_Sync()
        {
#if !NET40
            this.globalTarget.AcceptConnectionsAsynchronously = false;
#endif
            TcpServer_Listener_Restarts();
        }

#if !NET40
        [TestMethod]
        public void TcpServer_Listener_Restarts_Async()
        {
            this.globalTarget.AcceptConnectionsAsynchronously = true;
            TcpServer_Listener_Restarts();
        }
#endif

        public void TcpServer_Listener_Restarts()
        {
            this.globalTarget.Start();
            IPAddress iPAddress = (this.globalTarget.TcpListener.LocalEndpoint as IPEndPoint).Address;
            int port = (this.globalTarget.TcpListener.LocalEndpoint as IPEndPoint).Port;
            bool portOpen;

            portOpen = NetworkHelper.IsTcpPortInUseByListener(iPAddress, port);
            Assert.IsTrue(portOpen);
            this.globalTarget.Stop();

            portOpen = NetworkHelper.IsTcpPortInUseByListener(iPAddress, port);
            Assert.IsFalse(portOpen);

            this.globalTarget.Start();
            iPAddress = (this.globalTarget.TcpListener.LocalEndpoint as IPEndPoint).Address;
            port = (this.globalTarget.TcpListener.LocalEndpoint as IPEndPoint).Port;
            portOpen = NetworkHelper.IsTcpPortInUseByListener(iPAddress, port);
            Assert.IsTrue(portOpen);
        }

        void target_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            lock (sync)
            {
                argsList.Add(e);
                counters.ClientConnectedCount++;
            }
        }

        void target_Stopped(object sender, EventArgs e)
        {
            lock (sync)
            {
                counters.StoppedCount++;
            }
        }

        void target_Started(object sender, EventArgs e)
        {
            lock (sync)
            {
                counters.StartedCount++;
            }
        }

        void globalTarget_GeneralEvent(object sender, System.CommunicationFramework.Common.CancellableMethodManagerEventArgs e)
        {
            lock (sync)
            {
                counters.GeneralEventCount++;
            }
        }

        private void SubscribeToEvents(TcpServer target)
        {
            target.ClientConnected += target_ClientConnected;
            target.Stopped += target_Stopped;
            target.Started += target_Started;
            target.GeneralEvent += globalTarget_GeneralEvent;
        }
    }
}
