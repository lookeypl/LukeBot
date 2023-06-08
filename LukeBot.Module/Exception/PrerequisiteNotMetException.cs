using LukeBot.Common;

namespace LukeBot.Module
{
    public class PrerequisiteNotMetException: Exception
    {
        public PrerequisiteNotMetException(ModuleType type)
            : base(string.Format("Prerequisite for module {0} not met", type))
        {
        }
    }
}
