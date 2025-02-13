using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Globalization;
using LukeBot.Common;

namespace LukeBot.Tests
{
    [TestClass]
    public class Setup
    {
        [AssemblyInitialize]
        public static void AssemblySetup(TestContext context)
        {
            FileUtils.SetUnifiedCWD();

            if (!File.Exists("Data/props_test.lukebot"))
                throw new System.SystemException("Data/props_test.lukebot property file not visible or found from test fixture's perspective.");

            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {

        }
    }
}