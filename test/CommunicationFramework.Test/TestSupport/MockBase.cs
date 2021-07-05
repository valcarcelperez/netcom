namespace System.Communication.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.CommunicationFramework.Interfaces;
    using System.Threading;
    using System.Threading.Tasks;

    public class MockBase
    {
        /// <summary>
        /// Parameters that are passed to the methods.
        /// </summary>
        public MockParameters ActualParameters { get; private set; }

        public MockParameters ExpectedParameters { get; private set; }

        public void CompareParameters()
        {
            MockParameters.CompareParameters(ExpectedParameters, ActualParameters);
        }

        public MockBase()
        {
            ActualParameters = new MockParameters();
            ExpectedParameters = new MockParameters();
        }
    }
}
