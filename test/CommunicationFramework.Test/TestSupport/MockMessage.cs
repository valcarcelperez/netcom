using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace System.Communication.UnitTest
{
    public class MockMessage
    {
        public bool Field1 { get; set; }
        public string Field2 { get; set; }

        public static MockMessage GetTestMessage01()
        {
            MockMessage message = new MockMessage();
            message.Field1 = true;
            message.Field2 = "aaaaaaaaaabbbbbbbbbbccccccccccdddddddddd";
            return message;
        }

        public static void CompareMessages(MockMessage expected, MockMessage actual)
        {
            Assert.AreEqual(expected.Field1, actual.Field1);
            Assert.AreEqual(expected.Field2, actual.Field2);
        }
    }
}
