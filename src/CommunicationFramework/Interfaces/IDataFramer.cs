namespace System.CommunicationFramework.Interfaces
{
    /// <summary>
    /// Defines a Message Framer.
    /// </summary>
    public interface IDataFramer
    {
        /// <summary>
        /// Gets the size of the received data.
        /// </summary>
        int ReceivedDataSize { get; }

        /// <summary>
        /// Gets the buffer with the received data.
        /// </summary>
        byte[] Buffer { get; }

        /// <summary>
        /// Adds received data to the internal buffer.
        /// </summary>
        /// <param name="buffer">Received data.</param>
        /// <param name="index">Index where the data begins in the buffer.</param>
        /// <param name="size">Size of the received data.</param>
        /// <returns>True when the framer completed a message.</returns>
        bool FrameReceivedData(byte[] buffer, ref int index, int size);

        /// <summary>
        /// Removes all received data from the Framer buffer.
        /// </summary>
        void Reset();
    }
}
