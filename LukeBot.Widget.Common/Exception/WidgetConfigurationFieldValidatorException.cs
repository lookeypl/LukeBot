using LukeBot.Common;

namespace LukeBot.Widget.Common
{
    public class WidgetConfigurationFieldValidatorException: LukeBot.Common.Exception
    {
        public WidgetConfigurationFieldValidatorException(string fieldName)
            : base(string.Format("Validator for field {0} refused to make changes.", fieldName))
        {
        }
    }
}
