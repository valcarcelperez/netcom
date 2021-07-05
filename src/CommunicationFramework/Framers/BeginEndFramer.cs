namespace System.CommunicationFramework.Interfaces
{
    using System.CommunicationFramework.Common;
    using System.Text;

    /// <summary>
    /// Implementation of an IDataFramer where the frame has the format [Begin][Data][End].
    /// </summary>
    public class BeginEndFramer : DataFramerWithBeginBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the framer is receiving the frame begin.
        /// </summary>
        private bool receivingFrameBegin;

        /// <summary>
        /// Pattern matcher to detect the the frame end.
        /// </summary>
        private PatternMatcher patternMatcherFrameEnd;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeginEndFramer" /> class.
        /// </summary>
        /// <param name="receivedDataBufferSize">Size of the buffer used when storing data that is being framed.</param>
        /// <param name="frameBeginIndicator">Bytes that represents the frame begin.</param>
        /// <param name="frameEndIndicator">Bytes that represents the frame end.</param>
        public BeginEndFramer(int receivedDataBufferSize, byte[] frameBeginIndicator, byte[] frameEndIndicator)
            : base(receivedDataBufferSize, frameBeginIndicator)
        {
            Throw.ThrowIfNull(frameEndIndicator, "frameEndIndicator");
            this.patternMatcherFrameEnd = new PatternMatcher(frameEndIndicator);
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BeginEndFramer" /> class.
        /// </summary>
        /// <param name="receivedDataBufferSize">Size of the buffer used when storing data that is being framed.</param>
        /// <param name="frameBeginIndicator">Bytes that represents the frame begin.</param>
        /// <param name="frameEndIndicator">Bytes that represents the frame end.</param>
        public BeginEndFramer(int receivedDataBufferSize, string frameBeginIndicator, string frameEndIndicator)
            : this(receivedDataBufferSize, Encoding.ASCII.GetBytes(frameBeginIndicator), Encoding.ASCII.GetBytes(frameEndIndicator))
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
                if (this.receivingFrameBegin)
                {
                    bool frameBeginCompleted = this.PatternMatcherFrameBegin.NewByte(buffer[index]);
                    if (frameBeginCompleted)
                    {
                        Array.Copy(this.PatternMatcherFrameBegin.Pattern, 0, this.Buffer, 0, this.PatternMatcherFrameBegin.Pattern.Length);
                        this.ReceivedDataSize = this.PatternMatcherFrameBegin.Pattern.Length;
                        this.receivingFrameBegin = false;
                    }
                }
                else
                {
                    this.Buffer[this.ReceivedDataSize] = buffer[index];
                    this.ReceivedDataSize++;
                    bool frameEndCompleted = this.patternMatcherFrameEnd.NewByte(buffer[index]);
                    if (frameEndCompleted)
                    {
                        index++;
                        this.receivingFrameBegin = true;
                        return true;
                    }
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
            this.patternMatcherFrameEnd.Reset();
            this.receivingFrameBegin = true;
        }
    }
}
