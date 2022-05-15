using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace LukeBot.Tests
{
    [TestClass]
    public class Setup
    {
        [AssemblyInitialize]
        public static void AssemblySetup(TestContext context)
        {
            Common.FileUtils.SetUnifiedCWD();
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {

        }
    }
}