namespace System.CommunicationFramework.Interfaces
{
    /// <summary>
    /// Define the state of a BeginLengthFramer.
    /// </summary>
    public enum BeginLengthFramerState
    {
        /// <summary>
        /// Indicates that the framer is receiving the frame begin.
        /// </summary>
        ReceivingFrameBegin,

        /// <summary>
        /// Indicates that the framer is receiving the data length.
        /// </summary>
        ReceivingDataLength,

        /// <summary>
        /// Indicates that the framer is receiving the data.
        /// </summary>
        ReceivingData
    }
}
