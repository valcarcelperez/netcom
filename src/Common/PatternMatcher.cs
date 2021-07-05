namespace System.CommunicationFramework.Common
{
    /// <summary>
    /// Validates if a sequence of bytes matches a pattern.
    /// </summary>
    public class PatternMatcher
    {
        /// <summary>
        /// Number of found bytes in the pattern.
        /// </summary>
        private int index;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternMatcher" /> class.
        /// </summary>
        /// <param name="pattern">A byte array that represent the pattern to be found.</param>
        public PatternMatcher(byte[] pattern)
        {
            this.Pattern = pattern;
        }

        /// <summary>
        /// Gets the pattern in used.
        /// </summary>
        public byte[] Pattern { get; private set; }

        /// <summary>
        /// Reset the matcher.
        /// </summary>
        public void Reset()
        {
            this.index = 0;
        }

        /// <summary>
        /// Compare a new byte with the pattern using the current index.
        /// </summary>
        /// <param name="b">A byte to be compared.</param>
        /// <returns>True if b is the the last byte of a sequence of bytes that matches the pattern.</returns>
        public bool NewByte(byte b)
        {
            if (this.Pattern[this.index] == b)
            {
                this.index++; // the byte matches the current index.
            }
            else
            {
                this.index = 0; // the byte does not match the current index, reset the index to start matching the sequence from the beginning of the pattern.
            }

            bool completed = this.index == this.Pattern.Length;
            if (completed)
            {
                this.index = 0;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
