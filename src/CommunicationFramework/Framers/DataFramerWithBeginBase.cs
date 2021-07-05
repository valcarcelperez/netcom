namespace System.CommunicationFramework.Interfaces
{
    using System.CommunicationFramework.Common;
    using System.Text;

    /// <summary>
    /// Base class used for implementing framer that uses a frame begin indicator.
    /// </summary>
    public abstract class DataFramerWithBeginBase : IDataFramer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataFramerWithBeginBase" /> class.
        /// </summary>
        /// <param name="receivedDataBufferSize">Size of the buffer used when storing data that is being framed.</param>
        /// <param name="frameBeginIndicator">Bytes that represents the frame begin.</param>
        protected DataFramerWithBeginBase(int receivedDataBufferSize, byte[] frameBeginIndicator)
        {
            Throw.ThrowIfNull(frameBeginIndicator, "frameBeginIndicator");
            this.Buffer = new byte[receivedDataBufferSize];
            this.PatternMatcherFrameBegin = new PatternMatcher(frameBeginIndicator);
        }

        /// <summary>
        /// Gets or sets the size of the received data.
        /// </summary>
        public int ReceivedDataSize { get; protected set; }

        /// <summary>
        /// Gets the buffer where the received data is being stored.
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// Gets or sets Pattern matcher to detect the the frame begin.
        /// </summary>
        protected PatternMatcher PatternMatcherFrameBegin { get; set; }

        /// <summary>
        /// Process received data.
        /// </summary>
        /// <param name="buffer">Buffer that contains the data being processed.</param>
        /// <param name="index">Beginning of the data in the buffer.</param>
        /// <param name="size">Size of the data in the buffer.</param>
        /// <returns>True if a frame is completed.</returns>        
        public abstract bool FrameReceivedData(byte[] buffer, ref int index, int size);

        /// <summary>
        /// Resets the framer.
        /// </summary>
        public abstract void Reset();
    }
}
