namespace System.CommunicationFramework.Common
{
    using System;

    /// <summary>
    /// Helper class to validate some condition and throw an exception.
    /// </summary>
    public class Throw
    {
        /// <summary>
        /// Throws an exception when parameter is null.
        /// </summary>
        /// <param name="parameter">An object to be validated.</param>
        /// <param name="description">Description included in the exception when is an exception is raised.</param>
        public static void ThrowIfNull(object parameter, string description)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(description);
            }
        }
    }
}
