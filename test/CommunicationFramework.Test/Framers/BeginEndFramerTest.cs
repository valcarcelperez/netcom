using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Communication;
using System.CommunicationFramework.Interfaces;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationFramework
{
    [TestClass]
    public class BeginEndFramerTest
    {
        private byte[] frameBeginIndicator = Encoding.ASCII.GetBytes("ZCZC");
        private byte[] frameEndIndicator = Encoding.ASCII.GetBytes("NNNN");
        private byte[] buffer = Encoding.ASCII.GetBytes("ZCZCsome dataNNNNmore bytes");

        private BeginEndFramer GetTarget()
        {
            BeginEndFramer target = new BeginEndFramer(1024, frameBeginIndicator, frameEndIndicator);
            return target;
        }

        [TestMethod]
        public void BeginEndFramer_Constructor()
        {
            BeginEndFramer target = new BeginEndFramer(1024, frameBeginIndicator, frameEndIndicator);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BeginEndFramer_Constructor_FrameBeginIndicator_Cannot_Be_Null()
        {
            BeginEndFramer target = new BeginEndFramer(1024, null, frameEndIndicator);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BeginEndFramer_Constructor_FrameEndIndicator_Cannot_Be_Null()
        {
            BeginEndFramer target = new BeginEndFramer(1024, frameBeginIndicator, null);
        }

        [TestMethod]
        public void BeginEndFramer_FrameReceivedData_Frame_With_Begin_And_End()
        {
            BeginEndFramer target = GetTarget();            
            int index = 0;
            bool actual = target.FrameReceivedData(buffer, ref index, 17);
            Assert.IsTrue(actual);
            Assert.AreEqual(17, index);
            Assert.AreEqual(17, target.ReceivedDataSize);
            TestHelper.CompareArrays(buffer, target.Buffer, target.ReceivedDataSize);
        }

        [TestMethod]
        public void BeginEndFramer_FrameReceivedData_Frame_With_Begin_End_And_More_Data()
        {
            BeginEndFramer target = GetTarget();
            int index = 0;
            bool actual = target.FrameReceivedData(buffer, ref index, buffer.Length);
            Assert.IsTrue(actual);
            Assert.AreEqual(17, index);
            Assert.AreEqual(17, target.ReceivedDataSize);
            TestHelper.CompareArrays(buffer, target.Buffer, target.ReceivedDataSize);
        }

        [TestMethod]
        public void BeginEndFramer_FrameReceivedData_Three_Parts()
        {
            BeginEndFramer target = GetTarget();
            int index = 0;
            bool actual = target.FrameReceivedData(buffer, ref index, 3);
            Assert.IsFalse(actual);
            Assert.AreEqual(3, index);

            actual = target.FrameReceivedData(buffer, ref index, 5);
            Assert.IsFalse(actual);
            Assert.AreEqual(8, index);

            actual = target.FrameReceivedData(buffer, ref index, 15);
            Assert.IsTrue(actual);
            Assert.AreEqual(17, index);
            
            TestHelper.CompareArrays(buffer, target.Buffer, target.ReceivedDataSize);
        }
    }
}
