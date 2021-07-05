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
    public class BeginLengthFramerTest
    {
        private byte[] frameBeginIndicator = Encoding.ASCII.GetBytes("ZCZC");
        private byte[] buffer;

        private byte[] GetBuffer()
        {
            string data = "some data.";
            byte[] result = new byte[1024];
            Array.Copy(frameBeginIndicator, 0, result, 0, frameBeginIndicator.Length);
            byte[] dataLength = BitConverter.GetBytes((ushort)data.Length);
            Array.Copy(dataLength, 0, result, frameBeginIndicator.Length, 2);
            byte[] dataBytes = Encoding.ASCII.GetBytes(data);
            Array.Copy(dataBytes, 0, result, frameBeginIndicator.Length + 2, dataBytes.Length);
            return result;
        }

        private BeginLengthFramer GetTarget()
        {
            BeginLengthFramer targer = new BeginLengthFramer(1024, frameBeginIndicator);
            return targer;
        }

        [TestInitialize]
        public void TestInitializer()
        {
            buffer = GetBuffer();
        }

        [TestMethod]
        public void BeginLengthFramer_Constructor()
        {
            BeginLengthFramer target = new BeginLengthFramer(1024, frameBeginIndicator);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BeginLengthFramer_Constructor_FrameBeginIndicator_Cannot_Be_Null()
        {
            byte[] bytes = null;
            BeginLengthFramer target = new BeginLengthFramer(1024, bytes);
        }

        [TestMethod]
        public void BeginLengthFramer_FrameReceivedData_Frame_With_Begin_DataLength_And_Data()
        {
            BeginLengthFramer target = GetTarget();
            int index = 0;
            bool actual = target.FrameReceivedData(buffer, ref index, 16);
            Assert.IsTrue(actual);
            Assert.AreEqual(16, index);
            Assert.AreEqual(16, target.ReceivedDataSize);
            TestHelper.CompareArrays(buffer, target.Buffer, target.ReceivedDataSize);
        }

        [TestMethod]
        public void BeginLengthFramer_FrameReceivedData_Three_Parts()
        {
            BeginLengthFramer target = GetTarget();
            int index = 0;
            bool actual = target.FrameReceivedData(buffer, ref index, 3);
            Assert.IsFalse(actual);
            Assert.AreEqual(3, index);

            actual = target.FrameReceivedData(buffer, ref index, 5);
            Assert.IsFalse(actual);
            Assert.AreEqual(8, index);

            actual = target.FrameReceivedData(buffer, ref index, 15);
            Assert.IsTrue(actual);
            Assert.AreEqual(16, index);

            TestHelper.CompareArrays(buffer, target.Buffer, target.ReceivedDataSize);
        }
    }
}
