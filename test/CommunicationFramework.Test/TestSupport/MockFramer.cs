namespace System.Communication.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.CommunicationFramework.Interfaces;

    public struct MockFramerCounters
    {
        public int FrameDataToSendCount;
        public int FrameReceivedDataCount;
        public int ResetCount;
        public int BufferCount;
    }

    /// <summary>
    /// Defines a MessageFramer used for testing.
    /// </summary>
    public class MockMessageFramer : MockBase, IDataFramer
    {
        public const byte Stx = 2;
        public const byte Etx = 3;

        private byte[] buffer;

        public MockMessageFramer(int receivedDataBufferSize)
        {
            buffer = new byte[receivedDataBufferSize];
        }

        #region IDataFramer

        public int ReceivedDataSize { get; private set; }

        public byte[] Buffer 
        { 
            get
            {
                ActualCounters.BufferCount++;
                return this.buffer;
            }           
        }

        public bool FrameReceivedData(byte[] buffer, ref int index, int size)
        {
            ActualCounters.FrameReceivedDataCount++;
            ActualParameters.Parameter1 = buffer;
            ActualParameters.Parameter2 = index;
            ActualParameters.Parameter3 = size;

            Array.Copy(buffer, 0, Buffer, ReceivedDataSize, size);
            ReceivedDataSize += size;
            return Buffer[0] == Stx && Buffer[ReceivedDataSize - 1] == Etx;
        }

        public void Reset()
        {
            ActualCounters.ResetCount++;
            ReceivedDataSize = 0;
        }

        #endregion

        public MockFramerCounters ActualCounters;

        public MockFramerCounters ExpectedCounters;

        public void CompareCounters()
        {
            Assert.AreEqual(this.ExpectedCounters.FrameDataToSendCount, this.ActualCounters.FrameDataToSendCount);
            Assert.AreEqual(this.ExpectedCounters.FrameReceivedDataCount, this.ActualCounters.FrameReceivedDataCount);
            Assert.AreEqual(this.ExpectedCounters.ResetCount, this.ActualCounters.ResetCount);
        }
    }
}
