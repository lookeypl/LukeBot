using LukeBot.Common;

namespace LukeBot.Widget.Common
{
    public class ConfigurationFieldValidatorException: LukeBot.Common.Exception
    {
        public ConfigurationFieldValidatorException(string fieldName)
            : base(string.Format("Validator for field {0} refused to make changes.", fieldName))
        {
        }
    }
}
