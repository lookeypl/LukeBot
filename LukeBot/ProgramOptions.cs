using LukeBot.Common;
using CommandLine;


namespace LukeBot
{
    public class ProgramOptions
    {
        [Option('d', "dir",
            HelpText = "Use a specific Property Store instead of default one.",
            Default = Common.Constants.PROPERTY_STORE_FILE)]
        public string StoreDir { get; set; }

        public ProgramOptions()
        {
            StoreDir = "";
        }
    }
}