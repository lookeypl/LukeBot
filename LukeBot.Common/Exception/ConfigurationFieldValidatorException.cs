namespace LukeBot.Common
{
    public class ConfigurationFieldValidatorException: Exception
    {
        public ConfigurationFieldValidatorException(string fieldName)
            : base(string.Format("Validator for field {0} refused to make changes.", fieldName))
        {
        }
    }
}
