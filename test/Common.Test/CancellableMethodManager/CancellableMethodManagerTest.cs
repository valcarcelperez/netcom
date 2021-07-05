namespace Common.Test
{
    using System;
    using System.CommunicationFramework.Common;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public struct CancellableMethodManagerTestCounters
    {
        public int ProcessStartedCount;
        public int ProcessStopedCount;
        public int BeforeStartEventCount;
        public int BeforeStopEventCount;
        public int ProcessStartedEventCount;
        public int ProcessStoppedEventCount;
        public int CancellableProcessEventCount;
    }

    [TestClass]
    public class CancellableMethodManagerTest
    {
        private const string PROCESS = "process 01";
        private CancellableMethodManagerTestCounters counters;
        private bool stopTimeoutTest = false;
        private object sync = new object();

        private void CompareCounters(CancellableMethodManagerTestCounters expected, CancellableMethodManagerTestCounters actual)
        {
            lock (this.sync)
            {
                Assert.AreEqual(expected.BeforeStartEventCount, actual.BeforeStartEventCount, "BeforeStartEventCount");
                Assert.AreEqual(expected.BeforeStopEventCount, actual.BeforeStopEventCount, "BeforeStopEventCount");
                Assert.AreEqual(expected.CancellableProcessEventCount, actual.CancellableProcessEventCount, "CancellableProcessEventCount");
                Assert.AreEqual(expected.ProcessStartedCount, actual.ProcessStartedCount, "ProcessStartedCount");
                Assert.AreEqual(expected.ProcessStartedEventCount, actual.ProcessStartedEventCount, "ProcessStartedEventCount");
                Assert.AreEqual(expected.ProcessStopedCount, actual.ProcessStopedCount, "ProcessStopedCount");
                Assert.AreEqual(expected.ProcessStoppedEventCount, actual.ProcessStoppedEventCount, "ProcessStopedEventCount");
            }
        }

        [TestInitialize]
        public void ResetCounters()
        {
            this.counters = new CancellableMethodManagerTestCounters();
            this.stopTimeoutTest = false;
        }

        private CancellableMethodManager GetTarget()
        {
            CancellableMethodManager target = new CancellableMethodManager(this.Process, PROCESS);
            return target;
        }

        private void Process(CancellationToken cancellationToken)
        {
            lock (this.sync)
            {
                this.counters.ProcessStartedCount++;
            }

            while (!cancellationToken.IsCancellationRequested || this.stopTimeoutTest)
            {
                Thread.Sleep(10);
            }

            lock (this.sync)
            {
                this.counters.ProcessStopedCount++;
            }
        }

        [TestMethod]
        public void CancellableMethodManager_Constructor()
        {
            CancellableMethodManager target = new CancellableMethodManager(this.Process, PROCESS);
            Assert.IsNotNull(target);
            target.Dispose();
        }

        [TestMethod]
        public void CancellableMethodManager_ProcessName()
        {
            using (CancellableMethodManager target = new CancellableMethodManager(this.Process, PROCESS))
            {
                string expected = PROCESS;
                string actual = target.ProcessName;
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void CancellableMethodManager_Start()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                target.Start();
                Thread.Sleep(250);

                CancellableMethodManagerTestCounters expected = new CancellableMethodManagerTestCounters();
                expected.ProcessStartedCount = 1;
                this.CompareCounters(expected, this.counters);
            }
        }

        [TestMethod]
        public void CancellableMethodManager_Stop()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                target.Start();
                Thread.Sleep(250);
                this.ResetCounters();
                target.Stop();

                CancellableMethodManagerTestCounters expected = new CancellableMethodManagerTestCounters();
                expected.ProcessStopedCount = 1;
                this.CompareCounters(expected, this.counters);
            }        
        }

        [TestMethod]
        public void CancellableMethodManager_Running()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                bool expected = false;
                Assert.AreEqual(expected, target.Running);

                target.Start();
                Thread.Sleep(250);
                expected = true;
                Assert.AreEqual(expected, target.Running);

                target.Stop();
                expected = false;
                Assert.AreEqual(expected, target.Running);
            }
        }

        [TestMethod]
        public void CancellableMethodManager_BeforeStartEvent()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                target.BeforeStart += this.BeforeStart;
                target.Start();
                CancellableMethodManagerTestCounters expected = new CancellableMethodManagerTestCounters();
                expected.BeforeStartEventCount = 1;
                this.CompareCounters(expected, this.counters);
            }
        }

        [TestMethod]
        public void CancellableMethodManager_BeforeStopEvent()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                target.BeforeStop += this.BeforeStop;
                target.Start();
                Thread.Sleep(250);
                this.ResetCounters();
                target.Stop();

                CancellableMethodManagerTestCounters expected = new CancellableMethodManagerTestCounters();
                expected.BeforeStopEventCount = 1;
                expected.ProcessStopedCount = 1;
                this.CompareCounters(expected, this.counters);
            }
        }

        [TestMethod]
        public void CancellableMethodManager_ProcessStartedEvent()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                target.ProcessStarted += this.ProcessStarted;
                target.Start();
                Thread.Sleep(250);

                CancellableMethodManagerTestCounters expected = new CancellableMethodManagerTestCounters();
                expected.ProcessStartedEventCount = 1;
                expected.ProcessStartedCount = 1;
                this.CompareCounters(expected, this.counters);
            }
        }

        [TestMethod]
        public void CancellableMethodManager_ProcessStopEvent()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                target.ProcessStopped += this.ProcessStopped;
                target.Start();
                Thread.Sleep(250);
                this.ResetCounters();
                target.Stop();

                CancellableMethodManagerTestCounters expected = new CancellableMethodManagerTestCounters();
                expected.ProcessStoppedEventCount = 1;
                expected.ProcessStopedCount = 1;
                this.CompareCounters(expected, this.counters);
            }
        }

        [TestMethod]
        public void CancellableMethodManager_StopTimeout()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                target.CancellableMethodMenagerEvent += this.CancellableMethodManagerEvent;
                target.Start();
                Thread.Sleep(250);
                this.ResetCounters();
                this.stopTimeoutTest = true;
                target.Stop(1500);

                CancellableMethodManagerTestCounters expected = new CancellableMethodManagerTestCounters();
                expected.CancellableProcessEventCount = 1;
                this.CompareCounters(expected, this.counters);
            }
        }

        [TestMethod]
        public void CancellableMethodManager_Stop_Called_On_Dispose()
        {
            CancellableMethodManager target = this.GetTarget();
            target.Start();
            Thread.Sleep(250);
            this.ResetCounters();
            
            target.Dispose();
            CancellableMethodManagerTestCounters expected = new CancellableMethodManagerTestCounters();
            expected.ProcessStopedCount = 1;
            this.CompareCounters(expected, this.counters);
        }

        [TestMethod]
        public void CancellableMethodManager_Implements_IDisposable()
        {
            using (CancellableMethodManager target = this.GetTarget())
            {
                bool implementsIDisposable = target is IDisposable;
                Assert.IsTrue(implementsIDisposable);
            }            
        }

        private void CancellableMethodManagerEvent(object sender, CancellableMethodManagerEventArgs e)
        {
            lock (this.sync)
            {
                this.counters.CancellableProcessEventCount++;
            }
        }

        private void ProcessStopped(object sender, EventArgs e)
        {
            lock (this.sync)
            {
                this.counters.ProcessStoppedEventCount++;
            }
        }

        private void ProcessStarted(object sender, EventArgs e)
        {
            lock (this.sync)
            {
                this.counters.ProcessStartedEventCount++;
            }
        }

        private void BeforeStop(object sender, EventArgs e)
        {
            lock (this.sync)
            {
                this.counters.BeforeStopEventCount++;
            }
        }

        private void BeforeStart(object sender, EventArgs e)
        {
            lock (this.sync)
            {
                this.counters.BeforeStartEventCount++;
            }
        }
    }
}
