namespace System.Communication.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.CommunicationFramework.Interfaces;
    using System.Text;

    public struct MockEncoderCounters
    {
        public int EncodeMessageCount;
        public int DecodeMessageCount;
    }

    /// <summary>
    /// Implements a message encoder used by the test.
    /// </summary>
    public class MockEncoder : MockBase, IMessageEncoder<MockMessage>
    {
        public MockEncoderCounters ActualCounters;

        public MockEncoderCounters ExpectedCounters;

        public int EncodeMessage(MockMessage message, byte[] buffer, int index)
        {
            ActualCounters.EncodeMessageCount++;
            ActualParameters.Parameter1 = message;
            ActualParameters.Parameter2 = buffer;
            ActualParameters.Parameter3 = index;

            buffer[index] = MockMessageFramer.Stx;
            index++;

            string line = string.Format("{0},{1}", Convert.ToByte(message.Field1), message.Field2);
            byte[] data = Encoding.ASCII.GetBytes(line);
            Buffer.BlockCopy(data, 0, buffer, index, data.Length);
            index += data.Length;
            buffer[index] = MockMessageFramer.Etx;
            index++;
            return index;
        }

        public MockMessage DecodeMessage(byte[] buffer, int index, int size)
        {
            ActualCounters.DecodeMessageCount++;
            ActualParameters.Parameter1 = buffer;
            ActualParameters.Parameter2 = index;
            ActualParameters.Parameter3 = size;

            if (buffer[index] != MockMessageFramer.Stx
                || buffer[index + 2] != (byte)','
                || buffer[index + size - 1] != MockMessageFramer.Etx)
            {
                throw new Exception(string.Format("Invalid format. Received Data: {0}", Encoding.ASCII.GetString(buffer, index, size)));
            }

            index++;
            MockMessage mssage = new MockMessage();
            mssage.Field1 = Convert.ToBoolean(buffer[index]);
            index += 2;
            mssage.Field2 = Encoding.ASCII.GetString(buffer, index, size - 4);

            return mssage;         
        }

        public void CompareCounters()
        {
            Assert.AreEqual(ExpectedCounters.DecodeMessageCount, ActualCounters.DecodeMessageCount);
            Assert.AreEqual(ExpectedCounters.EncodeMessageCount, ActualCounters.EncodeMessageCount);
        }
    }
}
