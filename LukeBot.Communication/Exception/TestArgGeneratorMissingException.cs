using LukeBot.Communication.Common;


namespace LukeBot.Communication
{
    public class TestArgGeneratorMissingException: System.Exception
    {
        public TestArgGeneratorMissingException(string eventName)
            : base(string.Format("Missing test arg generator for event {0}", eventName))
        {}
    }
}
