namespace CommunicationFramework.Common.UnitTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.CommunicationFramework.Common;

    [TestClass]
    public class PatternMatcherTest
    {
        private PatternMatcher GetTarget()
        {
            byte[] pattern = new byte[] { 7, 8, 7, 8 };
            PatternMatcher target = new PatternMatcher(pattern);
            return target;
        }

        [TestMethod]
        public void PatternMatcher_NewByte_First_Byte_Not_In_Pattern()
        {
            PatternMatcher target = GetTarget();
            bool actualValue = target.NewByte(10);
            Assert.IsFalse(actualValue);            
        }

        [TestMethod]
        public void PatternMatcher_NewByte_Several_Bytes_Not_In_Pattern()
        {
            PatternMatcher target = GetTarget();
            bool actualValue = target.NewByte(10);
            actualValue = actualValue || target.NewByte(11);
            actualValue = actualValue || target.NewByte(12);
            actualValue = actualValue || target.NewByte(13);
            Assert.IsFalse(actualValue);
        }

        [TestMethod]
        public void PatternMatcher_NewByte_First_Byte_In_Pattern()
        {
            PatternMatcher target = GetTarget();
            bool actualValue = target.NewByte(7);
            Assert.IsFalse(actualValue);
        }

        [TestMethod]
        public void PatternMatcher_NewByte_Several_Bytes_In_Pattern()
        {
            PatternMatcher target = GetTarget();
            bool actualValue = target.NewByte(7);
            actualValue = actualValue || target.NewByte(8);
            actualValue = actualValue || target.NewByte(7);
            Assert.IsFalse(actualValue);
        }

        [TestMethod]
        public void PatternMatcher_NewByte_All_Bytes_In_Pattern()
        {
            PatternMatcher target = GetTarget();
            bool actualValue = target.NewByte(7);
            actualValue = actualValue || target.NewByte(8);
            actualValue = actualValue || target.NewByte(7);
            Assert.IsFalse(actualValue);
            actualValue = target.NewByte(8);
            Assert.IsTrue(actualValue);
        }

        [TestMethod]
        public void PatternMatcher_NewByte_All_Bytes_In_Pattern_After_Several_Bytes_Not_In_Pattern()
        {
            PatternMatcher target = GetTarget();
            bool actualValue = target.NewByte(7);
            actualValue = actualValue || target.NewByte(11);
            actualValue = actualValue || target.NewByte(12);
            actualValue = actualValue || target.NewByte(7);
            actualValue = actualValue || target.NewByte(8);
            actualValue = actualValue || target.NewByte(7);
            Assert.IsFalse(actualValue);
            actualValue = target.NewByte(8);
            Assert.IsTrue(actualValue);
        }

        [TestMethod]
        public void PatternMatcher_NewByte_Pattern_Two_Times()
        {
            PatternMatcher target = GetTarget();
            bool actualValue = target.NewByte(7);
            actualValue = actualValue || target.NewByte(11);
            actualValue = actualValue || target.NewByte(12);
            actualValue = actualValue || target.NewByte(7);
            actualValue = actualValue || target.NewByte(8);
            actualValue = actualValue || target.NewByte(7);
            Assert.IsFalse(actualValue);
            actualValue = target.NewByte(8);
            Assert.IsTrue(actualValue);

            actualValue = target.NewByte(7);
            actualValue = actualValue || target.NewByte(11);
            actualValue = actualValue || target.NewByte(12);
            actualValue = actualValue || target.NewByte(7);
            actualValue = actualValue || target.NewByte(8);
            actualValue = actualValue || target.NewByte(7);
            Assert.IsFalse(actualValue);
            actualValue = target.NewByte(8);
            Assert.IsTrue(actualValue);
        }

        [TestMethod]
        public void PatternMatcher_Reset()
        {
            PatternMatcher target = GetTarget();
            bool actualValue = target.NewByte(7);
            actualValue = actualValue || target.NewByte(11);
            actualValue = actualValue || target.NewByte(12);
            actualValue = actualValue || target.NewByte(7);
            actualValue = actualValue || target.NewByte(8);
            target.Reset();
            actualValue = actualValue || target.NewByte(7);
            actualValue = actualValue || target.NewByte(8);
            actualValue = actualValue || target.NewByte(7);
            Assert.IsFalse(actualValue);
            actualValue = target.NewByte(8);
            Assert.IsTrue(actualValue);
        }
    }
}
