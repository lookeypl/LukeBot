using CommandLine;
using LukeBot.Common;

namespace propmgr;

public class ProgramOptions
{
    [Option('d', "dir",
        Required = false,
        Default = Constants.PROPERTY_STORE_FILE,
        HelpText = "Property Store file to use")
    ]
    public string StoreDir { get; set; }

    public ProgramOptions()
    {
        StoreDir = "";
    }
}
