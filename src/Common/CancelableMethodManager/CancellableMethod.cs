namespace System.CommunicationFramework.Common
{
    using System.Threading;

    /// <summary>
    /// Delegate that defines a cancellable method.
    /// </summary>
    /// <param name="cancellationToken">CancellationToken to cancel the process.</param>
    public delegate void CancellableMethod(CancellationToken cancellationToken);
}
