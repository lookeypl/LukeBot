using System;

namespace LukeBot.Interface
{
    [Flags]
    public enum InterfaceType
    {
        none = 0,
        basic = 0x0001 | CommandLine,
        server = 0x0002 | CommandLine,

        CommandLine = 0x10000,
        Graphical = 0x20000,
    }
}