using LukeBot.Common;

namespace LukeBot.Module
{
    public class PrerequisiteNotMetException: Exception
    {
        public PrerequisiteNotMetException(string moduleName)
            : base(string.Format("Prerequisite for module {0} not met", moduleName))
        {
        }
    }
}
