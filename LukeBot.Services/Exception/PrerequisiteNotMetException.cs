using LukeBot.Common;

namespace LukeBot.Services
{
    public class PrerequisiteNotMetException: Exception
    {
        public PrerequisiteNotMetException(string type)
            : base(string.Format("Prerequisite for module {0} not met", type))
        {
        }
    }
}
