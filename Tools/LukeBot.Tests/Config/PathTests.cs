using Microsoft.VisualStudio.TestTools.UnitTesting;
using LukeBot.Config;

namespace LukeBot.Tests.Config
{
    [TestClass]
    public class PathTests
    {
        private const string TEST_STRING = "path.test.string";
        private readonly string[] TEST_STRING_ARRAY = TEST_STRING.Split('.');

        [TestMethod]
        public void Path_Start()
        {
            Path p = Path.Start();
            Assert.IsNotNull(p);
            Assert.IsTrue(p.Empty);
            Assert.AreEqual(0, p.Count);
        }

        [TestMethod]
        public void Path_Form()
        {
            Path p = Path.Form(TEST_STRING_ARRAY);
            Assert.IsNotNull(p);
            Assert.IsFalse(p.Empty);
            Assert.AreEqual(TEST_STRING_ARRAY.Length, p.Count);
        }

        [TestMethod]
        public void Path_Parse()
        {
            Path p = Path.Parse(TEST_STRING);
            Assert.IsNotNull(p);
            Assert.IsFalse(p.Empty);
            Assert.AreEqual(TEST_STRING_ARRAY.Length, p.Count);
        }

        [TestMethod]
        public void Path_Push()
        {
            Path p = Path.Start();
            foreach (string s in TEST_STRING_ARRAY)
            {
                p.Push(s);
            }

            Assert.IsFalse(p.Empty);
            Assert.AreEqual(TEST_STRING_ARRAY.Length, p.Count);
        }

        [TestMethod]
        public void Path_Pop()
        {
            Path p = Path.Form(TEST_STRING_ARRAY);
            Assert.IsFalse(p.Empty);
            Assert.AreEqual(TEST_STRING_ARRAY.Length, p.Count);

            for (int idx = 0; idx < TEST_STRING_ARRAY.Length; ++idx)
            {
                Assert.AreEqual(TEST_STRING_ARRAY[idx], p.Pop());
                Assert.AreEqual(TEST_STRING_ARRAY.Length - idx - 1, p.Count);
            }
        }

        [TestMethod]
        public void Path_PopEmpty()
        {
            Assert.ThrowsException<PathEmptyException>(() => Path.Start().Pop());
        }

        [TestMethod]
        public void Path_Copy()
        {
            Path src = Path.Form(TEST_STRING_ARRAY);
            Path cpy = src.Copy();

            Assert.IsNotNull(cpy);
            Assert.AreEqual(src.Empty, cpy.Empty);
            Assert.AreEqual(src.Count, cpy.Count);

            for (int idx = 0; idx < TEST_STRING_ARRAY.Length; ++idx)
            {
                Assert.AreEqual(src.Pop(), cpy.Pop());
            }
        }

        [TestMethod]
        public void Path_ToStringAndParse()
        {
            Path src = Path.Form(TEST_STRING_ARRAY);
            string srcString = src.ToString();

            Path cpy = Path.Parse(srcString);
            Assert.IsNotNull(cpy);
            Assert.AreEqual(src.Empty, cpy.Empty);
            Assert.AreEqual(src.Count, cpy.Count);

            for (int idx = 0; idx < TEST_STRING_ARRAY.Length; ++idx)
            {
                Assert.AreEqual(src.Pop(), cpy.Pop());
            }
        }
    }
}