using System;

namespace LukeBot.Interface
{
    public class UnrecognizedInterfaceTypeException: Exception
    {
        public UnrecognizedInterfaceTypeException(InterfaceType type)
            : base(string.Format("Invalid Interface type - {0}", type.ToString()))
        {
        }
    }
}
