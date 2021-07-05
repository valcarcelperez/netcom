using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommunicationFramework
{
    public static class TestHelper
    {
        public static void CompareArrays(byte[] a, byte[] b, int size)
        {
            for (int i = 0; i < size; i++)
            {
                Assert.AreEqual(a[i], b[i]);
            }
        }
    }
}
