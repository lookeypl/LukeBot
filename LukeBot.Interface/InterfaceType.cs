using System;

namespace LukeBot.Interface
{
    [Flags]
    public enum InterfaceType
    {
        None = 0,
        BasicCLI = 0x0001 | CommandLine,

        CommandLine = 0x10000,
        Graphical = 0x20000,
    }
}