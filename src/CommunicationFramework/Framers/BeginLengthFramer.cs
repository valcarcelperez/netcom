namespace System.CommunicationFramework.Interfaces
{
    using System.Text;

    /// <summary>
    /// Implementation of an IDataFramer where the frame has the format [Begin][two bytes indicating DataLength][Data].
    /// </summary>
    public class BeginLengthFramer : DataFramerWithBeginBase
    {
        /// <summary>
        /// State of the framer.
        /// </summary>
        private BeginLengthFramerState state;

        /// <summary>
        /// The length of the frame-begin and data-length together. [Begin][DataLength].
        /// </summary>
        private int beginAndDataLengthSize;

        /// <summary>
        /// The total length of the frame.
        /// This value is calculated after the data-length is received.
        /// </summary>
        private int frameLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeginLengthFramer" /> class.
        /// </summary>
        /// <param name="receivedDataBufferSize">Size of the buffer used when storing data that is being framed.</param>
        /// <param name="frameBeginIndicator">Bytes that represents the frame begin.</param>
        public BeginLengthFramer(int receivedDataBufferSize, byte[] frameBeginIndicator)
            : base(receivedDataBufferSize, frameBeginIndicator)
        {
            this.beginAndDataLengthSize = frameBeginIndicator.Length + 2;
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BeginLengthFramer" /> class.
        /// </summary>
        /// <param name="receivedDataBufferSize">Size of the buffer used when storing data that is being framed.</param>
        /// <param name="frameBeginIndicator">Bytes that represents the frame begin.</param>
        public BeginLengthFramer(int receivedDataBufferSize, string frameBeginIndicator)
            : this(receivedDataBufferSize, Encoding.ASCII.GetBytes(frameBeginIndicator))
        {
        }

        /// <summary>
        /// Process received data.
        /// </summary>
        /// <param name="buffer">Buffer that contains the data being processed.</param>
        /// <param name="index">Beginning of the data in the buffer.</param>
        /// <param name="size">Size of the data in the buffer.</param>
        /// <returns>True if a frame is completed.</returns>        
        public override bool FrameReceivedData(byte[] buffer, ref int index, int size)
        {
            int count = 0;
            while (count < size)
            {
                switch (this.state)
                {
                    case BeginLengthFramerState.ReceivingFrameBegin:
                        bool frameBeginCompleted = this.PatternMatcherFrameBegin.NewByte(buffer[index]);
                        if (frameBeginCompleted)
                        {
                            Array.Copy(this.PatternMatcherFrameBegin.Pattern, 0, this.Buffer, 0, this.PatternMatcherFrameBegin.Pattern.Length);
                            this.ReceivedDataSize = this.PatternMatcherFrameBegin.Pattern.Length;
                            this.state = BeginLengthFramerState.ReceivingDataLength;
                        }

                        break;

                    case BeginLengthFramerState.ReceivingDataLength:
                        this.Buffer[this.ReceivedDataSize] = buffer[index];
                        this.ReceivedDataSize++;
                        bool beginAndDataLengthReceived = this.ReceivedDataSize == this.beginAndDataLengthSize;
                        if (beginAndDataLengthReceived)
                        {
                            ushort dataLength = BitConverter.ToUInt16(this.Buffer, this.PatternMatcherFrameBegin.Pattern.Length);
                            if (dataLength == 0)
                            {
                                index++;
                                this.state = BeginLengthFramerState.ReceivingFrameBegin;
                                return true;
                            }

                            this.frameLength = this.PatternMatcherFrameBegin.Pattern.Length + 2 + dataLength;
                            this.state = BeginLengthFramerState.ReceivingData;
                        }

                        break;

                    case BeginLengthFramerState.ReceivingData:
                        this.Buffer[this.ReceivedDataSize] = buffer[index];
                        this.ReceivedDataSize++;
                        bool allDataReceived = this.ReceivedDataSize == this.frameLength;
                        if (allDataReceived)
                        {
                            index++;
                            this.state = BeginLengthFramerState.ReceivingFrameBegin;
                            return true;
                        }

                        break;
                }

                index++;
                count++;
            }

            return false;
        }

        /// <summary>
        /// Resets the framer.
        /// </summary>
        public override void Reset()
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes the framer.
        /// </summary>
        private void Initialize()
        {
            this.ReceivedDataSize = 0;
            this.PatternMatcherFrameBegin.Reset();
            this.state = BeginLengthFramerState.ReceivingFrameBegin;
        }
    }
}
