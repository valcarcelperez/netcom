namespace System.Communication.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class MockParameters
    {
        public object Parameter1;
        public object Parameter2;
        public object Parameter3;
        public object Parameter4;
        public object Parameter5;
        public object Parameter6;

        public static void CompareParameters(MockParameters expectedParameters, MockParameters actualParameters)
        {
            Assert.AreEqual(expectedParameters.Parameter1, actualParameters.Parameter1);
            Assert.AreEqual(expectedParameters.Parameter2, actualParameters.Parameter2);
            Assert.AreEqual(expectedParameters.Parameter3, actualParameters.Parameter3);
            Assert.AreEqual(expectedParameters.Parameter4, actualParameters.Parameter4);
            Assert.AreEqual(expectedParameters.Parameter5, actualParameters.Parameter5);
            Assert.AreEqual(expectedParameters.Parameter6, actualParameters.Parameter6);
        }
    }
}
