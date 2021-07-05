namespace System.CommunicationFramework.Interfaces
{
    /// <summary>
    /// Defines a generic message encoder.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    public interface IMessageEncoder<T>
    {
        /// <summary>
        /// Encode a message into a buffer.
        /// </summary>
        /// <param name="message">Message to be encoded.</param>
        /// <param name="buffer">Buffer that will contain the encoded message.</param>
        /// <param name="index">The index where the data will begin.</param>
        /// <returns>The size of the data in buffer.</returns>
        int EncodeMessage(T message, byte[] buffer, int index);

        /// <summary>
        /// Decodes a message from a buffer.
        /// </summary>
        /// <param name="buffer">Buffer with the data.</param>
        /// <param name="index">Index of the message in the buffer.</param>
        /// <param name="size">Size of the data in the buffer.</param>
        /// <returns>An object of type T.</returns>
        T DecodeMessage(byte[] buffer, int index, int size);
    }
}
