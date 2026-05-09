using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using LukeBot.Common;
using System.Threading.Tasks;
using System;


namespace LukeBot.Tests.Common
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void Utils_ConvertArgStringsToTuples_NormalTest()
        {
            List<string> argNames = new()
            {
                "ThisIsAString",
                "ThisIsAnInteger",
                "ThisIsALongString"
            };

            List<string> argValues = new()
            {
                "testString",
                "342",
                "this is a long string with spaces"
            };

            string[] longStringWords = argValues[2].Split(' ');

            List<string> argStrings = new ()
            {
                argNames[0] + "=" + argValues[0],
                argNames[1] + "=" + argValues[1],
                argNames[2] + "=\"" + longStringWords[0]
            };

            for (int i = 1; i < longStringWords.Length; ++i)
            {
                if (i == longStringWords.Length - 1)
                    argStrings.Add(longStringWords[i] + '"');
                else
                    argStrings.Add(longStringWords[i]);
            }

            IEnumerable<(string, string)> tuples = Utils.ConvertArgStringsToTuples(argStrings);

            int ctr = 0;
            foreach ((string a, string b) t in tuples)
            {
                Assert.AreEqual(argNames[ctr], t.a);
                Assert.AreEqual(argValues[ctr], t.b);
                ctr++;
            }
        }

        [TestMethod]
        [Ignore]
        public void Utils_ConvertArgStringsToTuples_ParseErrorTest()
        {
            Assert.IsTrue(false, "This test must be implemented!");
        }

        [TestMethod]
        public void Utils_SplitJSONs()
        {
            string testJSON1 = "{\"this_json\"=10,\"is_simple\"=\"haha\"}";
            string testJSON2 = "{\"test\"=10,\"inner\"={\"test\"=\"haha\"}}";

            string fullmsg = testJSON1 + testJSON2;

            List<string> result = Utils.SplitJSONs(fullmsg);

            Assert.AreEqual(testJSON1, result[0]);
            Assert.AreEqual(testJSON2, result[1]);
        }

        [TestMethod]
        public void Utils_Shorten_Short()
        {
            string msg = "This is short";

            Assert.AreEqual(msg, Utils.Shorten(msg, 30));
            Assert.AreEqual(msg, Utils.Shorten(msg, msg.Length));
        }

        [TestMethod]
        public void Utils_Shorten_Long()
        {
            string msg = "This is a very long message which is meant to check long messages";
            string expected1 = "This is a...";
            string expected2 = "This is a very long message which is meant to check long...";

            Assert.AreEqual(msg, Utils.Shorten(msg, msg.Length));
            Assert.AreEqual(expected2, Utils.Shorten(msg, msg.Length - 1));

            Assert.AreEqual(expected1, Utils.Shorten(msg, 12));
            Assert.AreEqual(expected1, Utils.Shorten(msg, 10));
        }

        [TestMethod]
        public async Task Utils_TryConnect_Successful()
        {
            const int ATTEMPTS_TOTAL = 5;
            const int EXPECTED_RESULT = ATTEMPTS_TOTAL - 1;

            int ret = await Utils.TryConnectAsync(ATTEMPTS_TOTAL, 10, async (attempt) =>
            {
                if (attempt < ATTEMPTS_TOTAL - 1)
                    throw new System.Exception(String.Format("Expected fail #{0}", attempt));
                return attempt;
            });

            Assert.AreEqual(EXPECTED_RESULT, ret);
        }

        class ExpectedInnerException: System.Exception
        {
            public ExpectedInnerException()
                : base("This Exception is expected")
            {}
        }

        [TestMethod]
        public async Task Utils_TryConnect_Failing()
        {
            const int ATTEMPTS_TOTAL = 5;

            ConnectionFailedException e = await Assert.ThrowsExceptionAsync<ConnectionFailedException>(async () =>
            {
                await Utils.TryConnectAsync(ATTEMPTS_TOTAL, 10, async (attempt) =>
                {
                    throw new ExpectedInnerException();
                });
            });

            Assert.IsInstanceOfType(e.InnerException, typeof(ExpectedInnerException));
        }
    }
}
