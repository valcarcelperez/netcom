using System;
using System.CommunicationFramework.Interfaces;
using System.Text;

namespace SendingText.Common
{
    public class TextMessageEncoder : IMessageEncoder<string>
    {
        public static byte[] StartOfText = new byte[] { 2 };
        public static byte[] EndOfText = new byte[] { 3 };
        private static int minMessageSize = StartOfText.Length + EndOfText.Length;

        public int EncodeMessage(string message, byte[] buffer, int index)
        {
            int initialIndex = index;
            byte[] data = Encoding.ASCII.GetBytes(message);

            Array.Copy(StartOfText, 0, buffer, index, StartOfText.Length);
            index += StartOfText.Length;

            Array.Copy(data, 0, buffer, index, data.Length);
            index += data.Length;
            
            Array.Copy(EndOfText, 0, buffer, index, EndOfText.Length);
            index += EndOfText.Length;
            
            return index - initialIndex;
        }

        public string DecodeMessage(byte[] buffer, int index, int size)
        {
            if (size < minMessageSize)
            {
                throw new FormatException("Invalid format. Frame is too short.");
            }

            byte[] receivedStartOfText = new byte[StartOfText.Length];
            Array.Copy(buffer, index, receivedStartOfText, 0, StartOfText.Length);

            bool expectedStartOfTextReceived = CompareArray(StartOfText, receivedStartOfText, StartOfText.Length);
            if (!expectedStartOfTextReceived)
            {
                throw new FormatException("Invalid format. Invalid Frame Begin");
            }

            byte[] receivedEndOfText = new byte[EndOfText.Length];
            Array.Copy(buffer, index + size - EndOfText.Length, receivedEndOfText, 0, EndOfText.Length);

            bool expectedEndOfTextReceived = CompareArray(EndOfText, receivedEndOfText, EndOfText.Length);
            if (!expectedEndOfTextReceived)
            {
                throw new FormatException("Invalid format. Invalid Frame End");
            }

            string result = Encoding.ASCII.GetString(buffer, index + StartOfText.Length, size - StartOfText.Length - EndOfText.Length);
            return result;
        }

        private bool CompareArray(byte[] a, byte[] b, int size)
        {
            for (int i = 0; i < size; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
