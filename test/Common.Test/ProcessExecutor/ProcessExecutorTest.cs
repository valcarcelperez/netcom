namespace Common.Test
{
    using System;
    using System.CommunicationFramework.Common;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProcessExecutorTest
    {
        private int timeout = 1000;
        private object lockObject = new object();
        private bool timedOutCallBackCalled = false;
        volatile bool processCalled = false;
        private ProcessExecutor testTarget;

        [TestInitialize]
        public void Init()
        {
            Action callBackAction = () =>
            {
                this.timedOutCallBackCalled = true;
            };

            this.testTarget = new ProcessExecutor(callBackAction, this.timeout);
        }

        [TestMethod]
        public void ProcessExecutor_Constructor()
        {
            Action callBackAction = () => { };
            ProcessExecutor target = new ProcessExecutor(callBackAction, 1000);
            Assert.IsNotNull(target);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ProcessExecutor_Constructor_Null_Parameter()
        {
            ProcessExecutor target = new ProcessExecutor(null, 1000);
        }

        [TestMethod]
        public async Task ProcessExecutor_ExecuteAsync_Process_Is_Called()
        {
            Func<Task> process = async () =>
            {
                this.processCalled = true;
                await Task.Delay(120);
            };

            await this.testTarget.ExecuteAsync(process);
            Assert.IsTrue(this.processCalled);
        }

        [TestMethod]
        public async Task ProcessExecutor_ExecuteAsync_Process_TimesOut()
        {
            Func<Task> process = async () =>
            {
                await Task.Delay(1200);
            };

            try
            {
                await this.testTarget.ExecuteAsync(process);
            }
            catch (TimeoutException)
            {
                Assert.IsTrue(this.timedOutCallBackCalled);
                return;
            }

            Assert.Fail("A Timeout exception was not raised.");
        }

        [TestMethod]
        public async Task ProcessExecutor_ExecuteAsync_With_Return_Value_Process_TimesOut()
        {
            Func<Task<int>> process = async () =>
            {
                await Task.Delay(1200);
                return 10;
            };

            int actual;
            try
            {
                actual = await this.testTarget.ExecuteAsync(process);
            }
            catch (TimeoutException)
            {
                Assert.IsTrue(this.timedOutCallBackCalled);
                return;
            }

            Assert.Fail("A Timeout exception was not raised.");
        }

        [TestMethod]
        public async Task ProcessExecutor_ExecuteAsync_Exeption_In_The_Process()
        {
            this.timeout = int.MaxValue;

            Exception expectedException = new Exception("exception test");
            Func<Task> process = async () =>
            {
                await Task.Delay(200);
                throw expectedException;
            };

            try
            {
                await this.testTarget.ExecuteAsync(process);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(expectedException, ex);
                Assert.IsFalse(this.timedOutCallBackCalled);
                return;
            }

            Assert.Fail("An exception was not raised.");
        }

        [TestMethod]
        public async Task ProcessExecutor_ExecuteAsync_With_Return_Value_Exeption_In_The_Process()
        {
            this.timeout = int.MaxValue;

            Exception expectedException = new Exception("exception test");
            Func<Task<int>> process = async () =>
            {
                await Task.Delay(200);
                throw expectedException;
            };

            try
            {
                await this.testTarget.ExecuteAsync(process);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(expectedException, ex);
                Assert.IsFalse(this.timedOutCallBackCalled);
                return;
            }

            Assert.Fail("An exception was not raised.");
        }

        [TestMethod]
        public async Task ProcessExecutor_ExecuteAsync_Process_Completes_On_Time()
        {
            Func<Task> process = async () =>
            {
                await Task.Delay(200);
            };

            await this.testTarget.ExecuteAsync(process);
            Assert.IsFalse(this.timedOutCallBackCalled);
        }

        [TestMethod]
        public async Task ProcessExecutor_ExecuteAsync_With_Return_Value_Process_Completes_On_Time()
        {
            Func<Task<int>> process = async () =>
            {
                await Task.Delay(200);
                return 10;
            };

            var actual = await this.testTarget.ExecuteAsync(process);
            Assert.IsFalse(this.timedOutCallBackCalled);
        }

        [TestMethod]
        public async Task ProcessExecutor_ExecuteAsync_With_Return_Value_Value_Is_Returned()
        {
            Func<Task<int>> process = async () =>
            {
                await Task.Delay(200);
                return 10;
            };

            var actual = await this.testTarget.ExecuteAsync(process);
            Assert.AreEqual(10, actual);
        }
    }
}
