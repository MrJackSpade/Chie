using Llama.Core.Tests.TestObjects;
using Llama.Core.Utils;
using Llama.Data.Collections;
using Llama.Data.Models;
using Loxifi;
using System.Reflection;

namespace Llama.Core.Tests
{
    [TestClass]
    public class ArrayShifterTests
    {
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
        public void TestLoadedData()
        {
            LlamaToken[] evaluated = LoadArray("kvCache", 8192, 0).ToArray();
            PointerArray<LlamaToken> buffer = LoadArray("buffer_5616", 8192, 5616);

            DummyShifter shifter = new();

            PointerArraySynchronizer<LlamaToken> syncer = new(shifter, 512, LlamaToken.Null);

            KvCacheState<LlamaToken> cacheState = new(evaluated, LlamaToken.Null);

            syncer.TranformCache(cacheState, buffer);
        }

        PointerArray<LlamaToken> LoadArray(string fileName, uint size, uint pointer)
        {
            PointerArray<LlamaToken> toReturn = new(size);
            toReturn.Pointer = pointer;
            uint i = 0;
            foreach (string line in File.ReadAllLines(fileName))
            {
                if(!line.Contains("|"))
                {
                    continue;
                }

                int id = int.Parse(line.To("|"));
                string value = line.From("|");

                if (id == 0)
                {
                    toReturn[i++] = LlamaToken.Null;
                }
                else if (id == 13)
                {
                    toReturn[i++] = new LlamaToken(13, "\\n");
                }
                else
                {
                    toReturn[i++] = new LlamaToken(id, value);
                }
            }

            return toReturn;
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
        public void TestShiftLarge()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', 'd','e','f','1','2','3','4','g', 'h', 'i', 'j', 'k', 'l',
                              'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', '\0', '\0', '\0', '\0',
                              '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0' },
                 new char[] { 'a', 'b', 'c', 'd','e','f','g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                              'q', 'r', 's', 't', 'u', 'v', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0',
                              '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0' },
                 22
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestOverflowSeekFill()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { },
                 new char[] { 'a', 'b', 'c', '\0', '\0', '\0' },
                 3
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

        [TestMethod]
        public void TestDoubleShiftInsert()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'h', 'i', 'j' },
                new char[] { 'a', 'b', 'c', '1', '2', '3', 'd', 'e', 'f', '1', '2', '3', 'h', 'i', 'j' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestDoubleShiftInsertCapped()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'h', 'i', 'j', 'k', 'l', 'm' },
                 new char[] { 'a', 'b', 'c', '1', '2', '3', 'd', 'e', 'f', '1', '2', '3', 'h', 'i', 'j' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestShiftInsert()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f' },
                 new char[] { 'a', 'b', 'c', '1', '2', '3', 'd', 'e', 'f' }

            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestShiftInsertCapped()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'q', 'r', 's' },
                 new char[] { 'a', 'b', 'c', '1', '2', '3', 'd', 'e', 'f' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestShiftStaggeredInsertCapped()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'q', 'r', 's' },
                 new char[] { 'a', 'b', 'c', '1', '2', '3', '4', 'd', 'e', 'f' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestStaggeredDoubleShiftInsert()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'h', 'i', 'j' },
                 new char[] { 'a', 'b', 'c', '1', '2', '3', '4', 'd', 'e', 'f', '1', '2', '3', 'h', 'i', 'j' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestStaggeredDoubleShiftInsertCapped()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'h', 'i', 'j', 'k', 'l', 'm' },
                 new char[] { 'a', 'b', 'c', '1', '2', '3', '4', 'd', 'e', 'f', '1', '2', '3', 'h', 'i', 'j' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }

        [TestMethod]
        public void TestStaggeredShiftInsert()
        {
            ShifterTestHarness shifterTestHarness = ShifterTestHarness.CreateEndExecute(
                 new char[] { 'a', 'b', 'c', 'd', 'e', 'f' },
                 new char[] { 'a', 'b', 'c', '1', '2', '3', '4', 'd', 'e', 'f' }
            );

            Assert.IsTrue(shifterTestHarness.AllMatch());
        }
    }
}