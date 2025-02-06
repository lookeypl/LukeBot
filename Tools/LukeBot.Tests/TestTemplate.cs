using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace LukeBot.Tests
{
    /**
     * This is a test template to make writing new test files easier
     * It does nothing and should be excluded from build.
     *
     * When making new test units, please follow:
     *   File path = <MainLibName>/<ClassName>Tests.cs
     *     - Ex. Config/ConfUtilTests.cs - tests for LukeBot.Config/ConfUtil.cs
     *   Class name = <LukeBotClassName>Tests
     *     - Ex. `ConfUtilTests` - test class for ConfUtil class
     *   All methods prefixed with "<LukeBotClassName>" and separated by "_"
     *     - TestMethods should add what type of test it is, if specifics are needed separate them with "_"
     *     - ClassInitialize-attributed methods = "<LukeBotClassName>_TestClassStartup"
     *     - TestInitialize-attributed methods = "<LukeBotClassName>_TestInitialize"
     *     - TestCleanup-attributed methods = "<LukeBotClassName>_TestCleanup"
     *     - ClassCleanup-attributed methods = "<LukeBotClassName>_TestInitialize"
     *     - If any of the above are not used, remove them instead of keeping empty stubs
     *     - Keep Initialize/Cleanup order as below
     *
     * TLDR: Copy the example below and rename it as needed
     */
    [TestClass]
    public class TestTemplate_PutYourClassNameHere
    {
        [ClassInitialize]
        public static void TestTemplate_TestClassStartup(TestContext context)
        {
        }

        [TestInitialize]
        public void TestTemplate_TestInitialize()
        {
        }

        [TestCleanup]
        public void TestTemplate_Cleanup()
        {
        }

        [ClassCleanup]
        public static void TestTemplate_TestClassTeardown()
        {
        }


        [TestMethod]
        public void TestTemplate_TestMethod()
        {
        }

        [TestMethod]
        public void TestTemplate_TestMethod_SpecificCase()
        {
        }
    }
}
