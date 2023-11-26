using LukeBot.Common;
using LukeBot.Interface;
using CommandLine;


namespace LukeBot
{
    public class ProgramOptions
    {
        [Option('d', "dir",
            HelpText = "Use a specific Property Store instead of default one.",
            Default = Common.Constants.PROPERTY_STORE_FILE)]
        public string StoreDir { get; set; }

        [Option("cli",
            HelpText = "Specify a type of interface to use. Defaults to basic. Available options:\n" +
                       "   basic, server",
            Default = InterfaceType.basic)]
        public InterfaceType CLI { get; set; }

        public ProgramOptions()
        {
            StoreDir = "";
            CLI = InterfaceType.basic;
        }
    }
}