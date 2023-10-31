using Llama.Core.Tests.TestObjects;

namespace Llama.Core.Tests
{
    [TestClass]
    public class ArrayShifterTests
    {
        [TestMethod]
        public void NoEval()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f' },
                 0
            );

            Assert.AreEqual(0, shifterTestHarness.EPointer);
            Assert.AreEqual(0, shifterTestHarness.BPointer);
        }

        [TestMethod]
        public void SixEval()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestDoubleShift()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', '1', '2', '3', 'd', 'e', 'f', '1', '2', '3', 'h', 'i', 'j' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'h', 'i', 'j' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestDoubleShiftCapped()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', '1', '2', '3', 'd', 'e', 'f', '1', '2', '3', 'h', 'i', 'j' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'h', 'i', 'j', 'k', 'l', 'm' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestShift()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', '1', '2', '3', 'd', 'e', 'f' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestShiftCapped()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', '1', '2', '3', 'd', 'e', 'f' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'q', 'r', 's' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestShiftStaggeredCapped()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', '1', '2', '3', '4', 'd', 'e', 'f' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'q', 'r', 's' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestStaggeredDoubleShift()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', '1', '2', '3', '4', 'd', 'e', 'f', '1', '2', '3', 'h', 'i', 'j' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'h', 'i', 'j' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestStaggeredDoubleShiftCapped()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', '1', '2', '3', '4', 'd', 'e', 'f', '1', '2', '3', 'h', 'i', 'j' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'h', 'i', 'j', 'k', 'l', 'm' }
            );
            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestStaggeredShift()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', '1', '2', '3', '4', 'd', 'e', 'f' },
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }
    }
}