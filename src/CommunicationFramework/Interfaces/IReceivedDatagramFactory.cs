namespace System.CommunicationFramework.Interfaces
{
    /// <summary>
    /// Defines a factory of ReceivedDatagramInfo. 
    /// </summary>
    public interface IReceivedDatagramFactory
    {
        /// <summary>
        /// Returns a ReceivedDatagram.
        /// </summary>
        /// <returns>A ReceivedDatagram.</returns>
        ReceivedDatagram GetReceivedDatagram();
    }
}
